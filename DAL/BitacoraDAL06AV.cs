using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class BitacoraDAL06AV
    {

        public DataTable ObtenerTodos(Dictionary<string, object> parametros)
        {
            string query = "SELECT * FROM Bitacora";

            SqlConnection conn = Conexion.Instancia.ObtenerConexion();

            SqlCommand cmd = new SqlCommand(query, conn);

            DataTable tabla = new DataTable();

            conn.Open();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(tabla);
            conn.Close();

            return tabla;
        }

        public DataTable ObtenerPorId( Dictionary<string, object> parametros)
        {
            string query = "SELECT * FROM Bitacora WHERE Id = @id";

            SqlConnection conn = Conexion.Instancia.ObtenerConexion();

            SqlCommand cmd = new SqlCommand(query, conn);

            foreach (var p in parametros)
            {
                cmd.Parameters.AddWithValue( p.Key, p.Value );
            }

            DataTable tabla = new DataTable();

            conn.Open();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(tabla);
            conn.Close();

            return tabla;
        }

        public DataTable ObtenerPorCategoria( Dictionary<string, object> parametros)
        {
            string query = "SELECT * FROM Bitacora WHERE Categoria = @categoria";

            SqlConnection conn = Conexion.Instancia.ObtenerConexion();

            SqlCommand cmd = new SqlCommand(query, conn);

            foreach (var p in parametros)
            {
                cmd.Parameters.AddWithValue( p.Key,p.Value );
            }

            DataTable tabla = new DataTable();
            conn.Open();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(tabla);
            conn.Close();

            return tabla;
        }

        public string ObtenerSiguienteCodigo()
        {
            string query = @"SELECT ISNULL(MAX(CAST(SUBSTRING(Codigo, 5, 6) AS INT)), 0) + 1 FROM Bitacora";

            SqlConnection conn = Conexion.Instancia.ObtenerConexion();

            SqlCommand cmd = new SqlCommand(query, conn);

            conn.Open();
            object resultado = cmd.ExecuteScalar();
            conn.Close();
            int numero = Convert.ToInt32(resultado);

            return $"BIT-{numero:D6}";
        }

        public DataTable ObtenerPorUsuario(Dictionary<string, object> parametros)
        {
            string query = "SELECT * FROM Bitacora WHERE UsuarioDni = @usuarioDni";

            SqlConnection conn = Conexion.Instancia.ObtenerConexion();

            SqlCommand cmd = new SqlCommand(query, conn);

            foreach (var p in parametros)
            {
                cmd.Parameters.AddWithValue( p.Key, p.Value );
            }

            DataTable tabla = new DataTable();
            conn.Open();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(tabla);
            conn.Close();

            return tabla;
        }

        public DataTable ObtenerPorCriticidad( Dictionary<string, object> parametros)
        {
            string query = "SELECT * FROM Bitacora WHERE Criticidad = @criticidad";

            SqlConnection conn = Conexion.Instancia.ObtenerConexion();

            SqlCommand cmd = new SqlCommand(query, conn);

            foreach (var p in parametros)
            {
                cmd.Parameters.AddWithValue( p.Key, p.Value );
            }

            DataTable tabla = new DataTable();
            conn.Open();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(tabla);
            conn.Close();

            return tabla;
        }

        public DataTable ObtenerPorModulo( Dictionary<string, object> parametros)
        {
            string query = "SELECT * FROM Bitacora WHERE Modulo = @modulo";

            SqlConnection conn = Conexion.Instancia.ObtenerConexion();

            SqlCommand cmd = new SqlCommand(query, conn);

            foreach (var p in parametros)
            {
                cmd.Parameters.AddWithValue( p.Key, p.Value );
            }

            DataTable tabla = new DataTable();
            conn.Open();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(tabla);
            conn.Close();

            return tabla;
        }

        public DataTable ObtenerEntreFechas(Dictionary<string, object> parametros)
        {
            string query = @"SELECT * FROM Bitacora WHERE Fecha BETWEEN @desde AND @hasta";

            SqlConnection conn = Conexion.Instancia.ObtenerConexion();

            SqlCommand cmd = new SqlCommand(query, conn);

            foreach (var p in parametros)
            {
                cmd.Parameters.AddWithValue( p.Key, p.Value );
            }

            DataTable tabla = new DataTable();

            conn.Open();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(tabla);
            conn.Close();

            return tabla;
        }

        public bool Guardar(Dictionary<string, object> parametros)
        {
            string query = @"INSERT INTO Bitacora ( Categoria, Codigo, Criticidad, Descripcion, Fecha, Id, Modulo, UsuarioDni)
                VALUES
                ( @categoria, @codigo, @criticidad, @descripcion, @fecha, @id, @modulo, @usuarioDni )";

            return Ejecutar(query, parametros);
        }

        private bool Ejecutar( string query, Dictionary<string, object> parametros)
        {
            SqlConnection conn = Conexion.Instancia.ObtenerConexion();

            SqlCommand cmd = new SqlCommand(query, conn);

            foreach (var p in parametros)
            {
                cmd.Parameters.AddWithValue( p.Key, p.Value ?? DBNull.Value );
            }

            conn.Open();
            int filas = cmd.ExecuteNonQuery();
            conn.Close();

            return filas > 0;
        }

    }
}

