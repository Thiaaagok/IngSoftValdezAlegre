using BE;
using BLL;
using BLL.Armado;
using PCFORGE_ValdezThiago_96VA.Common;
using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA
{
    [System.ComponentModel.DesignerCategory("Code")]
    public class FRMEspecificacion06AV : Form
    {
        private readonly Componente06AV _componente;
        private readonly ComponentesBLL06AV _bll = new ComponentesBLL06AV();

        public EspecificacionComponente06AV Especificacion { get; private set; }

        private TableLayoutPanel tabla;
        private Label lblAyuda, lblEstimada;
        private ComboBox cboPlataforma, cboTipoMemoria, cboFormato;
        private TextBox txtSocket, txtFrecuencia, txtCapacidad, txtSlots, txtMemoriaMax, txtConsumo, txtPotencia, txtTdp;
        private CheckBox chkGraficos, chkDesbloqueado, chkOverclock;
        private Button btnGuardar, btnCancelar, btnSugerir;

        public FRMEspecificacion06AV(Componente06AV componente, EspecificacionComponente06AV actual)
        {
            _componente = componente ?? new Componente06AV();
            ConstruirUI();
            Tema.AplicarFormulario(this);
            Tema.AplicarBotonPrimario(btnGuardar);
            Tema.AplicarBotonSecundario(btnCancelar);
            Tema.AplicarBotonSecundario(btnSugerir);
            tabla.BackColor = BackColor;
            lblAyuda.ForeColor = Tema.TextoSuave;
            lblEstimada.ForeColor = Tema.EsOscuro ? Tema.Advertencia : Tema.Naranja600;
            lblEstimada.BackColor = Tema.AdvertenciaSuave;
            Cargar(actual ?? _bll.SugerirEspecificacion(_componente));
            lblEstimada.Visible = actual == null || actual.Estimada;
        }

        private void ConstruirUI()
        {
            var t = GestorIdioma06AV.Instancia;
            Text = t.Obtener("pcf_esp_titulo") + " · " + (_componente.Codigo ?? "");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            lblAyuda = new Label { AutoSize = true, MaximumSize = new Size(470, 0), Margin = new Padding(3, 0, 3, 6),
                                   Text = t.Obtener("pcf_esp_ayuda") };
            lblEstimada = new Label { AutoSize = true, MaximumSize = new Size(470, 0), Padding = new Padding(6, 4, 6, 4),
                                      Margin = new Padding(3, 0, 3, 8), Text = t.Obtener("pcf_esp_estimada") };

            cboPlataforma = Combo(new[] { "", ReglasCompatibilidad06AV.Amd, ReglasCompatibilidad06AV.Intel });
            cboTipoMemoria = Combo(new[] { "" }.Concat(ReglasCompatibilidad06AV.TiposMemoria));
            cboFormato = Combo(new[] { "" }.Concat(ReglasCompatibilidad06AV.Formatos));
            txtSocket = new TextBox { Width = 220 };
            txtFrecuencia = Numero(); txtCapacidad = Numero(); txtSlots = Numero(); txtMemoriaMax = Numero();
            txtConsumo = Numero(); txtPotencia = Numero(); txtTdp = Numero();
            chkGraficos = new CheckBox { AutoSize = true };
            chkDesbloqueado = new CheckBox { AutoSize = true };
            chkOverclock = new CheckBox { AutoSize = true };

            tabla = new TableLayoutPanel
            {
                ColumnCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(16, 14, 16, 4), Location = new Point(0, 0)
            };
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tabla.Controls.Add(lblAyuda, 0, 0);
            tabla.SetColumnSpan(lblAyuda, 2);
            tabla.Controls.Add(lblEstimada, 0, 1);
            tabla.SetColumnSpan(lblEstimada, 2);
            tabla.RowCount = 2;

            switch (_componente.Tipo)
            {
                case TipoComponente06AV.Procesador:
                    Fila("pcf_esp_plataforma", cboPlataforma);
                    Fila("pcf_esp_socket", txtSocket);
                    Fila("pcf_esp_tdp_cpu", txtConsumo);
                    Fila("pcf_esp_graficos", chkGraficos);
                    Fila("pcf_esp_desbloqueado", chkDesbloqueado);
                    break;
                case TipoComponente06AV.PlacaMadre:
                    Fila("pcf_esp_plataforma", cboPlataforma);
                    Fila("pcf_esp_socket", txtSocket);
                    Fila("pcf_esp_tipo_memoria", cboTipoMemoria);
                    Fila("pcf_esp_frecuencia_max", txtFrecuencia);
                    Fila("pcf_esp_slots", txtSlots);
                    Fila("pcf_esp_memoria_max", txtMemoriaMax);
                    Fila("pcf_esp_formato", cboFormato);
                    Fila("pcf_esp_permite_oc", chkOverclock);
                    Fila("pcf_esp_consumo", txtConsumo);
                    break;
                case TipoComponente06AV.MemoriaRAM:
                    Fila("pcf_esp_tipo_memoria", cboTipoMemoria);
                    Fila("pcf_esp_frecuencia", txtFrecuencia);
                    Fila("pcf_esp_capacidad", txtCapacidad);
                    Fila("pcf_esp_consumo", txtConsumo);
                    break;
                case TipoComponente06AV.Fuente:
                    Fila("pcf_esp_potencia", txtPotencia);
                    break;
                case TipoComponente06AV.Gabinete:
                    Fila("pcf_esp_formato_gab", cboFormato);
                    break;
                case TipoComponente06AV.Refrigeracion:
                    txtSocket.Width = 260;
                    Fila("pcf_esp_sockets", txtSocket);
                    Fila("pcf_esp_tdp_cooler", txtTdp);
                    Fila("pcf_esp_consumo", txtConsumo);
                    break;
                default:
                    Fila("pcf_esp_consumo", txtConsumo);
                    break;
            }

            btnGuardar = new Button { Width = 110, Height = 32, Margin = new Padding(6, 0, 0, 0), Text = t.Obtener("guardar") };
            btnCancelar = new Button { Width = 110, Height = 32, Margin = new Padding(6, 0, 0, 0), Text = t.Obtener("cancelar") };
            btnSugerir = new Button { AutoSize = true, MinimumSize = new Size(190, 32), Height = 32, Margin = new Padding(6, 0, 0, 0), Text = t.Obtener("pcf_esp_sugerir") };
            btnGuardar.Click += (s, e) => Guardar();
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            btnSugerir.Click += (s, e) => { Cargar(_bll.SugerirEspecificacion(_componente)); lblEstimada.Visible = true; };

            var barra = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = 56, FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false, Padding = new Padding(0, 12, 14, 0)
            };
            barra.Controls.Add(btnGuardar);
            barra.Controls.Add(btnCancelar);
            barra.Controls.Add(btnSugerir);

            Controls.Add(barra);
            Controls.Add(tabla);
            AcceptButton = btnGuardar;
            CancelButton = btnCancelar;

            Size preferido = tabla.GetPreferredSize(Size.Empty);
            ClientSize = new Size(Math.Max(560, preferido.Width + 8), preferido.Height + barra.Height + 8);
        }

        private void Fila(string clave, Control campo)
        {
            int r = tabla.RowCount;
            tabla.RowCount = r + 1;
            tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var l = new Label
            {
                AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 9, 6, 3),
                Text = GestorIdioma06AV.Instancia.Obtener(clave) + ":"
            };
            campo.Anchor = AnchorStyles.Left;
            campo.Margin = new Padding(3, 6, 3, 6);
            if (!(campo is CheckBox)) Tema.AplicarEntrada(campo);
            tabla.Controls.Add(l, 0, r);
            tabla.Controls.Add(campo, 1, r);
        }

        private static ComboBox Combo(IEnumerable<string> valores)
        {
            var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
            foreach (string v in valores) c.Items.Add(v);
            c.SelectedIndex = 0;
            return c;
        }

        private static TextBox Numero() => new TextBox { Width = 90, MaxLength = 6 };

        private void Cargar(EspecificacionComponente06AV e)
        {
            e = e ?? new EspecificacionComponente06AV();
            Seleccionar(cboPlataforma, e.Plataforma);
            Seleccionar(cboTipoMemoria, e.TipoMemoria);
            Seleccionar(cboFormato, e.Formato);
            txtSocket.Text = e.Socket ?? "";
            txtFrecuencia.Text = Texto(e.FrecuenciaMemoriaMhz);
            txtCapacidad.Text = Texto(e.CapacidadGb);
            txtSlots.Text = Texto(e.SlotsMemoria);
            txtMemoriaMax.Text = Texto(e.MemoriaMaximaGb);
            txtConsumo.Text = Texto(e.ConsumoW);
            txtPotencia.Text = Texto(e.PotenciaW);
            txtTdp.Text = Texto(e.TdpSoportadoW);
            chkGraficos.Checked = e.GraficosIntegrados == true;
            chkDesbloqueado.Checked = e.Desbloqueado == true;
            chkOverclock.Checked = e.PermiteOverclock == true;
        }

        private static void Seleccionar(ComboBox c, string valor)
        {
            int i = string.IsNullOrWhiteSpace(valor) ? 0
                  : c.Items.Cast<string>().ToList().FindIndex(x => string.Equals(x, valor, StringComparison.OrdinalIgnoreCase));
            c.SelectedIndex = i < 0 ? 0 : i;
        }

        private static string Texto(int? v) => v.HasValue ? v.Value.ToString() : "";

        private void Guardar()
        {
            try
            {
                var tipo = _componente.Tipo;
                bool esCpu = tipo == TipoComponente06AV.Procesador, esPlaca = tipo == TipoComponente06AV.PlacaMadre;
                var e = new EspecificacionComponente06AV
                {
                    Plataforma = esCpu || esPlaca ? Nulo(cboPlataforma) : null,
                    Socket = esCpu || esPlaca || tipo == TipoComponente06AV.Refrigeracion ? NuloTexto(txtSocket.Text) : null,
                    TipoMemoria = esPlaca || tipo == TipoComponente06AV.MemoriaRAM ? Nulo(cboTipoMemoria) : null,
                    FrecuenciaMemoriaMhz = esPlaca || tipo == TipoComponente06AV.MemoriaRAM ? Entero(txtFrecuencia) : null,
                    CapacidadGb = tipo == TipoComponente06AV.MemoriaRAM ? Entero(txtCapacidad) : null,
                    SlotsMemoria = esPlaca ? Entero(txtSlots) : null,
                    MemoriaMaximaGb = esPlaca ? Entero(txtMemoriaMax) : null,
                    Formato = esPlaca || tipo == TipoComponente06AV.Gabinete ? Nulo(cboFormato) : null,
                    ConsumoW = tipo == TipoComponente06AV.Fuente || tipo == TipoComponente06AV.Gabinete ? null : Entero(txtConsumo),
                    PotenciaW = tipo == TipoComponente06AV.Fuente ? Entero(txtPotencia) : null,
                    TdpSoportadoW = tipo == TipoComponente06AV.Refrigeracion ? Entero(txtTdp) : null,
                    GraficosIntegrados = esCpu ? chkGraficos.Checked : (bool?)null,
                    Desbloqueado = esCpu ? chkDesbloqueado.Checked : (bool?)null,
                    PermiteOverclock = esPlaca ? chkOverclock.Checked : (bool?)null,
                    Estimada = false
                };
                Especificacion = e;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (FormatException ex)
            {
                ConfirmacionForm.MostrarInfo(ex.Message, GestorIdioma06AV.Instancia.Obtener("aviso"),
                    ConfirmacionForm.TipoConfirmacion.Advertencia, this);
            }
        }

        private static string Nulo(ComboBox c) => NuloTexto(c.SelectedItem as string);

        private static string NuloTexto(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private static int? Entero(TextBox t)
        {
            string s = t.Text.Trim();
            if (s.Length == 0) return null;
            if (!int.TryParse(s, out int v) || v < 0)
                throw new FormatException(GestorIdioma06AV.Instancia.Obtener("pcf_esp_numero_invalido", s));
            return v;
        }

        public static string Resumen(Componente06AV c, EspecificacionComponente06AV e)
        {
            var t = GestorIdioma06AV.Instancia;
            if (e == null) return t.Obtener("pcf_esp_sin");
            var partes = new List<string>();
            switch (c?.Tipo)
            {
                case TipoComponente06AV.Procesador:
                    partes.Add(e.Plataforma); partes.Add(e.Socket); partes.Add(W(e.ConsumoW)); break;
                case TipoComponente06AV.PlacaMadre:
                    partes.Add(e.Plataforma); partes.Add(e.Socket); partes.Add(e.TipoMemoria);
                    partes.Add(e.SlotsMemoria.HasValue ? e.SlotsMemoria + " slots" : null);
                    partes.Add(ReglasCompatibilidad06AV.NombreFormato(e.Formato)); break;
                case TipoComponente06AV.MemoriaRAM:
                    partes.Add(e.TipoMemoria); partes.Add(e.FrecuenciaMemoriaMhz.HasValue ? e.FrecuenciaMemoriaMhz + " MHz" : null);
                    partes.Add(e.CapacidadGb.HasValue ? e.CapacidadGb + " GB" : null); break;
                case TipoComponente06AV.Fuente:
                    partes.Add(W(e.PotenciaW)); break;
                case TipoComponente06AV.Gabinete:
                    partes.Add(ReglasCompatibilidad06AV.NombreFormato(e.Formato)); break;
                case TipoComponente06AV.Refrigeracion:
                    partes.Add(e.Socket); partes.Add(e.TdpSoportadoW.HasValue ? "TDP " + e.TdpSoportadoW + " W" : null); break;
                default:
                    partes.Add(W(e.ConsumoW)); break;
            }
            string r = string.Join(" · ", partes.Where(p => !string.IsNullOrWhiteSpace(p)));
            if (r.Length == 0) r = t.Obtener("pcf_esp_sin");
            return e.Estimada ? r + "  (" + t.Obtener("pcf_esp_estimada_corto") + ")" : r;
        }

        private static string W(int? w) => w.HasValue ? w + " W" : null;
    }
}
