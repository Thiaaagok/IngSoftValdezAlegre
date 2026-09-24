using BE;
using BLL;
using IngSoftValdezAlegre.Common;
using IngSoftValdezAlegre.UI;
using SER;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    [System.ComponentModel.DesignerCategory("Code")]
    public partial class ComprasControl : UserControl, IIdiomaAplicable06AV
    {
        private readonly CompraInsumosBLL06AV _bll = new CompraInsumosBLL06AV();
        private readonly ProveedoresBLL06AV _proveedoresBLL = new ProveedoresBLL06AV();

        private List<OrdenCompra06AV> _ordenes = new List<OrdenCompra06AV>();
        private List<PedidoCotizacion06AV> _cotizaciones = new List<PedidoCotizacion06AV>();

        private OrdenCompra06AV _ocSel;

        private enum Paso { Cotizar, Aprobar, Recibir, Finalizada }

        private Panel pnlLista;
        private Label lblTitulo;
        private Button btnActualizar, btnNueva;
        private DataGridView grOC;
        private Panel pnlDetalle;
        private Label lblDetTitulo, lblDetEstado;
        private FlowLayoutPanel flpDetalle;

        // Vista "nueva orden": asistente de 3 pasos (ver UI/AsistenteCompraControl06AV).
        private Panel pnlForm;
        private AsistenteCompraControl06AV asistente;

        // Vista "pedir cotización": proveedor + precio en una sola pantalla (RFN2 paso 3).
        private Panel pnlCotizacion;
        private PedidoCotizacionControl06AV cotizador;

        // Vista "recepción": control de lo que realmente llegó (RFN2 paso 5).
        private Panel pnlRecepcion;
        private RecepcionControl06AV recepcion;

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

        private void ConstruirUI()
        {
            ConstruirVistaLista();
            ConstruirVistaFormulario();

            Controls.Add(pnlLista);
            Controls.Add(pnlForm);
            Controls.Add(pnlCotizacion);
            Controls.Add(pnlRecepcion);
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

            lblDetTitulo = new Label { Dock = DockStyle.Top, AutoSize = false, Height = 26, Padding = new Padding(0, 2, 0, 0) };
            lblDetEstado = new Label { Dock = DockStyle.Top, AutoSize = false, Height = 24, Padding = new Padding(0, 0, 0, 6) };
            flpDetalle = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoScroll = true
            };
            pnlDetalle = new Panel { Dock = DockStyle.Right, Width = 380, Padding = new Padding(16, 14, 12, 12) };
            pnlDetalle.Controls.Add(flpDetalle);   
            pnlDetalle.Controls.Add(lblDetEstado); 
            pnlDetalle.Controls.Add(lblDetTitulo); 

            pnlLista = new Panel { Dock = DockStyle.Fill };
            pnlLista.Controls.Add(grOC);     
            pnlLista.Controls.Add(pnlDetalle); 
            pnlLista.Controls.Add(barraSup);   
        }

        private void ConstruirVistaFormulario()
        {
            asistente = new AsistenteCompraControl06AV { Dock = DockStyle.Fill };
            asistente.Cancelado += (s, e) => MostrarLista();
            asistente.Confirmado += (s, orden) => CrearOrden(orden);

            pnlForm = new Panel { Dock = DockStyle.Fill, Visible = false };
            pnlForm.Controls.Add(asistente);

            cotizador = new PedidoCotizacionControl06AV { Dock = DockStyle.Fill };
            cotizador.Cancelado += (s, e) => MostrarLista();
            cotizador.Confirmado += (s, c) => RegistrarCotizacion(c);
            cotizador.NuevoProveedor += (s, e) => AltaProveedorDesdeCotizador();

            pnlCotizacion = new Panel { Dock = DockStyle.Fill, Visible = false };
            pnlCotizacion.Controls.Add(cotizador);

            recepcion = new RecepcionControl06AV { Dock = DockStyle.Fill };
            recepcion.Cancelado += (s, e) => MostrarLista();
            recepcion.Confirmado += (s, r) => ConfirmarRecepcion(r);

            pnlRecepcion = new Panel { Dock = DockStyle.Fill, Visible = false };
            pnlRecepcion.Controls.Add(recepcion);
        }

        private static Button NuevoBoton(int width) =>
            new Button { Width = width, Height = 32, Margin = new Padding(6, 0, 0, 0), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };

        private void MostrarLista()
        {
            pnlForm.Visible = false;
            pnlCotizacion.Visible = false;
            pnlRecepcion.Visible = false;
            pnlLista.Visible = true;
            pnlLista.BringToFront();
        }

        private void AbrirFormularioNueva()
        {
            CargarFaltantes();
            pnlRecepcion.Visible = false;
            pnlCotizacion.Visible = false;
            pnlLista.Visible = false;
            pnlForm.Visible = true;
            pnlForm.BringToFront();
        }

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
                Repositor = oc.RepositorSolicitante?.Login ?? "",
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
                .Where(c => c.NumeroCompra == oc.Id)
                .OrderByDescending(c => c.FechaEmision)
                .FirstOrDefault();

        private void EstadoDe(OrdenCompra06AV oc, PedidoCotizacion06AV cot,
                              out string texto, out Color color, out Paso paso)
        {
            var t = GestorIdioma06AV.Instancia;

            if (oc.Estado == EstadoOrdenCompra06AV.Finalizada)
            {
                texto = t.Obtener("pcf_est_finalizada"); color = Tema.Exito; paso = Paso.Finalizada; return;
            }
            if (oc.Estado == EstadoOrdenCompra06AV.RecibidaParcial)
            {
                texto = t.Obtener("pcf_est_parcial"); color = Tema.Advertencia; paso = Paso.Recibir; return;
            }
            if (oc.Estado == EstadoOrdenCompra06AV.Enviada)
            {
                texto = t.Obtener("pcf_est_enviada"); color = Tema.Acento; paso = Paso.Recibir; return;
            }
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

        /// <summary>
        /// Arma los candidatos a reposición y se los pasa al asistente.
        /// Los componentes ya incluidos en una OC en curso viajan marcados como
        /// bloqueados (RFN2): se muestran, pero no se pueden volver a pedir.
        /// </summary>
        private void CargarFaltantes()
        {
            try
            {
                try { _ordenes = _bll.ObtenerOrdenesCompra() ?? _ordenes; } catch { }
                var enTramite = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var o in _ordenes.Where(o => o.Estado != EstadoOrdenCompra06AV.Finalizada))
                    foreach (var d in o.ComponentesFaltantes)
                        if (d.Componente != null && !enTramite.ContainsKey(d.Componente.Codigo))
                            enTramite[d.Componente.Codigo] = o.NumeroCompra;

                var items = new List<FaltanteItem06AV>();
                foreach (var i in _bll.ObtenerFaltantes())
                {
                    bool bloq = enTramite.TryGetValue(i.Codigo, out int ocNum);
                    items.Add(new FaltanteItem06AV
                    {
                        Codigo = i.Codigo,
                        Descripcion = i.Descripcion,
                        Stock = i.Stock,
                        StockMinimo = i.StockMinimo,
                        Bloqueado = bloq,
                        OrdenBloqueo = ocNum
                    });
                }

                var actual = UsuarioSesion06AV.Instancia().UsuarioActual;
                asistente.Cargar(items, actual?.Login);
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

            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_insumos")));
            var lineas = _ocSel.ComponentesFaltantes
                .Select(d => "•  " + (d.Componente != null ? d.Componente.Descripcion : "") + "   ×" + d.Cantidad);
            flpDetalle.Controls.Add(TextoSecundario(string.Join(Environment.NewLine, lineas)));

            if (cot != null)
            {
                flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_cotizacion_lbl")));
                string prov = cot.Proveedor != null ? cot.Proveedor.Nombre : "-";
                string linea = prov + "  —  " + cot.Costo.ToString("C0");
                if (!string.IsNullOrWhiteSpace(cot.Condiciones))
                    linea += Environment.NewLine + cot.Condiciones;
                flpDetalle.Controls.Add(TextoSecundario(linea));
            }

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

            var btnPedir = BotonDetalle(t.Obtener("pcf_pedir_cotizacion"), Tema.AplicarBotonAcento);
            btnPedir.Click += (s, e) => AbrirPedidoCotizacion();
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

        /// <summary>
        /// Abre la pantalla de pedido de cotización con la orden seleccionada. Reemplaza
        /// al combo del panel lateral + el diálogo modal de precio: elegir proveedor y
        /// fijar el precio son dos mitades de la misma decisión y ahora viven juntas,
        /// con la orden a la vista.
        /// </summary>
        private void AbrirPedidoCotizacion()
        {
            if (_ocSel == null)
            {
                MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_sel_orden"));
                return;
            }

            List<Proveedor06AV> proveedores;
            try { proveedores = _proveedoresBLL.ObtenerTodos(); }
            catch (Exception ex) { MostrarError(ex.Message); return; }

            cotizador.Cargar(_ocSel, proveedores, ProveedoresConOfertaAbierta(_ocSel));

            pnlLista.Visible = false;
            pnlForm.Visible = false;
            pnlRecepcion.Visible = false;
            pnlCotizacion.Visible = true;
            pnlCotizacion.BringToFront();
        }

        /// <summary>
        /// Proveedores que ya tienen una oferta sin resolver en esta orden: el BLL las
        /// rechaza, así que conviene que la pantalla las muestre apagadas de entrada.
        /// </summary>
        private List<int> ProveedoresConOfertaAbierta(OrdenCompra06AV oc)
        {
            return _cotizaciones
                .Where(c => c.NumeroCompra == oc.Id
                            && c.Estado == EstadoCotizacion06AV.PorAprobar
                            && c.Proveedor != null)
                .Select(c => c.Proveedor.Id)
                .Distinct()
                .ToList();
        }

        private void AltaProveedorDesdeCotizador()
        {
            using (var dlg = new FRMNuevoProveedor06AV())
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK || dlg.ProveedorCreado == null) return;
                try
                {
                    var proveedores = _proveedoresBLL.ObtenerTodos();
                    var creado = proveedores.FirstOrDefault(p => p.Cuit == dlg.ProveedorCreado.Cuit);
                    cotizador.RecargarProveedores(proveedores, creado != null ? creado.Id : (int?)null);
                }
                catch (Exception ex) { MostrarError(ex.Message); }
            }
        }

        private void RegistrarCotizacion(CotizacionArmada06AV armada)
        {
            if (_ocSel == null || armada == null || armada.Proveedor == null) return;

            try
            {
                var cot = _bll.RegistrarCotizacion(_ocSel.Id, armada.Proveedor.Id,
                                                   armada.Costo, armada.Condiciones);
                ConfirmacionForm.MostrarInfo(
                    GestorIdioma06AV.Instancia.Obtener("pcf_cotp_ok", cot.Numero,
                                                       armada.Proveedor.Nombre, armada.Costo.ToString("C0")),
                    GestorIdioma06AV.Instancia.Obtener("pcf_compras_titulo"),
                    ConfirmacionForm.TipoConfirmacion.Info, FindForm());

                int numero = _ocSel.NumeroCompra;
                MostrarLista();
                CargarOrdenes();
                SeleccionarOrden(numero);
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void AprobarDesaprobar(PedidoCotizacion06AV cot, bool aprobar)
        {
            if (cot == null) { MostrarError("No hay cotización para resolver."); return; }
            try
            {
                var gerente = UsuarioSesion06AV.Instancia().UsuarioActual;
                if (aprobar) _bll.AprobarCotizacion(cot.Numero, gerente);
                else _bll.DesaprobarCotizacion(cot.Numero, gerente);
                CargarOrdenes();
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        /// <summary>
        /// Abre el control de recepción con lo que todavía falta de la orden. Ya no se
        /// da por sentado que llegó todo: el operador declara qué bajó del camión.
        /// </summary>
        private void Recibir()
        {
            if (_ocSel == null) return;

            List<DetalleComponente06AV> pendiente;
            try { pendiente = _bll.ObtenerPendienteDeRecibir(_ocSel.Id); }
            catch (Exception ex) { MostrarError(ex.Message); return; }

            if (pendiente == null || pendiente.Count == 0)
            {
                MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_rec_ya_completa"));
                return;
            }

            recepcion.Cargar(_ocSel.NumeroCompra, pendiente);

            pnlLista.Visible = false;
            pnlForm.Visible = false;
            pnlCotizacion.Visible = false;
            pnlRecepcion.Visible = true;
            pnlRecepcion.BringToFront();
        }

        private void ConfirmarRecepcion(RecepcionArmada06AV r)
        {
            if (_ocSel == null || r == null) return;
            var t = GestorIdioma06AV.Instancia;

            try
            {
                _bll.RegistrarFacturaCompra(_ocSel.Id, r.FechaEntrega, r.Observaciones, r.Recibidos);

                ConfirmacionForm.MostrarInfo(
                    r.Completa
                        ? t.Obtener("pcf_rec_ok_completa", _ocSel.NumeroCompra)
                        : t.Obtener("pcf_rec_ok_parcial", _ocSel.NumeroCompra),
                    t.Obtener("pcf_rec_titulo"),
                    r.Completa ? ConfirmacionForm.TipoConfirmacion.Info
                               : ConfirmacionForm.TipoConfirmacion.Advertencia,
                    FindForm());

                MostrarLista();
                CargarOrdenes();
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void CrearOrden(OrdenArmada06AV orden)
        {
            if (orden == null || orden.Detalles == null || orden.Detalles.Count == 0) return;

            try
            {
                var repositor = UsuarioSesion06AV.Instancia().UsuarioActual;
                var oc = _bll.RegistrarOrdenCompra(orden.Detalles, orden.FechaLimite, repositor);
                ConfirmacionForm.MostrarInfo($"Orden de compra #{oc.NumeroCompra} creada.",
                    "Compras", ConfirmacionForm.TipoConfirmacion.Info, FindForm());
                MostrarLista();
                CargarOrdenes();
                SeleccionarOrden(oc.NumeroCompra);
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

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

        private void AplicarTema()
        {
            Tema.AplicarControl(this);
            Tema.AplicarTitulo(lblTitulo);
            Tema.AplicarGrilla(grOC);
            Tema.AplicarBotonSecundario(btnActualizar);
            Tema.AplicarBotonPrimario(btnNueva);
            asistente.AplicarTema();
            cotizador.AplicarTema();
            Tema.AplicarSubtitulo(lblDetTitulo);

            pnlDetalle.BackColor = Tema.FondoPanel;
            flpDetalle.BackColor = Tema.FondoPanel;
            lblDetTitulo.BackColor = Tema.FondoPanel;
            lblDetEstado.BackColor = Tema.FondoPanel;
            foreach (var p in new[] { pnlLista, pnlForm, pnlCotizacion, pnlRecepcion }) p.BackColor = Tema.FondoApp;
            recepcion.AplicarTema();

            ActualizarDetalle();
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            lblTitulo.Text = t.Obtener("pcf_compras_titulo");
            btnActualizar.Text = t.Obtener("pcf_actualizar");
            btnNueva.Text = "＋ " + t.Obtener("pcf_nueva_oc");
            asistente.AplicarIdioma();
            cotizador.AplicarIdioma();
            recepcion.AplicarIdioma();

            if (grOC.DataSource != null) CargarOrdenes();
            else ActualizarDetalle();
        }

        private void MostrarError(string mensaje) => ConfirmacionForm.MostrarInfo(
            mensaje, GestorIdioma06AV.Instancia.Obtener("aviso"),
            ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());

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

    }
}
