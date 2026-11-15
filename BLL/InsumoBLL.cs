using System;
using System.Collections.Generic;
using DAL_HRS_480;
using ENTIDADES_HRS_480;
using SERVICIOS_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// Maestro de Insumos (PRODUCTOS_BLL del enunciado): alta, modificación y baja lógica.
    /// Cada operación cambia la tabla Insumos y los TRIGGERS guardan la nueva versión en Insumos_C
    /// (bitácora de cambios, CUN14). Controla el rol, los datos y el DV de Insumos.
    /// </summary>
    public class InsumoBLL
    {
        private const string TABLA = "Insumos";
        private readonly InsumoDAL _dal = new InsumoDAL();

        public ResultadoOperacion<List<InsumoBE>> ObtenerInsumos(bool incluirBajas)
        {
            if (!ControlAcceso.PuedeGestionarInsumos() && !ControlAcceso.PuedeAuditarCambios())
                return MaestrosComun.Denegado<List<InsumoBE>>("ver insumos");
            try { return ResultadoOperacion<List<InsumoBE>>.Ok(_dal.ObtenerInsumos(incluirBajas)); }
            catch (Exception ex) { return MaestrosComun.Error<List<InsumoBE>>("Ver insumos", ex); }
        }

        private string? Validar(InsumoBE i)
        {
            i.Nombre = MaestrosComun.T(i.Nombre); i.Descripcion = MaestrosComun.T(i.Descripcion);
            i.Categoria = MaestrosComun.T(i.Categoria); i.UnidadMedida = MaestrosComun.T(i.UnidadMedida);
            if (i.Nombre.Length < 2 || i.Nombre.Length > 150) return "El nombre debe tener entre 2 y 150 caracteres.";
            if (i.Descripcion.Length > 300) return "La descripción no puede superar los 300 caracteres.";
            if (i.Categoria.Length > 50) return "La categoría no puede superar los 50 caracteres.";
            if (i.UnidadMedida.Length == 0 || i.UnidadMedida.Length > 30) return "Cargá la unidad de medida (máx. 30 caracteres).";
            if (i.StockActual < 0 || i.StockActual > 9999999) return "El stock actual debe estar entre 0 y 9.999.999.";
            if (i.StockMinimo < 0 || i.StockMinimo > 9999999) return "El stock mínimo debe estar entre 0 y 9.999.999.";
            if (_dal.ExisteNombre(i.Nombre, i.IdInsumo)) return $"Ya existe un insumo llamado \"{i.Nombre}\".";
            return null;
        }

        public ResultadoOperacion<InsumoBE> Alta(InsumoBE i)
        {
            const string OP = "Alta de insumo";
            if (!ControlAcceso.PuedeGestionarInsumos()) return MaestrosComun.Denegado<InsumoBE>(OP);
            try
            {
                i.IdInsumo = 0;
                string? motivo = Validar(i);
                if (motivo != null) return MaestrosComun.Rechazo<InsumoBE>(OP, motivo);
                bool dvOk = DigitoVerificador.Consistente(TABLA);
                i.IdInsumo = _dal.Insertar(i);      // TR_Insumos_RegistrarCambio → Insumos_C "Alta" (Act = 1)
                i.Activo = true;
                BitacoraModulo.Registrar(BitacoraModulo.MOD_MAESTROS, BitacoraModulo.INSUMO_ALTA, 4,
                    $"{i.Codigo} {i.Nombre}: stock {i.StockActual:0.##} / mínimo {i.StockMinimo:0.##} {i.UnidadMedida}.", "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, TABLA);
                return ResultadoOperacion<InsumoBE>.Ok(i, $"Insumo {i.Codigo} {i.Nombre} dado de alta.");
            }
            catch (Exception ex) { return MaestrosComun.Error<InsumoBE>(OP, ex); }
        }

        public ResultadoOperacion<InsumoBE> Modificar(InsumoBE i)
        {
            const string OP = "Modificación de insumo";
            if (!ControlAcceso.PuedeGestionarInsumos()) return MaestrosComun.Denegado<InsumoBE>(OP);
            try
            {
                var anterior = _dal.ObtenerPorId(i.IdInsumo);
                if (anterior == null || !anterior.Activo)
                    return MaestrosComun.Rechazo<InsumoBE>(OP, "El insumo no existe o está dado de baja.");
                string? motivo = Validar(i);
                if (motivo != null) return MaestrosComun.Rechazo<InsumoBE>(OP, motivo);
                bool dvOk = DigitoVerificador.Consistente(TABLA);
                if (!_dal.Modificar(i))              // TR_Insumos_RegistrarCambio → Insumos_C "Modificación"
                    return MaestrosComun.Rechazo<InsumoBE>(OP, "El insumo fue dado de baja por otro usuario.");
                i.Activo = true;
                BitacoraModulo.Registrar(BitacoraModulo.MOD_MAESTROS, BitacoraModulo.INSUMO_MODIF, 4,
                    $"{i.Codigo} {anterior.Nombre} → {i.Nombre}; stock {anterior.StockActual:0.##} → {i.StockActual:0.##}; mínimo {anterior.StockMinimo:0.##} → {i.StockMinimo:0.##}.", "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, TABLA);
                return ResultadoOperacion<InsumoBE>.Ok(i, $"Insumo {i.Codigo} {i.Nombre} modificado.");
            }
            catch (Exception ex) { return MaestrosComun.Error<InsumoBE>(OP, ex); }
        }

        /// <summary>Baja lógica: el DELETE lo intercepta el trigger TR_Insumos_BorradoLogico (Activo = 0).</summary>
        public ResultadoOperacion<InsumoBE> Eliminar(int idInsumo)
        {
            const string OP = "Baja de insumo";
            if (!ControlAcceso.PuedeGestionarInsumos()) return MaestrosComun.Denegado<InsumoBE>(OP);
            try
            {
                var i = _dal.ObtenerPorId(idInsumo);
                if (i == null || !i.Activo) return MaestrosComun.Rechazo<InsumoBE>(OP, "El insumo no existe o ya está dado de baja.");
                bool dvOk = DigitoVerificador.Consistente(TABLA);
                if (!_dal.Eliminar(idInsumo))
                    return MaestrosComun.Rechazo<InsumoBE>(OP, "No se pudo dar de baja el insumo.");
                i.Activo = false;
                BitacoraModulo.Registrar(BitacoraModulo.MOD_MAESTROS, BitacoraModulo.INSUMO_BAJA, 3,
                    $"{i.Codigo} {i.Nombre} dado de baja (borrado lógico).", "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, TABLA);
                return ResultadoOperacion<InsumoBE>.Ok(i, $"Insumo {i.Codigo} {i.Nombre} dado de baja. Se puede reactivar.");
            }
            catch (Exception ex) { return MaestrosComun.Error<InsumoBE>(OP, ex); }
        }

        public ResultadoOperacion<InsumoBE> Reactivar(int idInsumo)
        {
            const string OP = "Reactivación de insumo";
            if (!ControlAcceso.PuedeGestionarInsumos()) return MaestrosComun.Denegado<InsumoBE>(OP);
            try
            {
                var i = _dal.ObtenerPorId(idInsumo);
                if (i == null || i.Activo) return MaestrosComun.Rechazo<InsumoBE>(OP, "El insumo no está dado de baja.");
                bool dvOk = DigitoVerificador.Consistente(TABLA);
                if (!_dal.Reactivar(idInsumo)) return MaestrosComun.Rechazo<InsumoBE>(OP, "No se pudo reactivar el insumo.");
                i.Activo = true;
                BitacoraModulo.Registrar(BitacoraModulo.MOD_MAESTROS, BitacoraModulo.INSUMO_MODIF, 4,
                    $"{i.Codigo} {i.Nombre} reactivado.", "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, TABLA);
                return ResultadoOperacion<InsumoBE>.Ok(i, $"Insumo {i.Codigo} {i.Nombre} reactivado.");
            }
            catch (Exception ex) { return MaestrosComun.Error<InsumoBE>(OP, ex); }
        }
    }
}
