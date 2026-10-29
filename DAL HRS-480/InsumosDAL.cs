using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ENTIDADES_HRS_480;
using Microsoft.Data.SqlClient;

namespace DAL_HRS_480
{
    /// <summary>OBSOLETO: reemplazado por ReposicionDAL (transaccional y con Dígito Verificador). No usar.</summary>
    [Obsolete("Usar ReposicionDAL")]
    public class InsumosDAL
    {
        public List<InsumoBE> ObtenerTodos(bool soloCriticos = false)
        {
            var lista = new List<InsumoBE>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            string sql = "SELECT * FROM Insumos WHERE Activo=1";
            if (soloCriticos) sql += " AND Estado='Critico'";
            sql += " ORDER BY Nombre";
            using var cmd = new SqlCommand(sql, con);
            using var rd = cmd.ExecuteReader();
            while (rd.Read()) lista.Add(MapearInsumo(rd));
            return lista;
        }

        public InsumoBE ObtenerPorId(int id)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand("SELECT * FROM Insumos WHERE IdInsumo=@Id", con);
            cmd.Parameters.AddWithValue("@Id", id);
            using var rd = cmd.ExecuteReader();
            return rd.Read() ? MapearInsumo(rd) : null;
        }

        public int InsertarInsumo(InsumoBE i)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = @"
                INSERT INTO Insumos (Nombre, Descripcion, UnidadMedida, StockActual, StockMinimo)
                VALUES (@Nombre, @Desc, @Unidad, @StockActual, @StockMin);
                SELECT SCOPE_IDENTITY();";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Nombre", i.Nombre);
            cmd.Parameters.AddWithValue("@Desc", (object)i.Descripcion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Unidad", i.UnidadMedida);
            cmd.Parameters.AddWithValue("@StockActual", i.StockActual);
            cmd.Parameters.AddWithValue("@StockMin", i.StockMinimo);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void ActualizarStock(int idInsumo, decimal nuevaCantidad)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(
                "UPDATE Insumos SET StockActual=@Stock WHERE IdInsumo=@Id", con);
            cmd.Parameters.AddWithValue("@Stock", nuevaCantidad);
            cmd.Parameters.AddWithValue("@Id", idInsumo);
            cmd.ExecuteNonQuery();
        }

        private static InsumoBE MapearInsumo(SqlDataReader rd) => new InsumoBE
        {
            IdInsumo = (int)rd["IdInsumo"],
            Nombre = rd["Nombre"].ToString(),
            Descripcion = rd["Descripcion"]?.ToString(),
            UnidadMedida = rd["UnidadMedida"].ToString(),
            StockActual = (decimal)rd["StockActual"],
            StockMinimo = (decimal)rd["StockMinimo"],
            Activo = (bool)rd["Activo"],
            Estado = rd["Estado"].ToString()
        };

        // ══════════════════════════════════════════════════════
        // PROVEEDORES
        // ══════════════════════════════════════════════════════
        public List<ProveedorBE> ObtenerProveedores()
        {
            var lista = new List<ProveedorBE>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(
                "SELECT * FROM Proveedores WHERE Activo=1 ORDER BY Nombre", con);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new ProveedorBE
                {
                    IdProveedor = (int)rd["IdProveedor"],
                    Nombre = rd["Nombre"].ToString(),
                    Contacto = rd["Contacto"]?.ToString(),
                    Telefono = rd["Telefono"]?.ToString(),
                    Email = rd["Email"]?.ToString(),
                    Direccion = rd["Direccion"]?.ToString(),
                    CondicionesPago = rd["CondicionesPago"]?.ToString(),
                    Activo = (bool)rd["Activo"]
                });
            return lista;
        }

        public int InsertarProveedor(ProveedorBE p)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = @"
                INSERT INTO Proveedores (Nombre, Contacto, Telefono, Email, Direccion, CondicionesPago)
                VALUES (@Nombre, @Contacto, @Tel, @Email, @Dir, @Cond);
                SELECT SCOPE_IDENTITY();";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Nombre", p.Nombre);
            cmd.Parameters.AddWithValue("@Contacto", (object)p.Contacto ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Tel", (object)p.Telefono ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", (object)p.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Dir", (object)p.Direccion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Cond", (object)p.CondicionesPago ?? DBNull.Value);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // ══════════════════════════════════════════════════════
        // SOLICITUDES DE COMPRA
        // ══════════════════════════════════════════════════════
        public List<SolicitudCompraBE> ObtenerSolicitudes(string estado = null)
        {
            var lista = new List<SolicitudCompraBE>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            string sql = @"
                SELECT sc.*, i.Nombre AS NombreInsumo, i.UnidadMedida,
                       us.NombreUsuario AS NombreUsuarioSolic,
                       uv.NombreUsuario AS NombreUsuarioValida
                FROM SolicitudesCompra sc
                INNER JOIN Insumos i ON sc.IdInsumo = i.IdInsumo
                LEFT JOIN Usuarios us ON sc.IdUsuarioSolicita = us.IdUsuario
                LEFT JOIN Usuarios uv ON sc.IdUsuarioValida   = uv.IdUsuario
                WHERE 1=1";
            if (!string.IsNullOrEmpty(estado)) sql += " AND sc.Estado=@Estado";
            sql += " ORDER BY sc.FechaSolicitud DESC";

            using var cmd = new SqlCommand(sql, con);
            if (!string.IsNullOrEmpty(estado))
                cmd.Parameters.AddWithValue("@Estado", estado);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(MapearSolicitud(rd));
            return lista;
        }

        public int InsertarSolicitud(SolicitudCompraBE s, int idUsuario)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = @"
                INSERT INTO SolicitudesCompra
                    (IdInsumo, CantidadSolicitada, Prioridad, PresupuestoEstimado,
                     IdUsuarioSolicita, Observaciones)
                VALUES
                    (@IdInsumo, @Cantidad, @Prioridad, @Presupuesto, @IdUsuario, @Obs);
                SELECT SCOPE_IDENTITY();";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@IdInsumo", s.IdInsumo);
            cmd.Parameters.AddWithValue("@Cantidad", s.CantidadSolicitada);
            cmd.Parameters.AddWithValue("@Prioridad", s.Prioridad);
            cmd.Parameters.AddWithValue("@Presupuesto", (object)s.PresupuestoEstimado ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
            cmd.Parameters.AddWithValue("@Obs", (object)s.Observaciones ?? DBNull.Value);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void ValidarSolicitud(int idSolicitud, string estado,
                                      decimal presupuesto, int idUsuario)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = @"
                UPDATE SolicitudesCompra
                SET Estado=@Estado, PresupuestoEstimado=@Presupuesto,
                    IdUsuarioValida=@IdUsuario, FechaValidacion=GETDATE()
                WHERE IdSolicitud=@Id";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Estado", estado);
            cmd.Parameters.AddWithValue("@Presupuesto", presupuesto);
            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
            cmd.Parameters.AddWithValue("@Id", idSolicitud);
            cmd.ExecuteNonQuery();
        }

        private static SolicitudCompraBE MapearSolicitud(SqlDataReader rd) => new SolicitudCompraBE
        {
            IdSolicitud = (int)rd["IdSolicitud"],
            IdInsumo = (int)rd["IdInsumo"],
            NombreInsumo = rd["NombreInsumo"].ToString(),
            UnidadMedida = rd["UnidadMedida"].ToString(),
            CantidadSolicitada = (decimal)rd["CantidadSolicitada"],
            FechaSolicitud = (DateTime)rd["FechaSolicitud"],
            Prioridad = rd["Prioridad"].ToString(),
            PresupuestoEstimado = rd["PresupuestoEstimado"] == DBNull.Value
                                    ? (decimal?)null : (decimal)rd["PresupuestoEstimado"],
            Estado = rd["Estado"].ToString(),
            NombreUsuarioSolic = rd["NombreUsuarioSolic"]?.ToString(),
            NombreUsuarioValida = rd["NombreUsuarioValida"]?.ToString(),
            FechaValidacion = rd["FechaValidacion"] == DBNull.Value
                                    ? (DateTime?)null : (DateTime)rd["FechaValidacion"],
            Observaciones = rd["Observaciones"]?.ToString()
        };

        // ══════════════════════════════════════════════════════
        // ÓRDENES DE COMPRA
        // ══════════════════════════════════════════════════════
        public string GenerarNroOrden()
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(
                "SELECT ISNULL(MAX(CAST(SUBSTRING(NroOrden,4,LEN(NroOrden)) AS INT)),0)+1 FROM OrdenesCompra", con);
            int nro = (int)cmd.ExecuteScalar();
            return $"OC-{nro:D5}";
        }

        public int InsertarOrden(OrdenCompraBE o, int idUsuario)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = @"
                INSERT INTO OrdenesCompra
                    (NroOrden, IdSolicitud, IdProveedor, IdInsumo,
                     Cantidad, PrecioUnitario, PlazoDias, IdUsuarioRegistro)
                VALUES
                    (@Nro, @IdSolicitud, @IdProveedor, @IdInsumo,
                     @Cantidad, @Precio, @Plazo, @IdUsuario);
                SELECT SCOPE_IDENTITY();";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Nro", o.NroOrden);
            cmd.Parameters.AddWithValue("@IdSolicitud", o.IdSolicitud);
            cmd.Parameters.AddWithValue("@IdProveedor", o.IdProveedor);
            cmd.Parameters.AddWithValue("@IdInsumo", o.IdInsumo);
            cmd.Parameters.AddWithValue("@Cantidad", o.Cantidad);
            cmd.Parameters.AddWithValue("@Precio", o.PrecioUnitario);
            cmd.Parameters.AddWithValue("@Plazo", (object)o.PlazoDias ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public List<OrdenCompraBE> ObtenerOrdenes(string estado = null)
        {
            var lista = new List<OrdenCompraBE>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            string sql = @"
                SELECT oc.*, p.Nombre AS NombreProveedor, i.Nombre AS NombreInsumo
                FROM OrdenesCompra oc
                INNER JOIN Proveedores p ON oc.IdProveedor = p.IdProveedor
                INNER JOIN Insumos     i ON oc.IdInsumo    = i.IdInsumo
                WHERE 1=1";
            if (!string.IsNullOrEmpty(estado)) sql += " AND oc.Estado=@Estado";
            sql += " ORDER BY oc.FechaEmision DESC";

            using var cmd = new SqlCommand(sql, con);
            if (!string.IsNullOrEmpty(estado))
                cmd.Parameters.AddWithValue("@Estado", estado);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new OrdenCompraBE
                {
                    IdOrden = (int)rd["IdOrden"],
                    NroOrden = rd["NroOrden"].ToString(),
                    IdSolicitud = (int)rd["IdSolicitud"],
                    IdProveedor = (int)rd["IdProveedor"],
                    NombreProveedor = rd["NombreProveedor"].ToString(),
                    IdInsumo = (int)rd["IdInsumo"],
                    NombreInsumo = rd["NombreInsumo"].ToString(),
                    Cantidad = (decimal)rd["Cantidad"],
                    PrecioUnitario = (decimal)rd["PrecioUnitario"],
                    PrecioTotal = (decimal)rd["PrecioTotal"],
                    PlazoDias = rd["PlazoDias"] == DBNull.Value
                                        ? (int?)null : (int)rd["PlazoDias"],
                    FechaEmision = (DateTime)rd["FechaEmision"],
                    Estado = rd["Estado"].ToString()
                });
            return lista;
        }

        public void ActualizarEstadoOrden(int idOrden, string estado)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(
                "UPDATE OrdenesCompra SET Estado=@Estado WHERE IdOrden=@Id", con);
            cmd.Parameters.AddWithValue("@Estado", estado);
            cmd.Parameters.AddWithValue("@Id", idOrden);
            cmd.ExecuteNonQuery();
        }

        // ══════════════════════════════════════════════════════
        // RECEPCIONES DE INSUMOS
        // ══════════════════════════════════════════════════════
        public int InsertarRecepcion(RecepcionInsumoBE r, int idUsuario)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = @"
                INSERT INTO RecepcionesInsumos
                    (IdOrden, IdInsumo, CantidadRecibida, Estado, Observaciones, IdUsuarioRegistro)
                VALUES
                    (@IdOrden, @IdInsumo, @Cantidad, @Estado, @Obs, @IdUsuario);
                SELECT SCOPE_IDENTITY();";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@IdOrden", r.IdOrden);
            cmd.Parameters.AddWithValue("@IdInsumo", r.IdInsumo);
            cmd.Parameters.AddWithValue("@Cantidad", r.CantidadRecibida);
            cmd.Parameters.AddWithValue("@Estado", r.Estado);
            cmd.Parameters.AddWithValue("@Obs", (object)r.Observaciones ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }
}

