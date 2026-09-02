using BE;
using BLL;
using SER;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    [System.ComponentModel.DesignerCategory("Code")]
    public partial class ComponentesControl : AbmBaseControl06AV
    {
        private readonly ComponentesBLL06AV _bll = new ComponentesBLL06AV();
        private List<Componente06AV> _items = new List<Componente06AV>();

        private Label lblCodigo, lblDesc, lblTipo, lblMarca, lblModelo, lblPrecio, lblStock, lblStockMin;
        private TextBox txtCodigo, txtDesc, txtMarca, txtModelo, txtPrecio, txtStock, txtStockMin;
        private ComboBox cboTipo;

        public ComponentesControl()
        {
            lblCodigo = new Label(); lblDesc = new Label(); lblTipo = new Label();
            lblMarca = new Label(); lblModelo = new Label(); lblPrecio = new Label();
            lblStock = new Label(); lblStockMin = new Label();
            txtCodigo = new TextBox(); txtDesc = new TextBox(); txtMarca = new TextBox();
            txtModelo = new TextBox(); txtPrecio = new TextBox(); txtStock = new TextBox(); txtStockMin = new TextBox();
            cboTipo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cboTipo.DataSource = Enum.GetValues(typeof(TipoComponente06AV))
                .Cast<TipoComponente06AV>()
                .Select(x => new KeyValuePair<TipoComponente06AV, string>(x, NombreTipo(x)))
                .ToList();
            cboTipo.ValueMember = "Key";
            cboTipo.DisplayMember = "Value";
            InicializarAbm();
        }

        protected override string ClaveTitulo => "pcf_componentes_titulo";

        protected override void ConstruirCampos(TableLayoutPanel tabla)
        {
            AgregarCampo(lblCodigo, txtCodigo);
            AgregarCampo(lblDesc, txtDesc);
            AgregarCampo(lblTipo, cboTipo);
            AgregarCampo(lblMarca, txtMarca);
            AgregarCampo(lblModelo, txtModelo);
            AgregarCampo(lblPrecio, txtPrecio);
            AgregarCampo(lblStock, txtStock);
            AgregarCampo(lblStockMin, txtStockMin);
        }

        protected override void CargarDatosEnGrilla(DataGridView grilla)
        {
            _items = _bll.ObtenerTodos() ?? new List<Componente06AV>();
            grilla.DataSource = null;
            grilla.DataSource = _items;
        }

        protected override void PrepararNuevo()
        {
            txtCodigo.ReadOnly = false;
            txtCodigo.Clear(); txtDesc.Clear(); txtMarca.Clear(); txtModelo.Clear();
            txtPrecio.Text = "0"; txtStock.Text = "0"; txtStockMin.Text = "0";
            if (cboTipo.Items.Count > 0) cboTipo.SelectedIndex = 0;
        }

        protected override bool CargarSeleccionEnCampos()
        {
            if (!(Grilla.CurrentRow?.DataBoundItem is Componente06AV c)) return false;
            txtCodigo.Text = c.Codigo;
            txtDesc.Text = c.Descripcion;
            cboTipo.SelectedValue = c.Tipo;
            txtMarca.Text = c.Marca;
            txtModelo.Text = c.Modelo;
            txtPrecio.Text = c.PrecioUnitario.ToString("0.00");
            txtStock.Text = c.Stock.ToString();
            txtStockMin.Text = c.StockMinimo.ToString();
            txtCodigo.ReadOnly = true;
            return true;
        }

        protected override bool Guardar(bool editando)
        {
            if (!decimal.TryParse(txtPrecio.Text.Trim(), out decimal precio))
            { MostrarError("El precio debe ser un número válido."); return false; }
            if (!int.TryParse(txtStock.Text.Trim(), out int stock))
            { MostrarError("El stock debe ser un número entero."); return false; }
            if (!int.TryParse(txtStockMin.Text.Trim(), out int stockMin))
            { MostrarError("El stock mínimo debe ser un número entero."); return false; }

            var c = new Componente06AV
            {
                Codigo = txtCodigo.Text.Trim(),
                Descripcion = txtDesc.Text.Trim(),
                Tipo = cboTipo.SelectedValue is TipoComponente06AV tp ? tp : TipoComponente06AV.Otro,
                Marca = txtMarca.Text.Trim(),
                Modelo = txtModelo.Text.Trim(),
                PrecioUnitario = precio,
                Stock = stock,
                StockMinimo = stockMin
            };
            if (editando) _bll.Modificar(c); else _bll.Crear(c);
            return true;
        }

        protected override void EliminarSeleccion()
        {
            if (!(Grilla.CurrentRow?.DataBoundItem is Componente06AV c))
            {
                MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro"));
                return;
            }
            // El borrado es lógico: el trigger de la base impide el DELETE físico
            // y la baja queda asentada en la bitácora de cambios.
            if (!Confirmar(string.Format(GestorIdioma06AV.Instancia.Obtener("pcf_confirmar_baja_componente"), c.Codigo),
                           GestorIdioma06AV.Instancia.Obtener("eliminar"))) return;
            try { _bll.Eliminar(c.Codigo); RecargarGrilla(); }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        protected override void AplicarIdiomaCampos()
        {
            var t = GestorIdioma06AV.Instancia;
            lblCodigo.Text = t.Obtener("codigo") + ":";
            lblDesc.Text = t.Obtener("descripcion") + ":";
            lblTipo.Text = t.Obtener("tipo") + ":";
            lblMarca.Text = t.Obtener("marca") + ":";
            lblModelo.Text = t.Obtener("modelo") + ":";
            lblPrecio.Text = t.Obtener("precio") + ":";
            lblStock.Text = t.Obtener("stock") + ":";
            lblStockMin.Text = t.Obtener("pcf_col_minimo") + ":";
        }

        private static string NombreTipo(TipoComponente06AV t)
        {
            switch (t)
            {
                case TipoComponente06AV.Procesador: return "Procesador";
                case TipoComponente06AV.MemoriaRAM: return "Memoria RAM";
                case TipoComponente06AV.Disco: return "Disco";
                case TipoComponente06AV.PlacaMadre: return "Placa madre";
                case TipoComponente06AV.Fuente: return "Fuente";
                case TipoComponente06AV.Gabinete: return "Gabinete";
                case TipoComponente06AV.PlacaDeVideo: return "Placa de video";
                case TipoComponente06AV.Refrigeracion: return "Refrigeración";
                default: return "Otro";
            }
        }
    }
}
