using System.Collections.Generic;

namespace SER
{
    public interface IComponentePermiso06AV
    {
        string Id { get; set; }
        string Descripcion { get; set; }
        HashSet<Patente06AV> ObtenerPatentes();
    }
}
