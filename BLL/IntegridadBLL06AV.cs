using MPP;
using SER.Integridad;

namespace BLL
{
    public class IntegridadBLL06AV
    {
        private readonly IntegridadMPP06AV _mpp = new IntegridadMPP06AV();

        public void Recalcular()
        {
            ObjetoDV06AV obj = _mpp.Generar();
            _mpp.Persistir(obj);
        }

        public void RecalcularSeguro()
        {
            try { Recalcular(); }
            catch { }
        }

        public void AsegurarLineaBase()
        {
            if (_mpp.ObtenerAlmacenado().Tablas.Count == 0)
                Recalcular();
        }

        public ResultadoVerificacion06AV Verificar()
        {
            var resultado = new ResultadoVerificacion06AV();

            ObjetoDV06AV generado = _mpp.Generar();
            ObjetoDV06AV almacenado = _mpp.ObtenerAlmacenado();

            if (almacenado.Tablas.Count == 0)
            {
                resultado.SinLineaBase = true;
                resultado.EsConsistente = true;
                return resultado;
            }

            bool consistente = true;

            foreach (DigitoTabla06AV g in generado.Tablas)
            {
                DigitoTabla06AV a = almacenado.Buscar(g.Tabla);
                if (a == null || a.DVH != g.DVH || a.DVV != g.DVV)
                {
                    consistente = false;
                    resultado.TablasInconsistentes.Add(g.Tabla);
                    resultado.Detalles.Add(a == null
                        ? $"{g.Tabla}: sin dígito almacenado."
                        : $"{g.Tabla}: DVH {a.DVHHex} → {g.DVHHex}, DVV {a.DVVHex} → {g.DVVHex}.");
                }
            }

            foreach (DigitoTabla06AV a in almacenado.Tablas)
            {
                if (generado.Buscar(a.Tabla) == null)
                {
                    consistente = false;
                    resultado.TablasInconsistentes.Add(a.Tabla);
                    resultado.Detalles.Add($"{a.Tabla}: figura en DV pero no se pudo recalcular.");
                }
            }

            resultado.EsConsistente = consistente;
            return resultado;
        }

        public void Restaurar(string ruta) { _mpp.Restaurar(ruta); }

        public void Respaldar(string ruta) { _mpp.Respaldar(ruta); }
    }
}
