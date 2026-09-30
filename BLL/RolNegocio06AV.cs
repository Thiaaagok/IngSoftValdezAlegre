using BE;
using BLL.Excepciones;
using SER;

namespace BLL
{
    internal static class RolNegocio06AV
    {
        public static void Exigir(Usuario06AV usuario, RolUsuario06AV rol, string accion)
        {
            if (usuario == null)
                throw new ValidacionException06AV("usuario", $"Se requiere un usuario autenticado para {accion}.");
            if (!Coincide(usuario, rol))
                throw new ValidacionException06AV("rol",
                    $"El usuario '{usuario.Login}' no tiene el rol {rol} requerido para {accion}.");
        }

        private static bool Coincide(Usuario06AV u, RolUsuario06AV rol)
        {
            string d = Normalizar(u.RolDescripcion) + "|" + Normalizar(u.IdRol);
            switch (rol)
            {
                case RolUsuario06AV.Repositor:
                    return d.Contains("repositor");
                case RolUsuario06AV.GerenteCompras:
                    return d.Contains("gerentecompras") || d.Contains("gerentedecompras")
                        || (d.Contains("gerente") && d.Contains("compra"));
                case RolUsuario06AV.Almacenista:
                    return d.Contains("almacen");
                case RolUsuario06AV.Recepcionista:
                    return d.Contains("recepcion");
                case RolUsuario06AV.Gerente:
                    return d.Contains("gerente");
                default:
                    return false;
            }
        }

        private static string Normalizar(string s) =>
            string.IsNullOrEmpty(s) ? "" : s.Trim().ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
    }
}
