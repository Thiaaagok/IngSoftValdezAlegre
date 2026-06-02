using MPP;
using SER;
using System;
using System.Collections.Generic;

namespace BLL
{
    public class PatentesBLL06AV
    {
        private readonly PatenteMPP06AV MPP = new PatenteMPP06AV();

        public List<Patente06AV> ObtenerTodos()
        {
            try { return MPP.ObtenerTodos(); }
            catch (Exception) { throw; }
        }

        public Patente06AV ObtenerPorId(string id)
        {
            try { return MPP.ObtenerPorId(id); }
            catch (Exception) { throw; }
        }

        public void Agregar(Patente06AV patente)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(patente.Id))
                    throw new ArgumentException("La patente debe tener un Id.");
                if (string.IsNullOrWhiteSpace(patente.Descripcion))
                    throw new ArgumentException("La patente debe tener una descripción.");
                MPP.Agregar(patente);
            }
            catch (Exception) { throw; }
        }

        public void Modificar(Patente06AV patente)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(patente.Descripcion))
                    throw new ArgumentException("La patente debe tener una descripción.");
                MPP.Modificar(patente);
            }
            catch (Exception) { throw; }
        }

        public void Eliminar(string id)
        {
            try { MPP.Eliminar(id); }
            catch (Exception) { throw; }
        }
    }
}
