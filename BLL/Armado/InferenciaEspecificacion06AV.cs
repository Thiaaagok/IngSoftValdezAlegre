using BE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BLL.Armado
{
    public static class InferenciaEspecificacion06AV
    {
        public static void Completar(IEnumerable<Componente06AV> componentes)
        {
            if (componentes == null) return;
            foreach (Componente06AV c in componentes)
                if (c != null && c.Especificacion == null)
                {
                    EspecificacionComponente06AV e = Sugerir(c);
                    e.Estimada = true;
                    c.Especificacion = e;
                }
        }

        public static EspecificacionComponente06AV Sugerir(Componente06AV c)
        {
            var e = new EspecificacionComponente06AV();
            if (c == null) return e;
            string t = (" " + (c.Marca ?? "") + " " + (c.Modelo ?? "") + " " + (c.Descripcion ?? "") + " ").ToUpperInvariant();

            switch (c.Tipo)
            {
                case TipoComponente06AV.Procesador: Procesador(t, e); break;
                case TipoComponente06AV.PlacaMadre: PlacaMadre(t, e); break;
                case TipoComponente06AV.MemoriaRAM: Memoria(t, e); break;
                case TipoComponente06AV.Disco: e.ConsumoW = t.Contains("HDD") ? 8 : 6; break;
                case TipoComponente06AV.PlacaDeVideo: e.ConsumoW = Vatios(t) ?? ConsumoVideo(t); break;
                case TipoComponente06AV.Fuente: e.PotenciaW = Vatios(t); break;
                case TipoComponente06AV.Gabinete: e.Formato = Formato(t) ?? "ATX"; break;
                case TipoComponente06AV.Refrigeracion: Refrigeracion(t, e); break;
                default: e.ConsumoW = 5; break;
            }
            return e;
        }

        private static void Procesador(string t, EspecificacionComponente06AV e)
        {
            bool amd = t.Contains("AMD") || t.Contains("RYZEN") || t.Contains("ATHLON");
            bool intel = !amd && (t.Contains("INTEL") || t.Contains("CORE") || t.Contains("PENTIUM") || t.Contains("CELERON"));
            e.Plataforma = amd ? ReglasCompatibilidad06AV.Amd : intel ? ReglasCompatibilidad06AV.Intel : null;
            e.Socket = SocketExplicito(t);

            if (amd)
            {
                Match m = Regex.Match(t, @"RYZEN\s*(?:\d\s+)?(\d)(\d{3})\s*(X3D|XT|X|GE|G|F)?\b");
                int serie = m.Success ? int.Parse(m.Groups[1].Value) : 0;
                string sufijo = m.Success ? m.Groups[3].Value : "";
                if (e.Socket == null)
                    e.Socket = t.Contains("ATHLON") ? "AM4" : serie >= 7 ? "AM5" : serie > 0 ? "AM4" : null;
                e.GraficosIntegrados = t.Contains("ATHLON") || sufijo.StartsWith("G") ||
                                       (serie >= 7 && sufijo != "F");
                e.Desbloqueado = true;
                e.ConsumoW = Vatios(t) ?? (sufijo.StartsWith("X") ? (serie >= 7 ? 120 : 105) : 65);
            }
            else if (intel)
            {
                Match m = Regex.Match(t, @"I[3579]\s*-?\s*(\d{2})(\d{2,3})\s*(KF|KS|K|F|T)?\b");
                int generacion = m.Success ? int.Parse(m.Groups[1].Value) : 0;
                string sufijo = m.Success ? m.Groups[3].Value : "";
                if (e.Socket == null)
                    e.Socket = t.Contains("ULTRA") ? "LGA1851"
                             : generacion >= 12 && generacion <= 14 ? "LGA1700"
                             : generacion == 10 || generacion == 11 ? "LGA1200" : null;
                e.GraficosIntegrados = !sufijo.EndsWith("F") && !Regex.IsMatch(t, @"\d{3,5}F\b");
                e.Desbloqueado = sufijo.StartsWith("K") || Regex.IsMatch(t, @"\d{3,5}KF?\b");
                e.ConsumoW = Vatios(t) ?? (e.Desbloqueado == true ? 125 : 65);
            }
            else
            {
                e.ConsumoW = Vatios(t) ?? 65;
            }
        }

        private static readonly Dictionary<string, string[]> Chipsets = new Dictionary<string, string[]>
        {
            { "AM5",     new[] { "A620", "B650", "X670", "B850", "X870", "B840" } },
            { "AM4",     new[] { "A320", "B350", "X370", "B450", "X470", "A520", "B550", "X570" } },
            { "LGA1700", new[] { "H610", "B660", "H670", "Z690", "B760", "H770", "Z790" } },
            { "LGA1851", new[] { "H810", "B860", "Z890" } },
            { "LGA1200", new[] { "H410", "B460", "H470", "Z490", "H510", "B560", "H570", "Z590" } }
        };

        private static void PlacaMadre(string t, EspecificacionComponente06AV e)
        {
            string chipset = null;
            e.Socket = SocketExplicito(t);
            foreach (var par in Chipsets)
                foreach (string ch in par.Value)
                    if (t.Contains(ch))
                    {
                        chipset = ch;
                        if (e.Socket == null) e.Socket = par.Key;
                    }

            e.Plataforma = e.Socket == null ? (t.Contains("AMD") ? ReglasCompatibilidad06AV.Amd : t.Contains("INTEL") ? ReglasCompatibilidad06AV.Intel : null)
                         : e.Socket.StartsWith("AM") ? ReglasCompatibilidad06AV.Amd : ReglasCompatibilidad06AV.Intel;

            if (t.Contains("DDR5")) e.TipoMemoria = "DDR5";
            else if (t.Contains("DDR4") || Regex.IsMatch(t, @"\bD4\b|-D4\b")) e.TipoMemoria = "DDR4";
            else if (e.Socket == "AM5" || e.Socket == "LGA1851") e.TipoMemoria = "DDR5";
            else if (e.Socket == "LGA1700" && chipset != null && chipset.StartsWith("Z")) e.TipoMemoria = "DDR5";
            else if (e.Socket != null) e.TipoMemoria = "DDR4";

            e.FrecuenciaMemoriaMhz = e.TipoMemoria == "DDR5" ? (e.Plataforma == ReglasCompatibilidad06AV.Amd ? 5200 : 5600)
                                   : e.TipoMemoria == "DDR4" ? 3200 : (int?)null;

            e.Formato = Formato(t) ?? (chipset != null && Regex.IsMatch(t, chipset + @"M\b|" + chipset + "M-") ? "MicroATX" : "ATX");
            bool basica = chipset != null && (chipset.StartsWith("H6") || chipset.StartsWith("A") || chipset.StartsWith("H4") ||
                                              chipset.StartsWith("H5") || chipset.StartsWith("H8"));
            e.SlotsMemoria = e.Formato == "MiniITX" || (e.Formato == "MicroATX" && basica) ? 2 : 4;
            e.MemoriaMaximaGb = e.SlotsMemoria * (e.TipoMemoria == "DDR5" ? 48 : 32);
            e.PermiteOverclock = chipset == null ? (bool?)null
                               : e.Plataforma == ReglasCompatibilidad06AV.Amd ? !chipset.StartsWith("A")
                               : chipset.StartsWith("Z");
            e.ConsumoW = 50;
        }

        private static void Memoria(string t, EspecificacionComponente06AV e)
        {
            e.TipoMemoria = t.Contains("DDR5") ? "DDR5" : t.Contains("DDR4") ? "DDR4" : t.Contains("DDR3") ? "DDR3" : null;
            Match mhz = Regex.Match(t, @"\b(2[1-9]\d{2}|[3-8]\d{3})\s*(?:MHZ|MT/S)?\b");
            e.FrecuenciaMemoriaMhz = mhz.Success ? int.Parse(mhz.Groups[1].Value)
                                   : e.TipoMemoria == "DDR5" ? 5200 : e.TipoMemoria == "DDR4" ? 3200 : (int?)null;
            Match kit = Regex.Match(t, @"(\d)\s*X\s*(\d{1,3})\s*GB");
            Match gb = Regex.Match(t, @"(\d{1,3})\s*GB");
            e.CapacidadGb = kit.Success ? int.Parse(kit.Groups[2].Value) : gb.Success ? int.Parse(gb.Groups[1].Value) : (int?)null;
            e.ConsumoW = 5;
        }

        private static void Refrigeracion(string t, EspecificacionComponente06AV e)
        {
            e.Socket = "AM4,AM5,LGA1700,LGA1851,LGA1200";
            bool liquida = t.Contains("LIQU") || t.Contains("AIO") || t.Contains("KRAKEN");
            if (liquida) e.TdpSoportadoW = t.Contains("360") ? 300 : 250;
            else if (t.Contains("STOCK") || t.Contains("WRAITH STEALTH") || t.Contains("LAMINAR")) e.TdpSoportadoW = 65;
            else e.TdpSoportadoW = 150;
            e.ConsumoW = liquida ? 8 : 4;
        }

        private static string SocketExplicito(string t)
        {
            if (Regex.IsMatch(t, @"\bAM5\b")) return "AM5";
            if (Regex.IsMatch(t, @"\bAM4\b")) return "AM4";
            Match lga = Regex.Match(t, @"(?:LGA|SOCKET)\s*(1851|1700|1200|1151)\b");
            return lga.Success ? "LGA" + lga.Groups[1].Value : null;
        }

        private static string Formato(string t)
        {
            if (t.Contains("E-ATX") || t.Contains("EATX")) return "EATX";
            if (t.Contains("ITX")) return "MiniITX";
            if (t.Contains("MICRO") || t.Contains("MATX") || t.Contains("M-ATX")) return "MicroATX";
            if (t.Contains("ATX")) return "ATX";
            return null;
        }

        private static int? Vatios(string t)
        {
            Match m = Regex.Match(t, @"\b(\d{2,4})\s*W\b");
            return m.Success ? int.Parse(m.Groups[1].Value) : (int?)null;
        }

        private static readonly KeyValuePair<string, int>[] ConsumosVideo =
        {
            new KeyValuePair<string, int>("5090", 575), new KeyValuePair<string, int>("5080", 360),
            new KeyValuePair<string, int>("5070 TI", 300), new KeyValuePair<string, int>("5070", 250),
            new KeyValuePair<string, int>("5060 TI", 180), new KeyValuePair<string, int>("5060", 145),
            new KeyValuePair<string, int>("4090", 450), new KeyValuePair<string, int>("4080", 320),
            new KeyValuePair<string, int>("4070 TI", 285), new KeyValuePair<string, int>("4070 SUPER", 220),
            new KeyValuePair<string, int>("4070", 200), new KeyValuePair<string, int>("4060 TI", 160),
            new KeyValuePair<string, int>("4060", 115), new KeyValuePair<string, int>("3090", 350),
            new KeyValuePair<string, int>("3080", 320), new KeyValuePair<string, int>("3070", 220),
            new KeyValuePair<string, int>("3060 TI", 200), new KeyValuePair<string, int>("3060", 170),
            new KeyValuePair<string, int>("3050", 130), new KeyValuePair<string, int>("1660", 125),
            new KeyValuePair<string, int>("1650", 75), new KeyValuePair<string, int>("7900", 300),
            new KeyValuePair<string, int>("7800", 263), new KeyValuePair<string, int>("7700", 245),
            new KeyValuePair<string, int>("7600", 165), new KeyValuePair<string, int>("6700", 230),
            new KeyValuePair<string, int>("6600", 132), new KeyValuePair<string, int>("6500", 107),
            new KeyValuePair<string, int>("9070", 304), new KeyValuePair<string, int>("9060", 160)
        };

        private static int ConsumoVideo(string t)
        {
            foreach (var par in ConsumosVideo)
                if (t.Contains(par.Key)) return par.Value;
            return 200;
        }
    }
}
