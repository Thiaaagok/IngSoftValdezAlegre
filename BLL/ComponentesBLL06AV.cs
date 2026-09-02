using BE;
using BLL.Excepciones;
using MPP;
using SER;
using System;
using System.Collections.Generic;

namespace BLL
{
    public class ComponentesBLL06AV
    {
        private readonly ComponentesMPP06AV _mpp = new ComponentesMPP06AV();

        public List<Componente06AV> ObtenerTodos()
        {
            try { return _mpp.ObtenerTodos(); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener los componentes.", ex); }
        }

        /// <summary>Componentes cuyo stock está en o por debajo del mínimo (faltantes a comprar).</summary>
        public List<Componente06AV> ObtenerBajoStock()
        {
            try { return _mpp.ObtenerBajoStock(); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo obtener el stock bajo.", ex); }
        }

        /// <summary>Suma stock a un componente (al recibir una factura de compra).</summary>
        public void SumarStock(string codigo, int cantidad)
        {
            ValidarCodigo(codigo);
            if (cantidad <= 0)
                throw new ValidacionException06AV("cantidad", "La cantidad a sumar debe ser mayor a cero.");
            try { _mpp.SumarStock(codigo, cantidad); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo sumar el stock.", ex); }
        }

        public Componente06AV ObtenerPorCodigo(string codigo)
        {
            ValidarCodigo(codigo);
            try { return _mpp.ObtenerPorCodigo(codigo); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo obtener el componente.", ex); }
        }

        public void Crear(Componente06AV c)
        {
            Validar(c);
            var existente = _mpp.ObtenerPorCodigo(c.Codigo);
            if (existente != null && existente.BajaLogica)
                throw new DuplicadoException06AV(
                    $"El código {c.Codigo} pertenece a un componente dado de baja. " +
                    "Restauralo desde la Bitácora de Cambios en lugar de crearlo de nuevo.");
            if (existente != null)
                throw new DuplicadoException06AV($"Ya existe un componente con el código {c.Codigo}.");
            try { _mpp.Agregar(c); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo crear el componente.", ex); }

            AuditoriaPcFactory06AV.Alta($"Componente: {c.Codigo}", ModuloBitacora.Componentes);
        }

        public void Modificar(Componente06AV c)
        {
            Validar(c);
            if (_mpp.ObtenerPorCodigo(c.Codigo) == null)
                throw new NoEncontradoException06AV($"No existe un componente con el código {c.Codigo}.");
            try { _mpp.Modificar(c); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo modificar el componente.", ex); }

            AuditoriaPcFactory06AV.Modificacion($"Componente: {c.Codigo}", ModuloBitacora.Componentes);
        }

        /// <summary>
        /// Baja LÓGICA del componente (Bit_Lo_Bo = 1). Los componentes no se borran:
        /// el trigger TR_Componentes_BloquearDelete impide el borrado físico y el
        /// trigger de UPDATE deja la baja asentada en la bitácora de cambios.
        /// </summary>
        public void Eliminar(string codigo)
        {
            ValidarCodigo(codigo);
            var actual = _mpp.ObtenerPorCodigo(codigo);
            if (actual == null)
                throw new NoEncontradoException06AV($"No existe un componente con el código {codigo}.");
            if (actual.BajaLogica)
                throw new ValidacionException06AV("Codigo", $"El componente {codigo} ya está dado de baja.");
            try { _mpp.BajaLogica(codigo); }
            catch (PcFactoryException06AV) { throw; }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo dar de baja el componente.", ex); }

            AuditoriaPcFactory06AV.Baja($"Componente: {codigo}", ModuloBitacora.Componentes);
        }

        /// <summary>Deshace la baja lógica de un componente.</summary>
        public void Reactivar(string codigo)
        {
            ValidarCodigo(codigo);
            var actual = _mpp.ObtenerPorCodigo(codigo);
            if (actual == null)
                throw new NoEncontradoException06AV($"No existe un componente con el código {codigo}.");
            if (!actual.BajaLogica)
                throw new ValidacionException06AV("Codigo", $"El componente {codigo} ya está vigente.");
            try { _mpp.Reactivar(codigo); }
            catch (PcFactoryException06AV) { throw; }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo reactivar el componente.", ex); }

            AuditoriaPcFactory06AV.Modificacion($"Componente reactivado: {codigo}", ModuloBitacora.Componentes);
        }

        #region Bitácora de cambios (Componentes_C)

        /// <summary>
        /// Histórico de versiones de los componentes. Los filtros vacíos no filtran.
        /// </summary>
        public List<ComponenteHistorico06AV> ObtenerBitacora(string codigo, string descripcion,
                                                             DateTime? fechaIni, DateTime? fechaFin)
        {
            if (fechaIni.HasValue && fechaFin.HasValue && fechaIni.Value.Date > fechaFin.Value.Date)
                throw new ValidacionException06AV("FechaIni",
                    "La fecha de inicio no puede ser posterior a la fecha de fin.");

            try { return _mpp.ObtenerBitacora(codigo, descripcion, fechaIni, fechaFin); }
            catch (PcFactoryException06AV) { throw; }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo obtener la bitácora de componentes.", ex); }
        }

        /// <summary>
        /// Restaura como vigente una versión histórica. La app no escribe el
        /// histórico: actualiza el componente y el trigger versiona el cambio.
        /// </summary>
        public void ActivarHistorico(ComponenteHistorico06AV version)
        {
            if (version == null)
                throw new ValidacionException06AV("version", "Seleccioná una versión del histórico.");
            if (version.IdHistorico <= 0)
                throw new ValidacionException06AV("IdHistorico", "La versión histórica seleccionada no es válida.");
            if (version.Activo)
                throw new ValidacionException06AV("Act", "Esa versión ya es la vigente del componente.");

            try { _mpp.ActivarHistorico(version.IdHistorico); }
            catch (PcFactoryException06AV) { throw; }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo restaurar la versión histórica.", ex); }

            AuditoriaPcFactory06AV.Modificacion(
                $"Componente: {version.CodigoComponente} (restaurado del histórico #{version.IdHistorico})",
                ModuloBitacora.Componentes);
        }

        #endregion

        private void Validar(Componente06AV c)
        {
            if (c == null)
                throw new ValidacionException06AV("componente", "El componente no puede ser nulo.");
            ValidarCodigo(c.Codigo);
            if (string.IsNullOrWhiteSpace(c.Descripcion))
                throw new ValidacionException06AV("Descripcion", "La descripción es obligatoria.");
            if (c.PrecioUnitario < 0)
                throw new ValidacionException06AV("PrecioUnitario", "El precio no puede ser negativo.");
            if (c.Stock < 0)
                throw new ValidacionException06AV("Stock", "El stock no puede ser negativo.");
            if (c.StockMinimo < 0)
                throw new ValidacionException06AV("StockMinimo", "El stock mínimo no puede ser negativo.");
        }

        private void ValidarCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                throw new ValidacionException06AV("Codigo", "El código es obligatorio.");
        }
    }
}
