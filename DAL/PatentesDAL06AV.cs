using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class PatentesDAL06AV
    {
        public DataTable ObtenerTodos()
        {
            string query = "SELECT Id, Descripcion FROM Patentes";
            return EjecutarQuery(query, null);
        }

        public DataTable ObtenerPorId(Dictionary<string, object> parametros)
        {
            string query = "SELECT Id, Descripcion FROM Patentes WHERE Id = @id";
            return EjecutarQuery(query, parametros);
        }

        public void Agregar(Dictionary<string, object> parametros)
        {
            string query = "INSERT INTO Patentes (Id, Descripcion) VALUES (@id, @descripcion)";
            EjecutarNonQuery(query, parametros);
        }

        public void Modificar(Dictionary<string, object> parametros)
        {
            string query = "UPDATE Patentes SET Descripcion = @descripcion WHERE Id = @id";
            EjecutarNonQuery(query, parametros);
        }

        public void Eliminar(Dictionary<string, object> parametros)
        {
            string query = "DELETE FROM Patentes WHERE Id = @id";
            EjecutarNonQuery(query, parametros);
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
