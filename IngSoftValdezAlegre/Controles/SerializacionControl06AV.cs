using BE;
using BLL;
using BLL.Excepciones;
using IngSoftValdezAlegre.Common;
using IngSoftValdezAlegre.UI;
using SER;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    /// <summary>
    /// A03 Serialización. Primero se elige qué hacer y después se sigue un asistente de
    /// cuatro pasos, como el resto del sistema:
    ///   Serializar     — 1 clase y objetos, 2 ubicación, 3 serializar, 4 verificar.
    ///   Des-serializar — 5 ubicación de origen, 6 archivo, 7 des-serializar, 8 verificar.
    /// La lógica está en <see cref="SerializacionBLL06AV"/>; acá solo se muestra.
    /// </summary>
    [DesignerCategory("Code")]
    internal class SerializacionControl06AV : UserControl, IIdiomaAplicable06AV
    {
        private readonly Panel _inicio;
        private readonly Label _lblTitulo, _lblSubtitulo, _lblPregunta;
        private readonly TarjetaOpcion06AV _tarjetaSerializar, _tarjetaDeserializar;
        private readonly AsistenteSerializar06AV _serializar;
        private readonly AsistenteDeserializar06AV _deserializar;

        public SerializacionControl06AV()
        {
            _lblTitulo = new Label { AutoSize = true, Dock = DockStyle.Top };
            _lblSubtitulo = new Label { AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(2, 4, 0, 10) };
            _lblPregunta = new Label { AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(0, 10, 0, 8) };

            _tarjetaSerializar = new TarjetaOpcion06AV { Width = 420, Height = 76, Icono = IconoPcf06AV.Caja };
            _tarjetaDeserializar = new TarjetaOpcion06AV { Width = 420, Height = 76, Icono = IconoPcf06AV.Lupa };
            _tarjetaSerializar.Elegida += (s, e) => Mostrar(_serializar);
            _tarjetaDeserializar.Elegida += (s, e) => Mostrar(_deserializar);
            var tarjetas = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = true
            };
            tarjetas.Controls.Add(_tarjetaSerializar);
            tarjetas.Controls.Add(_tarjetaDeserializar);

            _inicio = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 12), AutoScroll = true };
            _inicio.Controls.Add(tarjetas);
            _inicio.Controls.Add(_lblPregunta);
            _inicio.Controls.Add(_lblSubtitulo);
            _inicio.Controls.Add(_lblTitulo);

            _serializar = new AsistenteSerializar06AV { Dock = DockStyle.Fill, Visible = false };
            _deserializar = new AsistenteDeserializar06AV { Dock = DockStyle.Fill, Visible = false };
            _serializar.Salir += (s, e) => Mostrar(null);
            _deserializar.Salir += (s, e) => Mostrar(null);

            Controls.Add(_inicio);
            Controls.Add(_serializar);
            Controls.Add(_deserializar);

            AplicarTema();
            AplicarIdioma();
            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Disposed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;
            Tema.TemaChanged += AplicarTema;
            Disposed += (s, e) => Tema.TemaChanged -= AplicarTema;
        }

        /// <summary>Muestra un asistente desde el paso 1, o la elección de operación con null.</summary>
        private void Mostrar(Control asistente)
        {
            _inicio.Visible = asistente == null;
            _serializar.Visible = asistente == _serializar;
            _deserializar.Visible = asistente == _deserializar;
            if (asistente == _serializar) _serializar.Empezar();
            if (asistente == _deserializar) _deserializar.Empezar();
            (asistente ?? _inicio).BringToFront();
        }

        public void AplicarTema()
        {
            Tema.AplicarControl(this);
            BackColor = Tema.FondoApp;
            _inicio.BackColor = Tema.FondoApp;
            foreach (Control c in _inicio.Controls) c.BackColor = Tema.FondoApp;
            Tema.AplicarTitulo(_lblTitulo);
            _lblSubtitulo.Font = Tema.FuenteRegular;
            _lblSubtitulo.ForeColor = Tema.TextoSuave;
            Tema.AplicarSubtitulo(_lblPregunta);
            _serializar.AplicarTema();
            _deserializar.AplicarTema();
            Invalidate(true);
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            _lblTitulo.Text = t.Obtener("ser_titulo");
            _lblSubtitulo.Text = t.Obtener("ser_subtitulo");
            _lblPregunta.Text = t.Obtener("ser_que_hacer");
            _tarjetaSerializar.Titulo = t.Obtener("ser_op_serializar");
            _tarjetaSerializar.Subtitulo = t.Obtener("ser_op_serializar_sub");
            _tarjetaDeserializar.Titulo = t.Obtener("ser_op_deserializar");
            _tarjetaDeserializar.Subtitulo = t.Obtener("ser_op_deserializar_sub");
            _tarjetaSerializar.Invalidate();
            _tarjetaDeserializar.Invalidate();
            _serializar.AplicarIdioma();
            _deserializar.AplicarIdioma();
        }

        // ══════════════════════════════════════════════════════════════════
        //  Textos y filas compartidos por los dos asistentes
        // ══════════════════════════════════════════════════════════════════

        internal static string TextoClase(ClaseSerializable06AV c)
        {
            var t = GestorIdioma06AV.Instancia;
            switch (c)
            {
                default: return t.Obtener("ser_clase_venta");
            }
        }

        /// <summary>El nombre de la clase sin la aclaración entre paréntesis, para los destacados.</summary>
        internal static string TextoClaseCorto(ClaseSerializable06AV c)
        {
            string texto = TextoClase(c);
            int i = texto.IndexOf(" (", StringComparison.Ordinal);
            return i > 0 ? texto.Substring(0, i) : texto;
        }

        internal static string TextoEstado(EstadoVerificacion06AV e)
        {
            var t = GestorIdioma06AV.Instancia;
            switch (e)
            {
                case EstadoVerificacion06AV.Coincide: return t.Obtener("ser_est_coincide");
                case EstadoVerificacion06AV.Difiere: return t.Obtener("ser_est_difiere");
                case EstadoVerificacion06AV.NoExisteEnBase: return t.Obtener("ser_est_no_existe");
                default: return t.Obtener("ser_est_falta");
            }
        }

        internal static string TextoEstadoVenta(EstadoVenta06AV e)
        {
            var t = GestorIdioma06AV.Instancia;
            switch (e)
            {
                case EstadoVenta06AV.Pendiente: return t.Obtener("pcf_est_vta_pendiente");
                case EstadoVenta06AV.Senada: return t.Obtener("pcf_est_vta_senada");
                case EstadoVenta06AV.EnProduccion: return t.Obtener("pcf_est_vta_produccion");
                case EstadoVenta06AV.Entregada: return t.Obtener("pcf_est_vta_entregada");
                default: return t.Obtener("pcf_est_vta_anulada");
            }
        }

        // TextBox multilínea necesita CRLF; el XML se graba con el salto del sistema.
        internal static string NormalizarSaltos(string texto) => (texto ?? "").Replace("\r\n", "\n").Replace("\n", "\r\n");

        internal static DataGridView NuevaGrilla(bool editable = false) => new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = !editable, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false, BorderStyle = BorderStyle.None
        };

        internal static TextBox NuevoXml() => new TextBox
        {
            Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false,
            Font = new Font("Consolas", 9.5f)
        };

        internal static void Columna(DataGridView g, string nombre, string clave, float peso, int minimo)
        {
            DataGridViewColumn c = g.Columns[nombre];
            if (c == null) return;
            c.HeaderText = GestorIdioma06AV.Instancia.Obtener(clave);
            c.FillWeight = peso;
            c.MinimumWidth = minimo;
        }

        /// <summary>Columnas y colores de una grilla de resultados de verificación.</summary>
        internal static void PrepararResultados(DataGridView g)
        {
            g.DataBindingComplete += (s, e) =>
            {
                Columna(g, "Clave", "ser_col_clave", 40, 60);
                Columna(g, "Descripcion", "ser_col_descripcion", 110, 140);
                Columna(g, "Estado", "ser_col_estado", 60, 110);
                Columna(g, "Detalle", "ser_col_detalle", 220, 200);
                g.ClearSelection();
            };
            g.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || !(g.Rows[e.RowIndex].DataBoundItem is ResultadoVm vm)) return;
                e.CellStyle.BackColor = vm.EstadoValor == EstadoVerificacion06AV.Coincide ? Tema.ExitoSuave
                                      : vm.EstadoValor == EstadoVerificacion06AV.Difiere ? Tema.AdvertenciaSuave : Tema.PeligroSuave;
                if (g.Columns[e.ColumnIndex].Name == "Estado")
                {
                    e.CellStyle.Font = Tema.FuenteBold;
                    e.CellStyle.ForeColor = vm.EstadoValor == EstadoVerificacion06AV.Coincide ? Tema.Exito
                                          : vm.EstadoValor == EstadoVerificacion06AV.Difiere ? Tema.Advertencia : Tema.Peligro;
                }
            };
        }

        /// <summary>Franja de resultado: verde si salió bien, roja si no.</summary>
        internal static void Franja(Label l, string texto, bool ok)
        {
            l.Text = texto;
            l.Visible = !string.IsNullOrEmpty(texto);
            l.ForeColor = ok ? Tema.Exito : Tema.Peligro;
            l.BackColor = ok ? Tema.ExitoSuave : Tema.PeligroSuave;
        }

        internal static Label NuevaFranja() => new Label
        {
            AutoSize = true, Padding = new Padding(12, 10, 12, 10), Visible = false, Tag = "propio"
        };

        internal static void Aviso(Control origen, string mensaje) => ConfirmacionForm.MostrarInfo(
            mensaje, GestorIdioma06AV.Instancia.Obtener("aviso"), ConfirmacionForm.TipoConfirmacion.Advertencia, origen.FindForm());

        internal class ObjetoVm
        {
            public ObjetoVm(object objeto) { Objeto = objeto; Refrescar(); }

            public bool Incluir { get; set; }
            public string Clave { get; private set; }
            public string Descripcion { get; private set; }
            public string Detalle { get; private set; }
            [Browsable(false)] public object Objeto { get; }

            public void Refrescar()
            {
                Clave = SerializacionBLL06AV.ClaveDe(Objeto);
                switch (Objeto)
                {
                    case Venta06AV v:
                        Descripcion = (v.Cliente?.NombreCompleto ?? "-") + " · " + (v.Computadora?.Nombre ?? "-");
                        Detalle = v.FechaVenta.ToString("dd/MM/yyyy") + " · " + TextoEstadoVenta(v.Estado) + " · " +
                                  v.PrecioTotal.ToString("C0", CultureInfo.CurrentCulture);
                        break;
                    default:
                        Descripcion = Objeto?.ToString();
                        Detalle = "";
                        break;
                }
            }
        }

        internal class ResultadoVm
        {
            public ResultadoVm(ResultadoVerificacion06AV r)
            {
                Clave = r.Clave;
                Descripcion = r.Descripcion;
                EstadoValor = r.Estado;
                Estado = TextoEstado(r.Estado);
                Detalle = r.Detalle ?? "";
            }

            public string Clave { get; }
            public string Descripcion { get; }
            public string Estado { get; }
            public string Detalle { get; }
            [Browsable(false)] public EstadoVerificacion06AV EstadoValor { get; }
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  CU-SER01 · Serializar (pasos 1 a 4)
    // ══════════════════════════════════════════════════════════════════════

    [DesignerCategory("Code")]
    internal class AsistenteSerializar06AV : AsistenteBase06AV
    {
        private readonly SerializacionBLL06AV _bll = new SerializacionBLL06AV();

        private ClaseSerializable06AV? _clase;
        private BindingList<SerializacionControl06AV.ObjetoVm> _filas = new BindingList<SerializacionControl06AV.ObjetoVm>();
        private string _nombrePropuesto;
        private string _rutaSerializada;
        private List<object> _serializados;
        private InformeVerificacion06AV _informe;
        private bool _verXml;

        // Paso 1
        private Label lblClaseTit, lblObjetosTit, lblSeleccion;
        private FlowLayoutPanel flpClases;
        private Button btnTodos, btnNinguno;
        private DataGridView grillaObjetos;

        // Paso 2
        private Label lblCarpetaTit, lblArchivoTit, lblAvisoDestino;
        private TextBox txtCarpeta, txtArchivo;
        private Button btnExaminar;

        // Paso 3
        private Label lblListoTit, lblListoDet, lblResultadoSer;

        // Paso 4
        private Label lblResultadoVerif;
        private Button btnVerPorObjeto, btnVerXml;
        private DataGridView grillaResultados;
        private TextBox txtXml;

        // Lateral y barra
        private Label lblResumenTit;
        private FichaDatos06AV ficha;
        private Button btnSerializar, btnVerificar, btnOtra, btnSalir;

        public event EventHandler Salir;

        public AsistenteSerializar06AV()
        {
            Pasos.PrimerNumero = 1;
            ConstruirPaso1(NuevaPagina());
            ConstruirPaso2(NuevaPagina());
            ConstruirPaso3(NuevaPagina());
            ConstruirPaso4(NuevaPagina());

            lblResumenTit = new Label { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 8) };
            ficha = new FichaDatos06AV { Dock = DockStyle.Top };
            Lateral.Controls.Add(ficha);
            Lateral.Controls.Add(lblResumenTit);

            btnSalir = AgregarBotonBarra(150, (s, e) => Salir?.Invoke(this, EventArgs.Empty), true);
            btnSerializar = AgregarBotonBarra(170, (s, e) => Serializar());
            btnVerificar = AgregarBotonBarra(170, (s, e) => Verificar());
            btnOtra = AgregarBotonBarra(170, (s, e) => Empezar());

            AplicarTema();
        }

        /// <summary>Arranca desde el paso 1 con todo limpio.</summary>
        public void Empezar()
        {
            _clase = null;
            _filas = new BindingList<SerializacionControl06AV.ObjetoVm>();
            grillaObjetos.DataSource = _filas;
            txtCarpeta.Clear();
            txtArchivo.Clear();
            _nombrePropuesto = null;
            Reiniciar();
            IrA(0);
            // Solo se serializan ventas: la clase queda elegida y se listan las ventas.
            ElegirClase(ClaseSerializable06AV.Venta);
        }

        // ── Armado ───────────────────────────────────────────────────────
        private void ConstruirPaso1(Panel pagina)
        {
            lblClaseTit = Rotulo();
            flpClases = Fila();
            foreach (ClaseSerializable06AV c in Enum.GetValues(typeof(ClaseSerializable06AV)))
            {
                var tarjeta = new TarjetaOpcion06AV
                {
                    Valor = c, Width = 290,
                    Icono = IconoPcf06AV.Carrito
                };
                ClaseSerializable06AV local = c;
                tarjeta.Elegida += (s, e) => ElegirClase(local);
                flpClases.Controls.Add(tarjeta);
            }

            lblObjetosTit = Rotulo();
            btnTodos = NuevoBoton(150);
            btnNinguno = NuevoBoton(150);
            btnTodos.Margin = new Padding(0, 0, 8, 6);
            btnNinguno.Margin = new Padding(0, 0, 12, 6);
            btnTodos.Click += (s, e) => Marcar(true);
            btnNinguno.Click += (s, e) => Marcar(false);
            lblSeleccion = new Label { AutoSize = true, Margin = new Padding(0, 9, 0, 0) };

            grillaObjetos = SerializacionControl06AV.NuevaGrilla(true);
            grillaObjetos.DataSource = _filas;
            grillaObjetos.DataBindingComplete += (s, e) => FormatearObjetos();
            // El tilde se confirma al hacer clic, no al salir de la celda.
            grillaObjetos.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (grillaObjetos.IsCurrentCellDirty) grillaObjetos.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            grillaObjetos.CellValueChanged += (s, e) => { if (e.RowIndex >= 0) CambioSeleccion(); };

            pagina.Controls.Add(grillaObjetos);
            pagina.Controls.Add(Pila(lblClaseTit, flpClases, lblObjetosTit, Fila(btnTodos, btnNinguno, lblSeleccion)));
        }

        private void ConstruirPaso2(Panel pagina)
        {
            lblCarpetaTit = Rotulo();
            txtCarpeta = new TextBox { Width = 460, ReadOnly = true, Margin = new Padding(0, 4, 8, 0) };
            btnExaminar = NuevoBoton(130);
            btnExaminar.Margin = new Padding(0);
            btnExaminar.Click += (s, e) => ElegirCarpeta();
            lblArchivoTit = Rotulo();
            txtArchivo = new TextBox { Width = 360, MaxLength = 150 };
            txtArchivo.TextChanged += (s, e) => { Reiniciar(); ActualizarAvisoDestino(); ActualizarBarra(); };
            lblAvisoDestino = new Label { AutoSize = true, Padding = new Padding(0, 12, 0, 0) };

            pagina.Controls.Add(Apilar(lblCarpetaTit, Fila(txtCarpeta, btnExaminar), lblArchivoTit, Fila(txtArchivo), lblAvisoDestino));
        }

        private void ConstruirPaso3(Panel pagina)
        {
            lblListoTit = Rotulo();
            lblListoDet = new Label { AutoSize = true, Padding = new Padding(0, 0, 0, 12) };
            lblResultadoSer = SerializacionControl06AV.NuevaFranja();
            pagina.Controls.Add(Apilar(lblListoTit, lblListoDet, Fila(lblResultadoSer)));
        }

        private void ConstruirPaso4(Panel pagina)
        {
            lblResultadoVerif = SerializacionControl06AV.NuevaFranja();
            btnVerPorObjeto = NuevoBoton(170);
            btnVerXml = NuevoBoton(170);
            btnVerPorObjeto.Margin = new Padding(0, 10, 6, 8);
            btnVerXml.Margin = new Padding(0, 10, 6, 8);
            btnVerPorObjeto.Click += (s, e) => { _verXml = false; MostrarVista(); };
            btnVerXml.Click += (s, e) => { _verXml = true; MostrarVista(); };

            grillaResultados = SerializacionControl06AV.NuevaGrilla();
            SerializacionControl06AV.PrepararResultados(grillaResultados);
            txtXml = SerializacionControl06AV.NuevoXml();

            pagina.Controls.Add(grillaResultados);
            pagina.Controls.Add(txtXml);
            pagina.Controls.Add(Pila(Fila(lblResultadoVerif), Fila(btnVerPorObjeto, btnVerXml)));
        }

        // ── Pasos ────────────────────────────────────────────────────────
        private List<object> Seleccionados() => _filas.Where(f => f.Incluir).Select(f => f.Objeto).ToList();

        private string Ruta()
        {
            string nombre = (txtArchivo.Text ?? "").Trim();
            if (nombre.Length == 0 || string.IsNullOrWhiteSpace(txtCarpeta.Text)) return null;
            if (!nombre.EndsWith(SerializacionBLL06AV.Extension, StringComparison.OrdinalIgnoreCase))
                nombre += SerializacionBLL06AV.Extension;
            try { return Path.Combine(txtCarpeta.Text, nombre); }
            catch (ArgumentException) { return null; }
        }

        protected override bool PasoCompleto(int paso)
        {
            switch (paso)
            {
                case 0: return _clase != null && _filas.Any(f => f.Incluir);
                case 1: return Ruta() != null;
                case 2: return _rutaSerializada != null;
                default: return _informe != null;
            }
        }

        protected override bool MuestraSiguiente(int paso) => paso < 2 || (paso == 2 && _rutaSerializada != null);

        protected override void AlSiguiente()
        {
            if (PasoActual == 1)
            {
                // Paso 2: se valida el destino con las reglas de la BLL antes de avanzar.
                string ruta = Ruta();
                try { SerializacionBLL06AV.ValidarDestino(ruta); }
                catch (ValidacionException06AV ex)
                {
                    lblAvisoDestino.Text = ex.Message;
                    lblAvisoDestino.ForeColor = Tema.Peligro;
                    return;
                }
            }
            base.AlSiguiente();
        }

        protected override void AlMostrarPaso(int paso)
        {
            foreach (TarjetaOpcion06AV c in flpClases.Controls.OfType<TarjetaOpcion06AV>())
                c.Seleccionada = _clase.HasValue && (ClaseSerializable06AV)c.Valor == _clase.Value;
            if (paso == 1 && string.IsNullOrWhiteSpace(txtArchivo.Text)) ProponerNombre();
            if (paso == 1) ActualizarAvisoDestino();
            if (paso == 3) MostrarVista();
            ActualizarResumen();
        }

        protected override void ActualizarBotonesPropios()
        {
            if (btnSerializar == null) return;
            btnSerializar.Visible = PasoActual == 2 && _rutaSerializada == null;
            btnVerificar.Visible = PasoActual == 3;
            btnOtra.Visible = PasoActual == 3 && _informe != null;
            Tema.AplicarBotonAcento(btnSerializar);
            Habilitar(btnVerificar, true, _informe == null);
            Habilitar(btnOtra, true, false);
            Tema.AplicarBotonSecundario(btnSalir);
            Habilitar(btnTodos, _filas.Count > 0, false);
            Habilitar(btnNinguno, _filas.Count > 0, false);
            ActualizarResumen();
        }

        // ── Paso 1 ───────────────────────────────────────────────────────
        private void ElegirClase(ClaseSerializable06AV clase)
        {
            _clase = clase;
            List<object> objetos;
            try
            {
                Cursor = Cursors.WaitCursor;
                objetos = _bll.ObtenerObjetos(clase);
            }
            catch (Exception ex)
            {
                objetos = new List<object>();
                SerializacionControl06AV.Aviso(this, ex.Message);
            }
            finally { Cursor = Cursors.Default; }

            MostrarObjetos(objetos);
            // Un nombre que propuso el sistema se actualiza con la clase; uno escrito a mano, no.
            if (string.IsNullOrWhiteSpace(txtArchivo.Text) || txtArchivo.Text == _nombrePropuesto) ProponerNombre();
            AlMostrarPaso(PasoActual);
        }

        private void MostrarObjetos(List<object> objetos)
        {
            _filas = new BindingList<SerializacionControl06AV.ObjetoVm>(objetos.Select(o => new SerializacionControl06AV.ObjetoVm(o)).ToList());
            grillaObjetos.DataSource = _filas;
            CambioSeleccion();
        }

        private void Marcar(bool incluir)
        {
            grillaObjetos.EndEdit();
            foreach (var f in _filas) f.Incluir = incluir;
            _filas.ResetBindings();
            CambioSeleccion();
        }

        private void CambioSeleccion()
        {
            var t = GestorIdioma06AV.Instancia;
            lblSeleccion.Text = _clase == null ? "" :
                _filas.Count == 0 ? t.Obtener("ser_sin_objetos")
                                  : t.Obtener("ser_seleccionados", _filas.Count(f => f.Incluir), _filas.Count);
            Reiniciar();
            ActualizarBarra();
        }

        private void FormatearObjetos()
        {
            foreach (DataGridViewColumn c in grillaObjetos.Columns) c.ReadOnly = c.Name != "Incluir";
            SerializacionControl06AV.Columna(grillaObjetos, "Clave", "ser_col_clave", 45, 60);
            SerializacionControl06AV.Columna(grillaObjetos, "Descripcion", "ser_col_descripcion", 190, 160);
            SerializacionControl06AV.Columna(grillaObjetos, "Detalle", "ser_col_detalle", 170, 160);
            DataGridViewColumn incluir = grillaObjetos.Columns["Incluir"];
            if (incluir != null)
            {
                incluir.HeaderText = GestorIdioma06AV.Instancia.Obtener("ser_col_incluir");
                incluir.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                incluir.Width = 70;
            }
            grillaObjetos.ClearSelection();
        }

        // ── Paso 2 ───────────────────────────────────────────────────────
        private void ElegirCarpeta()
        {
            using (var dlg = new FolderBrowserDialog
            {
                Description = GestorIdioma06AV.Instancia.Obtener("ser_elegir_destino"),
                ShowNewFolderButton = true,
                SelectedPath = Directory.Exists(txtCarpeta.Text) ? txtCarpeta.Text : SerializacionBLL06AV.CarpetaPorDefecto()
            })
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                txtCarpeta.Text = dlg.SelectedPath;
            }
            if (string.IsNullOrWhiteSpace(txtArchivo.Text)) ProponerNombre();
            Reiniciar();
            ActualizarAvisoDestino();
            ActualizarBarra();
        }

        private void ProponerNombre()
        {
            if (_clase == null) return;
            _nombrePropuesto = SerializacionBLL06AV.ProponerNombreArchivo(_clase.Value, DateTime.Now);
            txtArchivo.Text = _nombrePropuesto;
        }

        private void ActualizarAvisoDestino()
        {
            var t = GestorIdioma06AV.Instancia;
            string ruta = Ruta();
            bool existe = ruta != null && File.Exists(ruta);
            lblAvisoDestino.Text = ruta == null ? t.Obtener("ser_elegi_carpeta")
                                 : existe ? t.Obtener("ser_ya_existe", Path.GetFileName(ruta))
                                 : t.Obtener("ser_se_guardara", ruta);
            lblAvisoDestino.ForeColor = existe ? Tema.Advertencia : Tema.TextoSuave;
        }

        // ── Paso 3 ───────────────────────────────────────────────────────
        private void Serializar()
        {
            var t = GestorIdioma06AV.Instancia;
            string ruta = Ruta();
            if (_clase == null || ruta == null) return;

            if (File.Exists(ruta) && !ConfirmacionForm.Mostrar(
                    t.Obtener("ser_reemplazar", Path.GetFileName(ruta)), t.Obtener("ser_titulo"),
                    ConfirmacionForm.TipoConfirmacion.Pregunta, t.Obtener("ser_reemplazar_si"), t.Obtener("cancelar"), FindForm()))
                return;

            List<object> seleccion = Seleccionados();
            try
            {
                Cursor = Cursors.WaitCursor;
                PaqueteSerializado06AV paquete = _bll.Serializar(_clase.Value, seleccion, ruta);
                _rutaSerializada = ruta;
                _serializados = seleccion;
                SerializacionControl06AV.Franja(lblResultadoSer, "✓  " + t.Obtener("ser_ok_serializado", paquete.Cantidad, ruta), true);
            }
            catch (Exception ex)
            {
                SerializacionControl06AV.Franja(lblResultadoSer, ex.Message, false);
            }
            finally { Cursor = Cursors.Default; }
            ActualizarBarra();
        }

        /// <summary>Cualquier cambio en los pasos 1 o 2 invalida el archivo generado.</summary>
        private void Reiniciar()
        {
            _rutaSerializada = null;
            _serializados = null;
            _informe = null;
            if (lblResultadoSer != null) SerializacionControl06AV.Franja(lblResultadoSer, "", true);
        }

        // ── Paso 4 ───────────────────────────────────────────────────────
        private void Verificar()
        {
            if (_rutaSerializada == null) return;
            var t = GestorIdioma06AV.Instancia;
            try
            {
                Cursor = Cursors.WaitCursor;
                _informe = _bll.VerificarSerializacion(_rutaSerializada, _serializados);
                txtXml.Text = SerializacionControl06AV.NormalizarSaltos(_bll.LeerContenido(_rutaSerializada));
                int coinciden = _informe.Cuantos(EstadoVerificacion06AV.Coincide);
                bool ok = _informe.TodoCoincide();
                SerializacionControl06AV.Franja(lblResultadoVerif, ok
                    ? "✓  " + t.Obtener("ser_ok_verif_ser", coinciden)
                    : "✗  " + t.Obtener("ser_mal_verif_ser", coinciden, _informe.Resultados.Count) +
                      (_informe.ArchivoIntegro ? "" : "  " + t.Obtener("ser_alterado")), ok);
            }
            catch (Exception ex)
            {
                _informe = null;
                SerializacionControl06AV.Franja(lblResultadoVerif, ex.Message, false);
            }
            finally { Cursor = Cursors.Default; }
            MostrarVista();
            ActualizarBarra();
        }

        private void MostrarVista()
        {
            grillaResultados.DataSource = (_informe?.Resultados ?? new List<ResultadoVerificacion06AV>())
                .Select(r => new SerializacionControl06AV.ResultadoVm(r)).ToList();
            bool hay = _informe != null;
            btnVerPorObjeto.Visible = btnVerXml.Visible = hay;
            grillaResultados.Visible = hay && !_verXml;
            txtXml.Visible = hay && _verXml;
            (_verXml ? (Control)txtXml : grillaResultados).BringToFront();
            Habilitar(btnVerPorObjeto, true, !_verXml);
            Habilitar(btnVerXml, true, _verXml);
            if (!hay) SerializacionControl06AV.Franja(lblResultadoVerif, "", true);
        }

        // ── Lateral ──────────────────────────────────────────────────────
        private void ActualizarResumen()
        {
            if (ficha == null) return;
            var t = GestorIdioma06AV.Instancia;
            string ruta = Ruta();
            string estado = _informe != null ? t.Obtener(_informe.TodoCoincide() ? "ser_estado_verificado" : "ser_estado_con_dif")
                          : _rutaSerializada != null ? t.Obtener("ser_estado_grabado") : t.Obtener("ser_estado_sin_grabar");

            ficha.RotuloDestacado = t.Obtener("ser_clase_rotulo");
            ficha.ValorDestacado = _clase.HasValue ? SerializacionControl06AV.TextoClaseCorto(_clase.Value) : t.Obtener("ser_sin_elegir");
            ficha.ColorDestacado = _clase.HasValue ? Tema.Primario : Tema.TextoSuave;
            ficha.Definir(new[]
            {
                new DatoFicha06AV(t.Obtener("ser_res_objetos"), _clase.HasValue ? _filas.Count(f => f.Incluir) + " / " + _filas.Count : "—"),
                new DatoFicha06AV(t.Obtener("ser_res_estado"), estado),
                new DatoFicha06AV(t.Obtener("ser_res_carpeta"), string.IsNullOrWhiteSpace(txtCarpeta.Text) ? "—" : txtCarpeta.Text, true),
                new DatoFicha06AV(t.Obtener("ser_res_archivo"), ruta == null ? "—" : Path.GetFileName(ruta), true)
            });
            ficha.Height = ficha.AltoNecesario;
            ficha.Invalidate();

            if (lblListoDet != null)
                lblListoDet.Text = t.Obtener("ser_listo_det", _filas.Count(f => f.Incluir),
                    _clase.HasValue ? SerializacionControl06AV.TextoClase(_clase.Value) : "—", ruta ?? "—");
        }

        // ── Tema e idioma ────────────────────────────────────────────────
        public override void AplicarTema()
        {
            base.AplicarTema();
            foreach (Label l in new[] { lblClaseTit, lblObjetosTit, lblCarpetaTit, lblArchivoTit, lblListoTit }) Tema.AplicarSubtitulo(l);
            foreach (Label l in new[] { lblSeleccion, lblListoDet }) { l.Font = Tema.FuenteRegular; l.ForeColor = Tema.Texto; }
            lblAvisoDestino.Font = Tema.FuenteRegular;
            foreach (Label l in new[] { lblResultadoSer, lblResultadoVerif }) l.Font = Tema.FuenteBold;
            Tema.AplicarSubtitulo(lblResumenTit);
            lblResumenTit.BackColor = Tema.FondoPanel;
            Tema.AplicarEntrada(txtCarpeta);
            Tema.AplicarEntrada(txtArchivo);
            Tema.AplicarEntrada(txtXml);
            txtXml.Font = new Font("Consolas", 9.5f);
            Tema.AplicarBotonSecundario(btnExaminar);
            Tema.AplicarGrilla(grillaObjetos);
            Tema.AplicarGrilla(grillaResultados);
            ActualizarBarra();
            if (PasoActual == 3) MostrarVista();
        }

        public override void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            LblTitulo.Text = t.Obtener("ser_titulo") + " — " + t.Obtener("ser_op_serializar");
            LblSubtitulo.Text = t.Obtener("ser_op_serializar_sub");
            Pasos.DefinirPasos(new[] { t.Obtener("ser_paso1"), t.Obtener("ser_paso2"), t.Obtener("ser_paso3"), t.Obtener("ser_paso4") });
            LblAyuda.Text = t.Obtener("ser_hint" + (PasoActual + 1));
            BtnAtras.Text = "←  " + t.Obtener("pcf_asis_atras");
            BtnSiguiente.Text = t.Obtener("pcf_asis_siguiente") + "  →";
            btnSalir.Text = t.Obtener("ser_cambiar_op");
            btnSerializar.Text = t.Obtener("ser_btn_serializar");
            btnVerificar.Text = t.Obtener("ser_btn_verificar_ser");
            btnOtra.Text = t.Obtener("ser_otra");

            lblClaseTit.Text = t.Obtener("ser_que_clase");
            foreach (TarjetaOpcion06AV c in flpClases.Controls.OfType<TarjetaOpcion06AV>())
            {
                var clase = (ClaseSerializable06AV)c.Valor;
                c.Titulo = SerializacionControl06AV.TextoClase(clase);
                c.Subtitulo = t.Obtener("ser_clase_venta_sub");
                c.Invalidate();
            }
            lblObjetosTit.Text = t.Obtener("ser_que_objetos");
            btnTodos.Text = t.Obtener("ser_todos");
            btnNinguno.Text = t.Obtener("ser_ninguno");
            lblCarpetaTit.Text = t.Obtener("ser_carpeta_destino");
            lblArchivoTit.Text = t.Obtener("ser_nombre_archivo");
            btnExaminar.Text = t.Obtener("ser_examinar");
            lblListoTit.Text = t.Obtener("ser_listo_tit");
            btnVerPorObjeto.Text = t.Obtener("ser_ver_objetos");
            btnVerXml.Text = t.Obtener("ser_ver_xml");
            lblResumenTit.Text = t.Obtener("ser_resumen");

            foreach (var f in _filas) f.Refrescar();
            _filas.ResetBindings();
            CambioSeleccionTexto();
            if (PasoActual == 1) ActualizarAvisoDestino();
            ActualizarResumen();
        }

        private void CambioSeleccionTexto()
        {
            var t = GestorIdioma06AV.Instancia;
            lblSeleccion.Text = _clase == null ? "" :
                _filas.Count == 0 ? t.Obtener("ser_sin_objetos")
                                  : t.Obtener("ser_seleccionados", _filas.Count(f => f.Incluir), _filas.Count);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  CU-SER02 · Des-serializar (pasos 5 a 8)
    // ══════════════════════════════════════════════════════════════════════

    [DesignerCategory("Code")]
    internal class AsistenteDeserializar06AV : AsistenteBase06AV
    {
        private readonly SerializacionBLL06AV _bll = new SerializacionBLL06AV();

        private List<string> _archivos = new List<string>();
        private string _rutaElegida;
        private PaqueteSerializado06AV _paquete;
        private InformeVerificacion06AV _informe;
        private bool _cargando;

        // Paso 5
        private Label lblOrigenTit, lblEncontrados;
        private TextBox txtOrigen;
        private Button btnExaminar;

        // Paso 6
        private Label lblArchivosTit, lblVistaTit;
        private DataGridView grillaArchivos;
        private TextBox txtVista;

        // Paso 7
        private FichaDatos06AV fichaPaquete;
        private Label lblReconstruidosTit, lblResultadoDes;
        private DataGridView grillaObjetos;

        // Paso 8
        private Label lblIntegridad, lblResumenVerif;
        private DataGridView grillaResultados;

        // Lateral y barra
        private Label lblResumenTit;
        private FichaDatos06AV ficha;
        private Button btnDeserializar, btnVerificar, btnOtro, btnSalir;

        public event EventHandler Salir;

        public AsistenteDeserializar06AV()
        {
            Pasos.PrimerNumero = 5;
            ConstruirPaso5(NuevaPagina());
            ConstruirPaso6(NuevaPagina());
            ConstruirPaso7(NuevaPagina());
            ConstruirPaso8(NuevaPagina());

            lblResumenTit = new Label { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 8) };
            ficha = new FichaDatos06AV { Dock = DockStyle.Top };
            Lateral.Controls.Add(ficha);
            Lateral.Controls.Add(lblResumenTit);

            btnSalir = AgregarBotonBarra(150, (s, e) => Salir?.Invoke(this, EventArgs.Empty), true);
            btnDeserializar = AgregarBotonBarra(180, (s, e) => Deserializar());
            btnVerificar = AgregarBotonBarra(170, (s, e) => Verificar());
            btnOtro = AgregarBotonBarra(170, (s, e) => Empezar());

            AplicarTema();
        }

        public void Empezar()
        {
            txtOrigen.Clear();
            _archivos = new List<string>();
            _cargando = true;
            grillaArchivos.DataSource = null;
            _cargando = false;
            txtVista.Clear();
            _rutaElegida = null;
            Reiniciar();
            IrA(0);
        }

        // ── Armado ───────────────────────────────────────────────────────
        private void ConstruirPaso5(Panel pagina)
        {
            lblOrigenTit = Rotulo();
            txtOrigen = new TextBox { Width = 460, ReadOnly = true, Margin = new Padding(0, 4, 8, 0) };
            btnExaminar = NuevoBoton(130);
            btnExaminar.Margin = new Padding(0);
            btnExaminar.Click += (s, e) => ElegirCarpeta();
            lblEncontrados = new Label { AutoSize = true, Padding = new Padding(0, 12, 0, 0) };
            pagina.Controls.Add(Apilar(lblOrigenTit, Fila(txtOrigen, btnExaminar), lblEncontrados));
        }

        private void ConstruirPaso6(Panel pagina)
        {
            lblArchivosTit = Rotulo();
            grillaArchivos = SerializacionControl06AV.NuevaGrilla();
            grillaArchivos.Dock = DockStyle.Top;
            grillaArchivos.Height = 190;
            grillaArchivos.DataBindingComplete += (s, e) =>
            {
                SerializacionControl06AV.Columna(grillaArchivos, "Archivo", "ser_col_archivo", 170, 180);
                SerializacionControl06AV.Columna(grillaArchivos, "Modificado", "ser_col_modificado", 80, 120);
                SerializacionControl06AV.Columna(grillaArchivos, "Tamanio", "ser_col_tamanio", 50, 80);
                if (grillaArchivos.Columns["Tamanio"] != null)
                    grillaArchivos.Columns["Tamanio"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            };
            grillaArchivos.SelectionChanged += (s, e) => { if (!_cargando) ElegirArchivo(); };
            lblVistaTit = Rotulo();
            txtVista = SerializacionControl06AV.NuevoXml();

            pagina.Controls.Add(txtVista);
            pagina.Controls.Add(Pila(lblArchivosTit, grillaArchivos, lblVistaTit));
        }

        private void ConstruirPaso7(Panel pagina)
        {
            lblResultadoDes = SerializacionControl06AV.NuevaFranja();
            fichaPaquete = new FichaDatos06AV();
            lblReconstruidosTit = Rotulo();
            grillaObjetos = SerializacionControl06AV.NuevaGrilla();
            grillaObjetos.DataBindingComplete += (s, e) =>
            {
                SerializacionControl06AV.Columna(grillaObjetos, "Clave", "ser_col_clave", 45, 60);
                SerializacionControl06AV.Columna(grillaObjetos, "Descripcion", "ser_col_descripcion", 190, 160);
                SerializacionControl06AV.Columna(grillaObjetos, "Detalle", "ser_col_detalle", 170, 160);
                grillaObjetos.ClearSelection();
            };
            pagina.Controls.Add(grillaObjetos);
            pagina.Controls.Add(Pila(Fila(lblResultadoDes), fichaPaquete, lblReconstruidosTit));
        }

        private void ConstruirPaso8(Panel pagina)
        {
            lblIntegridad = SerializacionControl06AV.NuevaFranja();
            lblResumenVerif = new Label { AutoSize = true, Padding = new Padding(0, 8, 0, 8) };
            grillaResultados = SerializacionControl06AV.NuevaGrilla();
            SerializacionControl06AV.PrepararResultados(grillaResultados);
            pagina.Controls.Add(grillaResultados);
            pagina.Controls.Add(Pila(Fila(lblIntegridad), lblResumenVerif));
        }

        // ── Pasos ────────────────────────────────────────────────────────
        protected override bool PasoCompleto(int paso)
        {
            switch (paso)
            {
                case 0: return _archivos.Count > 0;
                case 1: return _rutaElegida != null;
                case 2: return _paquete != null;
                default: return _informe != null;
            }
        }

        protected override bool MuestraSiguiente(int paso) => paso < 2 || (paso == 2 && _paquete != null);

        protected override void AlMostrarPaso(int paso)
        {
            if (paso == 2) MostrarPaquete();
            if (paso == 3) MostrarInforme();
            ActualizarResumen();
        }

        protected override void ActualizarBotonesPropios()
        {
            if (btnDeserializar == null) return;
            btnDeserializar.Visible = PasoActual == 2 && _paquete == null;
            btnVerificar.Visible = PasoActual == 3;
            btnOtro.Visible = PasoActual == 3 && _informe != null;
            Tema.AplicarBotonAcento(btnDeserializar);
            Habilitar(btnVerificar, true, _informe == null);
            Habilitar(btnOtro, true, false);
            Tema.AplicarBotonSecundario(btnSalir);
            ActualizarResumen();
        }

        // ── Paso 5 ───────────────────────────────────────────────────────
        private void ElegirCarpeta()
        {
            using (var dlg = new FolderBrowserDialog
            {
                Description = GestorIdioma06AV.Instancia.Obtener("ser_elegir_origen"),
                ShowNewFolderButton = false,
                SelectedPath = Directory.Exists(txtOrigen.Text) ? txtOrigen.Text : SerializacionBLL06AV.CarpetaPorDefecto()
            })
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                txtOrigen.Text = dlg.SelectedPath;
            }
            CargarArchivos();
        }

        private void CargarArchivos()
        {
            var t = GestorIdioma06AV.Instancia;
            _rutaElegida = null;
            txtVista.Clear();
            Reiniciar();
            try { _archivos = _bll.ListarArchivos(txtOrigen.Text); }
            catch (Exception ex)
            {
                _archivos = new List<string>();
                lblEncontrados.Text = ex.Message;
                lblEncontrados.ForeColor = Tema.Peligro;
                ActualizarBarra();
                return;
            }

            _cargando = true;
            try
            {
                grillaArchivos.DataSource = _archivos.Select(a => new ArchivoVm(a)).ToList();
                // Sin fila elegida: el paso 6 lo hace el usuario.
                grillaArchivos.ClearSelection();
                grillaArchivos.CurrentCell = null;
            }
            finally { _cargando = false; }

            lblEncontrados.Text = _archivos.Count == 0 ? t.Obtener("ser_sin_archivos") : t.Obtener("ser_encontrados", _archivos.Count);
            lblEncontrados.ForeColor = _archivos.Count == 0 ? Tema.Advertencia : Tema.Exito;
            ActualizarBarra();
        }

        // ── Paso 6 ───────────────────────────────────────────────────────
        private void ElegirArchivo()
        {
            var fila = grillaArchivos.SelectedRows.Count > 0 ? grillaArchivos.SelectedRows[0].DataBoundItem as ArchivoVm : null;
            if (fila == null || fila.Ruta == _rutaElegida) return;
            Reiniciar();
            try
            {
                txtVista.Text = SerializacionControl06AV.NormalizarSaltos(_bll.LeerContenido(fila.Ruta));
                _rutaElegida = fila.Ruta;
            }
            catch (Exception ex)
            {
                txtVista.Text = ex.Message;
                _rutaElegida = null;
            }
            ActualizarBarra();
        }

        // ── Paso 7 ───────────────────────────────────────────────────────
        private void Deserializar()
        {
            if (_rutaElegida == null) return;
            var t = GestorIdioma06AV.Instancia;
            try
            {
                Cursor = Cursors.WaitCursor;
                _paquete = _bll.Deserializar(_rutaElegida);
                _informe = null;
                SerializacionControl06AV.Franja(lblResultadoDes,
                    "✓  " + t.Obtener("ser_ok_deserializado", _paquete.Objetos.Count, SerializacionControl06AV.TextoClase(_paquete.Clase)), true);
            }
            catch (Exception ex)
            {
                _paquete = null;
                SerializacionControl06AV.Franja(lblResultadoDes, "✗  " + ex.Message, false);
            }
            finally { Cursor = Cursors.Default; }
            MostrarPaquete();
            ActualizarBarra();
        }

        private void MostrarPaquete()
        {
            var t = GestorIdioma06AV.Instancia;
            grillaObjetos.DataSource = (_paquete?.Objetos ?? new List<object>())
                .Select(o => new SerializacionControl06AV.ObjetoVm(o))
                .Select(v => new { v.Clave, v.Descripcion, v.Detalle })
                .ToList();
            fichaPaquete.Visible = lblReconstruidosTit.Visible = _paquete != null;
            if (_paquete == null) return;

            fichaPaquete.RotuloDestacado = t.Obtener("ser_clase_rotulo");
            fichaPaquete.ValorDestacado = SerializacionControl06AV.TextoClase(_paquete.Clase);
            fichaPaquete.Definir(new[]
            {
                new DatoFicha06AV(t.Obtener("ser_res_objetos"), _paquete.Objetos.Count.ToString()),
                new DatoFicha06AV(t.Obtener("ser_res_version"), "v" + _paquete.Version),
                new DatoFicha06AV(t.Obtener("ser_res_generado"), _paquete.FechaGeneracion.ToString("dd/MM/yyyy HH:mm")),
                new DatoFicha06AV(t.Obtener("ser_res_por"), string.IsNullOrWhiteSpace(_paquete.GeneradoPor) ? "-" : _paquete.GeneradoPor)
            });
            fichaPaquete.Height = fichaPaquete.AltoNecesario;
            fichaPaquete.Invalidate();
        }

        // ── Paso 8 ───────────────────────────────────────────────────────
        private void Verificar()
        {
            if (_paquete == null) return;
            try
            {
                Cursor = Cursors.WaitCursor;
                _informe = _bll.VerificarDeserializacion(_paquete, _rutaElegida);
            }
            catch (Exception ex)
            {
                _informe = null;
                SerializacionControl06AV.Franja(lblIntegridad, "✗  " + ex.Message, false);
            }
            finally { Cursor = Cursors.Default; }
            MostrarInforme();
            ActualizarBarra();
        }

        private void MostrarInforme()
        {
            var t = GestorIdioma06AV.Instancia;
            grillaResultados.DataSource = (_informe?.Resultados ?? new List<ResultadoVerificacion06AV>())
                .Select(r => new SerializacionControl06AV.ResultadoVm(r)).ToList();
            if (_informe == null) { lblResumenVerif.Text = ""; return; }

            SerializacionControl06AV.Franja(lblIntegridad,
                (_informe.ArchivoIntegro ? "✓  " : "✗  ") + t.Obtener(_informe.ArchivoIntegro ? "ser_integro" : "ser_alterado"),
                _informe.ArchivoIntegro);
            string resumen = t.Obtener("ser_resumen_des",
                _informe.Cuantos(EstadoVerificacion06AV.Coincide),
                _informe.Cuantos(EstadoVerificacion06AV.Difiere),
                _informe.Cuantos(EstadoVerificacion06AV.NoExisteEnBase));
            if (_informe.ErrorComparacion != null) resumen += "  ·  " + t.Obtener("ser_error_base", _informe.ErrorComparacion);
            lblResumenVerif.Text = resumen;
            lblResumenVerif.ForeColor = _informe.TodoCoincide() ? Tema.Exito : Tema.Texto;
        }

        private void Reiniciar()
        {
            _paquete = null;
            _informe = null;
            if (lblResultadoDes != null) SerializacionControl06AV.Franja(lblResultadoDes, "", true);
            if (lblIntegridad != null) SerializacionControl06AV.Franja(lblIntegridad, "", true);
        }

        // ── Lateral ──────────────────────────────────────────────────────
        private void ActualizarResumen()
        {
            if (ficha == null) return;
            var t = GestorIdioma06AV.Instancia;
            string integridad = _informe == null ? "—" : t.Obtener(_informe.ArchivoIntegro ? "ser_res_integro_si" : "ser_res_integro_no");

            ficha.RotuloDestacado = t.Obtener("ser_res_archivo");
            ficha.ValorDestacado = _rutaElegida != null ? Path.GetFileName(_rutaElegida) : t.Obtener("ser_sin_elegir");
            ficha.ColorDestacado = _rutaElegida != null ? Tema.Primario : Tema.TextoSuave;
            ficha.Definir(new[]
            {
                new DatoFicha06AV(t.Obtener("ser_clase_rotulo"), _paquete != null ? SerializacionControl06AV.TextoClase(_paquete.Clase) : "—"),
                new DatoFicha06AV(t.Obtener("ser_res_objetos"), _paquete != null ? _paquete.Objetos.Count.ToString() : "—"),
                new DatoFicha06AV(t.Obtener("ser_res_integridad"), integridad, true),
                new DatoFicha06AV(t.Obtener("ser_res_carpeta"), string.IsNullOrWhiteSpace(txtOrigen.Text) ? "—" : txtOrigen.Text, true)
            });
            ficha.Height = ficha.AltoNecesario;
            ficha.Invalidate();
        }

        // ── Tema e idioma ────────────────────────────────────────────────
        public override void AplicarTema()
        {
            base.AplicarTema();
            foreach (Label l in new[] { lblOrigenTit, lblArchivosTit, lblVistaTit, lblReconstruidosTit }) Tema.AplicarSubtitulo(l);
            lblEncontrados.Font = Tema.FuenteBold;
            lblResumenVerif.Font = Tema.FuenteBold;
            foreach (Label l in new[] { lblResultadoDes, lblIntegridad }) l.Font = Tema.FuenteBold;
            Tema.AplicarSubtitulo(lblResumenTit);
            lblResumenTit.BackColor = Tema.FondoPanel;
            Tema.AplicarEntrada(txtOrigen);
            Tema.AplicarEntrada(txtVista);
            txtVista.Font = new Font("Consolas", 9.5f);
            Tema.AplicarBotonSecundario(btnExaminar);
            foreach (DataGridView g in new[] { grillaArchivos, grillaObjetos, grillaResultados }) Tema.AplicarGrilla(g);
            ActualizarBarra();
        }

        public override void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            LblTitulo.Text = t.Obtener("ser_titulo") + " — " + t.Obtener("ser_op_deserializar");
            LblSubtitulo.Text = t.Obtener("ser_op_deserializar_sub");
            Pasos.DefinirPasos(new[] { t.Obtener("ser_paso5"), t.Obtener("ser_paso6"), t.Obtener("ser_paso7"), t.Obtener("ser_paso8") });
            LblAyuda.Text = t.Obtener("ser_hint" + (PasoActual + 5));
            BtnAtras.Text = "←  " + t.Obtener("pcf_asis_atras");
            BtnSiguiente.Text = t.Obtener("pcf_asis_siguiente") + "  →";
            btnSalir.Text = t.Obtener("ser_cambiar_op");
            btnDeserializar.Text = t.Obtener("ser_btn_deserializar");
            btnVerificar.Text = t.Obtener("ser_btn_verificar_des");
            btnOtro.Text = t.Obtener("ser_otro_archivo");

            lblOrigenTit.Text = t.Obtener("ser_carpeta_origen");
            btnExaminar.Text = t.Obtener("ser_examinar");
            lblArchivosTit.Text = t.Obtener("ser_archivos");
            lblVistaTit.Text = t.Obtener("ser_contenido_xml");
            lblReconstruidosTit.Text = t.Obtener("ser_objetos_reconstruidos");
            lblResumenTit.Text = t.Obtener("ser_resumen");
            if (string.IsNullOrWhiteSpace(txtOrigen.Text)) lblEncontrados.Text = t.Obtener("ser_elegi_origen");
            if (PasoActual == 2) MostrarPaquete();
            if (PasoActual == 3) MostrarInforme();
            ActualizarResumen();
        }

        private class ArchivoVm
        {
            public ArchivoVm(string ruta)
            {
                Ruta = ruta;
                var info = new FileInfo(ruta);
                Archivo = info.Name;
                Modificado = info.LastWriteTime.ToString("dd/MM/yyyy HH:mm");
                Tamanio = (info.Length / 1024m).ToString("0.0", CultureInfo.CurrentCulture) + " KB";
            }

            public string Archivo { get; }
            public string Modificado { get; }
            public string Tamanio { get; }
            [Browsable(false)] public string Ruta { get; }
        }
    }
}
