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
    }
}
