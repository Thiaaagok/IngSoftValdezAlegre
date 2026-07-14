using MPP;
using SER.Integridad;

namespace BLL
{
    /// <summary>
    /// Fachada de negocio del Dígito Verificador (DV) según el diagrama de clases.
    /// Expone generar hash, calcular/actualizar DVH-DVV y verificar, reutilizando
    /// <see cref="DVMPP_06AV"/> y la verificación real de <see cref="IntegridadBLL06AV"/>.
    /// Es una vista "DV" sobre la lógica de integridad existente: no cambia el comportamiento
    /// del sistema (el login sigue usando IntegridadBLL06AV.Verificar()).
    /// </summary>
    public class DVBLL_06AV
    {
        private readonly DVMPP_06AV _mpp = new DVMPP_06AV();
        private readonly ICalculadorDigito06AV _calculador = new CalculadorHexadecimal06AV();

        /// <summary>Dígito/hash de una cadena, según la estrategia de cálculo vigente.</summary>
        public string GenerarHash(string datos) => _calculador.Calcular(datos);

        /// <summary>Calcula el DVH actual de una tabla (hexadecimal), sin guardarlo.</summary>
        public string CalcularDVH(string tabla)
        {
            _mpp.CalcularDVHyDVV(tabla, out string dvh, out _);
            return dvh;
        }

        /// <summary>Calcula el DVV actual de una tabla (hexadecimal), sin guardarlo.</summary>
        public string CalcularDVV(string tabla)
        {
            _mpp.CalcularDVHyDVV(tabla, out _, out string dvv);
            return dvv;
        }

        /// <summary>Recalcula y guarda el DVH y el DVV de una tabla.</summary>
        public void RecalcularDigitos(string tabla)
        {
            _mpp.CalcularDVHyDVV(tabla, out string dvh, out string dvv);
            _mpp.GuardarDVH(tabla, dvh);
            _mpp.GuardarDVV(tabla, dvv);
        }

        /// <summary>Recalcula y guarda el DVH de todas las tablas protegidas.</summary>
        public void ActualizarDVH()
        {
            foreach (string t in _mpp.TablasProtegidas())
                try { _mpp.CalcularDVHyDVV(t, out string dvh, out _); _mpp.GuardarDVH(t, dvh); }
                catch { /* si una tabla protegida aún no existe, se saltea */ }
        }

        /// <summary>Recalcula y guarda el DVV de todas las tablas protegidas.</summary>
        public void ActualizarDVV()
        {
            foreach (string t in _mpp.TablasProtegidas())
                try { _mpp.CalcularDVHyDVV(t, out _, out string dvv); _mpp.GuardarDVV(t, dvv); }
                catch { /* idem */ }
        }

        /// <summary>Compara el DVH recalculado de una tabla contra el almacenado.</summary>
        public bool VerificarDVH(string tabla) => CalcularDVH(tabla) == _mpp.ObtenerDVH(tabla);

        /// <summary>Compara el DVV recalculado de una tabla contra el almacenado.</summary>
        public bool VerificarDVV(string tabla) => CalcularDVV(tabla) == _mpp.ObtenerDVV(tabla);

        /// <summary>Verifica la integridad de toda la base (delega en la verificación real).</summary>
        public bool VerificarIntegridad() => new IntegridadBLL06AV().Verificar().EsConsistente;
    }
}
