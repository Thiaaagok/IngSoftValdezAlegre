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
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("IngenieriaSoftware202606AV_06AV!");
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("IngSoft06AV2026!");

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
        public string EncriptarReversible(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return texto;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs, Encoding.UTF8))
                    {
                        sw.Write(texto);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public string DesencriptarReversible(string textoEncriptado)
        {
            if (string.IsNullOrWhiteSpace(textoEncriptado))
                return textoEncriptado;

            try
            {
                byte[] datos = Convert.FromBase64String(textoEncriptado);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Key;
                    aes.IV = IV;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var ms = new MemoryStream(datos))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs, Encoding.UTF8))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch
            {
                return textoEncriptado;
            }
        }
    }
}
