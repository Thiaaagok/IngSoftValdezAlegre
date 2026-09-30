using BE;
using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;

namespace PCFORGE_ValdezThiago_96VA.Common
{
    internal sealed class ComprobantePdf06AV
    {
        private static readonly Color Oscuro = Color.FromArgb(18, 22, 26);
        private static readonly Color Grafito = Color.FromArgb(36, 43, 50);
        private static readonly Color Cian = Color.FromArgb(2, 201, 242);
        private static readonly Color CianTexto = Color.FromArgb(0, 128, 163);
        private static readonly Color CianSuave = Color.FromArgb(232, 248, 253);
        private static readonly Color Naranja = Color.FromArgb(249, 101, 1);
        private static readonly Color NaranjaTexto = Color.FromArgb(204, 82, 0);
        private static readonly Color NaranjaSuave = Color.FromArgb(254, 241, 231);
        private static readonly Color Verde = Color.FromArgb(24, 140, 80);
        private static readonly Color VerdeSuave = Color.FromArgb(227, 245, 234);
        private static readonly Color Rojo = Color.FromArgb(200, 40, 45);
        private static readonly Color RojoSuave = Color.FromArgb(252, 228, 228);
        private static readonly Color MarcaAgua = Color.FromArgb(250, 222, 222);
        private static readonly Color Tinta = Color.FromArgb(28, 33, 38);
        private static readonly Color Gris = Color.FromArgb(104, 113, 122);
        private static readonly Color GrisClaro = Color.FromArgb(165, 173, 181);
        private static readonly Color Borde = Color.FromArgb(222, 227, 232);
        private static readonly Color Fondo = Color.FromArgb(246, 248, 250);
        private static readonly Color Blanco = Color.White;

        private const float M = 40f;
        private const float AnchoUtil = PdfLienzo06AV.Ancho - 2 * M;
        private const float Derecha = PdfLienzo06AV.Ancho - M;
        private const float LineaPie = PdfLienzo06AV.Alto - 36f;
        private const float LimiteContenido = LineaPie - 14f;
        private const float AltoFila = 30f;
        private const float AnchoResumen = 205f;
        private const float AnchoIzquierda = AnchoUtil - AnchoResumen - 20f;

        private static readonly float[] Columnas = { 40f, AnchoUtil - 40f - 78f - 80f - 86f, 78f, 80f, 86f };

        private readonly PdfLienzo06AV _pdf = new PdfLienzo06AV();
        private readonly GestorIdioma06AV _t = GestorIdioma06AV.Instancia;
        private readonly Venta06AV _venta;
        private readonly OrdenProduccion06AV _orden;
        private readonly Pago06AV _sena;
        private readonly decimal _montoSena;
        private readonly bool _esFactura;
        private readonly string _numero;
        private readonly DateTime _emision = DateTime.Now;

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

        private string Titulo => _t.Obtener(_esFactura ? "pcf_factura_titulo" : "pcf_recibo_titulo");

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

            int total = _pdf.CantidadPaginas;
            for (int i = 1; i <= total; i++)
            {
                _pdf.IrAPagina(i);
                PiePagina(i, total);
            }
            _pdf.Guardar(ruta, Titulo + (string.IsNullOrWhiteSpace(_numero) ? "" : " " + _numero));
        }

        private void NuevaPagina()
        {
            _pdf.NuevaPagina();
            if (Anulada)
                _pdf.TextoRotado(_t.Obtener("pcf_doc_anulada"), PdfLienzo06AV.Ancho / 2f, PdfLienzo06AV.Alto / 2f + 40f,
                                 96f, 35f, FuentePdf06AV.Negrita, MarcaAgua, 8f);
        }

        private float PaginaContinuacion()
        {
            NuevaPagina();
            const float alto = 48f;
            _pdf.Rect(0, 0, PdfLienzo06AV.Ancho, alto, Oscuro);
            _pdf.Rect(0, alto, PdfLienzo06AV.Ancho, 2.5f, Cian);
            _pdf.Rect(0, alto, 150f, 2.5f, Naranja);
            _pdf.Texto("PC", M, 30f, 14f, FuentePdf06AV.Negrita, Cian);
            _pdf.Texto("FORGE", M + _pdf.Medir("PC ", 14f, FuentePdf06AV.Negrita), 30f, 14f, FuentePdf06AV.Negrita, Naranja);
            string derecha = Titulo + (string.IsNullOrWhiteSpace(_numero) ? "" : "  ·  " + _t.Obtener("pcf_doc_nro") + " " + _numero);
            _pdf.Texto(derecha, Derecha, 29f, 9f, FuentePdf06AV.Negrita, Blanco, AlineacionPdf06AV.Derecha, 0.6f);
            return alto + 26f;
        }

        private float EncabezadoCompleto()
        {
            const float alto = 128f;
            _pdf.Rect(0, 0, PdfLienzo06AV.Ancho, alto, Oscuro);

            if (_pdf.TieneImagen)
            {
                float altoLogo = 104f;
                float anchoLogo = altoLogo * _pdf.ProporcionImagen;
                _pdf.Imagen(M - 10f, (alto - altoLogo) / 2f, anchoLogo, altoLogo);
            }
            else
            {
                _pdf.Texto("PC", M, 72f, 26f, FuentePdf06AV.Negrita, Cian);
                _pdf.Texto("FORGE", M + _pdf.Medir("PC ", 26f, FuentePdf06AV.Negrita), 72f, 26f, FuentePdf06AV.Negrita, Naranja);
            }

            _pdf.Texto(Titulo, Derecha, 54f, 25f, FuentePdf06AV.Negrita, Blanco, AlineacionPdf06AV.Derecha, 1.5f);

            string numero = string.IsNullOrWhiteSpace(_numero)
                ? _t.Obtener("pcf_venta") + " #" + _venta.NumeroVenta
                : _t.Obtener("pcf_doc_nro") + " " + _numero;
            _pdf.Texto(numero, Derecha, 75f, 11.5f, FuentePdf06AV.Negrita, Cian, AlineacionPdf06AV.Derecha, 0.4f);

            float x = Derecha;
            string fecha = _emision.ToString("dd/MM/yyyy");
            _pdf.Texto(fecha, x, 95f, 9f, FuentePdf06AV.Negrita, Blanco, AlineacionPdf06AV.Derecha);
            x -= _pdf.Medir(fecha, 9f, FuentePdf06AV.Negrita) + 6f;
            _pdf.Texto(_t.Obtener("pcf_doc_emision"), x, 95f, 8.5f, FuentePdf06AV.Normal, GrisClaro, AlineacionPdf06AV.Derecha);

            _pdf.Texto("PC Forge  ·  PC Factory", Derecha, 110f, 8f, FuentePdf06AV.Normal, GrisClaro, AlineacionPdf06AV.Derecha, 0.3f);

            _pdf.Rect(0, alto, PdfLienzo06AV.Ancho, 3f, Cian);
            _pdf.Rect(0, alto, 190f, 3f, Naranja);
            return alto + 3f;
        }

        private float Tarjetas(float y)
        {
            float ancho = (AnchoUtil - 14f) / 2f;
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

            int filas = Math.Max(lineasCliente.Count + 1, datos.Count);
            float alto = 42f + (filas - 1) * 15f + 16f;

            float xi = M, xd = M + ancho + 14f;
            _pdf.RectRedondeado(xi, y, ancho, alto, 6f, Fondo, Borde);
            _pdf.RectRedondeado(xd, y, ancho, alto, 6f, Fondo, Borde);

            Etiqueta(_t.Obtener(_esFactura ? "pcf_doc_facturado_a" : "pcf_doc_recibimos_de"), xi + 14f, y + 20f);
            Etiqueta(_t.Obtener("pcf_doc_operacion"), xd + 14f, y + 20f);

            string nombre = cliente == null ? "-" : (cliente.Nombre + " " + cliente.Apellido).Trim();
            _pdf.Texto(_pdf.Recortar(nombre, ancho - 28f, 12f, FuentePdf06AV.Negrita), xi + 14f, y + 41f, 12f, FuentePdf06AV.Negrita, Tinta);
            float ly = y + 41f;
            foreach (string l in lineasCliente)
            {
                ly += 15f;
                _pdf.Texto(_pdf.Recortar(l, ancho - 28f, 9f, FuentePdf06AV.Normal), xi + 14f, ly, 9f, FuentePdf06AV.Normal, Gris);
            }

            float dy = y + 41f;
            foreach (var d in datos)
            {
                _pdf.Texto(d.Key, xd + 14f, dy, 8.5f, FuentePdf06AV.Normal, Gris);
                float libre = ancho - 28f - _pdf.Medir(d.Key, 8.5f, FuentePdf06AV.Normal) - 10f;
                _pdf.Texto(_pdf.Recortar(d.Value, libre, 9f, FuentePdf06AV.Negrita), xd + ancho - 14f, dy, 9f,
                           FuentePdf06AV.Negrita, Tinta, AlineacionPdf06AV.Derecha);
                dy += 15f;
            }
            return y + alto;
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

        private sealed class Item
        {
            public int Cantidad;
            public string Descripcion;
            public string Detalle;
            public string Codigo;
            public decimal Unitario;
            public decimal Importe => Unitario * Cantidad;
        }

        private List<Item> Items()
        {
            var comps = _venta.Computadora?.Componentes ?? new List<Componente06AV>();
            return comps.GroupBy(c => c.Codigo ?? c.Descripcion)
                        .Select(g => new Item
                        {
                            Cantidad = g.Count(),
                            Descripcion = g.First().Descripcion,
                            Detalle = ((g.First().Marca ?? "") + " " + (g.First().Modelo ?? "")).Trim(),
                            Codigo = g.First().Codigo,
                            Unitario = g.First().PrecioUnitario
                        })
                        .ToList();
        }

        private float EncabezadoTabla(float y)
        {
            _pdf.RectRedondeado(M, y, AnchoUtil, 24f, 4f, Grafito);
            _pdf.Rect(M, y + 12f, AnchoUtil, 12f, Grafito);
            string[] titulos =
            {
                _t.Obtener("pcf_doc_col_cant"), _t.Obtener("pcf_doc_col_desc"), _t.Obtener("pcf_doc_col_codigo"),
                _t.Obtener("pcf_doc_col_unit"), _t.Obtener("pcf_doc_col_importe")
            };
            float x = M;
            for (int i = 0; i < titulos.Length; i++)
            {
                float w = Columnas[i];
                var al = i == 0 ? AlineacionPdf06AV.Centro : i >= 3 ? AlineacionPdf06AV.Derecha : AlineacionPdf06AV.Izquierda;
                float tx = al == AlineacionPdf06AV.Centro ? x + w / 2f : al == AlineacionPdf06AV.Derecha ? x + w - 12f : x + 12f;
                _pdf.Texto(titulos[i], tx, y + 15.5f, 7f, FuentePdf06AV.Negrita, Blanco, al, 0.7f);
                x += w;
            }
            return y + 24f;
        }

        private float TablaItems(float y)
        {
            var items = Items();
            y = EncabezadoTabla(y);
            if (items.Count == 0)
            {
                _pdf.Rect(M, y, AnchoUtil, AltoFila, Fondo);
                _pdf.Texto(_t.Obtener("pcf_sin_componentes"), M + 12f, y + 19f, 9f, FuentePdf06AV.Cursiva, Gris);
                return y + AltoFila;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (y + AltoFila > LimiteContenido)
                    y = EncabezadoTabla(PaginaContinuacion());

                var it = items[i];
                if (i % 2 == 1) _pdf.Rect(M, y, AnchoUtil, AltoFila, Fondo);
                _pdf.Linea(M, y + AltoFila, Derecha, y + AltoFila, Borde, 0.6f);

                float x = M;
                _pdf.Texto(it.Cantidad.ToString(), x + Columnas[0] / 2f, y + 19f, 9.5f, FuentePdf06AV.Negrita, Tinta, AlineacionPdf06AV.Centro);
                x += Columnas[0];

                float anchoDesc = Columnas[1] - 20f;
                if (string.IsNullOrWhiteSpace(it.Detalle))
                {
                    _pdf.Texto(_pdf.Recortar(it.Descripcion, anchoDesc, 9f, FuentePdf06AV.Negrita), x + 12f, y + 19f, 9f, FuentePdf06AV.Negrita, Tinta);
                }
                else
                {
                    _pdf.Texto(_pdf.Recortar(it.Descripcion, anchoDesc, 9f, FuentePdf06AV.Negrita), x + 12f, y + 13.5f, 9f, FuentePdf06AV.Negrita, Tinta);
                    _pdf.Texto(_pdf.Recortar(it.Detalle, anchoDesc, 7.5f, FuentePdf06AV.Normal), x + 12f, y + 24f, 7.5f, FuentePdf06AV.Normal, Gris);
                }
                x += Columnas[1];

                _pdf.Texto(_pdf.Recortar(it.Codigo, Columnas[2] - 16f, 8f, FuentePdf06AV.Normal), x + 12f, y + 19f, 8f, FuentePdf06AV.Normal, Gris);
                x += Columnas[2];
                _pdf.Texto(Plata(it.Unitario), x + Columnas[3] - 12f, y + 19f, 9f, FuentePdf06AV.Normal, Tinta, AlineacionPdf06AV.Derecha);
                x += Columnas[3];
                _pdf.Texto(Plata(it.Importe), x + Columnas[4] - 12f, y + 19f, 9f, FuentePdf06AV.Negrita, Tinta, AlineacionPdf06AV.Derecha);
                y += AltoFila;
            }
            return y;
        }

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

        private void PiePagina(int pagina, int total)
        {
            _pdf.Linea(M, LineaPie, Derecha, LineaPie, Borde, 0.8f);
            _pdf.Rect(M, LineaPie - 1f, 40f, 2f, Cian);
            string izq = "PC Forge  ·  PC Factory   —   " + _t.Obtener("pcf_doc_generado", _emision.ToString("dd/MM/yyyy HH:mm"));
            _pdf.Texto(izq, M, LineaPie + 14f, 7.5f, FuentePdf06AV.Normal, GrisClaro);
            _pdf.Texto(_t.Obtener("pcf_doc_pagina", pagina, total), Derecha, LineaPie + 14f, 7.5f, FuentePdf06AV.Normal,
                       GrisClaro, AlineacionPdf06AV.Derecha);
        }

        private float FilaResumen(string etiqueta, string valor, float x, float y, Color color, bool negrita = false)
        {
            var fuente = negrita ? FuentePdf06AV.Negrita : FuentePdf06AV.Normal;
            float anchoValor = _pdf.Medir(valor, 9.5f, fuente);
            _pdf.Texto(_pdf.Recortar(etiqueta, AnchoResumen - anchoValor - 22f, 9f, FuentePdf06AV.Normal), x + 12f, y + 13f, 9f,
                       FuentePdf06AV.Normal, Gris);
            _pdf.Texto(valor, x + AnchoResumen - 12f, y + 13f, 9.5f, fuente, color, AlineacionPdf06AV.Derecha);
            return y + 20f;
        }

        private float BarraTotal(string etiqueta, string valor, float x, float y)
        {
            float anchoValor = _pdf.Medir(valor, 15f, FuentePdf06AV.Negrita);
            float anchoEtiqueta = _pdf.Medir(etiqueta, 8f, FuentePdf06AV.Negrita, 1f);
            bool unaLinea = 14f + anchoEtiqueta + 12f + anchoValor + 12f <= AnchoResumen;
            float alto = unaLinea ? 38f : 50f;
            _pdf.RectRedondeado(x, y, AnchoResumen, alto, 5f, Oscuro);
            _pdf.Rect(x, y + 8f, 3.5f, alto - 16f, Naranja);
            if (unaLinea)
            {
                _pdf.Texto(etiqueta, x + 14f, y + 23f, 8f, FuentePdf06AV.Negrita, GrisClaro, AlineacionPdf06AV.Izquierda, 1f);
                _pdf.Texto(valor, x + AnchoResumen - 12f, y + 24.5f, 15f, FuentePdf06AV.Negrita, Blanco, AlineacionPdf06AV.Derecha);
            }
            else
            {
                _pdf.Texto(_pdf.Recortar(etiqueta, AnchoResumen - 26f, 8f, FuentePdf06AV.Negrita), x + 14f, y + 18f, 8f,
                           FuentePdf06AV.Negrita, GrisClaro, AlineacionPdf06AV.Izquierda, 1f);
                _pdf.Texto(valor, x + AnchoResumen - 12f, y + 38f, 15f, FuentePdf06AV.Negrita, Blanco, AlineacionPdf06AV.Derecha);
            }
            return y + alto;
        }

        private void Etiqueta(string texto, float x, float linea)
        {
            _pdf.Texto(texto.ToUpper(), x, linea, 7f, FuentePdf06AV.Negrita, CianTexto, AlineacionPdf06AV.Izquierda, 1.1f);
        }

        private void Pastilla(string texto, float x, float y, Color fondo, Color color, bool grande = false)
        {
            float tam = grande ? 8f : 7f;
            float alto = grande ? 20f : 15f;
            float ancho = _pdf.Medir(texto, tam, FuentePdf06AV.Negrita, 0.6f) + (grande ? 24f : 16f);
            _pdf.RectRedondeado(x, y, ancho, alto, alto / 2f, fondo);
            _pdf.Texto(texto, x + ancho / 2f, y + alto / 2f + tam * 0.36f, tam, FuentePdf06AV.Negrita, color, AlineacionPdf06AV.Centro, 0.6f);
        }

        private static KeyValuePair<string, string> Par(string k, string v) => new KeyValuePair<string, string>(k, v ?? "-");

        private static string Plata(decimal v) => v.ToString("C2", CultureInfo.CurrentCulture);
    }
}
