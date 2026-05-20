using BE;
using MPP;
using SER.Encriptador;
using SER.Excepciones;
using SER.Generador;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
namespace SER
{
    public class UsuariosSER06AV
    {
        #region Login

        public Usuario06AV Login(string login, string contrasenia)
        {
            if (string.IsNullOrWhiteSpace(login))
                throw new UsuarioValidacionException("login", "El login no puede estar vacío.");

            if (string.IsNullOrWhiteSpace(contrasenia))
                throw new UsuarioValidacionException("contrasenia", "La contraseña no puede estar vacía.");

            BitacoraSER06AV bitacora = new BitacoraSER06AV();
            EncriptacionSER06AV enc = new EncriptacionSER06AV();
            UsuariosMPP06AV UsuariosMPP = new UsuariosMPP06AV();
            GeneradorID gid = new GeneradorID();

            const int MAX_INTENTOS = 3;
            const int VENTANA_HORAS = 3;

            try
            {
                var usuario = UsuariosMPP.ObtenerPorLogin(login);

                if (usuario == null)
                    throw new UsuarioNoEncontradoException(login);

                if (usuario.Bloqueado)
                    throw new UsuarioEstadoInvalidoException(usuario.Dni, "Bloqueado", "Login");

                if (!usuario.Activo)
                    throw new UsuarioEstadoInvalidoException(usuario.Dni, "Inactivo", "Login");

                string IdIntento = gid.GenerarId();

                string contraseniaHasheada = enc.Encriptar(contrasenia);
                if (contraseniaHasheada != usuario.Contrasenia)
                {
                    UsuariosMPP.RegistrarIntento(IdIntento,usuario.Dni, exitoso: false);
                    bitacora.LoginFallido(usuario.Dni);
                    int fallidos = UsuariosMPP.ContarFallidosRecientes(usuario.Dni, VENTANA_HORAS);

                    if (fallidos >= MAX_INTENTOS)
                    {
                        UsuariosMPP.BloquearUsuario(usuario.Dni);
                        throw new UsuarioEstadoInvalidoException(usuario.Dni, "Bloqueado", "Login");
                    }

                    throw new ContraseniaInvalidaException(usuario.Dni);
                }

                UsuariosMPP.RegistrarIntento(IdIntento,usuario.Dni, exitoso: true);

                RolesMPP06AV RolesMPP = new RolesMPP06AV();
                Rol06AV rol = RolesMPP.ObtenerPorId(usuario.IdRol);

                usuario.Email = enc.DesencriptarReversible(usuario.Email);
                UsuarioSesion06AV.Instancia().IniciarSesion(usuario, rol);
                bitacora.LoginExitoso(usuario.Dni);

                return usuario;
            }
            catch (UsuarioException) { throw; }
            catch (Exception ex)
            {
                bitacora.Error($"Error en Login: {ex.Message}", ModuloBitacora.Autenticacion, login);
                throw new UsuarioAccesoDatosException($"Login({login})", ex);
            }
        }
        public void Logout()
        {
            BitacoraSER06AV bitacora = new BitacoraSER06AV();

            try
            {
                var usuario = UsuarioSesion06AV.Instancia().UsuarioActual;

                if (usuario == null)
                    throw new UsuarioValidacionException("sesion", "No hay una sesión activa.");

                string dni = usuario.Dni;
                UsuarioSesion06AV.Instancia().CerrarSesion();
                bitacora.Logout(dni);
            }
            catch (UsuarioException)
            {
                throw;
            }
            catch (Exception ex)
            {
                bitacora.Error($"Error en Logout: {ex.Message}", ModuloBitacora.Autenticacion);
                throw new UsuarioAccesoDatosException("Logout", ex);
            }
        }

        #endregion

        #region Obtener
        public List<Usuario06AV> ObtenerTodos()
        {
            try
            {
                UsuariosMPP06AV MPP = new UsuariosMPP06AV();
                EncriptacionSER06AV enc = new EncriptacionSER06AV();

                var usuarios = MPP.ObtenerTodos();

                foreach (var u in usuarios) u.Email = enc.DesencriptarReversible(u.Email);

                return usuarios;
            }
            catch (UsuarioException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new UsuarioAccesoDatosException("ObtenerTodos", ex);
            }
        }

        public Usuario06AV ObtenerPorDni(string dni)
        {
            ValidarDni(dni);

            BitacoraSER06AV bitacora = new BitacoraSER06AV();

            try
            {
                UsuariosMPP06AV MPP = new UsuariosMPP06AV();
                EncriptacionSER06AV enc = new EncriptacionSER06AV();

                var usuario = MPP.ObtenerPorDni(dni);

                if (usuario == null)
                    throw new UsuarioNoEncontradoException(dni);
                usuario.Email = enc.DesencriptarReversible(usuario.Email);

                return usuario;
            }
            catch (UsuarioException)
            {
                throw;
            }
            catch (Exception ex)
            {
                bitacora.Error($"Error obteniendo un usuario: {ex.Message}", ModuloBitacora.Usuarios, dni);
                throw new UsuarioAccesoDatosException($"ObtenerPorDni({dni})", ex);
            }
        }

        #endregion

        #region Alta
        public bool CrearUsuario(string dni, string nombre, string apellido, string email, string rol, string dniOperador)
        {
            ValidarDni(dni);
            ValidarNombre(nombre, "Nombre");
            ValidarNombre(apellido, "Apellido");
            ValidarEmail(email);
            BitacoraSER06AV bitacora = new BitacoraSER06AV();
            try
            {
                UsuariosMPP06AV MPP = new UsuariosMPP06AV();
                EncriptacionSER06AV enc = new EncriptacionSER06AV();

                var existente = MPP.ObtenerPorDni(dni);
                if (existente != null)
                    throw new UsuarioDuplicadoException(dni);

                var usuario = new Usuario06AV
                {
                    Dni = dni,
                    Nombre = nombre,
                    Apellido = apellido,
                    Email = enc.EncriptarReversible(email),
                    IdRol = rol,
                    Activo = true,
                    Bloqueado = false,
                    Login = SetearLogin(dni, nombre),
                    Contrasenia = SetearContrasenia(dni, apellido)
                };

                bool resultado = MPP.CrearUsuario(usuario);
                bitacora.Alta($"Usuario: {dni}", ModuloBitacora.Usuarios, dniOperador);
                return resultado;
            }
            catch (UsuarioDuplicadoException ex)
            {
                bitacora.Error(ex.Message, ModuloBitacora.Usuarios, dniOperador);
                throw;
            }
            catch (UsuarioException)
            {
                throw;
            }
            catch (Exception ex)
            {
                bitacora.Error($"Error creando un usuario: {ex.Message}", ModuloBitacora.Usuarios, dniOperador);
                throw new UsuarioAccesoDatosException($"CrearUsuario({dni})", ex);
            }
        }

        #endregion

        #region Modificaciones y baja
        public bool EditarUsuario(Usuario06AV usuario, string dniOperador)
        {
            if (usuario == null)
                throw new UsuarioValidacionException("usuario", "El objeto usuario no puede ser nulo.");

            ValidarDni(usuario.Dni);
            ValidarEmail(usuario.Email);

            BitacoraSER06AV bitacora = new BitacoraSER06AV();
            try
            {
                UsuariosMPP06AV MPP = new UsuariosMPP06AV();
                EncriptacionSER06AV enc = new EncriptacionSER06AV();

                var existente = MPP.ObtenerPorDni(usuario.Dni);
                if (existente == null)
                    throw new UsuarioNoEncontradoException(usuario.Dni);
                usuario.Email = enc.EncriptarReversible(usuario.Email);

                bool resultado = MPP.EditarUsuario(usuario);
                bitacora.Modificacion($"Usuario: {usuario.Dni}", ModuloBitacora.Usuarios, dniOperador);
                return resultado;
            }
            catch (UsuarioException)
            {
                throw;
            }
            catch (Exception ex)
            {
                bitacora.Error($"Error editando usuario: {usuario.Dni}", ModuloBitacora.Usuarios, dniOperador);
                throw new UsuarioAccesoDatosException($"EditarUsuario({usuario.Dni})", ex);
            }
        }

        public bool EliminarUsuario(string dni, string dniOperador)
        {
            ValidarDni(dni);
            BitacoraSER06AV bitacora = new BitacoraSER06AV();
            try
            {
                UsuariosMPP06AV MPP = new UsuariosMPP06AV();

                var usuario = MPP.ObtenerPorDni(dni);
                if (usuario == null)
                    throw new UsuarioNoEncontradoException(dni);

                if (!usuario.Activo)
                    throw new UsuarioEstadoInvalidoException(dni, "Inactivo", "EliminarUsuario");

                bool resultado = MPP.EliminarUsuario(dni);
                bitacora.Baja($"Usuario: {dni}", ModuloBitacora.Usuarios, dniOperador);
                return resultado;
            }
            catch (UsuarioException)
            {
                throw;
            }
            catch (Exception ex)
            {
                bitacora.Error($"Error eliminando usuario: {dni}", ModuloBitacora.Usuarios, dniOperador);
                throw new UsuarioAccesoDatosException($"EliminarUsuario({dni})", ex);
            }
        }

        public bool ReactivarUsuario(string dni, string dniOperador)
        {
            ValidarDni(dni);
            BitacoraSER06AV bitacora = new BitacoraSER06AV();
            try
            {
                UsuariosMPP06AV MPP = new UsuariosMPP06AV();

                var usuario = MPP.ObtenerPorDni(dni);
                if (usuario == null)
                    throw new UsuarioNoEncontradoException(dni);

                if (usuario.Activo)
                    throw new UsuarioEstadoInvalidoException(dni, "Activo", "ReactivarUsuario");

                bool resultado = MPP.ReactivarUsuario(dni);
                bitacora.Modificacion($"Usuario: {dni} reactivado", ModuloBitacora.Usuarios, dniOperador);
                return resultado;
            }
            catch (UsuarioException)
            {
                throw;
            }
            catch (Exception ex)
            {
                bitacora.Error($"Error reactivando usuario: {dni}", ModuloBitacora.Usuarios, dniOperador);
                throw new UsuarioAccesoDatosException($"ReactivarUsuario({dni})", ex);
            }
        }

        public bool DesactivarUsuario(string dni, string dniOperador)
        {
            ValidarDni(dni);
            BitacoraSER06AV bitacora = new BitacoraSER06AV();
            try
            {
                UsuariosMPP06AV MPP = new UsuariosMPP06AV();

                var usuario = MPP.ObtenerPorDni(dni);
                if (usuario == null)
                    throw new UsuarioNoEncontradoException(dni);

                if (!usuario.Activo)
                    throw new UsuarioEstadoInvalidoException(dni, "Inactivo", "DesactivarUsuario");

                bool resultado = MPP.DesactivarUsuario(dni);
                bitacora.Baja($"Usuario: {dni} desactivado", ModuloBitacora.Usuarios, dniOperador);
                return resultado;
            }
            catch (UsuarioException)
            {
                throw;
            }
            catch (Exception ex)
            {
                bitacora.Error($"Error desactivando usuario: {dni}", ModuloBitacora.Usuarios, dniOperador);
                throw new UsuarioAccesoDatosException($"DesactivarUsuario({dni})", ex);
            }
        }

        public bool BloquearUsuario(string dni, string dniOperador)
        {
            ValidarDni(dni);
            BitacoraSER06AV bitacora = new BitacoraSER06AV();
            try
            {
                UsuariosMPP06AV MPP = new UsuariosMPP06AV();

                var usuario = MPP.ObtenerPorDni(dni);
                if (usuario == null)
                    throw new UsuarioNoEncontradoException(dni);

                if (usuario.Bloqueado)
                    throw new UsuarioEstadoInvalidoException(dni, "Bloqueado", "BloquearUsuario");

                if (!usuario.Activo)
                    throw new UsuarioEstadoInvalidoException(dni, "Inactivo", "BloquearUsuario");

                bool resultado = MPP.BloquearUsuario(dni);
                bitacora.Modificacion($"Usuario: {dni} bloqueado", ModuloBitacora.Usuarios, dniOperador);
                return resultado;
            }
            catch (UsuarioException)
            {
                throw;
            }
            catch (Exception ex)
            {
                bitacora.Error($"Error bloqueando usuario: {dni}", ModuloBitacora.Usuarios, dniOperador);
                throw new UsuarioAccesoDatosException($"BloquearUsuario({dni})", ex);
            }
        }

        public bool DesbloquearUsuario(string dni, string dniOperador)
        {
            ValidarDni(dni);
            BitacoraSER06AV bitacora = new BitacoraSER06AV();
            try
            {
                UsuariosMPP06AV MPP = new UsuariosMPP06AV();

                var usuario = MPP.ObtenerPorDni(dni);
                if (usuario == null)
                    throw new UsuarioNoEncontradoException(dni);

                if (!usuario.Bloqueado)
                    throw new UsuarioEstadoInvalidoException(dni, "Desbloqueado", "DesbloquearUsuario");

                bool resultado = MPP.DesbloquearUsuario(dni);
                MPP.LimpiarIntentosFallidos(dni);
                bitacora.Modificacion(
                    $"Usuario: {dni} desbloqueado. Se le requerirá cambiar contraseña.",
                    ModuloBitacora.Usuarios,
                    dniOperador);
                return resultado;
            }
            catch (UsuarioException) { throw; }
            catch (Exception ex)
            {
                bitacora.Error($"Error desbloqueando usuario: {dni}", ModuloBitacora.Usuarios, dniOperador);
                throw new UsuarioAccesoDatosException($"DesbloquearUsuario({dni})", ex);
            }
        }

        public bool CambiarContraseña(string dni, string contraseniaActual, string contraseniaNueva)
        {
            ValidarDni(dni);

            if (string.IsNullOrWhiteSpace(contraseniaActual))
                throw new UsuarioValidacionException("contrasenia actual", "La contraseña actual no puede estar vacía.");

            if (string.IsNullOrWhiteSpace(contraseniaNueva))
                throw new UsuarioValidacionException("contrasenia nueva", "La contraseña nueva no puede estar vacía.");

            if (contraseniaActual == contraseniaNueva)
                throw new UsuarioValidacionException("contrasenia nueva", "La nueva contraseña debe ser distinta a la actual.");

            BitacoraSER06AV bitacora = new BitacoraSER06AV();
            try
            {
                UsuariosMPP06AV MPP = new UsuariosMPP06AV();

                var usuario = MPP.ObtenerPorDni(dni);

                // Hasheamos la contraseña actual ingresada y la comparamos contra la que está almacenada(también hasheada).
                EncriptacionSER06AV enc = new EncriptacionSER06AV();
                string contraseniaActualHash = enc.Encriptar(contraseniaActual);
                string contraseniaNuevaHash = enc.Encriptar(contraseniaNueva);

                if (usuario.Contrasenia != contraseniaActualHash)
                    throw new ContraseniaInvalidaException(dni);


                bool resultado = MPP.CambiarContraseña(dni, contraseniaActualHash, contraseniaNuevaHash);
                MPP.LimpiarIntentosFallidos(dni);
                bitacora.Modificacion($"Usuario: {dni} cambio contraseña", ModuloBitacora.Usuarios, dni);
                return resultado;
            }
            catch (UsuarioException)
            {
                throw;
            }
            catch (Exception ex)
            {
                bitacora.Error($"Error cambiando contraseña: {dni}", ModuloBitacora.Usuarios, dni);
                throw new UsuarioAccesoDatosException($"CambiarContraseña({dni})", ex);
            }
        }

        #endregion

        #region Validaciones

        private static void ValidarDni(string dni)
        {
            if (string.IsNullOrWhiteSpace(dni))
                throw new UsuarioValidacionException("dni", "El DNI no puede estar vacío.");

            if (!dni.All(char.IsDigit))
                throw new UsuarioValidacionException("dni", $"El DNI '{dni}' solo debe contener dígitos.");

            if (dni.Length < 7 || dni.Length > 8)
                throw new UsuarioValidacionException("dni", $"El DNI '{dni}' debe tener entre 7 y 8 dígitos.");
        }

        private static void ValidarNombre(string valor, string campo)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new UsuarioValidacionException(campo, $"El campo '{campo}' no puede estar vacío.");
        }

        private static void ValidarEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new UsuarioValidacionException("email", "El email no puede estar vacío.");

            if (!email.Contains('@') || !email.Contains('.'))
                throw new UsuarioValidacionException("email", $"El email '{email}' no tiene un formato válido.");
        }

        #endregion

        #region Seteadores
        public string SetearLogin(string dni, string nombre)
        {
            return $"{dni}{nombre}";
        }

        public string SetearContrasenia(string dni, string apellido)
        {
            EncriptacionSER06AV Encriptacion = new EncriptacionSER06AV();
            string contraseniaNoHasheada = $"{dni}{apellido}";
            return Encriptacion.Encriptar(contraseniaNoHasheada);
        }

        #endregion
    }
}
