using System;
using System.Collections.Generic;
using DAL_HRS_480;
using SEGURIDAD_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// Fachada de Bitácora de eventos del módulo 2 (Recepción).
    /// En el DSS: CheckInBLL → BitacoraEventos.Registrar(...), sin retorno.
    /// Resuelve el IdTipoEvento por NOMBRE (deben coincidir con 01_SQL_Bitacora_y_DV.sql).
    /// Un fallo de bitácora NUNCA interrumpe la operación de negocio.
    /// </summary>
    public static class BitacoraEventos
    {
        public const int ID_MOD = 2;

        public const string DISPONIBILIDAD = "Consulta de disponibilidad";
        public const string ALTA_HUESPED   = "Alta de huésped";
        public const string RESERVA        = "Reserva creada";
        public const string PAGO_OK        = "Pago autorizado";
        public const string PAGO_RECHAZADO = "Pago rechazado";
        public const string RESERVA_CANCEL = "Reserva cancelada";
        public const string FACTURA        = "Factura emitida";
        public const string CHECKIN        = "Check-in realizado";
        public const string CHECKIN_RECH   = "Check-in rechazado";
        public const string CHECKOUT       = "Check-out realizado";
        public const string CHECKOUT_RECH  = "Check-out rechazado";
        public const string ERROR_TECNICO  = "Error técnico check-in/out";
        public const string REPORTE_FACTURA = "Reporte de factura exportado";

        private static readonly object _lock = new object();
        private static readonly Bitacora _bitacora = new Bitacora();
        private static Dictionary<string, int> _ids = new Dictionary<string, int>();

        private static int? ObtenerId(string nombre)
        {
            lock (_lock)
            {
                if (_ids.TryGetValue(nombre, out int id)) return id;

                var nuevo = new Dictionary<string, int>();
                foreach (var t in _bitacora.ObtenerTiposEvento(ID_MOD))
                    nuevo[t.Nombre] = t.IdTipoEvento;
                _ids = nuevo;

                return _ids.TryGetValue(nombre, out id) ? id : (int?)null;
            }
        }

        public static void Registrar(string evento, int criticidad, string detalle, string resultado)
        {
            try
            {
                int? idUsuario = SesionManager.Instancia.UsuarioActual?.IdUsuario;
                _bitacora.Registrar(idUsuario, ID_MOD, ObtenerId(evento),
                                    criticidad, detalle, resultado);
            }
            catch
            {
                // La bitácora no debe bloquear la operación de negocio
            }
        }
    }
}
