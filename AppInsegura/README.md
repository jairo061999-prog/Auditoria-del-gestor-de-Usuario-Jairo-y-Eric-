# Gestor de Usuarios y Partidas (versión corregida)

Versión corregida de `AppInsegura` aplicando los apuntes de programación segura
(ACT01 – Auditoría, DAM2 0490 Procesos y servicios). Todos los cambios están
comentados en el código con el prefijo `// CORRECCIÓN`.

## Ejecutar

```bash
cd AppInsegura          # carpeta del proyecto
dotnet run
```

## Usuarios de prueba (se crean al arrancar)

| Usuario | Contraseña | Rol |
|---|---|---|
| admin | admin1234 | admin |
| ana | ana2024 | jugador |

* Ya **no se muestran por pantalla** al arrancar y se guardan solo como hash PBKDF2 con sal.
* Son contraseñas conocidas, válidas solo para esta práctica (en una aplicación real no se dejarían en el código).
* Opcional: si defines la variable `APPINSEGURA_ADMIN_PASSWORD`, esa será la contraseña del admin.
* Los usuarios nuevos registrados desde el menú necesitan una contraseña de 8 a 64 caracteres
  (los de prueba se crean por una vía interna: `ana2024` tiene 7).
* Opcional: `APPINSEGURA_URL` cambia la URL del servidor (debe ser `https://`).
* Requiere el SDK indicado en `AppInsegura.csproj` (`net10.0`).
* Los usuarios viven en memoria: al cerrar la aplicación se pierden los registrados desde el menú.

## Resumen de correcciones

| Área | Antes | Ahora |
|---|---|---|
| Contraseñas | MD5 sin sal | PBKDF2-SHA256, sal aleatoria, 210.000 iteraciones, comparación en tiempo constante |
| Token de sesión | `Random`, 6 dígitos, en pantalla, en log y en `sesion.txt` | `RandomNumberGenerator`, 256 bits, solo en memoria, nunca se muestra |
| Búsqueda | SQL concatenado | Consulta parametrizada + validación por lista blanca |
| Panel admin | Solo ocultaba la opción | Autorización comprobada en `AuthService.ListarUsuarios` |
| Registro | Sin validar, rol libre, duplicados | Validación, rol siempre `jugador`, sin duplicados |
| Login | Intentos ilimitados | Bloqueo 5 min tras 5 fallos; mensaje genérico |
| Red | HTTP, API key en el código y en la URL | HTTPS, sin secretos en el cliente, POST + token en cabecera |
| Errores | Traza completa en pantalla | Mensaje genérico + `errores.log` interno |
| Secretos | `sk_live_...` en el código y contraseñas de prueba en pantalla | Clave eliminada; las de prueba ya no se muestran y se guardan con hash |
