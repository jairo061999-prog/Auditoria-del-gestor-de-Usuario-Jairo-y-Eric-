using System;
using System.IO;

namespace AppInsegura.Servicios
{
    // CORRECCIÓN - NUEVO (apuntes §8): registro interno para el desarrollador y los técnicos.
    // Al usuario solo se le muestran mensajes genéricos. NUNCA se registran
    // contraseñas, tokens ni datos personales.
    public static class Registro
    {
        private static readonly string Ruta = Path.Combine(AppContext.BaseDirectory, "errores.log");

        public static void Error(string mensaje)
        {
            try
            {
                File.AppendAllText(Ruta, $"{DateTime.UtcNow:O} [ERROR] {mensaje}{Environment.NewLine}");
            }
            catch
            {
                // Si no se puede escribir el registro, la aplicación no debe caerse ni mostrar detalles.
            }
        }
    }
}
