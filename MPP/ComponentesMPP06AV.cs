using BE;
using DAL;
using System;
using System.Collections.Generic;
using System.Data;

namespace MPP
{
    /// <summary>Mapea entre Componente06AV y la capa de acceso a datos.</summary>
    public class ComponentesMPP06AV
    {
        private readonly ComponentesDAL06AV _dal = new ComponentesDAL06AV();

        public List<Componente06AV> ObtenerTodos()
        {
            DataTable tabla = _dal.ObtenerTodos();
            var lista = new List<Componente06AV>();
            foreach (DataRow row in tabla.Rows)
                lista.Add(Mapear(row));
            return lista;
        }

        public Componente06AV ObtenerPorCodigo(string codigo)
        {
            DataTable tabla = _dal.ObtenerPorCodigo(codigo);
            if (tabla.Rows.Count == 0) return null;
            return Mapear(tabla.Rows[0]);
        }

        public void Agregar(Componente06AV c)
        {
            _dal.Agregar(c.Codigo, c.Descripcion, (int)c.Tipo, c.Marca, c.Modelo,
                         c.PrecioUnitario, c.StockDisponible);
        }

        public void Modificar(Componente06AV c)
        {
            _dal.Modificar(c.Codigo, c.Descripcion, (int)c.Tipo, c.Marca, c.Modelo,
                           c.PrecioUnitario, c.StockDisponible);
        }

        public void Eliminar(string codigo)
        {
            _dal.Eliminar(codigo);
        }

        /// <summary>Descuenta <paramref name="cantidad"/> unidades del stock del componente.</summary>
        public void DescontarStock(string codigo, int cantidad)
        {
            _dal.DescontarStock(codigo, cantidad);
        }

        private Componente06AV Mapear(DataRow row)
        {
            return new Componente06AV
            {
                Codigo          = row["Codigo"].ToString(),
                Descripcion     = row["Descripcion"].ToString(),
                Tipo            = (TipoComponente06AV)Convert.ToInt32(row["Tipo"]),
                Marca           = row["Marca"] == DBNull.Value ? "" : row["Marca"].ToString(),
                Modelo          = row["Modelo"] == DBNull.Value ? "" : row["Modelo"].ToString(),
                PrecioUnitario  = Convert.ToDecimal(row["PrecioUnitario"]),
                StockDisponible = Convert.ToInt32(row["StockDisponible"])
            };
        }
    }
}
