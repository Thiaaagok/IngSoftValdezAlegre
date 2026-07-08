using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Instalador
{
    /// <summary>
    /// Capa de acceso a datos del Instalador. Es autónoma (ADO.NET puro) para poder
    /// preparar la base ANTES de que exista, sin depender de la DAL de la aplicación.
    /// </summary>
    public class InstaladorDAL06AV
    {
        private readonly OpcionesInstalacion06AV _opciones;

        public InstaladorDAL06AV(OpcionesInstalacion06AV opciones)
        {
            _opciones = opciones ?? throw new ArgumentNullException(nameof(opciones));
        }

        /// <summary>
        /// Detecta instancias de SQL Server disponibles en la máquina/red para ofrecerlas
        /// en el asistente. Combina tres fuentes y nunca lanza excepción (si una falla,
        /// simplemente aporta menos resultados):
        ///   • LocalDB, listadas con la utilidad 'sqllocaldb info'.
        ///   • Instancias locales y de red, vía SqlDataSourceEnumerator.
        ///   • Valores comunes por defecto ('.' y '.\SQLEXPRESS').
        /// </summary>
        public static List<string> DetectarInstancias()
        {
            var lista = new List<string>();

            void Agregar(string s)
            {
                if (!string.IsNullOrWhiteSpace(s) &&
                    !lista.Contains(s.Trim(), StringComparer.OrdinalIgnoreCase))
                    lista.Add(s.Trim());
            }

            // 1) LocalDB (p. ej. la instancia automática "MSSQLLocalDB").
            try
            {
                var psi = new ProcessStartInfo("sqllocaldb", "info")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (Process p = Process.Start(psi))
                {
                    string salida = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(3000);
                    foreach (string linea in salida.Split('\n'))
                    {
                        string nombre = linea.Trim();
                        if (nombre.Length > 0)
                            Agregar("(localdb)\\" + nombre);
                    }
                }
            }
            catch { /* LocalDB puede no estar instalado */ }

            // 2) Instancias locales y de red visibles en el momento.
            try
            {
                DataTable dt = SqlDataSourceEnumerator.Instance.GetDataSources();
                foreach (DataRow r in dt.Rows)
                {
                    string servidor = r["ServerName"] as string;
                    string instancia = r["InstanceName"] as string;
                    if (string.IsNullOrEmpty(servidor)) continue;
                    Agregar(string.IsNullOrEmpty(instancia) ? servidor : servidor + "\\" + instancia);
                }
            }
            catch { /* el enumerador puede fallar o tardar; se ignora */ }

            // 3) Valores comunes por defecto, por si nada de lo anterior devolvió algo.
            Agregar(".");
            Agregar(".\\SQLEXPRESS");

            return lista;
        }

        /// <summary>Intenta abrir una conexión a 'master'. Lanza excepción si no puede.</summary>
        public void ProbarConexion()
        {
            using (var conn = new SqlConnection(_opciones.CadenaMaster()))
            {
                conn.Open();
            }
        }

        public bool ExisteBaseDatos()
        {
            using (var conn = new SqlConnection(_opciones.CadenaMaster()))
            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM sys.databases WHERE name = @nombre", conn))
            {
                cmd.Parameters.AddWithValue("@nombre", _opciones.BaseDatos);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        /// <summary>Crea la base de datos si todavía no existe.</summary>
        public void CrearBaseDatos()
        {
            if (ExisteBaseDatos()) return;

            // El nombre de la base no admite parámetros: se valida y se escapa.
            string db = ValidarNombreBase(_opciones.BaseDatos);

            using (var conn = new SqlConnection(_opciones.CadenaMaster()))
            using (var cmd = new SqlCommand($"CREATE DATABASE [{db}]", conn))
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Elimina la base (usado por la prueba del instalador para limpiar).</summary>
        public void EliminarBaseDatos()
        {
            if (!ExisteBaseDatos()) return;

            string db = ValidarNombreBase(_opciones.BaseDatos);
            string sql =
                $"ALTER DATABASE [{db}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{db}];";

            using (var conn = new SqlConnection(_opciones.CadenaMaster()))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Ejecuta un lote SQL contra la base de la aplicación. El texto se parte en
        /// sublotes separados por la palabra "GO" (como en SSMS), porque ADO.NET no
        /// entiende GO de forma nativa.
        /// </summary>
        public void EjecutarLote(string sqlBatch)
        {
            using (var conn = new SqlConnection(_opciones.CadenaBaseDatos()))
            {
                conn.Open();
                foreach (string sub in SepararPorGo(sqlBatch))
                {
                    if (string.IsNullOrWhiteSpace(sub)) continue;
                    using (var cmd = new SqlCommand(sub, conn))
                    {
                        cmd.CommandTimeout = 0;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public int ContarTablas()
        {
            using (var conn = new SqlConnection(_opciones.CadenaBaseDatos()))
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM sys.tables", conn))
            {
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public int ContarUsuarios()
        {
            using (var conn = new SqlConnection(_opciones.CadenaBaseDatos()))
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Usuarios", conn))
            {
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        /// <summary>Separa un script en sublotes usando líneas que contienen solo "GO".</summary>
        private static string[] SepararPorGo(string sql)
        {
            return Regex.Split(sql, @"^\s*GO\s*$",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);
        }

        /// <summary>Evita inyección por nombre de base: solo letras, números y _.</summary>
        private static string ValidarNombreBase(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre) || !Regex.IsMatch(nombre, @"^[A-Za-z0-9_]+$"))
                throw new ArgumentException($"Nombre de base de datos inválido: '{nombre}'.");
            return nombre;
        }
    }
}
