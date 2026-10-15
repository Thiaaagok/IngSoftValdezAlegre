using BE;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace PCFORGE_ValdezThiago_96VA.Common
{
    internal sealed class ReporteProveedoresPdf06AV : DocumentoPdf06AV
    {
        private static readonly float[] Anchos = { 50f, 128f, 124f, 80f, 30f, 28f, 75f };

        private readonly IList<FilaReporteProveedores06AV> _filas;
        private readonly string _filtro;
        private readonly string _usuario;

        private ReporteProveedoresPdf06AV(IList<FilaReporteProveedores06AV> filas, string filtro, string usuario)
        {
            _filas = filas ?? new List<FilaReporteProveedores06AV>();
            _filtro = filtro;
            _usuario = usuario;
        }

        public static void Guardar(string ruta, IList<FilaReporteProveedores06AV> filas, string filtro, string usuario, Image logo)
        {
            new ReporteProveedoresPdf06AV(filas, filtro, usuario).Generar(ruta, logo);
        }

        protected override string Titulo => _t.Obtener("rprov_doc_titulo");

        protected override string Numero => string.Empty;

        protected override string NumeroAlternativo => _filtro ?? string.Empty;

        protected override string ClavePie => "rpdf_generado_el";

        private void Generar(string ruta, Image logo)
        {
            _pdf.DefinirImagen(logo);
            NuevaPagina();
            float y = EncabezadoCompleto() + 20f;
            y = Indicadores(y);
            y = Tabla(y + 22f);
            if (!string.IsNullOrWhiteSpace(_usuario))
            {
                if (y + 30f > LimiteContenido) y = PaginaContinuacion();
                _pdf.Texto(_t.Obtener("rprov_doc_pie", _usuario, _filas.Count), M, y + 22f, 8f, FuentePdf06AV.Cursiva, Gris);
            }
            Finalizar(ruta);
        }

        private float Indicadores(float y)
        {
            int activos = _filas.Count(f => f.Activo);
            int conCompras = _filas.Count(f => f.OrdenesCompra > 0);
            decimal total = _filas.Sum(f => f.TotalComprado);
            var top = _filas.Where(f => f.TotalComprado > 0).OrderByDescending(f => f.TotalComprado).FirstOrDefault();

            var tarjetas = new[]
            {
                new[] { _t.Obtener("rprov_kpi_proveedores"), _filas.Count.ToString(), _t.Obtener("rprov_kpi_activos", activos) },
                new[] { _t.Obtener("rprov_kpi_con_compras"), conCompras.ToString(), _t.Obtener("rprov_kpi_cotizaciones", _filas.Sum(f => f.Cotizaciones)) },
                new[] { _t.Obtener("rprov_kpi_total"), Plata(total), _t.Obtener("rprov_kpi_ordenes", _filas.Sum(f => f.OrdenesCompra)) },
                new[] { _t.Obtener("rprov_kpi_principal"), top?.Nombre ?? "—",
                        top != null && total > 0 ? _t.Obtener("rprov_kpi_participacion", Math.Round(top.TotalComprado * 100m / total)) : "" }
            };

            const float gap = 10f;
            float ancho = (AnchoUtil - gap * 3) / 4f;
            const float alto = 62f;
            for (int i = 0; i < tarjetas.Length; i++)
            {
                float x = M + i * (ancho + gap);
                _pdf.RectRedondeado(x, y, ancho, alto, 6f, Fondo, Borde);
                _pdf.Rect(x, y + 10f, 3f, alto - 20f, i == 2 ? Naranja : Cian);
                Etiqueta(tarjetas[i][0], x + 12f, y + 17f);
                float tam = _pdf.Medir(tarjetas[i][1], 14f, FuentePdf06AV.Negrita) > ancho - 22f ? 10f : 14f;
                _pdf.Texto(_pdf.Recortar(tarjetas[i][1], ancho - 22f, tam, FuentePdf06AV.Negrita), x + 12f, y + 38f, tam,
                           FuentePdf06AV.Negrita, Tinta);
                _pdf.Texto(_pdf.Recortar(tarjetas[i][2], ancho - 22f, 7.5f, FuentePdf06AV.Normal), x + 12f, y + 53f, 7.5f,
                           FuentePdf06AV.Normal, Gris);
            }
            return y + alto;
        }

        private float Encabezado(float y)
        {
            _pdf.RectRedondeado(M, y, AnchoUtil, 24f, 4f, Grafito);
            _pdf.Rect(M, y + 12f, AnchoUtil, 12f, Grafito);
            string[] titulos =
            {
                _t.Obtener("rprov_col_codigo"), _t.Obtener("rprov_col_proveedor"), _t.Obtener("rprov_col_contacto"),
                _t.Obtener("rprov_col_tipos"), _t.Obtener("rprov_col_cot"), _t.Obtener("rprov_col_oc"), _t.Obtener("rprov_col_total")
            };
            float x = M;
            for (int i = 0; i < titulos.Length; i++)
            {
                bool derecha = i >= 4;
                float tx = derecha ? x + Anchos[i] - 8f : x + 8f;
                _pdf.Texto(_pdf.Recortar(titulos[i], Anchos[i] - 10f, 7f, FuentePdf06AV.Negrita), tx, y + 15.5f, 7f,
                           FuentePdf06AV.Negrita, Blanco, derecha ? AlineacionPdf06AV.Derecha : AlineacionPdf06AV.Izquierda, 0.5f);
                x += Anchos[i];
            }
            return y + 24f;
        }

        private float Tabla(float y)
        {
            const float alto = 34f;
            y = Encabezado(y);
            if (_filas.Count == 0)
            {
                _pdf.Rect(M, y, AnchoUtil, alto, Fondo);
                _pdf.Texto(_t.Obtener("rep_sin_datos"), M + 12f, y + 21f, 9f, FuentePdf06AV.Cursiva, Gris);
                return y + alto;
            }

            for (int i = 0; i < _filas.Count; i++)
            {
                if (y + alto > LimiteContenido) y = Encabezado(PaginaContinuacion());
                var f = _filas[i];
                if (i % 2 == 1) _pdf.Rect(M, y, AnchoUtil, alto, Fondo);
                _pdf.Linea(M, y + alto, Derecha, y + alto, Borde, 0.6f);
                Color tinta = f.Activo ? Tinta : GrisClaro;

                float x = M;
                _pdf.Texto(f.Codigo, x + 8f, y + 14f, 8f, FuentePdf06AV.Negrita, f.Activo ? CianTexto : GrisClaro);
                if (!f.Activo)
                    _pdf.Texto(_t.Obtener("pcf_prov_inactivo").ToUpper(), x + 8f, y + 26f, 6f, FuentePdf06AV.Negrita, Rojo,
                               AlineacionPdf06AV.Izquierda, 0.4f);
                x += Anchos[0];

                Celda(f.Nombre, "CUIT " + f.Cuit, x, y, Anchos[1], tinta);
                x += Anchos[1];
                Celda(string.IsNullOrWhiteSpace(f.Email) ? "-" : f.Email, f.Telefono, x, y, Anchos[2], tinta, false);
                x += Anchos[2];
                var tipos = _pdf.Envolver(string.IsNullOrWhiteSpace(f.Tipos) ? "-" : f.Tipos, Anchos[3] - 14f, 7.5f, FuentePdf06AV.Normal);
                _pdf.Texto(tipos[0], x + 8f, y + 14f, 7.5f, FuentePdf06AV.Normal, tinta);
                if (tipos.Count > 1)
                    _pdf.Texto(_pdf.Recortar(string.Join(" ", tipos.Skip(1)), Anchos[3] - 14f, 7.5f, FuentePdf06AV.Normal),
                               x + 8f, y + 25f, 7.5f, FuentePdf06AV.Normal, tinta);
                x += Anchos[3];
                _pdf.Texto(f.Cotizaciones.ToString(), x + Anchos[4] - 8f, y + 20f, 9f, FuentePdf06AV.Normal, tinta, AlineacionPdf06AV.Derecha);
                x += Anchos[4];
                _pdf.Texto(f.OrdenesCompra.ToString(), x + Anchos[5] - 8f, y + 20f, 9f, FuentePdf06AV.Negrita, tinta, AlineacionPdf06AV.Derecha);
                x += Anchos[5];
                _pdf.Texto(Plata(f.TotalComprado), x + Anchos[6] - 8f, y + 15f, 8.5f, FuentePdf06AV.Negrita, tinta, AlineacionPdf06AV.Derecha);
                _pdf.Texto(f.UltimaCompra.HasValue ? f.UltimaCompra.Value.ToString("dd/MM/yyyy") : "—",
                           x + Anchos[6] - 8f, y + 26f, 7f, FuentePdf06AV.Normal, Gris, AlineacionPdf06AV.Derecha);
                y += alto;
            }

            y = y + 42f > LimiteContenido ? PaginaContinuacion() : y + 6f;
            _pdf.RectRedondeado(Derecha - AnchoResumen, y, AnchoResumen, 30f, 5f, Oscuro);
            _pdf.Rect(Derecha - AnchoResumen, y + 7f, 3.5f, 16f, Naranja);
            _pdf.Texto(_t.Obtener("rprov_kpi_total"), Derecha - AnchoResumen + 14f, y + 19f, 7.5f, FuentePdf06AV.Negrita, GrisClaro,
                       AlineacionPdf06AV.Izquierda, 0.8f);
            _pdf.Texto(Plata(_filas.Sum(f => f.TotalComprado)), Derecha - 12f, y + 20f, 12f, FuentePdf06AV.Negrita, Blanco,
                       AlineacionPdf06AV.Derecha);
            return y + 30f;
        }

        private void Celda(string principal, string secundario, float x, float y, float ancho, Color tinta, bool negrita = true)
        {
            var fuente = negrita ? FuentePdf06AV.Negrita : FuentePdf06AV.Normal;
            _pdf.Texto(_pdf.Recortar(principal ?? "-", ancho - 14f, 8.5f, fuente), x + 8f, y + 14f, 8.5f, fuente, tinta);
            if (!string.IsNullOrWhiteSpace(secundario))
                _pdf.Texto(_pdf.Recortar(secundario, ancho - 14f, 7.5f, FuentePdf06AV.Normal), x + 8f, y + 26f, 7.5f,
                           FuentePdf06AV.Normal, Gris);
        }
    }
}
