using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.UI
{
    /// <summary>
    /// TABLERO DE ESTACIONES — vista principal de Órdenes de Producción.
    ///
    /// Por qué reemplaza a la grilla: una grilla ordena filas, pero el proceso real es
    /// un flujo con trabajo acumulado en cada etapa. Con columnas por estación el
    /// encargado ve en un segundo lo que la grilla esconde:
    ///   · CUÁNTAS órdenes están trancadas en cada etapa (cuello de botella),
    ///   · si la línea está cargada o vacía (barra de carga en la cabecera),
    ///   · y el avance como desplazamiento físico de izquierda a derecha.
    /// La grilla clásica sigue disponible en la misma pantalla para buscar y ordenar:
    /// el tablero es para OPERAR, la grilla para CONSULTAR.
    /// </summary>
    internal class TableroEstacionesControl06AV : UserControl
    {
        /// <summary>Ancho mínimo de una columna para que una tarjeta siga siendo legible.</summary>
        private const int AnchoMinimoColumna = 188;

        private readonly Panel _viewport;
        private readonly TableLayoutPanel _grilla;
        private readonly List<ColumnaEstacion06AV> _columnas = new List<ColumnaEstacion06AV>();

        public TableroEstacionesControl06AV()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            _grilla = new TableLayoutPanel
            {
                RowCount = 1,
                ColumnCount = 0,
                Padding = new Padding(6, 4, 6, 6),
                Location = new Point(0, 0)
            };
            _grilla.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Viewport con scroll: si la ventana no da para 5 columnas legibles, se
            // desplaza en horizontal en vez de aplastar las tarjetas.
            _viewport = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            _viewport.Controls.Add(_grilla);
            _viewport.Resize += (s, e) => AcomodarGrilla();

            Controls.Add(_viewport);
        }

        /// <summary>Reparte el ancho entre las columnas respetando el mínimo legible.</summary>
        private void AcomodarGrilla()
        {
            if (_columnas.Count == 0)
            {
                _grilla.Size = _viewport.ClientSize;
                return;
            }

            int disponible = _viewport.ClientSize.Width;
            int anchoColumna = Math.Max(AnchoMinimoColumna, disponible / _columnas.Count);
            int total = anchoColumna * _columnas.Count;

            bool hayScroll = total > disponible;
            _grilla.Size = new Size(total,
                _viewport.ClientSize.Height - (hayScroll ? SystemInformation.HorizontalScrollBarHeight : 0));

            for (int i = 0; i < _grilla.ColumnStyles.Count; i++)
            {
                _grilla.ColumnStyles[i].SizeType = SizeType.Absolute;
                _grilla.ColumnStyles[i].Width = anchoColumna;
            }
        }

        /// <summary>Se dispara cuando el usuario selecciona una tarjeta.</summary>
        public event EventHandler<TarjetaOrden06AV> TarjetaElegida;

        /// <summary>Se dispara al pedir la acción principal de una tarjeta.</summary>
        public event EventHandler<TarjetaOrden06AV> AccionPedida;

        /// <summary>Crea las columnas del tablero (una por estación del proceso).</summary>
        public void DefinirColumnas(IList<EstacionRiel06AV> estaciones)
        {
            _grilla.SuspendLayout();
            _grilla.Controls.Clear();
            _columnas.Clear();
            _grilla.ColumnStyles.Clear();
            _grilla.ColumnCount = estaciones == null ? 0 : estaciones.Count;

            for (int i = 0; estaciones != null && i < estaciones.Count; i++)
            {
                _grilla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / estaciones.Count));
                var col = new ColumnaEstacion06AV(estaciones[i], i) { Dock = DockStyle.Fill };
                _columnas.Add(col);
                _grilla.Controls.Add(col, i, 0);
            }

            _grilla.ResumeLayout();
            AcomodarGrilla();
        }

        /// <summary>Saca todas las tarjetas sin destruir las columnas.</summary>
        public void Limpiar()
        {
            foreach (ColumnaEstacion06AV c in _columnas) c.Limpiar();
        }

        /// <summary>Agrega una tarjeta a la columna indicada y engancha sus eventos.</summary>
        public void Agregar(int columna, TarjetaOrden06AV tarjeta)
        {
            if (columna < 0 || columna >= _columnas.Count || tarjeta == null) return;
            tarjeta.Elegida += (s, e) => SeleccionarInterno((TarjetaOrden06AV)s, true);
            tarjeta.AccionElegida += (s, e) => AccionPedida?.Invoke(this, (TarjetaOrden06AV)s);
            _columnas[columna].Agregar(tarjeta);
        }

        /// <summary>Recalcula contadores y barras de carga. Llamar al terminar de agregar.</summary>
        public void Recalcular()
        {
            int max = 0;
            foreach (ColumnaEstacion06AV c in _columnas) max = Math.Max(max, c.Cantidad);
            foreach (ColumnaEstacion06AV c in _columnas) c.ActualizarCabecera(max);
        }

        /// <summary>Marca como seleccionada la tarjeta cuya Etiqueta coincide.</summary>
        public void SeleccionarPorEtiqueta(Func<object, bool> coincide)
        {
            if (coincide == null) return;
            foreach (ColumnaEstacion06AV c in _columnas)
                foreach (TarjetaOrden06AV t in c.Tarjetas())
                    if (coincide(t.Etiqueta)) { SeleccionarInterno(t, true); return; }
        }

        /// <summary>Tarjeta actualmente seleccionada (o null).</summary>
        public TarjetaOrden06AV Seleccionada { get; private set; }

        private void SeleccionarInterno(TarjetaOrden06AV tarjeta, bool avisar)
        {
            if (Seleccionada == tarjeta) { if (avisar) TarjetaElegida?.Invoke(this, tarjeta); return; }

            foreach (ColumnaEstacion06AV c in _columnas)
                foreach (TarjetaOrden06AV t in c.Tarjetas())
                    if (t.Seleccionada) { t.Seleccionada = false; t.Invalidate(); }

            Seleccionada = tarjeta;
            if (tarjeta != null) { tarjeta.Seleccionada = true; tarjeta.Invalidate(); }
            if (avisar) TarjetaElegida?.Invoke(this, tarjeta);
        }

        public void AplicarTema()
        {
            BackColor = Tema.FondoApp;
            _viewport.BackColor = Tema.FondoApp;
            _grilla.BackColor = Tema.FondoApp;
            foreach (ColumnaEstacion06AV c in _columnas) c.AplicarTema();
        }

        /// <summary>Cambia los títulos de las columnas sin recrearlas (cambio de idioma).</summary>
        public void RenombrarColumnas(IList<EstacionRiel06AV> estaciones)
        {
            if (estaciones == null) return;
            for (int i = 0; i < _columnas.Count && i < estaciones.Count; i++)
                _columnas[i].Renombrar(estaciones[i].Titulo);
        }
    }

    /// <summary>Una columna del tablero: cabecera pintada + pila de tarjetas con scroll.</summary>
    internal class ColumnaEstacion06AV : Panel
    {
        private readonly CabeceraColumna06AV _cabecera;
        private readonly FlowLayoutPanel _pila;
        private readonly Label _vacio;

        public ColumnaEstacion06AV(EstacionRiel06AV estacion, int indice)
        {
            Indice = indice;
            Padding = new Padding(4, 0, 4, 0);

            _cabecera = new CabeceraColumna06AV(estacion) { Dock = DockStyle.Top, Height = 56 };

            _pila = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 6, 0, 6)
            };

            // Dock.Top, NO Fill: dos controles Fill en el mismo panel hacen que el
            // primero en resolverse se coma todo el espacio y el segundo quede en cero.
            _vacio = new Label
            {
                Dock = DockStyle.Top,
                Height = 44,
                Text = "—",
                TextAlign = ContentAlignment.TopCenter,
                Padding = new Padding(0, 16, 0, 0),
                Visible = false
            };

            Controls.Add(_pila);
            Controls.Add(_vacio);
            Controls.Add(_cabecera);
            AplicarTema();
        }

        public int Indice { get; }
        public int Cantidad => _pila.Controls.Count;

        public IEnumerable<TarjetaOrden06AV> Tarjetas()
        {
            foreach (Control c in _pila.Controls)
                if (c is TarjetaOrden06AV t) yield return t;
        }

        public void Limpiar()
        {
            for (int i = _pila.Controls.Count - 1; i >= 0; i--)
            {
                Control c = _pila.Controls[i];
                _pila.Controls.RemoveAt(i);
                c.Dispose();
            }
        }

        public void Agregar(TarjetaOrden06AV tarjeta)
        {
            // NADA de Anchor acá: dentro de un FlowLayoutPanel, un Anchor con Right
            // hace que el hijo se ajuste a la celda del flow y puede terminar con
            // ancho cero — la tarjeta se agrega, cuenta en Controls.Count, y no se ve.
            // El ancho se fija a mano acá y en OnResize.
            tarjeta.Width = AnchoTarjeta();
            _pila.Controls.Add(tarjeta);
        }

        private int AnchoTarjeta() => Math.Max(150, _pila.ClientSize.Width - 22);

        public void ActualizarCabecera(int maximo)
        {
            _cabecera.Actualizar(Cantidad, maximo);
            _vacio.Visible = Cantidad == 0;
        }

        public void Renombrar(string titulo) => _cabecera.Renombrar(titulo);

        public void AplicarTema()
        {
            BackColor = Tema.FondoApp;
            _pila.BackColor = Tema.FondoApp;
            _vacio.BackColor = Tema.FondoApp;
            _vacio.ForeColor = Tema.TextoSuave;
            _vacio.Font = Tema.FuenteRegular;
            _cabecera.Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_pila == null) return;
            int ancho = AnchoTarjeta();
            foreach (Control c in _pila.Controls) c.Width = ancho;
        }
    }

    /// <summary>Cabecera de columna: ícono de la estación, nombre, contador y barra de carga.</summary>
    internal class CabeceraColumna06AV : Control
    {
        private readonly EstacionRiel06AV _estacion;
        private int _cantidad;
        private int _maximo = 1;

        public CabeceraColumna06AV(EstacionRiel06AV estacion)
        {
            _estacion = estacion ?? new EstacionRiel06AV();
            SetStyle(Pintura06AV.EstilosDibujo, true);
            // El fondo se pinta en OnPaint; un Control puro no admite BackColor transparente.

        }

        public void Actualizar(int cantidad, int maximo)
        {
            _cantidad = cantidad;
            _maximo = Math.Max(1, maximo);
            Invalidate();
        }

        public void Renombrar(string titulo) { _estacion.Titulo = titulo; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pintura06AV.Calidad(g);
            using (var b = new SolidBrush(Tema.FondoApp)) g.FillRectangle(b, ClientRectangle);

            var caja = new Rectangle(0, 6, Width - 1, Height - 12);
            Pintura06AV.Rellenar(g, caja, 8, Tema.EsOscuro ? Tema.Grafito800 : Color.White);
            Pintura06AV.Borde(g, caja, 8, Tema.Borde);

            bool angosta = caja.Width < 150;
            if (!angosta)
            {
                var rIcono = new RectangleF(caja.X + 10, caja.Y + 8, 20, 20);
                Iconos06AV.Dibujar(g, _estacion.Icono, rIcono, Tema.Primario, 1.8f);
            }

            string contador = _cantidad.ToString();
            int anchoChip = Math.Max(24, Pintura06AV.AnchoChip(g, contador, Tema.FuenteMini, 20));
            var chip = new Rectangle(caja.Right - 10 - anchoChip, caja.Y + 8, anchoChip, 20);
            Pintura06AV.Chip(g, chip, contador, Tema.FuenteMini,
                             _cantidad > 0 ? Tema.PrimarioSuave : (Tema.EsOscuro ? Tema.Acero700 : Tema.Acero100),
                             _cantidad > 0 ? Tema.Primario : Tema.TextoSuave);

            int xTitulo = caja.X + (angosta ? 10 : 36);
            var rTitulo = new Rectangle(xTitulo, caja.Y + 7, Math.Max(10, chip.Left - xTitulo - 8), 21);
            Pintura06AV.TextoIzquierda(g, _estacion.Titulo, Tema.FuenteBold, Tema.TextoFuerte, rTitulo);

            // Barra de carga: cuánto pesa esta estación respecto de la más cargada.
            var pista = new Rectangle(caja.X + 10, caja.Bottom - 12, caja.Width - 20, 4);
            Pintura06AV.Rellenar(g, pista, 2, Tema.EsOscuro ? Tema.Acero700 : Tema.Acero200);
            if (_cantidad > 0)
            {
                int w = Math.Max(6, (int)(pista.Width * (_cantidad / (float)_maximo)));
                var llena = new Rectangle(pista.X, pista.Y, w, pista.Height);
                Pintura06AV.Rellenar(g, llena, 2,
                    _cantidad >= _maximo && _maximo > 1 ? Tema.Acento : Tema.Primario);
            }
        }
    }
}
