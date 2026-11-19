using System;
using System.Collections.Generic;
using DAL_HRS_480;
using ENTIDADES_HRS_480;
using SEGURIDAD_HRS_480;
using SERVICIOS_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// CUN13 Maestro de Clientes – ABM (CLIENTES_BLL del enunciado). Los clientes son los huéspedes del hotel.
    /// Valida el rol, los datos y el Dígito Verificador de Huespedes, y registra cada operación en la bitácora.
    /// </summary>
    public class ClienteBLL
    {
        private const string TABLA = "Huespedes";
        private readonly ClienteDAL _dal = new ClienteDAL();

        /// <summary>Botón ACTUALIZAR: trae los clientes activos de la base.</summary>
        public ResultadoOperacion<List<ClienteBE>> ObtenerClientes()
        {
            if (!ControlAcceso.PuedeGestionarClientes()) return MaestrosComun.Denegado<List<ClienteBE>>("ver clientes");
            try
            {
                var lista = _dal.ObtenerClientes();
                PrepararTarjetas(lista);
                return ResultadoOperacion<List<ClienteBE>>.Ok(lista);
            }
            catch (Exception ex) { return MaestrosComun.Error<List<ClienteBE>>("Ver clientes", ex); }
        }

        /// <summary>
        /// Encriptación reversible: la tarjeta está cifrada con AES en la base. Si el usuario tiene el permiso
        /// de datos sensibles (Supervisor) se muestra descifrada y la consulta queda en la bitácora; al resto
        /// (incluido el Administrador) solo se le muestran los últimos 4 dígitos.
        /// </summary>
        private static void PrepararTarjetas(List<ClienteBE> lista)
        {
            bool verCompleta = ControlAcceso.PuedeVerDatosSensibles();
            int conTarjeta = 0, errores = 0;
            foreach (var c in lista)
            {
                if (string.IsNullOrEmpty(c.TarjetaCifrada)) { c.Tarjeta = ""; continue; }
                conTarjeta++;
                try
                {
                    string plano = CifradoReversible.Desencriptar(c.TarjetaCifrada);
                    c.Tarjeta = verCompleta ? CifradoReversible.FormatearTarjeta(plano) : CifradoReversible.EnmascararTarjeta(plano);
                }
                catch
                {
                    c.Tarjeta = "(no se puede descifrar)";
                    errores++;
                }
            }
            if (verCompleta && conTarjeta > 0)
                BitacoraModulo.Registrar(BitacoraModulo.MOD_MAESTROS, BitacoraModulo.CLIENTE_DATOS_SENSIBLES, 2,
                    $"Se mostraron descifradas las tarjetas de {conTarjeta - errores} cliente/s en el Maestro de Clientes.", "EXITOSO");
            if (errores > 0)
                BitacoraModulo.Registrar(BitacoraModulo.MOD_MAESTROS, BitacoraModulo.CLIENTE_ERROR_DESCIFRAR, 1,
                    $"No se pudieron descifrar {errores} tarjeta/s: falta o cambió la clave de cifrado (clave_datos.key).", "FALLIDO");
        }

        public ResultadoOperacion<ClienteBE> Alta(ClienteBE c)
        {
            const string OP = "Alta de cliente";
            if (!ControlAcceso.PuedeGestionarClientes()) return MaestrosComun.Denegado<ClienteBE>(OP);
            string? motivo = MaestrosComun.ValidarCliente(c);
            if (motivo != null) return MaestrosComun.Rechazo<ClienteBE>(OP, motivo);
            try
            {
                bool dvOk = DigitoVerificador.Consistente(TABLA);
                var (existente, activo) = _dal.ObtenerPorDNI(c.DNI);
                if (existente != null && activo)
                    return MaestrosComun.Rechazo<ClienteBE>(OP, $"Ya existe un cliente con DNI {c.DNI} ({existente.Apellido}, {existente.Nombre}).");
                string msg;
                if (existente != null)
                {
                    // Estaba dado de baja: se reactiva con los datos nuevos (el DNI no se repite)
                    c.IdHuesped = existente.IdHuesped;
                    _dal.Modificar(c, reactivar: true);
                    msg = $"El cliente con DNI {c.DNI} estaba dado de baja: se reactivó con los datos cargados.";
                }
                else
                {
                    c.IdHuesped = _dal.Insertar(c);
                    msg = $"Cliente {c.Apellido}, {c.Nombre} dado de alta.";
                }
                c.DesdeXml = false;
                BitacoraModulo.Registrar(BitacoraModulo.MOD_MAESTROS, BitacoraModulo.CLIENTE_ALTA, 4, $"{c} — {msg}", "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, TABLA);
                return ResultadoOperacion<ClienteBE>.Ok(c, msg);
            }
            catch (Exception ex) { return MaestrosComun.Error<ClienteBE>(OP, ex); }
        }

        public ResultadoOperacion<ClienteBE> Modificar(ClienteBE c)
        {
            const string OP = "Modificación de cliente";
            if (!ControlAcceso.PuedeGestionarClientes()) return MaestrosComun.Denegado<ClienteBE>(OP);
            string? motivo = MaestrosComun.ValidarCliente(c);
            if (motivo != null) return MaestrosComun.Rechazo<ClienteBE>(OP, motivo);
            try
            {
                bool dvOk = DigitoVerificador.Consistente(TABLA);
                // Una fila que vino de un XML no tiene Id: se busca el cliente por DNI
                var (enBase, activo) = _dal.ObtenerPorDNI(c.DNI);
                if (enBase == null || !activo)
                    return MaestrosComun.Rechazo<ClienteBE>(OP, $"No hay un cliente activo con DNI {c.DNI} en la base. Para agregarlo usá Añadir.");
                c.IdHuesped = enBase.IdHuesped;
                if (!_dal.Modificar(c))
                    return MaestrosComun.Rechazo<ClienteBE>(OP, "El cliente fue modificado o dado de baja por otro usuario. Actualizá la grilla.");
                c.DesdeXml = false;
                BitacoraModulo.Registrar(BitacoraModulo.MOD_MAESTROS, BitacoraModulo.CLIENTE_MODIF, 4, $"{c} modificado.", "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, TABLA);
                return ResultadoOperacion<ClienteBE>.Ok(c, $"Cliente {c.Apellido}, {c.Nombre} modificado.");
            }
            catch (Exception ex) { return MaestrosComun.Error<ClienteBE>(OP, ex); }
        }

        /// <summary>Baja lógica (el huésped conserva sus reservas e historial).</summary>
        public ResultadoOperacion<ClienteBE> Eliminar(string dni)
        {
            const string OP = "Baja de cliente";
            if (!ControlAcceso.PuedeGestionarClientes()) return MaestrosComun.Denegado<ClienteBE>(OP);
            dni = MaestrosComun.T(dni);
            try
            {
                bool dvOk = DigitoVerificador.Consistente(TABLA);
                var (c, activo) = _dal.ObtenerPorDNI(dni);
                if (c == null || !activo)
                    return MaestrosComun.Rechazo<ClienteBE>(OP, $"No hay un cliente activo con DNI {dni} en la base.");
                if (!_dal.BajaLogica(c.IdHuesped))
                    return MaestrosComun.Rechazo<ClienteBE>(OP, "El cliente ya fue dado de baja por otro usuario.");
                BitacoraModulo.Registrar(BitacoraModulo.MOD_MAESTROS, BitacoraModulo.CLIENTE_BAJA, 3, $"{c} dado de baja (borrado lógico).", "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, TABLA);
                return ResultadoOperacion<ClienteBE>.Ok(c, $"Cliente {c.Apellido}, {c.Nombre} eliminado.");
            }
            catch (Exception ex) { return MaestrosComun.Error<ClienteBE>(OP, ex); }
        }
    }
}
