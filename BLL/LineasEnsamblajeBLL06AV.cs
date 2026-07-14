using BE;
using BLL.Excepciones;
using MPP;
using SER;
using System;
using System.Collections.Generic;

namespace BLL
{
    public class LineasEnsamblajeBLL06AV
    {
        private readonly LineasEnsamblajeMPP06AV _mpp = new LineasEnsamblajeMPP06AV();

        public List<LineaEnsamblaje06AV> ObtenerTodas()
        {
            try { return _mpp.ObtenerTodas(); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener las líneas.", ex); }
        }

        public LineaEnsamblaje06AV ObtenerPorId(int id)
        {
            try { return _mpp.ObtenerPorId(id); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo obtener la línea.", ex); }
        }

        public void Crear(LineaEnsamblaje06AV l)
        {
            Validar(l);
            try { _mpp.Agregar(l); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo crear la línea.", ex); }

            AuditoriaPcFactory06AV.Alta($"Línea: {l.Nombre}", ModuloBitacora.LineasEnsamblaje);
        }

        public void Modificar(LineaEnsamblaje06AV l)
        {
            Validar(l);
            if (_mpp.ObtenerPorId(l.Id) == null)
                throw new NoEncontradoException06AV($"No existe la línea #{l.Id}.");
            try { _mpp.Modificar(l); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo modificar la línea.", ex); }

            AuditoriaPcFactory06AV.Modificacion($"Línea #{l.Id}", ModuloBitacora.LineasEnsamblaje);
        }

        public void Eliminar(int id)
        {
            if (_mpp.ObtenerPorId(id) == null)
                throw new NoEncontradoException06AV($"No existe la línea #{id}.");
            try { _mpp.Eliminar(id); }
            catch (PcFactoryException06AV) { throw; }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo eliminar la línea.", ex); }

            AuditoriaPcFactory06AV.Baja($"Línea #{id}", ModuloBitacora.LineasEnsamblaje);
        }

        private void Validar(LineaEnsamblaje06AV l)
        {
            if (l == null)
                throw new ValidacionException06AV("linea", "La línea no puede ser nula.");
            if (string.IsNullOrWhiteSpace(l.Nombre))
                throw new ValidacionException06AV("Nombre", "El nombre de la línea es obligatorio.");
        }
    }
}
