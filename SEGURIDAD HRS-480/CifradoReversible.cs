using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SEGURIDAD_HRS_480
{
    /// <summary>
    /// Encriptación REVERSIBLE (simétrica) para datos sensibles de los clientes, como el número de tarjeta.
    /// A diferencia de <see cref="Encriptacion"/> (hash SHA-256 de contraseñas, que no se puede revertir),
    /// acá el dato se cifra con AES-256 y se puede volver a leer con la misma clave.
    ///
    ///  · Algoritmo: AES-256 en modo CBC con relleno PKCS7. Cada valor usa un vector de inicialización (IV)
    ///    aleatorio, así dos tarjetas iguales no quedan iguales en la base.
    ///  · Formato guardado: "AES1:" + Base64(IV de 16 bytes + texto cifrado).
    ///  · Clave: 32 bytes aleatorios guardados FUERA de la base, en %ProgramData%\HotelRydl\clave_datos.key.
    ///    Quien tenga solo la base (o un backup) no puede descifrar los datos.
    ///    Si se lleva la base a otro equipo, hay que copiar también ese archivo.
    /// </summary>
    public static class CifradoReversible
    {
        private const string PREFIJO = "AES1:";
        private static readonly object _lock = new object();
        private static byte[]? _clave;

        /// <summary>Carpeta y archivo de la clave (se puede cambiar para pruebas).</summary>
        public static string RutaClave { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "HotelRydl", "clave_datos.key");

        private static byte[] Clave()
        {
            lock (_lock)
            {
                if (_clave != null) return _clave;
                if (File.Exists(RutaClave))
                {
                    _clave = Convert.FromBase64String(File.ReadAllText(RutaClave).Trim());
                    if (_clave.Length != 32) throw new CryptographicException("La clave de cifrado de datos no es válida (" + RutaClave + ").");
                }
                else
                {
                    _clave = new byte[32];
                    using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(_clave);
                    Directory.CreateDirectory(Path.GetDirectoryName(RutaClave)!);
                    File.WriteAllText(RutaClave, Convert.ToBase64String(_clave));
                }
                return _clave;
            }
        }

        /// <summary>Cifra un texto. Devuelve "" si el texto está vacío.</summary>
        public static string Encriptar(string? textoPlano)
        {
            if (string.IsNullOrEmpty(textoPlano)) return "";
            using var aes = Aes.Create();
            aes.Key = Clave();
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            using var enc = aes.CreateEncryptor();
            byte[] datos = Encoding.UTF8.GetBytes(textoPlano);
            byte[] cifrado = enc.TransformFinalBlock(datos, 0, datos.Length);
            return PREFIJO + Convert.ToBase64String(aes.IV.Concat(cifrado).ToArray());
        }

        /// <summary>Descifra un valor generado por <see cref="Encriptar"/>.</summary>
        public static string Desencriptar(string? textoCifrado)
        {
            if (string.IsNullOrEmpty(textoCifrado)) return "";
            if (!textoCifrado.StartsWith(PREFIJO)) throw new CryptographicException("El dato no tiene el formato de cifrado esperado.");
            byte[] todo = Convert.FromBase64String(textoCifrado.Substring(PREFIJO.Length));
            using var aes = Aes.Create();
            aes.Key = Clave();
            aes.IV = todo.Take(16).ToArray();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            using var dec = aes.CreateDecryptor();
            byte[] plano = dec.TransformFinalBlock(todo, 16, todo.Length - 16);
            return Encoding.UTF8.GetString(plano);
        }

        /// <summary>Deja ver solo los últimos 4 dígitos: "**** **** **** 1234".</summary>
        public static string EnmascararTarjeta(string? tarjeta)
        {
            string nums = new string((tarjeta ?? "").Where(char.IsDigit).ToArray());
            if (nums.Length < 4) return "";
            return "**** **** **** " + nums.Substring(nums.Length - 4);
        }

        /// <summary>Formatea en grupos de 4: "4509 9535 6623 3704".</summary>
        public static string FormatearTarjeta(string? tarjeta)
        {
            string nums = new string((tarjeta ?? "").Where(char.IsDigit).ToArray());
            var sb = new StringBuilder();
            for (int i = 0; i < nums.Length; i++)
            {
                if (i > 0 && i % 4 == 0) sb.Append(' ');
                sb.Append(nums[i]);
            }
            return sb.ToString();
        }
    }
}
