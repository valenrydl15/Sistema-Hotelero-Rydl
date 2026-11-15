using System;
using System.Collections.Generic;
using DAL_HRS_480;
using ENTIDADES_HRS_480;
using SEGURIDAD_HRS_480;
using SERVICIOS_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// CUN14 Auditoría de cambios de insumos (PRODUCTOS_C_BLL del enunciado).
    /// Consulta el historial Insumos_C con filtros (código, nombre, fecha inicial y final)
    /// y ACTIVA una versión anterior (Act = 1), que vuelve a ser la vigente en Insumos.
    /// </summary>
    public class InsumoCBLL
    {
        private readonly InsumoCDAL _dal = new InsumoCDAL();

        public ResultadoOperacion<List<InsumoCBE>> Consultar(FiltroCambiosBE filtro)
        {
            const string OP = "Consulta de bitácora de cambios";
            if (!ControlAcceso.PuedeAuditarCambios()) return MaestrosComun.Denegado<List<InsumoCBE>>(OP);
            filtro ??= new FiltroCambiosBE();
            if (filtro.FechaInicio != null && filtro.FechaFin != null && filtro.FechaInicio.Value.Date > filtro.FechaFin.Value.Date)
                return MaestrosComun.Rechazo<List<InsumoCBE>>(OP, "La fecha inicial no puede ser posterior a la fecha final.");
            try
            {
                var lista = _dal.ObtenerCambios(filtro);
                BitacoraModulo.Registrar(BitacoraModulo.MOD_MAESTROS, BitacoraModulo.CAMBIOS_CONSULTA, 5,
                    $"Filtro: código {(filtro.IdInsumo == null ? "todos" : $"INS-{filtro.IdInsumo:D3}")}, nombre {filtro.Nombre ?? "todos"}, " +
                    $"desde {filtro.FechaInicio:dd/MM/yyyy} hasta {filtro.FechaFin:dd/MM/yyyy}. {lista.Count} registro/s.", "EXITOSO");
                return ResultadoOperacion<List<InsumoCBE>>.Ok(lista, lista.Count == 0 ? "No hay cambios con ese filtro." : $"{lista.Count} registro/s.");
            }
            catch (Exception ex) { return MaestrosComun.Error<List<InsumoCBE>>(OP, ex); }
        }

        public ResultadoOperacion<List<string>> ObtenerNombres()
        {
            if (!ControlAcceso.PuedeAuditarCambios()) return MaestrosComun.Denegado<List<string>>("ver nombres de insumos");
            try { return ResultadoOperacion<List<string>>.Ok(_dal.ObtenerNombres()); }
            catch (Exception ex) { return MaestrosComun.Error<List<string>>("Ver nombres de insumos", ex); }
        }

        /// <summary>Botón ACTIVAR: la versión elegida pasa a ser la vigente (Act = 1) y el insumo toma esos datos.</summary>
        public ResultadoOperacion<InsumoCBE> Activar(int idCambio)
        {
            const string OP = "Activar versión de insumo";
            if (!ControlAcceso.PuedeAuditarCambios()) return MaestrosComun.Denegado<InsumoCBE>(OP);
            try
            {
                var v = _dal.ObtenerCambio(idCambio);
                if (v == null) return MaestrosComun.Rechazo<InsumoCBE>(OP, "La versión elegida no existe.");
                if (v.Act) return MaestrosComun.Rechazo<InsumoCBE>(OP, "Esa versión ya es la vigente (Act = 1).");
                bool dvOk = DigitoVerificador.Consistente("Insumos");
                _dal.ActivarVersion(idCambio, SesionManager.Instancia.UsuarioActual?.IdUsuario);
                v.Act = true;
                BitacoraModulo.Registrar(BitacoraModulo.MOD_MAESTROS, BitacoraModulo.VERSION_ACTIVADA, 2,
                    $"{v.Codigo}: se activó la versión del {v.Fecha:dd/MM/yyyy} {v.Hora:hh\\:mm} (cambio #{v.IdCambio}): " +
                    $"{v.Nombre}, stock {v.StockActual:0.##}, mínimo {v.StockMinimo:0.##}, {(v.Activo ? "activo" : "dado de baja")}.", "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, "Insumos");
                return ResultadoOperacion<InsumoCBE>.Ok(v, $"Se activó la versión del {v.Fecha:dd/MM/yyyy} {v.Hora:hh\\:mm} de {v.Codigo} {v.Nombre}.");
            }
            catch (Exception ex) { return MaestrosComun.Error<InsumoCBE>(OP, ex); }
        }
    }
}
