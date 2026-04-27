using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BE;


namespace MPP
{
    public class MPP_Usuarios
    {
        //D AL dal = new DAL();
        DAL_Usuarios UsuariosDAL = new DAL_Usuarios();

        public List<Usuario06AV> ObtenerTodos()
        {
            DataTable tabla = UsuariosDAL.ObtenerTodos(new Dictionary<string, object>());

            List<Usuario06AV> lista = new List<Usuario06AV>();

            foreach (DataRow row in tabla.Rows)
            {
                lista.Add(MapearUsuario(row));
            }

            return lista;
        }

        public Usuario06AV ObtenerPorDni(string dni)
        {

            var parametros = new Dictionary<string, object>
            {
                { "@dni", dni }
            };

            DataTable tabla = UsuariosDAL.ObtenerPorDni(parametros);

            if (tabla.Rows.Count == 0)
                return null;

            return MapearUsuario(tabla.Rows[0]);
        }

        public bool CrearUsuario(Usuario06AV usuario)
        {
            var parametros = new Dictionary<string, object>
            {
                { "@dni", usuario.Dni },
                { "@nombre", usuario.Nombre },
                { "@apellido", usuario.Apellido },
                { "@email", usuario.Email },
                { "@rol", usuario.Rol },
                { "@activo", usuario.Activo },
                { "@bloqueado", usuario.Bloqueado },
                { "@login", usuario.Login },
                { "@contrasenia", usuario.Contrasenia }
            };

            return UsuariosDAL.CrearUsuario(parametros);
        }

        public bool EditarUsuario(Usuario06AV usuario)
        {
            var parametros = new Dictionary<string, object>
            {
                { "@dni", usuario.Dni },
                { "@nombre", usuario.Nombre },
                { "@apellido", usuario.Apellido },
                { "@email", usuario.Email },
                { "@rol", usuario.Rol }
            };

            return UsuariosDAL.EditarUsuario(parametros);
        }

        public bool EliminarUsuario(string dni)
        {

            var parametros = new Dictionary<string, object>
            {
                { "@dni", dni }
            };

            return UsuariosDAL.EliminarUsuario(parametros);
        }

        public bool ReactivarUsuario(string dni)
        {

            var parametros = new Dictionary<string, object>
            {
                { "@dni", dni }
            };

            return UsuariosDAL.ReactivarUsuario(parametros);
        }

        public bool BloquearUsuario(string dni)
        {

            var parametros = new Dictionary<string, object>
            {
                { "@dni", dni }
            };

            return UsuariosDAL.BloquearUsuario(parametros);
        }

        public bool DesbloquearUsuario(string dni)
        {

            var parametros = new Dictionary<string, object>
            {
                { "@dni", dni }
            };

            return UsuariosDAL.DesbloquearUsuario(parametros);
        }

        public bool CambiarContraseña(string dni, string actual, string nueva)
        {

            var parametros = new Dictionary<string, object>
            {
                { "@dni", dni },
                { "@actual", actual },
                { "@nueva", nueva }
            };

            return UsuariosDAL.CambiarContraseña(parametros);
        }

        // 🔹 Mapper clave
        private Usuario06AV MapearUsuario(DataRow row)
        {
            return new Usuario06AV
            {
                Dni = row["Dni"].ToString(),
                Nombre = row["Nombre"].ToString(),
                Apellido = row["Apellido"].ToString(),
                Email = row["Email"].ToString(),
                Rol = row["Rol"].ToString(),
                Activo = Convert.ToBoolean(row["Activo"]),
                Bloqueado = Convert.ToBoolean(row["Bloqueado"]),
                Login = row["Login"].ToString(),
                Contrasenia = row["Contrasenia"].ToString()
            };
        }
    }
}
