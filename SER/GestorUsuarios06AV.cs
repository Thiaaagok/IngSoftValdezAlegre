using BE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPP;
namespace SER
{
    public class GestorUsuarios06AV
    {
        public List<Usuario06AV> ObtenerTodos()
        {
          
            try
            {
                List<Usuario06AV> Usuarios = new List<Usuario06AV>();
                MPP_Usuarios UsuariosMPP = new MPP_Usuarios();
                Usuarios = UsuariosMPP.ObtenerTodos();
                return Usuarios;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener usuarios", ex);
            }
        }

        public Usuario06AV ObtenerPorDni(string dni)
        {
            
            try
            {
                Usuario06AV usuario = new Usuario06AV();
                MPP_Usuarios UsuariosMPP = new MPP_Usuarios();
                usuario = UsuariosMPP.ObtenerPorDni(dni);
                return usuario;
            }
            catch (Exception ex)
            {
                throw new Exception("Error obteniendo el usuario por dni", ex);
            }
        }

        public bool CrearUsuario(string dni, string nombre, string apellido, string email, string rol)
        {
            try
            {
                Usuario06AV usuario = new Usuario06AV();
                usuario.Dni = dni;
                usuario.Nombre = nombre;
                usuario.Apellido = apellido;
                usuario.Email = email;
                usuario.Rol = rol;
                usuario.Activo = true;
                usuario.Bloqueado = false;
                usuario.Login = setearLogin(dni, nombre);
                usuario.Contrasenia = setearContrasenia(dni, apellido);

                MPP_Usuarios UsuariosMPP = new MPP_Usuarios();
                bool resultado = UsuariosMPP.CrearUsuario(usuario);
                return resultado;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al crear el usuario", ex);
            }
        }

        public string setearLogin(string dni, string nombre)
        {
            return $"{dni}{nombre}";
        }

        public string setearContrasenia(string dni, string apellido)
        {
            //Encriptador
            return $"{dni}{apellido}";
        }

        public bool EditarUsuario(Usuario06AV usuario)
        {
            try
            {
                MPP_Usuarios UsuariosMPP = new MPP_Usuarios();
                bool resultado = UsuariosMPP.EditarUsuario(usuario); 
                return resultado;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al editar al usuario {usuario.Dni}", ex);
            }
        }

        public bool EliminarUsuario(string dni)
        {
            try
            {
                MPP_Usuarios UsuariosMPP = new MPP_Usuarios();
                bool resultado = UsuariosMPP.EliminarUsuario(dni); 
                return resultado;
            }
            catch(Exception ex)
            {
                throw new Exception($"Error al eliminar al usuario {dni}", ex);
            }
        }
        
        public bool ReactivarUsuario(string dni)
        {
            try
            {
                MPP_Usuarios UsuariosMPP = new MPP_Usuarios();
                bool resultado = UsuariosMPP.ReactivarUsuario(dni); 
                return resultado;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al reactivar al usuario {dni}", ex);
            }
        }

        public bool BloquearUsuario(string dni)
        {
            try
            {
                MPP_Usuarios UsuariosMPP = new MPP_Usuarios();
                bool resultado = UsuariosMPP.BloquearUsuario(dni); 
                return resultado;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al bloquear al usuario {dni}", ex);
            }
        }

        public bool DesbloquearUsuario(string dni)
        {
            try
            {
                MPP_Usuarios UsuariosMPP = new MPP_Usuarios();
                bool resultado = UsuariosMPP.DesbloquearUsuario(dni); 
                return resultado;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al desbloquear al usuario {dni}", ex);
            }
        }

        public bool CambiarContraseña(string dni, string contraseniaActual, string contraseniaNueva)
        {
            try
            {
                MPP_Usuarios UsuariosMPP = new MPP_Usuarios();
                bool resultado = UsuariosMPP.CambiarContraseña(dni,contraseniaActual,contraseniaNueva); 
                return resultado;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al cambiar la contraseña al usuario {dni}", ex);
            }
        }
    }
}
