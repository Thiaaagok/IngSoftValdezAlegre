using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BE
{
    public class Bitacora06AV
    {
        public int Codigo { get; set; }
        public int Categoria { get; set; }
        public string Criticidad { get; set; }
        public string Descripcion { get; set; }
        public DateTime Fecha { get; set; }
        public Guid Id { get; set; }
        public string Modulo { get; set; }
        public string UsuarioDni { get; set; }
    }
}
