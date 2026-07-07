using BLL;
using IngSoftValdezAlegre.Common;
using SER;
using SER.Integridad;
using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace IngSoftValdezAlegre
{
    /// <summary>
    /// GUI de REPARACIÓN del Dígito Verificador, para quien tiene la patente que lo
    /// habilita. Ofrece Recalcular (acepta/normaliza), Restore (vuelve a un backup) y Salir.
    /// </summary>
    public partial class FRMReparacionDV : Form
    {
        private readonly ResultadoVerificacion06AV _resultado;
        private readonly IntegridadBLL06AV _integridad = new IntegridadBLL06AV();

        public FRMReparacionDV(ResultadoVerificacion06AV resultado)
        {
            _resultado = resultado ?? new ResultadoVerificacion06AV();
            InitializeComponent();
            ConfigurarEstiloseIdioma();
        }

        private void ConfigurarEstiloseIdioma()
        {
            var t = GestorIdioma06AV.Instancia;

            StartPosition = FormStartPosition.CenterScreen;
            Text = t.Obtener("dv_titulo");
            BackColor = Tema.FondoApp;
            ForeColor = Tema.Texto;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            lblTitulo.Text = t.Obtener("dv_titulo");
            lblTitulo.Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold);
            lblTitulo.ForeColor = Tema.Peligro;

            lblIntro.Text = t.Obtener("dv_intro");
            lblIntro.ForeColor = Tema.Texto;

            lblDetalle.Text = t.Obtener("dv_tablas_afectadas");
            lblDetalle.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
            lblDetalle.ForeColor = Tema.TextoSuave;

            txtDetalle.BackColor = Tema.FondoElevado;
            txtDetalle.ForeColor = Tema.Texto;
            txtDetalle.Text = ConstruirDetalle();

            btnRecalcular.Text = t.Obtener("dv_recalcular");
            Tema.AplicarBotonPrimario(btnRecalcular);
            btnRecalcular.Click += (s, e) => Recalcular();

            btnRestore.Text = t.Obtener("dv_restore");
            Tema.AplicarBotonAcento(btnRestore);
            btnRestore.Click += (s, e) => Restaurar();

            btnSalir.Text = t.Obtener("dv_salir");
            Tema.AplicarBotonPeligro(btnSalir);
            btnSalir.Click += (s, e) => Salir();
        }

        private string ConstruirDetalle()
        {
            var sb = new StringBuilder();
            if (_resultado.Detalles.Count > 0)
            {
                foreach (string d in _resultado.Detalles)
                    sb.AppendLine("• " + d);
            }
            else if (_resultado.TablasInconsistentes.Count > 0)
            {
                foreach (string tabla in _resultado.TablasInconsistentes)
                    sb.AppendLine("• " + tabla);
            }
            else
            {
                sb.AppendLine(GestorIdioma06AV.Instancia.Obtener("dv_inconsistencia_generica"));
            }
            return sb.ToString();
        }

        // No resuelve la inconsistencia: la acepta y normaliza el DV.
        private void Recalcular()
        {
            var t = GestorIdioma06AV.Instancia;
            try
            {
                _integridad.Recalcular();
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("dv_recalculado"),
                    titulo: t.Obtener("dv_titulo"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Info,
                    owner: this);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MostrarError(ex.Message);
            }
        }

        private void Restaurar()
        {
            var t = GestorIdioma06AV.Instancia;

            using (var ofd = new OpenFileDialog
            {
                Title = t.Obtener("dv_restore_titulo"),
                Filter = "Backup SQL Server (*.bak)|*.bak|Todos los archivos (*.*)|*.*"
            })
            {
                if (ofd.ShowDialog(this) != DialogResult.OK)
                    return;

                bool confirmado = ConfirmacionForm.Mostrar(
                    t.Obtener("dv_restore_confirmar"),
                    titulo: t.Obtener("dv_restore"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                    textoSi: t.Obtener("dv_restore"),
                    textoNo: t.Obtener("cancelar"),
                    owner: this);
                if (!confirmado) return;

                try
                {
                    _integridad.Restaurar(ofd.FileName);
                    ConfirmacionForm.MostrarInfo(
                        t.Obtener("dv_restore_ok"),
                        titulo: t.Obtener("dv_titulo"),
                        tipo: ConfirmacionForm.TipoConfirmacion.Info,
                        owner: this);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    MostrarError(t.Obtener("dv_restore_error") + "\n" + ex.Message);
                }
            }
        }

        private void Salir()
        {
            Close();
            Application.Exit();
        }

        private void MostrarError(string mensaje)
        {
            ConfirmacionForm.MostrarInfo(
                mensaje,
                titulo: GestorIdioma06AV.Instancia.Obtener("error"),
                tipo: ConfirmacionForm.TipoConfirmacion.Error,
                owner: this);
        }
    }
}
