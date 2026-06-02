using MPP;
using SER;
using System;
using System.Collections.Generic;

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
                    throw new ArgumentException("La familia debe tener un Id.");
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

                // Dispara InvalidOperationException si hay duplicado
                familia.Agregar(patente);

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
                Familia06AV padre = _mpp.ObtenerPorId(idPadre);
                if (padre == null)
                    throw new InvalidOperationException("Familia padre no encontrada.");

                Familia06AV hijo = _mpp.ObtenerPorId(idHijo);
                if (hijo == null)
                    throw new InvalidOperationException("Familia hijo no encontrada.");

                // Dispara InvalidOperationException si hay patentes duplicadas
                padre.Agregar(hijo);

                _mpp.AgregarSubfamilia(idPadre, idHijo);
            }
            catch (Exception) { throw; }
        }

        public void QuitarSubfamilia(string idPadre, string idHijo)
        {
            try { _mpp.QuitarSubfamilia(idPadre, idHijo); }
            catch (Exception) { throw; }
        }
    }
}
