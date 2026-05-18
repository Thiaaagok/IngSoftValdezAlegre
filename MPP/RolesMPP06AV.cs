using BE;
using DAL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPP
{
    public class RolesMPP06AV
    {
        private RolesDAL06AV RolesDAL = new RolesDAL06AV();

        #region Obtener

        public List<Rol06AV> ObtenerTodos()
        {
            DataTable tabla = RolesDAL.ObtenerTodos();

            List<Rol06AV> lista = new List<Rol06AV>();

            foreach (DataRow row in tabla.Rows)
                lista.Add(MapearRol(row));

            return lista;
        }

        public Rol06AV ObtenerPorId(string id)
        {
            var parametros = new Dictionary<string, object>
            {
                { "@id", id }
            };

            DataTable tabla = RolesDAL.ObtenerPorId(parametros);

            if (tabla.Rows.Count == 0)
                return null;

            return MapearRol(tabla.Rows[0]);
        }

        #endregion

        #region Mapeo

        private Rol06AV MapearRol(DataRow row)
        {
            var rol = new Rol06AV
            {
                Id = row["Id"].ToString(),
                Codigo = row["Codigo"].ToString(),
                Descripcion = row["Descripcion"].ToString(),
                Permisos = ObtenerPermisos(row["Id"].ToString())
            };

            return rol;
        }

        private string[] ObtenerPermisos(string rolId)
        {
            var parametros = new Dictionary<string, object>
            {
                { "@id", rolId }
            };

            DataTable tabla = RolesDAL.ObtenerPermisosPorRol(parametros);

            return tabla.AsEnumerable()
                        .Select(r => r["Permiso"].ToString())
                        .ToArray();
        }

        #endregion
    }
}
