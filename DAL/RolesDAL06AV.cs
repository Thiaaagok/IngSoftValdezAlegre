using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class RolesDAL06AV
    {
        // ── Roles ────────────────────────────────────────────────────────────

        public DataTable ObtenerTodos()
        {
            return EjecutarQuery("SELECT Id, Descripcion, Codigo FROM Roles", null);
        }

        public DataTable ObtenerPorId(Dictionary<string, object> parametros)
        {
            return EjecutarQuery("SELECT Id, Descripcion, Codigo FROM Roles WHERE Id = @id", parametros);
        }

        public void Agregar(Dictionary<string, object> parametros)
        {
            EjecutarNonQuery("INSERT INTO Roles (Id, Descripcion, Codigo) VALUES (@id, @descripcion, @codigo)", parametros);
        }

        public void Modificar(Dictionary<string, object> parametros)
        {
            EjecutarNonQuery("UPDATE Roles SET Descripcion = @descripcion WHERE Id = @id", parametros);
        }

        public void Eliminar(Dictionary<string, object> parametros)
        {
            EjecutarNonQuery("DELETE FROM Roles WHERE Id = @id", parametros);
        }

        // ── Relación Rol → Patente ───────────────────────────────────────────

        public DataTable ObtenerPatentesPorRol(Dictionary<string, object> parametros)
        {
            string query = @"SELECT p.Id, p.Descripcion
                             FROM RolPatentes rp
                             JOIN Patentes p ON p.Id = rp.IdPatente
                             WHERE rp.IdRol = @idRol";
            return EjecutarQuery(query, parametros);
        }

        public void AgregarPatente(Dictionary<string, object> parametros)
        {
            EjecutarNonQuery(
                "INSERT INTO RolPatentes (IdRol, IdPatente) VALUES (@idRol, @idPatente)",
                parametros);
        }

        public void QuitarPatente(Dictionary<string, object> parametros)
        {
            EjecutarNonQuery(
                "DELETE FROM RolPatentes WHERE IdRol = @idRol AND IdPatente = @idPatente",
                parametros);
        }

        // ── Relación Rol → Familia ───────────────────────────────────────────

        public DataTable ObtenerFamiliasPorRol(Dictionary<string, object> parametros)
        {
            string query = @"SELECT f.Id, f.Descripcion
                             FROM RolFamilias rf
                             JOIN Familias f ON f.Id = rf.IdFamilia
                             WHERE rf.IdRol = @idRol";
            return EjecutarQuery(query, parametros);
        }

        public void AgregarFamilia(Dictionary<string, object> parametros)
        {
            EjecutarNonQuery(
                "INSERT INTO RolFamilias (IdRol, IdFamilia) VALUES (@idRol, @idFamilia)",
                parametros);
        }

        public void QuitarFamilia(Dictionary<string, object> parametros)
        {
            EjecutarNonQuery(
                "DELETE FROM RolFamilias WHERE IdRol = @idRol AND IdFamilia = @idFamilia",
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
