using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace BE
{
    public class UsuarioSesion06AV
    {
        private static UsuarioSesion06AV _Instancia;

        public Usuario06AV Usuario { get; set; }

        private UsuarioSesion06AV()
        {

        }

        public static UsuarioSesion06AV Instancia()
        {
            if( _Instancia == null)
            {
                _Instancia = new UsuarioSesion06AV();
            }
            return _Instancia;
        }

        public void CrearUsuario(Usuario06AV usuario) 
        {
            Usuario = usuario;
        }

        public Usuario06AV ObtenerUsuario()
        {
            return _Instancia.Usuario;
        }
    }
}
