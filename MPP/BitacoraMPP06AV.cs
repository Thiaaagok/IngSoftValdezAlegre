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
    public class BitacoraMPP06AV
    {
        BitacoraDAL06AV BitacoraDAL = new BitacoraDAL06AV();

        #region Guardar

        public bool Guardar(Bitacora06AV bitacora)
        {
            var parametros = new Dictionary<string, object>
            {
                { "@categoria",   bitacora.Categoria },
                { "@codigo", bitacora.Codigo },
                { "@criticidad",  bitacora.Criticidad },
                { "@descripcion", bitacora.Descripcion },
                { "@fecha",       bitacora.Fecha },
                { "@id",          bitacora.Id },
                { "@modulo",      bitacora.Modulo },
                { "@usuarioDni",  bitacora.UsuarioDni }
            };
            return BitacoraDAL.Guardar(parametros);
        }

        #endregion

        #region Obtener


        // AsEnumerable => Transforma un DataTable a IEnumerable<DataRow> para poder utilizar LINQ
        // .Select(row => MapearBitacora(row)) => Por cada fila de la tabla ejecuta MapearBitacora
        // .ToList() => Devuelte todo el resultado en una list para sel SER
        public List<Bitacora06AV> ObtenerTodos()
        {
            DataTable tabla = BitacoraDAL.ObtenerTodos(new Dictionary<string, object>());
            return tabla.AsEnumerable().Select(row => MapearBitacora(row)).ToList();
        }

        public Bitacora06AV ObtenerPorId(Guid id)
        {
            var parametros = new Dictionary<string, object> { { "@id", id } };
            DataTable tabla = BitacoraDAL.ObtenerPorId(parametros);

            if (tabla.Rows.Count == 0) return null;
            return MapearBitacora(tabla.Rows[0]);
        }

        public List<Bitacora06AV> ObtenerPorCategoria(string categoria)
        {
            var parametros = new Dictionary<string, object> { { "@categoria", categoria } };
            DataTable tabla = BitacoraDAL.ObtenerPorCategoria(parametros);
            return tabla.AsEnumerable().Select(row => MapearBitacora(row)).ToList();
        }

        public List<Bitacora06AV> ObtenerPorCriticidad(string criticidad)
        {
            var parametros = new Dictionary<string, object> { { "@criticidad", criticidad } };
            DataTable tabla = BitacoraDAL.ObtenerPorCriticidad(parametros);
            return tabla.AsEnumerable().Select(row => MapearBitacora(row)).ToList();
        }

        public List<Bitacora06AV> ObtenerPorModulo(string modulo)
        {
            var parametros = new Dictionary<string, object> { { "@modulo", modulo } };
            DataTable tabla = BitacoraDAL.ObtenerPorModulo(parametros);
            return tabla.AsEnumerable().Select(row => MapearBitacora(row)).ToList();
        }

        public List<Bitacora06AV> ObtenerPorUsuario(string usuarioDni)
        {
            var parametros = new Dictionary<string, object> { { "@usuarioDni", usuarioDni } };
            DataTable tabla = BitacoraDAL.ObtenerPorUsuario(parametros);
            return tabla.AsEnumerable().Select(row => MapearBitacora(row)).ToList();
        }

        public List<Bitacora06AV> ObtenerEntreFechas(DateTime desde, DateTime hasta)
        {
            var parametros = new Dictionary<string, object>
            {
                { "@desde", desde },
                { "@hasta", hasta }
            };
            DataTable tabla = BitacoraDAL.ObtenerEntreFechas(parametros);
            return tabla.AsEnumerable().Select(row => MapearBitacora(row)).ToList();
        }

        public string ObtenerSiguienteCodigo()
        {
            return BitacoraDAL.ObtenerSiguienteCodigo();
        }

        #endregion

        #region Mapper

        private Bitacora06AV MapearBitacora(DataRow row)
        {
            return new Bitacora06AV
            {
                Codigo = row["Codigo"].ToString(),
                Categoria = row["Categoria"].ToString(),
                Criticidad = row["Criticidad"].ToString(),
                Descripcion = row["Descripcion"].ToString(),
                Fecha = Convert.ToDateTime(row["Fecha"]),
                Id = row["Id"].ToString(),
                Modulo = row["Modulo"].ToString(),
                UsuarioDni = row["UsuarioDni"].ToString()
            };
        }

        #endregion
    }
}
