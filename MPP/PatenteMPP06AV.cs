using DAL;
using SER;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MPP
{
    public class PatenteMPP06AV
    {
        private readonly PatentesDAL06AV _dal = new PatentesDAL06AV();

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
            var parametros = new Dictionary<string, object> { { "@id", id } };
            DataTable tabla = _dal.ObtenerPorId(parametros);
            if (tabla.Rows.Count == 0) return null;
            return Mapear(tabla.Rows[0]);
        }

        public void Agregar(Patente06AV patente)
        {
            var parametros = new Dictionary<string, object>
            {
                { "@id", patente.Id },
                { "@descripcion", patente.Descripcion }
            };
            _dal.Agregar(parametros);
        }

        public void Modificar(Patente06AV patente)
        {
            var parametros = new Dictionary<string, object>
            {
                { "@id", patente.Id },
                { "@descripcion", patente.Descripcion }
            };
            _dal.Modificar(parametros);
        }

        public void Eliminar(string id)
        {
            var parametros = new Dictionary<string, object> { { "@id", id } };
            _dal.Eliminar(parametros);
        }

        /// <summary>
        /// Devuelve todas las patentes que el rol puede ejercer,
        /// expandiendo recursivamente la jerarquía de familias.
        /// </summary>
        public List<Patente06AV> ObtenerPatentesPorRol(string idRol)
        {
            DataTable tabla = _dal.ObtenerPatentesPorRol(idRol);
            var lista = new List<Patente06AV>();
            foreach (DataRow row in tabla.Rows)
                lista.Add(Mapear(row));
            return lista;
        }

        public Patente06AV Mapear(DataRow row)
        {
            return new Patente06AV
            {
                Id          = row["Id"].ToString(),
                Descripcion = row.Table.Columns.Contains("Descripcion")
                                  ? row["Descripcion"].ToString()
                                  : row["Id"].ToString()
            };
        }
    }
}
