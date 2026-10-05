namespace AppInsegura.Modelos
{
    public class Usuario
    {
        public string Nombre { get; set; } = "";

        // CORRECCIÓN (apuntes §3): aquí ya NO hay un hash MD5 sino una cadena
        // con formato "iteraciones.sal.hash" (PBKDF2 + sal aleatoria).
        public string ContrasenaHash { get; set; } = "";

        public string Rol { get; set; } = "jugador";

        // El token solo vive en memoria durante la sesión (no se escribe a disco ni se muestra).
        public string TokenSesion { get; set; } = "";
    }
}
