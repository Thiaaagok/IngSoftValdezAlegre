using BE;
using DAL;
using System;
using System.Collections.Generic;
using System.Data;

namespace MPP
{
    /// <summary>Mapea entre LineaEnsamblaje06AV y la capa de acceso a datos.</summary>
    public class LineasEnsamblajeMPP06AV
    {
        private readonly LineasEnsamblajeDAL06AV _dal = new LineasEnsamblajeDAL06AV();

        public List<LineaEnsamblaje06AV> ObtenerTodas()
        {
            var lista = new List<LineaEnsamblaje06AV>();
            foreach (DataRow row in _dal.ObtenerTodas().Rows) lista.Add(Mapear(row));
            return lista;
        }

        public LineaEnsamblaje06AV ObtenerPorId(int id)
        {
            DataTable tabla = _dal.ObtenerPorId(id);
            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        public void Agregar(LineaEnsamblaje06AV l)
        {
            l.Id = _dal.Agregar(l.Nombre, l.Descripcion, l.Disponible);
        }

        public void Modificar(LineaEnsamblaje06AV l)
        {
            _dal.Modificar(l.Id, l.Nombre, l.Descripcion, l.Disponible);
        }

        public void Eliminar(int id) => _dal.Eliminar(id);

        private LineaEnsamblaje06AV Mapear(DataRow row)
        {
            return new LineaEnsamblaje06AV
            {
                Id          = Convert.ToInt32(row["Id"]),
                Nombre      = row["Nombre"].ToString(),
                Descripcion = row["Descripcion"] == DBNull.Value ? "" : row["Descripcion"].ToString(),
                Disponible  = Convert.ToBoolean(row["Disponible"])
            };
        }
    }
}
