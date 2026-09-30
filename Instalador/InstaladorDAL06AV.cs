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
    public class InstaladorDAL06AV
    {
        private readonly OpcionesInstalacion06AV _opciones;

        public InstaladorDAL06AV(OpcionesInstalacion06AV opciones)
        {
            _opciones = opciones ?? throw new ArgumentNullException(nameof(opciones));
        }

        public static List<string> DetectarInstancias()
        {
            var lista = new List<string>();

            void Agregar(string s)
            {
                if (!string.IsNullOrWhiteSpace(s) &&
                    !lista.Contains(s.Trim(), StringComparer.OrdinalIgnoreCase))
                    lista.Add(s.Trim());
            }

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
            catch {  }

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
            catch {  }

            Agregar(".");
            Agregar(".\\SQLEXPRESS");

            return lista;
        }

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

        private static string[] SepararPorGo(string sql)
        {
            return Regex.Split(sql, @"^\s*GO\s*$",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);
        }

        private static string ValidarNombreBase(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre) || !Regex.IsMatch(nombre, @"^[A-Za-z0-9_]+$"))
                throw new ArgumentException($"Nombre de base de datos inválido: '{nombre}'.");
            return nombre;
        }
    }
}
