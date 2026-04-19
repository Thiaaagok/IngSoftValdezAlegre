using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BE
{
    public class Usuario
    {

        public Guid Id { get; set; }
        public string Login { get; set; }
        // Falta pasar por proceso de encriptación
        public string Contrasenia { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public bool Bloqueado { get; set; }
        public string Idioma { get; set; }
        public Guid Rol { get; set; }

    }
}
