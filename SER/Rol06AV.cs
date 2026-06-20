using System;
using System.Collections.Generic;
using System.Linq;

namespace SER
{
    public class Rol06AV : IComponentePermiso06AV
    {
        public string Id { get; set; }
        public string Descripcion { get; set; }
        public string Codigo { get; set; }

        private readonly List<IComponentePermiso06AV> _hijos = new List<IComponentePermiso06AV>();
        public IReadOnlyList<IComponentePermiso06AV> Hijos => _hijos.AsReadOnly();

        public HashSet<Patente06AV> ObtenerPatentes()
        {
            var resultado = new HashSet<Patente06AV>(new PatenteIdComparador());
            foreach (var hijo in _hijos)
                resultado.UnionWith(hijo.ObtenerPatentes());
            return resultado;
        }

        /// <summary>
        /// Agrega una Patente o Familia al rol.
        /// Lanza InvalidOperationException si alguna patente ya está contenida.
        /// </summary>
        public void Agregar(IComponentePermiso06AV componente)
        {
            var existentes = ObtenerPatentes();
            var nuevas = componente.ObtenerPatentes();
            var duplicada = nuevas.FirstOrDefault(p => existentes.Contains(p, new PatenteIdComparador()));
            if (duplicada != null)
                throw new InvalidOperationException(
                    $"No se puede agregar: la patente '{duplicada.Descripcion}' (Id: {duplicada.Id}) ya está contenida en este rol.");
            _hijos.Add(componente);
        }

            /// <summary>
            /// Agrega múltiples componentes validando todos antes de persistir ninguno.
            /// Si alguno genera duplicado, no se agrega ninguno.
            /// </summary>
            public void AgregarRango(IEnumerable<IComponentePermiso06AV> componentes)
            {
                var acumuladas = ObtenerPatentes();
            foreach (var componente in componentes)
            {
                var duplicada = componente.ObtenerPatentes()
                    .FirstOrDefault(p => acumuladas.Contains(p, new PatenteIdComparador()));
                if (duplicada != null)
                    throw new InvalidOperationException(
                        $"No se puede agregar: la patente '{duplicada.Descripcion}' (Id: {duplicada.Id}) ya está contenida en este rol.");
                acumuladas.UnionWith(componente.ObtenerPatentes());
            }
            _hijos.AddRange(componentes);
        }

        public void Quitar(IComponentePermiso06AV componente)
        {
            _hijos.RemoveAll(h => h.Id == componente.Id && h.GetType() == componente.GetType());
        }
    }
}
