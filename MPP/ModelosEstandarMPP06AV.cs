using BE;
using System;
using DAL;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MPP
{
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
                if (c == null) continue;
                int cantidad = row.Table.Columns.Contains("Cantidad") && row["Cantidad"] != System.DBNull.Value
                    ? System.Math.Max(1, System.Convert.ToInt32(row["Cantidad"])) : 1;
                for (int i = 0; i < cantidad; i++) comps.Add(c);
            }
            return comps;
        }

        public void Agregar(ModeloEstandar06AV modelo)
        {
            modelo.Id = _dal.Agregar(modelo.Nombre, modelo.Descripcion);
            foreach (var g in modelo.Componentes.GroupBy(c => c.Codigo))
                _dal.AgregarComponente(modelo.Id, g.Key, g.Count());
        }

        public void Modificar(ModeloEstandar06AV modelo)
        {
            _dal.Modificar(modelo.Id, modelo.Nombre, modelo.Descripcion);
            _dal.QuitarComponentes(modelo.Id);
            foreach (var g in modelo.Componentes.GroupBy(c => c.Codigo))
                _dal.AgregarComponente(modelo.Id, g.Key, g.Count());
        }

        public void Eliminar(int id) => _dal.Eliminar(id);
    }
}
