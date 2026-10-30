using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ENTIDADES_HRS_480
{
        public class Rol
        {
            public int IdRol { get; set; }
            public string Nombre { get; set; }
            public string Descripcion { get; set; }
            public override string ToString() => Nombre;
        }

        // --------------------------------------------------
        public class Modulo
        {
            public int IdModulo { get; set; }
            public string Nombre { get; set; }
            public override string ToString() => Nombre;
        }

        // --------------------------------------------------
        public class TipoEvento
        {
            public int IdTipoEvento { get; set; }
            public int IdModulo { get; set; }
            public string NombreModulo { get; set; }
            public string Nombre { get; set; }
            public int Criticidad { get; set; }
            public override string ToString() => Nombre;
        }

        // --------------------------------------------------
        public class Usuario
        {
            public int IdUsuario { get; set; }
            public string Nombre { get; set; }
            public string Apellido { get; set; }
            public string DNI { get; set; }
            public string Email { get; set; }
            public string NombreUsuario { get; set; }
            public string ContrasenaHash { get; set; }
            public int IdRol { get; set; }
            public string NombreRol { get; set; }
            public bool Activo { get; set; }
            public bool Bloqueado { get; set; }
            public int IntentosFallidos { get; set; }
            public int MaxIntentos { get; set; }
            public bool DebeChangiarClave { get; set; }
            public DateTime FechaCreacion { get; set; }
            public DateTime? FechaUltimoAcceso { get; set; }
            public bool SesionActiva { get; set; }

            public static string GenerarNombreUsuario(string dni, string nombre)
                => $"{dni.Trim()}{nombre.Trim()}";

            public static string GenerarClaveInicial(string dni, string apellido)
                => $"{dni.Trim()}{apellido.Trim()}";
        }

        // --------------------------------------------------
        public class RegistroBitacora
        {
            public int IdBitacora { get; set; }
            public int? IdUsuario { get; set; }
            public string Login { get; set; }   // NombreUsuario
            public string NombreCompleto { get; set; }   // Nombre + Apellido
            public string Nombre { get; set; }
            public string Apellido { get; set; }
            public DateTime Fecha { get; set; }
            public TimeSpan Hora { get; set; }
            public int? IdModulo { get; set; }
            public string NombreModulo { get; set; }
            public int? IdTipoEvento { get; set; }
            public string NombreEvento { get; set; }
            public string Detalle { get; set; }
            public int Criticidad { get; set; }
            public string Resultado { get; set; }
        }

    public interface IComponentePerfil
    {
        int Id { get; }
        string Nombre { get; }

       
        IEnumerable<Permiso> ObtenerPermisos();
    }

    
    public class Permiso : IComponentePerfil
    {
        public int IdPermiso { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public bool Activo { get; set; }

     
        public int Id => IdPermiso;
        public IEnumerable<Permiso> ObtenerPermisos()
            => new List<Permiso> { this };

        public override string ToString() => Nombre;
    }

  
    public class Familia : IComponentePerfil
    {
        public int IdFamilia { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public bool Activo { get; set; }

     
        public List<Permiso> Permisos { get; set; } = new List<Permiso>();
        public List<Familia> SubFamilias { get; set; } = new List<Familia>();

       
        public int Id => IdFamilia;

        public IEnumerable<Permiso> ObtenerPermisos()
        {
            var todos = new List<Permiso>(Permisos);
            foreach (var sub in SubFamilias)
                todos.AddRange(sub.ObtenerPermisos());
            return todos.GroupBy(p => p.IdPermiso).Select(g => g.First()); 
        }

        public override string ToString() => Nombre;
    }

  
    public class RolPerfil : IComponentePerfil
    {
        public int IdRol { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }

       
        public List<Permiso> PermisosDirectos { get; set; } = new List<Permiso>();
        public List<Familia> Familias { get; set; } = new List<Familia>();

       
        public int Id => IdRol;

      
        public IEnumerable<Permiso> ObtenerPermisos()
        {
            var todos = new List<Permiso>(PermisosDirectos);
            foreach (var f in Familias)
                todos.AddRange(f.ObtenerPermisos());
            return todos.GroupBy(p => p.IdPermiso).Select(g => g.First()); 
        }

        public override string ToString() => Nombre;



    }

    public class TablaDV
    {
        public int IdDV { get; set; }
        public string NombreTabla { get; set; }
        public string DVH { get; set; }  // Dígito Verificador Horizontal
        public string DVV { get; set; }  // Dígito Verificador Vertical
        public DateTime FechaCalculo { get; set; }
        public bool Valido { get; set; }
    }

    // ──────────────────────────────────────────────────────────
    // Resultado de la verificación completa del sistema
    // ──────────────────────────────────────────────────────────
    public class ResultadoVerificacionDV
    {
        public bool Consistente { get; set; }
        public List<InconsistenciaDV> Inconsistencias { get; set; } = new List<InconsistenciaDV>();
    }

    // ──────────────────────────────────────────────────────────
    // Detalle de una inconsistencia encontrada
    // ──────────────────────────────────────────────────────────
    public class InconsistenciaDV
    {
        public int IdInconsistencia { get; set; }
        public DateTime FechaDeteccion { get; set; }
        public string NombreTabla { get; set; }
        public string DVH_Esperado { get; set; }
        public string DVH_Calculado { get; set; }
        public string DVV_Esperado { get; set; }
        public string DVV_Calculado { get; set; }
        public bool Resuelta { get; set; }
        public string Resolucion { get; set; }

        // Para mostrar en el panel lateral del admin
        public string ResumenCorto =>
            $"[{FechaDeteccion:dd/MM/yy HH:mm}] Tabla: {NombreTabla} — " +
            (DVH_Esperado != DVH_Calculado ? "DVH " : "") +
            (DVV_Esperado != DVV_Calculado ? "DVV " : "") + "diferente";
    }

    // ──────────────────────────────────────────────────────────
    // Backup del estado correcto para el RESTORE
    // ──────────────────────────────────────────────────────────
    public class BackupDV
    {
        public int IdBackup { get; set; }
        public DateTime FechaBackup { get; set; }
        public string NombreTabla { get; set; }
        public string DVH { get; set; }
        public string DVV { get; set; }
        public string Descripcion { get; set; }
    }

    public class TipoHabitacionBE
    {
        public int IdTipoHabitacion { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal PrecioPorNoche { get; set; }
        public int Capacidad { get; set; }
        public bool Activo { get; set; }
        public override string ToString() => $"{Nombre} — ${PrecioPorNoche:N0}/noche";
    }

    // ──────────────────────────────────────────────────────────
    public class HabitacionBE
    {
        public int IdHabitacion { get; set; }
        public string Numero { get; set; }
        public int IdTipoHabitacion { get; set; }
        public string NombreTipo { get; set; }
        public decimal PrecioPorNoche { get; set; }
        public int Piso { get; set; }
        public string Estado { get; set; }  // Disponible/Ocupada/Limpieza/Mantenimiento
        public string Observaciones { get; set; }
        public bool Activa { get; set; }
        public override string ToString() => $"Hab. {Numero} — {NombreTipo} — ${PrecioPorNoche:N0}/noche ({Estado})";
    }

    // ──────────────────────────────────────────────────────────
    public class HuespedBE
    {
        public int IdHuesped { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string DNI { get; set; }
        public string Email { get; set; }
        public string Telefono { get; set; }
        public string Nacionalidad { get; set; }
        public DateTime FechaRegistro { get; set; }
        public bool Activo { get; set; }
        public override string ToString() => $"{Apellido}, {Nombre} (DNI: {DNI})";
    }

    // ──────────────────────────────────────────────────────────
    public class ReservaBE
    {
        public int IdReserva { get; set; }
        public int IdHuesped { get; set; }
        public string NombreHuesped { get; set; }
        public string ApellidoHuesped { get; set; }
        public string DNIHuesped { get; set; }
        public int IdHabitacion { get; set; }
        public string NumeroHabitacion { get; set; }
        public string TipoHabitacion { get; set; }
        public decimal PrecioPorNoche { get; set; }
        public DateTime FechaIngreso { get; set; }
        public DateTime FechaSalida { get; set; }
        public int CantNoches { get; set; }
        public string Estado { get; set; }
        public DateTime FechaReserva { get; set; }
        public string Observaciones { get; set; }
        public override string ToString() =>
            $"Reserva #{IdReserva} — {ApellidoHuesped}, {NombreHuesped} — Hab. {NumeroHabitacion} ({FechaIngreso:dd/MM/yy} → {FechaSalida:dd/MM/yy})";
    }

    // ──────────────────────────────────────────────────────────

    public class ResumenCheckInBE
    {
        public decimal MontoHabitacion { get; set; }

        public decimal Impuestos { get; set; }

        public decimal MontoTotal { get; set; }

        public int CantNoches { get; set; }

        public decimal PrecioPorNoche { get; set; }
    }
    // ──────────────────────────────────────────────────────────
    public class ResumenCheckOutBE
    {
        public int IdReserva { get; set; }
        public ReservaBE Reserva { get; set; }
       
        public decimal MontoHabitacion { get; set; }
       
        public decimal Impuestos { get; set; }
        public decimal MontoTotal { get; set; }
    }

    public class PagoBE
    {
        public int IdPago { get; set; }

        public int IdReserva { get; set; }

        public string MetodoPago { get; set; }

        public decimal Monto { get; set; }

        public DateTime FechaHora { get; set; }

        public string Estado { get; set; }

        public string UltimosCuatroTarjeta { get; set; }

        public int? IdUsuarioRegistro { get; set; }
    }

    public class ProveedorBE
    {
        public int IdProveedor { get; set; }
        public string Nombre { get; set; }
        public string Contacto { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
        public string Direccion { get; set; }
        public string CondicionesPago { get; set; }
        public bool Activo { get; set; }
        // Datos para pagarle por transferencia (CUN08 Registrar proveedor nuevo)
        public string CUIT { get; set; }
        public string CBU { get; set; }
        public string Banco { get; set; }
        public DateTime? FechaRegistroPago { get; set; }
        /// <summary>Proveedor nuevo = nunca se le pagó: todavía no tiene sus datos bancarios registrados.</summary>
        public bool EsNuevo => string.IsNullOrWhiteSpace(CBU) || string.IsNullOrWhiteSpace(CUIT);
        public override string ToString() => Nombre;
    }

    // ──────────────────────────────────────────────────────────
    public class InsumoBE
    {
        public int IdInsumo { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string UnidadMedida { get; set; }
        public decimal StockActual { get; set; }
        public decimal StockMinimo { get; set; }
        public bool Activo { get; set; }
        public string Estado { get; set; }  // 'Critico' o 'Normal' (columna calculada)
        public override string ToString() => $"{Nombre} ({StockActual} {UnidadMedida}) — {Estado}";
    }

    // ──────────────────────────────────────────────────────────
    public class SolicitudCompraBE
    {
        public int IdSolicitud { get; set; }
        public int IdInsumo { get; set; }
        public string NombreInsumo { get; set; }
        public string UnidadMedida { get; set; }
        public decimal CantidadSolicitada { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public string Prioridad { get; set; }  // 'Baja','Normal','Alta','Urgente'
        public decimal? PresupuestoEstimado { get; set; }
        public string Estado { get; set; }  // 'Pendiente','Aprobada','Rechazada','Ordenada'
        public int? IdUsuarioSolicita { get; set; }
        public string NombreUsuarioSolic { get; set; }
        public string NombreUsuarioValida { get; set; }
        public DateTime? FechaValidacion { get; set; }
        public string Observaciones { get; set; }
        public override string ToString() =>
            $"SC-{IdSolicitud:D4} | {NombreInsumo} x{CantidadSolicitada} | {Estado}";
    }

    // ──────────────────────────────────────────────────────────
    public class OrdenCompraBE
    {
        public int IdOrden { get; set; }
        public string NroOrden { get; set; }
        public int? IdSolicitud { get; set; }      // solo órdenes del flujo anterior
        public int? IdPreOrden { get; set; }       // CUN07 → CUN09
        public string NroPreOrden { get; set; }
        public int? IdPagoProveedor { get; set; }  // pago bancario que originó la orden
        public string NroOperacionPago { get; set; }
        public DateTime? FechaPago { get; set; }
        public int IdProveedor { get; set; }
        public string NombreProveedor { get; set; }
        public int IdInsumo { get; set; }
        public string NombreInsumo { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal PrecioTotal { get; set; }
        public int? PlazoDias { get; set; }
        public DateTime FechaEmision { get; set; }
        public string Estado { get; set; }  // 'Emitida' (pagada y enviada), 'Recibida', 'Con diferencia'
        public string DireccionEntrega { get; set; }
        public string EmailProveedor { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public override string ToString() => $"{NroOrden} | {NombreInsumo} | {Estado}";
    }

    // ──────────────────────────────────────────────────────────
    public class RecepcionInsumoBE
    {
        public int IdRecepcion { get; set; }
        public int IdOrden { get; set; }
        public string NroOrden { get; set; }
        public int IdInsumo { get; set; }
        public string NombreInsumo { get; set; }
        public decimal CantidadRecibida { get; set; }
        public DateTime FechaRecepcion { get; set; }
        public string Estado { get; set; }  // 'Aceptado','Con diferencia'
        public string Observaciones { get; set; }
        public int? IdFacturaProveedor { get; set; }
        public string NroFactura { get; set; }
    }
}

