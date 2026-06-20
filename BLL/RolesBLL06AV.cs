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

        #region ABM

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
                _mpp.Modificar(rol);
            }
            catch (Exception) { throw; }
        }

        public void Eliminar(string id)
        {
            try { _mpp.Eliminar(id); }
            catch (Exception) { throw; }
        }

        #endregion

        #region Gestión de hijos

        /// <summary>
        /// Agrega una patente al rol, validando que no esté ya contenida.
        /// </summary>
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

        /// <summary>
        /// Agrega una familia al rol, validando que ninguna de sus patentes
        /// ya esté contenida en el rol.
        /// </summary>
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

        #endregion

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

        private string BuscarRutaPatenteEnHijos(IEnumerable<IComponentePermiso06AV> hijos, string idPatente, string ruta)
        {
            foreach (IComponentePermiso06AV hijo in hijos)
            {
                Patente06AV patente = hijo as Patente06AV;
                if (patente != null)
                {
                    if (string.Equals(patente.Id, idPatente, StringComparison.OrdinalIgnoreCase))
                        return $"{ruta} > Patente '{patente.Descripcion}'";
                    continue;
                }

                Familia06AV familia = hijo as Familia06AV;
                if (familia != null)
                {
                    string encontrada = BuscarRutaPatenteEnHijos(
                        familia.Hijos,
                        idPatente,
                        $"{ruta} > Familia '{familia.Descripcion}'");
                    if (encontrada != null)
                        return encontrada;
                }
            }

            return null;
        }
    }
}
