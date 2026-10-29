using System;

namespace ENTIDADES_HRS_480
{
    /// <summary>Datos de una factura de estadía listos para imprimir (reporte PDF).</summary>
    public class FacturaReporteBE
    {
        public string NroFactura { get; set; } = "";
        public DateTime FechaEmision { get; set; }
        public string Estado { get; set; } = "";
        public decimal MontoTotal { get; set; }
        public int IdReserva { get; set; }

        public string Apellido { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string DNI { get; set; } = "";

        public string NumeroHabitacion { get; set; } = "";
        public string TipoHabitacion { get; set; } = "";
        public decimal PrecioPorNoche { get; set; }
        public DateTime FechaIngreso { get; set; }
        public DateTime FechaSalida { get; set; }
        public int Noches { get; set; }

        public string MetodoPago { get; set; } = "";
        public string UltimosCuatroTarjeta { get; set; } = "";
        public string EmitidaPor { get; set; } = "";

        public string Huesped => (Apellido + ", " + Nombre).Trim(' ', ',');
        public decimal Subtotal => Noches * PrecioPorNoche;
        public decimal Impuestos => MontoTotal - Subtotal;
    }
}
