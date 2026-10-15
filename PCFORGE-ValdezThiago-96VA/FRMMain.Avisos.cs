using BE;
using BLL;
using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA
{
    public partial class FRMMain
    {
        private const int IntervaloAvisosMs = 90 * 1000;
        private const int DetallesPorAviso = 3;

        private readonly Dictionary<string, Action> _navegacion = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);
        private readonly AvisosBLL06AV _avisosBLL = new AvisosBLL06AV();
        private List<Aviso06AV> _avisos = new List<Aviso06AV>();
        private Button btnAvisos;
        private ContextMenuStrip cmsAvisos;
        private Timer _timerAvisos;

        private static bool VeAvisos()
        {
            try
            {
                var s = UsuarioSesion06AV.Instancia();
                return s.TienePermiso(PatenteEnum06AV.RegistrarOrdenCompra) || s.TienePermiso(PatenteEnum06AV.AprobarCotizacion) ||
                       s.TienePermiso(PatenteEnum06AV.GestionarCompras) || s.TienePermiso(PatenteEnum06AV.PagarProveedores);
            }
            catch { return false; }
        }

        private void CrearBotonAvisos()
        {
            if (btnAvisos != null || !VeAvisos()) return;

            btnAvisos = new Button
            {
                Size = new Size(40, 34),
                Margin = new Padding(0, 0, 10, 0),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TabStop = false,
                Text = ""
            };
            TematizarBotonAvisos();
            btnAvisos.Click += (s, e) => { ActualizarAvisos(); MostrarAvisos(); };
            btnAvisos.Paint += DibujarInsignia;

            cmsAvisos = new ContextMenuStrip { ShowImageMargin = false, Padding = new Padding(4) };

            flpTopActions.Controls.Add(btnAvisos);
            flpTopActions.Controls.SetChildIndex(btnAvisos, 0);
            ActualizarTooltipAvisos();
        }

        private void TematizarBotonAvisos()
        {
            if (btnAvisos == null) return;
            ConfigurarBotonTopbar(btnAvisos, Tema.FondoElevado, Tema.Acero700);
            btnAvisos.ForeColor = Tema.EsOscuro ? Tema.Acero200 : Tema.Acero700;
            btnAvisos.Font = new Font("Segoe MDL2 Assets", 12f);
            if (cmsAvisos != null)
            {
                cmsAvisos.BackColor = Tema.FondoElevado;
                cmsAvisos.ForeColor = Tema.Texto;
            }
            btnAvisos.Invalidate();
        }

        private void ActualizarTooltipAvisos()
        {
            if (btnAvisos == null) return;
            var t = GestorIdioma06AV.Instancia;
            int total = _avisos.Count;
            toolTipMain.SetToolTip(btnAvisos, total == 0 ? t.Obtener("aviso_ninguno") : t.Obtener("aviso_tooltip", total));
        }

        private void IniciarAvisos()
        {
            if (btnAvisos == null) return;
            _timerAvisos = new Timer { Interval = IntervaloAvisosMs };
            _timerAvisos.Tick += (s, e) => ActualizarAvisos();
            _timerAvisos.Start();
            FormClosed += (s, e) => { _timerAvisos.Stop(); _timerAvisos.Dispose(); };

            Shown += (s, e) =>
            {
                ActualizarAvisos();
                if (_avisos.Count > 0) BeginInvoke((Action)MostrarAvisos);
            };
        }

        private void ActualizarAvisos()
        {
            if (btnAvisos == null) return;
            try { _avisos = _avisosBLL.Obtener() ?? new List<Aviso06AV>(); }
            catch { _avisos = new List<Aviso06AV>(); }
            ActualizarTooltipAvisos();
            btnAvisos.Invalidate();
        }

        private void DibujarInsignia(object sender, PaintEventArgs e)
        {
            int n = _avisos.Count;
            if (n == 0) return;
            string texto = n > 9 ? "9+" : n.ToString();
            bool urgente = _avisos.Any(a => a.Urgente);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var fuente = new Font("Segoe UI", 6.75f, FontStyle.Bold))
            {
                Size medida = TextRenderer.MeasureText(texto, fuente);
                int ancho = Math.Max(15, medida.Width - 2);
                var r = new Rectangle(btnAvisos.Width - ancho - 2, 1, ancho, 15);
                using (var b = new SolidBrush(urgente ? Tema.Peligro : Tema.Acento))
                    g.FillEllipse(b, r);
                TextRenderer.DrawText(g, texto, fuente, r, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }

        private void MostrarAvisos()
        {
            if (btnAvisos == null || cmsAvisos == null || !btnAvisos.IsHandleCreated) return;
            var t = GestorIdioma06AV.Instancia;

            foreach (ToolStripItem i in cmsAvisos.Items.Cast<ToolStripItem>().ToList()) i.Dispose();
            cmsAvisos.Items.Clear();
            cmsAvisos.BackColor = Tema.FondoElevado;

            var titulo = new ToolStripLabel(t.Obtener("aviso_titulo").ToUpper())
            {
                Font = new Font("Segoe UI Semibold", 8.25f, FontStyle.Bold),
                ForeColor = Tema.Primario,
                Margin = new Padding(6, 4, 6, 4)
            };
            cmsAvisos.Items.Add(titulo);

            if (_avisos.Count == 0)
            {
                cmsAvisos.Items.Add(new ToolStripLabel("✓  " + t.Obtener("aviso_ninguno"))
                {
                    ForeColor = Tema.Exito, Font = Tema.FuenteRegular, Margin = new Padding(6, 2, 16, 6)
                });
            }

            foreach (Aviso06AV a in _avisos)
            {
                var aviso = a;
                var item = new ToolStripMenuItem((a.Urgente ? "●  " : "○  ") + t.Obtener("aviso_" + a.Tipo, a.Cantidad))
                {
                    Font = Tema.FuenteBold,
                    ForeColor = a.Urgente ? Tema.Peligro : Tema.Texto,
                    ToolTipText = t.Obtener("aviso_ir_" + a.Tipo)
                };
                item.Click += (s, e) => Navegar(aviso.Tipo);
                cmsAvisos.Items.Add(item);

                foreach (string d in a.Detalle.Take(DetallesPorAviso))
                    cmsAvisos.Items.Add(new ToolStripLabel("      " + d)
                    {
                        Font = new Font("Segoe UI", 8.5f), ForeColor = Tema.TextoSuave, Margin = new Padding(6, 0, 16, 0)
                    });
                if (a.Cantidad > DetallesPorAviso)
                    cmsAvisos.Items.Add(new ToolStripLabel("      " + t.Obtener("aviso_y_mas", a.Cantidad - DetallesPorAviso))
                    {
                        Font = new Font("Segoe UI", 8.25f, FontStyle.Italic), ForeColor = Tema.TextoSuave, Margin = new Padding(6, 0, 16, 2)
                    });
            }

            cmsAvisos.Items.Add(new ToolStripSeparator());
            var refrescar = new ToolStripMenuItem("⟳  " + t.Obtener("pcf_actualizar")) { ForeColor = Tema.TextoSuave, Font = Tema.FuenteRegular };
            refrescar.Click += (s, e) => { ActualizarAvisos(); BeginInvoke((Action)MostrarAvisos); };
            cmsAvisos.Items.Add(refrescar);

            cmsAvisos.Show(btnAvisos, new Point(btnAvisos.Width, btnAvisos.Height + 2), ToolStripDropDownDirection.BelowLeft);
        }

        private void Navegar(TipoAviso06AV tipo)
        {
            string destino = tipo == TipoAviso06AV.CotizacionesPorAprobar ? "pcf_menu_cotizaciones" : "pcf_menu_compras";
            if (!_navegacion.TryGetValue(destino, out Action ir) && !_navegacion.TryGetValue("pcf_menu_compras", out ir)) return;
            ir();
        }
    }
}
