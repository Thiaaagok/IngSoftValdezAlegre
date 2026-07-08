using System.Data.SqlClient;

namespace Instalador
{
    /// <summary>
    /// Parámetros de la instalación: a qué servidor conectarse, qué base crear,
    /// con qué credenciales y de qué carpeta tomar los scripts .sql.
    /// </summary>
    public class OpcionesInstalacion06AV
    {
        /// <summary>Instancia de SQL Server. Por defecto la local ("." = (local)).</summary>
        public string Servidor { get; set; } = ".";

        /// <summary>Nombre de la base de datos a crear/preparar.</summary>
        public string BaseDatos { get; set; } = "IngSoftValdezAlegre";

        /// <summary>Si es true usa Autenticación de Windows; si no, Usuario/Contraseña.</summary>
        public bool SeguridadIntegrada { get; set; } = true;

        public string Usuario { get; set; }
        public string Contrasenia { get; set; }

        /// <summary>Carpeta con los scripts .sql a ejecutar, en orden alfabético.</summary>
        public string CarpetaScripts { get; set; }

        /// <summary>Cadena de conexión apuntando a la base 'master'.</summary>
        public string CadenaMaster() => Construir("master");

        /// <summary>Cadena de conexión apuntando a la base de la aplicación.</summary>
        public string CadenaBaseDatos() => Construir(BaseDatos);

        private string Construir(string catalogo)
        {
            var b = new SqlConnectionStringBuilder
            {
                DataSource = Servidor,
                InitialCatalog = catalogo
            };

            if (SeguridadIntegrada)
            {
                b.IntegratedSecurity = true;
            }
            else
            {
                b.UserID = Usuario;
                b.Password = Contrasenia;
            }

            return b.ConnectionString;
        }
    }
}
