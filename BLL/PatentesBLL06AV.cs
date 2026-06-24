using MPP;
using SER;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// Patentes son las hojas del árbol de permisos: el permiso atómico que finalmente
    /// se chequea contra <see cref="UsuarioSesion06AV.TienePermiso"/>. Se asignan a
    /// Familias o Roles, pero tienen su propio ABM en esta pantalla.
    /// </summary>
    public class PatentesBLL06AV
    {
        private readonly PatenteMPP06AV _mpp = new PatenteMPP06AV();

        public List<Patente06AV> ObtenerTodos() => _mpp.ObtenerTodos();

        public Patente06AV ObtenerPorId(string id) => _mpp.ObtenerPorId(id);

        public void Agregar(Patente06AV patente)
        {
            if (string.IsNullOrWhiteSpace(patente.Id))
                throw new ArgumentException("La patente debe tener un Id.");
            if (string.IsNullOrWhiteSpace(patente.Descripcion))
                throw new ArgumentException("La patente debe tener una descripción.");

            if (_mpp.ObtenerPorId(patente.Id) != null)
                throw new InvalidOperationException(
                    GestorIdioma06AV.Instancia.Obtener("val_patente_id_duplicado", patente.Id));

            ValidarDescripcionUnica(patente.Descripcion, idExcluir: null);

            _mpp.Agregar(patente);
        }

        public void Modificar(Patente06AV patente)
        {
            if (string.IsNullOrWhiteSpace(patente.Descripcion))
                throw new ArgumentException("La patente debe tener una descripción.");

            ValidarDescripcionUnica(patente.Descripcion, idExcluir: patente.Id);

            _mpp.Modificar(patente);
        }

        /// <summary>
        /// Elimina una patente solo si no está siendo usada en ninguna Familia
        /// ni asignada directamente a ningún Rol.
        /// </summary>
        public void Eliminar(string id)
        {
            Patente06AV patente = _mpp.ObtenerPorId(id);
            if (patente == null)
                throw new InvalidOperationException("Patente no encontrada.");

            FamiliasBLL06AV familiasBLL = new FamiliasBLL06AV();
            var familiasConPatente = familiasBLL.ObtenerTodos()
                .Where(f => ContienePatenteDirecta(f.Hijos, id))
                .ToList();

            if (familiasConPatente.Any())
            {
                string lista = string.Join(", ", familiasConPatente.Select(f => $"'{f.Descripcion}'"));
                throw new InvalidOperationException(
                    $"No se puede eliminar '{patente.Descripcion}' porque está asignada a las familias: {lista}. " +
                    $"Quitala de esas familias antes de eliminarla.");
            }

            RolesBLL06AV rolesBLL = new RolesBLL06AV();
            var rolesConPatente = rolesBLL.ObtenerTodos()
                .Where(r => ContienePatenteDirecta(r.Hijos, id))
                .ToList();

            if (rolesConPatente.Any())
            {
                string lista = string.Join(", ", rolesConPatente.Select(r => $"'{r.Descripcion}'"));
                throw new InvalidOperationException(
                    $"No se puede eliminar '{patente.Descripcion}' porque está asignada a los roles: {lista}. " +
                    $"Quitala de esos roles antes de eliminarla.");
            }

            _mpp.Eliminar(id);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private bool ContienePatenteDirecta(IEnumerable<IComponentePermiso06AV> hijos, string idPatente)
            => hijos.Any(h => h is Patente06AV p &&
                              string.Equals(p.Id, idPatente, StringComparison.OrdinalIgnoreCase));

        private void ValidarDescripcionUnica(string descripcion, string idExcluir)
        {
            bool existe = _mpp.ObtenerTodos().Any(p =>
                string.Equals(p.Descripcion.Trim(), descripcion.Trim(), StringComparison.OrdinalIgnoreCase)
                && !string.Equals(p.Id, idExcluir, StringComparison.OrdinalIgnoreCase));

            if (existe)
                throw new InvalidOperationException(
                    GestorIdioma06AV.Instancia.Obtener("val_patente_descripcion_duplicada", descripcion));
        }
    }
}
