using MPP;
using SER.Excepciones;
using SER.Generador;
using SER;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL
{
    public class BitacoraBLL06AV
    {
        #region Registro General

        public bool Registrar(CategoriaBitacora categoria, CriticidadBitacora criticidad, string descripcion, ModuloBitacora modulo, string usuarioDni = null)
        {
            try
            {
                BitacoraMPP06AV MPP_Bitacora = new BitacoraMPP06AV();
                if (string.IsNullOrWhiteSpace(descripcion))
                    throw new BitacoraValidacionException("descripcion", "La descripción no puede estar vacía.");
                GeneradorID gid = new GeneradorID();
                string id = gid.GenerarId();

                string codigo = MPP_Bitacora.ObtenerSiguienteCodigo();

                Bitacora06AV bitacora = new Bitacora06AV
                {
                    Categoria = categoria.ToString(),
                    Codigo = codigo,
                    Criticidad = criticidad.ToString(),
                    Descripcion = descripcion,
                    Fecha = DateTime.Now,
                    Id = id,
                    Modulo = modulo.ToString(),
                    UsuarioDni = usuarioDni
                };

                return MPP_Bitacora.Guardar(bitacora);
            }
            catch (BitacoraException) { throw; }
            catch (Exception ex)
            {
                throw new BitacoraAccesoDatosException("Registrar", ex);
            }
        }

        #endregion

        #region Obtener Eventos

        public Bitacora06AV ObtenerEventoPorId(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    throw new BitacoraValidacionException("id", "El Id no puede ser un Guid vacío.");
                }

                BitacoraMPP06AV MPP_Bitacora = new BitacoraMPP06AV();
                var resultado = MPP_Bitacora.ObtenerPorId(id);

                if (resultado == null)
                {
                    throw new BitacoraEventoNoEncontradoException(id.ToString());
                }

                return resultado;
            }
            catch (BitacoraException) 
            { 
                throw;
            }
            catch (Exception ex)
            {
                throw new BitacoraAccesoDatosException($"ObtenerEventoPorId({id})", ex);
            }
        }

        public List<Bitacora06AV> ObtenerEventosPorCategoria(CategoriaBitacora categoria)
        {
            try
            {
                BitacoraMPP06AV MPP_Bitacora = new BitacoraMPP06AV();
                return MPP_Bitacora.ObtenerPorCategoria(categoria.ToString());
            }
            catch (BitacoraException) 
            { 
                throw; 
            }
            catch (Exception ex)
            {
                throw new BitacoraAccesoDatosException($"ObtenerEventosPorCategoria({categoria})", ex);
            }
        }

        public List<Bitacora06AV> ObtenerEventosPorCriticidad(CriticidadBitacora criticidad)
        {
            try
            {
                BitacoraMPP06AV MPP_Bitacora = new BitacoraMPP06AV();
                return MPP_Bitacora.ObtenerPorCriticidad(criticidad.ToString());
            }
            catch (BitacoraException) 
            { 
                throw; 
            }
            catch (Exception ex)
            {
                throw new BitacoraAccesoDatosException($"ObtenerEventosPorCriticidad({criticidad})", ex);
            }
        }

        public List<Bitacora06AV> ObtenerEventosPorModulo(ModuloBitacora modulo)
        {
            try
            {
                BitacoraMPP06AV MPP_Bitacora = new BitacoraMPP06AV();
                return MPP_Bitacora.ObtenerPorModulo(modulo.ToString());
            }
            catch (BitacoraException) 
            { 
                throw; 
            }
            catch (Exception ex)
            {
                throw new BitacoraAccesoDatosException($"ObtenerEventosPorModulo({modulo})", ex);
            }
        }

        public List<Bitacora06AV> ObtenerEventosPorUsuario(string usuarioDni)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(usuarioDni))
                {
                    throw new BitacoraValidacionException("DNI", "El DNI del usuario no puede estar vacío.");
                }

                BitacoraMPP06AV MPP_Bitacora = new BitacoraMPP06AV();
                return MPP_Bitacora.ObtenerPorUsuario(usuarioDni);
            }
            catch (BitacoraException) 
            { 
                throw; 
            }
            catch (Exception ex)
            {
                throw new BitacoraAccesoDatosException($"ObtenerEventosPorUsuario({usuarioDni})", ex);
            }
        }

        public List<Bitacora06AV> ObtenerEventosEntreFechas(DateTime desde, DateTime hasta)
        {
            try
            {
                if (desde > hasta) throw new BitacoraRangoFechasInvalidoException(desde, hasta);
                BitacoraMPP06AV MPP_Bitacora = new BitacoraMPP06AV();
                return MPP_Bitacora.ObtenerEntreFechas(desde, hasta);
            }
            catch (BitacoraException) 
            { 
                throw; 
            }
            catch (Exception ex)
            {
                throw new BitacoraAccesoDatosException($"ObtenerEventosEntreFechas({desde:dd/MM/yyyy}, {hasta:dd/MM/yyyy})", ex);
            }
        }

        #endregion

        #region Login

        public bool LoginExitoso(string usuarioDni) =>
            Registrar(CategoriaBitacora.Login, CriticidadBitacora.Baja, "Inicio de sesión exitoso", ModuloBitacora.Autenticacion, usuarioDni);

        public bool LoginFallido(string usuarioDni) =>
            Registrar(CategoriaBitacora.LoginFallido, CriticidadBitacora.Media, "Intento de inicio de sesión fallido", ModuloBitacora.Autenticacion, usuarioDni);

        public bool Logout(string usuarioDni) =>
            Registrar(CategoriaBitacora.Logout, CriticidadBitacora.Baja, "Cierre de sesión", ModuloBitacora.Autenticacion, usuarioDni);

        #endregion

        #region Errores

        public bool Error(string descripcion, ModuloBitacora modulo, string usuarioDni = null) =>
            Registrar(CategoriaBitacora.Error, CriticidadBitacora.Alta, descripcion, modulo, usuarioDni);

        public bool ErrorCritico(string descripcion, ModuloBitacora modulo, string usuarioDni = null) =>
            Registrar(CategoriaBitacora.ErrorCritico, CriticidadBitacora.Critica, descripcion, modulo, usuarioDni);

        #endregion

        #region Acciones

        public bool Alta(string entidad, ModuloBitacora modulo, string usuarioDni) =>
            Registrar(CategoriaBitacora.Alta, CriticidadBitacora.Baja, $"Alta de {entidad}", modulo, usuarioDni);

        public bool Modificacion(string entidad, ModuloBitacora modulo, string usuarioDni) =>
            Registrar(CategoriaBitacora.Modificacion, CriticidadBitacora.Media, $"Modificación de {entidad}", modulo, usuarioDni);

        public bool Baja(string entidad, ModuloBitacora modulo, string usuarioDni) =>
            Registrar(CategoriaBitacora.Baja, CriticidadBitacora.Alta, $"Baja de {entidad}", modulo, usuarioDni);

        #endregion
    }
}
