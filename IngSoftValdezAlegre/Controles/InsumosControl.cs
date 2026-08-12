using SER;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    // ========================================================================
    //  InsumosControl — OBSOLETO tras el refactor de dominio.
    //  Insumo se fusionó en Componente: la gestión de stock ahora vive en
    //  ComponentesControl. Se deja un stub válido (sin InsumosBLL) para no
    //  romper la compilación; ya no figura en el menú.
    // ========================================================================
    [System.ComponentModel.DesignerCategory("Code")]
    public partial class InsumosControl : AbmBaseControl06AV
    {
        public InsumosControl()
        {
            InicializarAbm();
        }

        protected override string ClaveTitulo => "pcf_insumos_titulo";

        protected override void ConstruirCampos(TableLayoutPanel tabla) { }

        protected override void CargarDatosEnGrilla(DataGridView grilla)
        {
            grilla.DataSource = null;
        }

        protected override void PrepararNuevo() { }

        protected override bool CargarSeleccionEnCampos() => false;

        protected override bool Guardar(bool editando)
        {
            MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_insumos_fusionados"));
            return false;
        }

        protected override void EliminarSeleccion() { }

        protected override void AplicarIdiomaCampos() { }
    }
}
