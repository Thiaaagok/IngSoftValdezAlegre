using MPP;
using SER;
using System.Collections.Generic;

namespace BLL
{
    /// <summary>
    /// Patentes son las hojas del árbol de permisos: el permiso atómico que finalmente
    /// se chequea contra <see cref="UsuarioSesion06AV.TienePermiso"/>. No tienen ABM propio
    /// desde esta pantalla: se cargan desde la base y se asignan a Familias o Roles.
    /// </summary>
    public class PatentesBLL06AV
    {
        private readonly PatenteMPP06AV _mpp = new PatenteMPP06AV();

        public List<Patente06AV> ObtenerTodos() => _mpp.ObtenerTodos();

        public Patente06AV ObtenerPorId(string id) => _mpp.ObtenerPorId(id);
    }
}
