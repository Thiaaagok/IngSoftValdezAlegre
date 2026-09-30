using PCFORGE_ValdezThiago_96VA.Common;
using SER;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA.UI
{
    /// <summary>
    /// Carga SIMULADA de los datos de una tarjeta de crédito o débito para completar un cobro
    /// (seña o saldo final). Nada de lo que se escribe acá se persiste: los datos no viajan a
    /// la BLL ni a la base. Solo se validan en memoria (16 dígitos + dígito de control de Luhn,
    /// nombre y código de seguridad), se "autoriza" el pago con una espera simulada y después
    /// se descartan con Limpiar().
    ///
    /// Números de prueba: 4111 1111 1111 1111 (Visa) o 5555 5555 5555 4444 (Mastercard) se
    /// aprueban; 4000 0000 0000 0002 simula una tarjeta rechazada.
    /// </summary>
    [DesignerCategory("Code")]
    internal class DatosTarjetaControl06AV : Panel
    {
        private const string NumeroRechazado = "4000000000000002";

        private readonly Label lblTitulo, lblAviso, lblNumero, lblNombre, lblCodigo, lblMarca, lblCodigoAyuda;
        private readonly TextBox txtNumero, txtNombre, txtCodigo;
        private bool _formateando;
        private bool _credito = true;

        public DatosTarjetaControl06AV()
        {
            Dock = DockStyle.Top;
            Height = 200;
            Padding = new Padding(16, 10, 16, 8);
            ResizeRedraw = true;

            lblTitulo = new Label { Dock = DockStyle.Top, Height = 26, AutoSize = false };
            lblAviso = new Label { Dock = DockStyle.Top, Height = 22, AutoSize = false };

            txtNumero = new TextBox { Width = 230, MaxLength = 19 };
            txtNumero.KeyPress += SoloDigitos;
            txtNumero.TextChanged += (s, e) => FormatearNumero();
            lblMarca = new Label { AutoSize = true };

            txtNombre = new TextBox { Width = 320, MaxLength = 60, CharacterCasing = CharacterCasing.Upper };
            txtNombre.KeyPress += (s, e) =>
            {
                char c = e.KeyChar;
                if (!char.IsControl(c) && !char.IsLetter(c) && c != ' ' && c != '\'' && c != '-') e.Handled = true;
            };

            txtCodigo = new TextBox { Width = 70, MaxLength = 3, UseSystemPasswordChar = true };
            txtCodigo.KeyPress += SoloDigitos;
            lblCodigoAyuda = new Label { AutoSize = true };

            lblNumero = new Label();
            lblNombre = new Label();
            lblCodigo = new Label();

            var tabla = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2, Padding = new Padding(0, 2, 0, 0)
            };
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Fila(tabla, lblNumero, txtNumero, lblMarca);
            Fila(tabla, lblNombre, txtNombre, null);
            Fila(tabla, lblCodigo, txtCodigo, lblCodigoAyuda);

            Controls.Add(tabla);
            Controls.Add(lblAviso);
            Controls.Add(lblTitulo);

            AplicarTema();
            AplicarIdioma();
        }

        /// <summary>true = tarjeta de crédito; false = tarjeta de débito (solo cambia los textos).</summary>
        public bool Credito
        {
            get => _credito;
            set { _credito = value; AplicarIdioma(); }
        }

        private static void Fila(TableLayoutPanel t, Label etiqueta, Control campo, Label extra)
        {
            int fila = t.RowCount;
            t.RowCount = fila + 1;
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            etiqueta.AutoSize = true;
            etiqueta.Anchor = AnchorStyles.Left;
            etiqueta.Margin = new Padding(0, 9, 6, 3);

            var celda = new FlowLayoutPanel
            {
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                Margin = new Padding(0), Anchor = AnchorStyles.Left
            };
            campo.Margin = new Padding(0, 5, 0, 5);
            celda.Controls.Add(campo);
            if (extra != null)
            {
                extra.Margin = new Padding(10, 9, 0, 0);
                celda.Controls.Add(extra);
            }

            t.Controls.Add(etiqueta, 0, fila);
            t.Controls.Add(celda, 1, fila);
        }

        // ── Tema e idioma (los llama el control que lo contiene) ────────────────

        public void AplicarTema()
        {
            BackColor = Tema.FondoPanel;
            foreach (Control c in Controls) PintarRecursivo(c);

            lblTitulo.Font = Tema.FuenteSubtit;
            lblTitulo.ForeColor = Tema.TextoFuerte;
            lblAviso.Font = Tema.FuenteMini;
            lblAviso.ForeColor = Tema.TextoSuave;
            lblCodigoAyuda.ForeColor = Tema.TextoSuave;
            lblMarca.Font = Tema.FuenteBold;
            ActualizarMarca();
            Invalidate();
        }

        private static void PintarRecursivo(Control c)
        {
            if (c is TextBox) Tema.AplicarEntrada(c);
            else
            {
                c.BackColor = Tema.FondoPanel;
                c.ForeColor = Tema.Texto;
                c.Font = Tema.FuenteRegular;
            }
            foreach (Control h in c.Controls) PintarRecursivo(h);
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            lblTitulo.Text = t.Obtener(_credito ? "pcf_tj_titulo_credito" : "pcf_tj_titulo_debito");
            lblAviso.Text = t.Obtener("pcf_tj_aviso");
            lblNumero.Text = t.Obtener("pcf_tj_numero") + ":";
            lblNombre.Text = t.Obtener("pcf_tj_nombre") + ":";
            lblCodigo.Text = t.Obtener("pcf_tj_codigo") + ":";
            lblCodigoAyuda.Text = t.Obtener("pcf_tj_codigo_ayuda");
            ActualizarMarca();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(Tema.Borde))
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            using (var br = new SolidBrush(Tema.Primario))
                e.Graphics.FillRectangle(br, 0, 0, 4, Height);
        }

        // ── Entrada ───────────────────────────────────────────────────────────

        private static void SoloDigitos(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true;
        }

        private string Digitos() => new string(txtNumero.Text.Where(char.IsDigit).ToArray());

        /// <summary>Muestra el número en grupos de 4 ("4111 1111 1111 1111").</summary>
        private void FormatearNumero()
        {
            if (_formateando) return;
            _formateando = true;
            try
            {
                string d = Digitos();
                if (d.Length > 16) d = d.Substring(0, 16);
                string formateado = string.Join(" ",
                    Enumerable.Range(0, (d.Length + 3) / 4)
                              .Select(i => d.Substring(i * 4, Math.Min(4, d.Length - i * 4))));
                if (txtNumero.Text != formateado)
                {
                    txtNumero.Text = formateado;
                    txtNumero.SelectionStart = formateado.Length;
                }
            }
            finally { _formateando = false; }
            ActualizarMarca();
        }

        /// <summary>
        /// Indicador en vivo al lado del número: mientras se escribe muestra la marca; con los
        /// 16 dígitos aplica Luhn y marca en verde (número válido) o en rojo (no existe).
        /// </summary>
        private void ActualizarMarca()
        {
            if (lblMarca == null) return;
            var t = GestorIdioma06AV.Instancia;
            string d = Digitos();
            string marca = Marca(d);

            if (d.Length < 16)
            {
                lblMarca.Text = marca;
                lblMarca.ForeColor = Tema.Primario;
            }
            else if (!CumpleLuhn(d))
            {
                lblMarca.Text = "✖ " + t.Obtener("pcf_tj_luhn_mal");
                lblMarca.ForeColor = Tema.Peligro;
            }
            else if (string.IsNullOrEmpty(marca))
            {
                lblMarca.Text = "✖ " + t.Obtener("pcf_tj_marca_desconocida");
                lblMarca.ForeColor = Tema.Peligro;
            }
            else
            {
                lblMarca.Text = marca + "  ✔ " + t.Obtener("pcf_tj_luhn_ok");
                lblMarca.ForeColor = Tema.Exito;
            }
        }

        private static string Marca(string d)
        {
            if (string.IsNullOrEmpty(d)) return string.Empty;
            if (d.StartsWith("4")) return "VISA";
            if (d.Length >= 2)
            {
                int dos = int.Parse(d.Substring(0, 2));
                if (dos >= 51 && dos <= 55) return "MASTERCARD";
            }
            if (d.Length >= 4)
            {
                int cuatro = int.Parse(d.Substring(0, 4));
                if (cuatro >= 2221 && cuatro <= 2720) return "MASTERCARD";
            }
            if (d.StartsWith("50") || d.StartsWith("58")) return "MAESTRO";
            return string.Empty;
        }

        /// <summary>
        /// Algoritmo de Luhn (módulo 10, ISO/IEC 7812): el último dígito de toda tarjeta es un
        /// dígito de control calculado sobre los anteriores, así que un número inventado o mal
        /// tipeado casi nunca lo cumple.
        ///   1. Se recorre el número de derecha a izquierda.
        ///   2. Se duplica un dígito sí y uno no, empezando por el segundo desde la derecha.
        ///   3. Si al duplicar da más de 9, se le resta 9 (equivale a sumar sus dos cifras).
        ///   4. Se suman todos los dígitos: el número es válido si la suma es múltiplo de 10.
        /// Ej.: 4111 1111 1111 1111 → suma 30 → válido; cambiando el último 1 por 2 → inválido.
        /// </summary>
        internal static bool CumpleLuhn(string d)
        {
            if (string.IsNullOrEmpty(d) || !d.All(char.IsDigit)) return false;

            int suma = 0;
            bool doblar = false;
            for (int i = d.Length - 1; i >= 0; i--)
            {
                int n = d[i] - '0';
                if (doblar) { n *= 2; if (n > 9) n -= 9; }
                suma += n;
                doblar = !doblar;
            }
            return suma % 10 == 0;
        }

        // ── Validación y autorización simulada ──────────────────────────────────

        public void Limpiar()
        {
            txtNumero.Clear();
            txtNombre.Clear();
            txtCodigo.Clear();
            ActualizarMarca();
        }

        public bool Validar(out string error)
        {
            var t = GestorIdioma06AV.Instancia;
            string d = Digitos();

            if (d.Length != 16)
            { error = t.Obtener("pcf_tj_err_numero"); txtNumero.Focus(); return false; }
            if (!CumpleLuhn(d))
            { error = t.Obtener("pcf_tj_err_luhn"); txtNumero.Focus(); return false; }
            if (string.IsNullOrEmpty(Marca(d)))
            { error = t.Obtener("pcf_tj_err_marca"); txtNumero.Focus(); return false; }

            string nombre = txtNombre.Text.Trim();
            if (nombre.Length < 5 || !nombre.Contains(" "))
            { error = t.Obtener("pcf_tj_err_nombre"); txtNombre.Focus(); return false; }

            if (txtCodigo.Text.Length != 3 || !txtCodigo.Text.All(char.IsDigit))
            { error = t.Obtener("pcf_tj_err_codigo"); txtCodigo.Focus(); return false; }

            error = null;
            return true;
        }

        /// <summary>
        /// Simula el envío al procesador de pagos. Devuelve el código de autorización, o null si
        /// la tarjeta fue rechazada (en ese caso ya se le avisó al usuario).
        /// </summary>
        public string SimularAutorizacion(IWin32Window owner, decimal monto)
        {
            var t = GestorIdioma06AV.Instancia;
            string d = Digitos();
            string marca = Marca(d);
            string resumen = t.Obtener(_credito ? "pcf_fp_tarjeta" : "pcf_fp_tarjeta_debito") + "  ·  " +
                             (string.IsNullOrEmpty(marca) ? "" : marca + " ") + "•••• " + d.Substring(d.Length - 4) +
                             "  ·  " + monto.ToString("C2");

            using (var dlg = new FRMProcesandoPago06AV(t.Obtener("pcf_tj_procesando"), resumen))
                dlg.ShowDialog(owner);

            if (d == NumeroRechazado)
            {
                ConfirmacionForm.MostrarInfo(t.Obtener("pcf_tj_rechazada"), t.Obtener("pcf_tj_titulo_rechazo"),
                                             ConfirmacionForm.TipoConfirmacion.Error, owner);
                txtNumero.Focus();
                return null;
            }

            byte[] b = new byte[4];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(b);
            return (BitConverter.ToUInt32(b, 0) % 1000000).ToString("000000");
        }

        /// <summary>Ventanita de "procesando" con una espera fija: solo es escenografía.</summary>
        [DesignerCategory("Code")]
        private sealed class FRMProcesandoPago06AV : Form
        {
            private readonly Timer _timer = new Timer { Interval = 1800 };

            public FRMProcesandoPago06AV(string titulo, string detalle)
            {
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                ControlBox = false;
                ShowInTaskbar = false;
                ClientSize = new Size(420, 130);
                Text = titulo;

                var lblTit = new Label
                {
                    Text = titulo, Dock = DockStyle.Top, Height = 34, AutoSize = false,
                    TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 8, 0, 0)
                };
                var lblDet = new Label
                {
                    Text = detalle, Dock = DockStyle.Top, Height = 28, AutoSize = false,
                    TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 0, 0)
                };
                var barra = new ProgressBar
                {
                    Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 25,
                    Location = new Point(18, 80), Size = new Size(384, 16)
                };

                Controls.Add(barra);
                Controls.Add(lblDet);
                Controls.Add(lblTit);

                Tema.AplicarFormulario(this);
                lblTit.Font = Tema.FuenteSubtit;
                lblTit.ForeColor = Tema.TextoFuerte;
                lblDet.ForeColor = Tema.TextoSuave;

                _timer.Tick += (s, e) => { _timer.Stop(); DialogResult = DialogResult.OK; Close(); };
                Shown += (s, e) => _timer.Start();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) _timer.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}
