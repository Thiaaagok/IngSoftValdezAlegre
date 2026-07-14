using SER;
using System;

namespace BLL
{
    internal static class AuditoriaPcFactory06AV
    {
        public static void Alta(string entidad, ModuloBitacora modulo) =>
            Registrar(b => b.Alta(entidad, modulo, DniOperador()));

        public static void Modificacion(string entidad, ModuloBitacora modulo) =>
            Registrar(b => b.Modificacion(entidad, modulo, DniOperador()));

        public static void Baja(string entidad, ModuloBitacora modulo) =>
            Registrar(b => b.Baja(entidad, modulo, DniOperador()));

        private static string DniOperador() =>
            UsuarioSesion06AV.Instancia().UsuarioActual?.Dni;

        private static void Registrar(Action<BitacoraBLL06AV> accion)
        {
            try { new IntegridadBLL06AV().RecalcularSeguro(); } catch { }
            try { accion(new BitacoraBLL06AV()); } catch { }
        }
    }
}
