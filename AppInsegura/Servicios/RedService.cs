using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppInsegura.Servicios
{
    public class RedService
    {
        // CORRECCIÓN (apuntes del error 4 y 6):
        //  - Se elimina la ApiKey "sk_live_..." escrita en el código: en una app cliente NO hay secretos.
        //    La clave real vive en el servidor; el cliente se identifica con el token de sesión del usuario.
        //  - La URL pasa de HTTP a HTTPS y puede configurarse sin recompilar.
        private static readonly string UrlServidor =
            Environment.GetEnvironmentVariable("APPINSEGURA_URL") ?? "https://api.miapp-insegura.local/puntuaciones";

        private const int PuntuacionMaxima = 1_000_000;

        // CORRECCIÓN: devuelve true/false; el detalle técnico NO se muestra al usuario (apuntes §8).
        public bool EnviarPuntuacion(string nombreUsuario, int puntuacion, string tokenSesion)
        {
            // CORRECCIÓN (apuntes del error 1): validar antes de enviar.
            if (!AuthService.NombreValido(nombreUsuario) || puntuacion < 0 || puntuacion > PuntuacionMaxima ||
                string.IsNullOrEmpty(tokenSesion))
                return false;

            // CORRECCIÓN (apuntes del error 6): fallar de forma segura: si por error se configurase HTTP, no se envía nada.
            if (!UrlServidor.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                Registro.Error("URL del servidor no es HTTPS; envío cancelado.");
                return false;
            }

            try
            {
                return EnviarPuntuacionAsync(nombreUsuario, puntuacion, tokenSesion).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // CORRECCIÓN (apuntes del error 8): detalle técnico -> registro interno (sin datos sensibles). Al usuario, mensaje genérico.
                Registro.Error($"Fallo de red al sincronizar: {ex.GetType().Name}");
                return false;
            }
        }

        private async Task<bool> EnviarPuntuacionAsync(string nombreUsuario, int puntuacion, string tokenSesion)
        {
            // CORRECCIÓN (apuntes del error 6): se mantiene la validación de certificados por defecto (NO se desactiva).
            using var cliente = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            // CORRECCIÓN: los datos van en el cuerpo (POST + JSON), no en la URL, y el token en la cabecera.
            var peticion = new HttpRequestMessage(HttpMethod.Post, UrlServidor)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { usuario = nombreUsuario, puntos = puntuacion }),
                    Encoding.UTF8, "application/json")
            };
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenSesion);
            // CORRECCIÓN: ya no se imprime la URL (con la api_key) por pantalla.
            HttpResponseMessage respuesta = await cliente.SendAsync(peticion);
            return respuesta.IsSuccessStatusCode;
        }
    }
}
