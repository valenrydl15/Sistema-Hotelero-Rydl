using System;
using System.Collections.Generic;
using System.Linq;

namespace ENTIDADES_HRS_480
{
    // ══════════════════════════════════════════════════════════════════════
    // Catálogos
    // ══════════════════════════════════════════════════════════════════════
    public class InsumoBE
    {
        public int IdInsumo { get; set; }
        public string Codigo => $"INS-{IdInsumo:D3}";
        public string Nombre { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Categoria { get; set; } = "General";
        public string UnidadMedida { get; set; } = "";
        public decimal StockActual { get; set; }
        public decimal StockMinimo { get; set; }
        public bool Activo { get; set; }
        public string Estado { get; set; } = "";   // 'Critico' o 'Normal' (columna calculada)
        /// <summary>Faltante = stock en o por debajo del mínimo.</summary>
        public bool Faltante => StockActual <= StockMinimo;
        /// <summary>Cantidad sugerida para reponer: llevar el stock al doble del mínimo.</summary>
        public decimal CantidadSugerida => Math.Max(1, StockMinimo * 2 - StockActual);
        public override string ToString() => $"{Codigo} {Nombre}";
    }

    public class ProveedorBE
    {
        public const string PRE_REGISTRADO = "Pre-registrado";
        public const string REGISTRADO = "Registrado";

        public int IdProveedor { get; set; }
        public string Nombre { get; set; } = "";       // empresa
        public string Contacto { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Email { get; set; } = "";
        public string Direccion { get; set; } = "";
        public string CondicionesPago { get; set; } = "";
        public bool Activo { get; set; } = true;
        public string Estado { get; set; } = PRE_REGISTRADO;
        // Datos para pagarle (CUN10 Registrar proveedor)
        public string CUIT { get; set; } = "";
        public string Banco { get; set; } = "";
        public string CBU { get; set; } = "";
        public string Titular { get; set; } = "";
        public DateTime? FechaRegistroPago { get; set; }
        public bool PreRegistrado => Estado != REGISTRADO;
        public override string ToString() => PreRegistrado ? $"{Nombre} (pre-registrado)" : Nombre;
    }

    /// <summary>Aviso interno (a un usuario) o registro de envío a un proveedor.</summary>
    public class NotificacionBE
    {
        public int IdNotificacion { get; set; }
        public int? IdUsuarioDestino { get; set; }
        public int? IdProveedorDestino { get; set; }
        public int? IdOrden { get; set; }
        public string Tipo { get; set; } = "";
        public string Mensaje { get; set; } = "";
        public DateTime Fecha { get; set; }
        public bool Leida { get; set; }
    }

    /// <summary>Ingreso de mercadería al stock (cantidad anterior, recibida y disponible).</summary>
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
    }

    /// <summary>Línea de un documento del proceso (solicitud, cotización, orden, recepción).</summary>
    public class ItemInsumoBE
    {
        public int IdDetalle { get; set; }
        public int IdInsumo { get; set; }
        public string Codigo => $"INS-{IdInsumo:D3}";
        public string NombreInsumo { get; set; } = "";
        public string UnidadMedida { get; set; } = "";
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal => Math.Round(Cantidad * PrecioUnitario, 2);
    }

    // ══════════════════════════════════════════════════════════════════════
    // CUN06 Generar solicitud de cotización
    // ══════════════════════════════════════════════════════════════════════
    public class SolicitudCotizacionBE
    {
        public int IdSolicitudCotizacion { get; set; }
        public string NroSolicitud { get; set; } = "";
        public DateTime FechaSolicitud { get; set; }
        public int IdProveedor { get; set; }
        public string NombreProveedor { get; set; } = "";
        public string EmailProveedor { get; set; } = "";
        public string Estado { get; set; } = "Enviada";   // 'Enviada','Cotizada'
        public DateTime? FechaEnvio { get; set; }
        public List<ItemInsumoBE> Items { get; set; } = new List<ItemInsumoBE>();
    }

    // ══════════════════════════════════════════════════════════════════════
    // CUN08 Generar pre-orden de compra (cotización recibida y comparación)
    // ══════════════════════════════════════════════════════════════════════
    public class CotizacionBE
    {
        public int IdCotizacion { get; set; }
        public int IdSolicitudCotizacion { get; set; }
        public string NroSolicitud { get; set; } = "";
        public int IdProveedor { get; set; }
        public string NombreProveedor { get; set; } = "";
        public DateTime FechaCotizacion { get; set; }
        public string CUIT { get; set; } = "";
        public string Banco { get; set; } = "";
        public string CBU { get; set; } = "";
        public string Titular { get; set; } = "";
        public string Estado { get; set; } = "Recibida";  // 'Recibida','Procesada'
        /// <summary>Cantidad = cantidad disponible que informa el proveedor.</summary>
        public List<ItemInsumoBE> Items { get; set; } = new List<ItemInsumoBE>();
    }

    /// <summary>Una oferta (proveedor + precio) para un insumo, en la comparación de cotizaciones.</summary>
    public class OfertaBE
    {
        public int IdCotizacion { get; set; }
        public int IdProveedor { get; set; }
        public string NombreProveedor { get; set; } = "";
        public int IdInsumo { get; set; }
        public string NombreInsumo { get; set; } = "";
        public string UnidadMedida { get; set; } = "";
        public decimal CantidadSolicitada { get; set; }
        public decimal CantidadDisponible { get; set; }
        public decimal PrecioUnitario { get; set; }
        /// <summary>Lo que se pediría: lo solicitado, sin superar lo disponible.</summary>
        public decimal CantidadAPedir => Math.Min(CantidadSolicitada, CantidadDisponible);
        public decimal Subtotal => Math.Round(CantidadAPedir * PrecioUnitario, 2);
        /// <summary>La oferta más barata del insumo (en empate, la de más cantidad disponible).</summary>
        public bool MasConveniente { get; set; }
        public bool Elegida { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Orden de compra: Pre-orden → Generada → Pagada → Recibida (o Cancelada)
    // ══════════════════════════════════════════════════════════════════════
    public static class EstadoOrden
    {
        public const string PRE_ORDEN = "Pre-orden";
        public const string GENERADA = "Generada";
        public const string CANCELADA = "Cancelada";
        public const string PAGADA = "Pagada";
        public const string RECIBIDA = "Recibida";
    }

    public class OrdenCompraBE
    {
        public int IdOrden { get; set; }
        public string NroOrden { get; set; } = "";
        public DateTime FechaPreOrden { get; set; }
        public DateTime? FechaGeneracion { get; set; }
        public int IdProveedor { get; set; }
        public string NombreProveedor { get; set; } = "";
        public string EmailProveedor { get; set; } = "";
        public string EstadoProveedor { get; set; } = "";
        public int IdCotizacion { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; } = EstadoOrden.PRE_ORDEN;
        public string MotivoCancelacion { get; set; } = "";
        public string DireccionEntrega { get; set; } = "";
        public DateTime? FechaEnvio { get; set; }
        public string NroOperacionPago { get; set; } = "";
        public DateTime? FechaPago { get; set; }
        public List<ItemInsumoBE> Items { get; set; } = new List<ItemInsumoBE>();
        public string ResumenItems => string.Join(", ", Items.Select(i => $"{i.NombreInsumo} x{i.Cantidad:0.##}"));
        public override string ToString() => $"{NroOrden} | {NombreProveedor} | {Estado}";
    }

    // ══════════════════════════════════════════════════════════════════════
    // CUN11 Pagar orden de compra
    // ══════════════════════════════════════════════════════════════════════
    public class PagoOrdenCompraBE
    {
        public int IdPago { get; set; }
        public int IdOrden { get; set; }
        public decimal Monto { get; set; }
        public string Banco { get; set; } = "";
        public string CBU { get; set; } = "";
        public string Titular { get; set; } = "";
        public string NroOperacion { get; set; } = "";
        public string Estado { get; set; } = "";   // 'Aprobado','Rechazado'
        public DateTime FechaPago { get; set; }
        public string DetalleBanco { get; set; } = "";
    }

    // ══════════════════════════════════════════════════════════════════════
    // CUN12 Registrar recepción de insumos
    // ══════════════════════════════════════════════════════════════════════
    public class ItemRecepcionBE
    {
        public int IdInsumo { get; set; }
        public string NombreInsumo { get; set; } = "";
        public decimal CantidadPedida { get; set; }
        public decimal CantidadRecibida { get; set; }
    }

    public class RecepcionInsumoBE
    {
        public int IdRecepcion { get; set; }
        public int IdOrden { get; set; }
        public string NroOrden { get; set; } = "";
        public string NroFactura { get; set; } = "";
        public decimal TotalFactura { get; set; }
        public DateTime FechaRecepcion { get; set; }
        public string Estado { get; set; } = "Completa";   // 'Completa','Con diferencia'
        public string Observaciones { get; set; } = "";
        public List<ItemRecepcionBE> Items { get; set; } = new List<ItemRecepcionBE>();
        public List<MovimientoStockBE> Movimientos { get; set; } = new List<MovimientoStockBE>();
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
