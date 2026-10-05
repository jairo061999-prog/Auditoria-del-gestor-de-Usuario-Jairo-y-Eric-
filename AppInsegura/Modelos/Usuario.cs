namespace AppInsegura.Modelos
{
    public class Usuario
    {
        public string Nombre { get; set; } = "";

        // CORRECCIÓN (apuntes del error 3): aquí ya NO hay un hash MD5 sino una cadena
        // con formato "iteraciones.sal.hash" (PBKDF2 + sal aleatoria).
        public string ContrasenaHash { get; set; } = "";
        public string Rol { get; set; } = "jugador";
        public string TokenSesion { get; set; } = "";
    }
}
