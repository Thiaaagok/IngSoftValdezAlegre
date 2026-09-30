using System;
using System.Collections.Generic;
using System.Linq;

namespace SER
{
    public abstract class ComponentePermisoCompuesto06AV : IComponentePermiso06AV
    {
        public string Id { get; set; }
        public string Descripcion { get; set; }

        protected readonly List<IComponentePermiso06AV> _hijos = new List<IComponentePermiso06AV>();
        public IReadOnlyList<IComponentePermiso06AV> Hijos => _hijos.AsReadOnly();

        public HashSet<Patente06AV> ObtenerPatentes()
        {
            var resultado = new HashSet<Patente06AV>(new PatenteIdComparador());
            foreach (var hijo in _hijos)
                resultado.UnionWith(hijo.ObtenerPatentes());
            return resultado;
        }

        public virtual void Agregar(IComponentePermiso06AV componente)
        {
            var existentes = ObtenerPatentes();
            var nuevas = componente.ObtenerPatentes();
            var duplicada = nuevas.FirstOrDefault(p => existentes.Contains(p, new PatenteIdComparador()));
            if (duplicada != null)
                throw new InvalidOperationException(
                    $"No se puede agregar: la patente '{duplicada.Descripcion}' (Id: {duplicada.Id}) ya está contenida en {NombreContenedor()}.");
            _hijos.Add(componente);
        }

        /// <summary>
        /// Agrega múltiples componentes validando todos antes de persistir ninguno.
        /// Si alguno genera duplicado, no se agrega ninguno (operación atómica).
        /// </summary>
        public virtual void AgregarRango(IEnumerable<IComponentePermiso06AV> componentes)
        {
            var acumuladas = ObtenerPatentes();
            var lista = componentes.ToList();
            foreach (var componente in lista)
            {
                var duplicada = componente.ObtenerPatentes()
                    .FirstOrDefault(p => acumuladas.Contains(p, new PatenteIdComparador()));
                if (duplicada != null)
                    throw new InvalidOperationException(
                        $"No se puede agregar: la patente '{duplicada.Descripcion}' (Id: {duplicada.Id}) ya está contenida en {NombreContenedor()}.");
                acumuladas.UnionWith(componente.ObtenerPatentes());
            }
            _hijos.AddRange(lista);
        }

        public virtual void Quitar(IComponentePermiso06AV componente)
        {
            _hijos.RemoveAll(h => h.Id == componente.Id && h.GetType() == componente.GetType());
        }

        protected abstract string NombreContenedor();
    }
}
