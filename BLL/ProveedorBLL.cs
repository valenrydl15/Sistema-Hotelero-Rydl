using DAL_HRS_480;
using ENTIDADES_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>CUN09 Seleccionar proveedor (caso de uso incluido en CUN08).</summary>
    public class ProveedorBLL
    {
        public const int PLAZO_MAX_DIAS = 365;

        public ResultadoReposicion<List<ProveedorBE>> ObtenerProveedoresActivos()
        {
            if (!ControlAcceso.PuedeGestionarCompras())
                return ReposicionComun.Denegado<List<ProveedorBE>>("Ver proveedores");
            try
            {
                using var dal = new ReposicionDAL();
                return ResultadoReposicion<List<ProveedorBE>>.Ok(dal.ObtenerProveedoresActivos());
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<List<ProveedorBE>>("Ver proveedores", ex); }
        }

        public ResultadoReposicion<ProveedorSeleccionadoBE> SeleccionarProveedor(int idProveedor, decimal precioUnitario, int plazoDias)
        {
            const string OP = "Seleccionar proveedor";
            if (!ControlAcceso.PuedeGestionarCompras())
                return ReposicionComun.Denegado<ProveedorSeleccionadoBE>(OP);

            if (idProveedor <= 0)
                return ReposicionComun.Rechazo<ProveedorSeleccionadoBE>(OP, "Seleccioná un proveedor.");
            if (precioUnitario <= 0 || precioUnitario > 99_999_999m)
                return ReposicionComun.Rechazo<ProveedorSeleccionadoBE>(OP, "El precio unitario debe ser mayor a cero.");
            if (plazoDias < 0 || plazoDias > PLAZO_MAX_DIAS)
                return ReposicionComun.Rechazo<ProveedorSeleccionadoBE>(OP, $"El plazo de entrega debe estar entre 0 y {PLAZO_MAX_DIAS} días.");

            try
            {
                using var dal = new ReposicionDAL();
                var p = dal.ObtenerProveedorPorId(idProveedor);
                if (p == null || !p.Activo)
                    return ReposicionComun.Rechazo<ProveedorSeleccionadoBE>(OP, "El proveedor no existe o está inactivo.");
                if (string.IsNullOrWhiteSpace(p.Email) || !p.Email.Contains('@'))
                    return ReposicionComun.Rechazo<ProveedorSeleccionadoBE>(OP,
                        $"El proveedor {p.Nombre} no tiene un correo válido: no se le puede enviar la orden.");

                return ResultadoReposicion<ProveedorSeleccionadoBE>.Ok(
                    new ProveedorSeleccionadoBE { Proveedor = p, PrecioUnitario = precioUnitario, PlazoDias = plazoDias });
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<ProveedorSeleccionadoBE>(OP, ex); }
        }
    }
}
