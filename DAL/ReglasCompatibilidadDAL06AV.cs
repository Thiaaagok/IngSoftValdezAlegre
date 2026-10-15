using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    public class ReglasCompatibilidadDAL06AV
    {
        public DataTable ObtenerTodas()
        {
            SqlConnection conn = Conexion.Instancia.ObtenerConexion();
            SqlCommand cmd = new SqlCommand("sp_ReglasCompat_ObtenerTodas", conn) { CommandType = CommandType.StoredProcedure };
            DataTable tabla = new DataTable();
            conn.Open();
            new SqlDataAdapter(cmd).Fill(tabla);
            conn.Close();
            return tabla;
        }

        public int Guardar(int id, string codigo, string nombre, bool activa, int nivel, int? tipoA, string atributoA,
                           string operador, int? tipoB, string atributoB, decimal? valor, string mensaje)
        {
            var parametros = new Dictionary<string, object>
            {
                { "@Id",        id                              },
                { "@Codigo",    Nulo(codigo)                    },
                { "@Nombre",    nombre ?? ""                    },
                { "@Activa",    activa                          },
                { "@Nivel",     nivel                           },
                { "@TipoA",     tipoA.HasValue ? (object)tipoA.Value : DBNull.Value },
                { "@AtributoA", Nulo(atributoA)                 },
                { "@Operador",  Nulo(operador)                  },
                { "@TipoB",     tipoB.HasValue ? (object)tipoB.Value : DBNull.Value },
                { "@AtributoB", Nulo(atributoB)                 },
                { "@Valor",     valor.HasValue ? (object)valor.Value : DBNull.Value },
                { "@Mensaje",   Nulo(mensaje)                   }
            };
            SqlConnection conn = Conexion.Instancia.ObtenerConexion();
            SqlCommand cmd = new SqlCommand("sp_ReglasCompat_Guardar", conn) { CommandType = CommandType.StoredProcedure };
            foreach (var p in parametros) cmd.Parameters.AddWithValue(p.Key, p.Value);
            conn.Open();
            object resultado = cmd.ExecuteScalar();
            conn.Close();
            return resultado == null || resultado == DBNull.Value ? id : Convert.ToInt32(resultado);
        }

        public void Eliminar(int id)
        {
            SqlConnection conn = Conexion.Instancia.ObtenerConexion();
            SqlCommand cmd = new SqlCommand("sp_ReglasCompat_Eliminar", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Id", id);
            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        private static object Nulo(string valor) =>
            string.IsNullOrWhiteSpace(valor) ? (object)DBNull.Value : valor.Trim();
    }
}
