using System;

namespace Instalador
{
    /// <summary>Resultado de una verificación post-instalación.</summary>
    public class ResultadoInstalacion06AV
    {
        public int CantidadTablas { get; set; }
        public int CantidadUsuarios { get; set; }
        public bool Correcto => CantidadTablas > 0 && CantidadUsuarios > 0;
    }

    /// <summary>
    /// Lógica de negocio del Instalador. Coordina el flujo completo:
    ///   1. Preparar la base de datos (conexión + creación).
    ///   2. Ejecutar los scripts (esquema, procedimientos, seeds, admin).
    ///   3. Verificar que la instalación quedó consistente.
    /// </summary>
    public class InstaladorBLL06AV
    {
        private readonly OpcionesInstalacion06AV _opciones;
        private readonly InstaladorMPP06AV _mpp;
        private readonly InstaladorDAL06AV _dal;

        public InstaladorBLL06AV(OpcionesInstalacion06AV opciones)
        {
            _opciones = opciones ?? throw new ArgumentNullException(nameof(opciones));
            _mpp = new InstaladorMPP06AV(opciones);
            _dal = new InstaladorDAL06AV(opciones);
        }

        /// <summary>Ejecuta la instalación completa y devuelve el resultado de la verificación.</summary>
        public ResultadoInstalacion06AV Instalar(Action<string> log)
        {
            log?.Invoke("=== INSTALACIÓN DEL SISTEMA IngSoftValdezAlegre ===");

            _mpp.PrepararBaseDatos(log);

            log?.Invoke("Preparando estructura y datos iniciales...");
            int scripts = _mpp.EjecutarScripts(log);
            log?.Invoke($"Se ejecutaron {scripts} script(s) correctamente.");

            ResultadoInstalacion06AV resultado = Verificar();
            log?.Invoke(
                $"Verificación: {resultado.CantidadTablas} tabla(s), " +
                $"{resultado.CantidadUsuarios} usuario(s).");

            if (!resultado.Correcto)
                throw new InvalidOperationException(
                    "La verificación falló: la base no quedó con tablas y/o usuarios.");

            log?.Invoke("Instalación finalizada con éxito.");
            return resultado;
        }

        /// <summary>Comprueba que la base tenga tablas y al menos un usuario.</summary>
        public ResultadoInstalacion06AV Verificar()
        {
            return new ResultadoInstalacion06AV
            {
                CantidadTablas = _dal.ContarTablas(),
                CantidadUsuarios = _dal.ContarUsuarios()
            };
        }
    }
}
