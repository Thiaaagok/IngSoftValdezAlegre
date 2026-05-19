using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SER.Generador
{
    public class GeneradorID
    {
        public string GenerarId()
        {
            StringBuilder sb = new StringBuilder();

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                byte[] buffer = new byte[1];

                while (sb.Length < 30)
                {
                    rng.GetBytes(buffer);
                    int numero = buffer[0] % 10;
                    sb.Append(numero);
                }
            }

            return sb.ToString();
        }
    }
}
