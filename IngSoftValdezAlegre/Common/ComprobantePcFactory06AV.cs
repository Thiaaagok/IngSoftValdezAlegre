using BE;
using SER;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Common
{
    /// <summary>
    /// Arma los comprobantes de PC Factory (RFN1): recibo de seña (paso 3) y factura
    /// al entregar (paso 6). Genera un PDF con <see cref="PdfSimple06AV"/> en
    /// Documentos\PC Factory\Comprobantes y devuelve la ruta del archivo.
    /// </summary>
    public static class ComprobantePcFactory06AV
    {
        private static string CarpetaComprobantes()
        {
            string documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string carpeta = Path.Combine(documentos, "PC Factory", "Comprobantes");
            if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);
            return carpeta;
        }

        /// <summary>
        /// CU03: recibo por la seña recibida. Incluye datos del cliente, de la computadora,
        /// forma de pago, monto abonado, saldo pendiente y fecha estimada de entrega.
        /// </summary>
        public static string GenerarReciboSena(Venta06AV venta, Pago06AV sena)
        {
            if (venta == null) throw new ArgumentNullException(nameof(venta));
            var t = GestorIdioma06AV.Instancia;

            decimal monto = sena != null ? sena.Monto : venta.MontoSenaRequerido;

            var lineas = Cabecera(venta, sena?.NumeroRecibo);
            lineas.Add("");
            lineas.Add($"{t.Obtener("pcf_total")}: {Money(venta.PrecioTotal)}");
            lineas.Add($"{t.Obtener("pcf_sena_recibida")}: {Money(monto)}");
            if (sena != null)
            {
                lineas.Add($"{t.Obtener("pcf_forma_pago")}: {TextoFormaPago(sena.FormaPago)}");
                if (!string.IsNullOrWhiteSpace(sena.Referencia))
                    lineas.Add($"{t.Obtener("pcf_referencia")}: {sena.Referencia}");
            }
            lineas.Add($"{t.Obtener("pcf_saldo_pendiente")}: {Money(venta.PrecioTotal - monto)}");
            lineas.Add($"{t.Obtener("pcf_f_entrega")}: {venta.FechaEntregaEstimada:dd/MM/yyyy}");
            if (sena != null && !string.IsNullOrWhiteSpace(sena.Usuario))
                lineas.Add($"{t.Obtener("pcf_atendido_por")}: {sena.Usuario}");

            string nro = sena?.NumeroRecibo;
            string ruta = Path.Combine(CarpetaComprobantes(),
                $"Recibo_{(string.IsNullOrWhiteSpace(nro) ? "Venta_" + venta.NumeroVenta : nro)}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            PdfSimple06AV.Guardar(ruta, t.Obtener("pcf_recibo_titulo"), lineas);
            return ruta;
        }

        /// <summary>
        /// CU07: factura final al entregar. Detalla el anticipo, el saldo cancelado,
        /// el número de serie del equipo y la fecha de entrega.
        /// </summary>
        public static string GenerarFactura(Venta06AV venta, OrdenProduccion06AV orden = null)
        {
            if (venta == null) throw new ArgumentNullException(nameof(venta));
            var t = GestorIdioma06AV.Instancia;

            var sena = venta.Pagos.FirstOrDefault(p => p.Tipo == TipoPago06AV.Sena);
            var saldo = venta.Pagos.FirstOrDefault(p => p.Tipo == TipoPago06AV.SaldoFinal);

            var lineas = Cabecera(venta, saldo?.NumeroRecibo);
            lineas.Add("");

            if (orden != null)
            {
                lineas.Add($"{t.Obtener("pcf_orden")} #{orden.NumeroOrden}");
                if (!string.IsNullOrWhiteSpace(orden.NumeroSerie))
                    lineas.Add($"{t.Obtener("pcf_nro_serie")}: {orden.NumeroSerie}");
                if (orden.FechaCierre.HasValue)
                    lineas.Add($"{t.Obtener("pcf_f_entrega")}: {orden.FechaCierre.Value:dd/MM/yyyy}");
                lineas.Add("");
            }

            lineas.Add($"{t.Obtener("pcf_total")}: {Money(venta.PrecioTotal)}");
            lineas.Add($"{t.Obtener("pcf_sena")}: {Money(sena?.Monto ?? 0m)}" +
                       (sena != null ? $"   ({TextoFormaPago(sena.FormaPago)}, {sena.NumeroRecibo})" : ""));
            lineas.Add($"{t.Obtener("pcf_saldo_final")}: {Money(saldo?.Monto ?? 0m)}" +
                       (saldo != null ? $"   ({TextoFormaPago(saldo.FormaPago)})" : ""));
            lineas.Add($"{t.Obtener("pcf_total_pagado")}: {Money(venta.TotalAbonado)}");
            lineas.Add($"{t.Obtener("pcf_saldo_pendiente")}: {Money(venta.SaldoPendiente)}");

            string nro = saldo?.NumeroRecibo;
            string ruta = Path.Combine(CarpetaComprobantes(),
                $"Factura_{(string.IsNullOrWhiteSpace(nro) ? "Venta_" + venta.NumeroVenta : nro)}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            PdfSimple06AV.Guardar(ruta, t.Obtener("pcf_factura_titulo"), lineas);
            return ruta;
        }

        /// <summary>Abre el PDF con la aplicación por defecto del sistema.</summary>
        public static void Abrir(string ruta)
        {
            try { Process.Start(ruta); } catch { /* si no hay visor asociado, no rompemos el flujo */ }
        }

        /// <summary>
        /// Avisa que el comprobante quedó generado y pregunta si se quiere imprimir.
        ///   Sí → abre el PDF con el visor del sistema.
        ///   No → el archivo queda guardado y el usuario sigue en la pantalla del sistema.
        /// Devuelve true si se abrió el PDF.
        /// </summary>
        /// <param name="ruta">Ruta del PDF recién generado.</param>
        /// <param name="esFactura">true = factura (CU07); false = recibo de seña (CU03).</param>
        /// <param name="owner">Ventana propietaria del diálogo.</param>
        /// <param name="encabezado">Texto opcional que se muestra antes de la pregunta.</param>
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
                default: return t.Obtener("pcf_fp_efectivo");
            }
        }

        private static List<string> Cabecera(Venta06AV venta, string numeroComprobante)
        {
            var t = GestorIdioma06AV.Instancia;
            var l = new List<string>
            {
                "PC Forge - PC Factory",
                $"{t.Obtener("pcf_fecha")}: {DateTime.Now:dd/MM/yyyy HH:mm}"
            };

            if (!string.IsNullOrWhiteSpace(numeroComprobante))
                l.Add($"{t.Obtener("pcf_comprobante")}: {numeroComprobante}");

            l.Add($"{t.Obtener("pcf_venta")} #{venta.NumeroVenta}");
            l.Add("");

            if (venta.Cliente != null)
                l.Add($"{t.Obtener("pcf_cliente")}: {venta.Cliente.Nombre} {venta.Cliente.Apellido} (DNI {venta.Cliente.Dni})");

            if (venta.Computadora != null)
            {
                l.Add($"{venta.Computadora.Nombre} ({venta.Computadora.TipoConfiguracion})");
                l.Add($"{t.Obtener("pcf_componentes")}:");
                foreach (var g in venta.Computadora.Componentes.GroupBy(c => c.Codigo))
                {
                    var c = g.First();
                    string cant = g.Count() > 1 ? $"{g.Count()} x " : "";
                    l.Add($"   - {cant}{c.Descripcion} ({c.Marca} {c.Modelo})   {Money(c.PrecioUnitario * g.Count())}");
                }
            }
            return l;
        }

        private static string Money(decimal v) => "$" + v.ToString("N2", CultureInfo.InvariantCulture);
    }
}
