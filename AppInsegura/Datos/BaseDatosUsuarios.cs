using System;
using System.Collections.Generic;
using System.Linq;
using AppInsegura.Modelos;

namespace AppInsegura.Datos
{
    // Simula una tabla de base de datos (equivalente a una tabla SQLite/sqflite).
    // No usa un motor real para que el proyecto compile sin dependencias externas.
    public class BaseDatosUsuarios
    {
        private readonly List<Usuario> usuarios = new List<Usuario>();

        public void Agregar(Usuario usuario)
        {
            usuarios.Add(usuario);
        }

        // CORRECCIÓN (apuntes: mínimo privilegio / no confiar en la entrada):
        // antes devolvía la lista interna (con hashes y modificable desde fuera).
        // Ahora devuelve una copia de solo lectura.
        public IReadOnlyList<Usuario> ListarTodos()
        {
            return usuarios.ToList().AsReadOnly();
        }

        public Usuario? BuscarExacto(string nombre)
        {
            // CORRECCIÓN: comparación sin distinguir mayúsculas para evitar
            // suplantaciones del tipo "Admin" / "admin".
            return usuarios.FirstOrDefault(u =>
                string.Equals(u.Nombre, nombre, StringComparison.OrdinalIgnoreCase));
        }

        // CORRECCIÓN (apuntes §2 "Consultas parametrizadas"):
        // la consulta es una constante y el valor del usuario viaja SIEMPRE como
        // parámetro (@nombre), nunca concatenado dentro del texto SQL.
        public Usuario? BuscarPorNombre(string nombreBuscado)
        {
            const string consulta = "SELECT * FROM usuarios WHERE nombre = @nombre";
            var parametros = new Dictionary<string, string> { ["@nombre"] = nombreBuscado };
            return EjecutarConsultaParametrizada(consulta, parametros);
        }

        // CORRECCIÓN (apuntes §2): simulación de un motor SQL con parámetros: el valor se trata como DATO,
        // nunca se interpreta como parte de la consulta.
        // CORRECCIÓN: se elimina el Console.WriteLine("[DB] ...") que mostraba la consulta.
        private Usuario? EjecutarConsultaParametrizada(string consulta, Dictionary<string, string> parametros)
        {
            if (consulta != "SELECT * FROM usuarios WHERE nombre = @nombre")
            {
                return null; // fallar de forma segura
            }

            string nombre = parametros["@nombre"];
            return usuarios.FirstOrDefault(u => u.Nombre == nombre);
        }
    }
}
