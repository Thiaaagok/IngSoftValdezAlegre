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
    }
}
