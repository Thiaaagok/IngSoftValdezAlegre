using DAL;
using SER.Integridad;
using System.Collections.Generic;
using System.Data;

namespace MPP
{
    /// <summary>
    /// Fachada de mapeo del Dígito Verificador (DV) según el diagrama de clases.
    /// Expone obtener registros de una tabla, leer/guardar DVH y DVV y calcularlos,
    /// reutilizando <see cref="DVDAL_06AV"/> para los datos y
    /// <see cref="MotorDigitoVerificador06AV"/> para el cálculo. No cambia el comportamiento.
    /// </summary>
    public class DVMPP_06AV
    {
        private readonly DVDAL_06AV _dal = new DVDAL_06AV();
        private readonly MotorDigitoVerificador06AV _motor = new MotorDigitoVerificador06AV();

        public DataTable ObtenerRegistros(string tabla) => _dal.ObtenerTabla(tabla);

        public string ObtenerDVH(string tabla) => _dal.ObtenerDVH(tabla);
        public string ObtenerDVV(string tabla) => _dal.ObtenerDVV(tabla);

        public void GuardarDVH(string tabla, string dvh) => _dal.GuardarDVH(tabla, dvh);
        public void GuardarDVV(string tabla, string dvv) => _dal.GuardarDVV(tabla, dvv);

        /// <summary>Calcula el DVH y el DVV de una tabla (en hexadecimal) a partir de su contenido.</summary>
        public void CalcularDVHyDVV(string tabla, out string dvh, out string dvv)
        {
            _motor.Calcular(ObtenerRegistros(tabla), out long h, out long v);
            dvh = h.ToString("X");
            dvv = v.ToString("X");
        }

        /// <summary>Lista de tablas cuyo dígito verificador se controla.</summary>
        public IReadOnlyList<string> TablasProtegidas() => IntegridadDAL06AV.TablasProtegidas;
    }
}
