using DAL_HRS_480;
using ENTIDADES_HRS_480;
using SERVICIOS_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// Proveedores y CUN08 Registrar proveedor nuevo: si al proveedor elegido nunca se le pagó,
    /// el Encargado de Compras registra sus datos bancarios (CUIT y CBU) antes de pagarle (CUN09).
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

        /// <summary>CUN08: registra CUIT, CBU y banco del proveedor (y completa correo/dirección si se informan).</summary>
        public ResultadoReposicion<ProveedorBE> RegistrarProveedorNuevo(ProveedorBE datos)
        {
            const string OP = "Registrar proveedor nuevo";
            if (!ControlAcceso.PuedeRegistrarProveedor())
                return ReposicionComun.Denegado<ProveedorBE>(OP);
            if (datos == null || datos.IdProveedor <= 0)
                return ReposicionComun.Rechazo<ProveedorBE>(OP, "Seleccioná el proveedor a registrar.");

            string cuit = BancoServicio.SoloDigitos(datos.CUIT);
            string cbu = BancoServicio.SoloDigitos(datos.CBU);
            string email = (datos.Email ?? "").Trim();
            string banco = (datos.Banco ?? "").Trim();
            if (!BancoServicio.ValidarCUIT(cuit))
                return ReposicionComun.Rechazo<ProveedorBE>(OP, "El CUIT no es válido (11 dígitos con dígito verificador correcto).");
            if (!BancoServicio.ValidarCBU(cbu))
                return ReposicionComun.Rechazo<ProveedorBE>(OP, "El CBU no es válido (22 dígitos con dígitos verificadores correctos).");
            if (email.Length > 0 && (!email.Contains('@') || email.Length > 200))
                return ReposicionComun.Rechazo<ProveedorBE>(OP, "El correo del proveedor no es válido.");
            if (banco.Length == 0) banco = BancoServicio.BancoDelCBU(cbu);
            if (banco.Length > 100 || (datos.Direccion ?? "").Length > 300 || (datos.CondicionesPago ?? "").Length > 200)
                return ReposicionComun.Rechazo<ProveedorBE>(OP, "Alguno de los datos supera el largo permitido.");

            try
            {
                bool dvOk = DigitoVerificador.Consistente(TABLAS);
                ProveedorBE? p;
                using (var dal = new ReposicionDAL())
                {
                    p = dal.ObtenerProveedorPorId(datos.IdProveedor);
                    if (p == null || !p.Activo)
                        return ReposicionComun.Rechazo<ProveedorBE>(OP, "El proveedor no existe o está inactivo.");
                    if (!p.EsNuevo)
                        return ReposicionComun.Rechazo<ProveedorBE>(OP,
                            $"{p.Nombre} ya está registrado para pagos (CBU {p.CBU}): no hace falta registrarlo de nuevo.");
                    if (email.Length == 0 && (string.IsNullOrWhiteSpace(p.Email) || !p.Email.Contains('@')))
                        return ReposicionComun.Rechazo<ProveedorBE>(OP, "Cargá el correo del proveedor: la orden de compra se le envía por mail.");

                    dal.RegistrarDatosPagoProveedor(new ProveedorBE
                    {
                        IdProveedor = p.IdProveedor,
                        CUIT = cuit,
                        CBU = cbu,
                        Banco = banco,
                        Email = email,
                        Direccion = (datos.Direccion ?? "").Trim(),
                        CondicionesPago = (datos.CondicionesPago ?? "").Trim()
                    });
                    p = dal.ObtenerProveedorPorId(datos.IdProveedor)!;
                }

                BitacoraReposicion.Registrar(BitacoraReposicion.PROVEEDOR_NUEVO, 3,
                    $"{p.Nombre}: CUIT {p.CUIT}, CBU {p.CBU}{(banco.Length > 0 ? " (" + banco + ")" : "")}.", "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, TABLAS);
                return ResultadoReposicion<ProveedorBE>.Ok(p, $"Proveedor {p.Nombre} registrado: ya se le puede pagar.");
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<ProveedorBE>(OP, ex); }
        }
    }
}
