using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>
    /// Acceso a datos del Dígito Verificador. El DVH/DVV de cada tabla se guarda en
    /// una tabla central "DV" (Tabla, DVH, DVV), sin alterar las tablas de negocio.
    /// </summary>
    public class IntegridadDAL06AV
    {
        /// <summary>
        /// Tablas cuya integridad se controla. Se excluyen los logs (Bitacora,
        /// IntentosLogin) porque cambian constantemente. También actúa como whitelist
        /// para evitar inyección por nombre de tabla.
        /// </summary>
        public static readonly string[] TablasProtegidas =
        {
            // ── Seguridad / permisos ─────────────────────────────
            "Usuarios",
            "Roles",
            "Familias",
            "Patentes",
            "RolPatentes",
            "RolFamilias",
            "FamiliaPatentes",
            "FamiliaFamilias",

            // ── PC Factory: datos maestros y transaccionales ─────
            "Clientes",
            "Componentes",
            "Insumos",
            "Proveedores",
            "LineasEnsamblaje",
            "Computadoras",
            "ComputadoraComponentes",
            "OrdenesProduccion",
            "Pagos",
            "OrdenesCompra",
            "OrdenCompraDetalle",
            "PedidosCotizacion",
            "ModelosEstandar",
            "ModeloEstandarComponentes"
        };

        public void AsegurarEstructura()
        {
            const string sql = @"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DV')
                CREATE TABLE DV (
                    Tabla NVARCHAR(128) NOT NULL PRIMARY KEY,
                    DVH   NVARCHAR(64)  NOT NULL,
                    DVV   NVARCHAR(64)  NOT NULL
                );";

            using (SqlConnection conn = Conexion.Instancia.ObtenerConexion())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public DataTable ObtenerContenido(string tabla)
        {
            if (!EsTablaProtegida(tabla))
                throw new ArgumentException($"'{tabla}' no es una tabla protegida válida.");

            string query = $"SELECT * FROM [{tabla}]";

            using (SqlConnection conn = Conexion.Instancia.ObtenerConexion())
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                DataTable tablaDatos = new DataTable();
                conn.Open();
                new SqlDataAdapter(cmd).Fill(tablaDatos);
                return tablaDatos;
            }
        }

        private static bool EsTablaProtegida(string tabla)
        {
            foreach (string t in TablasProtegidas)
                if (string.Equals(t, tabla, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        public DataTable ObtenerDV()
        {
            AsegurarEstructura();
            using (SqlConnection conn = Conexion.Instancia.ObtenerConexion())
            using (SqlCommand cmd = new SqlCommand("SELECT Tabla, DVH, DVV FROM DV", conn))
            {
                DataTable tabla = new DataTable();
                conn.Open();
                new SqlDataAdapter(cmd).Fill(tabla);
                return tabla;
            }
        }

        /// <summary>Reescribe por completo la tabla DV dentro de una transacción.</summary>
        public void GuardarDV(IEnumerable<KeyValuePair<string, string[]>> digitosPorTabla)
        {
            AsegurarEstructura();

            using (SqlConnection conn = Conexion.Instancia.ObtenerConexion())
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand del = new SqlCommand("DELETE FROM DV", conn, tx))
                            del.ExecuteNonQuery();

                        foreach (var par in digitosPorTabla)
                        {
                            using (SqlCommand ins = new SqlCommand(
                                "INSERT INTO DV (Tabla, DVH, DVV) VALUES (@tabla, @dvh, @dvv)", conn, tx))
                            {
                                ins.Parameters.AddWithValue("@tabla", par.Key);
                                ins.Parameters.AddWithValue("@dvh", par.Value[0]);
                                ins.Parameters.AddWithValue("@dvv", par.Value[1]);
                                ins.ExecuteNonQuery();
                            }
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public void Respaldar(string ruta)
        {
            string db = NombreBaseDatos();
            string sql = $"BACKUP DATABASE [{db}] TO DISK = @ruta WITH INIT, FORMAT";

            using (SqlConnection conn = Conexion.Instancia.ObtenerConexion())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandTimeout = 0;
                cmd.Parameters.AddWithValue("@ruta", ruta);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Restaura la base desde un backup. Se conecta a 'master', pone la base en
        /// SINGLE_USER, restaura con REPLACE y vuelve a MULTI_USER aún si falla el restore.
        /// </summary>
        public void Restaurar(string ruta)
        {
            string db = NombreBaseDatos();

            using (SqlConnection conn = new SqlConnection(CadenaConexionMaster()))
            {
                conn.Open();

                Ejecutar(conn, $"ALTER DATABASE [{db}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
                try
                {
                    Ejecutar(conn, $"RESTORE DATABASE [{db}] FROM DISK = @ruta WITH REPLACE", ruta);
                }
                finally
                {
                    Ejecutar(conn, $"ALTER DATABASE [{db}] SET MULTI_USER");
                }
            }
        }

        private static void Ejecutar(SqlConnection conn, string sql, string ruta = null)
        {
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandTimeout = 0;
                if (ruta != null)
                    cmd.Parameters.AddWithValue("@ruta", ruta);
                cmd.ExecuteNonQuery();
            }
        }

        private static string NombreBaseDatos()
        {
            var b = new SqlConnectionStringBuilder(Conexion.Instancia.connectionString);
            return string.IsNullOrEmpty(b.InitialCatalog) ? "IngSoftValdezAlegre" : b.InitialCatalog;
        }

        private static string CadenaConexionMaster()
        {
            var b = new SqlConnectionStringBuilder(Conexion.Instancia.connectionString)
            {
                InitialCatalog = "master"
            };
            return b.ConnectionString;
        }
    }
}
