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
            if (_mpp.ObtenerPorCodigo(c.Codigo) != null)
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

        public void Eliminar(string codigo)
        {
            ValidarCodigo(codigo);
            if (_mpp.ObtenerPorCodigo(codigo) == null)
                throw new NoEncontradoException06AV($"No existe un componente con el código {codigo}.");
            try { _mpp.Eliminar(codigo); }
            catch (PcFactoryException06AV) { throw; }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo eliminar el componente.", ex); }

            AuditoriaPcFactory06AV.Baja($"Componente: {codigo}", ModuloBitacora.Componentes);
        }

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
