using BE;
using DAL;
using System;
using System.Collections.Generic;
using System.Data;

namespace MPP
{
    /// <summary>
    /// Mapea entre Componente06AV y la capa de acceso a datos. Tras el refactor,
    /// Componente absorbe a Insumo: es la pieza comprable/stockeable (Stock/StockMinimo).
    /// </summary>
    public class ComponentesMPP06AV
    {
        private readonly ComponentesDAL06AV _dal = new ComponentesDAL06AV();

        public List<Componente06AV> ObtenerTodos() => Listar(_dal.ObtenerTodos());

        public List<Componente06AV> ObtenerBajoStock() => Listar(_dal.ObtenerBajoStock());

        public Componente06AV ObtenerPorCodigo(string codigo)
        {
            DataTable tabla = _dal.ObtenerPorCodigo(codigo);
            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        public void Agregar(Componente06AV c)
        {
            _dal.Agregar(c.Codigo, c.Descripcion, c.Marca, c.Modelo,
                         c.PrecioUnitario, c.Stock, c.StockMinimo, (int)c.Tipo);
        }

        public void Modificar(Componente06AV c)
        {
            _dal.Modificar(c.Codigo, c.Descripcion, c.Marca, c.Modelo,
                           c.PrecioUnitario, c.Stock, c.StockMinimo, (int)c.Tipo);
        }

        public void Eliminar(string codigo) => _dal.Eliminar(codigo);

        /// <summary>Descuenta <paramref name="cantidad"/> unidades del stock del componente.</summary>
        public void DescontarStock(string codigo, int cantidad) => _dal.DescontarStock(codigo, cantidad);

        /// <summary>Suma <paramref name="cantidad"/> unidades al stock (al recibir una compra).</summary>
        public void SumarStock(string codigo, int cantidad) => _dal.SumarStock(codigo, cantidad);

        private List<Componente06AV> Listar(DataTable tabla)
        {
            var lista = new List<Componente06AV>();
            foreach (DataRow row in tabla.Rows) lista.Add(Mapear(row));
            return lista;
        }

        private Componente06AV Mapear(DataRow row)
        {
            return new Componente06AV
            {
                Codigo         = row["Codigo"].ToString(),
                Descripcion    = row["Descripcion"].ToString(),
                Marca          = row["Marca"] == DBNull.Value ? "" : row["Marca"].ToString(),
                Modelo         = row["Modelo"] == DBNull.Value ? "" : row["Modelo"].ToString(),
                PrecioUnitario = Convert.ToDecimal(row["PrecioUnitario"]),
                Stock          = Convert.ToInt32(row["Stock"]),
                StockMinimo    = Convert.ToInt32(row["StockMinimo"]),
                Tipo           = (TipoComponente06AV)Convert.ToInt32(row["Tipo"])
            };
        }
    }
}
