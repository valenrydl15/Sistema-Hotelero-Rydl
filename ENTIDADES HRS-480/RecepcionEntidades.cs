using System;

namespace ENTIDADES_HRS_480
{
    /// <summary>Datos que la pantalla de Recepción envía al CUN01 (Registrar Check-IN).</summary>
    public class SolicitudCheckInBE
    {
        public HuespedBE Cliente { get; set; } = new HuespedBE();
        public int IdHabitacion { get; set; }
        public DateTime FechaIngreso { get; set; }
        public DateTime FechaSalida { get; set; }
        public string Observaciones { get; set; } = "";
        public string MetodoPago { get; set; } = "";
        public string NumeroTarjeta { get; set; } = "";
        public string CodigoSeguridad { get; set; } = "";
    }

    /// <summary>Resultado de una operación de negocio (mensaje listo para mostrar).</summary>
    public class ResultadoOperacionBE
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = "";
        public string NroFactura { get; set; } = "";
        public int IdReserva { get; set; }
    }
}
