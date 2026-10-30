using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using ENTIDADES_HRS_480;

namespace DAL_HRS_480
{
    /// <summary>
    /// Acceso a datos del PN2 "Gestión de reposición de insumos":
    /// solicitud de cotización → cotización → pre-orden / orden de compra → pago → recepción.
    /// Una instancia = una conexión. Si se llama a Iniciar(), todas las operaciones siguientes
    /// corren en la MISMA transacción hasta Confirmar(); si se destruye sin confirmar, se revierte.
    /// </summary>
    public class ReposicionDAL : IDisposable
    {
        private SqlConnection? _con;
        private SqlTransaction? _tx;

        private SqlConnection Con
        {
            get
            {
                if (_con == null)
                {
                    _con = ConexionDB.ObtenerConexion();
                    _con.Open();
                }
                return _con;
            }
        }

        private SqlCommand Cmd(string sql) => new SqlCommand(sql, Con, _tx);

        public void Iniciar() { if (_tx == null) _tx = Con.BeginTransaction(IsolationLevel.ReadCommitted); }

        public void Confirmar()
        {
            _tx?.Commit();
            _tx?.Dispose();
            _tx = null;
        }

        public void Revertir()
        {
            try { _tx?.Rollback(); } catch { }
            _tx?.Dispose();
            _tx = null;
        }

        public void Dispose()
        {
            if (_tx != null) Revertir();
            _con?.Dispose();
            _con = null;
        }

        private static object V(object? o) => o ?? DBNull.Value;
        private static object Vs(string? s) => string.IsNullOrWhiteSpace(s) ? DBNull.Value : s;
        private static string S(object o) => o == DBNull.Value ? "" : o.ToString() ?? "";
        private static DateTime? D(object o) => o == DBNull.Value ? (DateTime?)null : (DateTime)o;

        /// <summary>Próximo número con prefijo ("SC-00001", "OC-00001"). Dentro de la transacción bloquea para no repetir.</summary>
        private string SiguienteNumero(string tabla, string columna, string prefijo)
        {
            string sql = $@"SELECT ISNULL(MAX(TRY_CAST(SUBSTRING({columna}, {prefijo.Length + 1}, 20) AS INT)), 0) + 1
                            FROM {tabla} WITH (UPDLOCK, HOLDLOCK)";
            using var cmd = Cmd(sql);
            return $"{prefijo}{Convert.ToInt32(cmd.ExecuteScalar()):D5}";
        }

        // ══════════════════════════════════════════════════════
        // INSUMOS / STOCK
        // ══════════════════════════════════════════════════════
        public List<InsumoBE> ObtenerInsumos(bool soloActivos = true)
        {
            var lista = new List<InsumoBE>();
            using var cmd = Cmd("SELECT * FROM Insumos" + (soloActivos ? " WHERE Activo = 1" : "") + " ORDER BY Categoria, Nombre");
            using var rd = cmd.ExecuteReader();
            while (rd.Read()) lista.Add(MapearInsumo(rd));
            return lista;
        }

        /// <param name="bloquear">Dentro de una transacción, bloquea la fila hasta confirmar (evita pisar el stock).</param>
        public InsumoBE? ObtenerInsumoPorId(int idInsumo, bool bloquear = false)
        {
            string sql = "SELECT * FROM Insumos" + (bloquear && _tx != null ? " WITH (UPDLOCK, ROWLOCK)" : "") + " WHERE IdInsumo = @Id";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@Id", idInsumo);
            using var rd = cmd.ExecuteReader();
            return rd.Read() ? MapearInsumo(rd) : null;
        }

        public void ActualizarStockInsumo(int idInsumo, decimal cantidadTotal)
        {
            using var cmd = Cmd("UPDATE Insumos SET StockActual = @S WHERE IdInsumo = @Id");
            cmd.Parameters.AddWithValue("@S", cantidadTotal);
            cmd.Parameters.AddWithValue("@Id", idInsumo);
            cmd.ExecuteNonQuery();
        }

        public int InsertarMovimientoStock(MovimientoStockBE m, int? idUsuario)
        {
            const string sql = @"INSERT INTO MovimientosStock
                (IdInsumo, IdRecepcion, Tipo, CantidadAnterior, CantidadMovimiento, CantidadNueva, IdUsuarioRegistro)
                VALUES (@Ins, @Rec, @Tipo, @Ant, @Mov, @Nue, @Usr);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@Ins", m.IdInsumo);
            cmd.Parameters.AddWithValue("@Rec", V(m.IdRecepcion));
            cmd.Parameters.AddWithValue("@Tipo", m.Tipo);
            cmd.Parameters.AddWithValue("@Ant", m.CantidadAnterior);
            cmd.Parameters.AddWithValue("@Mov", m.CantidadMovimiento);
            cmd.Parameters.AddWithValue("@Nue", m.CantidadNueva);
            cmd.Parameters.AddWithValue("@Usr", V(idUsuario));
            return (int)cmd.ExecuteScalar()!;
        }

        private static InsumoBE MapearInsumo(SqlDataReader rd) => new InsumoBE
        {
            IdInsumo = (int)rd["IdInsumo"],
            Nombre = S(rd["Nombre"]),
            Descripcion = S(rd["Descripcion"]),
            Categoria = S(rd["Categoria"]),
            UnidadMedida = S(rd["UnidadMedida"]),
            StockActual = (decimal)rd["StockActual"],
            StockMinimo = (decimal)rd["StockMinimo"],
            Activo = (bool)rd["Activo"],
            Estado = S(rd["Estado"])
        };

        // ══════════════════════════════════════════════════════
        // PROVEEDORES (CUN07 pre-registrar, CUN10 registrar)
        // ══════════════════════════════════════════════════════
        public List<ProveedorBE> ObtenerProveedoresActivos()
        {
            var lista = new List<ProveedorBE>();
            using var cmd = Cmd("SELECT * FROM Proveedores WHERE Activo = 1 ORDER BY Nombre");
            using var rd = cmd.ExecuteReader();
            while (rd.Read()) lista.Add(MapearProveedor(rd));
            return lista;
        }

        public ProveedorBE? ObtenerProveedorPorId(int idProveedor)
        {
            using var cmd = Cmd("SELECT * FROM Proveedores WHERE IdProveedor = @Id");
            cmd.Parameters.AddWithValue("@Id", idProveedor);
            using var rd = cmd.ExecuteReader();
            return rd.Read() ? MapearProveedor(rd) : null;
        }

        public bool ExisteProveedorConNombre(string nombre)
        {
            using var cmd = Cmd("SELECT COUNT(*) FROM Proveedores WHERE Nombre = @N");
            cmd.Parameters.AddWithValue("@N", nombre);
            return (int)cmd.ExecuteScalar()! > 0;
        }

        public int InsertarProveedorPreRegistrado(ProveedorBE p)
        {
            const string sql = @"INSERT INTO Proveedores (Nombre, Contacto, Telefono, Email, Direccion, CondicionesPago, Activo, Estado)
                VALUES (@Nom, @Con, @Tel, @Mail, @Dir, N'Transferencia', 1, N'Pre-registrado');
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@Nom", p.Nombre);
            cmd.Parameters.AddWithValue("@Con", Vs(p.Contacto));
            cmd.Parameters.AddWithValue("@Tel", Vs(p.Telefono));
            cmd.Parameters.AddWithValue("@Mail", Vs(p.Email));
            cmd.Parameters.AddWithValue("@Dir", Vs(p.Direccion));
            return (int)cmd.ExecuteScalar()!;
        }

        /// <summary>CUN10: completa los datos de pago y pasa el proveedor a "Registrado" (solo si seguía pre-registrado).</summary>
        public bool RegistrarProveedor(ProveedorBE p)
        {
            const string sql = @"UPDATE Proveedores
                SET CUIT = @Cuit, Banco = @Banco, CBU = @Cbu, Titular = @Tit, Estado = N'Registrado', FechaRegistroPago = GETDATE()
                WHERE IdProveedor = @Id AND Estado = N'Pre-registrado'";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@Cuit", p.CUIT);
            cmd.Parameters.AddWithValue("@Banco", p.Banco);
            cmd.Parameters.AddWithValue("@Cbu", p.CBU);
            cmd.Parameters.AddWithValue("@Tit", p.Titular);
            cmd.Parameters.AddWithValue("@Id", p.IdProveedor);
            return cmd.ExecuteNonQuery() == 1;
        }

        private static ProveedorBE MapearProveedor(SqlDataReader rd) => new ProveedorBE
        {
            IdProveedor = (int)rd["IdProveedor"],
            Nombre = S(rd["Nombre"]),
            Contacto = S(rd["Contacto"]),
            Telefono = S(rd["Telefono"]),
            Email = S(rd["Email"]),
            Direccion = S(rd["Direccion"]),
            CondicionesPago = S(rd["CondicionesPago"]),
            Activo = (bool)rd["Activo"],
            Estado = S(rd["Estado"]),
            CUIT = S(rd["CUIT"]),
            Banco = S(rd["Banco"]),
            CBU = S(rd["CBU"]),
            Titular = S(rd["Titular"]),
            FechaRegistroPago = D(rd["FechaRegistroPago"])
        };

        // ══════════════════════════════════════════════════════
        // SOLICITUDES DE COTIZACIÓN (CUN06)
        // ══════════════════════════════════════════════════════
        public string GenerarNroSolicitud() => SiguienteNumero("SolicitudesCotizacion", "NroSolicitud", "SC-");

        public int InsertarSolicitudCotizacion(SolicitudCotizacionBE s, int idUsuario)
        {
            const string sql = @"INSERT INTO SolicitudesCotizacion (NroSolicitud, IdProveedor, Estado, IdUsuarioRegistro)
                VALUES (@Nro, @Prov, N'Enviada', @Usr);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@Nro", s.NroSolicitud);
            cmd.Parameters.AddWithValue("@Prov", s.IdProveedor);
            cmd.Parameters.AddWithValue("@Usr", idUsuario);
            return (int)cmd.ExecuteScalar()!;
        }

        public void InsertarDetalleSolicitud(int idSolicitud, ItemInsumoBE item)
        {
            using var cmd = Cmd("INSERT INTO SolicitudesCotizacionDetalle (IdSolicitudCotizacion, IdInsumo, Cantidad) VALUES (@S, @I, @C)");
            cmd.Parameters.AddWithValue("@S", idSolicitud);
            cmd.Parameters.AddWithValue("@I", item.IdInsumo);
            cmd.Parameters.AddWithValue("@C", item.Cantidad);
            cmd.ExecuteNonQuery();
        }

        public void MarcarSolicitudEnviada(int idSolicitud)
        {
            using var cmd = Cmd("UPDATE SolicitudesCotizacion SET FechaEnvio = GETDATE() WHERE IdSolicitudCotizacion = @Id");
            cmd.Parameters.AddWithValue("@Id", idSolicitud);
            cmd.ExecuteNonQuery();
        }

        public bool ActualizarEstadoSolicitud(int idSolicitud, string nuevo, string esperado)
        {
            using var cmd = Cmd("UPDATE SolicitudesCotizacion SET Estado = @N WHERE IdSolicitudCotizacion = @Id AND Estado = @E");
            cmd.Parameters.AddWithValue("@N", nuevo);
            cmd.Parameters.AddWithValue("@Id", idSolicitud);
            cmd.Parameters.AddWithValue("@E", esperado);
            return cmd.ExecuteNonQuery() == 1;
        }

        public SolicitudCotizacionBE? ObtenerSolicitudPorId(int idSolicitud)
            => ObtenerSolicitudes(null, idSolicitud).FirstOrDefault();

        public List<SolicitudCotizacionBE> ObtenerSolicitudes(string? estado = null, int? idSolicitud = null)
        {
            var lista = new List<SolicitudCotizacionBE>();
            string sql = @"SELECT s.*, p.Nombre AS NombreProveedor, p.Email AS EmailProveedor
                FROM SolicitudesCotizacion s INNER JOIN Proveedores p ON s.IdProveedor = p.IdProveedor
                WHERE (@E IS NULL OR s.Estado = @E) AND (@Id IS NULL OR s.IdSolicitudCotizacion = @Id)
                ORDER BY s.FechaSolicitud DESC, s.IdSolicitudCotizacion DESC";
            using (var cmd = Cmd(sql))
            {
                cmd.Parameters.AddWithValue("@E", Vs(estado));
                cmd.Parameters.AddWithValue("@Id", V(idSolicitud));
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                    lista.Add(new SolicitudCotizacionBE
                    {
                        IdSolicitudCotizacion = (int)rd["IdSolicitudCotizacion"],
                        NroSolicitud = S(rd["NroSolicitud"]),
                        FechaSolicitud = (DateTime)rd["FechaSolicitud"],
                        IdProveedor = (int)rd["IdProveedor"],
                        NombreProveedor = S(rd["NombreProveedor"]),
                        EmailProveedor = S(rd["EmailProveedor"]),
                        Estado = S(rd["Estado"]),
                        FechaEnvio = D(rd["FechaEnvio"])
                    });
            }
            foreach (var s in lista) s.Items = ObtenerItems(
                @"SELECT d.IdDetalleSolicitud AS IdDetalle, d.IdInsumo, i.Nombre, i.UnidadMedida, d.Cantidad, CAST(0 AS DECIMAL(10,2)) AS PrecioUnitario
                  FROM SolicitudesCotizacionDetalle d INNER JOIN Insumos i ON d.IdInsumo = i.IdInsumo
                  WHERE d.IdSolicitudCotizacion = @Id ORDER BY i.Nombre", s.IdSolicitudCotizacion);
            return lista;
        }

        private List<ItemInsumoBE> ObtenerItems(string sql, int id)
        {
            var lista = new List<ItemInsumoBE>();
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@Id", id);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new ItemInsumoBE
                {
                    IdDetalle = (int)rd["IdDetalle"],
                    IdInsumo = (int)rd["IdInsumo"],
                    NombreInsumo = S(rd["Nombre"]),
                    UnidadMedida = S(rd["UnidadMedida"]),
                    Cantidad = (decimal)rd["Cantidad"],
                    PrecioUnitario = (decimal)rd["PrecioUnitario"]
                });
            return lista;
        }

        // ══════════════════════════════════════════════════════
        // COTIZACIONES (CUN08)
        // ══════════════════════════════════════════════════════
        public int InsertarCotizacion(CotizacionBE c, int idUsuario)
        {
            const string sql = @"INSERT INTO Cotizaciones (IdSolicitudCotizacion, IdProveedor, CUIT, Banco, CBU, Titular, Estado, IdUsuarioRegistro)
                VALUES (@Sol, @Prov, @Cuit, @Banco, @Cbu, @Tit, N'Recibida', @Usr);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@Sol", c.IdSolicitudCotizacion);
            cmd.Parameters.AddWithValue("@Prov", c.IdProveedor);
            cmd.Parameters.AddWithValue("@Cuit", c.CUIT);
            cmd.Parameters.AddWithValue("@Banco", c.Banco);
            cmd.Parameters.AddWithValue("@Cbu", c.CBU);
            cmd.Parameters.AddWithValue("@Tit", c.Titular);
            cmd.Parameters.AddWithValue("@Usr", idUsuario);
            return (int)cmd.ExecuteScalar()!;
        }

        public void InsertarDetalleCotizacion(int idCotizacion, ItemInsumoBE item)
        {
            using var cmd = Cmd(@"INSERT INTO CotizacionesDetalle (IdCotizacion, IdInsumo, CantidadDisponible, PrecioUnitario)
                                  VALUES (@C, @I, @Cant, @P)");
            cmd.Parameters.AddWithValue("@C", idCotizacion);
            cmd.Parameters.AddWithValue("@I", item.IdInsumo);
            cmd.Parameters.AddWithValue("@Cant", item.Cantidad);
            cmd.Parameters.AddWithValue("@P", item.PrecioUnitario);
            cmd.ExecuteNonQuery();
        }

        public bool ActualizarEstadoCotizacion(int idCotizacion, string nuevo, string esperado)
        {
            using var cmd = Cmd("UPDATE Cotizaciones SET Estado = @N WHERE IdCotizacion = @Id AND Estado = @E");
            cmd.Parameters.AddWithValue("@N", nuevo);
            cmd.Parameters.AddWithValue("@Id", idCotizacion);
            cmd.Parameters.AddWithValue("@E", esperado);
            return cmd.ExecuteNonQuery() == 1;
        }

        public CotizacionBE? ObtenerCotizacionPorId(int idCotizacion) => ObtenerCotizaciones(null, idCotizacion).FirstOrDefault();

        public List<CotizacionBE> ObtenerCotizaciones(string? estado = null, int? idCotizacion = null)
        {
            var lista = new List<CotizacionBE>();
            const string sql = @"SELECT c.*, s.NroSolicitud, p.Nombre AS NombreProveedor
                FROM Cotizaciones c
                INNER JOIN SolicitudesCotizacion s ON c.IdSolicitudCotizacion = s.IdSolicitudCotizacion
                INNER JOIN Proveedores p ON c.IdProveedor = p.IdProveedor
                WHERE (@E IS NULL OR c.Estado = @E) AND (@Id IS NULL OR c.IdCotizacion = @Id)
                ORDER BY c.FechaCotizacion, c.IdCotizacion";
            using (var cmd = Cmd(sql))
            {
                cmd.Parameters.AddWithValue("@E", Vs(estado));
                cmd.Parameters.AddWithValue("@Id", V(idCotizacion));
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                    lista.Add(new CotizacionBE
                    {
                        IdCotizacion = (int)rd["IdCotizacion"],
                        IdSolicitudCotizacion = (int)rd["IdSolicitudCotizacion"],
                        NroSolicitud = S(rd["NroSolicitud"]),
                        IdProveedor = (int)rd["IdProveedor"],
                        NombreProveedor = S(rd["NombreProveedor"]),
                        FechaCotizacion = (DateTime)rd["FechaCotizacion"],
                        CUIT = S(rd["CUIT"]),
                        Banco = S(rd["Banco"]),
                        CBU = S(rd["CBU"]),
                        Titular = S(rd["Titular"]),
                        Estado = S(rd["Estado"])
                    });
            }
            foreach (var c in lista) c.Items = ObtenerItems(
                @"SELECT d.IdDetalleCotizacion AS IdDetalle, d.IdInsumo, i.Nombre, i.UnidadMedida, d.CantidadDisponible AS Cantidad, d.PrecioUnitario
                  FROM CotizacionesDetalle d INNER JOIN Insumos i ON d.IdInsumo = i.IdInsumo
                  WHERE d.IdCotizacion = @Id ORDER BY i.Nombre", c.IdCotizacion);
            return lista;
        }

        /// <summary>Ofertas de todas las cotizaciones recibidas (sin procesar), con la cantidad que se había solicitado.</summary>
        public List<OfertaBE> ObtenerOfertasPendientes()
        {
            var lista = new List<OfertaBE>();
            const string sql = @"SELECT c.IdCotizacion, c.IdProveedor, p.Nombre AS NombreProveedor, d.IdInsumo, i.Nombre AS NombreInsumo, i.UnidadMedida,
                       sd.Cantidad AS CantidadSolicitada, d.CantidadDisponible, d.PrecioUnitario
                FROM Cotizaciones c
                INNER JOIN CotizacionesDetalle d ON d.IdCotizacion = c.IdCotizacion
                INNER JOIN SolicitudesCotizacionDetalle sd ON sd.IdSolicitudCotizacion = c.IdSolicitudCotizacion AND sd.IdInsumo = d.IdInsumo
                INNER JOIN Proveedores p ON c.IdProveedor = p.IdProveedor
                INNER JOIN Insumos i ON d.IdInsumo = i.IdInsumo
                WHERE c.Estado = N'Recibida'
                ORDER BY i.Nombre, d.PrecioUnitario, d.CantidadDisponible DESC";
            using var cmd = Cmd(sql);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new OfertaBE
                {
                    IdCotizacion = (int)rd["IdCotizacion"],
                    IdProveedor = (int)rd["IdProveedor"],
                    NombreProveedor = S(rd["NombreProveedor"]),
                    IdInsumo = (int)rd["IdInsumo"],
                    NombreInsumo = S(rd["NombreInsumo"]),
                    UnidadMedida = S(rd["UnidadMedida"]),
                    CantidadSolicitada = (decimal)rd["CantidadSolicitada"],
                    CantidadDisponible = (decimal)rd["CantidadDisponible"],
                    PrecioUnitario = (decimal)rd["PrecioUnitario"]
                });
            return lista;
        }

        // ══════════════════════════════════════════════════════
        // ÓRDENES DE COMPRA (CUN08 pre-orden, CUN09 generar/cancelar, CUN11 pagar)
        // ══════════════════════════════════════════════════════
        public string GenerarNroOrden() => SiguienteNumero("OrdenesCompra", "NroOrden", "OC-");

        public int InsertarOrden(OrdenCompraBE o, int idUsuario)
        {
            const string sql = @"INSERT INTO OrdenesCompra (NroOrden, IdProveedor, IdCotizacion, Total, Estado, IdUsuarioRegistro)
                VALUES (@Nro, @Prov, @Cot, @Total, N'Pre-orden', @Usr);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@Nro", o.NroOrden);
            cmd.Parameters.AddWithValue("@Prov", o.IdProveedor);
            cmd.Parameters.AddWithValue("@Cot", o.IdCotizacion);
            cmd.Parameters.AddWithValue("@Total", o.Total);
            cmd.Parameters.AddWithValue("@Usr", idUsuario);
            return (int)cmd.ExecuteScalar()!;
        }

        public void InsertarDetalleOrden(int idOrden, ItemInsumoBE item)
        {
            using var cmd = Cmd("INSERT INTO OrdenesCompraDetalle (IdOrden, IdInsumo, Cantidad, PrecioUnitario) VALUES (@O, @I, @C, @P)");
            cmd.Parameters.AddWithValue("@O", idOrden);
            cmd.Parameters.AddWithValue("@I", item.IdInsumo);
            cmd.Parameters.AddWithValue("@C", item.Cantidad);
            cmd.Parameters.AddWithValue("@P", item.PrecioUnitario);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Cambia el estado solo si la orden sigue en <paramref name="esperado"/> (evita que dos usuarios la procesen a la vez).</summary>
        public bool CambiarEstadoOrden(int idOrden, string nuevo, string esperado, int? idGerente = null, string? motivo = null, string? direccion = null)
        {
            const string sql = @"UPDATE OrdenesCompra
                SET Estado = @N,
                    FechaGeneracion   = CASE WHEN @N = N'Generada' THEN GETDATE() ELSE FechaGeneracion END,
                    IdUsuarioGerente  = ISNULL(@G, IdUsuarioGerente),
                    MotivoCancelacion = ISNULL(@M, MotivoCancelacion),
                    DireccionEntrega  = ISNULL(@Dir, DireccionEntrega)
                WHERE IdOrden = @Id AND Estado = @E";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@N", nuevo);
            cmd.Parameters.AddWithValue("@G", V(idGerente));
            cmd.Parameters.AddWithValue("@M", Vs(motivo));
            cmd.Parameters.AddWithValue("@Dir", Vs(direccion));
            cmd.Parameters.AddWithValue("@Id", idOrden);
            cmd.Parameters.AddWithValue("@E", esperado);
            return cmd.ExecuteNonQuery() == 1;
        }

        public void MarcarOrdenEnviada(int idOrden)
        {
            using var cmd = Cmd("UPDATE OrdenesCompra SET FechaEnvio = GETDATE() WHERE IdOrden = @Id");
            cmd.Parameters.AddWithValue("@Id", idOrden);
            cmd.ExecuteNonQuery();
        }

        public OrdenCompraBE? ObtenerOrdenPorId(int idOrden) => ObtenerOrdenes(null, idOrden).FirstOrDefault();

        public List<OrdenCompraBE> ObtenerOrdenes(string? estado = null, int? idOrden = null)
        {
            var lista = new List<OrdenCompraBE>();
            const string sql = @"SELECT o.*, p.Nombre AS NombreProveedor, p.Email AS EmailProveedor, p.Estado AS EstadoProveedor,
                       pg.NroOperacion, pg.FechaPago
                FROM OrdenesCompra o
                INNER JOIN Proveedores p ON o.IdProveedor = p.IdProveedor
                OUTER APPLY (SELECT TOP 1 x.NroOperacion, x.FechaPago FROM PagosOrdenCompra x
                             WHERE x.IdOrden = o.IdOrden AND x.Estado = N'Aprobado' ORDER BY x.IdPago DESC) pg
                WHERE (@E IS NULL OR o.Estado = @E) AND (@Id IS NULL OR o.IdOrden = @Id)
                ORDER BY o.FechaPreOrden DESC, o.IdOrden DESC";
            using (var cmd = Cmd(sql))
            {
                cmd.Parameters.AddWithValue("@E", Vs(estado));
                cmd.Parameters.AddWithValue("@Id", V(idOrden));
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                    lista.Add(new OrdenCompraBE
                    {
                        IdOrden = (int)rd["IdOrden"],
                        NroOrden = S(rd["NroOrden"]),
                        FechaPreOrden = (DateTime)rd["FechaPreOrden"],
                        FechaGeneracion = D(rd["FechaGeneracion"]),
                        IdProveedor = (int)rd["IdProveedor"],
                        NombreProveedor = S(rd["NombreProveedor"]),
                        EmailProveedor = S(rd["EmailProveedor"]),
                        EstadoProveedor = S(rd["EstadoProveedor"]),
                        IdCotizacion = (int)rd["IdCotizacion"],
                        Total = (decimal)rd["Total"],
                        Estado = S(rd["Estado"]),
                        MotivoCancelacion = S(rd["MotivoCancelacion"]),
                        DireccionEntrega = S(rd["DireccionEntrega"]),
                        FechaEnvio = D(rd["FechaEnvio"]),
                        NroOperacionPago = S(rd["NroOperacion"]),
                        FechaPago = D(rd["FechaPago"])
                    });
            }
            foreach (var o in lista) o.Items = ObtenerItems(
                @"SELECT d.IdDetalleOrden AS IdDetalle, d.IdInsumo, i.Nombre, i.UnidadMedida, d.Cantidad, d.PrecioUnitario
                  FROM OrdenesCompraDetalle d INNER JOIN Insumos i ON d.IdInsumo = i.IdInsumo
                  WHERE d.IdOrden = @Id ORDER BY i.Nombre", o.IdOrden);
            return lista;
        }

        public int InsertarPago(PagoOrdenCompraBE p, int idUsuario)
        {
            const string sql = @"INSERT INTO PagosOrdenCompra (IdOrden, Monto, Banco, CBU, Titular, NroOperacion, Estado, DetalleBanco, IdUsuarioRegistro)
                VALUES (@O, @M, @B, @Cbu, @T, @Nro, @E, @Det, @Usr);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@O", p.IdOrden);
            cmd.Parameters.AddWithValue("@M", p.Monto);
            cmd.Parameters.AddWithValue("@B", p.Banco);
            cmd.Parameters.AddWithValue("@Cbu", p.CBU);
            cmd.Parameters.AddWithValue("@T", p.Titular);
            cmd.Parameters.AddWithValue("@Nro", Vs(p.NroOperacion));
            cmd.Parameters.AddWithValue("@E", p.Estado);
            cmd.Parameters.AddWithValue("@Det", Vs(p.DetalleBanco.Length > 200 ? p.DetalleBanco.Substring(0, 200) : p.DetalleBanco));
            cmd.Parameters.AddWithValue("@Usr", idUsuario);
            return (int)cmd.ExecuteScalar()!;
        }

        // ══════════════════════════════════════════════════════
        // RECEPCIONES (CUN12)
        // ══════════════════════════════════════════════════════
        public bool ExisteFactura(int idProveedor, string nroFactura)
        {
            const string sql = @"SELECT COUNT(*) FROM RecepcionesInsumos r INNER JOIN OrdenesCompra o ON r.IdOrden = o.IdOrden
                                 WHERE o.IdProveedor = @P AND r.NroFactura = @N";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@P", idProveedor);
            cmd.Parameters.AddWithValue("@N", nroFactura);
            return (int)cmd.ExecuteScalar()! > 0;
        }

        public int InsertarRecepcion(RecepcionInsumoBE r, int idUsuario)
        {
            const string sql = @"INSERT INTO RecepcionesInsumos (IdOrden, NroFactura, TotalFactura, Estado, Observaciones, IdUsuarioRegistro)
                VALUES (@O, @F, @T, @E, @Obs, @Usr);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@O", r.IdOrden);
            cmd.Parameters.AddWithValue("@F", r.NroFactura);
            cmd.Parameters.AddWithValue("@T", r.TotalFactura);
            cmd.Parameters.AddWithValue("@E", r.Estado);
            cmd.Parameters.AddWithValue("@Obs", Vs(r.Observaciones));
            cmd.Parameters.AddWithValue("@Usr", idUsuario);
            return (int)cmd.ExecuteScalar()!;
        }

        public void InsertarDetalleRecepcion(int idRecepcion, ItemRecepcionBE item)
        {
            using var cmd = Cmd(@"INSERT INTO RecepcionesInsumosDetalle (IdRecepcion, IdInsumo, CantidadPedida, CantidadRecibida)
                                  VALUES (@R, @I, @P, @C)");
            cmd.Parameters.AddWithValue("@R", idRecepcion);
            cmd.Parameters.AddWithValue("@I", item.IdInsumo);
            cmd.Parameters.AddWithValue("@P", item.CantidadPedida);
            cmd.Parameters.AddWithValue("@C", item.CantidadRecibida);
            cmd.ExecuteNonQuery();
        }

        public List<RecepcionInsumoBE> ObtenerRecepciones()
        {
            var lista = new List<RecepcionInsumoBE>();
            const string sql = @"SELECT TOP 200 r.*, o.NroOrden FROM RecepcionesInsumos r
                                 INNER JOIN OrdenesCompra o ON r.IdOrden = o.IdOrden
                                 ORDER BY r.FechaRecepcion DESC, r.IdRecepcion DESC";
            using var cmd = Cmd(sql);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new RecepcionInsumoBE
                {
                    IdRecepcion = (int)rd["IdRecepcion"],
                    IdOrden = (int)rd["IdOrden"],
                    NroOrden = S(rd["NroOrden"]),
                    NroFactura = S(rd["NroFactura"]),
                    TotalFactura = (decimal)rd["TotalFactura"],
                    FechaRecepcion = (DateTime)rd["FechaRecepcion"],
                    Estado = S(rd["Estado"]),
                    Observaciones = S(rd["Observaciones"])
                });
            return lista;
        }

        // ══════════════════════════════════════════════════════
        // USUARIOS Y NOTIFICACIONES
        // ══════════════════════════════════════════════════════
        public List<Usuario> ObtenerUsuariosPorRol(string nombreRol)
        {
            var lista = new List<Usuario>();
            const string sql = @"SELECT u.IdUsuario, u.Nombre, u.Apellido, u.Email, u.NombreUsuario, u.IdRol, r.Nombre AS NombreRol
                FROM Usuarios u INNER JOIN Roles r ON u.IdRol = r.IdRol
                WHERE r.Nombre = @R AND u.Activo = 1 ORDER BY u.IdUsuario";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@R", nombreRol);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new Usuario
                {
                    IdUsuario = (int)rd["IdUsuario"],
                    Nombre = S(rd["Nombre"]),
                    Apellido = S(rd["Apellido"]),
                    Email = S(rd["Email"]),
                    NombreUsuario = S(rd["NombreUsuario"]),
                    IdRol = (int)rd["IdRol"],
                    NombreRol = S(rd["NombreRol"])
                });
            return lista;
        }

        public int InsertarNotificacion(NotificacionBE n)
        {
            const string sql = @"INSERT INTO Notificaciones (IdUsuarioDestino, IdProveedorDestino, IdOrden, Tipo, Mensaje, Leida)
                VALUES (@U, @P, @O, @T, @M, @L);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@U", V(n.IdUsuarioDestino));
            cmd.Parameters.AddWithValue("@P", V(n.IdProveedorDestino));
            cmd.Parameters.AddWithValue("@O", V(n.IdOrden));
            cmd.Parameters.AddWithValue("@T", n.Tipo);
            cmd.Parameters.AddWithValue("@M", n.Mensaje.Length > 400 ? n.Mensaje.Substring(0, 400) : n.Mensaje);
            cmd.Parameters.AddWithValue("@L", n.Leida);
            return (int)cmd.ExecuteScalar()!;
        }

        public List<NotificacionBE> ObtenerNotificaciones(int idUsuario, bool soloNoLeidas)
        {
            var lista = new List<NotificacionBE>();
            string sql = "SELECT TOP 200 * FROM Notificaciones WHERE IdUsuarioDestino = @U"
                       + (soloNoLeidas ? " AND Leida = 0" : "") + " ORDER BY Fecha DESC, IdNotificacion DESC";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@U", idUsuario);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new NotificacionBE
                {
                    IdNotificacion = (int)rd["IdNotificacion"],
                    IdUsuarioDestino = idUsuario,
                    IdOrden = rd["IdOrden"] == DBNull.Value ? (int?)null : (int)rd["IdOrden"],
                    Tipo = S(rd["Tipo"]),
                    Mensaje = S(rd["Mensaje"]),
                    Fecha = (DateTime)rd["Fecha"],
                    Leida = (bool)rd["Leida"]
                });
            return lista;
        }

        public void MarcarNotificacionesLeidas(int idUsuario)
        {
            using var cmd = Cmd("UPDATE Notificaciones SET Leida = 1 WHERE IdUsuarioDestino = @U AND Leida = 0");
            cmd.Parameters.AddWithValue("@U", idUsuario);
            cmd.ExecuteNonQuery();
        }

        public int ContarNotificacionesNoLeidas(int idUsuario)
        {
            using var cmd = Cmd("SELECT COUNT(*) FROM Notificaciones WHERE IdUsuarioDestino = @U AND Leida = 0");
            cmd.Parameters.AddWithValue("@U", idUsuario);
            return (int)cmd.ExecuteScalar()!;
        }
    }
}
