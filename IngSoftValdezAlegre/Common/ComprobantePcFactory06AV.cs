using BE;
using SER;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

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

        /// <summary>Recibo por la seña recibida. Devuelve la ruta del PDF generado.</summary>
        public static string GenerarReciboSena(OrdenProduccion06AV orden, decimal sena)
        {
            var t = GestorIdioma06AV.Instancia;
            var lineas = Cabecera(orden);
            lineas.Add("");
            lineas.Add($"{t.Obtener("pcf_total")}: {Money(orden.PrecioTotal)}");
            lineas.Add($"{t.Obtener("pcf_sena_recibida")}: {Money(sena)}");
            lineas.Add($"{t.Obtener("pcf_saldo_pendiente")}: {Money(orden.PrecioTotal - sena)}");

            string ruta = Path.Combine(CarpetaComprobantes(),
                $"Recibo_Orden_{orden.NumeroOrden}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            PdfSimple06AV.Guardar(ruta, t.Obtener("pcf_recibo_titulo"), lineas);
            return ruta;
        }

        /// <summary>Factura final al entregar la orden. Devuelve la ruta del PDF generado.</summary>
        public static string GenerarFactura(OrdenProduccion06AV orden)
        {
            var t = GestorIdioma06AV.Instancia;
            decimal sena = orden.Pagos.Where(p => p.Tipo == TipoPago06AV.Sena).Sum(p => p.Monto);
            decimal saldo = orden.Pagos.Where(p => p.Tipo == TipoPago06AV.SaldoFinal).Sum(p => p.Monto);

            var lineas = Cabecera(orden);
            lineas.Add("");
            lineas.Add($"{t.Obtener("pcf_total")}: {Money(orden.PrecioTotal)}");
            lineas.Add($"{t.Obtener("pcf_sena")}: {Money(sena)}");
            lineas.Add($"{t.Obtener("pcf_saldo_final")}: {Money(saldo)}");
            lineas.Add($"{t.Obtener("pcf_total_pagado")}: {Money(orden.TotalAbonado)}");

            string ruta = Path.Combine(CarpetaComprobantes(),
                $"Factura_Orden_{orden.NumeroOrden}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            PdfSimple06AV.Guardar(ruta, t.Obtener("pcf_factura_titulo"), lineas);
            return ruta;
        }

        /// <summary>Abre el PDF con la aplicación por defecto del sistema.</summary>
        public static void Abrir(string ruta)
        {
            try { Process.Start(ruta); } catch { /* si no hay visor asociado, no rompemos el flujo */ }
        }

        private static List<string> Cabecera(OrdenProduccion06AV orden)
        {
            var t = GestorIdioma06AV.Instancia;
            var l = new List<string>
            {
                "PC Forge - PC Factory",
                $"{t.Obtener("pcf_fecha")}: {DateTime.Now:dd/MM/yyyy HH:mm}",
                $"{t.Obtener("pcf_orden")} #{orden.NumeroOrden}",
                ""
            };
            if (orden.Cliente != null)
                l.Add($"{t.Obtener("pcf_cliente")}: {orden.Cliente.Nombre} {orden.Cliente.Apellido} (DNI {orden.Cliente.Dni})");
            if (orden.Computadora != null)
            {
                l.Add($"{orden.Computadora.Nombre} ({orden.Computadora.TipoConfiguracion})");
                l.Add($"{t.Obtener("pcf_componentes")}:");
                foreach (var c in orden.Computadora.Componentes)
                    l.Add($"   - {c.Descripcion} ({c.Marca} {c.Modelo})   {Money(c.PrecioUnitario)}");
            }
            return l;
        }

        private static string Money(decimal v) => "$" + v.ToString("N2", CultureInfo.InvariantCulture);
    }
}
