using BE;
using BLL.Excepciones;
using MPP;
using SER;
using System;
using System.Collections.Generic;

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

        public Proveedor06AV ObtenerPorId(int id)
        {
            try { return _mpp.ObtenerPorId(id); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo obtener el proveedor.", ex); }
        }

        public void Crear(Proveedor06AV p)
        {
            Validar(p);
            if (_mpp.ObtenerPorCuit(p.Cuit) != null)
                throw new DuplicadoException06AV($"Ya existe un proveedor con el CUIT {p.Cuit}.");
            try { _mpp.Agregar(p); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo crear el proveedor.", ex); }

            AuditoriaPcFactory06AV.Alta($"Proveedor CUIT: {p.Cuit}", ModuloBitacora.Proveedores);
        }

        public void Modificar(Proveedor06AV p)
        {
            Validar(p);
            if (_mpp.ObtenerPorId(p.Id) == null)
                throw new NoEncontradoException06AV($"No existe el proveedor #{p.Id}.");

            var mismoCuit = _mpp.ObtenerPorCuit(p.Cuit);
            if (mismoCuit != null && mismoCuit.Id != p.Id)
                throw new DuplicadoException06AV($"Otro proveedor ya usa el CUIT {p.Cuit}.");

            try { _mpp.Modificar(p); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo modificar el proveedor.", ex); }

            AuditoriaPcFactory06AV.Modificacion($"Proveedor #{p.Id}", ModuloBitacora.Proveedores);
        }

        public void Eliminar(int id)
        {
            if (_mpp.ObtenerPorId(id) == null)
                throw new NoEncontradoException06AV($"No existe el proveedor #{id}.");
            try { _mpp.Eliminar(id); }
            catch (PcFactoryException06AV) { throw; }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo eliminar el proveedor.", ex); }

            AuditoriaPcFactory06AV.Baja($"Proveedor #{id}", ModuloBitacora.Proveedores);
        }

        private void Validar(Proveedor06AV p)
        {
            if (p == null)
                throw new ValidacionException06AV("proveedor", "El proveedor no puede ser nulo.");
            if (string.IsNullOrWhiteSpace(p.Nombre))
                throw new ValidacionException06AV("Nombre", "El nombre es obligatorio.");
            if (string.IsNullOrWhiteSpace(p.Cuit))
                throw new ValidacionException06AV("Cuit", "El CUIT es obligatorio.");
        }
    }
}
