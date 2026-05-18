using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SER.Encriptador
{
    public class EncriptacionSER06AV
    {
        public string Encriptar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                throw new ArgumentException("El texto no puede estar vacío.", nameof(texto));

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(texto);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        public bool ComprararHash(string texto, string hashGuardado)
        {
            if (string.IsNullOrWhiteSpace(texto))
                throw new ArgumentException("El texto no puede estar vacío.", texto);

            if (string.IsNullOrWhiteSpace(hashGuardado))
                throw new ArgumentException("El hash no puede estar vacío.", hashGuardado);

            string hashNuevo = Encriptar(texto);
            return hashNuevo == hashGuardado;
        }
    }
}
