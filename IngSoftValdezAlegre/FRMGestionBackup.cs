using BLL;
using IngSoftValdezAlegre.Common;
using SER;
using SER.Integridad;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace IngSoftValdezAlegre
{
    /// <summary>
    /// GUI de Gestión de Backups. Permite:
    ///   • Generar un backup manual, eligiendo dónde guardarlo (por defecto en
    ///     C:\Backups\GestionUsuario\) con un nombre que incluye fecha y hora.
    ///   • Restaurar la base eligiendo un archivo .bak desde cualquier ubicación.
    /// Si no hay ningún backup elegido/existente, el restore no se ejecuta.
    /// </summary>
    public partial class FRMGestionBackup : Form
    {
        private readonly IntegridadBLL06AV _integridad = new IntegridadBLL06AV();

        public FRMGestionBackup()
        {
            InitializeComponent();
            ConfigurarEstiloseIdioma();
            ActualizarEstado();
        }

        private void ConfigurarEstiloseIdioma()
        {
            var t = GestorIdioma06AV.Instancia;

            StartPosition = FormStartPosition.CenterScreen;
            Text = t.Obtener("backup_titulo");
            BackColor = Tema.FondoApp;
            ForeColor = Tema.Texto;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            lblTitulo.Text = t.Obtener("backup_titulo");
            lblTitulo.Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold);
            lblTitulo.ForeColor = Tema.Primario;

            lblIntro.Text = string.Format(
                t.Obtener("backup_intro"), IntegridadBLL06AV.CarpetaBackupPorDefecto);
            lblIntro.ForeColor = Tema.TextoSuave;

            btnGenerar.Text = t.Obtener("backup_generar");
            Tema.AplicarBotonPrimario(btnGenerar);
            btnGenerar.Click += (s, e) => GenerarBackup();

            btnRestaurar.Text = t.Obtener("backup_restaurar");
            Tema.AplicarBotonAcento(btnRestaurar);
            btnRestaurar.Click += (s, e) => Restaurar();

            lblEstado.ForeColor = Tema.TextoSuave;
            lblEstado.Font = new Font("Segoe UI", 8.5f, FontStyle.Italic);

            btnCerrar.Text = t.Obtener("cerrar");
            Tema.AplicarBotonSecundario(btnCerrar);
            btnCerrar.Click += (s, e) => Close();
        }

        /// <summary>Muestra cuántos backups hay y la fecha del más reciente.</summary>
        private void ActualizarEstado()
        {
            var t = GestorIdioma06AV.Instancia;
            IList<InfoBackup06AV> backups = _integridad.ListarBackups();

            if (backups.Count == 0)
            {
                lblEstado.Text = t.Obtener("backup_sin_respaldos");
                return;
            }

            InfoBackup06AV ultimo = backups[0];
            lblEstado.Text = string.Format(
                t.Obtener("backup_estado"),
                backups.Count,
                ultimo.Fecha.ToString("dd/MM/yyyy HH:mm:ss"),
                ultimo.Nombre);
        }

        // ── Generar backup (elige dónde guardar) ──────────────────────
        private void GenerarBackup()
        {
            var t = GestorIdioma06AV.Instancia;

            using (var sfd = new SaveFileDialog
            {
                Title = t.Obtener("backup_generar"),
                Filter = "Backup SQL Server (*.bak)|*.bak",
                DefaultExt = "bak",
                AddExtension = true,
                InitialDirectory = _integridad.ObtenerCarpetaBackupPorDefecto(),
                FileName = _integridad.GenerarNombreArchivoBackup(),
                OverwritePrompt = true
            })
            {
                if (sfd.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    string ruta = _integridad.Respaldar(sfd.FileName);
                    ConfirmacionForm.MostrarInfo(
                        string.Format(t.Obtener("backup_generado_ok"), ruta),
                        titulo: t.Obtener("backup_titulo"),
                        tipo: ConfirmacionForm.TipoConfirmacion.Info,
                        owner: this);
                    ActualizarEstado();
                }
                catch (Exception ex)
                {
                    MostrarError(t.Obtener("backup_generar_error") + "\n" + ex.Message);
                }
            }
        }

        // ── Elegir archivo y restaurar (desde cualquier ubicación) ────
        private void Restaurar()
        {
            var t = GestorIdioma06AV.Instancia;

            // El restore SIEMPRE se hace en base a un backup: hay que elegir uno.
            string carpetaInicial = Directory.Exists(IntegridadBLL06AV.CarpetaBackupPorDefecto)
                ? IntegridadBLL06AV.CarpetaBackupPorDefecto
                : Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);

            using (var ofd = new OpenFileDialog
            {
                Title = t.Obtener("backup_restaurar_titulo"),
                Filter = "Backup SQL Server (*.bak)|*.bak|Todos los archivos (*.*)|*.*",
                InitialDirectory = carpetaInicial,
                CheckFileExists = true
            })
            {
                if (ofd.ShowDialog(this) != DialogResult.OK)
                    return;

                bool confirmado = ConfirmacionForm.Mostrar(
                    t.Obtener("backup_restaurar_confirmar"),
                    titulo: t.Obtener("backup_restaurar"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                    textoSi: t.Obtener("backup_restaurar"),
                    textoNo: t.Obtener("cancelar"),
                    owner: this);
                if (!confirmado) return;

                try
                {
                    _integridad.Restaurar(ofd.FileName);
                    ConfirmacionForm.MostrarInfo(
                        t.Obtener("backup_restaurado_ok"),
                        titulo: t.Obtener("backup_titulo"),
                        tipo: ConfirmacionForm.TipoConfirmacion.Info,
                        owner: this);
                    ActualizarEstado();
                }
                catch (Exception ex)
                {
                    MostrarError(t.Obtener("backup_restaurar_error") + "\n" + ex.Message);
                }
            }
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
