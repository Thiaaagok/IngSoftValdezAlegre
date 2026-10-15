namespace BE
{
    public class EspecificacionComponente06AV
    {
        public string Plataforma { get; set; }
        public string Socket { get; set; }
        public string TipoMemoria { get; set; }
        public int? FrecuenciaMemoriaMhz { get; set; }
        public int? CapacidadGb { get; set; }
        public int? SlotsMemoria { get; set; }
        public int? MemoriaMaximaGb { get; set; }
        public string Formato { get; set; }
        public int? ConsumoW { get; set; }
        public int? PotenciaW { get; set; }
        public int? TdpSoportadoW { get; set; }
        public bool? GraficosIntegrados { get; set; }
        public bool? Desbloqueado { get; set; }
        public bool? PermiteOverclock { get; set; }

        public bool Estimada { get; set; }

        public EspecificacionComponente06AV Copiar() => (EspecificacionComponente06AV)MemberwiseClone();
    }
}
