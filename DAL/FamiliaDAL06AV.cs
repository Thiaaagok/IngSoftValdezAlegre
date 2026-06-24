using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    public class FamiliaDAL06AV
    {
        // ── Familias ─────────────────────────────────────────────────────────

        public DataTable ObtenerTodos()
        {
            return EjecutarSP("sp_Familias_ObtenerTodos", null);
        }

        public DataTable ObtenerPorId(string id)
        {
            return EjecutarSP("sp_Familias_ObtenerPorId", new Dictionary<string, object>
            {
                { "@Id", id }
            });
        }

        public void Agregar(string id, string descripcion)
        {
            EjecutarSPNonQuery("sp_Familias_Agregar", new Dictionary<string, object>
            {
                { "@Id",          id          },
                { "@Descripcion", descripcion }
            });
        }

        public void Modificar(string id, string descripcion)
        {
            EjecutarSPNonQuery("sp_Familias_Modificar", new Dictionary<string, object>
            {
                { "@Id",          id          },
                { "@Descripcion", descripcion }
            });
        }

        public void Eliminar(string id)
        {
            EjecutarSPNonQuery("sp_Familias_Eliminar", new Dictionary<string, object>
            {
                { "@Id", id }
            });
        }

        // ── Relación Familia → Patente ────────────────────────────────────────

        public DataTable ObtenerPatentesDeFamilia(string idFamilia)
        {
            return EjecutarSP("sp_Familias_ObtenerPatentes", new Dictionary<string, object>
            {
                { "@IdFamilia", idFamilia }
            });
        }

        public void AgregarPatente(string idFamilia, string idPatente)
        {
            EjecutarSPNonQuery("sp_Familias_AgregarPatente", new Dictionary<string, object>
            {
                { "@IdFamilia", idFamilia },
                { "@IdPatente", idPatente }
            });
        }

        public void QuitarPatente(string idFamilia, string idPatente)
        {
            EjecutarSPNonQuery("sp_Familias_QuitarPatente", new Dictionary<string, object>
            {
                { "@IdFamilia", idFamilia },
                { "@IdPatente", idPatente }
            });
        }

        // ── Relación Familia → Familia hija ──────────────────────────────────

        public DataTable ObtenerSubfamiliasDeFamilia(string idPadre)
        {
            return EjecutarSP("sp_Familias_ObtenerSubfamilias", new Dictionary<string, object>
            {
                { "@IdPadre", idPadre }
            });
        }

        public void AgregarSubfamilia(string idPadre, string idHijo)
        {
            EjecutarSPNonQuery("sp_Familias_AgregarSubfamilia", new Dictionary<string, object>
            {
                { "@IdPadre", idPadre },
                { "@IdHijo",  idHijo  }
            });
        }

        public void QuitarSubfamilia(string idPadre, string idHijo)
        {
            EjecutarSPNonQuery("sp_Familias_QuitarSubfamilia", new Dictionary<string, object>
            {
                { "@IdPadre", idPadre },
                { "@IdHijo",  idHijo  }
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