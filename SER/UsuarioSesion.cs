using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace BE
{
    public class UsuarioSesion
    {
        private static UsuarioSesion _Instancia;
            
        public Guid Id { get; set; }
        // Falta pasar por proceso de encriptación
        public string Contrasenia { get; set; }
        public string Usuario { get; set; }
        public Guid Rol { get; set; }
        public string Idioma { get; set; }

        private UsuarioSesion()
        {

        }

        public static UsuarioSesion Instancia()
        {
            if( _Instancia == null)
            {
                _Instancia = new UsuarioSesion();
            }
            return _Instancia;
        }
    }
}
