using System.Collections.Generic;

namespace SER
{
    public class Patente06AV : IComponentePermiso
    {
        public string Id { get; set; }
        public string Descripcion { get; set; }

        public HashSet<Patente06AV> ObtenerPatentes()
        {
            return new HashSet<Patente06AV>(new PatenteIdComparador()) { this };
        }
    }

    public class PatenteIdComparador : IEqualityComparer<Patente06AV>
    {
        public bool Equals(Patente06AV x, Patente06AV y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.Id == y.Id;
        }

        public int GetHashCode(Patente06AV obj)
        {
            return obj?.Id?.GetHashCode() ?? 0;
        }
    }
}
