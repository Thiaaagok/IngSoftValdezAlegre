using BE;
using BLL;
using PCFORGE_ValdezThiago_96VA.Common;
using SER;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA
{
    public class FRMPagoProveedor06AV : Form
    {
        private readonly CompraInsumosBLL06AV _bll = new CompraInsumosBLL06AV();
        private readonly OrdenCompra06AV _orden;
        private readonly ResumenPagoProveedor06AV _resumen;

        private Label _lblCabecera, _lblResumen;
        private Label _lblMonto, _lblFecha, _lblForma, _lblReferencia;
        private NumericUpDown _numMonto;
        private DateTimePicker _dtpFecha;
        private ComboBox _cboForma;
        private TextBox _txtReferencia;
        private Button _btnGuardar, _btnCancelar;

        public PagoProveedor06AV PagoRegistrado { get; private set; }

        public FRMPagoProveedor06AV(OrdenCompra06AV orden, string proveedor, ResumenPagoProveedor06AV resumen)
        {
            _orden = orden ?? throw new ArgumentNullException(nameof(orden));
            _resumen = resumen ?? new ResumenPagoProveedor06AV();
            ConstruirUI(proveedor);
            AplicarTema();
        }

        private void ConstruirUI(string proveedor)
        {
            var t = GestorIdioma06AV.Instancia;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(440, 362);
            Text = t.Obtener("pcf_pago_titulo");

            _lblCabecera = new Label
            {
                Location = new Point(18, 14), Size = new Size(404, 24), AutoSize = false,
                Text = t.Obtener("pcf_orden_num") + " #" + _orden.NumeroCompra
            };
            _lblResumen = new Label
            {
                Location = new Point(18, 40), Size = new Size(404, 66), AutoSize = false, AutoEllipsis = true,
                Text = t.Obtener("pcf_proveedor") + ": " + (proveedor ?? "-") + Environment.NewLine +
                       t.Obtener("pcf_pago_facturado") + ": " + _resumen.Facturado.ToString("C2") + Environment.NewLine +
                       t.Obtener("pcf_pago_pagado") + ": " + _resumen.Pagado.ToString("C2") + Environment.NewLine +
                       t.Obtener("pcf_pago_saldo") + ": " + _resumen.Saldo.ToString("C2")
            };

            _lblMonto = Rotulo(t.Obtener("pcf_pago_monto"), 18, 114);
            _numMonto = new NumericUpDown
            {
                Location = new Point(18, 132), Width = 190, DecimalPlaces = 2, ThousandsSeparator = true,
                Minimum = 0, Maximum = Math.Max(0.01m, _resumen.Saldo), Increment = 1000
            };
            _numMonto.Value = Math.Min(_numMonto.Maximum, _resumen.Saldo);

            _lblFecha = Rotulo(t.Obtener("pcf_pago_fecha"), 232, 114);
            _dtpFecha = new DateTimePicker
            {
                Location = new Point(232, 132), Width = 190, Format = DateTimePickerFormat.Short,
                MaxDate = DateTime.Today.AddDays(1).AddTicks(-1), Value = DateTime.Today
            };

            _lblForma = Rotulo(t.Obtener("pcf_forma_pago"), 18, 172);
            _cboForma = new ComboBox { Location = new Point(18, 192), Width = 190, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboForma.Items.AddRange(new object[]
            {
                new Opcion(FormaPago06AV.Transferencia, t.Obtener("pcf_fp_transferencia")),
                new Opcion(FormaPago06AV.Efectivo, t.Obtener("pcf_fp_efectivo")),
                new Opcion(FormaPago06AV.TarjetaDebito, t.Obtener("pcf_fp_tarjeta_debito")),
                new Opcion(FormaPago06AV.Tarjeta, t.Obtener("pcf_fp_tarjeta"))
            });
            _cboForma.SelectedIndex = 0;

            _lblReferencia = Rotulo(t.Obtener("pcf_pago_referencia"), 18, 232);
            _txtReferencia = new TextBox { Location = new Point(18, 252), Width = 404, MaxLength = 100 };

            _btnGuardar = new Button { Size = new Size(160, 34), Location = new Point(262, 310), FlatStyle = FlatStyle.Flat, Text = t.Obtener("pcf_pago_registrar") };
            _btnCancelar = new Button { Size = new Size(110, 34), Location = new Point(144, 310), FlatStyle = FlatStyle.Flat, Text = t.Obtener("cancelar") };
            _btnGuardar.Click += (s, e) => Guardar();
            _btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[]
            {
                _lblCabecera, _lblResumen, _lblMonto, _numMonto, _lblFecha, _dtpFecha,
                _lblForma, _cboForma, _lblReferencia, _txtReferencia, _btnGuardar, _btnCancelar
            });
            AcceptButton = _btnGuardar;
            CancelButton = _btnCancelar;
        }

        private static Label Rotulo(string texto, int x, int y) =>
            new Label { Text = texto, AutoSize = true, Location = new Point(x, y) };

        private void AplicarTema()
        {
            Tema.AplicarFormulario(this);
            _lblCabecera.Font = Tema.FuenteSubtit;
            _lblCabecera.ForeColor = Tema.TextoFuerte;
            _lblResumen.Font = Tema.FuenteRegular;
            _lblResumen.ForeColor = Tema.Texto;
            foreach (Label l in new[] { _lblMonto, _lblFecha, _lblForma, _lblReferencia })
            {
                l.Font = Tema.FuenteMini;
                l.ForeColor = Tema.TextoSuave;
            }
            foreach (Control c in new Control[] { _numMonto, _dtpFecha, _cboForma, _txtReferencia })
                Tema.AplicarEntrada(c);
            Tema.AplicarBotonPrimario(_btnGuardar);
            Tema.AplicarBotonSecundario(_btnCancelar);
        }

        private void Guardar()
        {
            var forma = (_cboForma.SelectedItem as Opcion)?.Valor ?? FormaPago06AV.Transferencia;
            try
            {
                var usuario = UsuarioSesion06AV.Instancia().UsuarioActual;
                PagoRegistrado = _bll.RegistrarPagoProveedor(_orden.Id, _numMonto.Value, _dtpFecha.Value,
                                                             forma, _txtReferencia.Text, usuario);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                ConfirmacionForm.MostrarInfo(ex.Message, GestorIdioma06AV.Instancia.Obtener("aviso"),
                    ConfirmacionForm.TipoConfirmacion.Advertencia, this);
            }
        }

        private sealed class Opcion
        {
            public Opcion(FormaPago06AV valor, string texto) { Valor = valor; Texto = texto; }
            public FormaPago06AV Valor { get; }
            public string Texto { get; }
            public override string ToString() => Texto;
        }
    }
}
