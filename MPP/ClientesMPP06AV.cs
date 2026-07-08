using BE;
using DAL;
using System.Collections.Generic;
using System.Data;

namespace MPP
{
    /// <summary>Mapea entre la entidad Cliente06AV y la capa de acceso a datos.</summary>
    public class ClientesMPP06AV
    {
        private readonly ClientesDAL06AV _dal = new ClientesDAL06AV();

        public List<Cliente06AV> ObtenerTodos()
        {
            DataTable tabla = _dal.ObtenerTodos();
            var lista = new List<Cliente06AV>();
            foreach (DataRow row in tabla.Rows)
                lista.Add(Mapear(row));
            return lista;
        }

        public Cliente06AV ObtenerPorDni(string dni)
        {
            DataTable tabla = _dal.ObtenerPorDni(dni);
            if (tabla.Rows.Count == 0) return null;
            return Mapear(tabla.Rows[0]);
        }

        public void Agregar(Cliente06AV cliente)
        {
            _dal.Agregar(cliente.Dni, cliente.Nombre, cliente.Apellido,
                         cliente.Telefono, cliente.Direccion);
        }

        public void Modificar(Cliente06AV cliente)
        {
            _dal.Modificar(cliente.Dni, cliente.Nombre, cliente.Apellido,
                           cliente.Telefono, cliente.Direccion);
        }

        public void Eliminar(string dni)
        {
            _dal.Eliminar(dni);
        }

        private Cliente06AV Mapear(DataRow row)
        {
            return new Cliente06AV
            {
                Dni       = row["Dni"].ToString(),
                Nombre    = row["Nombre"].ToString(),
                Apellido  = row["Apellido"].ToString(),
                Telefono  = row.Table.Columns.Contains("Telefono")  ? row["Telefono"].ToString()  : "",
                Direccion = row.Table.Columns.Contains("Direccion") ? row["Direccion"].ToString() : ""
            };
        }
    }
}
