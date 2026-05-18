using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace BE
{
    public sealed class UsuarioSesion06AV
    {
        #region Instancia Estatica
        private static UsuarioSesion06AV _Instancia;

        #endregion

        #region Constructor Privado
        private UsuarioSesion06AV()
        {

        }

        #endregion

        #region Instancia()

        public static UsuarioSesion06AV Instancia()
        {
            if (_Instancia == null)
            {
                _Instancia = new UsuarioSesion06AV();
            }
            return _Instancia;
        }

        #endregion

        #region Propiedades
        public Usuario06AV UsuarioActual { get; private set; }
        public Rol06AV Rol { get; private set; }
        public DateTime? FechaInicioSesion { get; private set; }

        #endregion

        #region Metodos
        public void IniciarSesion(Usuario06AV usuario, Rol06AV rol)
        {
            if (usuario == null)
                throw new ArgumentNullException(nameof(usuario));

            UsuarioActual = usuario;
            Rol = rol;
            FechaInicioSesion = DateTime.Now;
        }

        public void CerrarSesion()
        {
            UsuarioActual = null;
            FechaInicioSesion = null;
            Rol = null;
        }

        public Usuario06AV ObtenerUsuario()
        {
            return UsuarioActual;
        }

        public bool TieneRol(string rol)
        {
            return Rol.Descripcion == rol;
        }

        public string NombreCompleto()
        {

            return $"{UsuarioActual.Nombre} {UsuarioActual.Apellido}";
        }
        #endregion
    }
}