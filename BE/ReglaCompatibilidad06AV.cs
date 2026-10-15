namespace BE
{
    public enum NivelReglaCompatibilidad06AV
    {
        Advertencia = 1,
        Incompatible = 2
    }

    public enum AtributoCompatibilidad06AV
    {
        Marca,
        Plataforma,
        Socket,
        TipoMemoria,
        FrecuenciaMemoriaMhz,
        CapacidadGb,
        SlotsMemoria,
        MemoriaMaximaGb,
        Formato,
        ConsumoW,
        PotenciaW,
        TdpSoportadoW,
        GraficosIntegrados,
        Desbloqueado,
        PermiteOverclock
    }

    public enum OperadorCompatibilidad06AV
    {
        Igual,
        Distinto,
        MenorOIgual,
        MayorOIgual,
        Contiene
    }

    public class ReglaCompatibilidad06AV
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public bool Sistema { get; set; }
        public bool Activa { get; set; } = true;
        public NivelReglaCompatibilidad06AV Nivel { get; set; } = NivelReglaCompatibilidad06AV.Incompatible;
        public TipoComponente06AV? TipoA { get; set; }
        public AtributoCompatibilidad06AV? AtributoA { get; set; }
        public OperadorCompatibilidad06AV? Operador { get; set; }
        public TipoComponente06AV? TipoB { get; set; }
        public AtributoCompatibilidad06AV? AtributoB { get; set; }
        public decimal? Valor { get; set; }
        public string Mensaje { get; set; }

        public override string ToString() => Nombre;
    }
}
