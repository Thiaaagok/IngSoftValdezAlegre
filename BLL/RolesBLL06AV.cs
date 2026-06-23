using MPP;
using SER;
using SER.Generador;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    public class RolesBLL06AV
    {
        private readonly RolesMPP06AV _mpp = new RolesMPP06AV();

        public List<Rol06AV> ObtenerTodos()
        {
            try { return _mpp.ObtenerTodos(); }
            catch (Exception) { throw; }
        }

        public Rol06AV ObtenerPorId(string id)
        {
            try { return _mpp.ObtenerPorId(id); }
            catch (Exception) { throw; }
        }

        public void Agregar(Rol06AV rol)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rol.Id))
                    throw new ArgumentException("El rol debe tener un Id.");
                if (string.IsNullOrWhiteSpace(rol.Descripcion))
                    throw new ArgumentException("El rol debe tener una descripción.");

                ValidarDescripcionUnica(rol.Descripcion, idExcluir: null);

                if (string.IsNullOrWhiteSpace(rol.Codigo))
                    rol.Codigo = GenerarCodigo(rol.Id);

                _mpp.Agregar(rol);
            }
            catch (Exception) { throw; }
        }

        public void Modificar(Rol06AV rol)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rol.Descripcion))
                    throw new ArgumentException("El rol debe tener una descripción.");

                ValidarDescripcionUnica(rol.Descripcion, idExcluir: rol.Id);

                _mpp.Modificar(rol);
            }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// Elimina un rol solo si no tiene hijos propios asignados.
        /// Los roles no pueden estar contenidos en otras estructuras,
        /// así que la única dependencia posible es que tenga patentes o familias propias.
        /// </summary>
        public void Eliminar(string id)
        {
            try
            {
                Rol06AV rol = _mpp.ObtenerPorId(id);
                if (rol == null)
                    throw new InvalidOperationException("Rol no encontrado.");

                if (rol.Hijos.Any())
                {
                    int patentes = rol.Hijos.Count(h => h is Patente06AV);
                    int familias = rol.Hijos.Count(h => h is Familia06AV);
                    var partes = new List<string>();
                    if (patentes > 0) partes.Add($"{patentes} patente{(patentes > 1 ? "s" : "")}");
                    if (familias > 0) partes.Add($"{familias} familia{(familias > 1 ? "s" : "")}");
                    throw new InvalidOperationException(
                        $"No se puede eliminar '{rol.Descripcion}' porque tiene {string.Join(" y ", partes)} asignadas. " +
                        $"Quitá sus hijos antes de eliminarlo.");
                }

                _mpp.Eliminar(id);
            }
            catch (Exception) { throw; }
        }

        // ── Gestión de hijos ─────────────────────────────────────────────────

        public void AgregarPatente(string idRol, string idPatente)
        {
            try
            {
                Rol06AV rol = _mpp.ObtenerPorId(idRol);
                if (rol == null)
                    throw new InvalidOperationException("Rol no encontrado.");

                PatentesBLL06AV patentesBLL = new PatentesBLL06AV();
                Patente06AV patente = patentesBLL.ObtenerPorId(idPatente);
                if (patente == null)
                    throw new InvalidOperationException("Patente no encontrada.");

                string rutaDuplicada = BuscarRutaPatente(rol, idPatente);
                if (rutaDuplicada != null)
                    throw new InvalidOperationException(
                        $"No se puede agregar la patente '{patente.Descripcion}' porque ya existe en: {rutaDuplicada}.");

                rol.Agregar(patente);
                _mpp.AgregarPatente(idRol, idPatente);
            }
            catch (Exception) { throw; }
        }

        public void QuitarPatente(string idRol, string idPatente)
        {
            try { _mpp.QuitarPatente(idRol, idPatente); }
            catch (Exception) { throw; }
        }

        public void AgregarFamilia(string idRol, string idFamilia)
        {
            try
            {
                Rol06AV rol = _mpp.ObtenerPorId(idRol);
                if (rol == null)
                    throw new InvalidOperationException("Rol no encontrado.");

                FamiliasBLL06AV familiasBLL = new FamiliasBLL06AV();
                Familia06AV familia = familiasBLL.ObtenerPorId(idFamilia);
                if (familia == null)
                    throw new InvalidOperationException("Familia no encontrada.");
                if (!familia.ObtenerPatentes().Any())
                    throw new InvalidOperationException("No se puede agregar una familia sin patentes efectivas.");

                // Verificar que la familia no sea ya subfamilia de otra familia en el sistema
                var todasFamilias = familiasBLL.ObtenerTodos();
                var familiasPadre = todasFamilias
                    .Where(f => f.Hijos.Any(h => h is Familia06AV sf &&
                                                  string.Equals(sf.Id, idFamilia, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (familiasPadre.Any())
                {
                    string lista = string.Join(", ", familiasPadre.Select(f => $"'{f.Descripcion}'"));
                    throw new InvalidOperationException(
                        $"No se puede agregar '{familia.Descripcion}' directamente al rol porque ya es subfamilia de: {lista}.");
                }

                foreach (Patente06AV patente in familia.ObtenerPatentes())
                {
                    string rutaDuplicada = BuscarRutaPatente(rol, patente.Id);
                    if (rutaDuplicada != null)
                        throw new InvalidOperationException(
                            $"No se puede agregar la familia '{familia.Descripcion}' porque la patente '{patente.Descripcion}' ya existe en: {rutaDuplicada}.");
                }

                rol.Agregar(familia);
                _mpp.AgregarFamilia(idRol, idFamilia);
            }
            catch (Exception) { throw; }
        }

        public void QuitarFamilia(string idRol, string idFamilia)
        {
            try { _mpp.QuitarFamilia(idRol, idFamilia); }
            catch (Exception) { throw; }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void ValidarDescripcionUnica(string descripcion, string idExcluir)
        {
            bool existe = _mpp.ObtenerTodos().Any(r =>
                string.Equals(r.Descripcion.Trim(), descripcion.Trim(), StringComparison.OrdinalIgnoreCase)
                && !string.Equals(r.Id, idExcluir, StringComparison.OrdinalIgnoreCase));

            if (existe)
                throw new InvalidOperationException(
                    GestorIdioma06AV.Instancia.Obtener("val_rol_descripcion_duplicada", descripcion));
        }

        private string GenerarCodigo(string id)
        {
            string limpio = (id ?? new GeneradorID().GenerarId()).Trim().ToUpperInvariant();
            return string.IsNullOrWhiteSpace(limpio)
                ? new GeneradorID().GenerarId().Substring(0, 8)
                : limpio.Substring(0, Math.Min(8, limpio.Length));
        }

        private string BuscarRutaPatente(Rol06AV rol, string idPatente)
        {
            if (rol == null) return null;
            return BuscarRutaPatenteEnHijos(rol.Hijos, idPatente, $"Rol '{rol.Descripcion}'");
        }

        private string BuscarRutaPatenteEnHijos(
            IEnumerable<IComponentePermiso06AV> hijos,
            string idPatente,
            string ruta)
        {
            foreach (var hijo in hijos)
            {
                if (hijo is Patente06AV p)
                {
                    if (string.Equals(p.Id, idPatente, StringComparison.OrdinalIgnoreCase))
                        return $"{ruta} > Patente '{p.Descripcion}'";
                    continue;
                }
                if (hijo is Familia06AV f)
                {
                    string encontrada = BuscarRutaPatenteEnHijos(
                        f.Hijos, idPatente, $"{ruta} > Familia '{f.Descripcion}'");
                    if (encontrada != null) return encontrada;
                }
            }
            return null;
        }
    }
}