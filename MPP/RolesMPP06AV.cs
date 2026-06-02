using DAL;
using SER;
using System.Collections.Generic;
using System.Data;

namespace MPP
{
    public class RolesMPP06AV
    {
        private readonly RolesDAL06AV _dal = new RolesDAL06AV();
        private readonly PatenteMPP06AV _patenteMPP = new PatenteMPP06AV();
        private readonly FamiliaMPP06AV _familiaMPP = new FamiliaMPP06AV();

        // ── CRUD Rol ─────────────────────────────────────────────────────────

        public List<Rol06AV> ObtenerTodos()
        {
            DataTable tabla = _dal.ObtenerTodos();
            var lista = new List<Rol06AV>();
            foreach (DataRow row in tabla.Rows)
                lista.Add(ObtenerPorId(row["Id"].ToString()));
            return lista;
        }

        public Rol06AV ObtenerPorId(string id)
        {
            var parametros = new Dictionary<string, object> { { "@id", id } };
            DataTable tabla = _dal.ObtenerPorId(parametros);
            if (tabla.Rows.Count == 0) return null;
            return MapearRol(tabla.Rows[0]);
        }

        public void Agregar(Rol06AV rol)
        {
            var parametros = new Dictionary<string, object>
            {
                { "@id", rol.Id },
                { "@descripcion", rol.Descripcion }
            };
            _dal.Agregar(parametros);
        }

        public void Modificar(Rol06AV rol)
        {
            var parametros = new Dictionary<string, object>
            {
                { "@id", rol.Id },
                { "@descripcion", rol.Descripcion }
            };
            _dal.Modificar(parametros);
        }

        public void Eliminar(string id)
        {
            _dal.Eliminar(new Dictionary<string, object> { { "@id", id } });
        }

        // ── Gestión de hijos ─────────────────────────────────────────────────

        public void AgregarPatente(string idRol, string idPatente)
        {
            _dal.AgregarPatente(new Dictionary<string, object>
            {
                { "@idRol", idRol },
                { "@idPatente", idPatente }
            });
        }

        public void QuitarPatente(string idRol, string idPatente)
        {
            _dal.QuitarPatente(new Dictionary<string, object>
            {
                { "@idRol", idRol },
                { "@idPatente", idPatente }
            });
        }

        public void AgregarFamilia(string idRol, string idFamilia)
        {
            _dal.AgregarFamilia(new Dictionary<string, object>
            {
                { "@idRol", idRol },
                { "@idFamilia", idFamilia }
            });
        }

        public void QuitarFamilia(string idRol, string idFamilia)
        {
            _dal.QuitarFamilia(new Dictionary<string, object>
            {
                { "@idRol", idRol },
                { "@idFamilia", idFamilia }
            });
        }

        // ── Mapeo ─────────────────────────────────────────────────────────────

        private Rol06AV MapearRol(DataRow row)
        {
            string id = row["Id"].ToString();
            var rol = new Rol06AV
            {
                Id = id,
                Descripcion = row["Descripcion"].ToString()
            };

            // Agregar patentes directas
            var paramRol = new Dictionary<string, object> { { "@idRol", id } };
            DataTable patentes = _dal.ObtenerPatentesPorRol(paramRol);
            foreach (DataRow p in patentes.Rows)
                rol.Agregar(_patenteMPP.Mapear(p));

            // Agregar familias (con su árbol interno ya construido)
            DataTable familias = _dal.ObtenerFamiliasPorRol(paramRol);
            foreach (DataRow f in familias.Rows)
            {
                var familia = _familiaMPP.ObtenerPorId(f["Id"].ToString());
                if (familia != null) rol.Agregar(familia);
            }

            return rol;
        }
    }
}
