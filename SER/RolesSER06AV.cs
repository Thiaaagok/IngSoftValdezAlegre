using BE;
using MPP;
using SER.Excepciones;
using System;
using System.Collections.Generic;

namespace SER
{
    public class RolesSER06AV
    {
        #region Obtener
        public List<Rol06AV> ObtenerTodos()
        {
            try
            {
                RolesMPP06AV MPP = new RolesMPP06AV();
                return MPP.ObtenerTodos();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Rol06AV ObtenerPorId(string id)
        {

            BitacoraSER06AV bitacora = new BitacoraSER06AV();

            try
            {
                RolesMPP06AV MPP = new RolesMPP06AV();
                var rol = MPP.ObtenerPorId(id);

                return rol;
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion
    }
}