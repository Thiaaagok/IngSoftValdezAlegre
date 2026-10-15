using BE;
using DAL;
using System;
using System.Collections.Generic;
using System.Data;

namespace MPP
{
    public class ReglasCompatibilidadMPP06AV
    {
        private readonly ReglasCompatibilidadDAL06AV _dal = new ReglasCompatibilidadDAL06AV();

        public List<ReglaCompatibilidad06AV> ObtenerTodas()
        {
            var lista = new List<ReglaCompatibilidad06AV>();
            foreach (DataRow row in _dal.ObtenerTodas().Rows)
                lista.Add(Mapear(row));
            return lista;
        }

        public void Guardar(ReglaCompatibilidad06AV r)
        {
            r.Id = _dal.Guardar(r.Id, r.Codigo, r.Nombre, r.Activa, (int)r.Nivel,
                                r.TipoA.HasValue ? (int)r.TipoA.Value : (int?)null, r.AtributoA?.ToString(),
                                r.Operador?.ToString(), r.TipoB.HasValue ? (int)r.TipoB.Value : (int?)null,
                                r.AtributoB?.ToString(), r.Valor, r.Mensaje);
        }

        public void Eliminar(int id) => _dal.Eliminar(id);

        private static ReglaCompatibilidad06AV Mapear(DataRow row)
        {
            return new ReglaCompatibilidad06AV
            {
                Id        = Convert.ToInt32(row["Id"]),
                Codigo    = row["Codigo"].ToString(),
                Nombre    = row["Nombre"].ToString(),
                Sistema   = Convert.ToBoolean(row["Sistema"]),
                Activa    = Convert.ToBoolean(row["Activa"]),
                Nivel     = Convert.ToInt32(row["Nivel"]) == 1 ? NivelReglaCompatibilidad06AV.Advertencia
                                                            : NivelReglaCompatibilidad06AV.Incompatible,
                TipoA     = row["TipoA"] == DBNull.Value ? (TipoComponente06AV?)null : (TipoComponente06AV)Convert.ToInt32(row["TipoA"]),
                AtributoA = Enumerado<AtributoCompatibilidad06AV>(row["AtributoA"]),
                Operador  = Enumerado<OperadorCompatibilidad06AV>(row["Operador"]),
                TipoB     = row["TipoB"] == DBNull.Value ? (TipoComponente06AV?)null : (TipoComponente06AV)Convert.ToInt32(row["TipoB"]),
                AtributoB = Enumerado<AtributoCompatibilidad06AV>(row["AtributoB"]),
                Valor     = row["Valor"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["Valor"]),
                Mensaje   = row["Mensaje"] == DBNull.Value ? null : row["Mensaje"].ToString()
            };
        }

        private static T? Enumerado<T>(object valor) where T : struct
        {
            if (valor == null || valor == DBNull.Value) return null;
            return Enum.TryParse(valor.ToString(), true, out T r) ? r : (T?)null;
        }
    }
}
