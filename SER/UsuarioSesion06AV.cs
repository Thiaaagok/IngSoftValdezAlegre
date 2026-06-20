using System;
using System.Collections.Generic;
using System.Linq;

namespace SER
{
    public sealed class UsuarioSesion06AV
    {
        #region Singleton

        private static UsuarioSesion06AV _Instancia;

        private UsuarioSesion06AV() { }

        public static UsuarioSesion06AV Instancia()
        {
            if (_Instancia == null)
                _Instancia = new UsuarioSesion06AV();
            return _Instancia;
        }

        #endregion

        #region Propiedades

        public Usuario06AV UsuarioActual { get; private set; }
        public Rol06AV Rol { get; private set; }
        public List<Patente06AV> Patentes { get; private set; }
        public DateTime? FechaInicioSesion { get; private set; }

        #endregion

        #region Gestión de sesión

        /// <summary>
        /// Inicia la sesión guardando usuario, rol y timestamp.
        /// Las patentes se cargan luego con <see cref="CargarPatentes"/>.
        /// </summary>
        public void IniciarSesion(Usuario06AV usuario, Rol06AV rol)
        {
            if (usuario == null) throw new ArgumentNullException(nameof(usuario));
            if (rol == null)     throw new ArgumentNullException(nameof(rol));

            UsuarioActual     = usuario;
            Rol               = rol;
            FechaInicioSesion = DateTime.Now;
            Patentes          = new List<Patente06AV>();
        }

        /// <summary>
        /// Carga en sesión la lista de patentes obtenidas del SP recursivo.
        /// Llamar inmediatamente después de <see cref="IniciarSesion"/>.
        /// </summary>
        public void CargarPatentes(List<Patente06AV> patentes)
        {
            Patentes = patentes ?? new List<Patente06AV>();
        }

        public void CerrarSesion()
        {
            UsuarioActual     = null;
            Rol               = null;
            Patentes          = null;
            FechaInicioSesion = null;
        }

        #endregion

        #region Consultas de sesión

        public Usuario06AV ObtenerUsuario() => UsuarioActual;

        public string NombreCompleto() =>
            UsuarioActual != null
                ? $"{UsuarioActual.Nombre} {UsuarioActual.Apellido}"
                : string.Empty;

        /// <summary>
        /// Verifica si el usuario tiene la patente indicada por el enum.
        /// El nombre del valor del enum debe coincidir con el Id en la BD.
        /// </summary>
        public bool TienePermiso(PatenteEnum06AV patente)
        {
            if (Patentes == null || Patentes.Count == 0) return false;
            string id = patente.ToString();
            return Patentes.Any(p => string.Equals(p.Id, id, StringComparison.Ordinal));
        }

        /// <summary>
        /// Verifica si el rol activo coincide con la descripción indicada.
        /// </summary>
        public bool TieneRol(string descripcion)
        {
            return Rol != null &&
                   string.Equals(Rol.Descripcion, descripcion, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Forma legacy: comprueba por Id de patente como string.
        /// Prefer TienePermiso(PatenteEnum) cuando sea posible.
        /// </summary>
        public bool TienePatente(string patenteId)
        {
            if (Patentes == null) return false;
            return Patentes.Any(p => string.Equals(p.Id, patenteId, StringComparison.Ordinal));
        }

        #endregion
    }
}
