using System;

namespace ENTIDADES_HRS_480
{
    /// <summary>Aviso interno (a un usuario) o registro de aviso a un proveedor.</summary>
    public class NotificacionBE
    {
        public int IdNotificacion { get; set; }
        public int? IdUsuarioDestino { get; set; }
        public int? IdProveedorDestino { get; set; }
        public int? IdSolicitud { get; set; }
        public int? IdOrden { get; set; }
        public string Tipo { get; set; } = "";
        public string Mensaje { get; set; } = "";
        public DateTime Fecha { get; set; }
        public bool Leida { get; set; }
    }

    /// <summary>Ingreso de mercadería al stock (trazabilidad de cada recepción).</summary>
    public class MovimientoStockBE
    {
        public int IdMovimiento { get; set; }
        public int IdInsumo { get; set; }
        public string NombreInsumo { get; set; } = "";
        public int? IdRecepcion { get; set; }
        public string Tipo { get; set; } = "Ingreso";
        public decimal CantidadAnterior { get; set; }
        public decimal CantidadMovimiento { get; set; }
        public decimal CantidadNueva { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public int? IdUsuarioRegistro { get; set; }
    }

    public class SolicitudReembolsoBE
    {
        public int IdReembolso { get; set; }
        public int IdOrden { get; set; }
        public string NroOrden { get; set; } = "";
        public string NombreInsumo { get; set; } = "";
        public string NombreProveedor { get; set; } = "";
        public decimal CantidadComprada { get; set; }
        public decimal CantidadRecibida { get; set; }
        public decimal Diferencia { get; set; }
        public decimal MontoReembolso { get; set; }
        public string Motivo { get; set; } = "";
        public DateTime FechaReembolso { get; set; }
        public string Estado { get; set; } = "Pendiente";
    }

    /// <summary>Resultado del CUN09: proveedor elegido con las condiciones pactadas.</summary>
    public class ProveedorSeleccionadoBE
    {
        public ProveedorBE Proveedor { get; set; } = new ProveedorBE();
        public decimal PrecioUnitario { get; set; }
        public int PlazoDias { get; set; }
    }

    /// <summary>Resultado de las operaciones de reposición (la BLL no lanza excepciones a la GUI).</summary>
    public class ResultadoReposicion<T>
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = "";
        public T Dato { get; set; } = default!;

        public static ResultadoReposicion<T> Ok(T dato, string mensaje = "")
            => new ResultadoReposicion<T> { Exito = true, Dato = dato, Mensaje = mensaje };

        public static ResultadoReposicion<T> Fallo(string mensaje)
            => new ResultadoReposicion<T> { Exito = false, Mensaje = mensaje };
    }
}
