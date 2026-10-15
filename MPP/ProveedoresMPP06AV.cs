using BE;
using DAL;
using System;
using System.Collections.Generic;
using System.Data;

namespace MPP
{
    public class ProveedoresMPP06AV
    {
        private readonly ProveedoresDAL06AV _dal = new ProveedoresDAL06AV();

        public List<Proveedor06AV> ObtenerTodos()
        {
            var lista = new List<Proveedor06AV>();
            foreach (DataRow row in _dal.ObtenerTodos().Rows) lista.Add(Mapear(row));
            return lista;
        }

        public Proveedor06AV ObtenerPorId(int id)
        {
            DataTable tabla = _dal.ObtenerPorId(id);
            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        public Proveedor06AV ObtenerPorCuit(string cuit)
        {
            DataTable tabla = _dal.ObtenerPorCuit(cuit);
            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        public void Agregar(Proveedor06AV p)
        {
            p.Id = _dal.Agregar(p.Nombre, p.Cuit, p.Email, p.Telefono, p.Direccion, p.Activo);
        }

        public void Modificar(Proveedor06AV p)
        {
            _dal.Modificar(p.Id, p.Nombre, p.Cuit, p.Email, p.Telefono, p.Direccion, p.Activo);
        }

        public bool Eliminar(int id) => _dal.Eliminar(id);

        public List<FilaReporteProveedores06AV> ObtenerReporte(Func<int, string> nombreTipo)
        {
            var tipos = new Dictionary<int, List<int>>();
            foreach (DataRow r in _dal.ObtenerReporteTipos().Rows)
            {
                int id = Convert.ToInt32(r["IdProveedor"]);
                if (!tipos.ContainsKey(id)) tipos[id] = new List<int>();
                tipos[id].Add(Convert.ToInt32(r["Tipo"]));
            }

            var lista = new List<FilaReporteProveedores06AV>();
            foreach (DataRow r in _dal.ObtenerReporte().Rows)
            {
                int id = Convert.ToInt32(r["Id"]);
                List<int> deEste = tipos.ContainsKey(id) ? tipos[id] : new List<int>();
                deEste.Sort();
                lista.Add(new FilaReporteProveedores06AV
                {
                    Id = id,
                    Nombre = r["Nombre"].ToString(),
                    Cuit = r["Cuit"].ToString(),
                    Email = r["Email"] == DBNull.Value ? "" : r["Email"].ToString(),
                    Telefono = r["Telefono"] == DBNull.Value ? "" : r["Telefono"].ToString(),
                    Direccion = r["Direccion"] == DBNull.Value ? "" : r["Direccion"].ToString(),
                    Activo = Convert.ToBoolean(r["Activo"]),
                    Cotizaciones = Convert.ToInt32(r["Cotizaciones"]),
                    OrdenesCompra = Convert.ToInt32(r["OrdenesCompra"]),
                    TotalComprado = r["TotalComprado"] == DBNull.Value ? 0m : Convert.ToDecimal(r["TotalComprado"]),
                    UltimaCompra = r["UltimaCompra"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["UltimaCompra"]),
                    Tipos = string.Join(", ", deEste.ConvertAll(t => nombreTipo(t)))
                });
            }
            return lista;
        }

        private Proveedor06AV Mapear(DataRow row)
        {
            return new Proveedor06AV
            {
                Id        = Convert.ToInt32(row["Id"]),
                Nombre    = row["Nombre"].ToString(),
                Cuit      = row["Cuit"].ToString(),
                Email     = row["Email"]     == DBNull.Value ? "" : row["Email"].ToString(),
                Telefono  = row["Telefono"]  == DBNull.Value ? "" : row["Telefono"].ToString(),
                Direccion = row["Direccion"] == DBNull.Value ? "" : row["Direccion"].ToString(),
                Activo    = !row.Table.Columns.Contains("Activo") || row["Activo"] == DBNull.Value || Convert.ToBoolean(row["Activo"])
            };
        }
    }
}
