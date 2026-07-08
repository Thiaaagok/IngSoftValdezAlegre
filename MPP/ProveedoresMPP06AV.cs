using BE;
using DAL;
using System;
using System.Collections.Generic;
using System.Data;

namespace MPP
{
    /// <summary>Mapea entre Proveedor06AV y la capa de acceso a datos.</summary>
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

        /// <summary>Inserta el proveedor y le asigna el Id generado.</summary>
        public void Agregar(Proveedor06AV p)
        {
            p.Id = _dal.Agregar(p.Nombre, p.Cuit, p.Email, p.Telefono, p.Direccion);
        }

        public void Modificar(Proveedor06AV p)
        {
            _dal.Modificar(p.Id, p.Nombre, p.Cuit, p.Email, p.Telefono, p.Direccion);
        }

        public void Eliminar(int id) => _dal.Eliminar(id);

        private Proveedor06AV Mapear(DataRow row)
        {
            return new Proveedor06AV
            {
                Id        = Convert.ToInt32(row["Id"]),
                Nombre    = row["Nombre"].ToString(),
                Cuit      = row["Cuit"].ToString(),
                Email     = row["Email"]     == DBNull.Value ? "" : row["Email"].ToString(),
                Telefono  = row["Telefono"]  == DBNull.Value ? "" : row["Telefono"].ToString(),
                Direccion = row["Direccion"] == DBNull.Value ? "" : row["Direccion"].ToString()
            };
        }
    }
}
