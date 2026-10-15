using BLL;
using SER;
using System.Drawing;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA.UI
{
    internal static class AyudaCuit06AV
    {
        public static string Evaluar(string texto, out Color color)
        {
            var t = GestorIdioma06AV.Instancia;
            string digitos = Cuit06AV.SoloDigitos(texto);
            color = Tema.TextoSuave;
            if (digitos.Length == 0) return t.Obtener("pcf_cuit_ayuda");
            if (digitos.Length < 11)
            {
                color = Tema.Advertencia;
                return t.Obtener("pcf_cuit_faltan", 11 - digitos.Length);
            }
            if (digitos.Length > 11)
            {
                color = Tema.Peligro;
                return t.Obtener("pcf_cuit_sobran");
            }
            if (Cuit06AV.EsValido(digitos))
            {
                color = Tema.Exito;
                return "✓  " + t.Obtener("pcf_cuit_ok", Cuit06AV.Formatear(digitos));
            }
            color = Tema.Peligro;
            return t.Obtener("pcf_cuit_dv");
        }

        public static void Vincular(TextBox entrada, Label ayuda)
        {
            void Actualizar()
            {
                ayuda.Text = Evaluar(entrada.Text, out Color c);
                ayuda.ForeColor = c;
            }
            entrada.TextChanged += (s, e) => Actualizar();
            Actualizar();
        }

        public static Label NuevaEtiqueta() => new Label
        {
            AutoSize = true,
            Font = Tema.FuenteMini,
            ForeColor = Tema.TextoSuave,
            BackColor = Color.Transparent,
            Margin = new Padding(3, 0, 3, 4)
        };
    }
}
