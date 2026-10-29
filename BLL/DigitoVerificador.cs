using System;
using System.Collections.Generic;
using System.Linq;
using SERVICIOS_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// Fachada del Dígito Verificador para el módulo de Recepción.
    /// En el DSS: CheckInBLL → DigitoVerificador.Recalcular(tablas), sin retorno.
    /// Un fallo del DV se informa en bitácora pero no corta la operación.
    /// </summary>
    public static class DigitoVerificador
    {
        /// <summary>
        /// Control previo: true si las tablas están íntegras ANTES de operar.
        /// Si no lo están, no se recalcula después (así no se "blanquea" una alteración).
        /// </summary>
        public static bool Consistente(params string[] tablas)
            => DigitoVerificadorServicio.EstaConsistente(tablas);

        /// <summary>Recalcula solo si <paramref name="recalcular"/> es true (operación exitosa y tablas íntegras).</summary>
        public static void RecalcularSi(bool recalcular, params string[] tablas)
        {
            if (recalcular) Recalcular(tablas);
        }

        /// <summary>Tablas (de las indicadas) que están íntegras antes de operar.</summary>
        public static HashSet<string> Integras(params string[] tablas)
        {
            var ok = new HashSet<string>();
            foreach (var t in tablas.Distinct())
                if (DigitoVerificadorServicio.EstaConsistente(t)) ok.Add(t);
            return ok;
        }

        /// <summary>Recalcula solo las tablas que figuraban como íntegras en el control previo.</summary>
        public static void RecalcularIntegras(HashSet<string> integras, params string[] tablas)
            => Recalcular(tablas.Where(integras.Contains).ToArray());

        public static void Recalcular(params string[] tablas)
        {
            var lista = tablas?.Distinct().ToArray() ?? new string[0];
            if (lista.Length == 0) return;

            try
            {
                new DigitoVerificadorServicio().RecalcularTablas(lista);
            }
            catch (Exception ex)
            {
                BitacoraEventos.Registrar(BitacoraEventos.ERROR_TECNICO, 1,
                    $"No se pudo recalcular el DV de: {string.Join(", ", lista)}. {ex.Message}",
                    "FALLIDO");
            }
        }
    }
}
