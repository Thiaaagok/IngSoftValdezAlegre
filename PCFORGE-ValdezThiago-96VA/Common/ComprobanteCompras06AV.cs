using BE;
using SER;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA.Common
{
    public static class ComprobanteCompras06AV
    {
        private static string Carpeta()
        {
            string documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string carpeta = Path.Combine(documentos, "PC Factory", "Ordenes de compra");
            if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);
            return carpeta;
        }

        public static string GenerarOrdenCompra(OrdenCompra06AV orden, PedidoCotizacion06AV cotizacion)
        {
            if (orden == null) throw new ArgumentNullException(nameof(orden));
            string ruta = Path.Combine(Carpeta(), $"OrdenCompra_{orden.NumeroCompra:00000}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            System.Drawing.Image logo = null;
            try { logo = Properties.Resources.pcforge_logo1; } catch { }
            OrdenCompraPdf06AV.Guardar(ruta, orden, cotizacion, logo);
            return ruta;
        }

        public static void Enviar(OrdenCompra06AV orden, Proveedor06AV proveedor, string ruta, IWin32Window owner = null)
        {
            var t = GestorIdioma06AV.Instancia;
            if (proveedor == null || string.IsNullOrWhiteSpace(proveedor.Email))
            {
                ConfirmacionForm.MostrarInfo(t.Obtener("pcf_oc_sin_email", proveedor?.Nombre ?? "-"),
                    t.Obtener("pcf_oc_doc_titulo"), ConfirmacionForm.TipoConfirmacion.Advertencia, owner);
                MostrarEnCarpeta(ruta);
                return;
            }

            string asunto = t.Obtener("pcf_oc_mail_asunto", orden.NumeroCompra);
            string cuerpo = t.Obtener("pcf_oc_mail_cuerpo", proveedor.Nombre, orden.NumeroCompra,
                                      orden.FechaLimite.ToString("dd/MM/yyyy"), Path.GetFileName(ruta));
            string mailto = "mailto:" + Uri.EscapeDataString(proveedor.Email.Trim()) +
                            "?subject=" + Uri.EscapeDataString(asunto) +
                            "&body=" + Uri.EscapeDataString(cuerpo);
            try { Process.Start(mailto); } catch { }
            MostrarEnCarpeta(ruta);
            ConfirmacionForm.MostrarInfo(t.Obtener("pcf_oc_mail_adjuntar", Path.GetFileName(ruta)),
                t.Obtener("pcf_oc_doc_titulo"), ConfirmacionForm.TipoConfirmacion.Info, owner);
        }

        public static void Ofrecer(OrdenCompra06AV orden, PedidoCotizacion06AV cotizacion, bool recienAprobada,
                                   IWin32Window owner = null)
        {
            var t = GestorIdioma06AV.Instancia;
            string ruta;
            try { ruta = GenerarOrdenCompra(orden, cotizacion); }
            catch (Exception ex)
            {
                ConfirmacionForm.MostrarInfo(t.Obtener("pcf_oc_error_pdf", ex.Message), t.Obtener("pcf_oc_doc_titulo"),
                    ConfirmacionForm.TipoConfirmacion.Advertencia, owner);
                return;
            }

            string proveedor = cotizacion?.Proveedor?.Nombre ?? "-";
            bool enviar = ConfirmacionForm.Mostrar(
                recienAprobada
                    ? t.Obtener("pcf_oc_aprobada_enviar", orden.NumeroCompra, proveedor, Path.GetFileName(ruta))
                    : t.Obtener("pcf_oc_generada", orden.NumeroCompra, Path.GetFileName(ruta)),
                t.Obtener("pcf_oc_doc_titulo"), ConfirmacionForm.TipoConfirmacion.Info,
                t.Obtener("pcf_oc_enviar"),
                recienAprobada ? t.Obtener("pcf_oc_mas_tarde") : t.Obtener("pcf_oc_abrir"), owner);

            if (enviar) Enviar(orden, cotizacion?.Proveedor, ruta, owner);
            else if (!recienAprobada) Abrir(ruta);
        }

        public static void MostrarEnCarpeta(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta)) return;
            try { Process.Start("explorer.exe", "/select,\"" + ruta + "\""); } catch { }
        }

        public static void Abrir(string ruta)
        {
            try { Process.Start(ruta); } catch { }
        }
    }
}
