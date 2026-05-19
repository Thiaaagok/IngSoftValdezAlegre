using System.Drawing;

namespace IngSoftValdezAlegre
{
    /// <summary>
    /// Paleta de colores centralizada para toda la aplicación (modo claro).
    /// Está pensada para poder agregar a futuro un modo oscuro: alcanza con
    /// duplicar la paleta y exponer un selector.
    /// </summary>
    internal static class Tema
    {
        // Azules
        public static readonly Color Azul50  = Color.FromArgb(227, 242, 253); // #E3F2FD
        public static readonly Color Azul400 = Color.FromArgb(66, 165, 245);  // #42A5F5
        public static readonly Color Azul800 = Color.FromArgb(21, 101, 192);  // #1565C0

        // Verdes
        public static readonly Color Verde50  = Color.FromArgb(232, 245, 233); // #E8F5E9
        public static readonly Color Verde400 = Color.FromArgb(102, 187, 106); // #66BB6A
        public static readonly Color Verde800 = Color.FromArgb(46, 125, 50);   // #2E7D32

        // Grises
        public static readonly Color Gris50  = Color.FromArgb(250, 250, 250); // #FAFAFA
        public static readonly Color Gris400 = Color.FromArgb(189, 189, 189); // #BDBDBD
        public static readonly Color Gris900 = Color.FromArgb(33, 33, 33);    // #212121

        // Amber
        public static readonly Color Amber50  = Color.FromArgb(255, 248, 225); // #FFF8E1
        public static readonly Color Amber400 = Color.FromArgb(255, 202, 40);  // #FFCA28

        // Semánticos
        public static readonly Color FondoApp    = Gris50;
        public static readonly Color FondoPanel  = Color.White;
        public static readonly Color Texto       = Gris900;
        public static readonly Color TextoSuave  = Color.FromArgb(97, 97, 97);
        public static readonly Color Borde       = Gris400;
        public static readonly Color Primario    = Azul800;
        public static readonly Color PrimarioHover = Color.FromArgb(13, 71, 161);
        public static readonly Color Exito       = Verde800;
        public static readonly Color Advertencia = Amber400;
        public static readonly Color Peligro     = Color.FromArgb(198, 40, 40); // rojo para inactivos / errores

        // Tipografías
        public static readonly Font FuenteRegular = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        public static readonly Font FuenteBold    = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
        public static readonly Font FuenteTitulo  = new Font("Segoe UI Semibold", 14f, FontStyle.Bold);
        public static readonly Font FuenteSubtit  = new Font("Segoe UI Semibold", 11f, FontStyle.Bold);
        public static readonly Font FuenteMenu    = new Font("Segoe UI", 10.5f, FontStyle.Regular);
    }
}
