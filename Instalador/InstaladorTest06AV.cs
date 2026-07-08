using System;

namespace Instalador
{
    /// <summary>
    /// Prueba del Instalador. Instala el sistema completo sobre una base de datos
    /// temporal (nombre + "_TestInstalador"), verifica que hayan quedado las tablas
    /// y el usuario administrador, y finalmente borra esa base para no dejar residuos.
    ///
    /// No toca la base real de la aplicación.
    /// </summary>
    public class InstaladorTest06AV
    {
        private readonly OpcionesInstalacion06AV _base;

        public InstaladorTest06AV(OpcionesInstalacion06AV opcionesBase)
        {
            _base = opcionesBase ?? throw new ArgumentNullException(nameof(opcionesBase));
        }

        /// <summary>Ejecuta la prueba. Devuelve true si pasó, false si falló.</summary>
        public bool Ejecutar(Action<string> log)
        {
            // Copia de las opciones apuntando a una base de prueba.
            var opciones = new OpcionesInstalacion06AV
            {
                Servidor = _base.Servidor,
                BaseDatos = _base.BaseDatos + "_TestInstalador",
                SeguridadIntegrada = _base.SeguridadIntegrada,
                Usuario = _base.Usuario,
                Contrasenia = _base.Contrasenia,
                CarpetaScripts = _base.CarpetaScripts
            };

            var dal = new InstaladorDAL06AV(opciones);
            var bll = new InstaladorBLL06AV(opciones);

            log?.Invoke("=== PRUEBA DEL INSTALADOR ===");
            log?.Invoke($"Base de prueba: {opciones.BaseDatos}");

            try
            {
                // Partir de cero: si quedó de una corrida anterior, se borra.
                dal.EliminarBaseDatos();

                ResultadoInstalacion06AV r = bll.Instalar(log);

                bool okTablas = r.CantidadTablas >= 10;   // se esperan ~11 tablas
                bool okUsuario = r.CantidadUsuarios >= 1;  // el admin sembrado

                log?.Invoke("");
                log?.Invoke($"[{(okTablas ? "PASA" : "FALLA")}] Tablas creadas: {r.CantidadTablas} (esperado >= 10)");
                log?.Invoke($"[{(okUsuario ? "PASA" : "FALLA")}] Usuarios sembrados: {r.CantidadUsuarios} (esperado >= 1)");

                bool exito = okTablas && okUsuario;
                log?.Invoke("");
                log?.Invoke(exito ? "RESULTADO: PRUEBA SUPERADA (OK)" : "RESULTADO: PRUEBA FALLIDA (X)");
                return exito;
            }
            catch (Exception ex)
            {
                log?.Invoke("RESULTADO: PRUEBA FALLIDA (excepción)");
                log?.Invoke(ex.Message);
                return false;
            }
            finally
            {
                try
                {
                    dal.EliminarBaseDatos();
                    log?.Invoke($"Base de prueba '{opciones.BaseDatos}' eliminada.");
                }
                catch (Exception ex)
                {
                    log?.Invoke("Advertencia: no se pudo eliminar la base de prueba: " + ex.Message);
                }
            }
        }
    }
}
