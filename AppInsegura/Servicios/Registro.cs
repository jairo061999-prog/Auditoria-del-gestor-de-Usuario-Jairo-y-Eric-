using System;
using System.IO;

namespace AppInsegura.Servicios
{
    // CORRECCIÓN - NUEVO (apuntes del error 8): registro interno para el desarrollador y los técnicos.
    // Al usuario solo se le muestran mensajes genéricos.
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
            }
        }
    }
}
