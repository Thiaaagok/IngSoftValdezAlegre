using System;
using System.Collections.Generic;
using System.Linq;

namespace SER
{
    public class Familia06AV : IComponentePermiso
    {
        public string Id { get; set; }
        public string Descripcion { get; set; }

        private readonly List<IComponentePermiso> _hijos = new List<IComponentePermiso>();
        public IReadOnlyList<IComponentePermiso> Hijos => _hijos.AsReadOnly();

        public HashSet<Patente06AV> ObtenerPatentes()
        {
            var resultado = new HashSet<Patente06AV>(new PatenteIdComparador());
            foreach (var hijo in _hijos)
                resultado.UnionWith(hijo.ObtenerPatentes());
            return resultado;
        }

        /// <summary>
        /// Agrega una Patente o Familia hija.
        /// Lanza InvalidOperationException si alguna patente del componente ya está contenida.
        /// </summary>
        public void Agregar(IComponentePermiso componente)
        {
            var existentes = ObtenerPatentes();
            var nuevas = componente.ObtenerPatentes();
            var duplicada = nuevas.FirstOrDefault(p => existentes.Contains(p, new PatenteIdComparador()));
            if (duplicada != null)
                throw new InvalidOperationException(
                    $"No se puede agregar: la patente '{duplicada.Descripcion}' (Id: {duplicada.Id}) ya está contenida en esta familia.");
            _hijos.Add(componente);
        }

        /// <summary>
        /// Agrega múltiples componentes validando todos antes de persistir ninguno.
        /// Si alguno genera duplicado, no se agrega ninguno.
        /// </summary>
        public void AgregarRango(IEnumerable<IComponentePermiso> componentes)
        {
            var acumuladas = ObtenerPatentes();
            foreach (var componente in componentes)
            {
                var duplicada = componente.ObtenerPatentes()
                    .FirstOrDefault(p => acumuladas.Contains(p, new PatenteIdComparador()));
                if (duplicada != null)
                    throw new InvalidOperationException(
                        $"No se puede agregar: la patente '{duplicada.Descripcion}' (Id: {duplicada.Id}) ya está contenida en esta familia.");
                acumuladas.UnionWith(componente.ObtenerPatentes());
            }
            _hijos.AddRange(componentes);
        }

        public void Quitar(IComponentePermiso componente)
        {
            _hijos.RemoveAll(h => h.Id == componente.Id && h.GetType() == componente.GetType());
        }
    }
}
