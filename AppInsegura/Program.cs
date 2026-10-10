using System;
using System.Text;
using AppInsegura.Datos;
using AppInsegura.Modelos;
using AppInsegura.Servicios;

namespace AppInsegura
{
    public class Program
    {
        private static readonly BaseDatosUsuarios baseDatos = new BaseDatosUsuarios();
        private static readonly AuthService auth = new AuthService(baseDatos);
        private static Usuario? usuarioActual = null;

        public static void Main(string[] args)
        {
            CargarUsuariosDePrueba();

            Console.WriteLine("=== Gestor de Usuarios y Partidas ===");
            // CORRECCIÓN: ya no se muestra por pantalla "(usuarios de prueba: admin/admin1234, ana/ana2024)".
            Console.WriteLine();

            bool salir = false;
            while (!salir)
            {
                MostrarMenu();
                string opcion = Console.ReadLine() ?? "";

                try
                {
                    switch (opcion)
                    {
                        case "1": Registrar(); break;
                        case "2": IniciarSesion(); break;
                        case "3": BuscarUsuario(); break;
                        case "4": VerPerfil(); break;
                        case "5": PanelAdministracion(); break;
                        case "6": SincronizarConServidor(); break;
                        case "7": CerrarSesion(); break; // CORRECCIÓN (apuntes del error 5): nueva opción para cerrar sesión y borrar el token.
                        case "0": salir = true; break;
                        default: Console.WriteLine("Opción no válida."); break;
                    }
                }
                // CORRECCIÓN (apuntes del error 8): errores previstos (validación / permisos): mensajes pensados para el usuario.
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine("Acceso denegado.");
                }
                // CORRECCIÓN (apuntes del error 8): antes se mostraba ex.ToString() (traza completa).
                // Ahora muestra un mensaje genérico al usuario y detalle técnico al registro interno.
                catch (Exception ex)
                {
                    Console.WriteLine("Ha ocurrido un error.");
                    Registro.Error(ex.ToString());
                }

                Console.WriteLine();
            }

            Console.WriteLine("Hasta luego.");
        }

        // CORRECCIÓN (apuntes del error 3 y error 4) - USUARIOS DE PRUEBA del ejercicio: admin/admin1234 y ana/ana2024.
        // Por seguridad, la contraseña se guarda con hash PBKDF2 y sal (nunca en limpio) y ya no la mostramos por pantalla al iniciar.
        // - Son contraseñas conocidas, válidas únicamente para esta práctica: en una aplicación real no se dejarían escritas en el código (apuntes del error 4).
        // - Si se define APPINSEGURA_ADMIN_PASSWORD, esa será la contraseña del admin en lugar de admin1234.
        private static void CargarUsuariosDePrueba()
        {
            string passAdmin = Environment.GetEnvironmentVariable("APPINSEGURA_ADMIN_PASSWORD") ?? "admin1234";

            try
            {
                auth.RegistrarUsuarioDePrueba("admin", passAdmin, "admin");
                auth.RegistrarUsuarioDePrueba("ana", "ana2024", "jugador");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("Aviso: no se han podido crear los usuarios de prueba.");
            }
        }

        private static void MostrarMenu()
        {
            Console.WriteLine("------------------------------------");
            Console.WriteLine($"Usuario actual: {(usuarioActual != null ? usuarioActual.Nombre : "ninguno")}");
            Console.WriteLine("1. Registrar usuario");
            Console.WriteLine("2. Iniciar sesión");
            Console.WriteLine("3. Buscar usuario por nombre");
            Console.WriteLine("4. Ver mi perfil");
            // CORRECCIÓN (apuntes del error 7): ocultar la opción es solo cosmética: la comprobación real se hace en AuthService.
            if (auth.EsAdministrador(usuarioActual))
            {
                Console.WriteLine("5. Panel de administración");
            }
            Console.WriteLine("6. Sincronizar partida con el servidor");
            Console.WriteLine("7. Cerrar sesión"); // CORRECCIÓN (apuntes del error 5): opción nueva.
            Console.WriteLine("0. Salir");
            Console.Write("Elige una opción: ");
        }

        private static void Registrar()
        {
            Console.Write("Nombre de usuario: ");
            string nombre = (Console.ReadLine() ?? "").Trim();
            Console.Write("Contraseña: ");
            string contrasena = LeerContrasena();

            // CORRECCIÓN (apuntes del error 1): la validación y las reglas están en AuthService (lanza ArgumentException si no son válidas).
            Usuario nuevo = auth.Registrar(nombre, contrasena);
            // CORRECCIÓN: ya no se muestra el rol asignado.
            Console.WriteLine($"Usuario '{nuevo.Nombre}' registrado correctamente.");
        }

        private static void IniciarSesion()
        {
            Console.Write("Nombre de usuario: ");
            string nombre = (Console.ReadLine() ?? "").Trim();
            Console.Write("Contraseña: ");
            string contrasena = LeerContrasena();

            Usuario? usuario = auth.IniciarSesion(nombre, contrasena);
            if (usuario == null)
            {
                // CORRECCIÓN (apuntes del error 8): mensaje genérico, no distingue usuario inexistente / contraseña errónea / cuenta bloqueada.
                Console.WriteLine("Usuario o contraseña incorrectos.");
                return;
            }

            usuarioActual = usuario;
            Console.WriteLine($"Bienvenido, {usuario.Nombre}.");
        }

        private static void CerrarSesion() // CORRECCIÓN (apuntes del error 5): al cerrar sesión se borra el token.
        {
            if (usuarioActual != null)
            {
                usuarioActual.TokenSesion = "";
                usuarioActual = null;
            }
            Console.WriteLine("Sesión cerrada.");
        }

        private static void BuscarUsuario()
        {
            // CORRECCIÓN (apuntes del error 7): antes cualquiera (sin sesión) podía buscar y ver el rol.
            if (usuarioActual == null)
            {
                Console.WriteLine("Primero debes iniciar sesión.");
                return;
            }

            Console.Write("Nombre a buscar: ");
            string nombre = (Console.ReadLine() ?? "").Trim();

            // CORRECCIÓN (apuntes del error 1 y error 2): la entrada se valida en el servicio y la consulta va parametrizada.
            bool existe = auth.ExisteUsuario(usuarioActual, nombre);
            Console.WriteLine(existe
                ? $"Encontrado: {nombre}"
                : "No se ha encontrado ningún usuario con ese nombre.");
        }

        private static void VerPerfil()
        {
            if (usuarioActual == null)
            {
                Console.WriteLine("Primero debes iniciar sesión.");
                return;
            }

            Console.WriteLine($"Nombre: {usuarioActual.Nombre}");
            Console.WriteLine($"Rol: {usuarioActual.Rol}");
            // CORRECCIÓN (apuntes del error 8): se elimina "Token de sesión: ..." de la pantalla.
        }

        private static void PanelAdministracion()
        {
            // CORRECCIÓN (apuntes del error 7): la autorización se comprueba al ejecutar la acción
            // (ListarUsuarios lanza UnauthorizedAccessException si no eres admin),
            // no solo ocultando la opción del menú.
            var usuarios = auth.ListarUsuarios(usuarioActual);

            Console.WriteLine("=== PANEL DE ADMINISTRACIÓN ===");
            Console.WriteLine("Lista de usuarios registrados:");
            foreach (var (nombre, rol) in usuarios)
            {
                Console.WriteLine($" - {nombre} ({rol})");
            }
        }

        private static void SincronizarConServidor()
        {
            if (usuarioActual == null)
            {
                Console.WriteLine("Primero debes iniciar sesión.");
                return;
            }

            // NOTA: el servidor debe recalcular/verificar la puntuación; el cliente no es de fiar.
            // El valor 1000 es solo la simulación original del ejercicio.
            var red = new RedService();
            bool ok = red.EnviarPuntuacion(usuarioActual.Nombre, 1000, usuarioActual.TokenSesion); // CORRECCIÓN (apuntes del error 4): se envía el token de sesión, no una clave fija.

            Console.WriteLine(ok
                ? "Partida sincronizada."
                : "No se ha podido sincronizar la partida. Inténtalo más tarde.");
        }

        // CORRECCIÓN: la contraseña no se muestra mientras se escribe.
        private static string LeerContrasena()
        {
            if (Console.IsInputRedirected)
                return Console.ReadLine() ?? "";

            var sb = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo tecla = Console.ReadKey(intercept: true);
                if (tecla.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                if (tecla.Key == ConsoleKey.Backspace)
                {
                    if (sb.Length > 0) sb.Length--;
                }
                else if (!char.IsControl(tecla.KeyChar) && sb.Length < 128)
                {
                    sb.Append(tecla.KeyChar);
                }
            }
            return sb.ToString();
        }
    }
}
