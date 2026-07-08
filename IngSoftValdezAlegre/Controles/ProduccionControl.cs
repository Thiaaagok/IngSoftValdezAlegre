using BE;
using BLL;
using IngSoftValdezAlegre.Common;
using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    /// <summary>
    /// Pantalla del proceso de Producción (RFN1): alta de orden (cliente + computadora),
    /// registro de seña, planificación (línea/fecha/responsable) y entrega (saldo + cierre).
    /// </summary>
    public class ProduccionControl : UserControl, IIdiomaAplicable06AV
    {
        private readonly OrdenProduccionBLL06AV _ordenesBLL = new OrdenProduccionBLL06AV();
        private readonly ClientesBLL06AV _clientesBLL = new ClientesBLL06AV();
        private readonly ComponentesBLL06AV _componentesBLL = new ComponentesBLL06AV();
        private readonly LineasEnsamblajeBLL06AV _lineasBLL = new LineasEnsamblajeBLL06AV();

        private Label lblTitulo;
        private DataGridView grilla;
        private Button btnSena, btnPlanificar, btnEntregar, btnRefrescar;

        // Alta de orden
        private Label lblCliente, lblTipo, lblComp, lblEntrega;
        private ComboBox cboCliente, cboTipo;
        private CheckedListBox clbComponentes;
        private DateTimePicker dtpEntrega;
        private Button btnRegistrar;

        // Planificación
        private Label lblLinea, lblInicio, lblResp;
        private ComboBox cboLinea;
        private DateTimePicker dtpInicio;
        private TextBox txtResp;

        public ProduccionControl()
        {
            ConstruirUI();
            AplicarTema();
            AplicarIdioma();
            AjustarLayout();
            Resize += (s, e) => AjustarLayout();
            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Disposed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;
            CargarCombos();
            CargarOrdenes();
        }

        private void ConstruirUI()
        {
            lblTitulo = new Label { AutoSize = true };
            grilla = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false
            };
            grilla.DataBindingComplete += (s, e) => OcultarColumnas();

            btnSena = new Button { Text = "Registrar seña" };
            btnPlanificar = new Button { Text = "Planificar" };
            btnEntregar = new Button { Text = "Entregar" };
            btnRefrescar = new Button { Text = "Refrescar" };
            btnSena.Click += (s, e) => Sena();
            btnPlanificar.Click += (s, e) => Planificar();
            btnEntregar.Click += (s, e) => Entregar();
            btnRefrescar.Click += (s, e) => CargarOrdenes();

            lblCliente = new Label { AutoSize = true };
            lblTipo = new Label { AutoSize = true };
            lblComp = new Label { AutoSize = true };
            lblEntrega = new Label { AutoSize = true };
            cboCliente = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cboTipo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cboTipo.DataSource = Enum.GetValues(typeof(TipoConfiguracion06AV));
            clbComponentes = new CheckedListBox { CheckOnClick = true };
            dtpEntrega = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(7) };
            btnRegistrar = new Button { Text = "Registrar orden" };
            btnRegistrar.Click += (s, e) => RegistrarOrden();

            lblLinea = new Label { AutoSize = true };
            lblInicio = new Label { AutoSize = true };
            lblResp = new Label { AutoSize = true };
            cboLinea = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            dtpInicio = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            txtResp = new TextBox();

            Controls.AddRange(new Control[]
            {
                lblTitulo, grilla, btnSena, btnPlanificar, btnEntregar, btnRefrescar,
                lblCliente, cboCliente, lblTipo, cboTipo, lblComp, clbComponentes, lblEntrega, dtpEntrega, btnRegistrar,
                lblLinea, cboLinea, lblInicio, dtpInicio, lblResp, txtResp
            });
        }

        private void AplicarTema()
        {
            Tema.AplicarControl(this);
            Tema.AplicarTitulo(lblTitulo);
            Tema.AplicarGrilla(grilla);
            Tema.AplicarBotonPrimario(btnRegistrar);
            Tema.AplicarBotonAcento(btnSena);
            Tema.AplicarBotonAcento(btnPlanificar);
            Tema.AplicarBotonPrimario(btnEntregar);
            Tema.AplicarBotonSecundario(btnRefrescar);
        }

        public void AplicarIdioma()
        {
            lblTitulo.Text = "Órdenes de Producción";
            btnSena.Text = "Registrar seña";
            btnPlanificar.Text = "Planificar";
            btnEntregar.Text = "Entregar";
            btnRefrescar.Text = "Refrescar";
            lblCliente.Text = "Cliente:";
            lblTipo.Text = "Tipo:";
            lblComp.Text = "Componentes:";
            lblEntrega.Text = "F. entrega:";
            btnRegistrar.Text = "Registrar orden";
            lblLinea.Text = "Línea:";
            lblInicio.Text = "F. inicio:";
            lblResp.Text = "Responsable:";
        }

        private void AjustarLayout()
        {
            int m = 12;
            int ancho = Math.Max(940, ClientSize.Width);
            int alto = Math.Max(560, ClientSize.Height);
            lblTitulo.SetBounds(m, m, 320, 30);

            int grillaW = (int)(ancho * 0.50);
            grilla.SetBounds(m, 50, grillaW - m, alto - 110);
            int by = alto - 50;
            btnSena.SetBounds(m, by, 120, 34);
            btnPlanificar.SetBounds(m + 128, by, 110, 34);
            btnEntregar.SetBounds(m + 246, by, 110, 34);
            btnRefrescar.SetBounds(m + 364, by, 100, 34);

            int fx = grillaW + m, etiqW = 90, campoX = fx + etiqW;
            int campoW = ancho - campoX - m, y = 52, paso = 34;
            void Fila(Control l, Control c, int alt = 26) { l.SetBounds(fx, y + 3, etiqW, 22); c.SetBounds(campoX, y, campoW, alt); y += (alt + 8); }
            Fila(lblCliente, cboCliente);
            Fila(lblTipo, cboTipo);
            Fila(lblComp, clbComponentes, 120);
            Fila(lblEntrega, dtpEntrega);
            btnRegistrar.SetBounds(campoX, y, 150, 34); y += 48;

            var sep = y;
            lblLinea.SetBounds(fx, sep + 3, etiqW, 22); cboLinea.SetBounds(campoX, sep, campoW, 26); sep += 34;
            lblInicio.SetBounds(fx, sep + 3, etiqW, 22); dtpInicio.SetBounds(campoX, sep, campoW, 26); sep += 34;
            lblResp.SetBounds(fx, sep + 3, etiqW, 22); txtResp.SetBounds(campoX, sep, campoW, 26);
        }

        private void CargarCombos()
        {
            try
            {
                cboCliente.DataSource = _clientesBLL.ObtenerTodos();
                cboLinea.DataSource = _lineasBLL.ObtenerTodas();
                clbComponentes.Items.Clear();
                foreach (var c in _componentesBLL.ObtenerTodos())
                    clbComponentes.Items.Add(c);   // usa Componente06AV.ToString()
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void CargarOrdenes()
        {
            try
            {
                grilla.DataSource = null;
                grilla.DataSource = _ordenesBLL.ObtenerTodas();
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void OcultarColumnas()
        {
            foreach (string col in new[] { "Computadora", "Pagos", "FechaInicioPrevista" })
                if (grilla.Columns[col] != null) grilla.Columns[col].Visible = false;
        }

        private OrdenProduccion06AV OrdenSeleccionada() =>
            grilla.CurrentRow?.DataBoundItem as OrdenProduccion06AV;

        private void RegistrarOrden()
        {
            var cliente = cboCliente.SelectedItem as Cliente06AV;
            if (cliente == null) { MostrarError("Elegí un cliente."); return; }

            var pc = new Computadora06AV
            {
                TipoConfiguracion = (TipoConfiguracion06AV)(cboTipo.SelectedItem ?? TipoConfiguracion06AV.Estandar),
                Nombre = cboTipo.SelectedItem?.ToString()
            };
            foreach (var it in clbComponentes.CheckedItems)
                if (it is Componente06AV comp) pc.Componentes.Add(comp);

            try
            {
                var orden = _ordenesBLL.RegistrarOrden(cliente, pc, dtpEntrega.Value);
                CargarOrdenes();
                ConfirmacionForm.MostrarInfo(
                    $"Orden #{orden.NumeroOrden} registrada. Total: ${orden.PrecioTotal:0.00}.",
                    "Producción", ConfirmacionForm.TipoConfirmacion.Info, FindForm());
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void Sena()
        {
            var o = OrdenSeleccionada();
            if (o == null) { MostrarError("Elegí una orden."); return; }
            try
            {
                decimal sena = _ordenesBLL.RegistrarSena(o.NumeroOrden);
                CargarOrdenes();
                ConfirmacionForm.MostrarInfo($"Seña registrada: ${sena:0.00}.",
                    "Producción", ConfirmacionForm.TipoConfirmacion.Info, FindForm());
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void Planificar()
        {
            var o = OrdenSeleccionada();
            if (o == null) { MostrarError("Elegí una orden."); return; }
            var linea = cboLinea.SelectedItem as LineaEnsamblaje06AV;
            if (linea == null) { MostrarError("Elegí una línea de ensamblaje."); return; }
            try
            {
                _ordenesBLL.Planificar(o.NumeroOrden, linea.Id, dtpInicio.Value, txtResp.Text.Trim());
                CargarOrdenes();
                CargarCombos(); // refresca disponibilidad de líneas
                ConfirmacionForm.MostrarInfo("Orden planificada.",
                    "Producción", ConfirmacionForm.TipoConfirmacion.Info, FindForm());
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void Entregar()
        {
            var o = OrdenSeleccionada();
            if (o == null) { MostrarError("Elegí una orden."); return; }
            bool ok = ConfirmacionForm.Mostrar(
                $"¿Entregar la orden #{o.NumeroOrden}? Se registrará el saldo pendiente (${o.SaldoPendiente:0.00}).",
                "Entregar", ConfirmacionForm.TipoConfirmacion.Advertencia, "Entregar", "Cancelar", FindForm());
            if (!ok) return;
            try
            {
                _ordenesBLL.Entregar(o.NumeroOrden);
                CargarOrdenes();
                CargarCombos();
                ConfirmacionForm.MostrarInfo("Orden entregada y cerrada.",
                    "Producción", ConfirmacionForm.TipoConfirmacion.Info, FindForm());
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void MostrarError(string mensaje) => ConfirmacionForm.MostrarInfo(
            mensaje, GestorIdioma06AV.Instancia.Obtener("aviso"),
            ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());
    }
}
