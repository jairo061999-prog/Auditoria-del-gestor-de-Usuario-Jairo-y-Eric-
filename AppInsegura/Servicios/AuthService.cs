using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AppInsegura.Datos;
using AppInsegura.Modelos;

namespace AppInsegura.Servicios
{
    public class AuthService
    {
        private readonly BaseDatosUsuarios baseDatos;

        // CORRECCIÓN (apuntes del error 3): hash lento con sal -> PBKDF2-SHA256 (antes MD5 sin sal).
        private const int Iteraciones = 210_000;
        private const int TamanoSal = 16;
        private const int TamanoHash = 32;

        // CORRECCIÓN (apuntes del error 1): validación por lista blanca.
        private static readonly Regex PatronNombre = new Regex("^[A-Za-z0-9_]{3,20}$", RegexOptions.Compiled);
        public const int LongitudMinimaContrasena = 8;
        public const int LongitudMaximaContrasena = 64;

        // CORRECCIÓN (apuntes del error 1): protección básica contra fuerza bruta (defensa en profundidad).
        private const int MaxIntentosFallidos = 5;
        private static readonly TimeSpan TiempoBloqueo = TimeSpan.FromMinutes(5);
        private readonly Dictionary<string, (int intentos, DateTime? bloqueadoHasta)> intentos =
            new Dictionary<string, (int, DateTime?)>(StringComparer.OrdinalIgnoreCase);

        // CORRECCIÓN (apuntes del error 8): hash "señuelo" para que el tiempo de respuesta sea parecido si el usuario no existe.
        private readonly string hashSenuelo;

        public AuthService(BaseDatosUsuarios baseDatos)
        {
            this.baseDatos = baseDatos;
            hashSenuelo = CalcularHash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)));
        }

        public static bool NombreValido(string? nombre) =>
            !string.IsNullOrWhiteSpace(nombre) && PatronNombre.IsMatch(nombre);

        // CORRECCIÓN (apuntes del error 1 y el error 7): el registro público SIEMPRE crea rol "jugador".
        // Antes aceptaba un parámetro "rol" cualquiera y no validaba nada.
        public Usuario Registrar(string nombre, string contrasena)
        {
            return CrearUsuario(nombre, contrasena, "jugador");
        }

        // CORRECCIÓN: solo para los usuarios de prueba del ejercicio (admin y ana). Valida el nombre, pero
        // NO aplica el mínimo de 8 caracteres, porque la contraseña original de "ana" tiene 7.
        // El registro desde el menú (opción 1) SÍ exige entre 8 y 64 caracteres.
        public Usuario RegistrarUsuarioDePrueba(string nombre, string contrasena, string rol)
        {
            return CrearUsuario(nombre, contrasena, rol, validarLongitudContrasena: false);
        }

        private Usuario CrearUsuario(string nombre, string contrasena, string rol, bool validarLongitudContrasena = true)
        {
            if (!NombreValido(nombre))
                throw new ArgumentException("Nombre no válido (3-20 caracteres: letras, números o guion bajo).");

            if (string.IsNullOrEmpty(contrasena) ||
                (validarLongitudContrasena && contrasena.Length < LongitudMinimaContrasena) ||
                contrasena.Length > LongitudMaximaContrasena)
                throw new ArgumentException(
                    $"La contraseña debe tener entre {LongitudMinimaContrasena} y {LongitudMaximaContrasena} caracteres.");

            if (baseDatos.BuscarExacto(nombre) != null)
                throw new ArgumentException("Ese nombre de usuario no está disponible.");
            var nuevo = new Usuario
            {
                Nombre = nombre,
                ContrasenaHash = CalcularHash(contrasena),
                Rol = rol,
                TokenSesion = ""
            };
            baseDatos.Agregar(nuevo);
            return nuevo;
        }
        public Usuario? IniciarSesion(string nombre, string contrasena)
        {
            if (!NombreValido(nombre) || string.IsNullOrEmpty(contrasena) || // CORRECCIÓN (apuntes del error 1): se valida la entrada antes de usarla.
                contrasena.Length > LongitudMaximaContrasena)
                return null;
            if (EstaBloqueado(nombre)) // CORRECCIÓN (apuntes §1): bloqueo anti fuerza bruta.
                return null;

            Usuario? usuario = baseDatos.BuscarExacto(nombre);

            // CORRECCIÓN (apuntes del error 8): se verifica SIEMPRE un hash (aunque el usuario no exista) para no delatar su existencia por tiempo.
            bool correcto = VerificarContrasena(contrasena, usuario?.ContrasenaHash ?? hashSenuelo);

            if (usuario == null || !correcto)
            {
                RegistrarFallo(nombre);
                return null;
            }

            intentos.Remove(nombre);

            // CORRECCIÓN (apuntes del error 5): token con generador criptográfico y 256 bits.
            usuario.TokenSesion = GenerarTokenSesion();

            // CORRECCIÓN (apuntes del error 8): ya no se imprime el token ni se guarda sesion.txt en disco.
            return usuario;
        }

        // CORRECCIÓN (apuntes del error 7): la autorización se comprueba AQUÍ (capa "servidor"),
        // no escondiendo la opción del menú. Se contrasta con el registro guardado
        // (nombre + token), no con lo que diga el objeto que maneja el cliente.
        public bool EsAdministrador(Usuario? solicitante)
        {
            if (solicitante == null || string.IsNullOrEmpty(solicitante.TokenSesion))
                return false;
            Usuario? real = baseDatos.BuscarExacto(solicitante.Nombre);
            if (real == null || string.IsNullOrEmpty(real.TokenSesion))
                return false;
            bool tokenOk = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(real.TokenSesion),
                Encoding.UTF8.GetBytes(solicitante.TokenSesion));
            return tokenOk && real.Rol == "admin"; // si algo no cuadra, se deniega (fallar de forma segura)
        }
        // CORRECCIÓN (apuntes el error 7): solo devuelve nombre y rol; nunca hashes ni tokens.
        public IReadOnlyList<(string Nombre, string Rol)> ListarUsuarios(Usuario? solicitante)
        {
            if (!EsAdministrador(solicitante))
                throw new UnauthorizedAccessException("Acceso denegado.");

            var lista = new List<(string, string)>();
            foreach (Usuario u in baseDatos.ListarTodos())
                lista.Add((u.Nombre, u.Rol));
            return lista;
        }

        // CORRECCIÓN (apuntes del error 2 y del error 7): cualquier usuario autenticado puede buscar, pero solo ve si existe (sin rol).
        public bool ExisteUsuario(Usuario? solicitante, string nombreBuscado)
        {
            if (solicitante == null || string.IsNullOrEmpty(solicitante.TokenSesion))
                throw new UnauthorizedAccessException("Acceso denegado.");
            if (!NombreValido(nombreBuscado))
                throw new ArgumentException("Nombre no válido.");
            return baseDatos.BuscarPorNombre(nombreBuscado) != null;
        }
        private static string CalcularHash(string contrasena)
        {
            byte[] sal = RandomNumberGenerator.GetBytes(TamanoSal);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, Iteraciones, HashAlgorithmName.SHA256, TamanoHash);
            return $"{Iteraciones}.{Convert.ToBase64String(sal)}.{Convert.ToBase64String(hash)}";
        }
        private static bool VerificarContrasena(string contrasena, string almacenado)
        {
            string[] partes = almacenado.Split('.');
            if (partes.Length != 3 || !int.TryParse(partes[0], out int iter))
                return false;
            byte[] sal = Convert.FromBase64String(partes[1]);
            byte[] esperado = Convert.FromBase64String(partes[2]);
            byte[] calculado = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, iter, HashAlgorithmName.SHA256, esperado.Length);
            // CORRECCIÓN (apuntes del error 3): comparación en tiempo constante.
            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }
        private static string GenerarTokenSesion()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
        //CORRECCIÓN
        private bool EstaBloqueado(string nombre)
        {
            if (intentos.TryGetValue(nombre, out var estado) && estado.bloqueadoHasta.HasValue)
            {
                if (DateTime.UtcNow < estado.bloqueadoHasta.Value) return true;
                intentos.Remove(nombre);
            }
            return false;
        }

        private void RegistrarFallo(string nombre)
        {
            intentos.TryGetValue(nombre, out var estado);
            int n = estado.intentos + 1;
            intentos[nombre] = n >= MaxIntentosFallidos
                ? (n, DateTime.UtcNow.Add(TiempoBloqueo))
                : (n, null);
        }
    }
}
