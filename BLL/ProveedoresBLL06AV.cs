using BE;
using BLL.Excepciones;
using MPP;
using SER;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BLL
{
    public class ProveedoresBLL06AV
    {
        private readonly ProveedoresMPP06AV _mpp = new ProveedoresMPP06AV();

        public List<Proveedor06AV> ObtenerTodos()
        {
            try { return _mpp.ObtenerTodos(); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener los proveedores.", ex); }
        }

        public List<Proveedor06AV> ObtenerActivos() => ObtenerTodos().Where(p => p.Activo).ToList();

        public Proveedor06AV ObtenerPorId(int id)
        {
            try { return _mpp.ObtenerPorId(id); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo obtener el proveedor.", ex); }
        }

        public void Crear(Proveedor06AV p)
        {
            Validar(p, true);
            if (_mpp.ObtenerPorCuit(p.Cuit) != null)
                throw new DuplicadoException06AV($"Ya existe un proveedor con el CUIT {p.Cuit}.");
            try { _mpp.Agregar(p); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo crear el proveedor.", ex); }

            AuditoriaPcFactory06AV.Alta($"Proveedor CUIT: {p.Cuit}", ModuloBitacora.Proveedores);
        }

        public void Modificar(Proveedor06AV p)
        {
            Proveedor06AV actual = _mpp.ObtenerPorId(p?.Id ?? 0);
            if (actual == null)
                throw new NoEncontradoException06AV($"No existe el proveedor #{p?.Id}.");
            bool cuitCambio = Cuit06AV.SoloDigitos(actual.Cuit) != Cuit06AV.SoloDigitos(p.Cuit);
            Validar(p, cuitCambio);

            var mismoCuit = _mpp.ObtenerPorCuit(p.Cuit);
            if (mismoCuit != null && mismoCuit.Id != p.Id)
                throw new DuplicadoException06AV($"Otro proveedor ya usa el CUIT {p.Cuit}.");

            try { _mpp.Modificar(p); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo modificar el proveedor.", ex); }

            AuditoriaPcFactory06AV.Modificacion($"Proveedor #{p.Id}", ModuloBitacora.Proveedores);
        }

        public bool Eliminar(int id)
        {
            var actual = _mpp.ObtenerPorId(id);
            if (actual == null)
                throw new NoEncontradoException06AV($"No existe el proveedor #{id}.");
            bool eliminado;
            try { eliminado = _mpp.Eliminar(id); }
            catch (PcFactoryException06AV) { throw; }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo eliminar el proveedor.", ex); }

            if (eliminado) AuditoriaPcFactory06AV.Baja($"Proveedor #{id} eliminado", ModuloBitacora.Proveedores);
            else AuditoriaPcFactory06AV.Baja($"Proveedor #{id} dado de baja (tiene cotizaciones)", ModuloBitacora.Proveedores);
            return eliminado;
        }

        public List<FilaReporteProveedores06AV> ObtenerReporte()
        {
            if (!UsuarioSesion06AV.Instancia().TienePermiso(PatenteEnum06AV.VerReporteProveedores))
                throw new ValidacionException06AV("patente", "No tenés permiso para ver el reporte de proveedores.");
            try
            {
                return _mpp.ObtenerReporte(t => GestorIdioma06AV.Instancia.Obtener("cmp_tipo_" + (TipoComponente06AV)t));
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo obtener el reporte de proveedores.", ex); }
        }

        private static readonly Regex FormatoEmail =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.CultureInvariant);

        private void Validar(Proveedor06AV p, bool exigirDigitoVerificador)
        {
            if (p == null)
                throw new ValidacionException06AV("proveedor", "El proveedor no puede ser nulo.");
            p.Nombre = p.Nombre?.Trim();
            if (string.IsNullOrWhiteSpace(p.Nombre))
                throw new ValidacionException06AV("Nombre", "El nombre es obligatorio.");
            if (p.Nombre.Length > 150)
                throw new ValidacionException06AV("Nombre", "El nombre admite hasta 150 caracteres.");
            if (string.IsNullOrWhiteSpace(p.Cuit))
                throw new ValidacionException06AV("Cuit", "El CUIT es obligatorio.");
            if (!Cuit06AV.TieneFormato(p.Cuit))
                throw new ValidacionException06AV("Cuit", "El CUIT debe tener 11 dígitos (por ejemplo 30-71234567-1).");
            if (exigirDigitoVerificador && !Cuit06AV.EsValido(p.Cuit))
                throw new ValidacionException06AV("Cuit", "El CUIT no es válido: revisá el prefijo y el dígito verificador.");
            p.Cuit = Cuit06AV.Formatear(p.Cuit);
            p.Email = p.Email?.Trim();
            if (!string.IsNullOrEmpty(p.Email) && !FormatoEmail.IsMatch(p.Email))
                throw new ValidacionException06AV("Email", "El email no tiene un formato válido.");
            if (!string.IsNullOrEmpty(p.Email) && p.Email.Length > 150)
                throw new ValidacionException06AV("Email", "El email admite hasta 150 caracteres.");
        }
    }
}
