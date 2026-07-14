using DAL;
using SER.Integridad;
using System;
using System.Collections.Generic;
using System.Data;

namespace MPP
{
    /// <summary>
    /// Orquesta el cálculo del Dígito Verificador: recorre las tablas protegidas,
    /// calcula sus DVH/DVV con el motor y mapea contra la tabla DV.
    /// </summary>
    public class IntegridadMPP06AV
    {
        private readonly IntegridadDAL06AV _dal = new IntegridadDAL06AV();
        private readonly MotorDigitoVerificador06AV _motor;

        public IntegridadMPP06AV() : this(new CalculadorHexadecimal06AV()) { }

        public IntegridadMPP06AV(ICalculadorDigito06AV calculador)
        {
            _motor = new MotorDigitoVerificador06AV(calculador);
        }

        public ObjetoDV06AV Generar()
        {
            var obj = new ObjetoDV06AV();

            foreach (string tabla in IntegridadDAL06AV.TablasProtegidas)
            {
                DataTable datos;
                // Si una tabla protegida todavía no existe en la base (p. ej. los
                // scripts de PC Factory aún no se corrieron), la salteamos en vez de
                // tumbar todo el cálculo del DV. Cuando la tabla exista, entra sola.
                try { datos = _dal.ObtenerContenido(tabla); }
                catch { continue; }

                long dvh, dvv;
                _motor.Calcular(datos, out dvh, out dvv);
                obj.Tablas.Add(new DigitoTabla06AV { Tabla = tabla, DVH = dvh, DVV = dvv });
            }

            return obj;
        }

        public void Persistir(ObjetoDV06AV obj)
        {
            var datos = new List<KeyValuePair<string, string[]>>();
            foreach (DigitoTabla06AV t in obj.Tablas)
                datos.Add(new KeyValuePair<string, string[]>(t.Tabla, new[] { t.DVHHex, t.DVVHex }));

            _dal.GuardarDV(datos);
        }

        public ObjetoDV06AV ObtenerAlmacenado()
        {
            var obj = new ObjetoDV06AV();
            DataTable dv = _dal.ObtenerDV();

            foreach (DataRow row in dv.Rows)
            {
                obj.Tablas.Add(new DigitoTabla06AV
                {
                    Tabla = row["Tabla"].ToString(),
                    DVH = ParseHex(row["DVH"].ToString()),
                    DVV = ParseHex(row["DVV"].ToString())
                });
            }

            return obj;
        }

        public void Respaldar(string ruta) { _dal.Respaldar(ruta); }

        public void Restaurar(string ruta) { _dal.Restaurar(ruta); }

        private static long ParseHex(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            try { return Convert.ToInt64(s, 16); }
            catch { return 0; }
        }
    }
}
