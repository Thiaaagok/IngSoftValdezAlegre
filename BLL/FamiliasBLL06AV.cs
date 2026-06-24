using MPP;
using SER;
using SER.Generador;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// Reglas de negocio para Familias. Una familia es un nodo del árbol de permisos
    /// (ver IComponentePermiso06AV) que puede agrupar Patentes y otras Familias (subfamilias).
    /// </summary>
    public class FamiliasBLL06AV
    {
        private readonly FamiliaMPP06AV _mpp = new FamiliaMPP06AV();

        public List<Familia06AV> ObtenerTodos() => _mpp.ObtenerTodos();

        public Familia06AV ObtenerPorId(string id) => _mpp.ObtenerPorId(id);

        public void Agregar(Familia06AV familia)
        {
            if (string.IsNullOrWhiteSpace(familia.Id))
                familia.Id = new GeneradorID().GenerarId();
            if (string.IsNullOrWhiteSpace(familia.Descripcion))
                throw new ArgumentException("La familia debe tener una descripción.");

            ValidarDescripcionUnica(familia.Descripcion, idExcluir: null);

            _mpp.Agregar(familia);
        }

        public void Modificar(Familia06AV familia)
        {
            if (string.IsNullOrWhiteSpace(familia.Descripcion))
                throw new ArgumentException("La familia debe tener una descripción.");

            ValidarDescripcionUnica(familia.Descripcion, idExcluir: familia.Id);

            _mpp.Modificar(familia);
        }

        /// <summary>
        /// Elimina una familia solo si no tiene dependencias.
        /// Orden de validación:
        ///   1. Hijos propios (patentes o subfamilias directas) → bloquear
        ///   2. Contenida como subfamilia en otra familia → aviso con nombres
        ///   3. Asignada directamente a un rol → aviso con nombres
        /// Si pasa todo, borra sus relaciones y luego la entidad.
        /// </summary>
        public void Eliminar(string id)
        {
            Familia06AV familia = _mpp.ObtenerPorId(id);
            if (familia == null)
                throw new InvalidOperationException("Familia no encontrada.");

            // 1. Tiene hijos propios
            if (familia.Hijos.Any())
            {
                int patentes = familia.Hijos.Count(h => h is Patente06AV);
                int subfamilias = familia.Hijos.Count(h => h is Familia06AV);
                var partes = new List<string>();
                if (patentes > 0) partes.Add($"{patentes} patente{(patentes > 1 ? "s" : "")}");
                if (subfamilias > 0) partes.Add($"{subfamilias} subfamilia{(subfamilias > 1 ? "s" : "")}");
                throw new InvalidOperationException(
                    $"No se puede eliminar '{familia.Descripcion}' porque tiene {string.Join(" y ", partes)} asignadas. " +
                    $"Quitá sus hijos antes de eliminarla.");
            }

            // 2. Es subfamilia de otra familia
            var familiasPadre = _mpp.ObtenerTodos()
                .Where(f => !string.Equals(f.Id, id, StringComparison.OrdinalIgnoreCase)
                            && f.Hijos.Any(h => h is Familia06AV sf &&
                                                string.Equals(sf.Id, id, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (familiasPadre.Any())
            {
                string lista = string.Join(", ", familiasPadre.Select(f => $"'{f.Descripcion}'"));
                throw new InvalidOperationException(
                    $"No se puede eliminar '{familia.Descripcion}' porque es subfamilia de: {lista}. " +
                    $"Quitala de esas familias antes de eliminarla.");
            }

            // 3. Está asignada directamente a un rol
            RolesBLL06AV rolesBLL = new RolesBLL06AV();
            var rolesConFamilia = rolesBLL.ObtenerTodos()
                .Where(r => r.Hijos.Any(h => h is Familia06AV f &&
                                              string.Equals(f.Id, id, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (rolesConFamilia.Any())
            {
                string lista = string.Join(", ", rolesConFamilia.Select(r => $"'{r.Descripcion}'"));
                throw new InvalidOperationException(
                    $"No se puede eliminar '{familia.Descripcion}' porque está asignada a los roles: {lista}. " +
                    $"Quitala de esos roles antes de eliminarla.");
            }

            _mpp.Eliminar(id);
        }

        // ── Agregar / Quitar patentes y subfamilias ──────────────────────────

        /// <summary>
        /// Agrega una patente directa a la familia, validando que no quede duplicada
        /// en la propia familia, en sus familias ancestras, ni en roles que ya la contengan.
        /// </summary>
        public void AgregarPatente(string idFamilia, string idPatente)
        {
            Familia06AV familia = _mpp.ObtenerPorId(idFamilia);
            if (familia == null)
                throw new InvalidOperationException("Familia no encontrada.");

            PatentesBLL06AV patentesBLL = new PatentesBLL06AV();
            Patente06AV patente = patentesBLL.ObtenerPorId(idPatente);
            if (patente == null)
                throw new InvalidOperationException("Patente no encontrada.");

            string rutaDuplicada = BuscarRutaPatente(familia, idPatente);
            if (rutaDuplicada != null)
                throw new InvalidOperationException(
                    $"No se puede agregar la patente '{patente.Descripcion}' porque ya existe en: {rutaDuplicada}.");

            familia.Agregar(patente);
            ValidarPatentesEnAncestros(idFamilia, new[] { patente });
            ValidarPatentesEnRolesQueContienenFamilia(idFamilia, new[] { patente });

            _mpp.AgregarPatente(idFamilia, idPatente);
        }

        public void QuitarPatente(string idFamilia, string idPatente) => _mpp.QuitarPatente(idFamilia, idPatente);

        /// <summary>
        /// Agrega una subfamilia, validando que no se generen ciclos, que la subfamilia
        /// no esté ya asignada a un rol, y que ninguna de sus patentes quede duplicada
        /// en el destino.
        /// </summary>
        public void AgregarSubfamilia(string idPadre, string idHijo)
        {
            if (string.Equals(idPadre, idHijo, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Una familia no puede contenerse a sí misma.");

            Familia06AV padre = _mpp.ObtenerPorId(idPadre);
            if (padre == null)
                throw new InvalidOperationException("Familia padre no encontrada.");

            Familia06AV hijo = _mpp.ObtenerPorId(idHijo);
            if (hijo == null)
                throw new InvalidOperationException("Familia hijo no encontrada.");
            if (!hijo.ObtenerPatentes().Any())
                throw new InvalidOperationException("No se puede agregar una familia sin patentes efectivas.");

            if (ContieneFamilia(hijo, idPadre))
                throw new InvalidOperationException(
                    "No se puede agregar la subfamilia porque generaría un ciclo en la jerarquía.");

            // Verificar que la subfamilia no esté ya asignada directamente a un rol
            RolesBLL06AV rolesBLL = new RolesBLL06AV();
            var rolesConHijo = rolesBLL.ObtenerTodos()
                .Where(r => r.Hijos.Any(h => h is Familia06AV f &&
                                              string.Equals(f.Id, idHijo, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (rolesConHijo.Any())
            {
                string lista = string.Join(", ", rolesConHijo.Select(r => $"'{r.Descripcion}'"));
                throw new InvalidOperationException(
                    $"No se puede agregar '{hijo.Descripcion}' como subfamilia porque ya está asignada directamente a los roles: {lista}.");
            }

            foreach (Patente06AV patente in hijo.ObtenerPatentes())
            {
                string rutaDuplicada = BuscarRutaPatente(padre, patente.Id);
                if (rutaDuplicada != null)
                    throw new InvalidOperationException(
                        $"No se puede agregar la familia '{hijo.Descripcion}' porque la patente '{patente.Descripcion}' ya existe en: {rutaDuplicada}.");
            }

            padre.Agregar(hijo);
            ValidarPatentesEnAncestros(idPadre, hijo.ObtenerPatentes());
            ValidarPatentesEnRolesQueContienenFamilia(idPadre, hijo.ObtenerPatentes());

            _mpp.AgregarSubfamilia(idPadre, idHijo);
        }

        public void QuitarSubfamilia(string idPadre, string idHijo) => _mpp.QuitarSubfamilia(idPadre, idHijo);

        // ── Validaciones cruzadas ────────────────────────────────────────────

        private void ValidarDescripcionUnica(string descripcion, string idExcluir)
        {
            bool existe = _mpp.ObtenerTodos().Any(f =>
                string.Equals(f.Descripcion.Trim(), descripcion.Trim(), StringComparison.OrdinalIgnoreCase)
                && !string.Equals(f.Id, idExcluir, StringComparison.OrdinalIgnoreCase));

            if (existe)
                throw new InvalidOperationException(
                    GestorIdioma06AV.Instancia.Obtener("val_familia_descripcion_duplicada", descripcion));
        }

        /// <summary>
        /// Recorre todas las familias que contienen (directa o indirectamente) a <paramref name="idFamilia"/>
        /// y verifica que ninguna de las patentes nuevas ya exista en otra rama de esa familia ancestra.
        /// </summary>
        private void ValidarPatentesEnAncestros(string idFamilia, IEnumerable<Patente06AV> patentesNuevas)
        {
            foreach (var posibleAncestro in _mpp.ObtenerTodos())
            {
                if (string.Equals(posibleAncestro.Id, idFamilia, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!ContieneFamilia(posibleAncestro, idFamilia))
                    continue;

                foreach (var p in patentesNuevas)
                {
                    string ruta = BuscarRutaPatenteExcluyendoFamilia(
                        posibleAncestro, p.Id, idFamilia, posibleAncestro.Descripcion);
                    if (ruta != null)
                        throw new InvalidOperationException(
                            $"No se puede agregar la patente '{p.Descripcion}' porque ya existe en: {ruta}.");
                }
            }
        }

        /// <summary>
        /// Misma validación que <see cref="ValidarPatentesEnAncestros"/> pero recorriendo roles
        /// que contienen a la familia, en vez de otras familias.
        /// </summary>
        private void ValidarPatentesEnRolesQueContienenFamilia(string idFamilia, IEnumerable<Patente06AV> patentesNuevas)
        {
            RolesBLL06AV rolesBLL = new RolesBLL06AV();
            foreach (var rol in rolesBLL.ObtenerTodos())
            {
                if (!ContieneFamilia(rol, idFamilia))
                    continue;

                foreach (var p in patentesNuevas)
                {
                    string ruta = BuscarRutaPatenteExcluyendoFamilia(
                        rol, p.Id, idFamilia, rol.Descripcion);
                    if (ruta != null)
                        throw new InvalidOperationException(
                            $"No se puede agregar la patente '{p.Descripcion}' porque ya existe en: {ruta}.");
                }
            }
        }

        private bool ContieneFamilia(IComponentePermiso06AV componente, string idFamilia)
        {
            if (componente is Familia06AV familia)
            {
                if (string.Equals(familia.Id, idFamilia, StringComparison.OrdinalIgnoreCase))
                    return true;
                return familia.Hijos.Any(h => ContieneFamilia(h, idFamilia));
            }
            if (componente is Rol06AV rol)
                return rol.Hijos.Any(h => ContieneFamilia(h, idFamilia));
            return false;
        }

        private string BuscarRutaPatente(IComponentePermiso06AV componente, string idPatente)
        {
            return BuscarRutaPatenteRecursivo(componente, idPatente, componente.Descripcion);
        }

        private string BuscarRutaPatenteRecursivo(IComponentePermiso06AV componente, string idPatente, string rutaActual)
        {
            if (componente is Patente06AV patente)
                return patente.Id == idPatente ? rutaActual : null;

            IEnumerable<IComponentePermiso06AV> hijos = null;
            if (componente is Familia06AV f) hijos = f.Hijos;
            else if (componente is Rol06AV r) hijos = r.Hijos;
            if (hijos == null) return null;

            foreach (var hijo in hijos)
            {
                string ruta = BuscarRutaPatenteRecursivo(hijo, idPatente, $"{rutaActual} > {hijo.Descripcion}");
                if (ruta != null) return ruta;
            }
            return null;
        }

        private string BuscarRutaPatenteExcluyendoFamilia(
            IComponentePermiso06AV componente,
            string idPatente,
            string idFamiliaExcluida,
            string rutaActual)
        {
            if (componente is Familia06AV f &&
                string.Equals(f.Id, idFamiliaExcluida, StringComparison.OrdinalIgnoreCase))
                return null;

            if (componente is Patente06AV p)
                return string.Equals(p.Id, idPatente, StringComparison.OrdinalIgnoreCase)
                    ? rutaActual : null;

            IEnumerable<IComponentePermiso06AV> hijos =
                (componente is Familia06AV fam) ? fam.Hijos :
                (componente is Rol06AV rol) ? rol.Hijos : null;

            if (hijos == null) return null;

            foreach (var hijo in hijos)
            {
                string ruta = BuscarRutaPatenteExcluyendoFamilia(
                    hijo, idPatente, idFamiliaExcluida, $"{rutaActual} > {hijo.Descripcion}");
                if (ruta != null) return ruta;
            }
            return null;
        }
    }
}
