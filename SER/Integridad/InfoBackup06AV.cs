using System;

namespace SER.Integridad
{
    /// <summary>
    /// Información básica de un archivo de backup existente en disco. Se usa para
    /// listar los respaldos disponibles y para poder inferir su fecha/hora de creación.
    /// </summary>
    public class InfoBackup06AV
    {
        /// <summary>Ruta completa del archivo .bak.</summary>
        public string Ruta { get; set; }

        /// <summary>Nombre del archivo (incluye la fecha/hora codificada en el nombre).</summary>
        public string Nombre { get; set; }

        /// <summary>Fecha y hora de creación/modificación del backup.</summary>
        public DateTime Fecha { get; set; }

        /// <summary>Tamaño del archivo en bytes.</summary>
        public long TamanioBytes { get; set; }

        /// <summary>Tamaño formateado en forma legible (KB / MB).</summary>
        public string TamanioLegible
        {
            get
            {
                if (TamanioBytes >= 1024 * 1024)
                    return $"{TamanioBytes / (1024d * 1024d):0.0} MB";
                if (TamanioBytes >= 1024)
                    return $"{TamanioBytes / 1024d:0.0} KB";
                return $"{TamanioBytes} B";
            }
        }

        public override string ToString() => $"{Nombre} ({Fecha:dd/MM/yyyy HH:mm:ss})";
    }
}
