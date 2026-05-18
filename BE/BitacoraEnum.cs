using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BE
{
    public enum CategoriaBitacora
    {
        Login = 1,
        LoginFallido = 2,
        Logout = 3,

        Alta = 10,
        Modificacion = 11,
        Baja = 12,

        Error = 90,
        ErrorCritico = 99
    }

    public enum CriticidadBitacora
    {
        Baja = 1,
        Media = 2,
        Alta = 3,
        Critica = 4
    }

    public enum ModuloBitacora
    {
        General = 1,

        Autenticacion = 10,
        Usuarios = 11,

        Clientes = 20,
        Proveedores = 21,
        Computadoras = 22,
        Ventas = 23,
        Compras = 24,

        Reportes = 31,
    }


}
