using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;


namespace SEGURIDAD_HRS_480
{
    public static class Encriptacion
    {
        public static string Encriptar(string contrasena)
        {
            if (string.IsNullOrEmpty(contrasena))
                throw new ArgumentException("La contraseña no puede estar vacía.");

           
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(contrasena));

          
            return Convert.ToHexString(bytes).ToLower();
        }

     
        public static bool Verificar(string contrasenaPlana, string hashAlmacenado)
        {
            if (string.IsNullOrEmpty(contrasenaPlana) || string.IsNullOrEmpty(hashAlmacenado))
                return false;

            string hashIngresado = Encriptar(contrasenaPlana);
            return hashIngresado == hashAlmacenado;
        }
    }
}

