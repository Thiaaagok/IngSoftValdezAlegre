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
    /// <summary>Lo que el operador armó en la pantalla: a quién y por cuánto.</summary>
    internal class CotizacionArmada06AV : EventArgs
    {
        public CotizacionArmada06AV(Proveedor06AV proveedor, decimal costo, string condiciones)
        {
            Proveedor = proveedor;
            Costo = costo;
            Condiciones = condiciones;
        }

        public Proveedor06AV Proveedor { get; }
        public decimal Costo { get; }
        public string Condiciones { get; }
    }

    /// <summary>
    /// PEDIR COTIZACIÓN (RFN2, paso 3) — pantalla completa.
    ///
    /// Antes esto vivía apretado en el panel de detalle de Compras: un ComboBox de
    /// proveedores de 330px y un botón que abría un diálogo modal aparte para escribir
    /// el precio. Eran dos pantallas para una sola decisión, y ninguna de las dos
    /// mostraba QUÉ se estaba cotizando: el operador elegía proveedor y tipeaba un
    /// número sin ver la orden.
    ///
    /// Acá se usa la misma estructura que "Nueva venta", porque el problema es el
    /// mismo: hay una parte de DECIDIR y una de CONFIRMAR.
    ///   · Izquierda — los dos pasos de la decisión: a qué proveedor se le pide
    ///     (tarjetas, no combo: se ven todos con su CUIT y los que ya ofertaron quedan
    ///     apagados en vez de desaparecer) y qué precio ofreció.
    ///   · Derecha — un ticket fijo con la orden que se está cotizando, sus insumos y
    ///     el costo en grande con su costo por unidad, que se actualiza mientras se
    ///     tipea. El botón de registrar está al pie del ticket, junto al número que
    ///     confirma.
    /// </summary>
    internal class PedidoCotizacionControl06AV : UserControl, IIdiomaAplicable06AV
    {
        // Cabecera
        private readonly Label _lblTitulo, _lblAyuda;

        // Izquierda — proveedor
        private readonly Label _lblProvTit, _lblSinProveedores, _lblBuscar;
        private readonly TextBox _txtBuscar;
        private readonly Button _btnNuevoProveedor;
        private readonly FlowLayoutPanel _flpProveedores;

        // Izquierda — oferta
        private readonly Label _lblOfertaTit, _lblCosto, _lblCondiciones;
        private readonly TextBox _txtCosto, _txtCondiciones;
        private readonly Panel _pnlOferta;

        // Derecha — ticket
        private readonly Panel _pnlTicket, _pnlTotal;
        private readonly Label _lblTicketTit, _lblInsumosTit, _lblTotalRotulo, _lblTotalValor, _lblUnitario;
        private readonly FichaDatos06AV _ficha;
        private readonly FlowLayoutPanel _flpInsumos, _flpAcciones;
        private readonly Button _btnRegistrar, _btnVolver;

        private readonly List<Proveedor06AV> _proveedores = new List<Proveedor06AV>();
        private readonly HashSet<int> _yaOfertaron = new HashSet<int>();
        private OrdenCompra06AV _orden;
        private Proveedor06AV _elegido;

        public PedidoCotizacionControl06AV()
        {
            // ── Cabecera ─────────────────────────────────────────
            _lblTitulo = new Label { AutoSize = true, Location = new Point(16, 14) };
            _lblAyuda = new Label { AutoSize = true, Location = new Point(18, 40) };

            var cabecera = new Panel { Dock = DockStyle.Top, Height = 70 };
            cabecera.Controls.Add(_lblAyuda);
            cabecera.Controls.Add(_lblTitulo);

            // ── Izquierda: proveedor ─────────────────────────────
            _lblProvTit = new Label
            {
                Dock = DockStyle.Top, Height = 34, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblBuscar = new Label
            {
                AutoSize = false, Width = 62, Height = 28,
                TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 2, 4, 0)
            };
            _txtBuscar = new TextBox { Width = 260, Height = 28 };
            _txtBuscar.TextChanged += (s, e) => RefrescarProveedores();

            _btnNuevoProveedor = NuevoBoton(180);
            _btnNuevoProveedor.Margin = new Padding(10, 0, 0, 0);
            _btnNuevoProveedor.Click += (s, e) => NuevoProveedor?.Invoke(this, EventArgs.Empty);

            var filaBusqueda = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 40,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, Padding = new Padding(0, 2, 0, 4)
            };
            filaBusqueda.Controls.Add(_lblBuscar);
            filaBusqueda.Controls.Add(_txtBuscar);
            filaBusqueda.Controls.Add(_btnNuevoProveedor);

            _lblSinProveedores = new Label
            {
                Dock = DockStyle.Top, Height = 34, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft, Visible = false
            };

            _flpProveedores = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true, AutoScroll = true,
                Padding = new Padding(0, 4, 4, 8)
            };

            // ── Izquierda: oferta ────────────────────────────────
            _lblOfertaTit = new Label
            {
                Dock = DockStyle.Bottom, Height = 32, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblCosto = new Label { AutoSize = true, Location = new Point(2, 6) };
            _txtCosto = new TextBox
            {
                Location = new Point(2, 26), Width = 200, Text = "0",
                Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold)
            };
            _txtCosto.TextChanged += (s, e) => ActualizarTicket();

            _lblCondiciones = new Label { AutoSize = true, Location = new Point(228, 6) };
            _txtCondiciones = new TextBox
            {
                Location = new Point(228, 26), Width = 420, Height = 86,
                Multiline = true, ScrollBars = ScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _pnlOferta = new Panel { Dock = DockStyle.Bottom, Height = 124 };
            _pnlOferta.Controls.AddRange(new Control[]
            {
                _lblCosto, _txtCosto, _lblCondiciones, _txtCondiciones
            });

            var cuerpo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18, 0, 18, 10) };
            cuerpo.Controls.Add(_flpProveedores);
            cuerpo.Controls.Add(_lblOfertaTit);
            cuerpo.Controls.Add(_pnlOferta);
            cuerpo.Controls.Add(_lblSinProveedores);
            cuerpo.Controls.Add(filaBusqueda);
            cuerpo.Controls.Add(_lblProvTit);

            // ── Derecha: ticket ──────────────────────────────────
            _lblTicketTit = new Label { Dock = DockStyle.Top, Height = 28, AutoSize = false };
            _ficha = new FichaDatos06AV { Dock = DockStyle.Top, Height = 150 };
            _lblInsumosTit = new Label
            {
                Dock = DockStyle.Top, Height = 30, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0, 6, 0, 0)
            };
            _flpInsumos = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoScroll = true,
                Padding = new Padding(0, 2, 0, 4)
            };

            _lblTotalRotulo = new Label { Dock = DockStyle.Top, Height = 18, AutoSize = false };
            _lblTotalValor = new Label { Dock = DockStyle.Top, Height = 40, AutoSize = false };
            _lblUnitario = new Label { Dock = DockStyle.Top, Height = 18, AutoSize = false };

            _pnlTotal = new Panel { Dock = DockStyle.Bottom, Height = 80 };
            _pnlTotal.Controls.Add(_lblUnitario);
            _pnlTotal.Controls.Add(_lblTotalValor);
            _pnlTotal.Controls.Add(_lblTotalRotulo);

            _btnRegistrar = NuevoBoton(300);
            _btnVolver = NuevoBoton(300);
            _btnRegistrar.Margin = new Padding(0, 6, 0, 4);
            _btnVolver.Margin = new Padding(0, 0, 0, 4);
            _btnRegistrar.Click += (s, e) => Confirmar();
            _btnVolver.Click += (s, e) => Cancelado?.Invoke(this, EventArgs.Empty);

            _flpAcciones = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            _flpAcciones.Controls.Add(_btnRegistrar);
            _flpAcciones.Controls.Add(_btnVolver);

            _pnlTicket = new Panel { Dock = DockStyle.Right, Width = 360, Padding = new Padding(18, 14, 18, 12) };
            _pnlTicket.Controls.Add(_flpInsumos);
            _pnlTicket.Controls.Add(_pnlTotal);
            _pnlTicket.Controls.Add(_flpAcciones);
            _pnlTicket.Controls.Add(_lblInsumosTit);
            _pnlTicket.Controls.Add(_ficha);
            _pnlTicket.Controls.Add(_lblTicketTit);

            Controls.Add(cuerpo);
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
        /// Prepara la pantalla para una orden concreta.
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
            _txtCosto.Text = "0";
            _txtCondiciones.Clear();

            RefrescarProveedores();
            AplicarIdioma();
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
        //  Proveedores
        // ══════════════════════════════════════════════════════════
        private void RefrescarProveedores()
        {
            var t = GestorIdioma06AV.Instancia;
            string filtro = (_txtBuscar.Text ?? string.Empty).Trim();

            List<Proveedor06AV> visibles = _proveedores
                .Where(p => filtro.Length == 0 || Coincide(p, filtro))
                .ToList();

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
            _lblSinProveedores.Text = vacio
                ? t.Obtener("pcf_cotp_sin_proveedores")
                : t.Obtener("pcf_cotp_sin_resultados");

            if (_elegido != null && !visibles.Any(p => p.Id == _elegido.Id)) { /* sigue elegido aunque no se vea */ }
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
                if (c is TarjetaOpcion06AV t)
                    t.Seleccionada = t.Valor is Proveedor06AV v && v.Id == p.Id;
            ActualizarTicket();
        }

        // ══════════════════════════════════════════════════════════
        //  Ticket
        // ══════════════════════════════════════════════════════════
        private int UnidadesPedidas =>
            _orden == null || _orden.ComponentesFaltantes == null
                ? 0 : _orden.ComponentesFaltantes.Sum(d => d.Cantidad);

        private bool CostoValido(out decimal costo) =>
            decimal.TryParse((_txtCosto.Text ?? "").Trim(), NumberStyles.Any,
                             CultureInfo.CurrentCulture, out costo) && costo > 0m;

        private void ActualizarTicket()
        {
            var t = GestorIdioma06AV.Instancia;

            // Ficha de contexto de la orden
            var datos = new List<DatoFicha06AV>();
            if (_orden != null)
            {
                datos.Add(new DatoFicha06AV(t.Obtener("pcf_cotp_orden"), "#" + _orden.NumeroCompra));
                datos.Add(new DatoFicha06AV(t.Obtener("pcf_cotp_limite"), _orden.FechaLimite.ToShortDateString()));
                datos.Add(new DatoFicha06AV(t.Obtener("pcf_cotp_items"),
                                            (_orden.ComponentesFaltantes?.Count ?? 0).ToString()));
                datos.Add(new DatoFicha06AV(t.Obtener("pcf_cotp_unidades"), UnidadesPedidas.ToString()));
                datos.Add(new DatoFicha06AV(t.Obtener("pcf_cotp_repositor"),
                                            _orden.RepositorSolicitante?.Login ?? "—", true));
            }
            _ficha.Titulo = null;
            _ficha.RotuloDestacado = t.Obtener("pcf_cotp_proveedor_elegido");
            _ficha.ValorDestacado = _elegido != null ? _elegido.Nombre : t.Obtener("pcf_cotp_sin_elegir");
            _ficha.ColorDestacado = _elegido != null ? Tema.Primario : Tema.TextoSuave;
            _ficha.Definir(datos);
            _ficha.Height = _ficha.AltoNecesario;

            // Insumos de la orden
            _flpInsumos.SuspendLayout();
            foreach (Control c in _flpInsumos.Controls.Cast<Control>().ToList()) c.Dispose();
            _flpInsumos.Controls.Clear();
            if (_orden != null && _orden.ComponentesFaltantes != null)
                foreach (DetalleComponente06AV d in _orden.ComponentesFaltantes)
                    _flpInsumos.Controls.Add(LineaInsumo(d));
            _flpInsumos.ResumeLayout();

            // Costo
            decimal costo;
            bool ok = CostoValido(out costo);
            _lblTotalValor.Text = ok ? costo.ToString("C0") : "—";
            _lblTotalValor.ForeColor = ok ? Tema.TextoFuerte : Tema.TextoSuave;

            int unidades = UnidadesPedidas;
            _lblUnitario.Text = ok && unidades > 0
                ? t.Obtener("pcf_cotp_unitario", (costo / unidades).ToString("C2"))
                : string.Empty;

            bool puede = _elegido != null && ok;
            _btnRegistrar.Enabled = puede;
            if (puede) Tema.AplicarBotonPrimario(_btnRegistrar);
            else Tema.AplicarBotonDeshabilitado(_btnRegistrar);
        }

        private Label LineaInsumo(DetalleComponente06AV d)
        {
            string desc = d.Componente != null ? d.Componente.Descripcion : "";
            return new Label
            {
                Text = "•  " + desc + "   ×" + d.Cantidad,
                AutoSize = true,
                MaximumSize = new Size(Math.Max(200, _pnlTicket.Width - 48), 0),
                Margin = new Padding(0, 0, 0, 4),
                ForeColor = Tema.Texto,
                BackColor = Tema.FondoPanel,
                Font = Tema.FuenteRegular
            };
        }

        private void Confirmar()
        {
            var t = GestorIdioma06AV.Instancia;

            if (_elegido == null) { Aviso(t.Obtener("pcf_cotp_falta_proveedor")); return; }

            decimal costo;
            if (!CostoValido(out costo)) { Aviso(t.Obtener("pcf_cotp_costo_invalido")); return; }

            Confirmado?.Invoke(this, new CotizacionArmada06AV(_elegido, costo, (_txtCondiciones.Text ?? "").Trim()));
        }

        private void Aviso(string mensaje) => Common.ConfirmacionForm.MostrarInfo(
            mensaje, GestorIdioma06AV.Instancia.Obtener("aviso"),
            Common.ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());

        private static Button NuevoBoton(int ancho) => new Button
        {
            Width = ancho, Height = 34,
            Margin = new Padding(0, 0, 8, 0),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };

        // ══════════════════════════════════════════════════════════
        //  Tema e idioma
        // ══════════════════════════════════════════════════════════
        public void AplicarTema()
        {
            Tema.AplicarControl(this);
            BackColor = Tema.FondoApp;

            Tema.AplicarTitulo(_lblTitulo);
            _lblAyuda.Font = Tema.FuenteRegular;
            _lblAyuda.ForeColor = Tema.TextoSuave;
            _lblAyuda.BackColor = Tema.FondoApp;
            _lblTitulo.BackColor = Tema.FondoApp;

            Tema.AplicarSubtitulo(_lblProvTit);
            Tema.AplicarSubtitulo(_lblOfertaTit);
            _lblProvTit.BackColor = Tema.FondoApp;
            _lblOfertaTit.BackColor = Tema.FondoApp;

            foreach (Control c in new Control[] { _flpProveedores, _pnlOferta })
                c.BackColor = Tema.FondoApp;

            _lblBuscar.Font = Tema.FuenteMini;
            _lblBuscar.ForeColor = Tema.TextoSuave;
            _lblBuscar.BackColor = Tema.FondoApp;
            _lblSinProveedores.Font = Tema.FuenteRegular;
            _lblSinProveedores.ForeColor = Tema.TextoSuave;
            _lblSinProveedores.BackColor = Tema.FondoApp;

            foreach (Label l in new[] { _lblCosto, _lblCondiciones })
            {
                l.Font = Tema.FuenteMini;
                l.ForeColor = Tema.TextoSuave;
                l.BackColor = Tema.FondoApp;
            }

            Tema.AplicarEntrada(_txtBuscar);
            Tema.AplicarEntrada(_txtCosto);
            Tema.AplicarEntrada(_txtCondiciones);
            _txtCosto.Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold);
            Tema.AplicarBotonSecundario(_btnNuevoProveedor);
            Tema.AplicarBotonSecundario(_btnVolver);

            // Ticket
            foreach (Control c in new Control[] { _pnlTicket, _flpInsumos, _pnlTotal, _flpAcciones })
                c.BackColor = Tema.FondoPanel;

            Tema.AplicarSubtitulo(_lblTicketTit);
            _lblTicketTit.BackColor = Tema.FondoPanel;
            Tema.AplicarSubtitulo(_lblInsumosTit);
            _lblInsumosTit.BackColor = Tema.FondoPanel;

            _lblTotalRotulo.Font = Tema.FuenteMini;
            _lblTotalRotulo.ForeColor = Tema.TextoSuave;
            _lblTotalRotulo.BackColor = Tema.FondoPanel;
            _lblTotalValor.Font = new Font("Segoe UI Semibold", 21f, FontStyle.Bold);
            _lblTotalValor.BackColor = Tema.FondoPanel;
            _lblUnitario.Font = Tema.FuenteMini;
            _lblUnitario.ForeColor = Tema.TextoSuave;
            _lblUnitario.BackColor = Tema.FondoPanel;

            ActualizarTicket();
            Invalidate(true);
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;

            _lblTitulo.Text = t.Obtener("pcf_cotp_titulo") +
                              (_orden != null ? " — " + t.Obtener("pcf_orden_num") + " #" + _orden.NumeroCompra : "");
            _lblAyuda.Text = t.Obtener("pcf_cotp_ayuda");
            _lblProvTit.Text = t.Obtener("pcf_cotp_proveedor_tit");
            _lblOfertaTit.Text = t.Obtener("pcf_cotp_oferta_tit");
            _lblBuscar.Text = t.Obtener("buscar");
            _btnNuevoProveedor.Text = "＋ " + t.Obtener("pcf_nuevo_proveedor");
            _lblCosto.Text = t.Obtener("pcf_cotp_costo");
            _lblCondiciones.Text = t.Obtener("pcf_cotp_condiciones");
            _lblTicketTit.Text = t.Obtener("pcf_cotp_ticket");
            _lblInsumosTit.Text = t.Obtener("pcf_cotp_insumos");
            _lblTotalRotulo.Text = t.Obtener("pcf_cotp_total");
            _btnRegistrar.Text = t.Obtener("pcf_cotp_registrar");
            _btnVolver.Text = t.Obtener("pcf_cotp_volver");

            ActualizarTicket();
        }
    }
}
