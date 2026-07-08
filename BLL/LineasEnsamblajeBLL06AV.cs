using BE;
using BLL.Excepciones;
using MPP;
using System;
using System.Collections.Generic;

namespace BLL
{
    /// <summary>Lógica de negocio del ABM de Líneas de Ensamblaje (PC Factory).</summary>
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
        }

        public void Modificar(LineaEnsamblaje06AV l)
        {
            Validar(l);
            if (_mpp.ObtenerPorId(l.Id) == null)
                throw new NoEncontradoException06AV($"No existe la línea #{l.Id}.");
            try { _mpp.Modificar(l); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo modificar la línea.", ex); }
        }

        public void Eliminar(int id)
        {
            if (_mpp.ObtenerPorId(id) == null)
                throw new NoEncontradoException06AV($"No existe la línea #{id}.");
            try { _mpp.Eliminar(id); }
            catch (PcFactoryException06AV) { throw; }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo eliminar la línea.", ex); }
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
