using BE;
using BLL;
using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    /// <summary>ABM de Insumos (PC Factory). Resalta en rojo los que están bajo stock (RFN2).</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public partial class InsumosControl : AbmBaseControl06AV
    {
        private readonly InsumosBLL06AV _bll = new InsumosBLL06AV();
        private List<Insumo06AV> _items = new List<Insumo06AV>();

        private Label lblCodigo, lblDesc, lblStock, lblMin;
        private TextBox txtCodigo, txtDesc, txtStock, txtMin;

        public InsumosControl()
        {
            lblCodigo = new Label(); lblDesc = new Label(); lblStock = new Label(); lblMin = new Label();
            txtCodigo = new TextBox(); txtDesc = new TextBox(); txtStock = new TextBox(); txtMin = new TextBox();
            InicializarAbm();
        }

        protected override string ClaveTitulo => "pcf_insumos_titulo";

        protected override void ConstruirCampos(TableLayoutPanel tabla)
        {
            AgregarCampo(lblCodigo, txtCodigo);
            AgregarCampo(lblDesc, txtDesc);
            AgregarCampo(lblStock, txtStock);
            AgregarCampo(lblMin, txtMin);
            Grilla.DataBindingComplete += (s, e) => ResaltarBajoStock();
        }

        protected override void CargarDatosEnGrilla(DataGridView grilla)
        {
            _items = _bll.ObtenerTodos() ?? new List<Insumo06AV>();
            grilla.DataSource = null;
            grilla.DataSource = _items;
        }

        /// <summary>Resalta en rojo suave los insumos con stock por debajo del mínimo (RFN2).</summary>
        private void ResaltarBajoStock()
        {
            foreach (DataGridViewRow fila in Grilla.Rows)
            {
                if (fila.DataBoundItem is Insumo06AV ins && ins.BajoStock)
                {
                    fila.DefaultCellStyle.BackColor = Color.MistyRose;
                    fila.DefaultCellStyle.ForeColor = Color.FromArgb(180, 30, 30);
                }
            }
        }

        protected override void PrepararNuevo()
        {
            txtCodigo.ReadOnly = false;
            txtCodigo.Clear(); txtDesc.Clear();
            txtStock.Text = "0"; txtMin.Text = "0";
        }

        protected override bool CargarSeleccionEnCampos()
        {
            if (!(Grilla.CurrentRow?.DataBoundItem is Insumo06AV i)) return false;
            txtCodigo.Text = i.Codigo;
            txtDesc.Text = i.Descripcion;
            txtStock.Text = i.Stock.ToString();
            txtMin.Text = i.StockMinimo.ToString();
            txtCodigo.ReadOnly = true;
            return true;
        }

        protected override bool Guardar(bool editando)
        {
            if (!int.TryParse(txtStock.Text.Trim(), out int stock))
            { MostrarError("El stock debe ser un número entero."); return false; }
            if (!int.TryParse(txtMin.Text.Trim(), out int min))
            { MostrarError("El stock mínimo debe ser un número entero."); return false; }

            var i = new Insumo06AV
            {
                Codigo = txtCodigo.Text.Trim(),
                Descripcion = txtDesc.Text.Trim(),
                Stock = stock,
                StockMinimo = min
            };
            if (editando) _bll.Modificar(i); else _bll.Crear(i);
            return true;
        }

        protected override void EliminarSeleccion()
        {
            if (!(Grilla.CurrentRow?.DataBoundItem is Insumo06AV i))
            {
                MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro"));
                return;
            }
            if (!Confirmar($"¿Eliminar el insumo {i.Codigo}?",
                           GestorIdioma06AV.Instancia.Obtener("eliminar"))) return;
            try { _bll.Eliminar(i.Codigo); RecargarGrilla(); }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        protected override void AplicarIdiomaCampos()
        {
            var t = GestorIdioma06AV.Instancia;
            lblCodigo.Text = t.Obtener("codigo") + ":";
            lblDesc.Text = t.Obtener("descripcion") + ":";
            lblStock.Text = t.Obtener("stock") + ":";
            lblMin.Text = t.Obtener("stock_minimo") + ":";
        }
    }
}
