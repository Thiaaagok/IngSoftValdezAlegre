using DAL;
using SER;
using System.Collections.Generic;
using System.Data;

namespace MPP
{
    public class PatenteMPP06AV
    {
        private readonly PatentesDAL06AV _dal = new PatentesDAL06AV();

        // ── CRUD Patente ──────────────────────────────────────────────────────

        public List<Patente06AV> ObtenerTodos()
        {
            DataTable tabla = _dal.ObtenerTodos();
            var lista = new List<Patente06AV>();
            foreach (DataRow row in tabla.Rows)
                lista.Add(Mapear(row));
            return lista;
        }

        public Patente06AV ObtenerPorId(string id)
        {
            DataTable tabla = _dal.ObtenerPorId(id);
            if (tabla.Rows.Count == 0) return null;
            return Mapear(tabla.Rows[0]);
        }

        public List<Patente06AV> ObtenerPatentesPorRol(string idRol)
        {
            DataTable tabla = _dal.ObtenerPatentesPorRol(idRol);
            var lista = new List<Patente06AV>();
            foreach (DataRow row in tabla.Rows)
                lista.Add(Mapear(row));
            return lista;
        }

        // ── Mapeo ─────────────────────────────────────────────────────────────

        public Patente06AV Mapear(DataRow row)
        {
            return new Patente06AV
            {
                Id = row["Id"].ToString(),
                Descripcion = row["Descripcion"].ToString()
            };
        }
    }
}