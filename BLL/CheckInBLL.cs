using System;
using System.Collections.Generic;
using System.Linq;
using DAL_HRS_480;
using ENTIDADES_HRS_480;
using SEGURIDAD_HRS_480;
using SERVICIOS_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// CUN01 — Registrar Check-IN.
    /// Igual que en el DSS: la BLL es quien lanza los casos de uso incluidos
    ///   «include» CUN02 SeleccionarHabitacion
    ///   «include» CUN03 RegistrarDatosCliente
    ///   «include» CUN04 RealizarCobro
    ///   «extend»  CUN05 RealizarCheckOut  (delegado en CheckOutBLL)
    /// y después de cada paso envía a BitacoraEventos / DigitoVerificador (sin retorno).
    /// </summary>
    public class CheckInBLL
    {
        private readonly CheckInDAL _dal = new CheckInDAL();
        private readonly CheckOutBLL _checkOut = new CheckOutBLL();   // CUN05 (extend)

        public const decimal PORCENTAJE_IMPUESTOS = 0.21m; // 21% IVA

        private int IdUsuarioActual =>
            SesionManager.Instancia.UsuarioActual?.IdUsuario ?? 0;

        private const string SIN_PERMISO =
            "❌ No tenés permiso para operar en Recepción. Solo Recepcionista o Administrador.";

        // ══════════════════════════════════════════════════════
        // Consultas de apoyo para la pantalla
        // ══════════════════════════════════════════════════════
        public List<TipoHabitacionBE> ObtenerTiposHabitacion()
            => _dal.ObtenerTiposHabitacion();

        public ResumenCheckInBE CalcularResumenCheckIn(
            DateTime fechaIngreso, DateTime fechaSalida, decimal precioPorNoche)
        {
            if (fechaSalida.Date <= fechaIngreso.Date)
                throw new ArgumentException("La fecha de salida debe ser posterior a la fecha de ingreso.");
            if (precioPorNoche <= 0)
                throw new ArgumentException("El precio por noche debe ser mayor a cero.");

            int noches = (fechaSalida.Date - fechaIngreso.Date).Days;
            decimal monto = noches * precioPorNoche;
            decimal impuestos = Math.Round(monto * PORCENTAJE_IMPUESTOS, 2);

            return new ResumenCheckInBE
            {
                CantNoches = noches,
                PrecioPorNoche = precioPorNoche,
                MontoHabitacion = monto,
                Impuestos = impuestos,
                MontoTotal = monto + impuestos
            };
        }

        /// <summary>
        /// Si el DNI ya está registrado devuelve los datos del huésped (para precargar el
        /// formulario en un nuevo check-in); si no existe devuelve null.
        /// </summary>
        public HuespedBE BuscarClientePorDNI(string dni)
        {
            if (!ControlAcceso.PuedeOperarRecepcion() || string.IsNullOrWhiteSpace(dni))
                return null;
            try { return _dal.ObtenerHuespedPorDNI(dni.Trim()); }
            catch { return null; }
        }

        // ══════════════════════════════════════════════════════
        // Consulta de disponibilidad (inicio de CUN01 / CUN02)
        // ══════════════════════════════════════════════════════
        public List<HabitacionBE> VerificarDisponibilidad(
            DateTime fechaIngreso, DateTime fechaSalida, out string mensaje)
        {
            mensaje = "";

            if (!ControlAcceso.PuedeOperarRecepcion())
            {
                mensaje = SIN_PERMISO;
                return new List<HabitacionBE>();
            }
            if (fechaIngreso.Date >= fechaSalida.Date)
            {
                mensaje = "❌ La fecha de salida debe ser posterior a la de ingreso.";
                return new List<HabitacionBE>();
            }
            if (fechaIngreso.Date < DateTime.Today)
            {
                mensaje = "❌ La fecha de ingreso no puede ser anterior a hoy.";
                return new List<HabitacionBE>();
            }

            try
            {
                var disponibles = _dal.ObtenerHabitacionesDisponibles(fechaIngreso, fechaSalida);
                if (disponibles.Count == 0)
                    mensaje = "No hay habitaciones disponibles para el período seleccionado.";

                BitacoraEventos.Registrar(BitacoraEventos.DISPONIBILIDAD, 5,
                    $"Consulta {fechaIngreso:dd/MM/yyyy} → {fechaSalida:dd/MM/yyyy}. " +
                    $"Habitaciones disponibles: {disponibles.Count}.", "EXITOSO");
                return disponibles;
            }
            catch (Exception ex)
            {
                BitacoraEventos.Registrar(BitacoraEventos.ERROR_TECNICO, 1,
                    $"Error al consultar disponibilidad. {ex.Message}", "FALLIDO");
                mensaje = $"❌ Error al consultar disponibilidad: {ex.Message}";
                return new List<HabitacionBE>();
            }
        }

        // ══════════════════════════════════════════════════════
        // CUN01 — Registrar Check-IN  (orquesta los includes)
        // ══════════════════════════════════════════════════════
        public ResultadoOperacionBE RegistrarCheckIn(SolicitudCheckInBE s)
        {
            var res = new ResultadoOperacionBE();
            var integras = DigitoVerificador.Integras("Huespedes", "Habitaciones", "Reservas", "CheckIns", "Pagos", "Facturas");
            var tocadas = new HashSet<string>();
            int idReserva = 0;
            bool completado = false;
            string dni = s?.Cliente?.DNI ?? "";

            try
            {
                // 0. Acceso (solo Recepcionista / Administrador)
                if (!ControlAcceso.PuedeOperarRecepcion())
                {
                    res.Mensaje = SIN_PERMISO;
                    BitacoraEventos.Registrar(BitacoraEventos.CHECKIN_RECH, 2,
                        "Check-in rechazado: el usuario no tiene rol de Recepción.", "FALLIDO");
                    return res;
                }

                // 1. Validaciones propias del CUN01
                string error = Validar(s);
                if (error != null)
                {
                    res.Mensaje = "❌ " + error;
                    BitacoraEventos.Registrar(BitacoraEventos.CHECKIN_RECH, 3,
                        $"Check-in rechazado por validación: {error} DNI: {dni}", "FALLIDO");
                    return res;
                }

                // 2. «include» CUN02 — Seleccionar habitación
                var habitacion = SeleccionarHabitacion(s.IdHabitacion, s.FechaIngreso, s.FechaSalida, out error);
                if (habitacion == null)
                {
                    res.Mensaje = "❌ " + error;
                    BitacoraEventos.Registrar(BitacoraEventos.CHECKIN_RECH, 3,
                        $"Check-in rechazado: {error} DNI: {dni}", "FALLIDO");
                    return res;
                }

                var resumen = CalcularResumenCheckIn(s.FechaIngreso, s.FechaSalida, habitacion.PrecioPorNoche);

                // 3. «include» CUN03 — Registrar datos del cliente
                int idHuesped = RegistrarDatosCliente(s.Cliente);

                // 4. Crear reserva pendiente
                idReserva = _dal.InsertarReserva(new ReservaBE
                {
                    IdHuesped = idHuesped,
                    IdHabitacion = habitacion.IdHabitacion,
                    FechaIngreso = s.FechaIngreso.Date,
                    FechaSalida = s.FechaSalida.Date,
                    Observaciones = s.Observaciones
                }, IdUsuarioActual);
                res.IdReserva = idReserva;

                BitacoraEventos.Registrar(BitacoraEventos.RESERVA, 4,
                    $"Reserva #{idReserva} creada. Hab. {habitacion.Numero}, DNI: {dni}, " +
                    $"{s.FechaIngreso:dd/MM/yyyy} → {s.FechaSalida:dd/MM/yyyy}.", "EXITOSO");
                DigitoVerificador.RecalcularIntegras(integras, "Reservas");

                // 5. «include» CUN04 — Realizar cobro (banco, pago y factura)
                var cobro = RealizarCobro(idReserva, dni, s.MetodoPago, s.NumeroTarjeta,
                                          s.CodigoSeguridad, resumen.MontoTotal);
                if (!cobro.Exito)
                {
                    res.Mensaje = cobro.Mensaje;
                    return res;           // CUN04 ya canceló la reserva y dejó bitácora/DV
                }
                res.NroFactura = cobro.NroFactura;

                // 6. Registrar el check-in y cambiar estados
                _dal.InsertarCheckIn(idReserva, DateTime.Now, IdUsuarioActual, s.Observaciones);
                _dal.ActualizarEstadoReserva(idReserva, "CheckIn");
                _dal.ActualizarEstadoHabitacion(habitacion.IdHabitacion, "Ocupada");
                completado = true;
                tocadas.Add("CheckIns"); tocadas.Add("Reservas"); tocadas.Add("Habitaciones");

                BitacoraEventos.Registrar(BitacoraEventos.CHECKIN, 3,
                    $"Check-in realizado. Reserva #{idReserva}, Hab. {habitacion.Numero}, DNI: {dni}, " +
                    $"{s.FechaIngreso:dd/MM/yyyy} → {s.FechaSalida:dd/MM/yyyy}, " +
                    $"Factura: {cobro.NroFactura}, Total: ${resumen.MontoTotal:N2}.", "EXITOSO");

                res.Exito = true;
                res.Mensaje =
                    "✔ Check-in realizado correctamente.\n\n" +
                    $"Habitación: {habitacion.Numero}\n" +
                    $"Noches: {resumen.CantNoches}\n" +
                    $"Precio por noche: ${resumen.PrecioPorNoche:N2}\n" +
                    $"Impuestos (21%): ${resumen.Impuestos:N2}\n" +
                    $"TOTAL: ${resumen.MontoTotal:N2}\n" +
                    $"Factura N°: {cobro.NroFactura}";
                return res;
            }
            catch (Exception ex)
            {
                // Reserva creada pero flujo incompleto → se cancela
                if (idReserva > 0 && !completado)
                {
                    try
                    {
                        _dal.ActualizarEstadoReserva(idReserva, "Cancelada");
                        tocadas.Add("Reservas");
                    }
                    catch { }
                }

                BitacoraEventos.Registrar(BitacoraEventos.ERROR_TECNICO, 1,
                    $"Error en check-in. Reserva #{idReserva}, DNI: {dni}. {ex.Message}", "FALLIDO");

                res.Exito = false;
                res.Mensaje = $"❌ Error al realizar el check-in: {ex.Message}";
                return res;
            }
            finally
            {
                DigitoVerificador.RecalcularIntegras(integras, tocadas.ToArray());
            }
        }

        private static string Validar(SolicitudCheckInBE s)
        {
            if (s == null || s.Cliente == null) return "Faltan los datos del check-in.";
            if (s.IdHabitacion <= 0) return "Debe seleccionar una habitación.";
            if (string.IsNullOrWhiteSpace(s.Cliente.Nombre)) return "Debe ingresar el nombre.";
            if (string.IsNullOrWhiteSpace(s.Cliente.Apellido)) return "Debe ingresar el apellido.";
            if (string.IsNullOrWhiteSpace(s.Cliente.DNI)) return "Debe ingresar el DNI.";
            if (s.FechaSalida.Date <= s.FechaIngreso.Date) return "La fecha de salida debe ser posterior a la fecha de ingreso.";
            if (string.IsNullOrWhiteSpace(s.MetodoPago)) return "Debe seleccionar un método de pago.";
            return null;
        }

        // ══════════════════════════════════════════════════════
        // CUN02 — Seleccionar habitación («include» de CUN01)
        // ══════════════════════════════════════════════════════
        public HabitacionBE SeleccionarHabitacion(
            int idHabitacion, DateTime fechaIngreso, DateTime fechaSalida, out string error)
        {
            error = null;

            var hab = _dal.ObtenerHabitacionPorId(idHabitacion);
            if (hab == null)
            {
                error = $"Habitación Id {idHabitacion} inexistente.";
                return null;
            }
            if (hab.Estado != "Disponible")
            {
                error = $"La habitación {hab.Numero} ya no se encuentra disponible (estado '{hab.Estado}').";
                return null;
            }

            // Chequea que siga libre en el período (otra reserva pudo tomarla)
            bool libre = _dal.ObtenerHabitacionesDisponibles(fechaIngreso, fechaSalida)
                             .Any(h => h.IdHabitacion == idHabitacion);
            if (!libre)
            {
                error = $"La habitación {hab.Numero} no está libre en el período solicitado.";
                return null;
            }
            return hab;
        }

        // ══════════════════════════════════════════════════════
        // CUN03 — Registrar datos del cliente («include» de CUN01)
        // Devuelve el IdHuesped (existente por DNI o recién creado).
        // ══════════════════════════════════════════════════════
        public int RegistrarDatosCliente(HuespedBE cliente)
        {
            var integras = DigitoVerificador.Integras("Huespedes");
            var existente = _dal.ObtenerHuespedPorDNI(cliente.DNI);
            if (existente != null)
                return existente.IdHuesped;

            int idHuesped = _dal.InsertarHuesped(new HuespedBE
            {
                Nombre = cliente.Nombre,
                Apellido = cliente.Apellido,
                DNI = cliente.DNI,
                Email = cliente.Email,
                Telefono = cliente.Telefono,
                Nacionalidad = cliente.Nacionalidad
            });

            BitacoraEventos.Registrar(BitacoraEventos.ALTA_HUESPED, 4,
                $"Alta de huésped. Id: {idHuesped}, DNI: {cliente.DNI}, {cliente.Apellido}, {cliente.Nombre}.",
                "EXITOSO");
            DigitoVerificador.RecalcularIntegras(integras, "Huespedes");
            return idHuesped;
        }

        // ══════════════════════════════════════════════════════
        // CUN04 — Realizar cobro («include» de CUN01)
        // Banco → pago → factura. Si el banco rechaza: cancela la reserva.
        // ══════════════════════════════════════════════════════
        public ResultadoOperacionBE RealizarCobro(
            int idReserva, string dni, string metodoPago,
            string numeroTarjeta, string codigoSeguridad, decimal monto)
        {
            var res = new ResultadoOperacionBE { IdReserva = idReserva };
            var integras = DigitoVerificador.Integras("Huespedes", "Habitaciones", "Reservas", "CheckIns", "Pagos", "Facturas");
            var tocadas = new HashSet<string>();

            try
            {
                bool aceptado = new BancoServicio().AutorizarPago(
                    dni, numeroTarjeta, codigoSeguridad, monto, out string respuestaBanco);

                if (!aceptado)
                {
                    _dal.ActualizarEstadoReserva(idReserva, "Cancelada");
                    tocadas.Add("Reservas");

                    // Nunca se registra número de tarjeta ni código de seguridad
                    BitacoraEventos.Registrar(BitacoraEventos.PAGO_RECHAZADO, 2,
                        $"Pago rechazado. Reserva #{idReserva}, DNI: {dni}, " +
                        $"Monto: ${monto:N2}. Banco: {respuestaBanco}", "FALLIDO");
                    BitacoraEventos.Registrar(BitacoraEventos.RESERVA_CANCEL, 3,
                        $"Reserva #{idReserva} cancelada por pago rechazado.", "EXITOSO");

                    res.Mensaje = "❌ Pago rechazado.\n" + respuestaBanco;
                    return res;
                }

                string ultimosCuatro = "";
                if (!string.IsNullOrWhiteSpace(numeroTarjeta))
                {
                    string nums = new string(numeroTarjeta.Where(char.IsDigit).ToArray());
                    if (nums.Length >= 4) ultimosCuatro = nums.Substring(nums.Length - 4);
                }

                _dal.InsertarPago(new PagoBE
                {
                    IdReserva = idReserva,
                    MetodoPago = metodoPago,
                    Monto = monto,
                    Estado = "Realizado",
                    UltimosCuatroTarjeta = ultimosCuatro,
                    IdUsuarioRegistro = IdUsuarioActual
                });
                tocadas.Add("Pagos");

                BitacoraEventos.Registrar(BitacoraEventos.PAGO_OK, 3,
                    $"Pago autorizado. Reserva #{idReserva}, {metodoPago}" +
                    (ultimosCuatro != "" ? $" ****{ultimosCuatro}" : "") +
                    $", Monto: ${monto:N2}.", "EXITOSO");

                string nroFactura = _dal.GenerarNroFactura();
                _dal.InsertarFactura(idReserva, nroFactura, monto, IdUsuarioActual);
                tocadas.Add("Facturas");

                BitacoraEventos.Registrar(BitacoraEventos.FACTURA, 4,
                    $"Factura {nroFactura} emitida. Reserva #{idReserva}, Total: ${monto:N2}.", "EXITOSO");

                res.Exito = true;
                res.NroFactura = nroFactura;
                return res;
            }
            finally
            {
                DigitoVerificador.RecalcularIntegras(integras, tocadas.ToArray());
            }
        }

        // ══════════════════════════════════════════════════════
        // CUN05 — Realizar Check-OUT («extend» de CUN01)
        // La BLL de Check-IN lo lanza; la pantalla solo habla con esta clase.
        // ══════════════════════════════════════════════════════
        public List<ReservaBE> ObtenerReservasActivas()
            => ControlAcceso.PuedeOperarRecepcion() ? _checkOut.ObtenerReservasActivas() : new List<ReservaBE>();

        public ResumenCheckOutBE CalcularResumenCheckOut(int idReserva)
            => _checkOut.CalcularResumenCheckOut(idReserva);

        public ResultadoOperacionBE RealizarCheckOut(int idReserva)
            => _checkOut.RealizarCheckOut(idReserva);
    }
}
