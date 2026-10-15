using BE;
using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;

namespace PCFORGE_ValdezThiago_96VA.Common
{
    internal sealed class ComprobantePdf06AV : DocumentoPdf06AV
    {
        private readonly Venta06AV _venta;
        private readonly OrdenProduccion06AV _orden;
        private readonly Pago06AV _sena;
        private readonly decimal _montoSena;
        private readonly bool _esFactura;
        private readonly string _numero;

        private ComprobantePdf06AV(Venta06AV venta, OrdenProduccion06AV orden, Pago06AV sena, decimal montoSena,
                                   bool esFactura, string numero)
        {
            _venta = venta;
            _orden = orden;
            _sena = sena;
            _montoSena = montoSena;
            _esFactura = esFactura;
            _numero = numero;
        }

        public static void GuardarFactura(string ruta, Venta06AV venta, OrdenProduccion06AV orden, string numero, Image logo)
        {
            new ComprobantePdf06AV(venta, orden, null, 0m, true, numero).Generar(ruta, logo);
        }

        public static void GuardarRecibo(string ruta, Venta06AV venta, Pago06AV sena, decimal monto, string numero, Image logo)
        {
            new ComprobantePdf06AV(venta, null, sena, monto, false, numero).Generar(ruta, logo);
        }

        protected override string Titulo => _t.Obtener(_esFactura ? "pcf_factura_titulo" : "pcf_recibo_titulo");

        protected override string Numero => _numero;

        protected override string NumeroAlternativo => _t.Obtener("pcf_venta") + " #" + _venta.NumeroVenta;

        protected override string MarcaAgua => Anulada ? _t.Obtener("pcf_doc_anulada") : null;

        private bool Anulada => _venta.Estado == EstadoVenta06AV.Anulada;

        private void Generar(string ruta, Image logo)
        {
            _pdf.DefinirImagen(logo);
            NuevaPagina();
            float y = EncabezadoCompleto();
            y = Tarjetas(y + 22f);
            y = Equipo(y + 22f);
            y = TablaItems(y + 12f);

            float alto = _esFactura ? AltoResumenFactura() : AltoResumenRecibo();
            if (y + 20f + alto > LimiteContenido)
                y = PaginaContinuacion();
            else
                y += 20f;

            y = _esFactura ? ResumenFactura(y) : ResumenRecibo(y);
            Cierre(y);

            Finalizar(ruta);
        }




        private float Tarjetas(float y)
        {
            float ancho = AnchoTarjeta;
            var cliente = _venta.Cliente;

            var lineasCliente = new List<string>();
            if (cliente != null)
            {
                if (!string.IsNullOrWhiteSpace(cliente.Dni)) lineasCliente.Add(_t.Obtener("pcf_doc_dni") + " " + cliente.Dni);
                if (!string.IsNullOrWhiteSpace(cliente.Telefono)) lineasCliente.Add(_t.Obtener("pcf_doc_telefono") + " " + cliente.Telefono);
                if (!string.IsNullOrWhiteSpace(cliente.Direccion))
                    lineasCliente.AddRange(_pdf.Envolver(cliente.Direccion, ancho - 28f, 9f, FuentePdf06AV.Normal).Take(2));
            }

            var datos = new List<KeyValuePair<string, string>>
            {
                Par(_t.Obtener("pcf_venta"), "#" + _venta.NumeroVenta),
                Par(_t.Obtener("pcf_doc_fecha_venta"), _venta.FechaVenta.ToString("dd/MM/yyyy"))
            };
            if (_esFactura)
            {
                int? nroOrden = _orden?.NumeroOrden ?? _venta.NumeroOrdenProduccion;
                if (nroOrden.HasValue) datos.Add(Par(_t.Obtener("pcf_doc_orden_prod"), "#" + nroOrden.Value));
                if (!string.IsNullOrWhiteSpace(_orden?.NumeroSerie)) datos.Add(Par(_t.Obtener("pcf_nro_serie"), _orden.NumeroSerie));
                DateTime entrega = _orden?.FechaCierre ?? _venta.FechaEntregaEstimada;
                datos.Add(Par(_t.Obtener(_orden?.FechaCierre != null ? "pcf_f_entrega" : "pcf_f_entrega_estimada"), entrega.ToString("dd/MM/yyyy")));
                var saldo = _venta.Pagos?.FirstOrDefault(p => p.Tipo == TipoPago06AV.SaldoFinal);
                string atendio = !string.IsNullOrWhiteSpace(saldo?.Usuario) ? saldo.Usuario : _venta.UsuarioRegistro;
                if (!string.IsNullOrWhiteSpace(atendio)) datos.Add(Par(_t.Obtener("pcf_atendido_por"), atendio));
            }
            else
            {
                datos.Add(Par(_t.Obtener("pcf_f_entrega_estimada"), _venta.FechaEntregaEstimada.ToString("dd/MM/yyyy")));
                if (_sena != null) datos.Add(Par(_t.Obtener("pcf_forma_pago"), ComprobantePcFactory06AV.TextoFormaPago(_sena.FormaPago)));
                string atendio = !string.IsNullOrWhiteSpace(_sena?.Usuario) ? _sena.Usuario : _venta.UsuarioRegistro;
                if (!string.IsNullOrWhiteSpace(atendio)) datos.Add(Par(_t.Obtener("pcf_atendido_por"), atendio));
            }

            string nombre = cliente == null ? "-" : (cliente.Nombre + " " + cliente.Apellido).Trim();
            return DibujarTarjetas(y, _t.Obtener(_esFactura ? "pcf_doc_facturado_a" : "pcf_doc_recibimos_de"), nombre,
                                   lineasCliente, _t.Obtener("pcf_doc_operacion"), datos);
        }

        private float Equipo(float y)
        {
            var pc = _venta.Computadora;
            Etiqueta(_t.Obtener("pcf_doc_equipo"), M, y + 8f);
            string nombre = pc?.Nombre;
            if (string.IsNullOrWhiteSpace(nombre)) nombre = _t.Obtener("pcf_computadora");

            string cantidad = pc == null ? "" : _t.Obtener("pcf_armar_n_componentes", pc.Componentes?.Count ?? 0);
            float anchoCantidad = _pdf.Medir(cantidad, 8.5f, FuentePdf06AV.Normal);

            string tipo = pc == null ? null
                : _t.Obtener(pc.TipoConfiguracion == TipoConfiguracion06AV.Estandar ? "pcf_venta_tipo_estandar" : "pcf_venta_tipo_config");
            float anchoPastilla = tipo == null ? 0f : _pdf.Medir(tipo.ToUpper(), 7f, FuentePdf06AV.Negrita, 0.6f) + 16f;

            float maxNombre = AnchoUtil - anchoCantidad - anchoPastilla - 30f;
            nombre = _pdf.Recortar(nombre, maxNombre, 14f, FuentePdf06AV.Negrita);
            _pdf.Texto(nombre, M, y + 28f, 14f, FuentePdf06AV.Negrita, Tinta);

            if (tipo != null)
            {
                float px = M + _pdf.Medir(nombre, 14f, FuentePdf06AV.Negrita) + 10f;
                Pastilla(tipo.ToUpper(), px, y + 15.5f, CianSuave, CianTexto);
            }
            _pdf.Texto(cantidad, Derecha, y + 28f, 8.5f, FuentePdf06AV.Normal, Gris, AlineacionPdf06AV.Derecha);
            return y + 36f;
        }

        private List<ItemPdf> Items()
        {
            var comps = _venta.Computadora?.Componentes ?? new List<Componente06AV>();
            return comps.GroupBy(c => c.Codigo ?? c.Descripcion)
                        .Select(g => new ItemPdf
                        {
                            Cantidad = g.Count(),
                            Descripcion = g.First().Descripcion,
                            Detalle = ((g.First().Marca ?? "") + " " + (g.First().Modelo ?? "")).Trim(),
                            Codigo = g.First().Codigo,
                            Unitario = g.First().PrecioUnitario
                        })
                        .ToList();
        }


        private float TablaItems(float y) => TablaItems(y, Items(), _t.Obtener("pcf_sin_componentes"));

        private decimal Subtotal => Items().Sum(i => i.Importe);

        private decimal Ajuste => _venta.PrecioTotal - Subtotal;

        private List<Pago06AV> Pagos => (_venta.Pagos ?? new List<Pago06AV>()).OrderBy(p => p.Fecha).ToList();

        private float AltoResumenFactura()
        {
            float izquierda = 30f + 22f + Math.Max(1, Pagos.Count) * 20f + 40f;
            float derecha = (Ajuste != 0m ? 2 : 1) * 20f + 44f + 2 * 20f;
            return Math.Max(izquierda, derecha);
        }

        private float AltoResumenRecibo() => 150f;

        private float ResumenFactura(float y)
        {
            float xr = Derecha - AnchoResumen;
            float ry = y + 4f;
            ry = FilaResumen(_t.Obtener("pcf_doc_subtotal"), Plata(Subtotal), xr, ry, Tinta);
            if (Ajuste != 0m) ry = FilaResumen(_t.Obtener("pcf_doc_ajuste"), Plata(Ajuste), xr, ry, Tinta);
            ry = BarraTotal(_t.Obtener("pcf_doc_total"), Plata(_venta.PrecioTotal), xr, ry + 4f);
            ry = FilaResumen(_t.Obtener("pcf_doc_total_abonado"), Plata(_venta.TotalAbonado), xr, ry + 8f, Tinta);
            decimal saldo = _venta.SaldoPendiente;
            ry = FilaResumen(_t.Obtener("pcf_saldo_pendiente"), Plata(saldo), xr, ry, saldo > 0m ? NaranjaTexto : Verde, true);

            Etiqueta(_t.Obtener("pcf_doc_pagos"), M, y + 12f);
            float ty = y + 22f;
            float[] anchos = { 56f, 54f, 52f, 62f, AnchoIzquierda - 224f };
            string[] titulos =
            {
                _t.Obtener("pcf_doc_col_concepto"), _t.Obtener("pcf_doc_col_recibo"), _t.Obtener("pcf_doc_col_fecha"),
                _t.Obtener("pcf_doc_col_forma"), _t.Obtener("pcf_doc_col_importe")
            };
            _pdf.Linea(M, ty + 18f, M + AnchoIzquierda, ty + 18f, Borde, 0.8f);
            float x = M;
            for (int i = 0; i < titulos.Length; i++)
            {
                bool der = i == titulos.Length - 1;
                _pdf.Texto(titulos[i], der ? x + anchos[i] : x, ty + 12f, 6.5f, FuentePdf06AV.Negrita, Gris,
                           der ? AlineacionPdf06AV.Derecha : AlineacionPdf06AV.Izquierda, 0.6f);
                x += anchos[i];
            }
            ty += 18f;

            var pagos = Pagos;
            if (pagos.Count == 0)
            {
                _pdf.Texto(_t.Obtener("pcf_doc_sin_pagos"), M, ty + 14f, 8.5f, FuentePdf06AV.Cursiva, Gris);
                ty += 20f;
            }
            foreach (var p in pagos)
            {
                string concepto = _t.Obtener(p.Tipo == TipoPago06AV.Sena ? "pcf_sena" : "pcf_saldo_final");
                string[] valores =
                {
                    concepto, p.NumeroRecibo ?? "-", p.Fecha.ToString("dd/MM/yyyy"),
                    ComprobantePcFactory06AV.TextoFormaPago(p.FormaPago), Plata(p.Monto)
                };
                x = M;
                for (int i = 0; i < valores.Length; i++)
                {
                    bool der = i == valores.Length - 1;
                    var fuente = i == 0 || der ? FuentePdf06AV.Negrita : FuentePdf06AV.Normal;
                    float tam = i == 0 || der ? 8.5f : 8f;
                    string v = _pdf.Recortar(valores[i], anchos[i] - (der ? 0f : 4f), tam, fuente);
                    _pdf.Texto(v, der ? x + anchos[i] : x, ty + 14f, tam, fuente, i == 0 || der ? Tinta : Gris,
                               der ? AlineacionPdf06AV.Derecha : AlineacionPdf06AV.Izquierda);
                    x += anchos[i];
                }
                _pdf.Linea(M, ty + 20f, M + AnchoIzquierda, ty + 20f, Borde, 0.5f);
                ty += 20f;
            }

            ty += 14f;
            if (Anulada)
                Pastilla(_t.Obtener("pcf_doc_anulada"), M, ty, RojoSuave, Rojo, true);
            else if (saldo <= 0m)
                Pastilla(_t.Obtener("pcf_doc_pagada"), M, ty, VerdeSuave, Verde, true);
            else
                Pastilla(_t.Obtener("pcf_doc_con_saldo") + "  " + Plata(saldo), M, ty, NaranjaSuave, NaranjaTexto, true);
            ty += 20f;

            if (ty + 30f <= LimiteContenido)
            {
                _pdf.Texto(_t.Obtener("pcf_doc_gracias"), M, ty + 24f, 9f, FuentePdf06AV.Cursiva, Gris);
                ty += 30f;
            }
            return Math.Max(ry, ty);
        }

        private float ResumenRecibo(float y)
        {
            const float alto = 104f;
            _pdf.Rect(M, y, AnchoIzquierda, alto, CianSuave);
            _pdf.Rect(M, y, 4f, alto, Cian);
            Etiqueta(_t.Obtener("pcf_doc_recibimos"), M + 18f, y + 21f);
            _pdf.Texto(Plata(_montoSena), M + 18f, y + 50f, 23f, FuentePdf06AV.Negrita, Tinta);

            float ly = y + 67f;
            string concepto = _t.Obtener("pcf_doc_concepto_sena", _venta.NumeroVenta);
            foreach (string l in _pdf.Envolver(concepto, AnchoIzquierda - 36f, 8.5f, FuentePdf06AV.Normal).Take(2))
            {
                _pdf.Texto(l, M + 18f, ly, 8.5f, FuentePdf06AV.Normal, Gris);
                ly += 12f;
            }
            if (_sena != null)
            {
                string forma = _t.Obtener("pcf_forma_pago") + ": " + ComprobantePcFactory06AV.TextoFormaPago(_sena.FormaPago);
                if (!string.IsNullOrWhiteSpace(_sena.Referencia))
                    forma += "   ·   " + _t.Obtener("pcf_referencia") + ": " + _sena.Referencia;
                _pdf.Texto(_pdf.Recortar(forma, AnchoIzquierda - 36f, 8.5f, FuentePdf06AV.Negrita), M + 18f, ly + 4f, 8.5f,
                           FuentePdf06AV.Negrita, Tinta);
            }

            float xr = Derecha - AnchoResumen;
            float ry = y + 4f;
            ry = FilaResumen(_t.Obtener("pcf_doc_total_venta"), Plata(_venta.PrecioTotal), xr, ry, Tinta);
            ry = FilaResumen(_t.Obtener("pcf_sena_recibida"), "- " + Plata(_montoSena), xr, ry, Verde, true);
            ry = BarraTotal(_t.Obtener("pcf_doc_a_pagar_retiro"), Plata(_venta.PrecioTotal - _montoSena), xr, ry + 4f);

            float fy = y + alto + 18f;
            if (Anulada) Pastilla(_t.Obtener("pcf_doc_anulada"), M, fy, RojoSuave, Rojo, true);
            return Math.Max(y + alto + (Anulada ? 38f : 0f), ry);
        }

        private void Cierre(float y)
        {
            float espacio = LimiteContenido - y;
            if (!_esFactura)
            {
                if (espacio < 70f) y = PaginaContinuacion() - 20f;
                float fy = Math.Max(y + 58f, Math.Min(LimiteContenido - 30f, y + 90f));
                float xr = Derecha - AnchoResumen;
                _pdf.Linea(xr + 10f, fy, Derecha - 10f, fy, GrisClaro, 0.8f);
                _pdf.Texto(_t.Obtener("pcf_doc_firma"), xr + AnchoResumen / 2f, fy + 13f, 8f, FuentePdf06AV.Normal, Gris, AlineacionPdf06AV.Centro);
                foreach (string l in _pdf.Envolver(_t.Obtener("pcf_doc_nota_recibo"), AnchoIzquierda, 8.5f, FuentePdf06AV.Cursiva).Take(2))
                {
                    _pdf.Texto(l, M, fy, 8.5f, FuentePdf06AV.Cursiva, Gris);
                    fy += 12f;
                }
            }
        }







    }
}
