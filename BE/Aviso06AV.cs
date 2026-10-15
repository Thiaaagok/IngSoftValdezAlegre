using System.Collections.Generic;

namespace BE
{
    public class Aviso06AV
    {
        public TipoAviso06AV Tipo { get; set; }
        public int Cantidad { get; set; }
        public List<string> Detalle { get; set; } = new List<string>();
        public bool Urgente { get; set; }
    }
}
