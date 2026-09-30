using BE;
using SER;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA.UI
{
    /// <summary>
    /// Ficha del cliente de la venta (CU "Registrar venta", paso 2 / alternativo 2.1).
    /// Tres estados:
    ///   • Vacía: invita a buscar por DNI.
    ///   • Encontrado: tarjeta con avatar de iniciales, nombre, DNI, teléfono, dirección
    ///     y la insignia "Cliente registrado".
    ///   • No encontrado: aviso con borde punteado; un clic pide el alta del cliente.
    /// </summary>
    [DesignerCategory("Code")]
    internal class FichaCliente06AV : Control
    {
        private enum Estado { Vacia, Encontrado, NoEncontrado }

        private Estado _estado = Estado.Vacia;
        private Cliente06AV _cliente;
        private string _dni;
        private bool _hot;

        private static readonly Font FuenteNombre = new Font("Segoe UI Semibold", 12.5f, FontStyle.Bold);
        private static readonly Font FuenteIniciales = new Font("Segoe UI Semibold", 13f, FontStyle.Bold);

        /// <summary>Se dispara al hacer clic en la ficha de "no encontrado".</summary>
        public event EventHandler RegistrarSolicitado;

        public FichaCliente06AV()
        {
            SetStyle(Pintura06AV.EstilosDibujo, true);
            Dock = DockStyle.Top;
            Height = 96;
        }

        public void MostrarVacia() { _estado = Estado.Vacia; _cliente = null; _dni = null; Actualizar(); }

        public void MostrarCliente(Cliente06AV cliente)
        {
            if (cliente == null) { MostrarVacia(); return; }
            _estado = Estado.Encontrado; _cliente = cliente; _dni = cliente.Dni; Actualizar();
        }

        public void MostrarNoEncontrado(string dni)
        {
            _estado = Estado.NoEncontrado; _cliente = null; _dni = dni; Actualizar();
        }

        private void Actualizar()
        {
            Cursor = _estado == Estado.NoEncontrado ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hot = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hot = false; Invalidate(); }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (_estado == Estado.NoEncontrado && e.Button == MouseButtons.Left)
                RegistrarSolicitado?.Invoke(this, EventArgs.Empty);
        }

        // ── Dibujo ────────────────────────────────────────────────────────────

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pintura06AV.Calidad(g);
            using (var b = new SolidBrush(Parent != null ? Parent.BackColor : Tema.FondoApp))
                g.FillRectangle(b, ClientRectangle);

            var caja = new Rectangle(3, 6, Math.Min(Width - 8, 680), Height - 14);
            if (caja.Width <= 40 || caja.Height <= 20) return;

            switch (_estado)
            {
                case Estado.Encontrado: DibujarEncontrado(g, caja); break;
                case Estado.NoEncontrado: DibujarNoEncontrado(g, caja); break;
                default: DibujarVacia(g, caja); break;
            }
        }

        private void DibujarEncontrado(Graphics g, Rectangle caja)
        {
            var t = GestorIdioma06AV.Instancia;
            var c = _cliente;

            Pintura06AV.Sombra(g, caja, Pintura06AV.RadioTarjeta, 4, 16);
            Pintura06AV.Rellenar(g, caja, Pintura06AV.RadioTarjeta, Tema.FondoPanel);
            Pintura06AV.Borde(g, caja, Pintura06AV.RadioTarjeta, Pintura06AV.Mezclar(Tema.Borde, Tema.Exito, 0.45f));

            // franja de acento a la izquierda
            using (var clip = Pintura06AV.Redondeado(caja, Pintura06AV.RadioTarjeta))
            {
                Region anterior = g.Clip;
                g.SetClip(clip, CombineMode.Intersect);
                using (var b = new SolidBrush(Tema.Exito)) g.FillRectangle(b, caja.X, caja.Y, 5, caja.Height);
                g.Clip = anterior;
            }

            // avatar con iniciales
            var avatar = new Rectangle(caja.X + 20, caja.Y + (caja.Height - 50) / 2, 50, 50);
            using (var b = new LinearGradientBrush(avatar, Tema.Primario,
                                                   Pintura06AV.Mezclar(Tema.Primario, Tema.Grafito900, 0.35f), 45f))
                g.FillEllipse(b, avatar);
            Pintura06AV.TextoCentrado(g, Iniciales(c), FuenteIniciales, Color.White, avatar);

            int x = avatar.Right + 16;

            // insignia "Cliente registrado" arriba a la derecha
            string insignia = t.Obtener("pcf_venta_cliente_encontrado");
            int wChip = Pintura06AV.AnchoChip(g, insignia, Tema.FuenteMini, 34);
            var chip = new Rectangle(caja.Right - 14 - wChip, caja.Y + 12, wChip, 22);
            Pintura06AV.Chip(g, chip, "", Tema.FuenteMini, Pintura06AV.Suave(Tema.Exito, 36), Tema.Exito);
            Iconos06AV.Dibujar(g, IconoPcf06AV.Check, new RectangleF(chip.X + 8, chip.Y + 4, 14, 14), Tema.Exito, 2f);
            Pintura06AV.TextoIzquierda(g, insignia, Tema.FuenteMini, Tema.Exito,
                                       new Rectangle(chip.X + 24, chip.Y, chip.Width - 26, chip.Height));

            // nombre
            string nombre = ((c.Nombre ?? "") + " " + (c.Apellido ?? "")).Trim();
            Pintura06AV.TextoIzquierda(g, nombre, FuenteNombre, Tema.TextoFuerte,
                                       new Rectangle(x, caja.Y + 10, Math.Max(40, chip.X - x - 10), 28));

            // DNI como chip + teléfono y dirección
            string dni = t.Obtener("dni") + " " + FormatearDni(c.Dni);
            int wDni = Pintura06AV.AnchoChip(g, dni, Tema.FuenteBold, 18);
            var chipDni = new Rectangle(x, caja.Y + 44, wDni, 24);
            Pintura06AV.Chip(g, chipDni, dni, Tema.FuenteBold, Pintura06AV.Suave(Tema.Primario, 30), Tema.Primario);

            string extra = string.Join("   ·   ",
                new[] { c.Telefono, c.Direccion }.Where(s => !string.IsNullOrWhiteSpace(s)));
            if (extra.Length > 0)
                Pintura06AV.TextoIzquierda(g, extra, Tema.FuenteRegular, Tema.TextoSuave,
                    new Rectangle(chipDni.Right + 12, chipDni.Y, Math.Max(20, caja.Right - chipDni.Right - 26), chipDni.Height));
        }

        private void DibujarNoEncontrado(Graphics g, Rectangle caja)
        {
            var t = GestorIdioma06AV.Instancia;

            Color fondo = Pintura06AV.Mezclar(Tema.FondoPanel, Tema.Peligro, _hot ? 0.10f : 0.05f);
            Pintura06AV.Rellenar(g, caja, Pintura06AV.RadioTarjeta, fondo);
            BordePunteado(g, caja, Tema.Peligro);

            var icono = new Rectangle(caja.X + 20, caja.Y + (caja.Height - 44) / 2, 44, 44);
            using (var b = new SolidBrush(Pintura06AV.Suave(Tema.Peligro, 40))) g.FillEllipse(b, icono);
            Iconos06AV.Dibujar(g, IconoPcf06AV.Alerta, new RectangleF(icono.X + 11, icono.Y + 11, 22, 22), Tema.Peligro, 2f);

            int x = icono.Right + 16;

            // botón visual "+ Registrar cliente"
            string accion = "＋ " + t.Obtener("pcf_registrar_cliente");
            int wAcc = Pintura06AV.AnchoChip(g, accion, Tema.FuenteBold, 28);
            var btn = new Rectangle(caja.Right - 16 - wAcc, caja.Y + (caja.Height - 30) / 2, wAcc, 30);
            Pintura06AV.Rellenar(g, btn, 8, _hot ? Tema.PrimarioHover : Tema.Primario);
            Pintura06AV.TextoCentrado(g, accion, Tema.FuenteBold, Color.White, btn);

            int ancho = Math.Max(40, btn.X - x - 12);
            Pintura06AV.TextoIzquierda(g, t.Obtener("pcf_venta_cliente_no_existe_tit", FormatearDni(_dni)),
                                       Tema.FuenteSubtit, Tema.TextoFuerte, new Rectangle(x, caja.Y + 14, ancho, 26));
            Pintura06AV.TextoIzquierda(g, t.Obtener("pcf_venta_cliente_no_existe_det"),
                                       Tema.FuenteRegular, Tema.TextoSuave, new Rectangle(x, caja.Y + 42, ancho, 22));
        }

        private void DibujarVacia(Graphics g, Rectangle caja)
        {
            var t = GestorIdioma06AV.Instancia;

            Pintura06AV.Rellenar(g, caja, Pintura06AV.RadioTarjeta,
                                 Pintura06AV.Mezclar(Tema.FondoApp, Tema.FondoPanel, 0.5f));
            BordePunteado(g, caja, Tema.EsOscuro ? Tema.Acero700 : Tema.Acero300);

            var icono = new Rectangle(caja.X + 20, caja.Y + (caja.Height - 44) / 2, 44, 44);
            using (var b = new SolidBrush(Pintura06AV.Suave(Tema.Primario, 26))) g.FillEllipse(b, icono);
            Iconos06AV.Dibujar(g, IconoPcf06AV.Lupa, new RectangleF(icono.X + 11, icono.Y + 11, 22, 22), Tema.Primario, 2f);

            int x = icono.Right + 16;
            int ancho = Math.Max(40, caja.Right - x - 16);
            Pintura06AV.TextoIzquierda(g, t.Obtener("pcf_venta_cliente_vacio_tit"),
                                       Tema.FuenteBold, Tema.Texto, new Rectangle(x, caja.Y + 16, ancho, 22));
            Pintura06AV.TextoIzquierda(g, t.Obtener("pcf_venta_cliente_hint"),
                                       Tema.FuenteRegular, Tema.TextoSuave, new Rectangle(x, caja.Y + 40, ancho, 22));
        }

        private static void BordePunteado(Graphics g, Rectangle caja, Color color)
        {
            var rr = new Rectangle(caja.X, caja.Y, caja.Width - 1, caja.Height - 1);
            using (var path = Pintura06AV.Redondeado(rr, Pintura06AV.RadioTarjeta))
            using (var p = new Pen(color, 1.4f) { DashStyle = DashStyle.Dash })
                g.DrawPath(p, path);
        }

        // ── Formato ───────────────────────────────────────────────────────────

        private static string Iniciales(Cliente06AV c)
        {
            char a = string.IsNullOrWhiteSpace(c.Nombre) ? ' ' : char.ToUpper(c.Nombre.Trim()[0]);
            char b = string.IsNullOrWhiteSpace(c.Apellido) ? ' ' : char.ToUpper(c.Apellido.Trim()[0]);
            return (a.ToString() + b).Trim();
        }

        /// <summary>34345678 → 34.345.678</summary>
        private static string FormatearDni(string dni)
        {
            if (string.IsNullOrEmpty(dni) || !dni.All(char.IsDigit) || dni.Length < 7) return dni ?? "";
            string r = "";
            int n = 0;
            for (int i = dni.Length - 1; i >= 0; i--)
            {
                r = dni[i] + r;
                if (++n % 3 == 0 && i > 0) r = "." + r;
            }
            return r;
        }
    }
}
