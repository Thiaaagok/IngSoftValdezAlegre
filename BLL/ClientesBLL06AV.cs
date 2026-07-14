using BE;
using BLL.Excepciones;
using MPP;
using SER;
using System;
using System.Collections.Generic;

namespace BLL
{
    public class ClientesBLL06AV
    {
        private readonly ClientesMPP06AV _mpp = new ClientesMPP06AV();

        public List<Cliente06AV> ObtenerTodos()
        {
            try { return _mpp.ObtenerTodos(); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener los clientes.", ex); }
        }

        public Cliente06AV ObtenerPorDni(string dni)
        {
            ValidarDni(dni);
            try { return _mpp.ObtenerPorDni(dni); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo obtener el cliente.", ex); }
        }

        public void Crear(Cliente06AV cliente)
        {
            Validar(cliente);

            if (_mpp.ObtenerPorDni(cliente.Dni) != null)
                throw new DuplicadoException06AV($"Ya existe un cliente con el DNI {cliente.Dni}.");

            try { _mpp.Agregar(cliente); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo crear el cliente.", ex); }

            AuditoriaPcFactory06AV.Alta($"Cliente: {cliente.Dni}", ModuloBitacora.Clientes);
        }

        public void Modificar(Cliente06AV cliente)
        {
            Validar(cliente);

            if (_mpp.ObtenerPorDni(cliente.Dni) == null)
                throw new NoEncontradoException06AV($"No existe un cliente con el DNI {cliente.Dni}.");

            try { _mpp.Modificar(cliente); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo modificar el cliente.", ex); }

            AuditoriaPcFactory06AV.Modificacion($"Cliente: {cliente.Dni}", ModuloBitacora.Clientes);
        }

        public void Eliminar(string dni)
        {
            ValidarDni(dni);

            if (_mpp.ObtenerPorDni(dni) == null)
                throw new NoEncontradoException06AV($"No existe un cliente con el DNI {dni}.");

            try { _mpp.Eliminar(dni); }
            catch (PcFactoryException06AV) { throw; }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo eliminar el cliente.", ex); }

            AuditoriaPcFactory06AV.Baja($"Cliente: {dni}", ModuloBitacora.Clientes);
        }

        // ── Validaciones ─────────────────────────────────────────────
        private void Validar(Cliente06AV cliente)
        {
            if (cliente == null)
                throw new ValidacionException06AV("cliente", "El cliente no puede ser nulo.");

            ValidarDni(cliente.Dni);

            if (string.IsNullOrWhiteSpace(cliente.Nombre))
                throw new ValidacionException06AV("Nombre", "El nombre es obligatorio.");

            if (string.IsNullOrWhiteSpace(cliente.Apellido))
                throw new ValidacionException06AV("Apellido", "El apellido es obligatorio.");
        }

        private void ValidarDni(string dni)
        {
            if (string.IsNullOrWhiteSpace(dni))
                throw new ValidacionException06AV("Dni", "El DNI es obligatorio.");

            foreach (char c in dni.Trim())
                if (!char.IsDigit(c))
                    throw new ValidacionException06AV("Dni", "El DNI debe contener solo números.");
        }
    }
}
