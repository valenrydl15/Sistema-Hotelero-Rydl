using System;
using System.Collections.Generic;
using System.Linq;

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

    // ══════════════════════════════════════════════════════════════════════
    // CUN06 Realizar cotización
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Precio y plazo que informó un proveedor para la cotización.</summary>
    public class CotizacionDetalleBE
    {
        public int IdDetalleCotizacion { get; set; }
        public int IdCotizacion { get; set; }
        public int IdProveedor { get; set; }
        public string NombreProveedor { get; set; } = "";
        public decimal PrecioUnitario { get; set; }
        public int PlazoDias { get; set; }
        /// <summary>Lo calcula quien arma la cotización (Cantidad × PrecioUnitario).</summary>
        public decimal Subtotal { get; set; }
        /// <summary>La oferta más conveniente: menor total y, si empatan, menor plazo.</summary>
        public bool MasConveniente { get; set; }
    }

    /// <summary>Un insumo y una cantidad cotizados con varios proveedores.</summary>
    public class CotizacionBE
    {
        public int IdCotizacion { get; set; }
        public string NroCotizacion => $"CT-{IdCotizacion:D5}";
        public int IdInsumo { get; set; }
        public string NombreInsumo { get; set; } = "";
        public string UnidadMedida { get; set; } = "";
        public decimal Cantidad { get; set; }
        public DateTime FechaCotizacion { get; set; }
        public string Estado { get; set; } = "Abierta";   // 'Abierta','Con pre-orden','Anulada'
        public string Observaciones { get; set; } = "";
        public int? IdUsuarioRegistro { get; set; }
        public string NombreUsuario { get; set; } = "";
        public List<CotizacionDetalleBE> Detalles { get; set; } = new List<CotizacionDetalleBE>();

        public CotizacionDetalleBE? MejorOferta =>
            Detalles.OrderBy(d => d.PrecioUnitario).ThenBy(d => d.PlazoDias).FirstOrDefault();
    }

    // ══════════════════════════════════════════════════════════════════════
    // CUN07 Generar pre-orden de compra
    // ══════════════════════════════════════════════════════════════════════
    public class PreOrdenCompraBE
    {
        public int IdPreOrden { get; set; }
        public string NroPreOrden { get; set; } = "";
        public int IdCotizacion { get; set; }
        public int IdDetalleCotizacion { get; set; }
        public int IdInsumo { get; set; }
        public string NombreInsumo { get; set; } = "";
        public string UnidadMedida { get; set; } = "";
        public int IdProveedor { get; set; }
        public string NombreProveedor { get; set; } = "";
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal PrecioTotal { get; set; }
        public int PlazoDias { get; set; }
        public string Prioridad { get; set; } = "Normal";
        public DateTime FechaPreOrden { get; set; }
        public string Estado { get; set; } = "Pendiente";   // 'Pendiente','Comprada'
        public string Observaciones { get; set; } = "";
        public int? IdUsuarioRegistro { get; set; }
        public string NombreUsuario { get; set; } = "";
        /// <summary>El proveedor nunca cobró: hay que registrarlo antes de pagar (CUN08).</summary>
        public bool ProveedorNuevo { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════
    // CUN09 Realizar compra de insumos (pago bancario)
    // ══════════════════════════════════════════════════════════════════════
    public class PagoProveedorBE
    {
        public int IdPagoProveedor { get; set; }
        public int IdPreOrden { get; set; }
        public int IdProveedor { get; set; }
        public decimal Monto { get; set; }
        public string MedioPago { get; set; } = "Transferencia bancaria";
        public string CBUDestino { get; set; } = "";
        public string NroOperacion { get; set; } = "";
        public DateTime FechaPago { get; set; }
        public string Estado { get; set; } = "";   // 'Aprobado','Rechazado'
        public string DetalleBanco { get; set; } = "";
    }

    // ══════════════════════════════════════════════════════════════════════
    // CUN10 Registrar insumos recibidos (con la factura del proveedor)
    // ══════════════════════════════════════════════════════════════════════
    public class FacturaProveedorBE
    {
        public int IdFacturaProveedor { get; set; }
        public int IdOrden { get; set; }
        public int IdProveedor { get; set; }
        public string NroFactura { get; set; } = "";
        public DateTime FechaFactura { get; set; }
        public int IdInsumo { get; set; }
        public decimal CantidadFacturada { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Total => CantidadFacturada * PrecioUnitario;
    }

    /// <summary>Lo que el Encargado de Depósito carga al recibir: la factura y lo que realmente llegó.</summary>
    public class RecepcionConFacturaBE
    {
        public int IdOrden { get; set; }
        public string NroFactura { get; set; } = "";
        public DateTime FechaFactura { get; set; }
        public decimal CantidadFacturada { get; set; }
        public decimal PrecioUnitarioFacturado { get; set; }
        public decimal CantidadRecibida { get; set; }
        public string Observaciones { get; set; } = "";
    }

    /// <summary>Resultado de comparar orden, factura y mercadería recibida.</summary>
    public class ControlRecepcionBE
    {
        public RecepcionInsumoBE Recepcion { get; set; } = new RecepcionInsumoBE();
        public MovimientoStockBE Movimiento { get; set; } = new MovimientoStockBE();
        public List<string> Diferencias { get; set; } = new List<string>();
        public bool Coincide => Diferencias.Count == 0;
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
