using BLL;
using IngSoftValdezAlegre.Common;
using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    public partial class PatentesControl : UserControl, IIdiomaAplicable06AV
    {
        private readonly PatentesBLL06AV _patentesSer = new PatentesBLL06AV();

        private List<Patente06AV> _patentes = new List<Patente06AV>();
        private bool _creando;
        private bool _suspenderSeleccion;

        public PatentesControl()
        {
            InitializeComponent();
            AplicarTema();
            AplicarIdioma();
            AjustarLayout();
            Resize += (s, e) => AjustarLayout();

            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Disposed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;

            // Observer: repintar cuando se cambia entre tema claro y oscuro.
            Tema.TemaChanged += AplicarTema;
            Disposed += (s, e) => Tema.TemaChanged -= AplicarTema;

            CargarDatos();
        }


        private void AplicarTema()
        {
            Tema.AplicarControl(this);
            Tema.AplicarTitulo(lblTitulo);
            Tema.AplicarGrilla(grilla);
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            lblTitulo.Text = t.Obtener("titulo_patentes");

            if (grilla.Columns["Id"] != null) grilla.Columns["Id"].HeaderText = t.Obtener("id");
            if (grilla.Columns["Descripcion"] != null) grilla.Columns["Descripcion"].HeaderText = t.Obtener("descripcion");
        }

        private void AjustarLayout()
        {
            int margen = 8;
            int ancho = Math.Max(560, ClientSize.Width);
            int alto = Math.Max(420, ClientSize.Height);
            int izquierdaW = Math.Max(240, ancho / 3);
            int derechaX = margen + izquierdaW + 16;
            int derechaW = ancho - derechaX - margen;

            lblTitulo.SetBounds(margen, 0, 240, 34);
            grilla.SetBounds(margen, 42, izquierdaW, alto - 50);
        }


        private void CargarDatos(string idSeleccionar = null)
        {
            _suspenderSeleccion = true;
            try
            {
                _patentes = _patentesSer.ObtenerTodos() ?? new List<Patente06AV>();
                grilla.DataSource = null;
                grilla.DataSource = _patentes;
                AplicarIdioma();
            }
            catch (Exception ex)
            {
                MostrarError(ex.Message);
            }
            finally
            {
                _suspenderSeleccion = false;
            }

        }

        private void MostrarError(string mensaje)
        {
            ConfirmacionForm.MostrarInfo(
                mensaje,
                GestorIdioma06AV.Instancia.Obtener("aviso"),
                ConfirmacionForm.TipoConfirmacion.Advertencia,
                FindForm());
        }
    }
}
