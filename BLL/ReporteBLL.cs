using System;
using System.Collections.Generic;
using System.IO;
using DAL_HRS_480;
using ENTIDADES_HRS_480;
using SERVICIOS_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// Reportes PDF de los requisitos funcionales:
    ///  · PN1 Recepción  → factura de estadía y listado de facturas (acceso: Recepcionista / Administrador).
    ///  · PN2 Reposición → orden de compra y listado de órdenes (acceso: Administrador, Compras y Almacén).
    /// Son de solo lectura: no modifican tablas, por lo que no alteran los dígitos verificadores.
    /// Cada exportación queda registrada en la bitácora.
    /// </summary>
    public class ReporteBLL
    {
        private readonly ReportesDAL _dal = new ReportesDAL();

        private static string Usuario => ReposicionComun.NombreActual;

        private static string? ValidarRuta(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta)) return "Indicá dónde guardar el archivo PDF.";
            if (!ruta.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) return "El archivo debe tener extensión .pdf.";
            string? dir = Path.GetDirectoryName(Path.GetFullPath(ruta));
            if (dir == null || !Directory.Exists(dir)) return "La carpeta de destino no existe.";
            return null;
        }

        private static string MensajeIO(Exception ex)
            => ex is IOException || ex is UnauthorizedAccessException
                ? "No se pudo escribir el archivo. Verificá que no esté abierto en otro programa y que tengas permiso en esa carpeta."
                : "Ocurrió un error al generar el PDF: " + ex.Message;

        // ═══════════════════════════ PN1 — Facturas ═══════════════════════════
        public ResultadoReposicion<List<FacturaReporteBE>> ObtenerFacturas(DateTime? desde, DateTime? hasta)
        {
            if (!ControlAcceso.PuedeOperarRecepcion())
                return ResultadoReposicion<List<FacturaReporteBE>>.Fallo("Su rol no tiene permiso para ver facturas.");
            if (desde.HasValue && hasta.HasValue && hasta.Value.Date < desde.Value.Date)
                return ResultadoReposicion<List<FacturaReporteBE>>.Fallo("La fecha 'hasta' no puede ser anterior a 'desde'.");
            try { return ResultadoReposicion<List<FacturaReporteBE>>.Ok(_dal.ObtenerFacturas(desde, hasta)); }
            catch (Exception ex)
            {
                BitacoraEventos.Registrar(BitacoraEventos.ERROR_TECNICO, 1, "Consulta de facturas: " + ex.Message, "FALLIDO");
                return ResultadoReposicion<List<FacturaReporteBE>>.Fallo("No se pudieron obtener las facturas. Detalle: " + ex.Message);
            }
        }

        /// <summary>Exporta una factura a PDF. Devuelve la ruta generada.</summary>
        public ResultadoReposicion<string> ExportarFactura(string nroFactura, string ruta)
        {
            if (!ControlAcceso.PuedeOperarRecepcion())
            {
                BitacoraEventos.Registrar(BitacoraEventos.REPORTE_FACTURA, 2,
                    $"{Usuario} intentó exportar la factura {nroFactura} sin permiso.", "RECHAZADO");
                return ResultadoReposicion<string>.Fallo("Su rol no tiene permiso para exportar facturas.");
            }
            string? err = ValidarRuta(ruta);
            if (err != null) return ResultadoReposicion<string>.Fallo(err);
            try
            {
                var f = _dal.ObtenerFactura(nroFactura);
                if (f == null) return ResultadoReposicion<string>.Fallo("No se encontró la factura " + nroFactura + ".");
                ReporteServicio.FacturaPdf(f, ruta, Usuario);
                BitacoraEventos.Registrar(BitacoraEventos.REPORTE_FACTURA, 4,
                    $"Factura {nroFactura} exportada a PDF por {Usuario}.", "EXITOSO");
                return ResultadoReposicion<string>.Ok(ruta, "Factura " + nroFactura + " exportada a PDF.");
            }
            catch (Exception ex)
            {
                BitacoraEventos.Registrar(BitacoraEventos.ERROR_TECNICO, 1, $"Exportar factura {nroFactura}: {ex.Message}", "FALLIDO");
                return ResultadoReposicion<string>.Fallo(MensajeIO(ex));
            }
        }

        /// <summary>Exporta el listado de facturas del período a PDF.</summary>
        public ResultadoReposicion<string> ExportarListadoFacturas(DateTime? desde, DateTime? hasta, string ruta)
        {
            string? err = ValidarRuta(ruta);
            if (err != null) return ResultadoReposicion<string>.Fallo(err);
            var r = ObtenerFacturas(desde, hasta);
            if (!r.Exito) return ResultadoReposicion<string>.Fallo(r.Mensaje);
            if (r.Dato.Count == 0) return ResultadoReposicion<string>.Fallo("No hay facturas en el período elegido.");
            try
            {
                string periodo = "Período: " + (desde.HasValue ? desde.Value.ToString("dd/MM/yyyy") : "inicio")
                               + " al " + (hasta.HasValue ? hasta.Value.ToString("dd/MM/yyyy") : "hoy");
                ReporteServicio.ListadoFacturasPdf(r.Dato, periodo, ruta, Usuario);
                BitacoraEventos.Registrar(BitacoraEventos.REPORTE_FACTURA, 4,
                    $"Listado de {r.Dato.Count} facturas exportado a PDF por {Usuario}. {periodo}.", "EXITOSO");
                return ResultadoReposicion<string>.Ok(ruta, "Listado de facturas exportado a PDF.");
            }
            catch (Exception ex)
            {
                BitacoraEventos.Registrar(BitacoraEventos.ERROR_TECNICO, 1, "Exportar listado de facturas: " + ex.Message, "FALLIDO");
                return ResultadoReposicion<string>.Fallo(MensajeIO(ex));
            }
        }

        // ═══════════════════════════ PN2 — Órdenes de compra ═══════════════════════════
        public ResultadoReposicion<string> ExportarOrdenCompra(int idOrden, string ruta)
        {
            if (!ControlAcceso.PuedeAccederReposicion()) return ReposicionComun.Denegado<string>("Exportar orden de compra a PDF");
            string? err = ValidarRuta(ruta);
            if (err != null) return ResultadoReposicion<string>.Fallo(err);
            try
            {
                using var dal = new ReposicionDAL();
                var o = dal.ObtenerOrdenPorId(idOrden);
                if (o == null) return ReposicionComun.Rechazo<string>("Exportar orden de compra a PDF", "La orden de compra no existe.");
                var p = dal.ObtenerProveedorPorId(o.IdProveedor);
                ReporteServicio.OrdenCompraPdf(o, p, ruta, Usuario);
                BitacoraReposicion.Registrar(BitacoraReposicion.REPORTE_ORDEN, 4,
                    $"Orden {o.NroOrden} exportada a PDF por {Usuario}.", "EXITOSO");
                return ResultadoReposicion<string>.Ok(ruta, "Orden " + o.NroOrden + " exportada a PDF.");
            }
            catch (Exception ex)
            {
                BitacoraReposicion.Registrar(BitacoraReposicion.ERROR_TECNICO, 1, $"Exportar orden #{idOrden}: {ex.Message}", "FALLIDO");
                return ResultadoReposicion<string>.Fallo(MensajeIO(ex));
            }
        }

        /// <param name="estado">null = todas.</param>
        public ResultadoReposicion<string> ExportarListadoOrdenes(string? estado, string ruta)
        {
            if (!ControlAcceso.PuedeAccederReposicion()) return ReposicionComun.Denegado<string>("Exportar órdenes de compra a PDF");
            string? err = ValidarRuta(ruta);
            if (err != null) return ResultadoReposicion<string>.Fallo(err);
            try
            {
                using var dal = new ReposicionDAL();
                var lista = dal.ObtenerOrdenes(estado);
                if (lista.Count == 0) return ResultadoReposicion<string>.Fallo("No hay órdenes de compra para exportar con ese filtro.");
                string filtro = "Estado: " + (estado ?? "todas");
                ReporteServicio.ListadoOrdenesPdf(lista, filtro, ruta, Usuario);
                BitacoraReposicion.Registrar(BitacoraReposicion.REPORTE_ORDEN, 4,
                    $"Listado de {lista.Count} órdenes exportado a PDF por {Usuario}. {filtro}.", "EXITOSO");
                return ResultadoReposicion<string>.Ok(ruta, "Listado de órdenes exportado a PDF.");
            }
            catch (Exception ex)
            {
                BitacoraReposicion.Registrar(BitacoraReposicion.ERROR_TECNICO, 1, "Exportar listado de órdenes: " + ex.Message, "FALLIDO");
                return ResultadoReposicion<string>.Fallo(MensajeIO(ex));
            }
        }
    }
}
