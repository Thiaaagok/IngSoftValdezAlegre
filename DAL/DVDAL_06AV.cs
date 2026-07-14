using System;
using System.Collections.Generic;
using System.Data;

namespace DAL
{
    /// <summary>
    /// Fachada de acceso a datos del Dígito Verificador (DV) según el diagrama de clases.
    /// Expone obtener el contenido de una tabla y leer/guardar su DVH y DVV, delegando en
    /// <see cref="IntegridadDAL06AV"/>, que es la implementación real sobre la tabla de
    /// control DV (Tabla, DVH, DVV). No cambia el comportamiento del sistema.
    /// </summary>
    public class DVDAL_06AV
    {
        private readonly IntegridadDAL06AV _dal = new IntegridadDAL06AV();

        /// <summary>Contenido completo de una tabla protegida (para calcular sus dígitos).</summary>
        public DataTable ObtenerTabla(string tabla) => _dal.ObtenerContenido(tabla);

        public string ObtenerDVH(string tabla) => LeerCampoDV(tabla, "DVH");
        public string ObtenerDVV(string tabla) => LeerCampoDV(tabla, "DVV");

        public void GuardarDVH(string tabla, string dvh) => GuardarCampoDV(tabla, dvh, null);
        public void GuardarDVV(string tabla, string dvv) => GuardarCampoDV(tabla, null, dvv);

        private string LeerCampoDV(string tabla, string columna)
        {
            foreach (DataRow r in _dal.ObtenerDV().Rows)
                if (string.Equals(r["Tabla"].ToString(), tabla, StringComparison.OrdinalIgnoreCase))
                    return r[columna].ToString();
            return null;
        }

        // La tabla DV se reescribe completa (así funciona IntegridadDAL06AV.GuardarDV):
        // se lee todo, se modifica la fila de la tabla pedida y se vuelve a guardar.
        private void GuardarCampoDV(string tabla, string dvh, string dvv)
        {
            var datos = new List<KeyValuePair<string, string[]>>();
            bool encontrada = false;

            foreach (DataRow r in _dal.ObtenerDV().Rows)
            {
                string t = r["Tabla"].ToString();
                string h = r["DVH"].ToString();
                string v = r["DVV"].ToString();
                if (string.Equals(t, tabla, StringComparison.OrdinalIgnoreCase))
                {
                    if (dvh != null) h = dvh;
                    if (dvv != null) v = dvv;
                    encontrada = true;
                }
                datos.Add(new KeyValuePair<string, string[]>(t, new[] { h, v }));
            }

            if (!encontrada)
                datos.Add(new KeyValuePair<string, string[]>(tabla, new[] { dvh ?? "0", dvv ?? "0" }));

            _dal.GuardarDV(datos);
        }
    }
}
