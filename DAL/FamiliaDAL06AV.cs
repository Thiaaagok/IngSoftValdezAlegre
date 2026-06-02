using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class FamiliaDAL06AV
    {
        // ── Familias ────────────────────────────────────────────────────────

        public DataTable ObtenerTodos()
        {
            return EjecutarQuery("SELECT Id, Descripcion FROM Familias", null);
        }

        public DataTable ObtenerPorId(Dictionary<string, object> parametros)
        {
            return EjecutarQuery("SELECT Id, Descripcion FROM Familias WHERE Id = @id", parametros);
        }

        public void Agregar(Dictionary<string, object> parametros)
        {
            EjecutarNonQuery("INSERT INTO Familias (Id, Descripcion) VALUES (@id, @descripcion)", parametros);
        }

        public void Modificar(Dictionary<string, object> parametros)
        {
            EjecutarNonQuery("UPDATE Familias SET Descripcion = @descripcion WHERE Id = @id", parametros);
        }

        public void Eliminar(Dictionary<string, object> parametros)
        {
            EjecutarNonQuery("DELETE FROM Familias WHERE Id = @id", parametros);
        }

        // ── Relación Familia → Patente ───────────────────────────────────────

        /// <summary>Devuelve las patentes directas de una familia (no recursivo).</summary>
        public DataTable ObtenerPatentesDeFamilia(Dictionary<string, object> parametros)
        {
            string query = @"SELECT p.Id, p.Descripcion
                             FROM FamiliaPatentes fp
                             JOIN Patentes p ON p.Id = fp.IdPatente
                             WHERE fp.IdFamilia = @idFamilia";
            return EjecutarQuery(query, parametros);
        }

        public void AgregarPatente(Dictionary<string, object> parametros)
        {
            EjecutarNonQuery(
                "INSERT INTO FamiliaPatentes (IdFamilia, IdPatente) VALUES (@idFamilia, @idPatente)",
                parametros);
        }

        public void QuitarPatente(Dictionary<string, object> parametros)
        {
            EjecutarNonQuery(
                "DELETE FROM FamiliaPatentes WHERE IdFamilia = @idFamilia AND IdPatente = @idPatente",
                parametros);
        }

        // ── Relación Familia → Familia hija ─────────────────────────────────

        /// <summary>Devuelve las subfamilias directas de una familia.</summary>
        public DataTable ObtenerSubfamiliasDeFamilia(Dictionary<string, object> parametros)
        {
            string query = @"SELECT f.Id, f.Descripcion
                             FROM FamiliaFamilias ff
                             JOIN Familias f ON f.Id = ff.IdHijo
                             WHERE ff.IdPadre = @idPadre";
            return EjecutarQuery(query, parametros);
        }

        public void AgregarSubfamilia(Dictionary<string, object> parametros)
        {
            EjecutarNonQuery(
                "INSERT INTO FamiliaFamilias (IdPadre, IdHijo) VALUES (@idPadre, @idHijo)",
                parametros);
        }

        public void QuitarSubfamilia(Dictionary<string, object> parametros)
        {
            EjecutarNonQuery(
                "DELETE FROM FamiliaFamilias WHERE IdPadre = @idPadre AND IdHijo = @idHijo",
                parametros);
        }

        #region Helpers

        private DataTable EjecutarQuery(string query, Dictionary<string, object> parametros)
        {
            SqlConnection conn = Conexion.Instancia.ObtenerConexion();
            SqlCommand cmd = new SqlCommand(query, conn);
            if (parametros != null)
                foreach (var p in parametros)
                    cmd.Parameters.AddWithValue(p.Key, p.Value);
            DataTable tabla = new DataTable();
            conn.Open();
            new SqlDataAdapter(cmd).Fill(tabla);
            conn.Close();
            return tabla;
        }

        private void EjecutarNonQuery(string query, Dictionary<string, object> parametros)
        {
            SqlConnection conn = Conexion.Instancia.ObtenerConexion();
            SqlCommand cmd = new SqlCommand(query, conn);
            if (parametros != null)
                foreach (var p in parametros)
                    cmd.Parameters.AddWithValue(p.Key, p.Value);
            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        #endregion
    }
}
