using BE;
using BLL.Excepciones;
using MPP;
using System;
using System.Collections.Generic;

namespace BLL
{
    /// <summary>Lógica de negocio del ABM de Insumos (PC Factory), incluye bajo stock (RFN2).</summary>
    public class InsumosBLL06AV
    {
        private readonly InsumosMPP06AV _mpp = new InsumosMPP06AV();

        public List<Insumo06AV> ObtenerTodos()
        {
            try { return _mpp.ObtenerTodos(); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener los insumos.", ex); }
        }

        /// <summary>Insumos con stock por debajo (o en) el mínimo, para alertar al repositor.</summary>
        public List<Insumo06AV> ObtenerBajoStock()
        {
            try { return _mpp.ObtenerBajoStock(); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo obtener el stock bajo.", ex); }
        }

        public Insumo06AV ObtenerPorCodigo(string codigo)
        {
            ValidarCodigo(codigo);
            try { return _mpp.ObtenerPorCodigo(codigo); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo obtener el insumo.", ex); }
        }

        public void Crear(Insumo06AV i)
        {
            Validar(i);
            if (_mpp.ObtenerPorCodigo(i.Codigo) != null)
                throw new DuplicadoException06AV($"Ya existe un insumo con el código {i.Codigo}.");
            try { _mpp.Agregar(i); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo crear el insumo.", ex); }
        }

        public void Modificar(Insumo06AV i)
        {
            Validar(i);
            if (_mpp.ObtenerPorCodigo(i.Codigo) == null)
                throw new NoEncontradoException06AV($"No existe un insumo con el código {i.Codigo}.");
            try { _mpp.Modificar(i); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo modificar el insumo.", ex); }
        }

        public void Eliminar(string codigo)
        {
            ValidarCodigo(codigo);
            if (_mpp.ObtenerPorCodigo(codigo) == null)
                throw new NoEncontradoException06AV($"No existe un insumo con el código {codigo}.");
            try { _mpp.Eliminar(codigo); }
            catch (PcFactoryException06AV) { throw; }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo eliminar el insumo.", ex); }
        }

        private void Validar(Insumo06AV i)
        {
            if (i == null)
                throw new ValidacionException06AV("insumo", "El insumo no puede ser nulo.");
            ValidarCodigo(i.Codigo);
            if (string.IsNullOrWhiteSpace(i.Descripcion))
                throw new ValidacionException06AV("Descripcion", "La descripción es obligatoria.");
            if (i.Stock < 0)
                throw new ValidacionException06AV("Stock", "El stock no puede ser negativo.");
            if (i.StockMinimo < 0)
                throw new ValidacionException06AV("StockMinimo", "El stock mínimo no puede ser negativo.");
        }

        private void ValidarCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                throw new ValidacionException06AV("Codigo", "El código es obligatorio.");
        }
    }
}
