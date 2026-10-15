using BE;
using SER;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Armado
{
    public enum NivelCompatibilidad06AV
    {
        Compatible,
        Advertencia,
        Incompatible
    }

    public sealed class ProblemaCompatibilidad06AV
    {
        private readonly string _texto;

        internal ProblemaCompatibilidad06AV(NivelCompatibilidad06AV nivel, string regla, IEnumerable<Componente06AV> involucrados,
                                            params object[] argumentos)
        {
            Nivel = nivel;
            Regla = regla;
            Argumentos = argumentos ?? new object[0];
            Codigos = involucrados.Where(c => c != null).Select(c => c.Codigo).Distinct().ToList();
        }

        internal ProblemaCompatibilidad06AV(NivelCompatibilidad06AV nivel, string regla, string texto,
                                            IEnumerable<Componente06AV> involucrados)
            : this(nivel, regla, involucrados)
        {
            _texto = texto;
        }

        public NivelCompatibilidad06AV Nivel { get; internal set; }
        public string Regla { get; }
        public object[] Argumentos { get; }
        public IReadOnlyList<string> Codigos { get; }

        public bool Involucra(Componente06AV c) => c != null && Codigos.Contains(c.Codigo);

        public string Mensaje => _texto ?? GestorIdioma06AV.Instancia.Obtener(Regla, Argumentos);
    }

    public sealed class ResultadoCompatibilidad06AV
    {
        private readonly List<ProblemaCompatibilidad06AV> _problemas;

        internal ResultadoCompatibilidad06AV(IEnumerable<ProblemaCompatibilidad06AV> problemas)
        {
            _problemas = problemas.OrderByDescending(p => p.Nivel).ToList();
        }

        public IReadOnlyList<ProblemaCompatibilidad06AV> Problemas => _problemas;

        public NivelCompatibilidad06AV Nivel =>
            _problemas.Count == 0 ? NivelCompatibilidad06AV.Compatible : _problemas.Max(p => p.Nivel);

        public bool EsIncompatible => Nivel == NivelCompatibilidad06AV.Incompatible;

        public IEnumerable<ProblemaCompatibilidad06AV> DeNivel(NivelCompatibilidad06AV nivel) =>
            _problemas.Where(p => p.Nivel == nivel);

        public string Texto(NivelCompatibilidad06AV nivel, string separador = "\n") =>
            string.Join(separador, DeNivel(nivel).Select(p => p.Mensaje).Distinct());

        public string Principal => _problemas.Count == 0 ? string.Empty : _problemas[0].Mensaje;
    }

    public static class ReglasCompatibilidad06AV
    {
        public const int ModulosRamSinLimite = 8;

        private sealed class Predeterminada
        {
            public Predeterminada(NivelCompatibilidad06AV nivel, decimal? valor = null) { Nivel = nivel; Valor = valor; }
            public NivelCompatibilidad06AV Nivel { get; }
            public decimal? Valor { get; }
        }

        private static readonly Dictionary<string, Predeterminada> Predeterminadas = new Dictionary<string, Predeterminada>
        {
            { "plataforma",          new Predeterminada(NivelCompatibilidad06AV.Incompatible) },
            { "socket",              new Predeterminada(NivelCompatibilidad06AV.Incompatible) },
            { "oc_cpu",              new Predeterminada(NivelCompatibilidad06AV.Advertencia) },
            { "ddr",                 new Predeterminada(NivelCompatibilidad06AV.Incompatible) },
            { "ram_tipos",           new Predeterminada(NivelCompatibilidad06AV.Incompatible) },
            { "ram_mezcla",          new Predeterminada(NivelCompatibilidad06AV.Advertencia) },
            { "ram_oc",              new Predeterminada(NivelCompatibilidad06AV.Advertencia) },
            { "ram_slots",           new Predeterminada(NivelCompatibilidad06AV.Incompatible, 4) },
            { "ram_max",             new Predeterminada(NivelCompatibilidad06AV.Incompatible) },
            { "formato",             new Predeterminada(NivelCompatibilidad06AV.Incompatible) },
            { "cooler_socket",       new Predeterminada(NivelCompatibilidad06AV.Incompatible) },
            { "cooler_tdp",          new Predeterminada(NivelCompatibilidad06AV.Incompatible) },
            { "fuente_insuficiente", new Predeterminada(NivelCompatibilidad06AV.Incompatible) },
            { "fuente_limite",       new Predeterminada(NivelCompatibilidad06AV.Advertencia, 30) },
            { "sin_video",           new Predeterminada(NivelCompatibilidad06AV.Incompatible) }
        };

        private static readonly object Candado = new object();
        private static Dictionary<string, ReglaCompatibilidad06AV> _sistema = new Dictionary<string, ReglaCompatibilidad06AV>();
        private static List<ReglaCompatibilidad06AV> _propias = new List<ReglaCompatibilidad06AV>();
        private static bool _configuradas;

        public static IEnumerable<string> CodigosSistema => Predeterminadas.Keys;

        public static void Configurar(IEnumerable<ReglaCompatibilidad06AV> reglas)
        {
            var sistema = new Dictionary<string, ReglaCompatibilidad06AV>(StringComparer.OrdinalIgnoreCase);
            var propias = new List<ReglaCompatibilidad06AV>();
            foreach (ReglaCompatibilidad06AV r in reglas ?? Enumerable.Empty<ReglaCompatibilidad06AV>())
            {
                if (r == null) continue;
                if (r.Sistema) sistema[r.Codigo] = r;
                else propias.Add(r);
            }
            lock (Candado)
            {
                _sistema = sistema;
                _propias = propias;
                _configuradas = true;
            }
        }

        private static void Asegurar()
        {
            if (_configuradas) return;
            new CompatibilidadBLL06AV().Recargar();
        }

        public static bool Activa(string codigo)
        {
            Asegurar();
            return !_sistema.TryGetValue(codigo, out ReglaCompatibilidad06AV r) || r.Activa;
        }

        public static NivelCompatibilidad06AV NivelDe(string codigo)
        {
            Asegurar();
            if (_sistema.TryGetValue(codigo, out ReglaCompatibilidad06AV r))
                return r.Nivel == NivelReglaCompatibilidad06AV.Advertencia ? NivelCompatibilidad06AV.Advertencia
                                                                           : NivelCompatibilidad06AV.Incompatible;
            return Predeterminadas.TryGetValue(codigo, out Predeterminada p) ? p.Nivel : NivelCompatibilidad06AV.Incompatible;
        }

        public static decimal? ValorDe(string codigo)
        {
            Asegurar();
            if (_sistema.TryGetValue(codigo, out ReglaCompatibilidad06AV r) && r.Valor.HasValue) return r.Valor;
            return Predeterminadas.TryGetValue(codigo, out Predeterminada p) ? p.Valor : null;
        }

        public static decimal MargenFuente => 1m + (ValorDe("fuente_limite") ?? 30m) / 100m;

        private static void Agregar(List<ProblemaCompatibilidad06AV> r, string codigo, IEnumerable<Componente06AV> involucrados,
                                    params object[] argumentos)
        {
            if (!Activa(codigo)) return;
            r.Add(new ProblemaCompatibilidad06AV(NivelDe(codigo), "cmp_" + codigo, involucrados, argumentos));
        }

        public const string Amd = "AMD";
        public const string Intel = "Intel";

        public static readonly string[] Plataformas = { Amd, Intel };
        public static readonly string[] TiposMemoria = { "DDR4", "DDR5" };
        public static readonly string[] Formatos = { "MiniITX", "MicroATX", "ATX", "EATX" };

        public static string PlataformaDe(Componente06AV c)
        {
            if (c == null) return null;
            string p = Normalizar(c.Especificacion?.Plataforma);
            if (p == "AMD") return Amd;
            if (p == "INTEL") return Intel;
            string texto = ((c.Marca ?? "") + " " + (c.Modelo ?? "") + " " + (c.Descripcion ?? "")).ToUpperInvariant();
            if (c.Tipo == TipoComponente06AV.Procesador)
            {
                if (texto.Contains("AMD") || texto.Contains("RYZEN") || texto.Contains("ATHLON")) return Amd;
                if (texto.Contains("INTEL") || texto.Contains("CORE ") || texto.Contains("PENTIUM") || texto.Contains("CELERON")) return Intel;
            }
            return null;
        }

        public static bool UsaPlataforma(TipoComponente06AV tipo) =>
            tipo == TipoComponente06AV.Procesador || tipo == TipoComponente06AV.PlacaMadre;

        public static int ConsumoEstimado(IEnumerable<Componente06AV> piezas) =>
            (piezas ?? Enumerable.Empty<Componente06AV>())
                .Where(c => c != null && c.Tipo != TipoComponente06AV.Fuente)
                .Sum(c => Math.Max(0, c.Especificacion?.ConsumoW ?? 0));

        public static int FuenteRecomendada(int consumo)
        {
            if (consumo <= 0) return 0;
            int minimo = (int)Math.Ceiling(consumo * MargenFuente);
            return Math.Max(300, (int)(Math.Ceiling(minimo / 50m) * 50));
        }

        public static int MaximoModulosRam(Componente06AV placa, Componente06AV memoria)
        {
            int porDefecto = (int)(ValorDe("ram_slots") ?? 4);
            int maximo = Activa("ram_slots") ? placa?.Especificacion?.SlotsMemoria ?? porDefecto : ModulosRamSinLimite;
            int? tope = placa?.Especificacion?.MemoriaMaximaGb;
            int? modulo = memoria?.Especificacion?.CapacidadGb;
            if (Activa("ram_max") && tope.HasValue && modulo.HasValue && modulo.Value > 0)
                maximo = Math.Min(maximo, tope.Value / modulo.Value);
            return Math.Max(1, maximo);
        }

        public static ResultadoCompatibilidad06AV EvaluarCandidato(Componente06AV candidato, IEnumerable<Componente06AV> piezasPrevias,
                                                                  int cantidad = 1)
        {
            if (candidato == null) return new ResultadoCompatibilidad06AV(Enumerable.Empty<ProblemaCompatibilidad06AV>());
            var equipo = (piezasPrevias ?? Enumerable.Empty<Componente06AV>())
                .Where(c => c != null && !(c.Tipo == candidato.Tipo && !AdmiteVariosModelos(c.Tipo)))
                .ToList();
            for (int i = 0; i < Math.Max(1, cantidad); i++) equipo.Add(candidato);

            var problemas = Evaluar(equipo, false).Where(p => p.Involucra(candidato)).ToList();
            return new ResultadoCompatibilidad06AV(problemas);
        }

        public static ResultadoCompatibilidad06AV EvaluarEquipo(IEnumerable<Componente06AV> piezas, bool completo = true) =>
            new ResultadoCompatibilidad06AV(Evaluar((piezas ?? Enumerable.Empty<Componente06AV>()).Where(c => c != null).ToList(), completo));

        private static bool AdmiteVariosModelos(TipoComponente06AV tipo) =>
            tipo == TipoComponente06AV.Disco || tipo == TipoComponente06AV.Otro;

        private static List<ProblemaCompatibilidad06AV> Evaluar(List<Componente06AV> piezas, bool completo)
        {
            var r = new List<ProblemaCompatibilidad06AV>();
            Componente06AV cpu = Primero(piezas, TipoComponente06AV.Procesador);
            Componente06AV placa = Primero(piezas, TipoComponente06AV.PlacaMadre);
            Componente06AV fuente = Primero(piezas, TipoComponente06AV.Fuente);
            Componente06AV gabinete = Primero(piezas, TipoComponente06AV.Gabinete);
            Componente06AV cooler = Primero(piezas, TipoComponente06AV.Refrigeracion);
            List<Componente06AV> memorias = piezas.Where(c => c.Tipo == TipoComponente06AV.MemoriaRAM).ToList();
            bool hayVideo = piezas.Any(c => c.Tipo == TipoComponente06AV.PlacaDeVideo);

            ReglasProcesadorPlaca(r, cpu, placa);
            ReglasMemoria(r, memorias, placa);
            ReglasGabinete(r, placa, gabinete);
            ReglasRefrigeracion(r, cooler, cpu, placa);
            ReglasFuente(r, fuente, piezas);

            if (completo && cpu?.Especificacion?.GraficosIntegrados == false && !hayVideo)
                Agregar(r, "sin_video", new[] { cpu }, Nombre(cpu));

            ReglasPropias(r, piezas);
            return r;
        }

        private static void ReglasProcesadorPlaca(List<ProblemaCompatibilidad06AV> r, Componente06AV cpu, Componente06AV placa)
        {
            if (cpu == null || placa == null) return;
            string pCpu = PlataformaDe(cpu), pPlaca = PlataformaDe(placa);
            string sCpu = Socket(cpu), sPlaca = Socket(placa);

            if (pCpu != null && pPlaca != null && pCpu != pPlaca)
                Agregar(r, "plataforma", new[] { cpu, placa },
                                                     pCpu, pPlaca);
            else if (sCpu != null && sPlaca != null && sCpu != sPlaca)
                Agregar(r, "socket", new[] { cpu, placa },
                                                     sCpu, sPlaca);

            if (cpu.Especificacion?.Desbloqueado == true && placa.Especificacion?.PermiteOverclock == false)
                Agregar(r, "oc_cpu", new[] { cpu, placa },
                                                     Nombre(cpu), Nombre(placa));
        }

        private static void ReglasMemoria(List<ProblemaCompatibilidad06AV> r, List<Componente06AV> memorias, Componente06AV placa)
        {
            if (memorias.Count == 0) return;
            var modelos = memorias.GroupBy(m => m.Codigo).Select(g => g.First()).ToList();

            if (modelos.Count > 1)
                Agregar(r, "ram_mezcla", modelos);

            var tipos = modelos.Select(m => Normalizar(m.Especificacion?.TipoMemoria)).Where(t => t != null).Distinct().ToList();
            if (tipos.Count > 1)
                Agregar(r, "ram_tipos", modelos,
                                                     string.Join(" / ", tipos));

            if (placa == null) return;
            EspecificacionComponente06AV ep = placa.Especificacion;
            string tipoPlaca = Normalizar(ep?.TipoMemoria);

            foreach (Componente06AV m in modelos)
            {
                string tipo = Normalizar(m.Especificacion?.TipoMemoria);
                if (tipo != null && tipoPlaca != null && tipo != tipoPlaca)
                {
                    Agregar(r, "ddr", new[] { m, placa },
                                                         tipo, tipoPlaca);
                    continue;
                }
                int? mhz = m.Especificacion?.FrecuenciaMemoriaMhz, tope = ep?.FrecuenciaMemoriaMhz;
                if (mhz.HasValue && tope.HasValue && mhz.Value > tope.Value)
                    Agregar(r, "ram_oc", new[] { m, placa },
                                                         mhz.Value, tope.Value);
            }

            int? slots = ep?.SlotsMemoria;
            if (slots.HasValue && memorias.Count > slots.Value)
                Agregar(r, "ram_slots", modelos.Concat(new[] { placa }), memorias.Count, slots.Value);

            int? maximo = ep?.MemoriaMaximaGb;
            if (maximo.HasValue && memorias.All(m => m.Especificacion?.CapacidadGb != null))
            {
                int total = memorias.Sum(m => m.Especificacion.CapacidadGb.Value);
                if (total > maximo.Value)
                    Agregar(r, "ram_max", modelos.Concat(new[] { placa }), total, maximo.Value);
            }
        }

        private static void ReglasGabinete(List<ProblemaCompatibilidad06AV> r, Componente06AV placa, Componente06AV gabinete)
        {
            if (placa == null || gabinete == null) return;
            int fPlaca = RangoFormato(placa.Especificacion?.Formato), fGabinete = RangoFormato(gabinete.Especificacion?.Formato);
            if (fPlaca >= 0 && fGabinete >= 0 && fPlaca > fGabinete)
                Agregar(r, "formato", new[] { placa, gabinete },
                                                     NombreFormato(placa.Especificacion.Formato),
                                                     NombreFormato(gabinete.Especificacion.Formato));
        }

        private static void ReglasRefrigeracion(List<ProblemaCompatibilidad06AV> r, Componente06AV cooler, Componente06AV cpu,
                                                Componente06AV placa)
        {
            if (cooler == null) return;
            EspecificacionComponente06AV ec = cooler.Especificacion;
            string socket = Socket(cpu) ?? Socket(placa);
            Componente06AV contra = cpu ?? placa;

            List<string> soportados = (ec?.Socket ?? "")
                .Split(new[] { ',', ';', '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(Normalizar).Where(s => s != null).ToList();
            if (socket != null && soportados.Count > 0 && !soportados.Contains(socket))
                Agregar(r, "cooler_socket", new[] { cooler, contra },
                                                     socket);

            int? tdpCpu = cpu?.Especificacion?.ConsumoW, tdpCooler = ec?.TdpSoportadoW;
            if (tdpCpu.HasValue && tdpCooler.HasValue && tdpCooler.Value < tdpCpu.Value)
                Agregar(r, "cooler_tdp", new[] { cooler, cpu },
                                                     tdpCpu.Value, tdpCooler.Value);
        }

        private static void ReglasFuente(List<ProblemaCompatibilidad06AV> r, Componente06AV fuente, List<Componente06AV> piezas)
        {
            int? potencia = fuente?.Especificacion?.PotenciaW;
            if (!potencia.HasValue) return;
            int consumo = ConsumoEstimado(piezas);
            if (consumo <= 0) return;

            var involucrados = new[] { fuente }.Concat(piezas.Where(c => (c.Especificacion?.ConsumoW ?? 0) > 0));
            int recomendada = FuenteRecomendada(consumo);
            if (potencia.Value < consumo)
                Agregar(r, "fuente_insuficiente", involucrados,
                                                     potencia.Value, consumo, recomendada);
            else if (potencia.Value < consumo * MargenFuente)
                Agregar(r, "fuente_limite", involucrados,
                                                     potencia.Value, consumo, recomendada);
        }

        private static void ReglasPropias(List<ProblemaCompatibilidad06AV> r, List<Componente06AV> piezas)
        {
            Asegurar();
            foreach (ReglaCompatibilidad06AV regla in _propias)
            {
                if (!regla.Activa || !regla.TipoA.HasValue || !regla.TipoB.HasValue || !regla.AtributoA.HasValue ||
                    !regla.AtributoB.HasValue || !regla.Operador.HasValue) continue;

                NivelCompatibilidad06AV nivel = regla.Nivel == NivelReglaCompatibilidad06AV.Advertencia
                    ? NivelCompatibilidad06AV.Advertencia : NivelCompatibilidad06AV.Incompatible;
                var piezasA = Distintas(piezas, regla.TipoA.Value);
                var piezasB = Distintas(piezas, regla.TipoB.Value);

                foreach (Componente06AV a in piezasA)
                    foreach (Componente06AV b in piezasB)
                    {
                        if (a.Codigo == b.Codigo) continue;
                        object va = ValorAtributo(a, regla.AtributoA.Value), vb = ValorAtributo(b, regla.AtributoB.Value);
                        if (va == null || vb == null) continue;
                        if (Cumple(va, regla.Operador.Value, vb, regla.AtributoA.Value, regla.AtributoB.Value)) continue;
                        r.Add(new ProblemaCompatibilidad06AV(nivel, "cmp_propia", TextoPropia(regla, a, b, va, vb), new[] { a, b }));
                    }
            }
        }

        private static List<Componente06AV> Distintas(IEnumerable<Componente06AV> piezas, TipoComponente06AV tipo) =>
            piezas.Where(c => c.Tipo == tipo).GroupBy(c => c.Codigo).Select(g => g.First()).ToList();

        public static object ValorAtributo(Componente06AV c, AtributoCompatibilidad06AV atributo)
        {
            if (c == null) return null;
            if (atributo == AtributoCompatibilidad06AV.Marca) return string.IsNullOrWhiteSpace(c.Marca) ? null : c.Marca;
            if (atributo == AtributoCompatibilidad06AV.Plataforma) return PlataformaDe(c);
            EspecificacionComponente06AV e = c.Especificacion;
            if (e == null) return null;
            switch (atributo)
            {
                case AtributoCompatibilidad06AV.Socket: return string.IsNullOrWhiteSpace(e.Socket) ? null : e.Socket;
                case AtributoCompatibilidad06AV.TipoMemoria: return string.IsNullOrWhiteSpace(e.TipoMemoria) ? null : e.TipoMemoria;
                case AtributoCompatibilidad06AV.Formato: return string.IsNullOrWhiteSpace(e.Formato) ? null : e.Formato;
                case AtributoCompatibilidad06AV.FrecuenciaMemoriaMhz: return (object)e.FrecuenciaMemoriaMhz;
                case AtributoCompatibilidad06AV.CapacidadGb: return (object)e.CapacidadGb;
                case AtributoCompatibilidad06AV.SlotsMemoria: return (object)e.SlotsMemoria;
                case AtributoCompatibilidad06AV.MemoriaMaximaGb: return (object)e.MemoriaMaximaGb;
                case AtributoCompatibilidad06AV.ConsumoW: return (object)e.ConsumoW;
                case AtributoCompatibilidad06AV.PotenciaW: return (object)e.PotenciaW;
                case AtributoCompatibilidad06AV.TdpSoportadoW: return (object)e.TdpSoportadoW;
                case AtributoCompatibilidad06AV.GraficosIntegrados: return (object)e.GraficosIntegrados;
                case AtributoCompatibilidad06AV.Desbloqueado: return (object)e.Desbloqueado;
                case AtributoCompatibilidad06AV.PermiteOverclock: return (object)e.PermiteOverclock;
                default: return null;
            }
        }

        public static bool Cumple(object a, OperadorCompatibilidad06AV op, object b,
                                  AtributoCompatibilidad06AV atributoA, AtributoCompatibilidad06AV atributoB)
        {
            if (op == OperadorCompatibilidad06AV.Contiene)
            {
                string buscado = Normalizar(Convert.ToString(b));
                return buscado == null || Convert.ToString(a)
                    .Split(new[] { ',', ';', '/' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(Normalizar).Contains(buscado);
            }

            decimal? na = Numero(a, atributoA), nb = Numero(b, atributoB);
            if (op == OperadorCompatibilidad06AV.MenorOIgual || op == OperadorCompatibilidad06AV.MayorOIgual)
            {
                if (!na.HasValue || !nb.HasValue) return true;
                return op == OperadorCompatibilidad06AV.MenorOIgual ? na.Value <= nb.Value : na.Value >= nb.Value;
            }

            bool iguales = na.HasValue && nb.HasValue && !(a is string) ? na.Value == nb.Value
                         : string.Equals(Normalizar(Convert.ToString(a)), Normalizar(Convert.ToString(b)), StringComparison.Ordinal);
            return op == OperadorCompatibilidad06AV.Igual ? iguales : !iguales;
        }

        private static decimal? Numero(object v, AtributoCompatibilidad06AV atributo)
        {
            if (v == null) return null;
            if (atributo == AtributoCompatibilidad06AV.Formato)
            {
                int rango = RangoFormato(Convert.ToString(v));
                return rango < 0 ? (decimal?)null : rango;
            }
            if (v is int i) return i;
            if (v is decimal d) return d;
            if (v is bool) return null;
            return decimal.TryParse(Convert.ToString(v), System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture, out decimal x) ? x : (decimal?)null;
        }

        private static string TextoPropia(ReglaCompatibilidad06AV regla, Componente06AV a, Componente06AV b, object va, object vb)
        {
            string valorA = TextoValor(va), valorB = TextoValor(vb);
            if (!string.IsNullOrWhiteSpace(regla.Mensaje))
                return regla.Mensaje.Replace("{A}", Nombre(a)).Replace("{B}", Nombre(b))
                                    .Replace("{valorA}", valorA).Replace("{valorB}", valorB);
            return GestorIdioma06AV.Instancia.Obtener("cmp_propia", regla.Nombre, Nombre(a), valorA, Nombre(b), valorB);
        }

        private static string TextoValor(object v)
        {
            if (v is bool b) return GestorIdioma06AV.Instancia.Obtener(b ? "cmp_si" : "cmp_no");
            return Convert.ToString(v, System.Globalization.CultureInfo.CurrentCulture);
        }

        private static Componente06AV Primero(IEnumerable<Componente06AV> piezas, TipoComponente06AV tipo) =>
            piezas.FirstOrDefault(c => c.Tipo == tipo);

        private static string Socket(Componente06AV c)
        {
            if (c == null || c.Tipo == TipoComponente06AV.Refrigeracion) return null;
            return Normalizar(c.Especificacion?.Socket);
        }

        public static string Normalizar(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            return new string(valor.Where(ch => !char.IsWhiteSpace(ch) && ch != '-').ToArray()).ToUpperInvariant();
        }

        private static int RangoFormato(string formato)
        {
            string f = Normalizar(formato);
            if (f == null) return -1;
            if (f.Contains("EATX")) return 3;
            if (f.Contains("ITX")) return 0;
            if (f.Contains("MICRO") || f.Contains("MATX")) return 1;
            if (f.Contains("ATX")) return 2;
            return -1;
        }

        public static string NombreFormato(string formato)
        {
            switch (RangoFormato(formato))
            {
                case 0: return "Mini-ITX";
                case 1: return "Micro-ATX";
                case 2: return "ATX";
                case 3: return "E-ATX";
                default: return formato;
            }
        }

        private static string Nombre(Componente06AV c)
        {
            if (c == null) return "";
            string n = ((c.Marca ?? "") + " " + (c.Modelo ?? "")).Trim();
            return n.Length > 0 ? n : c.Descripcion;
        }
    }
}
