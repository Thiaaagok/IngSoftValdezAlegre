using BE;
using BLL.Excepciones;
using MPP;
using SER;
using System;
using System.Collections.Generic;

namespace BLL
{
    public class ModelosEstandarBLL06AV
    {
        private readonly ModelosEstandarMPP06AV _mpp = new ModelosEstandarMPP06AV();

        public List<ModeloEstandar06AV> ObtenerTodos()
        {
            try { return _mpp.ObtenerTodos(); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener los modelos estándar.", ex); }
        }

        public void Crear(ModeloEstandar06AV modelo)
        {
            Validar(modelo);
            try { _mpp.Agregar(modelo); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo crear el modelo estándar.", ex); }

            AuditoriaPcFactory06AV.Alta($"Modelo estándar: {modelo.Nombre}", ModuloBitacora.Produccion);
        }

        public void Modificar(ModeloEstandar06AV modelo)
        {
            Validar(modelo);
            if (modelo.Id <= 0)
                throw new NoEncontradoException06AV("El modelo estándar no existe.");
            try { _mpp.Modificar(modelo); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo modificar el modelo estándar.", ex); }

            AuditoriaPcFactory06AV.Modificacion($"Modelo estándar #{modelo.Id}", ModuloBitacora.Produccion);
        }

        public void Eliminar(int id)
        {
            if (id <= 0)
                throw new NoEncontradoException06AV("El modelo estándar no existe.");
            try { _mpp.Eliminar(id); }
            catch (PcFactoryException06AV) { throw; }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo eliminar el modelo estándar.", ex); }

            AuditoriaPcFactory06AV.Baja($"Modelo estándar #{id}", ModuloBitacora.Produccion);
        }

        private void Validar(ModeloEstandar06AV modelo)
        {
            if (modelo == null)
                throw new ValidacionException06AV("modelo", "El modelo no puede ser nulo.");
            if (string.IsNullOrWhiteSpace(modelo.Nombre))
                throw new ValidacionException06AV("Nombre", "El nombre del modelo es obligatorio.");
            if (modelo.Componentes == null || modelo.Componentes.Count == 0)
                throw new ValidacionException06AV("Componentes", "El modelo debe tener al menos un componente.");
        }
    }
}
