using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre
{
    /// <summary>
    /// Identidad visual centralizada para PC Factory.
    /// Paleta: grafito industrial, cian tecnologico y naranja de produccion.
    /// Soporta modo claro y modo oscuro via EsOscuro / ToggleTema().
    /// </summary>
    internal static class Tema
    {
        // ── Estado del tema ──────────────────────────────────────────────────
        private static bool _oscuro = false;
        public static bool EsOscuro => _oscuro;

        public static void ToggleTema() => _oscuro = !_oscuro;

        // ── Paleta primitiva (fija) ──────────────────────────────────────────
        // Neutros
        public static readonly Color Grafito950 = Color.FromArgb(2, 6, 23);
        public static readonly Color Grafito900 = Color.FromArgb(15, 23, 42);
        public static readonly Color Grafito800 = Color.FromArgb(30, 41, 59);
        public static readonly Color Acero700   = Color.FromArgb(51, 65, 85);
        public static readonly Color Acero500   = Color.FromArgb(100, 116, 139);
        public static readonly Color Acero300   = Color.FromArgb(203, 213, 225);
        public static readonly Color Acero200   = Color.FromArgb(226, 232, 240);
        public static readonly Color Acero100   = Color.FromArgb(241, 245, 249);
        public static readonly Color Acero50    = Color.FromArgb(248, 250, 252);

        // Marca / tecnologia
        public static readonly Color Cian50  = Color.FromArgb(236, 254, 255);
        public static readonly Color Cian100 = Color.FromArgb(207, 250, 254);
        public static readonly Color Cian400 = Color.FromArgb(34, 211, 238);
        public static readonly Color Cian600 = Color.FromArgb(8, 145, 178);
        public static readonly Color Cian700 = Color.FromArgb(14, 116, 144);

        // Produccion
        public static readonly Color Naranja50  = Color.FromArgb(255, 247, 237);
        public static readonly Color Naranja500 = Color.FromArgb(249, 115, 22);
        public static readonly Color Naranja600 = Color.FromArgb(234, 88, 12);

        // Estados
        public static readonly Color Verde50  = Color.FromArgb(240, 253, 244);
        public static readonly Color Verde600 = Color.FromArgb(22, 163, 74);
        public static readonly Color Verde700 = Color.FromArgb(21, 128, 61);
        public static readonly Color Amber50  = Color.FromArgb(255, 251, 235);
        public static readonly Color Amber500 = Color.FromArgb(245, 158, 11);
        public static readonly Color Rojo50   = Color.FromArgb(254, 242, 242);
        public static readonly Color Rojo600  = Color.FromArgb(220, 38, 38);
        public static readonly Color Rojo700  = Color.FromArgb(185, 28, 28);

        // Oscuro — fondos alternativos para estado/advertencia en dark mode
        private static readonly Color OscuroAmber  = Color.FromArgb(45, 35, 5);
        private static readonly Color OscuroVerde  = Color.FromArgb(5, 40, 20);
        private static readonly Color OscuroRojo   = Color.FromArgb(50, 10, 10);
        private static readonly Color OscuroCian   = Color.FromArgb(5, 35, 45);

        // Alias de compatibilidad
        public static readonly Color Azul50  = Cian50;
        public static readonly Color Azul400 = Cian400;
        public static readonly Color Azul800 = Cian700;
        public static readonly Color Verde400 = Color.FromArgb(74, 222, 128);
        public static readonly Color Verde800 = Verde700;
        public static readonly Color Gris50   = Acero50;
        public static readonly Color Gris400  = Acero300;
        public static readonly Color Gris900  = Grafito900;
        public static readonly Color Amber400 = Amber500;

        // ── Colores semánticos dinámicos ─────────────────────────────────────
        public static Color FondoApp      => _oscuro ? Grafito900            : Acero100;
        public static Color FondoPanel    => _oscuro ? Grafito800            : Color.White;
        public static Color FondoElevado  => _oscuro ? Grafito800            : Color.White;
        public static Color Texto         => _oscuro ? Acero200              : Grafito900;
        public static Color TextoFuerte   => _oscuro ? Color.White           : Grafito950;
        public static Color TextoSuave    => _oscuro ? Acero500              : Acero500;
        public static Color TextoInvertido => Color.White;
        public static Color Borde         => _oscuro ? Acero700              : Acero200;
        public static Color Primario      => _oscuro ? Cian400               : Cian600;
        public static Color PrimarioHover => _oscuro ? Cian600               : Cian700;
        public static Color PrimarioSuave => _oscuro ? OscuroCian            : Cian50;
        public static Color Acento        => Naranja500;
        public static Color AcentoHover   => Naranja600;
        public static Color AcentoSuave   => _oscuro ? OscuroAmber           : Naranja50;
        public static Color Exito         => _oscuro ? Verde600              : Verde700;
        public static Color ExitoSuave    => _oscuro ? OscuroVerde           : Verde50;
        public static Color Advertencia   => Amber500;
        public static Color AdvertenciaSuave => _oscuro ? OscuroAmber        : Amber50;
        public static Color Peligro       => _oscuro ? Color.FromArgb(248, 113, 113) : Rojo600;
        public static Color PeligroHover  => _oscuro ? Rojo600               : Rojo700;
        public static Color PeligroSuave  => _oscuro ? OscuroRojo            : Rojo50;
        public static Color Seleccion     => _oscuro ? Acero700              : Cian100;
        public static Color FondoCabecera => _oscuro ? Acero700              : Grafito900;

        // ── Tipografias (fijas) ──────────────────────────────────────────────
        public static readonly Font FuenteRegular = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        public static readonly Font FuenteBold    = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
        public static readonly Font FuenteTitulo  = new Font("Segoe UI Semibold", 15f, FontStyle.Bold);
        public static readonly Font FuenteSubtit  = new Font("Segoe UI Semibold", 11f, FontStyle.Bold);
        public static readonly Font FuenteMenu    = new Font("Segoe UI", 10.5f, FontStyle.Regular);
        public static readonly Font FuenteMini    = new Font("Segoe UI Semibold", 8.25f, FontStyle.Bold);

        // ── Métodos de aplicación ────────────────────────────────────────────
        public static void AplicarFormulario(Form form)
        {
            form.BackColor = FondoApp;
            form.Font = FuenteRegular;
            Aplicar(form);
        }

        public static void AplicarControl(UserControl control)
        {
            control.BackColor = FondoApp;
            control.Font = FuenteRegular;
            Aplicar(control);
        }

        public static void Aplicar(Control root)
        {
            foreach (Control control in root.Controls)
            {
                AplicarControlBasico(control);
                Aplicar(control);
            }
        }

        public static void AplicarPanelContenedor(Control control)
        {
            control.BackColor = FondoPanel;
            control.ForeColor = Texto;
        }

        public static void AplicarTitulo(Label label)
        {
            label.Font = FuenteTitulo;
            label.ForeColor = TextoFuerte;
            label.BackColor = Color.Transparent;
        }

        public static void AplicarSubtitulo(Label label)
        {
            label.Font = FuenteSubtit;
            label.ForeColor = Texto;
            label.BackColor = Color.Transparent;
        }

        public static void AplicarBotonPrimario(Button button)
        {
            AplicarBoton(button, Primario, TextoInvertido, PrimarioHover);
            button.FlatAppearance.BorderColor = Primario;
        }

        public static void AplicarBotonAcento(Button button)
        {
            AplicarBoton(button, Acento, TextoInvertido, AcentoHover);
            button.FlatAppearance.BorderColor = Acento;
        }

        public static void AplicarBotonPeligro(Button button)
        {
            AplicarBoton(button, Peligro, TextoInvertido, PeligroHover);
            button.FlatAppearance.BorderColor = Peligro;
        }

        public static void AplicarBotonSecundario(Button button)
        {
            AplicarBoton(button, FondoPanel, Texto, _oscuro ? Acero700 : Acero100);
            button.FlatAppearance.BorderColor = Borde;
        }

        public static void AplicarBotonDeshabilitado(Button button)
        {
            button.BackColor = _oscuro ? Acero700 : Acero200;
            button.ForeColor = Acero500;
            button.FlatAppearance.BorderColor = _oscuro ? Acero700 : Acero200;
        }

        public static void AplicarGrilla(DataGridView grid)
        {
            grid.BackgroundColor = FondoPanel;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.GridColor = Borde;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            grid.ColumnHeadersDefaultCellStyle.BackColor = FondoCabecera;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextoInvertido;
            grid.ColumnHeadersDefaultCellStyle.Font = FuenteBold;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = FondoCabecera;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextoInvertido;

            grid.DefaultCellStyle.BackColor = FondoPanel;
            grid.DefaultCellStyle.ForeColor = Texto;
            grid.DefaultCellStyle.Font = FuenteRegular;
            grid.DefaultCellStyle.SelectionBackColor = Seleccion;
            grid.DefaultCellStyle.SelectionForeColor = TextoFuerte;
            grid.AlternatingRowsDefaultCellStyle.BackColor = _oscuro ? Grafito900 : Acero50;
            grid.AlternatingRowsDefaultCellStyle.ForeColor = Texto;
        }

        public static void AplicarEntrada(Control control)
        {
            control.BackColor = FondoPanel;
            control.ForeColor = Texto;
            control.Font = FuenteRegular;
        }

        private static void AplicarControlBasico(Control control)
        {
            if (control is Button button)
            {
                AplicarBotonPrimario(button);
                return;
            }

            if (control is DataGridView grid)
            {
                AplicarGrilla(grid);
                return;
            }

            if (control is TextBox || control is ComboBox || control is DateTimePicker)
            {
                AplicarEntrada(control);
                return;
            }

            if (control is CheckBox || control is RadioButton)
            {
                control.BackColor = Color.Transparent;
                control.ForeColor = Texto;
                control.Font = FuenteRegular;
                return;
            }

            if (control is Label label)
            {
                label.ForeColor = Texto;
                label.Font = FuenteRegular;
                return;
            }

            if (control is Panel || control is GroupBox ||
                control is FlowLayoutPanel || control is TableLayoutPanel)
            {
                control.BackColor = FondoPanel;
                control.ForeColor = Texto;
            }
        }

        private static void AplicarBoton(Button button, Color fondo, Color texto, Color hover)
        {
            button.BackColor = fondo;
            button.ForeColor = texto;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = hover;
            button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(hover, 0.05f);
            button.Font = FuenteBold;
            button.UseVisualStyleBackColor = false;
            button.Cursor = Cursors.Hand;
        }
    }
}
