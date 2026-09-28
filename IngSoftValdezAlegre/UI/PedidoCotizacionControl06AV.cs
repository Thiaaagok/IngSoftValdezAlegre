using BE;
using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.UI
{
    /// <summary>Lo que el operador armó en la pantalla: a quién, a qué precio cada producto y el total.</summary>
    internal class CotizacionArmada06AV : EventArgs
    {
        public CotizacionArmada06AV(Proveedor06AV proveedor, List<DetalleComponente06AV> precios, string condiciones)
        {
            Proveedor = proveedor;
            Precios = precios;
            Condiciones = condiciones;
        }

        public Proveedor06AV Proveedor { get; }

        /// <summary>Una línea por componente de la orden, con su precio unitario.</summary>
        public List<DetalleComponente06AV> Precios { get; }

        public decimal Costo => Precios.Sum(d => d.Subtotal);
        public string Condiciones { get; }
    }

    /// <summary>
    /// PEDIR COTIZACIÓN (RFN2, paso 3) — asistente de tres pasos, como el de nueva orden:
    ///   1. PROVEEDOR  — tarjetas con todos los proveedores; los que ya tienen una oferta
    ///      abierta en esta orden se ven apagados.
    ///   2. PRECIOS    — una fila por producto de la orden: el operador carga el precio
    ///      por unidad que le pasó el proveedor y el subtotal (precio × cantidad) y el
    ///      total se calculan solos.
    ///   3. CONFIRMAR  — repaso de la oferta y condiciones antes de registrarla.
    /// A la derecha queda fijo el ticket con la orden y el total que se va armando.
    /// </summary>
    internal class PedidoCotizacionControl06AV : UserControl, IIdiomaAplicable06AV
    {
        private const int Pasos = 3;

        // Cabecera y pasos
        private readonly Label _lblTitulo, _lblAyuda;
        private readonly PasosWizard06AV _pasos;

        // Paso 1 — proveedor
        private readonly Panel _pagina1;
        private readonly Label _lblSinProveedores, _lblBuscar;
        private readonly TextBox _txtBuscar;
        private readonly Button _btnNuevoProveedor;
        private readonly FlowLayoutPanel _flpProveedores;

        // Paso 2 — precios por producto
        private readonly Panel _pagina2;
        private readonly TableLayoutPanel _tablaPrecios;
        private readonly Label _lblTotalTabla;
        private readonly List<FilaPrecio> _filas = new List<FilaPrecio>();
        private Label _hComponente, _hCantidad, _hPrecio, _hSubtotal;

        // Paso 3 — confirmar
        private readonly Panel _pagina3;
        private readonly Label _lblCondiciones, _lblResumenTit;
        private readonly TextBox _txtCondiciones;
        private readonly FlowLayoutPanel _flpResumen;

        // Ticket
        private readonly Panel _pnlTicket, _pnlTotal;
        private readonly Label _lblTicketTit, _lblInsumosTit, _lblTotalRotulo, _lblTotalValor, _lblProgreso;
        private readonly FichaDatos06AV _ficha;
        private readonly FlowLayoutPanel _flpInsumos;

        // Barra inferior
        private readonly Panel _barra;
        private readonly Button _btnVolver, _btnAtras, _btnSiguiente, _btnRegistrar;

        private readonly List<Proveedor06AV> _proveedores = new List<Proveedor06AV>();
        private readonly HashSet<int> _yaOfertaron = new HashSet<int>();
        private OrdenCompra06AV _orden;
        private Proveedor06AV _elegido;
        private int _paso;

        public PedidoCotizacionControl06AV()
        {
            // ── Cabecera ─────────────────────────────────────────
            _lblTitulo = new Label { AutoSize = true, Location = new Point(16, 12) };
            var cabecera = new Panel { Dock = DockStyle.Top, Height = 46 };
            cabecera.Controls.Add(_lblTitulo);

            _pasos = new PasosWizard06AV { Dock = DockStyle.Top };
            _pasos.PasoElegido += (s, i) => IrA(i);

            _lblAyuda = new Label { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(16, 2, 16, 8) };

            // ── Paso 1: proveedor ────────────────────────────────
            _lblBuscar = new Label { AutoSize = true, Margin = new Padding(0, 8, 6, 0) };
            _txtBuscar = new TextBox { Width = 260, Margin = new Padding(0, 4, 0, 0) };
            _txtBuscar.TextChanged += (s, e) => RefrescarProveedores();
            _btnNuevoProveedor = NuevoBoton(190);
            _btnNuevoProveedor.Margin = new Padding(12, 0, 0, 0);
            _btnNuevoProveedor.Click += (s, e) => NuevoProveedor?.Invoke(this, EventArgs.Empty);

            var filaBusqueda = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true, Padding = new Padding(0, 4, 0, 6)
            };
            filaBusqueda.Controls.AddRange(new Control[] { _lblBuscar, _txtBuscar, _btnNuevoProveedor });

            _lblSinProveedores = new Label { Dock = DockStyle.Top, Height = 34, AutoSize = false, Visible = false };
            _flpProveedores = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true, AutoScroll = true, Padding = new Padding(0, 4, 4, 8)
            };

            _pagina1 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 4, 16, 8) };
            _pagina1.Controls.Add(_flpProveedores);
            _pagina1.Controls.Add(_lblSinProveedores);
            _pagina1.Controls.Add(filaBusqueda);

            // ── Paso 2: precios ──────────────────────────────────
            _tablaPrecios = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 4, Padding = new Padding(0, 0, 0, 6)
            };
            _tablaPrecios.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _tablaPrecios.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _tablaPrecios.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            _tablaPrecios.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));

            _lblTotalTabla = new Label { Dock = DockStyle.Top, AutoSize = false, Height = 40, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 8, 0) };

            var scrollPrecios = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(0, 0, 8, 0) };
            scrollPrecios.Controls.Add(_lblTotalTabla);
            scrollPrecios.Controls.Add(_tablaPrecios);

            _pagina2 = new Panel { Dock = DockStyle.Fill, Visible = false, Padding = new Padding(16, 6, 16, 8) };
            _pagina2.Controls.Add(scrollPrecios);

            // ── Paso 3: confirmar ────────────────────────────────
            _lblCondiciones = new Label { Dock = DockStyle.Top, AutoSize = false, Height = 24 };
            _txtCondiciones = new TextBox
            {
                Dock = DockStyle.Top, Height = 80, Multiline = true, ScrollBars = ScrollBars.Vertical, MaxLength = 500
            };
            _lblResumenTit = new Label { Dock = DockStyle.Top, AutoSize = false, Height = 34, Padding = new Padding(0, 10, 0, 0) };
            _flpResumen = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true
            };
            _pagina3 = new Panel { Dock = DockStyle.Fill, Visible = false, Padding = new Padding(16, 6, 16, 8) };
            _pagina3.Controls.Add(_flpResumen);
            _pagina3.Controls.Add(_lblResumenTit);
            _pagina3.Controls.Add(_txtCondiciones);
            _pagina3.Controls.Add(_lblCondiciones);

            // ── Derecha: ticket ──────────────────────────────────
            _lblTicketTit = new Label { Dock = DockStyle.Top, Height = 28, AutoSize = false };
            _ficha = new FichaDatos06AV { Dock = DockStyle.Top, Height = 150 };
            _lblInsumosTit = new Label { Dock = DockStyle.Top, Height = 30, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0, 6, 0, 0) };
            _flpInsumos = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoScroll = true, Padding = new Padding(0, 2, 0, 4)
            };
            _lblTotalRotulo = new Label { Dock = DockStyle.Top, Height = 18, AutoSize = false };
            _lblTotalValor = new Label { Dock = DockStyle.Top, Height = 42, AutoSize = false };
            _lblProgreso = new Label { Dock = DockStyle.Top, Height = 20, AutoSize = false };
            _pnlTotal = new Panel { Dock = DockStyle.Bottom, Height = 86 };
            _pnlTotal.Controls.Add(_lblProgreso);
            _pnlTotal.Controls.Add(_lblTotalValor);
            _pnlTotal.Controls.Add(_lblTotalRotulo);

            _pnlTicket = new Panel { Dock = DockStyle.Right, Width = 360, Padding = new Padding(18, 14, 18, 12) };
            _pnlTicket.Controls.Add(_flpInsumos);
            _pnlTicket.Controls.Add(_pnlTotal);
            _pnlTicket.Controls.Add(_lblInsumosTit);
            _pnlTicket.Controls.Add(_ficha);
            _pnlTicket.Controls.Add(_lblTicketTit);

            // ── Barra inferior ───────────────────────────────────
            _btnVolver = NuevoBoton(120);
            _btnAtras = NuevoBoton(120);
            _btnSiguiente = NuevoBoton(150);
            _btnRegistrar = NuevoBoton(200);
            _btnVolver.Click += (s, e) => Cancelado?.Invoke(this, EventArgs.Empty);
            _btnAtras.Click += (s, e) => IrA(_paso - 1);
            _btnSiguiente.Click += (s, e) => Siguiente();
            _btnRegistrar.Click += (s, e) => Confirmar();

            var flpBarra = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 10, 10, 0)
            };
            flpBarra.Controls.AddRange(new Control[] { _btnVolver, _btnAtras, _btnSiguiente, _btnRegistrar });
            _barra = new Panel { Dock = DockStyle.Bottom, Height = 58 };
            _barra.Controls.Add(flpBarra);

            var cuerpo = new Panel { Dock = DockStyle.Fill };
            cuerpo.Controls.Add(_pagina3);
            cuerpo.Controls.Add(_pagina2);
            cuerpo.Controls.Add(_pagina1);

            var izquierda = new Panel { Dock = DockStyle.Fill };
            izquierda.Controls.Add(cuerpo);
            izquierda.Controls.Add(_barra);
            izquierda.Controls.Add(_lblAyuda);
            izquierda.Controls.Add(_pasos);

            Controls.Add(izquierda);
            Controls.Add(_pnlTicket);
            Controls.Add(cabecera);

            AplicarTema();
            AplicarIdioma();
            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Disposed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;
            Tema.TemaChanged += AplicarTema;
            Disposed += (s, e) => Tema.TemaChanged -= AplicarTema;
        }

        public event EventHandler<CotizacionArmada06AV> Confirmado;
        public event EventHandler Cancelado;
        /// <summary>El operador quiere dar de alta un proveedor que no está en la lista.</summary>
        public event EventHandler NuevoProveedor;

        /// <summary>
        /// Prepara la pantalla para una orden concreta y vuelve al paso 1.
        /// <paramref name="conOfertaAbierta"/> son los proveedores que ya tienen una oferta
        /// sin resolver en esta orden: se muestran, pero no se pueden volver a elegir.
        /// </summary>
        public void Cargar(OrdenCompra06AV orden, IEnumerable<Proveedor06AV> proveedores,
                           IEnumerable<int> conOfertaAbierta)
        {
            _orden = orden;

            _proveedores.Clear();
            if (proveedores != null)
                _proveedores.AddRange(proveedores.Where(p => p != null)
                                                 .OrderBy(p => p.Nombre, StringComparer.CurrentCultureIgnoreCase));
            _yaOfertaron.Clear();
            if (conOfertaAbierta != null)
                foreach (int id in conOfertaAbierta) _yaOfertaron.Add(id);

            _elegido = null;
            _txtBuscar.Clear();
            _txtCondiciones.Clear();

            ArmarFilasPrecio();
            RefrescarProveedores();
            IrA(0);
        }

        /// <summary>Vuelve a listar los proveedores manteniendo el elegido, tras un alta.</summary>
        public void RecargarProveedores(IEnumerable<Proveedor06AV> proveedores, int? seleccionar = null)
        {
            _proveedores.Clear();
            if (proveedores != null)
                _proveedores.AddRange(proveedores.Where(p => p != null)
                                                 .OrderBy(p => p.Nombre, StringComparer.CurrentCultureIgnoreCase));
            if (seleccionar.HasValue)
            {
                _txtBuscar.Clear();
                _elegido = _proveedores.FirstOrDefault(p => p.Id == seleccionar.Value);
            }
            RefrescarProveedores();
        }

        // ══════════════════════════════════════════════════════════
        //  Navegación
        // ══════════════════════════════════════════════════════════

        private bool PasoCompleto(int paso)
        {
            if (paso == 0) return _elegido != null;
            if (paso == 1) return _filas.Count > 0 && _filas.All(f => f.Precio > 0);
            return true;
        }

        private void Siguiente()
        {
            if (!PasoCompleto(_paso)) return;
            IrA(_paso + 1);
        }

        private void IrA(int paso)
        {
            paso = Math.Max(0, Math.Min(Pasos - 1, paso));
            // No se salta un paso sin completar el anterior.
            for (int i = 0; i < paso; i++)
                if (!PasoCompleto(i)) { paso = i; break; }
            _paso = paso;

            _pagina1.Visible = _paso == 0;
            _pagina2.Visible = _paso == 1;
            _pagina3.Visible = _paso == 2;
            (_paso == 0 ? _pagina1 : _paso == 1 ? _pagina2 : _pagina3).BringToFront();
            if (_paso == 2) ArmarResumen();

            if (_paso == 1)
            {
                FilaPrecio primera = _filas.FirstOrDefault(f => f.Precio <= 0) ?? _filas.FirstOrDefault();
                primera?.Enfocar();
            }

            AplicarIdioma();
        }

        private void ActualizarBarra()
        {
            _pasos.Actual = _paso;
            _btnAtras.Visible = _paso > 0;
            _btnSiguiente.Visible = _paso < Pasos - 1;
            _btnRegistrar.Visible = _paso == Pasos - 1;

            bool puede = PasoCompleto(_paso);
            _btnSiguiente.Enabled = puede;
            if (puede) Tema.AplicarBotonPrimario(_btnSiguiente);
            else Tema.AplicarBotonDeshabilitado(_btnSiguiente);
            Tema.AplicarBotonAcento(_btnRegistrar);
        }

        // ══════════════════════════════════════════════════════════
        //  Paso 1 · Proveedores
        // ══════════════════════════════════════════════════════════
        private void RefrescarProveedores()
        {
            var t = GestorIdioma06AV.Instancia;
            string filtro = (_txtBuscar.Text ?? string.Empty).Trim();

            List<Proveedor06AV> visibles = _proveedores.Where(p => filtro.Length == 0 || Coincide(p, filtro)).ToList();

            _flpProveedores.SuspendLayout();
            foreach (Control c in _flpProveedores.Controls.Cast<Control>().ToList()) c.Dispose();
            _flpProveedores.Controls.Clear();

            foreach (Proveedor06AV p in visibles)
            {
                bool bloqueado = _yaOfertaron.Contains(p.Id);
                var tarjeta = new TarjetaOpcion06AV
                {
                    Valor = p,
                    Titulo = p.Nombre,
                    Subtitulo = Detalle(p),
                    Etiqueta = bloqueado ? t.Obtener("pcf_cotp_ya_oferto") : null,
                    Icono = IconoPcf06AV.Carrito,
                    Habilitada = !bloqueado,
                    Width = 300,
                    Seleccionada = _elegido != null && _elegido.Id == p.Id
                };
                Proveedor06AV local = p;
                tarjeta.Elegida += (s, e) => Elegir(local);
                _flpProveedores.Controls.Add(tarjeta);
            }
            _flpProveedores.ResumeLayout();

            bool vacio = _proveedores.Count == 0;
            _lblSinProveedores.Visible = vacio || visibles.Count == 0;
            _lblSinProveedores.Text = vacio ? t.Obtener("pcf_cotp_sin_proveedores") : t.Obtener("pcf_cotp_sin_resultados");

            ActualizarTicket();
        }

        private static bool Coincide(Proveedor06AV p, string filtro)
        {
            string f = filtro.ToLowerInvariant();
            return (p.Nombre ?? "").ToLowerInvariant().Contains(f)
                || (p.Cuit ?? "").ToLowerInvariant().Contains(f)
                || (p.Email ?? "").ToLowerInvariant().Contains(f);
        }

        private static string Detalle(Proveedor06AV p)
        {
            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(p.Cuit)) partes.Add("CUIT " + p.Cuit);
            if (!string.IsNullOrWhiteSpace(p.Telefono)) partes.Add(p.Telefono);
            else if (!string.IsNullOrWhiteSpace(p.Email)) partes.Add(p.Email);
            return string.Join("  ·  ", partes);
        }

        private void Elegir(Proveedor06AV p)
        {
            _elegido = p;
            foreach (Control c in _flpProveedores.Controls)
                if (c is TarjetaOpcion06AV t) t.Seleccionada = t.Valor is Proveedor06AV v && v.Id == p.Id;
            ActualizarTicket();
        }

        // ══════════════════════════════════════════════════════════
        //  Paso 2 · Precios por producto
        // ══════════════════════════════════════════════════════════
        private void ArmarFilasPrecio()
        {
            _tablaPrecios.SuspendLayout();
            foreach (Control c in _tablaPrecios.Controls.Cast<Control>().ToList()) c.Dispose();
            _tablaPrecios.Controls.Clear();
            _tablaPrecios.RowStyles.Clear();
            _filas.Clear();

            _hComponente = Encabezado(ContentAlignment.MiddleLeft);
            _hCantidad = Encabezado(ContentAlignment.MiddleRight);
            _hPrecio = Encabezado(ContentAlignment.MiddleLeft);
            _hSubtotal = Encabezado(ContentAlignment.MiddleRight);
            _tablaPrecios.RowCount = 1;
            _tablaPrecios.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _tablaPrecios.Controls.Add(_hComponente, 0, 0);
            _tablaPrecios.Controls.Add(_hCantidad, 1, 0);
            _tablaPrecios.Controls.Add(_hPrecio, 2, 0);
            _tablaPrecios.Controls.Add(_hSubtotal, 3, 0);

            if (_orden?.ComponentesFaltantes != null)
                foreach (DetalleComponente06AV d in _orden.ComponentesFaltantes)
                {
                    var fila = new FilaPrecio(d);
                    fila.Cambio += (s, e) => ActualizarTicket();
                    int r = _tablaPrecios.RowCount++;
                    _tablaPrecios.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    _tablaPrecios.Controls.Add(fila.Descripcion, 0, r);
                    _tablaPrecios.Controls.Add(fila.Cantidad, 1, r);
                    _tablaPrecios.Controls.Add(fila.Entrada, 2, r);
                    _tablaPrecios.Controls.Add(fila.Subtotal, 3, r);
                    _filas.Add(fila);
                }

            _tablaPrecios.ResumeLayout();
            AplicarTemaTabla();
        }

        private static Label Encabezado(ContentAlignment alineacion) => new Label
        {
            AutoSize = false, Dock = DockStyle.Fill, Height = 30, TextAlign = alineacion,
            Margin = new Padding(0, 0, 0, 4), Padding = new Padding(6, 0, 6, 0)
        };

        private List<DetalleComponente06AV> PreciosCargados() => _filas.Select(f => new DetalleComponente06AV
        {
            Componente = f.Item.Componente,
            Cantidad = f.Item.Cantidad,
            PrecioUnitario = f.Precio
        }).ToList();

        private decimal Total => _filas.Sum(f => f.SubtotalValor);

        // ══════════════════════════════════════════════════════════
        //  Paso 3 · Confirmar
        // ══════════════════════════════════════════════════════════
        private void ArmarResumen()
        {
            var t = GestorIdioma06AV.Instancia;
            _flpResumen.SuspendLayout();
            foreach (Control c in _flpResumen.Controls.Cast<Control>().ToList()) c.Dispose();
            _flpResumen.Controls.Clear();

            _flpResumen.Controls.Add(LineaResumen(t.Obtener("pcf_cotp_proveedor_elegido") + ":  " + (_elegido?.Nombre ?? "-"), Tema.FuenteBold, Tema.TextoFuerte, 0));
            foreach (FilaPrecio f in _filas)
                _flpResumen.Controls.Add(LineaResumen(
                    "•  " + f.Nombre + "   ×" + f.Item.Cantidad + "   ·   " +
                    t.Obtener("pcf_cotp_c_u", f.Precio.ToString("C2")) + "   =   " + f.SubtotalValor.ToString("C2"),
                    Tema.FuenteRegular, Tema.Texto, 0));
            _flpResumen.Controls.Add(LineaResumen(t.Obtener("pcf_cotp_total") + ":  " + Total.ToString("C2"), Tema.FuenteSubtit, Tema.Primario, 10));
            _flpResumen.ResumeLayout();
        }

        private static Label LineaResumen(string texto, Font fuente, Color color, int arriba) => new Label
        {
            Text = texto, AutoSize = true, Font = fuente, ForeColor = color, Margin = new Padding(2, 4 + arriba, 2, 2)
        };

        private void Confirmar()
        {
            var t = GestorIdioma06AV.Instancia;
            if (_elegido == null) { Aviso(t.Obtener("pcf_cotp_falta_proveedor")); IrA(0); return; }
            if (!PasoCompleto(1))
            {
                Aviso(t.Obtener("pcf_cotp_falta_precio", _filas.Count(f => f.Precio <= 0)));
                IrA(1);
                return;
            }
            Confirmado?.Invoke(this, new CotizacionArmada06AV(_elegido, PreciosCargados(), (_txtCondiciones.Text ?? "").Trim()));
        }

        // ══════════════════════════════════════════════════════════
        //  Ticket
        // ══════════════════════════════════════════════════════════
        private int UnidadesPedidas => _orden?.ComponentesFaltantes?.Sum(d => d.Cantidad) ?? 0;

        private void ActualizarTicket()
        {
            var t = GestorIdioma06AV.Instancia;

            var datos = new List<DatoFicha06AV>();
            if (_orden != null)
            {
                datos.Add(new DatoFicha06AV(t.Obtener("pcf_cotp_orden"), "#" + _orden.NumeroCompra));
                datos.Add(new DatoFicha06AV(t.Obtener("pcf_cotp_limite"), _orden.FechaLimite.ToShortDateString()));
                datos.Add(new DatoFicha06AV(t.Obtener("pcf_cotp_items"), (_orden.ComponentesFaltantes?.Count ?? 0).ToString()));
                datos.Add(new DatoFicha06AV(t.Obtener("pcf_cotp_unidades"), UnidadesPedidas.ToString()));
                datos.Add(new DatoFicha06AV(t.Obtener("pcf_cotp_repositor"), _orden.RepositorSolicitante?.Login ?? "—", true));
            }
            _ficha.Titulo = null;
            _ficha.RotuloDestacado = t.Obtener("pcf_cotp_proveedor_elegido");
            _ficha.ValorDestacado = _elegido != null ? _elegido.Nombre : t.Obtener("pcf_cotp_sin_elegir");
            _ficha.ColorDestacado = _elegido != null ? Tema.Primario : Tema.TextoSuave;
            _ficha.Definir(datos);
            _ficha.Height = _ficha.AltoNecesario;

            _flpInsumos.SuspendLayout();
            foreach (Control c in _flpInsumos.Controls.Cast<Control>().ToList()) c.Dispose();
            _flpInsumos.Controls.Clear();
            foreach (FilaPrecio f in _filas)
            {
                string texto = "•  " + f.Nombre + "   ×" + f.Item.Cantidad;
                if (f.Precio > 0) texto += Environment.NewLine + "    " + t.Obtener("pcf_cotp_c_u", f.Precio.ToString("C2")) + "  →  " + f.SubtotalValor.ToString("C2");
                _flpInsumos.Controls.Add(new Label
                {
                    Text = texto, AutoSize = true,
                    MaximumSize = new Size(Math.Max(200, _pnlTicket.Width - 48), 0),
                    Margin = new Padding(0, 0, 0, 6),
                    ForeColor = f.Precio > 0 ? Tema.Texto : Tema.TextoSuave,
                    BackColor = Tema.FondoPanel, Font = Tema.FuenteRegular
                });
            }
            _flpInsumos.ResumeLayout();

            int conPrecio = _filas.Count(f => f.Precio > 0);
            decimal total = Total;
            _lblTotalValor.Text = conPrecio > 0 ? total.ToString("C2") : "—";
            _lblTotalValor.ForeColor = conPrecio == _filas.Count && conPrecio > 0 ? Tema.TextoFuerte : Tema.TextoSuave;
            _lblProgreso.Text = t.Obtener("pcf_cotp_con_precio", conPrecio, _filas.Count);
            _lblProgreso.ForeColor = conPrecio == _filas.Count ? Tema.Exito : Tema.TextoSuave;
            _lblTotalTabla.Text = t.Obtener("pcf_cotp_total") + ":   " + total.ToString("C2");

            ActualizarBarra();
        }

        private void Aviso(string mensaje) => Common.ConfirmacionForm.MostrarInfo(
            mensaje, GestorIdioma06AV.Instancia.Obtener("aviso"),
            Common.ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());

        private static Button NuevoBoton(int ancho) => new Button
        {
            Width = ancho, Height = 34, Margin = new Padding(8, 0, 0, 0), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };

        // ══════════════════════════════════════════════════════════
        //  Tema e idioma
        // ══════════════════════════════════════════════════════════
        public void AplicarTema()
        {
            Tema.AplicarControl(this);
            BackColor = Tema.FondoApp;

            Tema.AplicarTitulo(_lblTitulo);
            _lblTitulo.BackColor = Tema.FondoApp;
            _lblAyuda.Font = Tema.FuenteRegular;
            _lblAyuda.ForeColor = Tema.TextoSuave;
            _lblAyuda.BackColor = Tema.FondoApp;

            foreach (Control c in new Control[] { _pagina1, _pagina2, _pagina3, _flpProveedores, _flpResumen, _barra, _tablaPrecios })
                c.BackColor = Tema.FondoApp;
            foreach (Control c in _barra.Controls) c.BackColor = Tema.FondoApp;
            foreach (Control c in Controls) if (!(c == _pnlTicket)) c.BackColor = Tema.FondoApp;
            foreach (Control c in _pagina2.Controls) c.BackColor = Tema.FondoApp;

            _lblBuscar.Font = Tema.FuenteMini;
            _lblBuscar.ForeColor = Tema.TextoSuave;
            _lblSinProveedores.ForeColor = Tema.TextoSuave;
            Tema.AplicarEntrada(_txtBuscar);
            Tema.AplicarEntrada(_txtCondiciones);
            Tema.AplicarBotonSecundario(_btnNuevoProveedor);
            Tema.AplicarBotonSecundario(_btnVolver);
            Tema.AplicarBotonSecundario(_btnAtras);

            _lblCondiciones.Font = Tema.FuenteMini;
            _lblCondiciones.ForeColor = Tema.TextoSuave;
            Tema.AplicarSubtitulo(_lblResumenTit);
            _lblTotalTabla.Font = Tema.FuenteSubtit;
            _lblTotalTabla.ForeColor = Tema.TextoFuerte;

            foreach (Control c in new Control[] { _pnlTicket, _flpInsumos, _pnlTotal })
                c.BackColor = Tema.FondoPanel;
            Tema.AplicarSubtitulo(_lblTicketTit);
            Tema.AplicarSubtitulo(_lblInsumosTit);
            _lblTicketTit.BackColor = _lblInsumosTit.BackColor = Tema.FondoPanel;
            _lblTotalRotulo.Font = Tema.FuenteMini;
            _lblTotalRotulo.ForeColor = Tema.TextoSuave;
            _lblTotalRotulo.BackColor = Tema.FondoPanel;
            _lblTotalValor.Font = new Font("Segoe UI Semibold", 20f, FontStyle.Bold);
            _lblTotalValor.BackColor = Tema.FondoPanel;
            _lblProgreso.Font = Tema.FuenteMini;
            _lblProgreso.BackColor = Tema.FondoPanel;

            AplicarTemaTabla();
            ActualizarTicket();
            Invalidate(true);
        }

        private void AplicarTemaTabla()
        {
            foreach (Label h in new[] { _hComponente, _hCantidad, _hPrecio, _hSubtotal })
            {
                if (h == null) continue;
                h.Font = Tema.FuenteBold;
                h.ForeColor = Tema.TextoInvertido;
                h.BackColor = Tema.FondoCabecera;
            }
            foreach (FilaPrecio f in _filas) f.AplicarTema();
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;

            _lblTitulo.Text = t.Obtener("pcf_cotp_titulo") +
                              (_orden != null ? " — " + t.Obtener("pcf_orden_num") + " #" + _orden.NumeroCompra : "");
            _pasos.DefinirPasos(new[] { t.Obtener("pcf_cotp_paso1"), t.Obtener("pcf_cotp_paso2"), t.Obtener("pcf_cotp_paso3") });
            _lblAyuda.Text = t.Obtener(_paso == 0 ? "pcf_cotp_hint1" : _paso == 1 ? "pcf_cotp_hint2" : "pcf_cotp_hint3");

            _lblBuscar.Text = t.Obtener("buscar");
            _btnNuevoProveedor.Text = "＋ " + t.Obtener("pcf_nuevo_proveedor");

            if (_hComponente != null)
            {
                _hComponente.Text = t.Obtener("pcf_cotp_col_componente");
                _hCantidad.Text = t.Obtener("pcf_cotp_col_cantidad");
                _hPrecio.Text = t.Obtener("pcf_cotp_col_precio");
                _hSubtotal.Text = t.Obtener("pcf_cotp_col_subtotal");
            }

            _lblCondiciones.Text = t.Obtener("pcf_cotp_condiciones");
            _lblResumenTit.Text = t.Obtener("pcf_cotp_resumen");

            _lblTicketTit.Text = t.Obtener("pcf_cotp_ticket");
            _lblInsumosTit.Text = t.Obtener("pcf_cotp_insumos");
            _lblTotalRotulo.Text = t.Obtener("pcf_cotp_total");

            _btnVolver.Text = t.Obtener("pcf_cotp_volver");
            _btnAtras.Text = "←  " + t.Obtener("pcf_asis_atras");
            _btnSiguiente.Text = t.Obtener("pcf_asis_siguiente") + "  →";
            _btnRegistrar.Text = t.Obtener("pcf_cotp_registrar");

            if (_paso == 2) ArmarResumen();
            ActualizarTicket();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // El ticket se angosta en pantallas chicas para dejarle lugar a la tabla de precios.
            if (_pnlTicket != null) _pnlTicket.Width = Width < 1000 ? 300 : 360;
            if (_lblAyuda != null) _lblAyuda.MaximumSize = new Size(Math.Max(200, Width - (_pnlTicket?.Width ?? 0) - 20), 0);
        }

        // ══════════════════════════════════════════════════════════
        //  Una fila de la tabla de precios
        // ══════════════════════════════════════════════════════════
        private sealed class FilaPrecio
        {
            public FilaPrecio(DetalleComponente06AV item)
            {
                Item = item;
                Componente06AV c = item.Componente;
                Nombre = c?.Descripcion ?? c?.Codigo ?? "";

                string marca = string.Join(" ", new[] { c?.Marca, c?.Modelo }.Where(x => !string.IsNullOrWhiteSpace(x)));
                Descripcion = new Label
                {
                    AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(6, 8, 6, 8),
                    Text = Nombre + Environment.NewLine + (c?.Codigo ?? "") + (marca.Length > 0 ? "  ·  " + marca : "")
                };
                Cantidad = new Label
                {
                    AutoSize = true, Anchor = AnchorStyles.Right, Margin = new Padding(10, 8, 10, 8),
                    Text = "×" + item.Cantidad
                };
                Entrada = new NumericUpDown
                {
                    DecimalPlaces = 2, Minimum = 0, Maximum = BLL.CompraInsumosBLL06AV.PrecioUnitarioMaximo,
                    ThousandsSeparator = true, Increment = 1, TextAlign = HorizontalAlignment.Right,
                    Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(6, 6, 6, 6)
                };
                // Se recalcula al tipear, no solo al salir del campo.
                Entrada.ValueChanged += (s, e) => Refrescar();
                Entrada.KeyUp += (s, e) => { LeerTexto(); };
                Entrada.Enter += (s, e) => Entrada.Select(0, Entrada.Text.Length);
                Subtotal = new Label { AutoSize = true, Anchor = AnchorStyles.Right, Margin = new Padding(6, 8, 10, 8) };
                Refrescar();
            }

            public DetalleComponente06AV Item { get; }
            public string Nombre { get; }
            public Label Descripcion { get; }
            public Label Cantidad { get; }
            public NumericUpDown Entrada { get; }
            public Label Subtotal { get; }
            public event EventHandler Cambio;

            private decimal _precio;
            public decimal Precio => _precio;
            public decimal SubtotalValor => Math.Round(_precio * Item.Cantidad, 2);

            public void Enfocar() { Entrada.Focus(); Entrada.Select(0, Entrada.Text.Length); }

            // NumericUpDown recién confirma Value al perder el foco: se lee el texto para que
            // el subtotal y el total acompañen lo que se va escribiendo.
            private void LeerTexto()
            {
                if (decimal.TryParse(Entrada.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal v) &&
                    v >= 0 && v <= Entrada.Maximum && v != _precio)
                {
                    _precio = Math.Round(v, 2);
                    Mostrar();
                }
            }

            private void Refrescar()
            {
                if (_precio == Entrada.Value) { Mostrar(); return; }
                _precio = Entrada.Value;
                Mostrar();
            }

            private void Mostrar()
            {
                Subtotal.Text = _precio > 0 ? SubtotalValor.ToString("C2") : "—";
                Subtotal.ForeColor = _precio > 0 ? Tema.TextoFuerte : Tema.TextoSuave;
                Cambio?.Invoke(this, EventArgs.Empty);
            }

            public void AplicarTema()
            {
                Descripcion.Font = Tema.FuenteRegular;
                Descripcion.ForeColor = Tema.Texto;
                Cantidad.Font = Tema.FuenteBold;
                Cantidad.ForeColor = Tema.TextoFuerte;
                Subtotal.Font = Tema.FuenteBold;
                Tema.AplicarEntrada(Entrada);
                Entrada.Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold);
                foreach (Control c in new Control[] { Descripcion, Cantidad, Subtotal }) c.BackColor = Tema.FondoApp;
                Mostrar();
            }
        }
    }
}
