using BE;
using BLL;
using IngSoftValdezAlegre.Common;
using SER;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    /// <summary>
    /// Compras (RFN2) rediseñado como una sola pantalla centrada en la Orden de Compra.
    /// La lista muestra el ESTADO de cada orden y, al seleccionarla, un panel de detalle
    /// indica el ÚNICO paso siguiente ("¿Qué sigue?") con el botón correspondiente:
    ///   Pendiente de cotizar → Pedir cotización a un proveedor.
    ///   Cotización por aprobar → Aprobar / Rechazar (gerente).
    ///   Lista para recibir (Enviada) → Recibir insumos (suma stock).
    ///   Recibida (Finalizada) → sin acción.
    /// "Nueva orden de compra" abre un formulario simple con los insumos faltantes.
    /// La lógica de negocio (CompraInsumosBLL06AV) no se toca.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public partial class ComprasControl : UserControl, IIdiomaAplicable06AV
    {
        private readonly CompraInsumosBLL06AV _bll = new CompraInsumosBLL06AV();
        private readonly ProveedoresBLL06AV _proveedoresBLL = new ProveedoresBLL06AV();

        private List<OrdenCompra06AV> _ordenes = new List<OrdenCompra06AV>();
        private List<PedidoCotizacion06AV> _cotizaciones = new List<PedidoCotizacion06AV>();
        private BindingList<FaltanteVm06AV> _faltantes = new BindingList<FaltanteVm06AV>();

        private OrdenCompra06AV _ocSel;

        private enum Paso { Cotizar, Aprobar, Recibir, Finalizada }

        // ── Vista lista (hub) ─────────────────────────────────────────
        private Panel pnlLista;
        private Label lblTitulo;
        private Button btnActualizar, btnNueva;
        private DataGridView grOC;
        private Panel pnlDetalle;
        private Label lblDetTitulo, lblDetEstado;
        private FlowLayoutPanel flpDetalle;

        // ── Vista formulario (nueva OC) ───────────────────────────────
        private Panel pnlForm;
        private Label lblFormTitulo, lblLimite, lblRepositor;
        private DataGridView grFaltantes;
        private DateTimePicker dtpLimite;
        private TextBox txtRepositor;
        private Button btnCrear, btnVolver;

        public ComprasControl()
        {
            ConstruirUI();
            AplicarTema();
            AplicarIdioma();
            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Disposed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;
            Tema.TemaChanged += AplicarTema;
            Disposed += (s, e) => Tema.TemaChanged -= AplicarTema;
            MostrarLista();
            CargarOrdenes();
        }

        // ══════════════════════════════════════════════════════════════
        //  Construcción de la interfaz
        // ══════════════════════════════════════════════════════════════
        private void ConstruirUI()
        {
            ConstruirVistaLista();
            ConstruirVistaFormulario();

            Controls.Add(pnlLista);
            Controls.Add(pnlForm);
        }

        private void ConstruirVistaLista()
        {
            lblTitulo = new Label { AutoSize = true, Location = new Point(4, 14) };

            btnActualizar = NuevoBoton(120);
            btnNueva = NuevoBoton(210);
            btnActualizar.Click += (s, e) => CargarOrdenes();
            btnNueva.Click += (s, e) => AbrirFormularioNueva();

            var flpAcciones = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 10, 8, 0)
            };
            flpAcciones.Controls.AddRange(new Control[] { btnActualizar, btnNueva });

            var barraSup = new Panel { Dock = DockStyle.Top, Height = 56 };
            barraSup.Controls.Add(lblTitulo);
            barraSup.Controls.Add(flpAcciones);

            // Grilla de órdenes de compra
            grOC = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                AllowUserToDeleteRows = false, MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false, BorderStyle = BorderStyle.None
            };
            grOC.SelectionChanged += (s, e) => ActualizarDetalle();
            grOC.DataBindingComplete += (s, e) => FormatearGrillaOrdenes();

            // Panel de detalle (derecha)
            lblDetTitulo = new Label { Dock = DockStyle.Top, AutoSize = false, Height = 26, Padding = new Padding(0, 2, 0, 0) };
            lblDetEstado = new Label { Dock = DockStyle.Top, AutoSize = false, Height = 24, Padding = new Padding(0, 0, 0, 6) };
            flpDetalle = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoScroll = true
            };
            pnlDetalle = new Panel { Dock = DockStyle.Right, Width = 380, Padding = new Padding(16, 14, 12, 12) };
            pnlDetalle.Controls.Add(flpDetalle);   // Fill primero
            pnlDetalle.Controls.Add(lblDetEstado); // Top
            pnlDetalle.Controls.Add(lblDetTitulo); // Top

            pnlLista = new Panel { Dock = DockStyle.Fill };
            pnlLista.Controls.Add(grOC);      // Fill primero
            pnlLista.Controls.Add(pnlDetalle); // Right
            pnlLista.Controls.Add(barraSup);   // Top
        }

        private void ConstruirVistaFormulario()
        {
            lblFormTitulo = new Label { Dock = DockStyle.Top, AutoSize = false, Height = 40, Padding = new Padding(6, 10, 0, 0) };

            grFaltantes = new DataGridView
            {
                Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                MultiSelect = false, SelectionMode = DataGridViewSelectionMode.CellSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false, BorderStyle = BorderStyle.None
            };
            grFaltantes.DataBindingComplete += (s, e) => FormatearGrillaFaltantes();
            // La tilde de "Incluir" se confirma al instante (sin salir de la celda).
            grFaltantes.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (grFaltantes.IsCurrentCellDirty)
                    grFaltantes.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            lblLimite = new Label { AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
            lblRepositor = new Label { AutoSize = true, Padding = new Padding(12, 6, 0, 0) };
            dtpLimite = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(5), Width = 140 };
            txtRepositor = new TextBox { Width = 220 };
            btnCrear = NuevoBoton(160);
            btnVolver = NuevoBoton(120);
            btnCrear.Click += (s, e) => CrearOrden();
            btnVolver.Click += (s, e) => MostrarLista();

            var barraForm = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = 58, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, Padding = new Padding(8, 12, 8, 8)
            };
            barraForm.Controls.AddRange(new Control[]
            {
                lblLimite, dtpLimite, lblRepositor, txtRepositor, btnCrear, btnVolver
            });

            pnlForm = new Panel { Dock = DockStyle.Fill, Visible = false };
            pnlForm.Controls.Add(grFaltantes);  // Fill primero
            pnlForm.Controls.Add(barraForm);    // Bottom
            pnlForm.Controls.Add(lblFormTitulo);// Top
        }

        private static Button NuevoBoton(int width) =>
            new Button { Width = width, Height = 32, Margin = new Padding(6, 0, 0, 0), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };

        // ══════════════════════════════════════════════════════════════
        //  Navegación entre vistas
        // ══════════════════════════════════════════════════════════════
        private void MostrarLista()
        {
            pnlForm.Visible = false;
            pnlLista.Visible = true;
            pnlLista.BringToFront();
        }

        private void AbrirFormularioNueva()
        {
            CargarFaltantes();
            pnlLista.Visible = false;
            pnlForm.Visible = true;
            pnlForm.BringToFront();
        }

        // ══════════════════════════════════════════════════════════════
        //  Carga de datos
        // ══════════════════════════════════════════════════════════════
        private void CargarOrdenes()
        {
            try
            {
                _ordenes = _bll.ObtenerOrdenesCompra() ?? new List<OrdenCompra06AV>();
                _cotizaciones = _bll.ObtenerCotizaciones() ?? new List<PedidoCotizacion06AV>();

                int? seleccion = (grOC.CurrentRow?.DataBoundItem as OrdenVm)?.Numero;

                var vm = _ordenes
                    .OrderByDescending(o => o.NumeroCompra)
                    .Select(CrearVm)
                    .ToList();

                grOC.DataSource = null;
                grOC.DataSource = vm;

                if (seleccion.HasValue) SeleccionarOrden(seleccion.Value);
                ActualizarDetalle();
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private OrdenVm CrearVm(OrdenCompra06AV oc)
        {
            var cot = CotizacionDe(oc);
            EstadoDe(oc, cot, out string texto, out Color color, out _);
            return new OrdenVm
            {
                Numero = oc.NumeroCompra,
                Repositor = oc.RepositorSolicitante,
                Limite = oc.FechaLimite.ToShortDateString(),
                Estado = texto,
                Costo = cot != null ? cot.Costo.ToString("C0") : "—",
                EstadoColor = color,
                Orden = oc,
                Cotizacion = cot
            };
        }

        private PedidoCotizacion06AV CotizacionDe(OrdenCompra06AV oc) =>
            _cotizaciones
                .Where(c => c.NumeroCompra == oc.NumeroCompra)
                .OrderByDescending(c => c.Numero)
                .FirstOrDefault();

        /// <summary>Traduce el par (orden, cotización) al texto/color/paso siguiente.</summary>
        private void EstadoDe(OrdenCompra06AV oc, PedidoCotizacion06AV cot,
                              out string texto, out Color color, out Paso paso)
        {
            var t = GestorIdioma06AV.Instancia;

            if (oc.Estado == EstadoOrdenCompra06AV.Finalizada)
            {
                texto = t.Obtener("pcf_est_finalizada"); color = Tema.Exito; paso = Paso.Finalizada; return;
            }
            if (oc.Estado == EstadoOrdenCompra06AV.Enviada)
            {
                texto = t.Obtener("pcf_est_enviada"); color = Tema.Acento; paso = Paso.Recibir; return;
            }
            // Pendiente
            if (cot != null && cot.Estado == EstadoCotizacion06AV.PorAprobar)
            {
                texto = t.Obtener("pcf_est_por_aprobar"); color = Tema.Primario; paso = Paso.Aprobar; return;
            }
            if (cot != null && cot.Estado == EstadoCotizacion06AV.Desaprobada)
            {
                texto = t.Obtener("pcf_est_rechazada"); color = Tema.Peligro; paso = Paso.Cotizar; return;
            }
            texto = t.Obtener("pcf_est_pendiente"); color = Tema.Advertencia; paso = Paso.Cotizar;
        }

        private void CargarFaltantes()
        {
            try
            {
                // Insumos que ya están en una orden en curso (no finalizada): no se pueden volver a pedir.
                try { _ordenes = _bll.ObtenerOrdenesCompra() ?? _ordenes; } catch { }
                var enTramite = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var o in _ordenes.Where(o => o.Estado != EstadoOrdenCompra06AV.Finalizada))
                    foreach (var d in o.InsumosFaltantes)
                        if (d.Insumo != null && !enTramite.ContainsKey(d.Insumo.Codigo))
                            enTramite[d.Insumo.Codigo] = o.NumeroCompra;

                _faltantes = new BindingList<FaltanteVm06AV>();
                foreach (var i in _bll.ObtenerFaltantes())
                {
                    bool bloq = enTramite.TryGetValue(i.Codigo, out int ocNum);
                    _faltantes.Add(new FaltanteVm06AV
                    {
                        Incluir = !bloq,
                        Bloqueado = bloq,
                        Codigo = i.Codigo,
                        Descripcion = bloq ? i.Descripcion + "  — ya pedido (OC #" + ocNum + ")" : i.Descripcion,
                        Stock = i.Stock, StockMinimo = i.StockMinimo,
                        Cantidad = Math.Max(1, i.StockMinimo - i.Stock + 1)
                    });
                }
                grFaltantes.DataSource = null;
                grFaltantes.DataSource = _faltantes;

                bool hay = _faltantes.Count > 0;
                btnCrear.Enabled = hay;
                var t = GestorIdioma06AV.Instancia;
                lblFormTitulo.Text = hay
                    ? t.Obtener("pcf_nueva_oc")
                    : t.Obtener("pcf_nueva_oc") + "  —  " + t.Obtener("pcf_sin_faltantes");
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void SeleccionarOrden(int numero)
        {
            foreach (DataGridViewRow row in grOC.Rows)
                if (row.DataBoundItem is OrdenVm vm && vm.Numero == numero)
                {
                    row.Selected = true;
                    if (row.Cells.Count > 0) grOC.CurrentCell = row.Cells[0];
                    return;
                }
        }

        // ══════════════════════════════════════════════════════════════
        //  Panel de detalle: "¿Qué sigue?"
        // ══════════════════════════════════════════════════════════════
        private void ActualizarDetalle()
        {
            flpDetalle.Controls.Clear();
            var t = GestorIdioma06AV.Instancia;

            _ocSel = (grOC.CurrentRow?.DataBoundItem as OrdenVm)?.Orden;

            if (_ocSel == null)
            {
                lblDetTitulo.Text = string.Empty;
                lblDetEstado.Text = string.Empty;
                flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_sel_orden")));
                return;
            }

            var cot = CotizacionDe(_ocSel);
            EstadoDe(_ocSel, cot, out string estadoTxt, out Color estadoColor, out Paso paso);

            lblDetTitulo.Text = t.Obtener("pcf_orden_num") + " #" + _ocSel.NumeroCompra;
            lblDetEstado.Text = "● " + estadoTxt;
            lblDetEstado.ForeColor = estadoColor;
            lblDetEstado.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);

            // Insumos
            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_insumos")));
            var lineas = _ocSel.InsumosFaltantes
                .Select(d => "•  " + (d.Insumo != null ? d.Insumo.Descripcion : d.Insumo?.Codigo) + "   ×" + d.Cantidad);
            flpDetalle.Controls.Add(TextoSecundario(string.Join(Environment.NewLine, lineas)));

            // Cotización (si existe)
            if (cot != null)
            {
                flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_cotizacion_lbl")));
                string prov = cot.Proveedor != null ? cot.Proveedor.Nombre : "-";
                string linea = prov + "  —  " + cot.Costo.ToString("C0");
                if (!string.IsNullOrWhiteSpace(cot.Condiciones))
                    linea += Environment.NewLine + cot.Condiciones;
                flpDetalle.Controls.Add(TextoSecundario(linea));
            }

            // Separador + "¿Qué sigue?"
            flpDetalle.Controls.Add(Separador());
            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_que_sigue"), fuerte: true));

            switch (paso)
            {
                case Paso.Cotizar: SeccionCotizar(cot); break;
                case Paso.Aprobar: SeccionAprobar(cot); break;
                case Paso.Recibir: SeccionRecibir(); break;
                case Paso.Finalizada: SeccionFinalizada(); break;
            }
        }

        private void SeccionCotizar(PedidoCotizacion06AV cotRechazada)
        {
            var t = GestorIdioma06AV.Instancia;
            flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_hint_cotizar")));

            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_proveedor")));
            var cbo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 330, Margin = new Padding(0, 2, 0, 6) };
            CargarProveedoresEn(cbo);
            Tema.AplicarEntrada(cbo);
            flpDetalle.Controls.Add(cbo);

            var btnNuevoProv = BotonDetalle(t.Obtener("pcf_nuevo_proveedor"), Tema.AplicarBotonSecundario);
            btnNuevoProv.Click += (s, e) =>
            {
                using (var dlg = new FRMNuevoProveedor06AV())
                    if (dlg.ShowDialog(FindForm()) == DialogResult.OK && dlg.ProveedorCreado != null)
                    {
                        CargarProveedoresEn(cbo);
                        foreach (var it in cbo.Items)
                            if (it is Proveedor06AV pr && pr.Cuit == dlg.ProveedorCreado.Cuit) { cbo.SelectedItem = it; break; }
                    }
            };
            flpDetalle.Controls.Add(btnNuevoProv);

            var btnPedir = BotonDetalle(t.Obtener("pcf_pedir_cotizacion"), Tema.AplicarBotonAcento);
            btnPedir.Click += (s, e) => PedirCotizacion(cbo.SelectedItem as Proveedor06AV);
            flpDetalle.Controls.Add(btnPedir);
        }

        private void SeccionAprobar(PedidoCotizacion06AV cot)
        {
            var t = GestorIdioma06AV.Instancia;
            flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_hint_aprobar")));

            var btnAprobar = BotonDetalle(t.Obtener("pcf_aprobar"), Tema.AplicarBotonPrimario);
            btnAprobar.Click += (s, e) => AprobarDesaprobar(cot, true);
            flpDetalle.Controls.Add(btnAprobar);

            var btnRechazar = BotonDetalle(t.Obtener("pcf_rechazar"), Tema.AplicarBotonPeligro);
            btnRechazar.Click += (s, e) => AprobarDesaprobar(cot, false);
            flpDetalle.Controls.Add(btnRechazar);
        }

        private void SeccionRecibir()
        {
            var t = GestorIdioma06AV.Instancia;
            flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_hint_recibir")));
            var btnRecibir = BotonDetalle(t.Obtener("pcf_recibir_insumos"), Tema.AplicarBotonPrimario);
            btnRecibir.Click += (s, e) => Recibir();
            flpDetalle.Controls.Add(btnRecibir);
        }

        private void SeccionFinalizada()
        {
            var t = GestorIdioma06AV.Instancia;
            flpDetalle.Controls.Add(TextoSecundario("✓  " + t.Obtener("pcf_hint_finalizada")));
        }

        // ══════════════════════════════════════════════════════════════
        //  Acciones (delegan en la BLL, sin cambios de lógica)
        // ══════════════════════════════════════════════════════════════
        private void PedirCotizacion(Proveedor06AV prov)
        {
            if (_ocSel == null) { MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_sel_orden")); return; }
            if (prov == null) { MostrarError("Elegí un proveedor."); return; }

            using (var dlg = new FRMCotizacion06AV())
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                try
                {
                    var cot = _bll.RegistrarCotizacion(_ocSel.NumeroCompra, prov.Id, dlg.Costo, dlg.Condiciones);
                    ConfirmacionForm.MostrarInfo(
                        $"Cotización #{cot.Numero} enviada a {prov.Nombre} (${dlg.Costo:0.00}).",
                        "Compras", ConfirmacionForm.TipoConfirmacion.Info, FindForm());
                    CargarOrdenes();
                }
                catch (Exception ex) { MostrarError(ex.Message); }
            }
        }

        private void AprobarDesaprobar(PedidoCotizacion06AV cot, bool aprobar)
        {
            if (cot == null) { MostrarError("No hay cotización para resolver."); return; }
            try
            {
                if (aprobar) _bll.AprobarCotizacion(cot.Numero);
                else _bll.DesaprobarCotizacion(cot.Numero);
                CargarOrdenes();
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void Recibir()
        {
            if (_ocSel == null) return;
            bool ok = ConfirmacionForm.Mostrar(
                $"¿Recibir los insumos de la orden #{_ocSel.NumeroCompra}? Se sumará el stock.",
                "Recibir", ConfirmacionForm.TipoConfirmacion.Advertencia, "Recibir", "Cancelar", FindForm());
            if (!ok) return;
            try
            {
                _bll.RecibirInsumos(_ocSel.NumeroCompra);
                ConfirmacionForm.MostrarInfo("Insumos recibidos y stock actualizado.",
                    "Compras", ConfirmacionForm.TipoConfirmacion.Info, FindForm());
                CargarOrdenes();
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void CrearOrden()
        {
            var detalles = new List<DetalleInsumo06AV>();
            foreach (var f in _faltantes)
                if (f.Incluir && f.Cantidad > 0)
                    detalles.Add(new DetalleInsumo06AV
                    {
                        Cantidad = f.Cantidad,
                        Insumo = new Insumo06AV { Codigo = f.Codigo, Descripcion = f.Descripcion, Stock = f.Stock, StockMinimo = f.StockMinimo }
                    });

            try
            {
                var oc = _bll.RegistrarOrdenCompra(detalles, dtpLimite.Value, txtRepositor.Text.Trim());
                ConfirmacionForm.MostrarInfo($"Orden de compra #{oc.NumeroCompra} creada.",
                    "Compras", ConfirmacionForm.TipoConfirmacion.Info, FindForm());
                MostrarLista();
                CargarOrdenes();
                SeleccionarOrden(oc.NumeroCompra);
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void CargarProveedoresEn(ComboBox cbo)
        {
            try { cbo.DataSource = _proveedoresBLL.ObtenerTodos(); }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        // ══════════════════════════════════════════════════════════════
        //  Formato de grillas
        // ══════════════════════════════════════════════════════════════
        private void FormatearGrillaOrdenes()
        {
            var t = GestorIdioma06AV.Instancia;
            void H(string col, string key) { if (grOC.Columns[col] != null) grOC.Columns[col].HeaderText = t.Obtener(key); }
            H("Numero", "pcf_col_numero");
            H("Repositor", "pcf_repositor");
            H("Limite", "pcf_f_limite");
            H("Estado", "pcf_col_estado");
            H("Costo", "pcf_costo");
            if (grOC.Columns["Numero"] != null) grOC.Columns["Numero"].FillWeight = 40;
            if (grOC.Columns["Costo"] != null)
                grOC.Columns["Costo"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            foreach (DataGridViewRow row in grOC.Rows)
                if (row.DataBoundItem is OrdenVm vm && grOC.Columns["Estado"] != null)
                {
                    row.Cells["Estado"].Style.ForeColor = vm.EstadoColor;
                    row.Cells["Estado"].Style.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
                }
        }

        private void FormatearGrillaFaltantes()
        {
            var t = GestorIdioma06AV.Instancia;
            foreach (DataGridViewColumn col in grFaltantes.Columns)
                col.ReadOnly = col.Name != "Cantidad" && col.Name != "Incluir";

            void H(string col, string key) { if (grFaltantes.Columns[col] != null) grFaltantes.Columns[col].HeaderText = t.Obtener(key); }
            H("Incluir", "pcf_col_incluir");
            H("Codigo", "pcf_col_codigo");
            H("Descripcion", "pcf_col_descripcion");
            H("Stock", "pcf_col_stock");
            H("StockMinimo", "pcf_col_minimo");
            H("Cantidad", "pcf_col_cantidad");
            if (grFaltantes.Columns["Incluir"] != null) grFaltantes.Columns["Incluir"].FillWeight = 28;

            // Insumos ya pedidos en otra orden: en gris y bloqueados (no se pueden tildar).
            foreach (DataGridViewRow row in grFaltantes.Rows)
                if (row.DataBoundItem is FaltanteVm06AV vm && vm.Bloqueado)
                {
                    row.DefaultCellStyle.ForeColor = Tema.TextoSuave;
                    if (grFaltantes.Columns["Incluir"] != null) row.Cells["Incluir"].ReadOnly = true;
                    if (grFaltantes.Columns["Cantidad"] != null) row.Cells["Cantidad"].ReadOnly = true;
                }
        }

        // ══════════════════════════════════════════════════════════════
        //  Helpers de UI (detalle)
        // ══════════════════════════════════════════════════════════════
        private Label Etiqueta(string texto, bool fuerte = false)
        {
            return new Label
            {
                Text = texto, AutoSize = true, Margin = new Padding(0, fuerte ? 4 : 8, 0, 2),
                ForeColor = fuerte ? Tema.Texto : Tema.TextoSuave,
                Font = new Font("Segoe UI Semibold", fuerte ? 10f : 8.5f, FontStyle.Bold)
            };
        }

        private Label TextoSecundario(string texto)
        {
            return new Label
            {
                Text = texto, AutoSize = true, MaximumSize = new Size(336, 0),
                Margin = new Padding(0, 0, 0, 4), ForeColor = Tema.Texto,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
            };
        }

        private Panel Separador() => new Panel { Height = 1, Width = 336, BackColor = Tema.Borde, Margin = new Padding(0, 10, 0, 6) };

        private Button BotonDetalle(string texto, Action<Button> estilo)
        {
            var b = new Button { Text = texto, Width = 200, Height = 32, Margin = new Padding(0, 4, 0, 0), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            estilo(b);
            return b;
        }

        // ══════════════════════════════════════════════════════════════
        //  Tema e idioma
        // ══════════════════════════════════════════════════════════════
        private void AplicarTema()
        {
            Tema.AplicarControl(this);
            Tema.AplicarTitulo(lblTitulo);
            Tema.AplicarGrilla(grOC);
            Tema.AplicarGrilla(grFaltantes);
            Tema.AplicarBotonSecundario(btnActualizar);
            Tema.AplicarBotonPrimario(btnNueva);
            Tema.AplicarBotonPrimario(btnCrear);
            Tema.AplicarBotonSecundario(btnVolver);
            Tema.AplicarSubtitulo(lblFormTitulo);
            Tema.AplicarSubtitulo(lblDetTitulo);

            pnlDetalle.BackColor = Tema.FondoPanel;
            flpDetalle.BackColor = Tema.FondoPanel;
            lblDetTitulo.BackColor = Tema.FondoPanel;
            lblDetEstado.BackColor = Tema.FondoPanel;
            foreach (var p in new[] { pnlLista, pnlForm }) p.BackColor = Tema.FondoApp;

            ActualizarDetalle();
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            lblTitulo.Text = t.Obtener("pcf_compras_titulo");
            btnActualizar.Text = t.Obtener("pcf_actualizar");
            btnNueva.Text = "＋ " + t.Obtener("pcf_nueva_oc");
            lblFormTitulo.Text = t.Obtener("pcf_nueva_oc");
            lblLimite.Text = t.Obtener("pcf_f_limite") + ":";
            lblRepositor.Text = t.Obtener("pcf_repositor") + ":";
            btnCrear.Text = t.Obtener("pcf_crear_orden");
            btnVolver.Text = t.Obtener("pcf_atras");

            if (grOC.DataSource != null) CargarOrdenes();
            else ActualizarDetalle();
        }

        private void MostrarError(string mensaje) => ConfirmacionForm.MostrarInfo(
            mensaje, GestorIdioma06AV.Instancia.Obtener("aviso"),
            ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());

        // ══════════════════════════════════════════════════════════════
        //  View-models
        // ══════════════════════════════════════════════════════════════
        /// <summary>Fila de la grilla de órdenes (con estado ya resuelto).</summary>
        private class OrdenVm
        {
            public int Numero { get; set; }
            public string Repositor { get; set; }
            public string Limite { get; set; }
            public string Estado { get; set; }
            public string Costo { get; set; }

            [Browsable(false)] public Color EstadoColor { get; set; }
            [Browsable(false)] public OrdenCompra06AV Orden { get; set; }
            [Browsable(false)] public PedidoCotizacion06AV Cotizacion { get; set; }
        }

        /// <summary>Fila editable para armar la orden de compra (insumo + cantidad a pedir).</summary>
        private class FaltanteVm06AV
        {
            public bool Incluir { get; set; }
            [Browsable(false)] public bool Bloqueado { get; set; }
            public string Codigo { get; set; }
            public string Descripcion { get; set; }
            public int Stock { get; set; }
            public int StockMinimo { get; set; }
            public int Cantidad { get; set; }
        }
    }
}
