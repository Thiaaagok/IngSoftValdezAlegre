using BE;
using IngSoftValdezAlegre.Common;
using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace IngSoftValdezAlegre
{
    /// <summary>
    /// Asistente "Armá tu PC" (estilo CompraGamer): recorre los tipos de componente
    /// de a uno, mostrando los disponibles de ese tipo para elegir uno (o saltear).
    /// A la izquierda muestra el resumen y el total. Devuelve la lista elegida en
    /// <see cref="Seleccionados"/>. Un componente por tipo.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class FRMArmarPc06AV : Form
    {
        private static readonly TipoComponente06AV[] OrdenTipos =
        {
            TipoComponente06AV.Procesador, TipoComponente06AV.PlacaMadre,
            TipoComponente06AV.MemoriaRAM, TipoComponente06AV.Disco,
            TipoComponente06AV.PlacaDeVideo, TipoComponente06AV.Fuente,
            TipoComponente06AV.Gabinete, TipoComponente06AV.Refrigeracion,
            TipoComponente06AV.Otro
        };

        // Tipos que se pueden saltear (opcionales). El resto es obligatorio:
        // Procesador, Placa madre, Memoria RAM, Disco, Fuente y Gabinete.
        private static readonly TipoComponente06AV[] Salteables =
        {
            TipoComponente06AV.PlacaDeVideo, TipoComponente06AV.Refrigeracion, TipoComponente06AV.Otro
        };
        private static bool EsSalteable(TipoComponente06AV t) => System.Array.IndexOf(Salteables, t) >= 0;

        private readonly List<Componente06AV> _todos;
        private readonly List<TipoComponente06AV> _pasos;
        private int _paso;
        private readonly Dictionary<TipoComponente06AV, Componente06AV> _elegidos =
            new Dictionary<TipoComponente06AV, Componente06AV>();

        /// <summary>Componentes elegidos (uno por tipo). Válido tras cerrar con OK.</summary>
        public List<Componente06AV> Seleccionados => _elegidos.Values.Where(c => c != null).ToList();

        private Label lblPaso, lblResumenTit, lblTotal;
        private ListBox lstComp, lstResumen;
        private Button btnAtras, btnSaltear, btnSiguiente;

        public FRMArmarPc06AV(IEnumerable<Componente06AV> componentes, IEnumerable<Componente06AV> preseleccion = null)
        {
            _todos = (componentes ?? Enumerable.Empty<Componente06AV>()).ToList();
            _pasos = OrdenTipos.Where(t => _todos.Any(c => c.Tipo == t)).ToList();
            if (preseleccion != null)
                foreach (var c in preseleccion)
                    if (c != null) _elegidos[c.Tipo] = c;

            ConstruirUI();
            Tema.AplicarFormulario(this);
            Tema.AplicarBotonPrimario(btnSiguiente);
            Tema.AplicarBotonSecundario(btnAtras);
            Tema.AplicarBotonSecundario(btnSaltear);
            MostrarPaso();
        }

        private void ConstruirUI()
        {
            Text = "Armá tu PC";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ClientSize = new Size(840, 560);
            MinimumSize = new Size(700, 480);

            // --- Resumen (izquierda) ---
            lblResumenTit = new Label { Dock = DockStyle.Top, Height = 30, Text = "Tu PC",
                Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold), Padding = new Padding(2, 4, 0, 0) };
            lstResumen = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false,
                SelectionMode = SelectionMode.None, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f) };
            lblTotal = new Label { Dock = DockStyle.Bottom, Height = 42, TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold) };
            var pnlResumen = new Panel { Dock = DockStyle.Left, Width = 310, Padding = new Padding(14, 12, 8, 12) };
            pnlResumen.Controls.Add(lstResumen);
            pnlResumen.Controls.Add(lblTotal);
            pnlResumen.Controls.Add(lblResumenTit);

            // --- Selección (derecha) ---
            lblPaso = new Label { Dock = DockStyle.Top, Height = 52, Padding = new Padding(4, 12, 0, 0),
                Font = new Font("Segoe UI Semibold", 12.5f, FontStyle.Bold) };
            lstComp = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, Font = new Font("Segoe UI", 10.5f) };
            lstComp.DoubleClick += (s, e) => { if (lstComp.SelectedItem != null) Avanzar(false); };
            var pnlMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 8, 14, 8) };
            pnlMain.Controls.Add(lstComp);
            pnlMain.Controls.Add(lblPaso);

            // --- Barra de navegación ---
            btnAtras = Boton("← Volver atrás", 150);
            btnSaltear = Boton("Saltear paso", 130);
            btnSiguiente = Boton("Siguiente →", 150);
            btnAtras.Click += (s, e) => Retroceder();
            btnSaltear.Click += (s, e) => Avanzar(true);
            btnSiguiente.Click += (s, e) => Avanzar(false);
            var barra = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 58,
                FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 12, 14, 0) };
            barra.Controls.Add(btnSiguiente);
            barra.Controls.Add(btnSaltear);
            barra.Controls.Add(btnAtras);

            Controls.Add(pnlMain);
            Controls.Add(pnlResumen);
            Controls.Add(barra);
        }

        private static Button Boton(string t, int w) =>
            new Button { Text = t, Width = w, Height = 34, Margin = new Padding(6, 0, 0, 0) };

        private void MostrarPaso()
        {
            if (_pasos.Count == 0)
            {
                lblPaso.Text = "No hay componentes cargados. Cargá componentes primero.";
                lstComp.Enabled = btnSaltear.Enabled = btnAtras.Enabled = false;
                btnSiguiente.Text = "Cerrar";
                return;
            }

            var tipo = _pasos[_paso];
            bool salteable = EsSalteable(tipo);
            btnSaltear.Visible = salteable;
            lblPaso.Text = $"Paso {_paso + 1} de {_pasos.Count}:  elegí {NombreTipo(tipo)}" +
                           (salteable ? "   (opcional)" : "");

            lstComp.Items.Clear();
            foreach (var c in _todos.Where(x => x.Tipo == tipo).OrderBy(x => x.PrecioUnitario))
                lstComp.Items.Add(new ItemComp(c));

            if (_elegidos.TryGetValue(tipo, out var elegido) && elegido != null)
                for (int i = 0; i < lstComp.Items.Count; i++)
                    if (((ItemComp)lstComp.Items[i]).C.Codigo == elegido.Codigo) { lstComp.SelectedIndex = i; break; }

            btnAtras.Enabled = _paso > 0;
            btnSiguiente.Text = _paso == _pasos.Count - 1 ? "Finalizar ✔" : "Siguiente →";
            ActualizarResumen();
        }

        private void Avanzar(bool saltear)
        {
            if (_pasos.Count == 0) { DialogResult = DialogResult.Cancel; Close(); return; }

            var tipo = _pasos[_paso];
            if (saltear)
            {
                _elegidos.Remove(tipo);
            }
            else
            {
                if (lstComp.SelectedItem is ItemComp it) _elegidos[tipo] = it.C;
                // Los obligatorios no se pueden dejar sin elegir.
                if (!EsSalteable(tipo) && !(_elegidos.TryGetValue(tipo, out var ya) && ya != null))
                {
                    ConfirmacionForm.MostrarInfo($"Tenés que elegir {NombreTipo(tipo)} para continuar.",
                        "Armá tu PC", ConfirmacionForm.TipoConfirmacion.Advertencia, this);
                    return;
                }
            }

            if (_paso == _pasos.Count - 1)
            {
                DialogResult = DialogResult.OK;
                Close();
                return;
            }
            _paso++;
            MostrarPaso();
        }

        private void Retroceder()
        {
            if (lstComp.SelectedItem is ItemComp it) _elegidos[_pasos[_paso]] = it.C;
            if (_paso > 0) { _paso--; MostrarPaso(); }
        }

        private void ActualizarResumen()
        {
            lstResumen.Items.Clear();
            decimal total = 0;
            foreach (var tipo in _pasos)
                if (_elegidos.TryGetValue(tipo, out var c) && c != null)
                {
                    lstResumen.Items.Add($"{NombreTipoCorto(tipo)}: {c.Descripcion} — ${c.PrecioUnitario:0.00}");
                    total += c.PrecioUnitario;
                }
            lblTotal.Text = $"Total: ${total:0.00}";
        }

        private static string NombreTipo(TipoComponente06AV t)
        {
            switch (t)
            {
                case TipoComponente06AV.Procesador: return "el procesador";
                case TipoComponente06AV.PlacaMadre: return "la placa madre";
                case TipoComponente06AV.MemoriaRAM: return "la memoria RAM";
                case TipoComponente06AV.Disco: return "el disco";
                case TipoComponente06AV.PlacaDeVideo: return "la placa de video";
                case TipoComponente06AV.Fuente: return "la fuente";
                case TipoComponente06AV.Gabinete: return "el gabinete";
                case TipoComponente06AV.Refrigeracion: return "la refrigeración";
                default: return "otros componentes";
            }
        }

        private static string NombreTipoCorto(TipoComponente06AV t)
        {
            switch (t)
            {
                case TipoComponente06AV.Procesador: return "CPU";
                case TipoComponente06AV.PlacaMadre: return "Motherboard";
                case TipoComponente06AV.MemoriaRAM: return "RAM";
                case TipoComponente06AV.Disco: return "Disco";
                case TipoComponente06AV.PlacaDeVideo: return "GPU";
                case TipoComponente06AV.Fuente: return "Fuente";
                case TipoComponente06AV.Gabinete: return "Gabinete";
                case TipoComponente06AV.Refrigeracion: return "Cooler";
                default: return "Otro";
            }
        }

        private class ItemComp
        {
            public readonly Componente06AV C;
            public ItemComp(Componente06AV c) { C = c; }
            public override string ToString() =>
                $"{C.Descripcion}   ·   {C.Marca} {C.Modelo}   ·   ${C.PrecioUnitario:0.00}   (stock {C.StockDisponible})";
        }
    }
}

namespace IngSoftValdezAlegre.Controles
{
    /// <summary>
    /// Muestra el resumen de una PC armada de forma prolija: por cada tipo de
    /// componente, un encabezado (Procesador, Placa madre, …) y debajo el componente
    /// elegido con su precio. Cierra con el total. (Definido acá para no depender de
    /// una entrada extra en el .csproj.)
    /// </summary>
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

        /// <summary>Reconstruye el resumen a partir de la lista de componentes.</summary>
        public void Mostrar(List<Componente06AV> componentes)
        {
            SuspendLayout();
            Controls.Clear();

            if (componentes == null || componentes.Count == 0)
            {
                Controls.Add(new Label
                {
                    AutoSize = true,
                    ForeColor = Tema.Acero500,
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
                        .OrderBy(g => System.Array.IndexOf(Orden, g.Key)))
            {
                Controls.Add(new Label
                {
                    AutoSize = true,
                    Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                    ForeColor = Tema.Primario,
                    Text = NombreTipo(grupo.Key),
                    Margin = new Padding(0, 8, 0, 1)
                });

                foreach (var c in grupo)
                {
                    total += c.PrecioUnitario;
                    Controls.Add(new Label
                    {
                        AutoSize = true,
                        ForeColor = Tema.Acero700,
                        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                        Text = $"     {c.Descripcion}   ·   {c.Marca} {c.Modelo}   ·   ${c.PrecioUnitario:0.00}",
                        Margin = new Padding(0, 0, 0, 1)
                    });
                }
            }

            Controls.Add(new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                Text = $"Total:  ${total:0.00}     ·     {componentes.Count} componente{(componentes.Count == 1 ? "" : "s")}",
                Margin = new Padding(0, 10, 0, 2)
            });

            ResumeLayout();
        }

        private static string NombreTipo(TipoComponente06AV t)
        {
            switch (t)
            {
                case TipoComponente06AV.Procesador: return "Procesador";
                case TipoComponente06AV.PlacaMadre: return "Placa madre";
                case TipoComponente06AV.MemoriaRAM: return "Memoria RAM";
                case TipoComponente06AV.Disco: return "Disco";
                case TipoComponente06AV.PlacaDeVideo: return "Placa de video";
                case TipoComponente06AV.Fuente: return "Fuente";
                case TipoComponente06AV.Gabinete: return "Gabinete";
                case TipoComponente06AV.Refrigeracion: return "Refrigeración";
                default: return "Otros";
            }
        }
    }
}
