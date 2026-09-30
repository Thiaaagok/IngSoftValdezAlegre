using DAL;
using SER;
using System.Collections.Generic;
using System.Data;

namespace MPP
{
    public class FamiliaMPP06AV
    {
        private readonly FamiliaDAL06AV _dal = new FamiliaDAL06AV();
        private readonly PatenteMPP06AV _patenteMPP = new PatenteMPP06AV();

        public List<Familia06AV> ObtenerTodos()
        {
            DataTable tabla = _dal.ObtenerTodos();
            var lista = new List<Familia06AV>();
            foreach (DataRow row in tabla.Rows)
                lista.Add(ObtenerPorId(row["Id"].ToString()));
            return lista;
        }

        public Familia06AV ObtenerPorId(string id, HashSet<string> visitados = null)
        {
            if (visitados == null) visitados = new HashSet<string>();
            if (visitados.Contains(id)) return null;
            visitados.Add(id);

            DataTable tabla = _dal.ObtenerPorId(id);
            if (tabla.Rows.Count == 0) return null;

            var familia = new Familia06AV
            {
                Id = tabla.Rows[0]["Id"].ToString(),
                Descripcion = tabla.Rows[0]["Descripcion"].ToString()
            };

            DataTable patentes = _dal.ObtenerPatentesDeFamilia(id);
            foreach (DataRow row in patentes.Rows)
                familia.Agregar(_patenteMPP.Mapear(row));

            DataTable subfamilias = _dal.ObtenerSubfamiliasDeFamilia(id);
            foreach (DataRow row in subfamilias.Rows)
            {
                var sub = ObtenerPorId(row["Id"].ToString(), visitados);
                if (sub != null) familia.Agregar(sub);
            }

            return familia;
        }

        public void Agregar(Familia06AV familia)
        {
            _dal.Agregar(familia.Id, familia.Descripcion);
        }

        public void Modificar(Familia06AV familia)
        {
            _dal.Modificar(familia.Id, familia.Descripcion);
        }

        public void Eliminar(string id)
        {
            _dal.Eliminar(id);
        }

        public void AgregarPatente(string idFamilia, string idPatente)
        {
            _dal.AgregarPatente(idFamilia, idPatente);
        }

        public void QuitarPatente(string idFamilia, string idPatente)
        {
            _dal.QuitarPatente(idFamilia, idPatente);
        }

        public void AgregarSubfamilia(string idPadre, string idHijo)
        {
            _dal.AgregarSubfamilia(idPadre, idHijo);
        }

        public void QuitarSubfamilia(string idPadre, string idHijo)
        {
            _dal.QuitarSubfamilia(idPadre, idHijo);
        }
    }
}
