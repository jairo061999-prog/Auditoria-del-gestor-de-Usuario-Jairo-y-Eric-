# Resumen de corrección:
Se mantienen los usuarios de prueba:
admin / admin1234 (Rol: admin)
ana / ana2024 (Rol: jugador)
Contraseñas y Tokens: Sustituido MD5 por PBKDF2-SHA256

Seguridad y Control de Acceso: Consultas parametrizadas  y comprobación de autorización/roles en el servidor (AuthService).

Protección de Login y Registro: Bloqueo de 5 minutos tras 5 intentos fallidos, mensajes de error genéricos y restricción del registro a rol jugador con contraseñas.

Red y Secretos: Uso obligatorio de HTTPS, eliminación de API keys expuestas y volcado de errores detallados en errores.log sin mostrar trazados en pantalla.

Entre otros errores
