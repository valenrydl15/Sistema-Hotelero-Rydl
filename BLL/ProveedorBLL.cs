using DAL_HRS_480;
using ENTIDADES_HRS_480;
using SERVICIOS_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// CUN07 Pre-registrar proveedor (Encargado de Compras, extiende a CUN06): proveedor que nunca le vendió al hotel,
    /// con sus datos básicos. CUN10 Registrar proveedor (Gerente de Compras, extiende a CUN09): se completan
    /// CUIT, banco, CBU y titular con lo informado en su cotización, para poder pagarle.
    /// </summary>
    public class ProveedorBLL
    {
        private static readonly string[] TABLAS = { "Proveedores" };

        public ResultadoReposicion<List<ProveedorBE>> ObtenerProveedoresActivos()
        {
            if (!ControlAcceso.PuedeAccederReposicion())
                return ReposicionComun.Denegado<List<ProveedorBE>>("Ver proveedores");
            try
            {
                using var dal = new ReposicionDAL();
                return ResultadoReposicion<List<ProveedorBE>>.Ok(dal.ObtenerProveedoresActivos());
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<List<ProveedorBE>>("Ver proveedores", ex); }
        }

        public ResultadoReposicion<ProveedorBE> ObtenerProveedor(int idProveedor)
        {
            if (!ControlAcceso.PuedeAccederReposicion())
                return ReposicionComun.Denegado<ProveedorBE>("Ver proveedor");
            try
            {
                using var dal = new ReposicionDAL();
                var p = dal.ObtenerProveedorPorId(idProveedor);
                return p == null ? ResultadoReposicion<ProveedorBE>.Fallo("El proveedor no existe.") : ResultadoReposicion<ProveedorBE>.Ok(p);
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<ProveedorBE>("Ver proveedor", ex); }
        }

        // ══════════════════════════════════════════════════════
        // CUN07 Pre-registrar proveedor
        // ══════════════════════════════════════════════════════
        public ResultadoReposicion<ProveedorBE> PreRegistrarProveedor(ProveedorBE datos)
        {
            const string OP = "Pre-registrar proveedor";
            if (!ControlAcceso.PuedePreRegistrarProveedor())
                return ReposicionComun.Denegado<ProveedorBE>(OP);
            if (datos == null) return ReposicionComun.Rechazo<ProveedorBE>(OP, "Cargá los datos del proveedor.");

            string empresa = (datos.Nombre ?? "").Trim(), contacto = (datos.Contacto ?? "").Trim(),
                   telefono = (datos.Telefono ?? "").Trim(), direccion = (datos.Direccion ?? "").Trim(), email = (datos.Email ?? "").Trim();
            if (empresa.Length == 0 || contacto.Length == 0 || telefono.Length == 0 || direccion.Length == 0)
                return ReposicionComun.Rechazo<ProveedorBE>(OP, "Empresa, contacto, teléfono y dirección son obligatorios.");
            if (empresa.Length > 150 || contacto.Length > 100 || telefono.Length > 30 || direccion.Length > 300 || email.Length > 200)
                return ReposicionComun.Rechazo<ProveedorBE>(OP, "Alguno de los datos supera el largo permitido.");
            if (telefono.Count(char.IsDigit) < 6)
                return ReposicionComun.Rechazo<ProveedorBE>(OP, "El teléfono no es válido.");
            if (email.Length > 0 && !email.Contains('@'))
                return ReposicionComun.Rechazo<ProveedorBE>(OP, "El correo no es válido.");

            try
            {
                bool dvOk = DigitoVerificador.Consistente(TABLAS);
                ProveedorBE p;
                using (var dal = new ReposicionDAL())
                {
                    if (dal.ExisteProveedorConNombre(empresa))
                        return ReposicionComun.Rechazo<ProveedorBE>(OP, $"Ya existe un proveedor llamado {empresa}.");
                    int id = dal.InsertarProveedorPreRegistrado(new ProveedorBE
                    { Nombre = empresa, Contacto = contacto, Telefono = telefono, Direccion = direccion, Email = email });
                    p = dal.ObtenerProveedorPorId(id)!;
                }
                BitacoraReposicion.Registrar(BitacoraReposicion.PROVEEDOR_PRE, 4, $"{empresa} (contacto {contacto}, tel. {telefono}).", "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, TABLAS);
                return ResultadoReposicion<ProveedorBE>.Ok(p, $"Proveedor {empresa} pre-registrado: ya le podés pedir cotización.");
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<ProveedorBE>(OP, ex); }
        }

        // ══════════════════════════════════════════════════════
        // CUN10 Registrar proveedor
        // ══════════════════════════════════════════════════════
        public ResultadoReposicion<ProveedorBE> RegistrarProveedor(int idProveedor, string cuit, string banco, string cbu, string titular)
        {
            const string OP = "Registrar proveedor";
            if (!ControlAcceso.PuedeRegistrarProveedor())
                return ReposicionComun.Denegado<ProveedorBE>(OP);

            banco = (banco ?? "").Trim(); titular = (titular ?? "").Trim();
            if (!BancoServicio.ValidarCUIT(cuit))
                return ReposicionComun.Rechazo<ProveedorBE>(OP, $"El CUIT debe tener 11 dígitos (con o sin guiones). Se ingresaron {BancoServicio.SoloDigitos(cuit).Length}.");
            if (!BancoServicio.ValidarCBU(cbu))
                return ReposicionComun.Rechazo<ProveedorBE>(OP, $"El CBU debe tener 22 dígitos numéricos. Se ingresaron {BancoServicio.SoloDigitos(cbu).Length}.");
            if (banco.Length == 0) banco = BancoServicio.BancoDelCBU(cbu);
            if (banco.Length == 0 || titular.Length == 0)
                return ReposicionComun.Rechazo<ProveedorBE>(OP, "El banco y el titular de la cuenta son obligatorios.");
            if (banco.Length > 100 || titular.Length > 150)
                return ReposicionComun.Rechazo<ProveedorBE>(OP, "El banco o el titular superan el largo permitido.");

            try
            {
                bool dvOk = DigitoVerificador.Consistente(TABLAS);
                ProveedorBE? p;
                using (var dal = new ReposicionDAL())
                {
                    p = dal.ObtenerProveedorPorId(idProveedor);
                    if (p == null || !p.Activo)
                        return ReposicionComun.Rechazo<ProveedorBE>(OP, "El proveedor no existe o está inactivo.");
                    if (!p.PreRegistrado)
                        return ReposicionComun.Rechazo<ProveedorBE>(OP, $"{p.Nombre} ya está registrado: no hace falta registrarlo de nuevo.");
                    if (!dal.RegistrarProveedor(new ProveedorBE
                    { IdProveedor = idProveedor, CUIT = BancoServicio.SoloDigitos(cuit), Banco = banco, CBU = BancoServicio.SoloDigitos(cbu), Titular = titular }))
                        return ReposicionComun.Rechazo<ProveedorBE>(OP, $"{p.Nombre} ya fue registrado por otro usuario.");
                    p = dal.ObtenerProveedorPorId(idProveedor)!;
                }
                BitacoraReposicion.Registrar(BitacoraReposicion.PROVEEDOR_REGISTRADO, 3,
                    $"{p.Nombre}: CUIT {p.CUIT}, {p.Banco}, CBU {p.CBU}, titular {p.Titular}.", "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, TABLAS);
                return ResultadoReposicion<ProveedorBE>.Ok(p, $"Proveedor {p.Nombre} registrado: ya se le puede pagar.");
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<ProveedorBE>(OP, ex); }
        }
    }
}
