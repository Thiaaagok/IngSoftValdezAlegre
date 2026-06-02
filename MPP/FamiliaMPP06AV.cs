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
    public class FamiliaMPP06AV
    {
        private readonly FamiliaDAL06AV _dal = new FamiliaDAL06AV();
        private readonly PatenteMPP06AV _patenteMPP = new PatenteMPP06AV();

        // ── CRUD Familia ─────────────────────────────────────────────────────

        public List<Familia06AV> ObtenerTodos()
        {
            DataTable tabla = _dal.ObtenerTodos();
            var lista = new List<Familia06AV>();
            foreach (DataRow row in tabla.Rows)
                lista.Add(ObtenerPorId(row["Id"].ToString())); // carga hijos
            return lista;
        }

        /// <summary>
        /// Carga la familia con todos sus hijos directos (patentes y subfamilias).
        /// Usa un conjunto de visitados para evitar ciclos.
        /// </summary>
        public Familia06AV ObtenerPorId(string id, HashSet<string> visitados = null)
        {
            if (visitados == null) visitados = new HashSet<string>();
            if (visitados.Contains(id)) return null; // ciclo detectado
            visitados.Add(id);

            var parametros = new Dictionary<string, object> { { "@id", id } };
            DataTable tabla = _dal.ObtenerPorId(parametros);
            if (tabla.Rows.Count == 0) return null;

            var familia = new Familia06AV
            {
                Id = tabla.Rows[0]["Id"].ToString(),
                Descripcion = tabla.Rows[0]["Descripcion"].ToString()
            };

            // Agregar patentes directas
            var paramFam = new Dictionary<string, object> { { "@idFamilia", id } };
            DataTable patentes = _dal.ObtenerPatentesDeFamilia(paramFam);
            foreach (DataRow row in patentes.Rows)
                familia.Agregar(_patenteMPP.Mapear(row));

            // Agregar subfamilias (recursivo)
            var paramPadre = new Dictionary<string, object> { { "@idPadre", id } };
            DataTable subfamilias = _dal.ObtenerSubfamiliasDeFamilia(paramPadre);
            foreach (DataRow row in subfamilias.Rows)
            {
                var sub = ObtenerPorId(row["Id"].ToString(), visitados);
                if (sub != null) familia.Agregar(sub);
            }

            return familia;
        }

        public void Agregar(Familia06AV familia)
        {
            var parametros = new Dictionary<string, object>
            {
                { "@id", familia.Id },
                { "@descripcion", familia.Descripcion }
            };
            _dal.Agregar(parametros);
        }

        public void Modificar(Familia06AV familia)
        {
            var parametros = new Dictionary<string, object>
            {
                { "@id", familia.Id },
                { "@descripcion", familia.Descripcion }
            };
            _dal.Modificar(parametros);
        }

        public void Eliminar(string id)
        {
            _dal.Eliminar(new Dictionary<string, object> { { "@id", id } });
        }

        // ── Gestión de hijos ─────────────────────────────────────────────────

        public void AgregarPatente(string idFamilia, string idPatente)
        {
            var parametros = new Dictionary<string, object>
            {
                { "@idFamilia", idFamilia },
                { "@idPatente", idPatente }
            };
            _dal.AgregarPatente(parametros);
        }

        public void QuitarPatente(string idFamilia, string idPatente)
        {
            var parametros = new Dictionary<string, object>
            {
                { "@idFamilia", idFamilia },
                { "@idPatente", idPatente }
            };
            _dal.QuitarPatente(parametros);
        }

        public void AgregarSubfamilia(string idPadre, string idHijo)
        {
            var parametros = new Dictionary<string, object>
            {
                { "@idPadre", idPadre },
                { "@idHijo", idHijo }
            };
            _dal.AgregarSubfamilia(parametros);
        }

        public void QuitarSubfamilia(string idPadre, string idHijo)
        {
            var parametros = new Dictionary<string, object>
            {
                { "@idPadre", idPadre },
                { "@idHijo", idHijo }
            };
            _dal.QuitarSubfamilia(parametros);
        }
    }
}
