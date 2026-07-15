using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Instalador
{
    /// <summary>
    /// Punto de entrada del Instalador del sistema IngSoftValdezAlegre.
    ///
    /// • Sin parámetros abre el ASISTENTE GRÁFICO (elegir instancia, instalar, abrir el sistema).
    /// • Con parámetros de línea de comandos corre en MODO CONSOLA:
    ///     Instalador.exe --servidor . --bd IngSoftValdezAlegre
    ///     Instalador.exe --usuario sa --password 1234        (SQL Auth)
    ///     Instalador.exe --test                              (prueba del instalador)
    ///     Instalador.exe --silent                            (no pide confirmaciones)
    ///     Instalador.exe --help
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (RequiereConsola(args))
                return EjecutarConsola(args);

            return EjecutarAsistente();
        }

        /// <summary>Hay que usar consola si se pasó algún flag reconocido de línea de comandos.</summary>
        private static bool RequiereConsola(string[] args)
        {
            foreach (string a in args)
            {
                switch (a.ToLowerInvariant())
                {
                    case "--test":
                    case "--silent":
                    case "--help":
                    case "-h":
                    case "--servidor":
                    case "--bd":
                    case "--usuario":
                    case "--password":
                    case "--scripts":
                    case "--consola":
                        return true;
                }
            }
            return false;
        }

        // ──────────────────────────────────────────────────────────────
        //  MODO ASISTENTE (GUI)
        // ──────────────────────────────────────────────────────────────
        private static int EjecutarAsistente()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            OpcionesInstalacion06AV opciones;
            using (var conexion = new FrmConexion06AV())
            {
                if (conexion.ShowDialog() != DialogResult.OK)
                    return 2; // el usuario canceló
                opciones = conexion.Opciones;
            }

            if (string.IsNullOrWhiteSpace(opciones.CarpetaScripts))
                opciones.CarpetaScripts = CarpetaScriptsPorDefecto();

            using (var progreso = new FrmProgreso06AV(opciones))
            {
                progreso.ShowDialog();
                return progreso.Exito ? 0 : 1;
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  MODO CONSOLA
        // ──────────────────────────────────────────────────────────────
        private static int EjecutarConsola(string[] args)
        {
            // Como el proyecto es WinExe, hay que engancharse (o crear) una consola.
            if (!AttachConsole(ATTACH_PARENT_PROCESS))
                AllocConsole();

            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "Instalador - IngSoftValdezAlegre";

            try
            {
                if (TieneFlag(args, "--help") || TieneFlag(args, "-h"))
                {
                    MostrarAyuda();
                    return 0;
                }

                var opciones = ConstruirOpciones(args);
                bool esTest = TieneFlag(args, "--test");
                bool silent = TieneFlag(args, "--silent");

                Console.WriteLine("======================================================");
                Console.WriteLine("  INSTALADOR - Sistema IngSoftValdezAlegre");
                Console.WriteLine("======================================================");
                Console.WriteLine($"  Servidor : {opciones.Servidor}");
                Console.WriteLine($"  Base     : {opciones.BaseDatos}{(esTest ? " (modo prueba)" : "")}");
                Console.WriteLine($"  Auth     : {(opciones.SeguridadIntegrada ? "Windows" : "SQL (" + opciones.Usuario + ")")}");
                Console.WriteLine($"  Scripts  : {opciones.CarpetaScripts}");
                Console.WriteLine("======================================================");
                Console.WriteLine();

                if (!silent && !Confirmar("¿Continuar con la operación? (S/N): "))
                {
                    Console.WriteLine("Operación cancelada por el usuario.");
                    return 2;
                }

                if (esTest)
                {
                    bool ok = new InstaladorTest06AV(opciones).Ejecutar(Log);
                    return ok ? 0 : 1;
                }

                new InstaladorBLL06AV(opciones).Instalar(Log);

                // Dejar la app apuntando a la instancia elegida (en todas las ubicaciones
                // del ejecutable), igual que hace el asistente gráfico.
                var exes = ConfiguradorApp06AV.LocalizarTodosExeApp();
                int escritos = 0;
                foreach (string exe in exes)
                    if (ConfiguradorApp06AV.EscribirCadenaConexion(exe, opciones.CadenaBaseDatos()))
                        escritos++;
                Console.WriteLine(escritos > 0
                    ? $"Configuración de la aplicación actualizada ({escritos} ubicación/es)."
                    : "Aviso: no se encontró el ejecutable del sistema para configurarlo.");

                Console.WriteLine();
                Console.WriteLine("Instalación completada. Credenciales iniciales:");
                Console.WriteLine("   Login: admin   Contraseña: Admin1234");
                Console.WriteLine("   (el sistema pedirá cambiarla en el primer ingreso)");
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine();
                Console.WriteLine("ERROR: " + ex.Message);
                Console.ResetColor();
                return 1;
            }
            finally
            {
                if (EsInteractivo())
                {
                    Console.WriteLine();
                    Console.WriteLine("Presioná una tecla para salir...");
                    Console.ReadKey();
                }
            }
        }

        private static OpcionesInstalacion06AV ConstruirOpciones(string[] args)
        {
            var o = new OpcionesInstalacion06AV
            {
                Servidor = ValorArg(args, "--servidor") ?? ".",
                BaseDatos = ValorArg(args, "--bd") ?? "IngSoftValdezAlegre",
                CarpetaScripts = ValorArg(args, "--scripts") ?? CarpetaScriptsPorDefecto()
            };

            string usuario = ValorArg(args, "--usuario");
            if (!string.IsNullOrEmpty(usuario))
            {
                o.SeguridadIntegrada = false;
                o.Usuario = usuario;
                o.Contrasenia = ValorArg(args, "--password") ?? "";
            }

            return o;
        }

        /// <summary>
        /// Carpeta de scripts por defecto: la subcarpeta "Scripts" junto al ejecutable
        /// (los .sql se copian ahí al compilar). Si no está, prueba con la carpeta SQL
        /// del repositorio (útil al correr desde Visual Studio).
        /// </summary>
        private static string CarpetaScriptsPorDefecto()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            string junto = Path.Combine(baseDir, "Scripts");
            if (Directory.Exists(junto)) return junto;

            try
            {
                var dir = new DirectoryInfo(baseDir);
                for (int i = 0; i < 5 && dir != null; i++)
                {
                    string candidato = Path.Combine(dir.FullName, "SQL");
                    if (Directory.Exists(candidato)) return candidato;
                    dir = dir.Parent;
                }
            }
            catch { /* se devuelve el default de todas formas */ }

            return junto;
        }

        private static void Log(string mensaje) => Console.WriteLine(mensaje);

        private static bool Confirmar(string prompt)
        {
            Console.Write(prompt);
            string r = Console.ReadLine();
            return r != null && (r.Trim().Equals("S", StringComparison.OrdinalIgnoreCase)
                              || r.Trim().Equals("SI", StringComparison.OrdinalIgnoreCase)
                              || r.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase));
        }

        private static bool EsInteractivo()
        {
            try { return !Console.IsInputRedirected; }
            catch { return false; }
        }

        private static void MostrarAyuda()
        {
            Console.WriteLine("Instalador del sistema IngSoftValdezAlegre");
            Console.WriteLine();
            Console.WriteLine("  (sin parámetros)    Abre el asistente gráfico de instalación.");
            Console.WriteLine("  --servidor <inst>   Instancia de SQL Server (por defecto '.').");
            Console.WriteLine("  --bd <nombre>       Nombre de la base (por defecto IngSoftValdezAlegre).");
            Console.WriteLine("  --scripts <ruta>    Carpeta con los .sql (por defecto \\Scripts junto al exe).");
            Console.WriteLine("  --usuario <user>    Usa SQL Auth con ese usuario (si no, Windows Auth).");
            Console.WriteLine("  --password <pass>   Contraseña para SQL Auth.");
            Console.WriteLine("  --test              Corre la prueba del instalador sobre una base temporal.");
            Console.WriteLine("  --silent            No pide confirmación.");
            Console.WriteLine("  --help              Muestra esta ayuda.");
        }

        // ── Utilidades de parsing de argumentos ──────────────────────
        private static string ValorArg(string[] args, string clave)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i].Equals(clave, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            return null;
        }

        private static bool TieneFlag(string[] args, string flag)
        {
            foreach (string a in args)
                if (a.Equals(flag, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        // ── Consola para un proceso WinExe ───────────────────────────
        private const int ATTACH_PARENT_PROCESS = -1;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();
    }
}
