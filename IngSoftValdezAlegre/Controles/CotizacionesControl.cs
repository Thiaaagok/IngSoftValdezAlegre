using BE;
using BLL;
using IngSoftValdezAlegre.Common;
using IngSoftValdezAlegre.UI;
using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    /// <summary>
    /// MESA DE COTIZACIONES (RFN2, paso 3 y 4).
    ///
    /// Antes el circuito de cotización vivía apretado en el panel de detalle de
    /// Compras: un combo de proveedor y un botón. Eso alcanzaba para cargar UNA
    /// oferta, pero no para lo que realmente hace un gerente de compras, que es
    /// PEDIR VARIAS Y ELEGIR.
    ///
    /// Esta pantalla es una mesa de comparación:
    ///   · izquierda, las órdenes que esperan resolución, con cuántas ofertas tienen;
    ///   · centro, las ofertas de la orden elegida una al lado de la otra, con precio
    ///     total, costo por unidad, barra proporcional y diferencia porcentual contra
    ///     la más barata. La mejor se marca sola;
    ///   · abajo, el formulario para sumar otra oferta sin salir de la pantalla.
    ///
    /// Adjudicar una oferta desaprueba el resto en el mismo acto (lo resuelve el BLL)
    /// y pasa la orden a Enviada.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class CotizacionesControl : UserControl, IIdiomaAplicable06AV
    {
        private readonly CompraInsumosBLL06AV _bll = new CompraInsumosBLL06AV();
        private readonly ProveedoresBLL06AV _proveedoresBLL = new ProveedoresBLL06AV();

        private List<OrdenCompra06AV> _ordenes = new List<OrdenCompra06AV>();
        private List<PedidoCotizacion06AV> _cotizaciones = new List<PedidoCotizacion06AV>();
        private OrdenCompra06AV _sel;

        // Cabecera
        private Label lblTitulo;
        private ComboBox cboFiltro;
        private Button btnActualizar;

        // Lista de órdenes
        private Panel pnlLista;
        private Label lblListaTit;
        private FlowLayoutPanel flpOrdenes;
        private Label lblListaVacia;

        // Mesa
        private Panel pnlMesa, pnlCabOrden, pnlNueva;
        private Label lblOrdenTit, lblOrdenDet, lblOfertasTit, lblMesaVacia;
        private FlowLayoutPanel flpOfertas;

        // Alta de oferta
        private Label lblNuevaTit, lblProveedor, lblCosto, lblCondiciones;
        private ComboBox cboProveedor;
        private TextBox txtCosto, txtCondiciones;
        private Button btnNuevoProveedor, btnRegistrar;

        public CotizacionesControl()
        {
            ConstruirUI();
            AplicarTema();
            AplicarIdioma();
            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Disposed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;
            Tema.TemaChanged += AplicarTema;
            Disposed += (s, e) => Tema.TemaChanged -= AplicarTema;
            Cargar();
        }

        #region Construcción

        private void ConstruirUI()
        {
            // ── Cabecera ─────────────────────────────────────────
            lblTitulo = new Label { AutoSize = true, Location = new Point(6, 16) };

            cboFiltro = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 190
            };
            cboFiltro.SelectedIndexChanged += (s, e) => RefrescarLista();

            btnActualizar = NuevoBoton(120);
            btnActualizar.Click += (s, e) => Cargar();

            var flpCab = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 12, 10, 0)
            };
            flpCab.Controls.Add(cboFiltro);
            flpCab.Controls.Add(btnActualizar);

            var barraSup = new Panel { Dock = DockStyle.Top, Height = 56 };
            barraSup.Controls.Add(lblTitulo);
            barraSup.Controls.Add(flpCab);

            // ── Lista de órdenes ─────────────────────────────────
            lblListaTit = new Label { Dock = DockStyle.Top, Height = 26, AutoSize = false, Padding = new Padding(4, 4, 0, 0) };
            lblListaVacia = new Label { Dock = DockStyle.Top, Height = 60, AutoSize = false, Padding = new Padding(4, 10, 4, 0), Visible = false };
            flpOrdenes = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 4, 0, 8)
            };

            pnlLista = new Panel { Dock = DockStyle.Left, Width = 300, Padding = new Padding(10, 0, 10, 8) };
            pnlLista.Controls.Add(flpOrdenes);
            pnlLista.Controls.Add(lblListaVacia);
            pnlLista.Controls.Add(lblListaTit);

            // ── Cabecera de la orden elegida ─────────────────────
            lblOrdenTit = new Label { AutoSize = false, Dock = DockStyle.Top, Height = 30, Padding = new Padding(2, 4, 0, 0) };
            lblOrdenDet = new Label { AutoSize = false, Dock = DockStyle.Top, Height = 42, Padding = new Padding(2, 0, 0, 0) };
            pnlCabOrden = new Panel { Dock = DockStyle.Top, Height = 78, Padding = new Padding(14, 8, 14, 0) };
            pnlCabOrden.Controls.Add(lblOrdenDet);
            pnlCabOrden.Controls.Add(lblOrdenTit);

            // ── Formulario de nueva oferta ───────────────────────
            lblNuevaTit = new Label { AutoSize = true, Location = new Point(14, 10) };
            lblProveedor = new Label { AutoSize = true, Location = new Point(16, 42) };
            lblCosto = new Label { AutoSize = true, Location = new Point(330, 42) };
            lblCondiciones = new Label { AutoSize = true, Location = new Point(470, 42) };

            cboProveedor = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240, Location = new Point(16, 62) };
            txtCosto = new TextBox { Width = 120, Location = new Point(330, 62), Text = "0" };
            txtCondiciones = new TextBox { Width = 260, Location = new Point(470, 62) };

            btnNuevoProveedor = NuevoBoton(150);
            btnNuevoProveedor.Location = new Point(264, 62);
            btnNuevoProveedor.Width = 56;
            btnNuevoProveedor.Click += (s, e) => NuevoProveedor();

            btnRegistrar = NuevoBoton(190);
            btnRegistrar.Location = new Point(744, 61);
            btnRegistrar.Click += (s, e) => RegistrarOferta();

            pnlNueva = new Panel { Dock = DockStyle.Bottom, Height = 112 };
            pnlNueva.Controls.AddRange(new Control[]
            {
                lblNuevaTit, lblProveedor, cboProveedor, btnNuevoProveedor,
                lblCosto, txtCosto, lblCondiciones, txtCondiciones, btnRegistrar
            });

            // ── Ofertas ──────────────────────────────────────────
            lblOfertasTit = new Label { Dock = DockStyle.Top, Height = 26, AutoSize = false, Padding = new Padding(14, 4, 0, 0) };
            lblMesaVacia = new Label { Dock = DockStyle.Top, Height = 40, AutoSize = false, Padding = new Padding(16, 8, 0, 0) };
            flpOfertas = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = true,
                Padding = new Padding(14, 4, 8, 8)
            };

            pnlMesa = new Panel { Dock = DockStyle.Fill };
            pnlMesa.Controls.Add(flpOfertas);
            pnlMesa.Controls.Add(lblMesaVacia);
            pnlMesa.Controls.Add(lblOfertasTit);
            pnlMesa.Controls.Add(pnlCabOrden);
            pnlMesa.Controls.Add(pnlNueva);

            Controls.Add(pnlMesa);
            Controls.Add(pnlLista);
            Controls.Add(barraSup);
        }

        private static Button NuevoBoton(int ancho) => new Button
        {
            Width = ancho,
            Height = 32,
            Margin = new Padding(6, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };

        #endregion

        #region Datos

        private void Cargar()
        {
            try
            {
                _ordenes = _bll.ObtenerOrdenesCompra() ?? new List<OrdenCompra06AV>();
                _cotizaciones = _bll.ObtenerCotizaciones() ?? new List<PedidoCotizacion06AV>();
                CargarProveedores();
                RefrescarLista();
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void CargarProveedores()
        {
            try
            {
                object anterior = cboProveedor.SelectedItem;
                cboProveedor.DataSource = null;
                cboProveedor.DataSource = _proveedoresBLL.ObtenerTodos();
                if (anterior != null) cboProveedor.SelectedItem = anterior;
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private List<PedidoCotizacion06AV> OfertasDe(OrdenCompra06AV oc) =>
            oc == null
                ? new List<PedidoCotizacion06AV>()
                : _cotizaciones.Where(c => c.NumeroCompra == oc.Id)
                               .OrderBy(c => c.Costo)
                               .ToList();

        /// <summary>Estado de la orden desde el punto de vista de la mesa de cotizaciones.</summary>
        private enum Situacion { SinOfertas, ConOfertas, Adjudicada, Cerrada }

        private Situacion SituacionDe(OrdenCompra06AV oc)
        {
            if (oc.Estado == EstadoOrdenCompra06AV.Finalizada) return Situacion.Cerrada;
            if (oc.Estado == EstadoOrdenCompra06AV.Enviada) return Situacion.Adjudicada;
            return OfertasDe(oc).Any(c => c.Estado == EstadoCotizacion06AV.PorAprobar)
                ? Situacion.ConOfertas
                : Situacion.SinOfertas;
        }

        private static int UnidadesDe(OrdenCompra06AV oc) =>
            oc?.ComponentesFaltantes == null ? 0 : oc.ComponentesFaltantes.Sum(d => d.Cantidad);

        #endregion

        #region Lista de órdenes

        private void RefrescarLista()
        {
            var t = GestorIdioma06AV.Instancia;
            int? seleccionada = _sel?.NumeroCompra;

            flpOrdenes.SuspendLayout();
            foreach (Control c in flpOrdenes.Controls.Cast<Control>().ToList()) c.Dispose();
            flpOrdenes.Controls.Clear();

            List<OrdenCompra06AV> visibles = _ordenes
                .Where(PasaFiltro)
                .OrderBy(o => SituacionDe(o) == Situacion.ConOfertas ? 0
                            : SituacionDe(o) == Situacion.SinOfertas ? 1 : 2)
                .ThenBy(o => o.FechaLimite)
                .ToList();

            foreach (OrdenCompra06AV oc in visibles)
            {
                List<PedidoCotizacion06AV> ofertas = OfertasDe(oc);
                Situacion sit = SituacionDe(oc);
                int dias = (int)(oc.FechaLimite.Date - DateTime.Today).TotalDays;
                bool cerrada = sit == Situacion.Cerrada;

                var tarjeta = new TarjetaOrden06AV
                {
                    Etiqueta = oc,
                    Clave = "#" + oc.NumeroCompra,
                    Titulo = t.Obtener("pcf_cot_items",
                                       oc.ComponentesFaltantes.Count, UnidadesDe(oc)),
                    Subtitulo = t.Obtener("pcf_repositor") + ": " +
                                (oc.RepositorSolicitante != null ? oc.RepositorSolicitante.Login : "-"),
                    Chips = new[] { TextoSituacion(sit), t.Obtener("pcf_cot_ofertas_n", ofertas.Count) },
                    PieIzquierda = t.Obtener("pcf_f_limite") + ": " + oc.FechaLimite.ToShortDateString(),
                    TextoPlazo = cerrada ? null : TextoPlazo(dias),
                    Urgencia = cerrada ? 0 : (dias < 0 ? 2 : (dias <= 2 ? 1 : 0))
                };
                var ocLocal = oc;
                tarjeta.Elegida += (s, e) => Seleccionar(ocLocal);
                flpOrdenes.Controls.Add(tarjeta);
            }

            flpOrdenes.ResumeLayout();
            AjustarAnchoTarjetas();

            lblListaVacia.Visible = visibles.Count == 0;
            lblListaTit.Text = t.Obtener("pcf_cot_ordenes") + "  (" + visibles.Count + ")";

            OrdenCompra06AV nueva = seleccionada.HasValue
                ? visibles.FirstOrDefault(o => o.NumeroCompra == seleccionada.Value)
                : null;
            Seleccionar(nueva ?? visibles.FirstOrDefault());
        }

        private bool PasaFiltro(OrdenCompra06AV oc)
        {
            switch (cboFiltro.SelectedIndex)
            {
                case 1: return SituacionDe(oc) == Situacion.SinOfertas;
                case 2: return SituacionDe(oc) == Situacion.ConOfertas;
                case 3: return SituacionDe(oc) == Situacion.Adjudicada
                            || SituacionDe(oc) == Situacion.Cerrada;
                default: return true;
            }
        }

        private string TextoSituacion(Situacion s)
        {
            var t = GestorIdioma06AV.Instancia;
            switch (s)
            {
                case Situacion.SinOfertas: return t.Obtener("pcf_cot_sit_sin");
                case Situacion.ConOfertas: return t.Obtener("pcf_cot_sit_con");
                case Situacion.Adjudicada: return t.Obtener("pcf_cot_sit_adj");
                default: return t.Obtener("pcf_cot_sit_cerrada");
            }
        }

        private string TextoPlazo(int dias)
        {
            var t = GestorIdioma06AV.Instancia;
            if (dias < 0) return t.Obtener("pcf_plazo_atraso", -dias);
            if (dias == 0) return t.Obtener("pcf_plazo_hoy");
            return t.Obtener("pcf_plazo_dias", dias);
        }

        private void AjustarAnchoTarjetas()
        {
            int ancho = Math.Max(200, flpOrdenes.ClientSize.Width - 22);
            foreach (Control c in flpOrdenes.Controls) c.Width = ancho;
        }

        private void Seleccionar(OrdenCompra06AV oc)
        {
            _sel = oc;
            foreach (Control c in flpOrdenes.Controls)
                if (c is TarjetaOrden06AV t)
                {
                    bool esta = ReferenceEquals(t.Etiqueta, oc);
                    if (t.Seleccionada != esta) { t.Seleccionada = esta; t.Invalidate(); }
                }
            RefrescarMesa();
        }

        #endregion

        #region Mesa de ofertas

        private void RefrescarMesa()
        {
            var t = GestorIdioma06AV.Instancia;

            flpOfertas.SuspendLayout();
            foreach (Control c in flpOfertas.Controls.Cast<Control>().ToList()) c.Dispose();
            flpOfertas.Controls.Clear();

            if (_sel == null)
            {
                lblOrdenTit.Text = string.Empty;
                lblOrdenDet.Text = string.Empty;
                lblOfertasTit.Text = string.Empty;
                lblMesaVacia.Text = t.Obtener("pcf_cot_elegi_orden");
                lblMesaVacia.Visible = true;
                pnlNueva.Visible = false;
                flpOfertas.ResumeLayout();
                return;
            }

            List<PedidoCotizacion06AV> ofertas = OfertasDe(_sel);
            Situacion sit = SituacionDe(_sel);
            bool resoluble = sit == Situacion.ConOfertas || sit == Situacion.SinOfertas;

            lblOrdenTit.Text = t.Obtener("pcf_orden_num") + " #" + _sel.NumeroCompra +
                               "   —   " + TextoSituacion(sit);
            lblOrdenDet.Text =
                string.Join("   ·   ", _sel.ComponentesFaltantes
                    .Select(d => (d.Componente != null ? d.Componente.Descripcion : "?") + " ×" + d.Cantidad)) +
                Environment.NewLine +
                t.Obtener("pcf_f_limite") + ": " + _sel.FechaLimite.ToShortDateString() +
                "   ·   " + t.Obtener("pcf_cot_unidades", UnidadesDe(_sel));

            lblOfertasTit.Text = t.Obtener("pcf_cot_ofertas") + "  (" + ofertas.Count + ")";

            decimal mejor = ofertas.Count > 0 ? ofertas.Min(c => c.Costo) : 0m;
            decimal peor = ofertas.Count > 0 ? ofertas.Max(c => c.Costo) : 0m;
            int unidades = UnidadesDe(_sel);

            foreach (PedidoCotizacion06AV cot in ofertas)
            {
                var tarjeta = new TarjetaCotizacion06AV
                {
                    Cotizacion = cot,
                    MejorCosto = mejor,
                    PeorCosto = peor,
                    Unidades = unidades,
                    EsMejor = ofertas.Count > 1 && cot.Costo == mejor
                                                && cot.Estado != EstadoCotizacion06AV.Desaprobada,
                    PermiteResolver = resoluble,
                    TextoMejor = t.Obtener("pcf_cot_mejor"),
                    TextoAprobar = t.Obtener("pcf_cot_adjudicar"),
                    TextoDescartar = t.Obtener("pcf_cot_descartar"),
                    TextoUnidad = t.Obtener("pcf_cot_por_unidad"),
                    TextoSinCondiciones = t.Obtener("pcf_cot_sin_cond"),
                    TextoEstadoAprobada = t.Obtener("pcf_cot_adjudicada"),
                    TextoEstadoDescartada = t.Obtener("pcf_cot_descartada"),
                    TextoEstadoPendiente = t.Obtener("pcf_est_por_aprobar")
                };
                var cotLocal = cot;
                tarjeta.Aprobar += (s, e) => Resolver(cotLocal, true);
                tarjeta.Descartar += (s, e) => Resolver(cotLocal, false);
                flpOfertas.Controls.Add(tarjeta);
            }

            lblMesaVacia.Text = t.Obtener("pcf_cot_sin_ofertas");
            lblMesaVacia.Visible = ofertas.Count == 0;
            pnlNueva.Visible = resoluble;

            flpOfertas.ResumeLayout();
            AplicarTemaMesa();
        }

        private void Resolver(PedidoCotizacion06AV cot, bool adjudicar)
        {
            var t = GestorIdioma06AV.Instancia;
            string proveedor = cot.Proveedor != null ? cot.Proveedor.Nombre : "-";

            bool ok = ConfirmacionForm.Mostrar(
                adjudicar
                    ? t.Obtener("pcf_cot_confirmar_adj", proveedor, cot.Costo.ToString("C0"))
                    : t.Obtener("pcf_cot_confirmar_desc", proveedor),
                t.Obtener("pcf_menu_cotizaciones"),
                adjudicar ? ConfirmacionForm.TipoConfirmacion.Info
                          : ConfirmacionForm.TipoConfirmacion.Advertencia,
                adjudicar ? t.Obtener("pcf_cot_adjudicar") : t.Obtener("pcf_cot_descartar"),
                t.Obtener("cancelar"), FindForm());
            if (!ok) return;

            try
            {
                var gerente = UsuarioSesion06AV.Instancia().UsuarioActual;
                if (adjudicar) _bll.AprobarCotizacion(cot.Numero, gerente);
                else _bll.DesaprobarCotizacion(cot.Numero, gerente);
                Cargar();
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void RegistrarOferta()
        {
            var t = GestorIdioma06AV.Instancia;
            if (_sel == null) return;

            if (!(cboProveedor.SelectedItem is Proveedor06AV proveedor))
            {
                MostrarError(t.Obtener("pcf_cot_falta_proveedor"));
                return;
            }
            if (!decimal.TryParse(txtCosto.Text.Trim(), out decimal costo) || costo < 0)
            {
                MostrarError(t.Obtener("pcf_cot_costo_invalido"));
                return;
            }

            // Un mismo proveedor no cotiza dos veces la misma orden mientras su oferta siga abierta.
            bool repetido = OfertasDe(_sel).Any(c => c.Proveedor != null &&
                                                     c.Proveedor.Id == proveedor.Id &&
                                                     c.Estado == EstadoCotizacion06AV.PorAprobar);
            if (repetido)
            {
                MostrarError(t.Obtener("pcf_cot_repetido", proveedor.Nombre));
                return;
            }

            try
            {
                _bll.RegistrarCotizacion(_sel.Id, proveedor.Id, costo, txtCondiciones.Text.Trim());
                txtCosto.Text = "0";
                txtCondiciones.Text = string.Empty;
                Cargar();
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void NuevoProveedor()
        {
            using (var f = new FRMNuevoProveedor06AV())
            {
                if (f.ShowDialog(FindForm()) != DialogResult.OK) return;
                CargarProveedores();
                if (f.ProveedorCreado != null)
                    foreach (object o in cboProveedor.Items)
                        if (o is Proveedor06AV p && p.Id == f.ProveedorCreado.Id)
                        { cboProveedor.SelectedItem = o; break; }
            }
        }

        #endregion

        #region Tema e idioma

        public void AplicarTema()
        {
            Tema.AplicarControl(this);
            BackColor = Tema.FondoApp;

            Tema.AplicarTitulo(lblTitulo);
            Tema.AplicarBotonSecundario(btnActualizar);
            Tema.AplicarBotonSecundario(btnNuevoProveedor);
            Tema.AplicarBotonPrimario(btnRegistrar);
            Tema.AplicarEntrada(cboFiltro);
            Tema.AplicarEntrada(cboProveedor);
            Tema.AplicarEntrada(txtCosto);
            Tema.AplicarEntrada(txtCondiciones);

            foreach (Control c in new Control[] { pnlLista, flpOrdenes, pnlMesa, flpOfertas, pnlCabOrden })
                c.BackColor = Tema.FondoApp;

            pnlNueva.BackColor = Tema.FondoPanel;

            Tema.AplicarSubtitulo(lblListaTit);
            lblListaTit.BackColor = Tema.FondoApp;
            Tema.AplicarSubtitulo(lblOfertasTit);
            lblOfertasTit.BackColor = Tema.FondoApp;
            Tema.AplicarSubtitulo(lblNuevaTit);
            lblNuevaTit.BackColor = Tema.FondoPanel;

            lblOrdenTit.Font = Tema.FuenteSubtit;
            lblOrdenTit.ForeColor = Tema.TextoFuerte;
            lblOrdenTit.BackColor = Tema.FondoApp;
            lblOrdenDet.Font = Tema.FuenteRegular;
            lblOrdenDet.ForeColor = Tema.TextoSuave;
            lblOrdenDet.BackColor = Tema.FondoApp;

            foreach (Label l in new[] { lblListaVacia, lblMesaVacia })
            {
                l.Font = Tema.FuenteRegular;
                l.ForeColor = Tema.TextoSuave;
                l.BackColor = Tema.FondoApp;
            }

            foreach (Label l in new[] { lblProveedor, lblCosto, lblCondiciones })
            {
                l.Font = Tema.FuenteMini;
                l.ForeColor = Tema.TextoSuave;
                l.BackColor = Tema.FondoPanel;
            }

            AplicarTemaMesa();
            Invalidate(true);
        }

        private void AplicarTemaMesa()
        {
            foreach (Control c in flpOfertas.Controls) c.Invalidate();
            foreach (Control c in flpOrdenes.Controls) c.Invalidate();
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;

            lblTitulo.Text = t.Obtener("pcf_menu_cotizaciones");
            btnActualizar.Text = t.Obtener("pcf_actualizar");
            btnRegistrar.Text = t.Obtener("pcf_cot_registrar");
            btnNuevoProveedor.Text = "+";

            lblNuevaTit.Text = t.Obtener("pcf_cot_nueva");
            lblProveedor.Text = t.Obtener("pcf_proveedor");
            lblCosto.Text = t.Obtener("pcf_costo");
            lblCondiciones.Text = t.Obtener("pcf_condiciones");

            int filtro = cboFiltro.SelectedIndex;
            cboFiltro.Items.Clear();
            cboFiltro.Items.AddRange(new object[]
            {
                t.Obtener("pcf_cot_f_todas"),
                t.Obtener("pcf_cot_f_sin"),
                t.Obtener("pcf_cot_f_con"),
                t.Obtener("pcf_cot_f_resueltas")
            });
            cboFiltro.SelectedIndex = filtro >= 0 ? filtro : 0;

            if (_ordenes.Count > 0) RefrescarLista();
        }

        #endregion

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (flpOrdenes != null) AjustarAnchoTarjetas();
            if (btnRegistrar != null && pnlNueva != null)
                btnRegistrar.Left = Math.Max(txtCondiciones.Right + 16, pnlNueva.ClientSize.Width - 210);
        }

        private void MostrarError(string mensaje) => ConfirmacionForm.MostrarInfo(
            mensaje, GestorIdioma06AV.Instancia.Obtener("aviso"),
            ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());
    }
}
