using BE;
using System;
using DAL;
using System.Collections.Generic;
using System.Data;

namespace MPP
{
    /// <summary>Mapea entre ModeloEstandar06AV y la capa de datos, resolviendo sus componentes.</summary>
    public class ModelosEstandarMPP06AV
    {
        private readonly ModelosEstandarDAL06AV _dal = new ModelosEstandarDAL06AV();
        private readonly ComponentesMPP06AV _componentes = new ComponentesMPP06AV();

        public List<ModeloEstandar06AV> ObtenerTodos()
        {
            var lista = new List<ModeloEstandar06AV>();
            foreach (DataRow row in _dal.ObtenerTodos().Rows)
            {
                var modelo = new ModeloEstandar06AV
                {
                    Id = System.Convert.ToInt32(row["Id"]),
                    Nombre = row["Nombre"].ToString(),
                    Descripcion = row["Descripcion"] == DBNull.Value ? "" : row["Descripcion"].ToString(),
                    Componentes = ObtenerComponentes(System.Convert.ToInt32(row["Id"]))
                };
                lista.Add(modelo);
            }
            return lista;
        }

        private List<Componente06AV> ObtenerComponentes(int idModelo)
        {
            var comps = new List<Componente06AV>();
            foreach (DataRow row in _dal.ObtenerComponentes(idModelo).Rows)
            {
                var c = _componentes.ObtenerPorCodigo(row["CodigoComponente"].ToString());
                if (c != null) comps.Add(c);
            }
            return comps;
        }

        public void Agregar(ModeloEstandar06AV modelo)
        {
            modelo.Id = _dal.Agregar(modelo.Nombre, modelo.Descripcion);
            foreach (var c in modelo.Componentes)
                _dal.AgregarComponente(modelo.Id, c.Codigo);
        }

        public void Modificar(ModeloEstandar06AV modelo)
        {
            _dal.Modificar(modelo.Id, modelo.Nombre, modelo.Descripcion);
            _dal.QuitarComponentes(modelo.Id);
            foreach (var c in modelo.Componentes)
                _dal.AgregarComponente(modelo.Id, c.Codigo);
        }

        public void Eliminar(int id) => _dal.Eliminar(id);
    }
}
