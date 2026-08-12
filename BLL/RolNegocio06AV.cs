using BE;
using BLL.Excepciones;
using SER;

namespace BLL
{
    /// <summary>
    /// Validación del rol de negocio (RFN1/RFN2) de un usuario para acciones sensibles
    /// del circuito de compras/ventas (p. ej. solo un Repositor registra una orden de
    /// compra, solo un Gerente de Compras aprueba una cotización).
    ///
    /// El rol se compara contra <see cref="Usuario06AV.RolDescripcion"/> e
    /// <see cref="Usuario06AV.IdRol"/> por palabra clave (case-insensitive, sin espacios).
    /// Es el ÚNICO punto a ajustar si en tu instalación los roles se llaman distinto.
    /// </summary>
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
