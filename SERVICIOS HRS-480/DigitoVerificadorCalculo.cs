using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Globalization;

namespace SERVICIOS_HRS_480
{
    public class DigitoVerificadorCalculo
    {
        // ══════════════════════════════════════════════════════
        // DVH — Dígito Verificador HORIZONTAL
        // Recorre COLUMNAS (cada columna = un vector horizontal)
        //
        // Para cada columna:
        //   → hashea cada celda individualmente
        //   → concatena esos hashes
        //   → hashea la concatenación → hash de esa columna
        // Luego concatena todos los hashes de columna y hashea → DVH
        // ══════════════════════════════════════════════════════
        public string CalcularDVH(DataTable tabla)
        {
            if (tabla.Rows.Count == 0)
                return HashSHA256("TABLA_VACIA");

            var hashsPorColumna = new StringBuilder();

            foreach (DataColumn col in tabla.Columns)
            {
                // Paso 1: hashear cada celda de esta columna (recorre filas)
                var hashsCeldas = new StringBuilder();
                foreach (DataRow fila in tabla.Rows)
                {
                    string valorCelda = Valor(fila[col]);
                    hashsCeldas.Append(HashSHA256(valorCelda));
                }

                // Paso 2: hashear la concatenación de celdas de la columna
                string hashColumna = HashSHA256(hashsCeldas.ToString());
                hashsPorColumna.Append(hashColumna);
            }

            // Paso 3: hash final de todos los hashes de columna → DVH
            return HashSHA256(hashsPorColumna.ToString());
        }

        // ══════════════════════════════════════════════════════
        // DVV — Dígito Verificador VERTICAL
        // Recorre FILAS (cada fila = un vector vertical)
        //
        // Para cada fila:
        //   → hashea cada celda individualmente
        //   → concatena esos hashes
        //   → hashea la concatenación → hash de esa fila
        // Luego concatena todos los hashes de fila y hashea → DVV
        // ══════════════════════════════════════════════════════
        public string CalcularDVV(DataTable tabla)
        {
            if (tabla.Rows.Count == 0)
                return HashSHA256("TABLA_VACIA");

            var hashsPorFila = new StringBuilder();

            foreach (DataRow fila in tabla.Rows)
            {
                // Paso 1: hashear cada celda de esta fila (recorre columnas)
                var hashsCeldas = new StringBuilder();
                foreach (DataColumn col in tabla.Columns)
                {
                    string valorCelda = Valor(fila[col]);
                    hashsCeldas.Append(HashSHA256(valorCelda));
                }

                // Paso 2: hashear la concatenación de celdas de la fila
                string hashFila = HashSHA256(hashsCeldas.ToString());
                hashsPorFila.Append(hashFila);
            }

            // Paso 3: hash final de todos los hashes de fila → DVV
            return HashSHA256(hashsPorFila.ToString());
        }

        // ══════════════════════════════════════════════════════
        // Texto de una celda, SIEMPRE igual sin importar la configuración
        // regional de la PC (fechas y decimales). Si no, el mismo dato daría
        // un DV distinto en otra computadora y saltaría una falsa inconsistencia.
        // ══════════════════════════════════════════════════════
        private static string Valor(object valor)
        {
            if (valor == null || valor == DBNull.Value) return "";
            if (valor is DateTime fecha)
                return fecha.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            if (valor is IFormattable formateable)
                return formateable.ToString(null, CultureInfo.InvariantCulture);
            return valor.ToString() ?? "";
        }

        // ══════════════════════════════════════════════════════
        // SHA-256 interno — método privado de la BLL
        // ══════════════════════════════════════════════════════
        private static string HashSHA256(string texto)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(texto));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
