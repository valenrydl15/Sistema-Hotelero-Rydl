using System;
using System.Collections.Generic;
using System.Linq;
using DAL_HRS_480;
using ENTIDADES_HRS_480;
using SEGURIDAD_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// CUN05 — Realizar Check-OUT («extend» de CUN01).
    /// Lo lanza CheckInBLL; registra en Bitácora y recalcula DV al terminar.
    /// </summary>
    public class CheckOutBLL
    {
        private readonly CheckInDAL _dal = new CheckInDAL();

        private int IdUsuarioActual =>
            SesionManager.Instancia.UsuarioActual?.IdUsuario ?? 0;

        public List<ReservaBE> ObtenerReservasActivas()
            => _dal.ObtenerReservasConCheckIn();

        public ResumenCheckOutBE CalcularResumenCheckOut(int idReserva)
        {
            var reserva = _dal.ObtenerReservaPorId(idReserva);
            var resumen = new ResumenCheckOutBE { IdReserva = idReserva, Reserva = reserva };
            if (reserva == null) return resumen;

            decimal monto = reserva.CantNoches * reserva.PrecioPorNoche;
            decimal impuestos = Math.Round(monto * CheckInBLL.PORCENTAJE_IMPUESTOS, 2);
            resumen.MontoHabitacion = monto;
            resumen.Impuestos = impuestos;
            resumen.MontoTotal = monto + impuestos;
            return resumen;
        }

        public ResultadoOperacionBE RealizarCheckOut(int idReserva)
        {
            var res = new ResultadoOperacionBE { IdReserva = idReserva };
            var integras = DigitoVerificador.Integras("CheckOuts", "Reservas", "Habitaciones");
            var tocadas = new HashSet<string>();

            try
            {
                if (!ControlAcceso.PuedeOperarRecepcion())
                {
                    res.Mensaje = "❌ No tenés permiso para operar en Recepción. Solo Recepcionista o Administrador.";
                    BitacoraEventos.Registrar(BitacoraEventos.CHECKOUT_RECH, 2,
                        $"Check-out rechazado: el usuario no tiene rol de Recepción. Reserva #{idReserva}.",
                        "FALLIDO");
                    return res;
                }

                var reserva = _dal.ObtenerReservaPorId(idReserva);
                string error = null;
                if (reserva == null) error = "Reserva no encontrada.";
                else if (reserva.Estado != "CheckIn") error = "La reserva no tiene un check-in activo.";

                if (error != null)
                {
                    res.Mensaje = "❌ " + error;
                    BitacoraEventos.Registrar(BitacoraEventos.CHECKOUT_RECH, 3,
                        $"Check-out rechazado. Reserva #{idReserva}: {error}", "FALLIDO");
                    return res;
                }

                var resumen = CalcularResumenCheckOut(idReserva);

                _dal.InsertarCheckOut(resumen, IdUsuarioActual);
                tocadas.Add("CheckOuts");
                _dal.ActualizarEstadoReserva(idReserva, "CheckOut");
                tocadas.Add("Reservas");
                _dal.ActualizarEstadoHabitacion(reserva.IdHabitacion, "Disponible");
                tocadas.Add("Habitaciones");

                BitacoraEventos.Registrar(BitacoraEventos.CHECKOUT, 3,
                    $"Check-out realizado. Reserva #{idReserva}, Hab. {reserva.NumeroHabitacion}, " +
                    $"DNI: {reserva.DNIHuesped}, {reserva.FechaIngreso:dd/MM/yyyy} → {reserva.FechaSalida:dd/MM/yyyy}, " +
                    $"Total: ${resumen.MontoTotal:N2}.", "EXITOSO");

                res.Exito = true;
                res.Mensaje =
                    "✔ Check-out realizado correctamente.\n\n" +
                    $"Habitación: {reserva.NumeroHabitacion}\n" +
                    "La habitación quedó disponible.";
                return res;
            }
            catch (Exception ex)
            {
                BitacoraEventos.Registrar(BitacoraEventos.ERROR_TECNICO, 1,
                    $"Error en check-out. Reserva #{idReserva}. {ex.Message}", "FALLIDO");
                res.Mensaje = $"❌ Error al realizar el check-out: {ex.Message}";
                return res;
            }
            finally
            {
                DigitoVerificador.RecalcularIntegras(integras, tocadas.ToArray());
            }
        }
    }
}
