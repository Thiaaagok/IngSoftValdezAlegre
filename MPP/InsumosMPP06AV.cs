using BE;
using DAL;
using System;
using System.Collections.Generic;
using System.Data;

namespace MPP
{
    /// <summary>Mapea entre Insumo06AV y la capa de acceso a datos.</summary>
    public class InsumosMPP06AV
    {
        private readonly InsumosDAL06AV _dal = new InsumosDAL06AV();

        public List<Insumo06AV> ObtenerTodos() => Listar(_dal.ObtenerTodos());

        public List<Insumo06AV> ObtenerBajoStock() => Listar(_dal.ObtenerBajoStock());

        public Insumo06AV ObtenerPorCodigo(string codigo)
        {
            DataTable tabla = _dal.ObtenerPorCodigo(codigo);
            if (tabla.Rows.Count == 0) return null;
            return Mapear(tabla.Rows[0]);
        }

        public void Agregar(Insumo06AV i) => _dal.Agregar(i.Codigo, i.Descripcion, i.Stock, i.StockMinimo);

        public void Modificar(Insumo06AV i) => _dal.Modificar(i.Codigo, i.Descripcion, i.Stock, i.StockMinimo);

        public void Eliminar(string codigo) => _dal.Eliminar(codigo);

        private List<Insumo06AV> Listar(DataTable tabla)
        {
            var lista = new List<Insumo06AV>();
            foreach (DataRow row in tabla.Rows) lista.Add(Mapear(row));
            return lista;
        }

        private Insumo06AV Mapear(DataRow row)
        {
            return new Insumo06AV
            {
                Codigo      = row["Codigo"].ToString(),
                Descripcion = row["Descripcion"].ToString(),
                Stock       = Convert.ToInt32(row["Stock"]),
                StockMinimo = Convert.ToInt32(row["StockMinimo"])
            };
        }
    }
}
