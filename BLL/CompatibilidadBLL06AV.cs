using BE;
using BLL.Armado;
using BLL.Excepciones;
using MPP;
using SER;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    public class CompatibilidadBLL06AV
    {
        private readonly ReglasCompatibilidadMPP06AV _mpp = new ReglasCompatibilidadMPP06AV();

        public List<ReglaCompatibilidad06AV> ObtenerReglas()
        {
            try { return _mpp.ObtenerTodas(); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener las reglas de compatibilidad.", ex); }
        }

        public void Recargar()
        {
            try { ReglasCompatibilidad06AV.Configurar(_mpp.ObtenerTodas()); }
            catch { ReglasCompatibilidad06AV.Configurar(null); }
        }

        public void Guardar(ReglaCompatibilidad06AV regla)
        {
            ExigirPatente();
            Validar(regla);
            bool nueva = regla.Id <= 0;
            try { _mpp.Guardar(regla); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo guardar la regla de compatibilidad.", ex); }

            if (nueva) AuditoriaPcFactory06AV.Alta($"Regla de compatibilidad: {regla.Nombre}", ModuloBitacora.Componentes);
            else AuditoriaPcFactory06AV.Modificacion($"Regla de compatibilidad: {regla.Nombre}", ModuloBitacora.Componentes);
            Recargar();
        }

        public void Eliminar(ReglaCompatibilidad06AV regla)
        {
            ExigirPatente();
            if (regla == null) throw new ValidacionException06AV("regla", "Elegí una regla.");
            if (regla.Sistema)
                throw new ValidacionException06AV("Sistema", "Las reglas del sistema no se pueden eliminar: se pueden desactivar.");
            try { _mpp.Eliminar(regla.Id); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo eliminar la regla de compatibilidad.", ex); }

            AuditoriaPcFactory06AV.Baja($"Regla de compatibilidad: {regla.Nombre}", ModuloBitacora.Componentes);
            Recargar();
        }

        public static bool EsNumerico(AtributoCompatibilidad06AV a) =>
            a == AtributoCompatibilidad06AV.FrecuenciaMemoriaMhz || a == AtributoCompatibilidad06AV.CapacidadGb ||
            a == AtributoCompatibilidad06AV.SlotsMemoria || a == AtributoCompatibilidad06AV.MemoriaMaximaGb ||
            a == AtributoCompatibilidad06AV.ConsumoW || a == AtributoCompatibilidad06AV.PotenciaW ||
            a == AtributoCompatibilidad06AV.TdpSoportadoW;

        public static bool EsOrdenable(AtributoCompatibilidad06AV a) => EsNumerico(a) || a == AtributoCompatibilidad06AV.Formato;

        public static bool EsLogico(AtributoCompatibilidad06AV a) =>
            a == AtributoCompatibilidad06AV.GraficosIntegrados || a == AtributoCompatibilidad06AV.Desbloqueado ||
            a == AtributoCompatibilidad06AV.PermiteOverclock;

        private static void Validar(ReglaCompatibilidad06AV r)
        {
            if (r == null) throw new ValidacionException06AV("regla", "La regla no puede ser nula.");
            if (!Enum.IsDefined(typeof(NivelReglaCompatibilidad06AV), r.Nivel))
                throw new ValidacionException06AV("Nivel", "Elegí si la regla bloquea o advierte.");

            if (r.Sistema)
            {
                if (r.Codigo == "fuente_limite" && (!r.Valor.HasValue || r.Valor < 0 || r.Valor > 200))
                    throw new ValidacionException06AV("Valor", "El margen de la fuente debe estar entre 0 y 200 %.");
                if (r.Codigo == "ram_slots" && (!r.Valor.HasValue || r.Valor < 1 || r.Valor > 16 || r.Valor != Math.Floor(r.Valor.Value)))
                    throw new ValidacionException06AV("Valor", "Las ranuras por defecto deben ser un número entero entre 1 y 16.");
                return;
            }

            if (string.IsNullOrWhiteSpace(r.Nombre))
                throw new ValidacionException06AV("Nombre", "El nombre de la regla es obligatorio.");
            if (r.Nombre.Trim().Length > 150)
                throw new ValidacionException06AV("Nombre", "El nombre no puede superar los 150 caracteres.");
            if (!r.TipoA.HasValue || !r.TipoB.HasValue || !r.AtributoA.HasValue || !r.AtributoB.HasValue || !r.Operador.HasValue)
                throw new ValidacionException06AV("Condicion", "Completá las dos piezas, sus datos y la condición.");
            if (r.TipoA == r.TipoB && r.AtributoA == r.AtributoB)
                throw new ValidacionException06AV("Condicion", "La regla compara un dato consigo mismo.");

            OperadorCompatibilidad06AV op = r.Operador.Value;
            AtributoCompatibilidad06AV a = r.AtributoA.Value, b = r.AtributoB.Value;
            bool ordenar = op == OperadorCompatibilidad06AV.MenorOIgual || op == OperadorCompatibilidad06AV.MayorOIgual;
            if (ordenar && !(EsOrdenable(a) && EsOrdenable(b) && EsNumerico(a) == EsNumerico(b)))
                throw new ValidacionException06AV("Operador", "\"Menor o igual\" y \"mayor o igual\" sólo comparan números o formatos de placa.");
            if (op == OperadorCompatibilidad06AV.Contiene && (EsNumerico(a) || EsLogico(a)))
                throw new ValidacionException06AV("Operador", "\"Contiene\" sólo sirve para datos de texto (por ejemplo, una lista de sockets).");
            if (EsLogico(a) != EsLogico(b))
                throw new ValidacionException06AV("Operador", "Un dato sí/no sólo se puede comparar con otro dato sí/no.");
            if (r.Mensaje != null && r.Mensaje.Length > 300)
                throw new ValidacionException06AV("Mensaje", "El mensaje no puede superar los 300 caracteres.");
        }

        private static void ExigirPatente()
        {
            UsuarioSesion06AV sesion;
            try { sesion = UsuarioSesion06AV.Instancia(); }
            catch { return; }
            if (sesion?.UsuarioActual != null && !sesion.TienePermiso(PatenteEnum06AV.GestionarCompatibilidad))
                throw new ValidacionException06AV("permiso", "No tenés permiso para gestionar las reglas de compatibilidad.");
        }
    }
}
