using BE;
using BLL.Armado;
using PCFORGE_ValdezThiago_96VA.Common;
using PCFORGE_ValdezThiago_96VA.UI;
using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA
{
    [System.ComponentModel.DesignerCategory("Code")]
    public class FRMArmarPc06AV : Form
    {
        private static bool EsSalteable(TipoComponente06AV t) => !ArmadorComputadora06AV.EsObligatorio(t);

        private readonly List<Componente06AV> _todos;
        private readonly List<TipoComponente06AV> _pasos;
        private int _paso;
        private readonly Dictionary<TipoComponente06AV, Componente06AV> _elegidos =
            new Dictionary<TipoComponente06AV, Componente06AV>();
        private readonly Dictionary<TipoComponente06AV, int> _cantidades =
            new Dictionary<TipoComponente06AV, int>();
        private readonly List<string> _avisos = new List<string>();
        private string _marca;

        public List<Componente06AV> Seleccionados =>
            ArmadorComputadora06AV.Pasos
                .Where(p => _elegidos.TryGetValue(p.Tipo, out Componente06AV c) && c != null)
                .SelectMany(p => Enumerable.Repeat(_elegidos[p.Tipo], Cantidad(p.Tipo)))
                .ToList();

        private ChasisPcControl06AV chasis;
        private PasosWizard06AV pasos;
        private Label lblChasisTit, lblPasoTit, lblPasoAyuda, lblSinOpciones, lblConsumo, lblAviso;
        private Label lblMarca, lblModulos, lblModulosInfo;
        private FlowLayoutPanel flpCatalogo, pnlMarcas, pnlCantidad;
        private Button btnAtras, btnSaltear, btnSiguiente, btnCancelar, btnAmd, btnIntel;
        private NumericUpDown nudModulos;
        private Panel pnlChasis;
        private readonly ToolTip _tip = new ToolTip { InitialDelay = 250, AutoPopDelay = 12000 };
        private bool _cargandoCantidad;

        public FRMArmarPc06AV(IEnumerable<Componente06AV> componentes,
                              IEnumerable<Componente06AV> preseleccion = null)
        {
            _todos = (componentes ?? Enumerable.Empty<Componente06AV>()).ToList();
            _pasos = ArmadorComputadora06AV.Pasos
                .Where(p => p.Obligatorio || _todos.Any(c => c.Tipo == p.Tipo))
                .Select(p => p.Tipo)
                .ToList();
            new BLL.CompatibilidadBLL06AV().Recargar();
            InferenciaEspecificacion06AV.Completar(_todos);
            if (preseleccion != null)
                foreach (var g in preseleccion.Where(c => c != null).GroupBy(c => c.Tipo))
                {
                    Componente06AV primero = g.First();
                    Componente06AV delCatalogo = _todos.FirstOrDefault(x => x.Codigo == primero.Codigo) ?? primero;
                    _elegidos[g.Key] = delCatalogo;
                    _cantidades[g.Key] = g.Count(c => c.Codigo == primero.Codigo);
                }
            Componente06AV cpu = Elegido(TipoComponente06AV.Procesador);
            if (cpu != null) _marca = ReglasCompatibilidad06AV.PlataformaDe(cpu);

            ConstruirUI();
            AplicarTema();
            Tema.TemaChanged += AplicarTema;
            FormClosed += (s, e) => Tema.TemaChanged -= AplicarTema;
            MostrarPaso();
        }

        #region Construcción

        private void ConstruirUI()
        {
            var t = GestorIdioma06AV.Instancia;

            Text = t.Obtener("pcf_armar_titulo");
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ClientSize = new Size(1000, 640);
            MinimumSize = new Size(860, 560);

            lblChasisTit = new Label { Dock = DockStyle.Top, Height = 30, AutoSize = false };
            chasis = new ChasisPcControl06AV { Dock = DockStyle.Fill };
            chasis.BahiaElegida += (s, i) => IrAlPaso(i);

            pnlChasis = new Panel { Dock = DockStyle.Left, Width = 330, Padding = new Padding(18, 14, 14, 14) };
            pnlChasis.Controls.Add(chasis);
            pnlChasis.Controls.Add(lblChasisTit);

            pasos = new PasosWizard06AV { Dock = DockStyle.Top, Height = 48 };
            pasos.PasoElegido += (s, i) => IrAlPaso(i);

            lblPasoTit = new Label { Dock = DockStyle.Top, Height = 30, AutoSize = false, Padding = new Padding(2, 4, 0, 0) };
            lblPasoAyuda = new Label { Dock = DockStyle.Top, Height = 22, AutoSize = false, Padding = new Padding(2, 0, 0, 0) };
            lblSinOpciones = new Label { Dock = DockStyle.Top, Height = 40, AutoSize = false, Padding = new Padding(4, 8, 0, 0), Visible = false };
            lblConsumo = new Label { Dock = DockStyle.Top, Height = 22, AutoSize = false, Padding = new Padding(2, 2, 0, 0), Visible = false };
            lblAviso = new Label { Dock = DockStyle.Top, Height = 40, AutoSize = false, Padding = new Padding(8, 4, 6, 4), Visible = false };

            lblMarca = new Label { AutoSize = true, Margin = new Padding(0, 9, 8, 0) };
            btnAmd = BotonMarca(ReglasCompatibilidad06AV.Amd);
            btnIntel = BotonMarca(ReglasCompatibilidad06AV.Intel);
            pnlMarcas = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 44, WrapContents = false, Padding = new Padding(0, 4, 0, 4), Visible = false
            };
            pnlMarcas.Controls.Add(lblMarca);
            pnlMarcas.Controls.Add(btnAmd);
            pnlMarcas.Controls.Add(btnIntel);

            lblModulos = new Label { AutoSize = true, Margin = new Padding(0, 9, 8, 0) };
            nudModulos = new NumericUpDown { Width = 64, Minimum = 1, Maximum = 1, Value = 1, Margin = new Padding(0, 5, 12, 0) };
            nudModulos.ValueChanged += (s, e) => CambiarCantidadRam();
            lblModulosInfo = new Label { AutoSize = true, Margin = new Padding(0, 9, 0, 0) };
            pnlCantidad = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 40, WrapContents = false, Padding = new Padding(0, 2, 0, 2), Visible = false
            };
            pnlCantidad.Controls.Add(lblModulos);
            pnlCantidad.Controls.Add(nudModulos);
            pnlCantidad.Controls.Add(lblModulosInfo);

            flpCatalogo = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 6, 6, 6)
            };

            var pnlCatalogo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 10, 14, 6) };
            pnlCatalogo.Controls.Add(flpCatalogo);
            pnlCatalogo.Controls.Add(lblSinOpciones);
            pnlCatalogo.Controls.Add(lblAviso);
            pnlCatalogo.Controls.Add(lblConsumo);
            pnlCatalogo.Controls.Add(pnlCantidad);
            pnlCatalogo.Controls.Add(pnlMarcas);
            pnlCatalogo.Controls.Add(lblPasoAyuda);
            pnlCatalogo.Controls.Add(lblPasoTit);
            pnlCatalogo.Controls.Add(pasos);

            btnCancelar = Boton(120);
            btnAtras = Boton(140);
            btnSaltear = Boton(150);
            btnSiguiente = Boton(170);
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            btnAtras.Click += (s, e) => Retroceder();
            btnSaltear.Click += (s, e) => Avanzar(true);
            btnSiguiente.Click += (s, e) => Avanzar(false);

            var barra = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 13, 16, 0)
            };
            barra.Controls.Add(btnSiguiente);
            barra.Controls.Add(btnSaltear);
            barra.Controls.Add(btnAtras);
            barra.Controls.Add(btnCancelar);

            Controls.Add(pnlCatalogo);
            Controls.Add(pnlChasis);
            Controls.Add(barra);

            CancelButton = btnCancelar;
            ArmarChasis();
        }

        private static Button Boton(int ancho) => new Button
        {
            Width = ancho,
            Height = 34,
            Margin = new Padding(8, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };

        private Button BotonMarca(string marca)
        {
            var b = new Button
            {
                Text = marca, Width = 92, Height = 32, Margin = new Padding(0, 2, 8, 0),
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = marca,
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold)
            };
            b.Click += (s, e) => ElegirMarca(marca);
            return b;
        }

        private void ArmarChasis()
        {
            var bahias = _pasos.Select(tipo => new BahiaPc06AV
            {
                Tipo = tipo,
                Nombre = NombreTipo(tipo),
                Icono = IconoDe(tipo),
                Opcional = EsSalteable(tipo),
                Puesto = _elegidos.TryGetValue(tipo, out Componente06AV c) ? c : null,
                Cantidad = Cantidad(tipo)
            }).ToList();

            chasis.DefinirBahias(bahias);
            pasos.DefinirPasos(_pasos.Select(NombreTipoCorto));
        }

        #endregion

        #region Navegación

        private void MostrarPaso()
        {
            var t = GestorIdioma06AV.Instancia;

            if (_pasos.Count == 0)
            {
                lblPasoTit.Text = t.Obtener("pcf_armar_sin_catalogo");
                lblPasoAyuda.Text = string.Empty;
                btnSaltear.Visible = btnAtras.Visible = false;
                btnSiguiente.Text = t.Obtener("cerrar");
                return;
            }

            TipoComponente06AV tipo = _pasos[_paso];
            bool salteable = EsSalteable(tipo);

            pasos.Actual = _paso;
            chasis.Actual = _paso;

            lblPasoTit.Text = t.Obtener("pcf_armar_elegi", NombreTipo(tipo));
            lblPasoAyuda.Text = salteable
                ? t.Obtener("pcf_armar_opcional")
                : t.Obtener("pcf_armar_obligatorio");

            CargarCatalogo(tipo);
            ActualizarPaneles(tipo);

            btnAtras.Visible = _paso > 0;
            btnSaltear.Visible = salteable;
            btnSiguiente.Text = _paso == _pasos.Count - 1
                ? t.Obtener("pcf_armar_finalizar")
                : t.Obtener("pcf_asis_siguiente") + "  →";
            btnAtras.Text = "←  " + t.Obtener("pcf_asis_atras");
            btnSaltear.Text = t.Obtener("pcf_armar_saltear");
            btnCancelar.Text = t.Obtener("cancelar");
            lblChasisTit.Text = t.Obtener("pcf_armar_tu_pc");
        }

        private void CargarCatalogo(TipoComponente06AV tipo)
        {
            var t = GestorIdioma06AV.Instancia;

            flpCatalogo.SuspendLayout();
            foreach (Control c in flpCatalogo.Controls.Cast<Control>().ToList()) c.Dispose();
            flpCatalogo.Controls.Clear();

            _elegidos.TryGetValue(tipo, out Componente06AV elegido);
            List<Componente06AV> previas = PiezasPrevias(tipo);

            var opciones = _todos
                .Where(c => c.Tipo == tipo)
                .Where(c => !ReglasCompatibilidad06AV.UsaPlataforma(tipo) || _marca == null ||
                            ReglasCompatibilidad06AV.PlataformaDe(c) == null ||
                            ReglasCompatibilidad06AV.PlataformaDe(c) == _marca)
                .Select(c => new
                {
                    Componente = c,
                    Resultado = ReglasCompatibilidad06AV.EvaluarCandidato(
                        c, previas, elegido != null && elegido.Codigo == c.Codigo ? Cantidad(tipo) : 1)
                })
                .OrderBy(x => x.Resultado.EsIncompatible)
                .ThenBy(x => x.Componente.PrecioUnitario)
                .ToList();

            decimal tope = opciones.Count > 0 ? opciones.Max(x => x.Componente.PrecioUnitario) : 0m;

            foreach (var opcion in opciones)
            {
                Componente06AV c = opcion.Componente;
                ResultadoCompatibilidad06AV res = opcion.Resultado;
                var ficha = new TarjetaComponente06AV
                {
                    Componente = c,
                    PrecioTope = tope,
                    Icono = IconoDe(tipo),
                    Seleccionada = elegido != null && elegido.Codigo == c.Codigo,
                    Width = AnchoFicha(),
                    TextoStock = t.Obtener("pcf_armar_stock"),
                    TextoSinStock = t.Obtener("pcf_armar_sin_stock"),
                    Compatibilidad = previas.Count > 0 || res.Problemas.Count > 0 ? res.Nivel : (NivelCompatibilidad06AV?)null,
                    MotivoCompatibilidad = res.Principal,
                    TextoCompatible = t.Obtener("pcf_armar_compatible"),
                    TextoAdvertencia = t.Obtener("pcf_armar_advertencia"),
                    TextoIncompatible = t.Obtener("pcf_armar_incompatible")
                };
                if (res.Problemas.Count > 0)
                    _tip.SetToolTip(ficha, string.Join("\n", res.Problemas.Select(pr => "• " + pr.Mensaje).Distinct()));
                Componente06AV cLocal = c;
                ficha.Elegida += (s, e) => Elegir(tipo, cLocal);
                flpCatalogo.Controls.Add(ficha);
            }

            lblSinOpciones.Text = t.Obtener("pcf_armar_sin_opciones");
            lblSinOpciones.Visible = opciones.Count == 0;
            flpCatalogo.ResumeLayout();
        }

        private int AnchoFicha() => Math.Max(280, flpCatalogo.ClientSize.Width - 24);

        private void Elegir(TipoComponente06AV tipo, Componente06AV componente)
        {
            bool mismo = _elegidos.TryGetValue(tipo, out Componente06AV anterior) && anterior != null &&
                         anterior.Codigo == componente.Codigo;
            _elegidos[tipo] = componente;
            _cantidades[tipo] = mismo ? Cantidad(tipo) : 1;
            if (tipo == TipoComponente06AV.Procesador)
                _marca = ReglasCompatibilidad06AV.PlataformaDe(componente) ?? _marca;

            _avisos.Clear();
            RevisarPosteriores(tipo);
            chasis.Poner(tipo, componente, Cantidad(tipo));

            foreach (Control c in flpCatalogo.Controls)
                if (c is TarjetaComponente06AV f)
                {
                    bool esta = f.Componente != null && f.Componente.Codigo == componente.Codigo;
                    if (f.Seleccionada != esta) f.Seleccionada = esta;
                }

            bool elegirModulos = tipo == TipoComponente06AV.MemoriaRAM &&
                                 ReglasCompatibilidad06AV.MaximoModulosRam(Elegido(TipoComponente06AV.PlacaMadre), componente) > 1;
            if (!elegirModulos && _paso < _pasos.Count - 1) _paso++;
            MostrarPaso();
        }

        private void RevisarPosteriores(TipoComponente06AV desde)
        {
            var t = GestorIdioma06AV.Instancia;
            int inicio = Orden(desde);
            foreach (PasoArmado06AV paso in ArmadorComputadora06AV.Pasos.Where(p => Orden(p.Tipo) > inicio).ToList())
            {
                Componente06AV pieza = Elegido(paso.Tipo);
                if (pieza == null) continue;

                if (paso.Tipo == TipoComponente06AV.MemoriaRAM)
                {
                    int maximo = ReglasCompatibilidad06AV.MaximoModulosRam(Elegido(TipoComponente06AV.PlacaMadre), pieza);
                    if (Cantidad(paso.Tipo) > maximo)
                    {
                        _cantidades[paso.Tipo] = maximo;
                        chasis.Poner(paso.Tipo, pieza, maximo);
                        _avisos.Add(t.Obtener("pcf_armar_ram_ajustada", maximo));
                    }
                }

                ResultadoCompatibilidad06AV res = ReglasCompatibilidad06AV.EvaluarCandidato(
                    pieza, PiezasPrevias(paso.Tipo), Cantidad(paso.Tipo));
                if (!res.EsIncompatible) continue;

                _elegidos.Remove(paso.Tipo);
                _cantidades.Remove(paso.Tipo);
                chasis.Poner(paso.Tipo, null);
                _avisos.Add(t.Obtener("pcf_armar_quitado", NombreTipo(paso.Tipo),
                                      res.Texto(NivelCompatibilidad06AV.Incompatible, " ")));
            }
        }

        private void ElegirMarca(string marca)
        {
            _marca = _marca == marca ? null : marca;
            if (_pasos.Count == 0) return;
            TipoComponente06AV tipo = _pasos[_paso];
            CargarCatalogo(tipo);
            ActualizarPaneles(tipo);
        }

        private void CambiarCantidadRam()
        {
            if (_cargandoCantidad) return;
            Componente06AV ram = Elegido(TipoComponente06AV.MemoriaRAM);
            if (ram == null) return;
            _cantidades[TipoComponente06AV.MemoriaRAM] = (int)nudModulos.Value;
            chasis.Poner(TipoComponente06AV.MemoriaRAM, ram, (int)nudModulos.Value);
            _avisos.Clear();
            RevisarPosteriores(TipoComponente06AV.MemoriaRAM);
            ActualizarPaneles(TipoComponente06AV.MemoriaRAM);
        }

        private void ActualizarPaneles(TipoComponente06AV tipo)
        {
            var t = GestorIdioma06AV.Instancia;

            pnlMarcas.Visible = ReglasCompatibilidad06AV.UsaPlataforma(tipo) &&
                                _todos.Any(c => c.Tipo == tipo && ReglasCompatibilidad06AV.PlataformaDe(c) != null);
            lblMarca.Text = t.Obtener("pcf_armar_marca");
            foreach (Button b in new[] { btnAmd, btnIntel })
            {
                if ((string)b.Tag == _marca) Tema.AplicarBotonPrimario(b);
                else Tema.AplicarBotonSecundario(b);
            }

            Componente06AV ram = Elegido(TipoComponente06AV.MemoriaRAM);
            Componente06AV placa = Elegido(TipoComponente06AV.PlacaMadre);
            int maximo = ReglasCompatibilidad06AV.MaximoModulosRam(placa, ram);
            pnlCantidad.Visible = tipo == TipoComponente06AV.MemoriaRAM && ram != null && maximo > 1;
            if (pnlCantidad.Visible)
            {
                _cargandoCantidad = true;
                nudModulos.Maximum = maximo;
                nudModulos.Value = Math.Min(maximo, Math.Max(1, Cantidad(TipoComponente06AV.MemoriaRAM)));
                _cargandoCantidad = false;
                int total = (ram.Especificacion?.CapacidadGb ?? 0) * (int)nudModulos.Value;
                lblModulos.Text = t.Obtener("pcf_armar_modulos");
                lblModulosInfo.Text = placa?.Especificacion?.SlotsMemoria != null && placa.Especificacion.MemoriaMaximaGb != null
                    ? t.Obtener("pcf_armar_modulos_info", placa.Especificacion.SlotsMemoria, placa.Especificacion.MemoriaMaximaGb, total)
                    : t.Obtener("pcf_armar_modulos_total", maximo, total);
            }

            int consumo = ReglasCompatibilidad06AV.ConsumoEstimado(Seleccionados);
            lblConsumo.Visible = consumo > 0;
            lblConsumo.Text = t.Obtener("pcf_armar_consumo", consumo, ReglasCompatibilidad06AV.FuenteRecomendada(consumo));

            lblAviso.Visible = _avisos.Count > 0;
            lblAviso.Text = string.Join("  ", _avisos);
            lblAviso.Height = _avisos.Count > 1 ? 56 : 40;
        }

        private Componente06AV Elegido(TipoComponente06AV tipo) =>
            _elegidos.TryGetValue(tipo, out Componente06AV c) ? c : null;

        private int Cantidad(TipoComponente06AV tipo) =>
            _cantidades.TryGetValue(tipo, out int n) && n > 0 ? n : 1;

        private static int Orden(TipoComponente06AV tipo)
        {
            for (int i = 0; i < ArmadorComputadora06AV.Pasos.Count; i++)
                if (ArmadorComputadora06AV.Pasos[i].Tipo == tipo) return i;
            return int.MaxValue;
        }

        private List<Componente06AV> PiezasPrevias(TipoComponente06AV tipo)
        {
            int limite = Orden(tipo);
            return Seleccionados.Where(c => Orden(c.Tipo) < limite).ToList();
        }

        private void IrAlPaso(int indice)
        {
            if (indice < 0 || indice >= _pasos.Count) return;
            _paso = indice;
            MostrarPaso();
        }

        private void Avanzar(bool saltear)
        {
            var t = GestorIdioma06AV.Instancia;
            if (_pasos.Count == 0) { DialogResult = DialogResult.Cancel; Close(); return; }

            TipoComponente06AV tipo = _pasos[_paso];

            if (saltear)
            {
                Componente06AV cpu = Elegido(TipoComponente06AV.Procesador);
                if (tipo == TipoComponente06AV.PlacaDeVideo && cpu?.Especificacion?.GraficosIntegrados == false)
                {
                    ConfirmacionForm.MostrarInfo(
                        t.Obtener("cmp_sin_video", (cpu.Marca + " " + cpu.Modelo).Trim()),
                        t.Obtener("pcf_armar_titulo"), ConfirmacionForm.TipoConfirmacion.Advertencia, this);
                    return;
                }
                _elegidos.Remove(tipo);
                _cantidades.Remove(tipo);
                chasis.Poner(tipo, null);
            }
            else if (!EsSalteable(tipo) &&
                     !(_elegidos.TryGetValue(tipo, out Componente06AV ya) && ya != null))
            {
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("pcf_armar_falta", NombreTipo(tipo)),
                    t.Obtener("pcf_armar_titulo"), ConfirmacionForm.TipoConfirmacion.Advertencia, this);
                return;
            }

            if (_paso == _pasos.Count - 1)
            {
                List<TipoComponente06AV> faltantes = _pasos
                    .Where(x => !EsSalteable(x) && !(_elegidos.ContainsKey(x) && _elegidos[x] != null))
                    .ToList();

                if (faltantes.Count > 0)
                {
                    ConfirmacionForm.MostrarInfo(
                        t.Obtener("pcf_armar_faltan",
                                  string.Join(", ", faltantes.Select(NombreTipoCorto))),
                        t.Obtener("pcf_armar_titulo"), ConfirmacionForm.TipoConfirmacion.Advertencia, this);
                    IrAlPaso(_pasos.IndexOf(faltantes[0]));
                    return;
                }

                ResultadoCompatibilidad06AV equipo = ReglasCompatibilidad06AV.EvaluarEquipo(Seleccionados);
                if (equipo.EsIncompatible)
                {
                    ConfirmacionForm.MostrarInfo(
                        t.Obtener("pcf_armar_incompatibles",
                                  "• " + equipo.Texto(NivelCompatibilidad06AV.Incompatible, "\n• ")),
                        t.Obtener("pcf_armar_titulo"), ConfirmacionForm.TipoConfirmacion.Advertencia, this);
                    return;
                }
                if (equipo.Nivel == NivelCompatibilidad06AV.Advertencia &&
                    !ConfirmacionForm.Mostrar(
                        t.Obtener("pcf_armar_advertencias",
                                  "• " + equipo.Texto(NivelCompatibilidad06AV.Advertencia, "\n• ")),
                        t.Obtener("pcf_armar_titulo"), ConfirmacionForm.TipoConfirmacion.Pregunta,
                        t.Obtener("pcf_armar_continuar"), t.Obtener("pcf_armar_revisar"), this))
                    return;

                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            _paso++;
            MostrarPaso();
        }

        private void Retroceder()
        {
            if (_paso > 0) { _paso--; MostrarPaso(); }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (flpCatalogo == null) return;
            int ancho = AnchoFicha();
            foreach (Control c in flpCatalogo.Controls) c.Width = ancho;
        }

        #endregion

        #region Tema

        private void AplicarTema()
        {
            var t = GestorIdioma06AV.Instancia;

            Tema.AplicarFormulario(this);
            BackColor = Tema.FondoApp;

            pnlChasis.BackColor = Tema.FondoPanel;
            chasis.BackColor = Tema.FondoPanel;
            lblChasisTit.Font = Tema.FuenteSubtit;
            lblChasisTit.ForeColor = Tema.TextoFuerte;
            lblChasisTit.BackColor = Tema.FondoPanel;

            chasis.TextoTotal = t.Obtener("pcf_armar_total");
            chasis.TextoOpcional = t.Obtener("pcf_armar_opcional_corto");
            chasis.TextoVacio = t.Obtener("pcf_armar_vacio");
            chasis.TextoPiezas = t.Obtener("pcf_armar_piezas");

            foreach (Control c in Controls)
                if (c is Panel || c is FlowLayoutPanel) c.BackColor = Tema.FondoApp;
            pnlChasis.BackColor = Tema.FondoPanel;
            flpCatalogo.BackColor = Tema.FondoApp;

            lblPasoTit.Font = Tema.FuenteSubtit;
            lblPasoTit.ForeColor = Tema.TextoFuerte;
            lblPasoTit.BackColor = Tema.FondoApp;
            lblPasoAyuda.Font = Tema.FuenteRegular;
            lblPasoAyuda.ForeColor = Tema.TextoSuave;
            lblPasoAyuda.BackColor = Tema.FondoApp;
            lblSinOpciones.Font = Tema.FuenteRegular;
            lblSinOpciones.ForeColor = Tema.TextoSuave;
            lblSinOpciones.BackColor = Tema.FondoApp;
            lblConsumo.Font = Tema.FuenteMini;
            lblConsumo.ForeColor = Tema.TextoSuave;
            lblConsumo.BackColor = Tema.FondoApp;
            lblAviso.Font = Tema.FuenteRegular;
            lblAviso.ForeColor = Tema.EsOscuro ? Tema.Advertencia : Tema.Naranja600;
            lblAviso.BackColor = Tema.AdvertenciaSuave;
            foreach (Label l in new[] { lblMarca, lblModulos, lblModulosInfo })
            {
                l.Font = l == lblModulosInfo ? Tema.FuenteMini : Tema.FuenteBold;
                l.ForeColor = l == lblModulosInfo ? Tema.TextoSuave : Tema.TextoFuerte;
            }
            pnlMarcas.BackColor = pnlCantidad.BackColor = Tema.FondoApp;
            Tema.AplicarEntrada(nudModulos);

            Tema.AplicarBotonPrimario(btnSiguiente);
            Tema.AplicarBotonSecundario(btnAtras);
            Tema.AplicarBotonSecundario(btnSaltear);
            Tema.AplicarBotonSecundario(btnCancelar);
            if (_pasos.Count > 0) ActualizarPaneles(_pasos[_paso]);

            Invalidate(true);
        }

        #endregion

        #region Nombres e íconos de los tipos

        private static IconoPcf06AV IconoDe(TipoComponente06AV t)
        {
            switch (t)
            {
                case TipoComponente06AV.Procesador: return IconoPcf06AV.Chip;
                case TipoComponente06AV.PlacaMadre: return IconoPcf06AV.Chip;
                case TipoComponente06AV.MemoriaRAM: return IconoPcf06AV.Chip;
                case TipoComponente06AV.Disco: return IconoPcf06AV.Caja;
                case TipoComponente06AV.PlacaDeVideo: return IconoPcf06AV.Chip;
                case TipoComponente06AV.Fuente: return IconoPcf06AV.Caja;
                case TipoComponente06AV.Gabinete: return IconoPcf06AV.Caja;
                case TipoComponente06AV.Refrigeracion: return IconoPcf06AV.Destornillador;
                default: return IconoPcf06AV.Caja;
            }
        }

        private static string NombreTipo(TipoComponente06AV t)
        {
            var i = GestorIdioma06AV.Instancia;
            switch (t)
            {
                case TipoComponente06AV.Procesador: return i.Obtener("pcf_tipo_procesador");
                case TipoComponente06AV.PlacaMadre: return i.Obtener("pcf_tipo_placa_madre");
                case TipoComponente06AV.MemoriaRAM: return i.Obtener("pcf_tipo_ram");
                case TipoComponente06AV.Disco: return i.Obtener("pcf_tipo_disco");
                case TipoComponente06AV.PlacaDeVideo: return i.Obtener("pcf_tipo_video");
                case TipoComponente06AV.Fuente: return i.Obtener("pcf_tipo_fuente");
                case TipoComponente06AV.Gabinete: return i.Obtener("pcf_tipo_gabinete");
                case TipoComponente06AV.Refrigeracion: return i.Obtener("pcf_tipo_refrigeracion");
                default: return i.Obtener("pcf_tipo_otro");
            }
        }

        private static string NombreTipoCorto(TipoComponente06AV t)
        {
            switch (t)
            {
                case TipoComponente06AV.Procesador: return "CPU";
                case TipoComponente06AV.PlacaMadre: return "Mother";
                case TipoComponente06AV.MemoriaRAM: return "RAM";
                case TipoComponente06AV.Disco: return "Disco";
                case TipoComponente06AV.PlacaDeVideo: return "GPU";
                case TipoComponente06AV.Fuente: return "Fuente";
                case TipoComponente06AV.Gabinete: return "Gabinete";
                case TipoComponente06AV.Refrigeracion: return "Cooler";
                default: return "Otro";
            }
        }

        #endregion
    }
}

namespace PCFORGE_ValdezThiago_96VA.Controles
{
    [System.ComponentModel.DesignerCategory("Code")]
    public class ResumenPcControl06AV : FlowLayoutPanel
    {
        private static readonly TipoComponente06AV[] Orden =
        {
            TipoComponente06AV.Procesador, TipoComponente06AV.PlacaMadre,
            TipoComponente06AV.MemoriaRAM, TipoComponente06AV.Disco,
            TipoComponente06AV.PlacaDeVideo, TipoComponente06AV.Fuente,
            TipoComponente06AV.Gabinete, TipoComponente06AV.Refrigeracion,
            TipoComponente06AV.Otro
        };

        public ResumenPcControl06AV()
        {
            FlowDirection = FlowDirection.TopDown;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            WrapContents = false;
            Margin = new Padding(0);
            Padding = new Padding(0, 2, 0, 2);
            Mostrar(null);
        }

        public void Mostrar(List<Componente06AV> componentes)
        {
            SuspendLayout();
            foreach (Control c in Controls.Cast<Control>().ToList()) c.Dispose();
            Controls.Clear();

            if (componentes == null || componentes.Count == 0)
            {
                Controls.Add(new Label
                {
                    AutoSize = true,
                    ForeColor = Tema.TextoSuave,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                    Text = GestorIdioma06AV.Instancia.Obtener("pcf_sin_componentes"),
                    Margin = new Padding(0, 2, 0, 2)
                });
                ResumeLayout();
                return;
            }

            decimal total = 0;
            foreach (var grupo in componentes
                        .GroupBy(c => c.Tipo)
                        .OrderBy(g => Array.IndexOf(Orden, g.Key)))
            {
                Controls.Add(new Label
                {
                    AutoSize = true,
                    Font = Tema.FuenteMini,
                    ForeColor = Tema.Primario,
                    Text = NombreTipo(grupo.Key).ToUpperInvariant(),
                    Margin = new Padding(0, 8, 0, 1)
                });

                foreach (var mismo in grupo.GroupBy(c => c.Codigo))
                {
                    Componente06AV c = mismo.First();
                    int cantidad = mismo.Count();
                    total += c.PrecioUnitario * cantidad;
                    Controls.Add(new Label
                    {
                        AutoSize = true,
                        ForeColor = Tema.Texto,
                        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                        Text = "   " + (cantidad > 1 ? cantidad + " × " : "") + c.Descripcion + "   ·   " +
                               (c.Marca + " " + c.Modelo).Trim() + "   ·   " + (c.PrecioUnitario * cantidad).ToString("C0"),
                        Margin = new Padding(0, 0, 0, 1)
                    });
                }
            }

            Controls.Add(new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                ForeColor = Tema.TextoFuerte,
                Text = GestorIdioma06AV.Instancia.Obtener("pcf_armar_total") + ":  " +
                       total.ToString("C0") + "     ·     " +
                       GestorIdioma06AV.Instancia.Obtener("pcf_armar_n_componentes", componentes.Count),
                Margin = new Padding(0, 10, 0, 2)
            });

            ResumeLayout();
        }

        private static string NombreTipo(TipoComponente06AV t)
        {
            var i = GestorIdioma06AV.Instancia;
            switch (t)
            {
                case TipoComponente06AV.Procesador: return i.Obtener("pcf_tipo_procesador");
                case TipoComponente06AV.PlacaMadre: return i.Obtener("pcf_tipo_placa_madre");
                case TipoComponente06AV.MemoriaRAM: return i.Obtener("pcf_tipo_ram");
                case TipoComponente06AV.Disco: return i.Obtener("pcf_tipo_disco");
                case TipoComponente06AV.PlacaDeVideo: return i.Obtener("pcf_tipo_video");
                case TipoComponente06AV.Fuente: return i.Obtener("pcf_tipo_fuente");
                case TipoComponente06AV.Gabinete: return i.Obtener("pcf_tipo_gabinete");
                case TipoComponente06AV.Refrigeracion: return i.Obtener("pcf_tipo_refrigeracion");
                default: return i.Obtener("pcf_tipo_otro");
            }
        }
    }
}
