using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SERVICIOS_HRS_480
{
    public class BancoServicio
    {       
        public bool AutorizarPago(
                string dni,
                string numeroTarjeta,
                string codigoSeguridad,
                decimal monto,
                out string mensaje)
        {
                mensaje = "";

                if (string.IsNullOrWhiteSpace(dni))
                {
                    mensaje = "DNI no informado.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(numeroTarjeta))
                {
                    mensaje = "Número de tarjeta no informado.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(codigoSeguridad))
                {
                    mensaje = "Código de seguridad no informado.";
                    return false;
                }

                if (monto <= 0)
                {
                    mensaje = "El monto debe ser mayor a cero.";
                    return false;
                }

                // Simulación de comunicación con el banco.
                // En el proyecto real sería reemplazado por la API bancaria.

                string numeros = new string(
                    numeroTarjeta.Where(char.IsDigit).ToArray());

                string codigo = new string(
                    codigoSeguridad.Where(char.IsDigit).ToArray());

                if (numeros.Length < 4)
                {
                    mensaje = "Número de tarjeta inválido.";
                    return false;
                }

                if (codigo.Length < 3)
                {
                    mensaje = "Código de seguridad inválido.";
                    return false;
                }

                mensaje = "Operación autorizada por el banco.";
                return true;
        }

        // ══════════════════════════════════════════════════════════════════
        // PN2 · CUN09 Realizar compra de insumos: pago al proveedor por transferencia.
        // Simulación de la conexión con el banco (en producción sería la API del banco).
        // ══════════════════════════════════════════════════════════════════
        public const decimal LIMITE_POR_TRANSFERENCIA = 50_000_000m;

        public class ResultadoTransferencia
        {
            public bool Aprobada { get; set; }
            public string NroOperacion { get; set; } = "";
            public string Mensaje { get; set; } = "";
        }

        public ResultadoTransferencia RealizarTransferencia(string cuitDestino, string cbuDestino, decimal monto, string concepto)
        {
            string cbu = SoloDigitos(cbuDestino);
            string cuit = SoloDigitos(cuitDestino);

            if (!ValidarCBU(cbu))
                return new ResultadoTransferencia { Mensaje = "Transferencia rechazada: el CBU de destino no es válido." };
            if (!ValidarCUIT(cuit))
                return new ResultadoTransferencia { Mensaje = "Transferencia rechazada: el CUIT del destinatario no es válido." };
            if (monto <= 0)
                return new ResultadoTransferencia { Mensaje = "Transferencia rechazada: el monto debe ser mayor a cero." };
            if (monto > LIMITE_POR_TRANSFERENCIA)
                return new ResultadoTransferencia
                {
                    Mensaje = $"Transferencia rechazada: el monto supera el límite por operación del banco ($ {LIMITE_POR_TRANSFERENCIA:N2})."
                };

            // Número de comprobante que devuelve el banco
            string nro = "TRF-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + Math.Abs((cbu + monto + concepto).GetHashCode() % 10000).ToString("D4");
            return new ResultadoTransferencia
            {
                Aprobada = true,
                NroOperacion = nro,
                Mensaje = $"Transferencia aprobada por el banco. Comprobante {nro}."
            };
        }

        public static string SoloDigitos(string? s) => new string((s ?? "").Where(char.IsDigit).ToArray());

        /// <summary>Solo números, espacios, guiones y puntos (sin letras ni otros símbolos).</summary>
        private static bool SoloNumerosYSeparadores(string? s)
            => !string.IsNullOrWhiteSpace(s) && s.All(c => char.IsDigit(c) || c == ' ' || c == '-' || c == '.');

        /// <summary>CBU: 22 dígitos numéricos (se aceptan espacios o guiones entre grupos).</summary>
        public static bool ValidarCBU(string? cbuTexto)
            => SoloNumerosYSeparadores(cbuTexto) && SoloDigitos(cbuTexto).Length == 22;

        /// <summary>CUIT/CUIL: 11 dígitos numéricos (con o sin guiones, por ejemplo 30-71234567-1).</summary>
        public static bool ValidarCUIT(string? cuitTexto)
            => SoloNumerosYSeparadores(cuitTexto) && SoloDigitos(cuitTexto).Length == 11;

        /// <summary>Nombre del banco según los 3 primeros dígitos del CBU (si es uno conocido).</summary>
        public static string BancoDelCBU(string? cbuTexto)
        {
            string cbu = SoloDigitos(cbuTexto);
            if (cbu.Length < 3) return "";
            return cbu.Substring(0, 3) switch
            {
                "007" => "Banco de Galicia",
                "011" => "Banco de la Nación Argentina",
                "014" => "Banco de la Provincia de Buenos Aires",
                "015" => "ICBC",
                "017" => "BBVA Argentina",
                "027" => "Banco Supervielle",
                "029" => "Banco Ciudad",
                "034" => "Banco Patagonia",
                "044" => "Banco Hipotecario",
                "072" => "Banco Santander",
                "150" => "HSBC",
                "191" => "Banco Credicoop",
                "285" => "Banco Macro",
                _ => ""
            };
        }
    }
}
