using MPP;
using SER;
using SER.Generador;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    public class FamiliasBLL06AV
    {
        private readonly FamiliaMPP06AV _mpp = new FamiliaMPP06AV();

        public List<Familia06AV> ObtenerTodos()
        {
            try { return _mpp.ObtenerTodos(); }
            catch (Exception) { throw; }
        }

        public Familia06AV ObtenerPorId(string id)
        {
            try { return _mpp.ObtenerPorId(id); }
            catch (Exception) { throw; }
        }

        public void Agregar(Familia06AV familia)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(familia.Id))
                    familia.Id = new GeneradorID().GenerarId();
                if (string.IsNullOrWhiteSpace(familia.Descripcion))
                    throw new ArgumentException("La familia debe tener una descripción.");
                _mpp.Agregar(familia);
            }
            catch (Exception) { throw; }
        }

        public void Modificar(Familia06AV familia)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(familia.Descripcion))
                    throw new ArgumentException("La familia debe tener una descripción.");
                _mpp.Modificar(familia);
            }
            catch (Exception) { throw; }
        }

        public void Eliminar(string id)
        {
            try { _mpp.Eliminar(id); }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// Agrega una patente a una familia, validando que no esté ya contenida
        /// (directamente o dentro de una subfamilia).
        /// </summary>
        public void AgregarPatente(string idFamilia, string idPatente)
        {
            try
            {
                Familia06AV familia = _mpp.ObtenerPorId(idFamilia);
                if (familia == null)
                    throw new InvalidOperationException("Familia no encontrada.");

                PatentesBLL06AV patentesBLL = new PatentesBLL06AV();
                Patente06AV patente = patentesBLL.ObtenerPorId(idPatente);
                if (patente == null)
                    throw new InvalidOperationException("Patente no encontrada.");

                string rutaDuplicada =  BuscarRutaPatente(familia, idPatente);
                if (rutaDuplicada != null)
                    throw new InvalidOperationException(
                        $"No se puede agregar la patente '{patente.Descripcion}' porque ya existe en: {rutaDuplicada}.");

                familia.Agregar(patente);
                ValidarPatentesEnAncestros(idFamilia, new[] { patente });
                ValidarPatentesEnRolesQueContienenFamilia(idFamilia, new[] { patente });

                _mpp.AgregarPatente(idFamilia, idPatente);
            }
            catch (Exception) { throw; }
        }

        public void QuitarPatente(string idFamilia, string idPatente)
        {
            try { _mpp.QuitarPatente(idFamilia, idPatente); }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// Agrega una subfamilia a una familia, validando que ninguna patente
        /// de la subfamilia ya esté contenida en la familia destino.
        /// </summary>
        public void AgregarSubfamilia(string idPadre, string idHijo)
        {
            try
            {
                if (string.Equals(idPadre, idHijo, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Una familia no puede contenerse a si misma.");

                Familia06AV padre = _mpp.ObtenerPorId(idPadre);
                if (padre == null)
                    throw new InvalidOperationException("Familia padre no encontrada.");

                Familia06AV hijo = _mpp.ObtenerPorId(idHijo);
                if (hijo == null)
                    throw new InvalidOperationException("Familia hijo no encontrada.");
                if (!hijo.ObtenerPatentes().Any())
                    throw new InvalidOperationException("No se puede agregar una familia sin patentes efectivas.");

                if (ContieneFamilia(hijo, idPadre))
                    throw new InvalidOperationException("No se puede agregar la subfamilia porque generaria un ciclo en la jerarquia.");

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
            catch (Exception) { throw; }
        }

        public void QuitarSubfamilia(string idPadre, string idHijo)
        {
            try { _mpp.QuitarSubfamilia(idPadre, idHijo); }
            catch (Exception) { throw; }
        }

        private void ValidarPatentesEnAncestros(string idFamilia, IEnumerable<Patente06AV> patentesNuevas)
        {
            foreach (var posibleAncestro in _mpp.ObtenerTodos())
            {
                if (string.Equals(posibleAncestro.Id, idFamilia, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!ContieneFamilia(posibleAncestro, idFamilia))
                    continue;

                var duplicada = patentesNuevas.FirstOrDefault(p => BuscarRutaPatente(posibleAncestro, p.Id) != null);

                if (duplicada != null)
                {
                    string ruta = BuscarRutaPatente(posibleAncestro, duplicada.Id);
                    throw new InvalidOperationException(
                        $"No se puede agregar la patente '{duplicada.Descripcion}' porque ya existe en: {ruta}.");
                }
            }
        }

        private void ValidarPatentesEnRolesQueContienenFamilia(string idFamilia, IEnumerable<Patente06AV> patentesNuevas)
        {
            RolesBLL06AV rolesBLL = new RolesBLL06AV();
            foreach (var rol in rolesBLL.ObtenerTodos())
            {
                if (!ContieneFamilia(rol, idFamilia))
                    continue;

                var duplicada = patentesNuevas.FirstOrDefault(p => BuscarRutaPatente(rol, p.Id) != null);

                if (duplicada != null)
                {
                    string ruta = BuscarRutaPatente(rol, duplicada.Id);
                    throw new InvalidOperationException(
                        $"No se puede agregar la patente '{duplicada.Descripcion}' porque ya existe en: {ruta}.");
                }
            }
        }

        private bool ContieneFamilia(IComponentePermiso06AV componente, string idFamilia)
        {
            Familia06AV familia = componente as Familia06AV;
            if (familia != null)
            {
                if (string.Equals(familia.Id, idFamilia, StringComparison.OrdinalIgnoreCase))
                    return true;

                return familia.Hijos.Any(h => ContieneFamilia(h, idFamilia));
            }

            Rol06AV rol = componente as Rol06AV;
            if (rol != null)
                return rol.Hijos.Any(h => ContieneFamilia(h, idFamilia));

            return false;
        }

        private string BuscarRutaPatente(IComponentePermiso06AV componente, string idPatente)
        {
            return BuscarRutaPatenteRecursivo(
                componente,
                idPatente,
                componente.Descripcion);
        }

        private string BuscarRutaPatenteRecursivo(
    IComponentePermiso06AV componente,
    string idPatente,
    string rutaActual)
        {
            if (componente is Patente06AV patente)
            {
                return patente.Id == idPatente
                    ? rutaActual
                    : null;
            }

            IEnumerable<IComponentePermiso06AV> hijos = null;

            if (componente is Familia06AV familia)
                hijos = familia.Hijos;

            else if (componente is Rol06AV rol)
                hijos = rol.Hijos;

            if (hijos == null)
                return null;

            foreach (var hijo in hijos)
            {
                string ruta = BuscarRutaPatenteRecursivo(
                    hijo,
                    idPatente,
                    $"{rutaActual} > {hijo.Descripcion}");

                if (ruta != null)
                    return ruta;
            }

            return null;
        }
    }
}
