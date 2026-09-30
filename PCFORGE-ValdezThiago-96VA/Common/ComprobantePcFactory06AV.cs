using BE;
using SER;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA.Common
{
    public static class ComprobantePcFactory06AV
    {
        private static string CarpetaComprobantes()
        {
            string documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string carpeta = Path.Combine(documentos, "PC Factory", "Comprobantes");
            if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);
            return carpeta;
        }

        public static string GenerarReciboSena(Venta06AV venta, Pago06AV sena)
        {
            if (venta == null) throw new ArgumentNullException(nameof(venta));

            decimal monto = sena != null ? sena.Monto : venta.MontoSenaRequerido;
            string nro = sena?.NumeroRecibo;
            string ruta = Path.Combine(CarpetaComprobantes(),
                $"Recibo_{(string.IsNullOrWhiteSpace(nro) ? "Venta_" + venta.NumeroVenta : nro)}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            ComprobantePdf06AV.GuardarRecibo(ruta, venta, sena, monto, nro, Logo());
            return ruta;
        }

        public static string GenerarFactura(Venta06AV venta, OrdenProduccion06AV orden = null)
        {
            if (venta == null) throw new ArgumentNullException(nameof(venta));

            var saldo = venta.Pagos.FirstOrDefault(p => p.Tipo == TipoPago06AV.SaldoFinal);
            string nro = saldo?.NumeroRecibo;
            string ruta = Path.Combine(CarpetaComprobantes(),
                $"Factura_{(string.IsNullOrWhiteSpace(nro) ? "Venta_" + venta.NumeroVenta : nro)}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            ComprobantePdf06AV.GuardarFactura(ruta, venta, orden, nro, Logo());
            return ruta;
        }

        private static System.Drawing.Image Logo()
        {
            try { return Properties.Resources.pcforge_logo1; }
            catch { return null; }
        }

        public static void Abrir(string ruta)
        {
            try { Process.Start(ruta); } catch {  }
        }

        public static bool PreguntarEImprimir(string ruta, bool esFactura,
                                              IWin32Window owner = null, string encabezado = null)
        {
            var t = GestorIdioma06AV.Instancia;

            string mensaje = string.IsNullOrWhiteSpace(encabezado) ? "" : encabezado.TrimEnd() + "\n\n";
            mensaje += (esFactura ? t.Obtener("pcf_factura_generada") : t.Obtener("pcf_recibo_generado")) + "\n" +
                       ruta + "\n\n" +
                       (esFactura ? t.Obtener("pcf_desea_imprimir_factura") : t.Obtener("pcf_desea_imprimir_recibo"));

            bool imprimir = ConfirmacionForm.Mostrar(
                mensaje,
                t.Obtener(esFactura ? "pcf_factura_titulo" : "pcf_recibo_titulo"),
                ConfirmacionForm.TipoConfirmacion.Pregunta,
                t.Obtener("pcf_imprimir"),
                t.Obtener("pcf_solo_guardar"),
                owner);

            if (imprimir) Abrir(ruta);
            return imprimir;
        }

        public static string TextoFormaPago(FormaPago06AV forma)
        {
            var t = GestorIdioma06AV.Instancia;
            switch (forma)
            {
                case FormaPago06AV.Transferencia: return t.Obtener("pcf_fp_transferencia");
                case FormaPago06AV.Tarjeta: return t.Obtener("pcf_fp_tarjeta");
                case FormaPago06AV.TarjetaDebito: return t.Obtener("pcf_fp_tarjeta_debito");
                default: return t.Obtener("pcf_fp_efectivo");
            }
        }
    }
}
