using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Instalador
{
    public class InstaladorMPP06AV
    {
        private readonly OpcionesInstalacion06AV _opciones;
        private readonly InstaladorDAL06AV _dal;

        public InstaladorMPP06AV(OpcionesInstalacion06AV opciones)
        {
            _opciones = opciones ?? throw new ArgumentNullException(nameof(opciones));
            _dal = new InstaladorDAL06AV(opciones);
        }

        public void PrepararBaseDatos(Action<string> log)
        {
            log?.Invoke($"Verificando conexión al servidor '{_opciones.Servidor}'...");
            _dal.ProbarConexion();
            log?.Invoke("Conexión establecida.");

            if (_dal.ExisteBaseDatos())
            {
                log?.Invoke($"La base '{_opciones.BaseDatos}' ya existe: se usará la existente.");
            }
            else
            {
                log?.Invoke($"Creando base de datos '{_opciones.BaseDatos}'...");
                _dal.CrearBaseDatos();
                log?.Invoke("Base de datos creada.");
            }
        }

        public int EjecutarScripts(Action<string> log)
        {
            string carpeta = _opciones.CarpetaScripts;

            if (string.IsNullOrWhiteSpace(carpeta) || !Directory.Exists(carpeta))
                throw new DirectoryNotFoundException(
                    $"No se encontró la carpeta de scripts: '{carpeta}'.");

            List<string> archivos = Directory
                .GetFiles(carpeta, "*.sql")
                .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (archivos.Count == 0)
                throw new FileNotFoundException(
                    $"No hay scripts .sql en la carpeta: '{carpeta}'.");

            foreach (string archivo in archivos)
            {
                log?.Invoke($"Ejecutando {Path.GetFileName(archivo)}...");
                string sql = File.ReadAllText(archivo);
                _dal.EjecutarLote(sql);
            }

            return archivos.Count;
        }
    }
}
