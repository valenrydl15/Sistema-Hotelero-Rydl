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

        /// <summary>
        /// CUN11 Pagar orden de compra: el sistema le envía al banco número de orden, banco, CBU, titular y monto.
        /// El banco aprueba (devuelve el número de operación) o rechaza.
        /// </summary>
        public ResultadoTransferencia Transferir(string nroOrden, string banco, string cbu, string titular, decimal monto)
        {
            if (string.IsNullOrWhiteSpace(banco))
                return new ResultadoTransferencia { Mensaje = "Pago rechazado por el banco: falta el banco de destino." };
            if (!ValidarCBU(cbu))
                return new ResultadoTransferencia { Mensaje = "Pago rechazado por el banco: el CBU de destino no es válido." };
            if (string.IsNullOrWhiteSpace(titular))
                return new ResultadoTransferencia { Mensaje = "Pago rechazado por el banco: falta el titular de la cuenta." };
            if (monto <= 0)
                return new ResultadoTransferencia { Mensaje = "Pago rechazado por el banco: el monto debe ser mayor a cero." };
            if (monto > LIMITE_POR_TRANSFERENCIA)
                return new ResultadoTransferencia
                {
                    Mensaje = $"Pago rechazado por el banco: el monto supera el límite por operación ($ {LIMITE_POR_TRANSFERENCIA:N2})."
                };

            string nro = "TRF-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" +
                         Math.Abs((SoloDigitos(cbu) + monto + nroOrden).GetHashCode() % 10000).ToString("D4");
            return new ResultadoTransferencia
            {
                Aprobada = true,
                NroOperacion = nro,
                Mensaje = $"Pago aprobado por el banco. Operación {nro}."
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
