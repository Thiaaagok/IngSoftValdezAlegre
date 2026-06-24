using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    public class RolesDAL06AV
    {

        public DataTable ObtenerTodos()
        {
            return EjecutarSP("sp_Roles_ObtenerTodos", null);
        }

        public DataTable ObtenerPorId(string id)
        {
            return EjecutarSP("sp_Roles_ObtenerPorId", new Dictionary<string, object>
            {
                { "@Id", id }
            });
        }

        public void Agregar(string id, string descripcion, string codigo)
        {
            EjecutarSPNonQuery("sp_Roles_Agregar", new Dictionary<string, object>
            {
                { "@Id",          id          },
                { "@Descripcion", descripcion },
                { "@Codigo",      codigo      }
            });
        }

        public void Modificar(string id, string descripcion)
        {
            EjecutarSPNonQuery("sp_Roles_Modificar", new Dictionary<string, object>
            {
                { "@Id",          id          },
                { "@Descripcion", descripcion }
            });
        }

        public void Eliminar(string id)
        {
            EjecutarSPNonQuery("sp_Roles_Eliminar", new Dictionary<string, object>
            {
                { "@Id", id }
            });
        }

        // ── Relación Rol → Patente ────────────────────────────────────────────

        public DataTable ObtenerPatentesPorRol(string idRol)
        {
            return EjecutarSP("sp_Roles_ObtenerPatentes", new Dictionary<string, object>
            {
                { "@IdRol", idRol }
            });
        }

        public void AgregarPatente(string idRol, string idPatente)
        {
            EjecutarSPNonQuery("sp_Roles_AgregarPatente", new Dictionary<string, object>
            {
                { "@IdRol",     idRol     },
                { "@IdPatente", idPatente }
            });
        }

        public void QuitarPatente(string idRol, string idPatente)
        {
            EjecutarSPNonQuery("sp_Roles_QuitarPatente", new Dictionary<string, object>
            {
                { "@IdRol",     idRol     },
                { "@IdPatente", idPatente }
            });
        }

        // ── Relación Rol → Familia ────────────────────────────────────────────

        public DataTable ObtenerFamiliasPorRol(string idRol)
        {
            return EjecutarSP("sp_Roles_ObtenerFamilias", new Dictionary<string, object>
            {
                { "@IdRol", idRol }
            });
        }

        public void AgregarFamilia(string idRol, string idFamilia)
        {
            EjecutarSPNonQuery("sp_Roles_AgregarFamilia", new Dictionary<string, object>
            {
                { "@IdRol",     idRol     },
                { "@IdFamilia", idFamilia }
            });
        }

        public void QuitarFamilia(string idRol, string idFamilia)
        {
            EjecutarSPNonQuery("sp_Roles_QuitarFamilia", new Dictionary<string, object>
            {
                { "@IdRol",     idRol     },
                { "@IdFamilia", idFamilia }
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
