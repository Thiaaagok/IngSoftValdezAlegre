using System;

namespace SER.Integridad
{
    public class InfoBackup06AV
    {
        public string Ruta { get; set; }

        public string Nombre { get; set; }

        public DateTime Fecha { get; set; }

        public long TamanioBytes { get; set; }

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
