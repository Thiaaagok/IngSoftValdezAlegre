using System;

namespace BE
{
    public class ControlCalidad06AV
    {
        public bool Encendido { get; set; }
        public bool Conexiones { get; set; }
        public bool SistemaOperativo { get; set; }
        public bool Drivers { get; set; }
        public string Observaciones { get; set; }
        public DateTime? Fecha { get; set; }
        public string Responsable { get; set; }
        public bool Registrado => Fecha.HasValue;
        public bool Aprobado => Encendido && Conexiones && SistemaOperativo && Drivers;
        public override string ToString() =>
            !Registrado ? "Sin control de calidad" : (Aprobado ? "Control de calidad aprobado" : "Control de calidad rechazado");
    }
}
