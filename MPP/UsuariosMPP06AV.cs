using BE;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;


namespace MPP
{
    public class UsuariosMPP06AV
    {
        UsuariosDAL06AV UsuariosDAL = new UsuariosDAL06AV();

        #region Obtener
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

        public Usuario06AV ObtenerPorLogin(string login)
        {
            var parametros = new Dictionary<string, object>
            {
                { "@login", login }
            };

            DataTable tabla = UsuariosDAL.ObtenerPorLogin(parametros);

            if (tabla.Rows.Count == 0)
                return null;

            return MapearUsuario(tabla.Rows[0]);
        }
        #endregion

        #region Alta, Modificacion y Eliminar
        public bool CrearUsuario(Usuario06AV usuario)
        {
            var parametros = new Dictionary<string, object>
            {
                { "@dni", usuario.Dni },
                { "@nombre", usuario.Nombre },
                { "@apellido", usuario.Apellido },
                { "@email", usuario.Email },
                { "@IdRol", usuario.IdRol },
                { "@activo", usuario.Activo },
                { "@bloqueado", usuario.Bloqueado },
                { "@login", usuario.Login },
                { "@contrasenia", usuario.Contrasenia },
                { "@debeCambiarContrasenia", true }
            };

            return UsuariosDAL.CrearUsuario(parametros);
        }

        public bool EditarUsuario(Usuario06AV usuario)
        {
            var parametros = new Dictionary<string, object>
            {
                { "@email", usuario.Email },
                { "@rol", usuario.IdRol},
                { "@dni", usuario.Dni }
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

        #endregion

        #region Acciones sobre un usuario

        public bool ReactivarUsuario(string dni)
        {

            var parametros = new Dictionary<string, object>
            {
                { "@dni", dni }
            };

            return UsuariosDAL.ReactivarUsuario(parametros);
        }

        public bool DesactivarUsuario(string dni)
        {

            var parametros = new Dictionary<string, object>
            {
                { "@dni", dni }
            };

            return UsuariosDAL.DesactivarUsuario(parametros);
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

        #endregion

        #region Mapper
        private Usuario06AV MapearUsuario(DataRow row)
        {
            return new Usuario06AV
            {
                Dni = row["Dni"].ToString(),
                Nombre = row["Nombre"].ToString(),
                Apellido = row["Apellido"].ToString(),
                Email = row["Email"].ToString(),
                IdRol = row["IdRol"].ToString(),
                DebeCambiarContrasenia = Convert.ToBoolean(row["DebeCambiarContrasenia"]),
                Activo = Convert.ToBoolean(row["Activo"]),
                Bloqueado = Convert.ToBoolean(row["Bloqueado"]),
                Login = row["Login"].ToString(),
                Contrasenia = row["Contrasenia"].ToString()
            };
        }

        #endregion

        #region Intentos Login
        public bool RegistrarIntento(string id, string dni, bool exitoso)
        {
            var parametros = new Dictionary<string, object>
        {
            { "@id", id },
            { "@dni", dni },
            { "@exitoso", exitoso }
        };
            return UsuariosDAL.RegistrarIntentoSesion(parametros);
        }

        public int ContarFallidosRecientes(string dni, int horas)
        {
            var parametros = new Dictionary<string, object>
            {
                { "@dni", dni },
                { "@desde", DateTime.Now.AddHours(-horas) }
            };
            return UsuariosDAL.ObtenerIntentosFallidosRecientes(parametros);
        }

        public bool LimpiarIntentosFallidos(string dni)
        {
            var parametros = new Dictionary<string, object> { { "@dni", dni } };
            return UsuariosDAL.LimpiarIntentosFallidos(parametros);
        }
        #endregion
    }
}
