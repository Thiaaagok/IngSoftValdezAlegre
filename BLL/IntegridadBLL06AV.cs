using MPP;
using SER.Integridad;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BLL
{
    public class IntegridadBLL06AV
    {
        private readonly IntegridadMPP06AV _mpp = new IntegridadMPP06AV();

        /// <summary>
        /// Carpeta por defecto donde se guardan los backups (manuales y automáticos).
        /// El nombre del sistema es "GestionUsuario", por eso la subcarpeta.
        /// </summary>
        public const string CarpetaBackupPorDefecto = @"C:\Backups\GestionUsuario";

        /// <summary>Extensión estándar de los archivos de backup de SQL Server.</summary>
        public const string ExtensionBackup = ".bak";

        public void Recalcular()
        {
            ObjetoDV06AV obj = _mpp.Generar();
            _mpp.Persistir(obj);
        }

        public void RecalcularSeguro()
        {
            try { Recalcular(); }
            catch { }
        }

        public void AsegurarLineaBase()
        {
            if (_mpp.ObtenerAlmacenado().Tablas.Count == 0)
                Recalcular();
        }

        public ResultadoVerificacion06AV Verificar()
        {
            var resultado = new ResultadoVerificacion06AV();

            ObjetoDV06AV generado = _mpp.Generar();
            ObjetoDV06AV almacenado = _mpp.ObtenerAlmacenado();

            if (almacenado.Tablas.Count == 0)
            {
                resultado.SinLineaBase = true;
                resultado.EsConsistente = true;
                return resultado;
            }

            bool consistente = true;

            foreach (DigitoTabla06AV g in generado.Tablas)
            {
                DigitoTabla06AV a = almacenado.Buscar(g.Tabla);
                if (a == null || a.DVH != g.DVH || a.DVV != g.DVV)
                {
                    consistente = false;
                    resultado.TablasInconsistentes.Add(g.Tabla);
                    resultado.Detalles.Add(a == null
                        ? $"{g.Tabla}: sin dígito almacenado."
                        : $"{g.Tabla}: DVH {a.DVHHex} → {g.DVHHex}, DVV {a.DVVHex} → {g.DVVHex}.");
                }
            }

            foreach (DigitoTabla06AV a in almacenado.Tablas)
            {
                if (generado.Buscar(a.Tabla) == null)
                {
                    consistente = false;
                    resultado.TablasInconsistentes.Add(a.Tabla);
                    resultado.Detalles.Add($"{a.Tabla}: figura en DV pero no se pudo recalcular.");
                }
            }

            resultado.EsConsistente = consistente;
            return resultado;
        }

        // ─────────────────────────────────────────────────────────────
        //  BACKUP / RESTORE
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Devuelve (creándola si hace falta) la carpeta por defecto para los backups.
        /// </summary>
        public string ObtenerCarpetaBackupPorDefecto()
        {
            if (!Directory.Exists(CarpetaBackupPorDefecto))
                Directory.CreateDirectory(CarpetaBackupPorDefecto);
            return CarpetaBackupPorDefecto;
        }

        /// <summary>
        /// Genera un nombre de archivo de backup en el que se lee fácilmente la fecha
        /// y hora de creación. Ej.: "GestionUsuario_Backup_2026-07-08_23-45-10.bak".
        /// </summary>
        public string GenerarNombreArchivoBackup(DateTime? momento = null)
        {
            DateTime m = momento ?? DateTime.Now;
            return $"GestionUsuario_Backup_{m:yyyy-MM-dd_HH-mm-ss}{ExtensionBackup}";
        }

        /// <summary>Ruta completa por defecto (carpeta por defecto + nombre con fecha/hora).</summary>
        public string GenerarRutaBackupPorDefecto()
        {
            return Path.Combine(ObtenerCarpetaBackupPorDefecto(), GenerarNombreArchivoBackup());
        }

        /// <summary>
        /// Realiza un backup en la ruta indicada. Se asegura de que la carpeta destino
        /// exista y de que el archivo tenga extensión .bak. Devuelve la ruta usada.
        /// </summary>
        public string Respaldar(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
                throw new ArgumentException("Debe indicarse una ruta de destino para el backup.");

            ruta = NormalizarRutaBackup(ruta);

            string carpeta = Path.GetDirectoryName(ruta);
            if (!string.IsNullOrEmpty(carpeta) && !Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            _mpp.Respaldar(ruta);
            return ruta;
        }

        /// <summary>
        /// Backup "de un solo clic": lo guarda en la carpeta por defecto con un nombre
        /// que incluye fecha y hora. Devuelve la ruta del archivo generado.
        /// </summary>
        public string RespaldarEnCarpetaPorDefecto()
        {
            string ruta = GenerarRutaBackupPorDefecto();
            _mpp.Respaldar(ruta);
            return ruta;
        }

        /// <summary>
        /// Restaura la base desde el backup indicado. NO se puede restaurar si no se
        /// eligió un archivo o si el archivo no existe: en ese caso se lanza excepción.
        /// El archivo puede estar en cualquier ubicación de la computadora.
        /// </summary>
        public void Restaurar(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
                throw new InvalidOperationException(
                    "No se puede restaurar: no se seleccionó ningún archivo de backup.");

            if (!File.Exists(ruta))
                throw new FileNotFoundException(
                    "No se puede restaurar: el archivo de backup indicado no existe.", ruta);

            _mpp.Restaurar(ruta);
        }

        /// <summary>
        /// Lista los backups (.bak) existentes en la carpeta indicada (o la de defecto),
        /// del más nuevo al más viejo. Sirve para saber si hay algún backup disponible.
        /// </summary>
        public IList<InfoBackup06AV> ListarBackups(string carpeta = null)
        {
            carpeta = string.IsNullOrWhiteSpace(carpeta) ? CarpetaBackupPorDefecto : carpeta;

            if (!Directory.Exists(carpeta))
                return new List<InfoBackup06AV>();

            return Directory.GetFiles(carpeta, "*" + ExtensionBackup)
                .Select(f => new FileInfo(f))
                .OrderByDescending(fi => fi.LastWriteTime)
                .Select(fi => new InfoBackup06AV
                {
                    Ruta = fi.FullName,
                    Nombre = fi.Name,
                    Fecha = fi.LastWriteTime,
                    TamanioBytes = fi.Length
                })
                .ToList();
        }

        /// <summary>True si existe al menos un backup en la carpeta indicada (o la de defecto).</summary>
        public bool HayBackupsDisponibles(string carpeta = null)
        {
            return ListarBackups(carpeta).Count > 0;
        }

        private static string NormalizarRutaBackup(string ruta)
        {
            if (!ruta.EndsWith(ExtensionBackup, StringComparison.OrdinalIgnoreCase))
                ruta += ExtensionBackup;
            return ruta;
        }
    }
}
