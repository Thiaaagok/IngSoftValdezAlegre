using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>Acceso a datos de Clientes (PC Factory). Usa procedimientos almacenados.</summary>
    public class ClientesDAL06AV
    {
        public DataTable ObtenerTodos()
        {
            return EjecutarSP("sp_Clientes_ObtenerTodos", null);
        }

        public DataTable ObtenerPorDni(string dni)
        {
            return EjecutarSP("sp_Clientes_ObtenerPorDni", new Dictionary<string, object>
            {
                { "@Dni", dni }
            });
        }

        public void Agregar(string dni, string nombre, string apellido, string telefono, string direccion)
        {
            EjecutarSPNonQuery("sp_Clientes_Agregar", new Dictionary<string, object>
            {
                { "@Dni",       dni       },
                { "@Nombre",    nombre    },
                { "@Apellido",  apellido  },
                { "@Telefono",  (object)telefono  ?? "" },
                { "@Direccion", (object)direccion ?? "" }
            });
        }

        public void Modificar(string dni, string nombre, string apellido, string telefono, string direccion)
        {
            EjecutarSPNonQuery("sp_Clientes_Modificar", new Dictionary<string, object>
            {
                { "@Dni",       dni       },
                { "@Nombre",    nombre    },
                { "@Apellido",  apellido  },
                { "@Telefono",  (object)telefono  ?? "" },
                { "@Direccion", (object)direccion ?? "" }
            });
        }

        public void Eliminar(string dni)
        {
            EjecutarSPNonQuery("sp_Clientes_Eliminar", new Dictionary<string, object>
            {
                { "@Dni", dni }
            });
        }

        #region Helpers

        private DataTable EjecutarSP(string nombreSP, Dictionary<string, object> parametros)
        {
            SqlConnection conn = Conexion.Instancia.ObtenerConexion();
            SqlCommand cmd = new SqlCommand(nombreSP, conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            if (parametros != null)
                foreach (var p in parametros)
                    cmd.Parameters.AddWithValue(p.Key, p.Value);
            DataTable tabla = new DataTable();
            conn.Open();
            new SqlDataAdapter(cmd).Fill(tabla);
            conn.Close();
            return tabla;
        }

        private void EjecutarSPNonQuery(string nombreSP, Dictionary<string, object> parametros)
        {
            SqlConnection conn = Conexion.Instancia.ObtenerConexion();
            SqlCommand cmd = new SqlCommand(nombreSP, conn)
            {
                CommandType = CommandType.StoredProcedure
            };
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
