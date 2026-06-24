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

        // ── CRUD Rol ──────────────────────────────────────────────────────────

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
            DataTable tabla = _dal.ObtenerPorId(id);
            if (tabla.Rows.Count == 0) return null;
            return MapearRol(tabla.Rows[0]);
        }

        public void Agregar(Rol06AV rol)
        {
            _dal.Agregar(rol.Id, rol.Descripcion, rol.Codigo);
        }

        public void Modificar(Rol06AV rol)
        {
            _dal.Modificar(rol.Id, rol.Descripcion);
        }

        public void Eliminar(string id)
        {
            _dal.Eliminar(id);
        }

        // ── Gestión de hijos ──────────────────────────────────────────────────

        public void AgregarPatente(string idRol, string idPatente)
        {
            _dal.AgregarPatente(idRol, idPatente);
        }

        public void QuitarPatente(string idRol, string idPatente)
        {
            _dal.QuitarPatente(idRol, idPatente);
        }

        public void AgregarFamilia(string idRol, string idFamilia)
        {
            _dal.AgregarFamilia(idRol, idFamilia);
        }

        public void QuitarFamilia(string idRol, string idFamilia)
        {
            _dal.QuitarFamilia(idRol, idFamilia);
        }

        // ── Mapeo ─────────────────────────────────────────────────────────────

        private Rol06AV MapearRol(DataRow row)
        {
            string id = row["Id"].ToString();
            var rol = new Rol06AV
            {
                Id = id,
                Descripcion = row["Descripcion"].ToString(),
                Codigo = row.Table.Columns.Contains("Codigo")
                                  ? row["Codigo"].ToString()
                                  : id
            };

            // Patentes directas
            DataTable patentes = _dal.ObtenerPatentesPorRol(id);
            foreach (DataRow p in patentes.Rows)
                rol.Agregar(_patenteMPP.Mapear(p));

            // Familias con su árbol interno ya construido
            DataTable familias = _dal.ObtenerFamiliasPorRol(id);
            foreach (DataRow f in familias.Rows)
            {
                var familia = _familiaMPP.ObtenerPorId(f["Id"].ToString());
                if (familia != null) rol.Agregar(familia);
            }

            return rol;
        }
    }
}