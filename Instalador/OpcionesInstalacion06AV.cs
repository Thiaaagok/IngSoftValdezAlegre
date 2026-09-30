using System.Data.SqlClient;

namespace Instalador
{
    public class OpcionesInstalacion06AV
    {
        public string Servidor { get; set; } = ".";

        public string BaseDatos { get; set; } = "IngSoftValdezAlegre";

        public bool SeguridadIntegrada { get; set; } = true;

        public string Usuario { get; set; }
        public string Contrasenia { get; set; }

        public string CarpetaScripts { get; set; }

        public string CadenaMaster() => Construir("master");

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
