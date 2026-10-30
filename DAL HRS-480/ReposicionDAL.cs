using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using ENTIDADES_HRS_480;

namespace DAL_HRS_480
{
    /// <summary>
    /// Acceso a datos del proceso de negocio PN2 "Gestión de reposición de insumos"
    /// (cotización → pre-orden → pago → orden de compra → factura y recepción).
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
        private static string S(object o) => o == DBNull.Value ? "" : o.ToString() ?? "";

        // ══════════════════════════════════════════════════════
        // INSUMOS / STOCK
        // ══════════════════════════════════════════════════════
        public List<InsumoBE> ObtenerInsumos(bool soloActivos = true)
        {
            var lista = new List<InsumoBE>();
            string sql = "SELECT * FROM Insumos" + (soloActivos ? " WHERE Activo = 1" : "") + " ORDER BY Nombre";
            using var cmd = Cmd(sql);
            using var rd = cmd.ExecuteReader();
            while (rd.Read()) lista.Add(MapearInsumo(rd));
            return lista;
        }

        /// <param name="bloquear">Dentro de una transacción, bloquea la fila hasta confirmar (evita pisar el stock).</param>
        public InsumoBE? ObtenerInsumoPorId(int idInsumo, bool bloquear = false)
        {
            string sql = "SELECT * FROM Insumos" + (bloquear && _tx != null ? " WITH (UPDLOCK, ROWLOCK)" : "")
                       + " WHERE IdInsumo = @Id";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@Id", idInsumo);
            using var rd = cmd.ExecuteReader();
            return rd.Read() ? MapearInsumo(rd) : null;
        }

        public decimal? ObtenerUltimoPrecioInsumo(int idInsumo)
        {
            using var cmd = Cmd("SELECT TOP 1 PrecioUnitario FROM OrdenesCompra WHERE IdInsumo = @Id ORDER BY FechaEmision DESC, IdOrden DESC");
            cmd.Parameters.AddWithValue("@Id", idInsumo);
            var r = cmd.ExecuteScalar();
            return r == null || r == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r);
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
            UnidadMedida = S(rd["UnidadMedida"]),
            StockActual = (decimal)rd["StockActual"],
            StockMinimo = (decimal)rd["StockMinimo"],
            Activo = (bool)rd["Activo"],
            Estado = S(rd["Estado"])
        };

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
            const string sql = @"INSERT INTO Notificaciones
                (IdUsuarioDestino, IdProveedorDestino, IdSolicitud, IdOrden, Tipo, Mensaje, Leida)
                VALUES (@U, @P, @S, @O, @T, @M, @L);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@U", V(n.IdUsuarioDestino));
            cmd.Parameters.AddWithValue("@P", V(n.IdProveedorDestino));
            cmd.Parameters.AddWithValue("@S", V(n.IdSolicitud));
            cmd.Parameters.AddWithValue("@O", V(n.IdOrden));
            cmd.Parameters.AddWithValue("@T", n.Tipo);
            cmd.Parameters.AddWithValue("@M", n.Mensaje.Length > 400 ? n.Mensaje.Substring(0, 400) : n.Mensaje);
            cmd.Parameters.AddWithValue("@L", n.Leida);
            return (int)cmd.ExecuteScalar()!;
        }

        public List<NotificacionBE> ObtenerNotificaciones(int idUsuario, bool soloNoLeidas)
        {
            var lista = new List<NotificacionBE>();
            string sql = @"SELECT TOP 200 * FROM Notificaciones WHERE IdUsuarioDestino = @U"
                       + (soloNoLeidas ? " AND Leida = 0" : "") + " ORDER BY Fecha DESC, IdNotificacion DESC";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@U", idUsuario);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new NotificacionBE
                {
                    IdNotificacion = (int)rd["IdNotificacion"],
                    IdUsuarioDestino = idUsuario,
                    IdSolicitud = rd["IdSolicitud"] == DBNull.Value ? (int?)null : (int)rd["IdSolicitud"],
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

        // ══════════════════════════════════════════════════════
        // PROVEEDORES
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
            CUIT = S(rd["CUIT"]),
            CBU = S(rd["CBU"]),
            Banco = S(rd["Banco"]),
            FechaRegistroPago = rd["FechaRegistroPago"] == DBNull.Value ? (DateTime?)null : (DateTime)rd["FechaRegistroPago"]
        };

        /// <summary>CUN08: guarda los datos bancarios del proveedor (pasa a estar registrado para pagos).</summary>
        public bool RegistrarDatosPagoProveedor(ProveedorBE p)
        {
            const string sql = @"UPDATE Proveedores
                SET CUIT = @Cuit, CBU = @Cbu, Banco = @Banco,
                    Email = ISNULL(@Email, Email), Direccion = ISNULL(@Dir, Direccion),
                    CondicionesPago = ISNULL(@Cond, CondicionesPago), FechaRegistroPago = GETDATE()
                WHERE IdProveedor = @Id";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@Cuit", p.CUIT);
            cmd.Parameters.AddWithValue("@Cbu", p.CBU);
            cmd.Parameters.AddWithValue("@Banco", V(string.IsNullOrWhiteSpace(p.Banco) ? null : p.Banco));
            cmd.Parameters.AddWithValue("@Email", V(string.IsNullOrWhiteSpace(p.Email) ? null : p.Email));
            cmd.Parameters.AddWithValue("@Dir", V(string.IsNullOrWhiteSpace(p.Direccion) ? null : p.Direccion));
            cmd.Parameters.AddWithValue("@Cond", V(string.IsNullOrWhiteSpace(p.CondicionesPago) ? null : p.CondicionesPago));
            cmd.Parameters.AddWithValue("@Id", p.IdProveedor);
            return cmd.ExecuteNonQuery() == 1;
        }

        public bool ExistePagoAprobadoAProveedor(int idProveedor)
        {
            using var cmd = Cmd("SELECT COUNT(*) FROM PagosProveedor WHERE IdProveedor = @Id AND Estado = N'Aprobado'");
            cmd.Parameters.AddWithValue("@Id", idProveedor);
            return (int)cmd.ExecuteScalar()! > 0;
        }

        // ══════════════════════════════════════════════════════
        // COTIZACIONES (CUN06)
        // ══════════════════════════════════════════════════════
        public int InsertarCotizacion(CotizacionBE c, int idUsuario)
        {
            const string sql = @"INSERT INTO Cotizaciones (IdInsumo, Cantidad, Estado, Observaciones, IdUsuarioRegistro)
                VALUES (@Ins, @Cant, N'Abierta', @Obs, @Usr);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@Ins", c.IdInsumo);
            cmd.Parameters.AddWithValue("@Cant", c.Cantidad);
            cmd.Parameters.AddWithValue("@Obs", V(string.IsNullOrWhiteSpace(c.Observaciones) ? null : c.Observaciones));
            cmd.Parameters.AddWithValue("@Usr", idUsuario);
            return (int)cmd.ExecuteScalar()!;
        }

        public int InsertarDetalleCotizacion(CotizacionDetalleBE d)
        {
            const string sql = @"INSERT INTO CotizacionesDetalle (IdCotizacion, IdProveedor, PrecioUnitario, PlazoDias)
                VALUES (@Cot, @Prov, @Precio, @Plazo);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@Cot", d.IdCotizacion);
            cmd.Parameters.AddWithValue("@Prov", d.IdProveedor);
            cmd.Parameters.AddWithValue("@Precio", d.PrecioUnitario);
            cmd.Parameters.AddWithValue("@Plazo", d.PlazoDias);
            return (int)cmd.ExecuteScalar()!;
        }

        private const string SelectCotizacion = @"
            SELECT c.*, i.Nombre AS NombreInsumo, i.UnidadMedida, u.NombreUsuario
            FROM Cotizaciones c
            INNER JOIN Insumos  i ON c.IdInsumo = i.IdInsumo
            LEFT  JOIN Usuarios u ON c.IdUsuarioRegistro = u.IdUsuario";

        public CotizacionBE? ObtenerCotizacionPorId(int idCotizacion)
        {
            CotizacionBE? c;
            using (var cmd = Cmd(SelectCotizacion + " WHERE c.IdCotizacion = @Id"))
            {
                cmd.Parameters.AddWithValue("@Id", idCotizacion);
                using var rd = cmd.ExecuteReader();
                c = rd.Read() ? MapearCotizacion(rd) : null;
            }
            if (c != null) c.Detalles = ObtenerDetallesCotizacion(c.IdCotizacion, c.Cantidad);
            return c;
        }

        /// <param name="estado">null = todas.</param>
        public List<CotizacionBE> ObtenerCotizaciones(string? estado = null)
        {
            var lista = new List<CotizacionBE>();
            string sql = SelectCotizacion + (string.IsNullOrEmpty(estado) ? "" : " WHERE c.Estado = @E")
                       + " ORDER BY c.FechaCotizacion DESC, c.IdCotizacion DESC";
            using (var cmd = Cmd(sql))
            {
                if (!string.IsNullOrEmpty(estado)) cmd.Parameters.AddWithValue("@E", estado);
                using var rd = cmd.ExecuteReader();
                while (rd.Read()) lista.Add(MapearCotizacion(rd));
            }
            foreach (var c in lista) c.Detalles = ObtenerDetallesCotizacion(c.IdCotizacion, c.Cantidad);
            return lista;
        }

        public List<CotizacionDetalleBE> ObtenerDetallesCotizacion(int idCotizacion, decimal cantidad)
        {
            var lista = new List<CotizacionDetalleBE>();
            const string sql = @"SELECT d.*, p.Nombre AS NombreProveedor
                FROM CotizacionesDetalle d INNER JOIN Proveedores p ON d.IdProveedor = p.IdProveedor
                WHERE d.IdCotizacion = @Id ORDER BY d.PrecioUnitario, d.PlazoDias";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@Id", idCotizacion);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new CotizacionDetalleBE
                {
                    IdDetalleCotizacion = (int)rd["IdDetalleCotizacion"],
                    IdCotizacion = idCotizacion,
                    IdProveedor = (int)rd["IdProveedor"],
                    NombreProveedor = S(rd["NombreProveedor"]),
                    PrecioUnitario = (decimal)rd["PrecioUnitario"],
                    PlazoDias = (int)rd["PlazoDias"],
                    Subtotal = Math.Round(cantidad * (decimal)rd["PrecioUnitario"], 2)
                });
            if (lista.Count > 0) lista[0].MasConveniente = true;   // ya vienen ordenadas por precio y plazo
            return lista;
        }

        /// <summary>Cambia el estado solo si sigue en <paramref name="estadoEsperado"/>.</summary>
        public bool ActualizarEstadoCotizacion(int idCotizacion, string nuevoEstado, string estadoEsperado)
        {
            using var cmd = Cmd("UPDATE Cotizaciones SET Estado = @N WHERE IdCotizacion = @Id AND Estado = @Esp");
            cmd.Parameters.AddWithValue("@N", nuevoEstado);
            cmd.Parameters.AddWithValue("@Id", idCotizacion);
            cmd.Parameters.AddWithValue("@Esp", estadoEsperado);
            return cmd.ExecuteNonQuery() == 1;
        }

        /// <summary>¿El insumo ya tiene una cotización abierta o una pre-orden sin comprar?</summary>
        public string? ObtenerPedidoAbiertoDeInsumo(int idInsumo)
        {
            const string sql = @"
                SELECT TOP 1 x.Ref FROM (
                    SELECT N'la cotización CT-' + RIGHT(N'0000' + CAST(IdCotizacion AS NVARCHAR(10)), 5) AS Ref
                    FROM Cotizaciones WHERE IdInsumo = @Ins AND Estado = N'Abierta'
                    UNION ALL
                    SELECT N'la pre-orden ' + NroPreOrden FROM PreOrdenesCompra WHERE IdInsumo = @Ins AND Estado = N'Pendiente'
                ) x";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@Ins", idInsumo);
            var r = cmd.ExecuteScalar();
            return r == null || r == DBNull.Value ? null : r.ToString();
        }

        private static CotizacionBE MapearCotizacion(SqlDataReader rd) => new CotizacionBE
        {
            IdCotizacion = (int)rd["IdCotizacion"],
            IdInsumo = (int)rd["IdInsumo"],
            NombreInsumo = S(rd["NombreInsumo"]),
            UnidadMedida = S(rd["UnidadMedida"]),
            Cantidad = (decimal)rd["Cantidad"],
            FechaCotizacion = (DateTime)rd["FechaCotizacion"],
            Estado = S(rd["Estado"]),
            Observaciones = S(rd["Observaciones"]),
            IdUsuarioRegistro = rd["IdUsuarioRegistro"] == DBNull.Value ? (int?)null : (int)rd["IdUsuarioRegistro"],
            NombreUsuario = S(rd["NombreUsuario"])
        };

        // ══════════════════════════════════════════════════════
        // PRE-ÓRDENES DE COMPRA (CUN07)
        // ══════════════════════════════════════════════════════
        /// <summary>Próximo número "PO-00001". Dentro de la transacción toma un bloqueo para no repetir números.</summary>
        public string GenerarNroPreOrden()
        {
            const string sql = @"SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(NroPreOrden, 4, 20) AS INT)), 0) + 1
                                 FROM PreOrdenesCompra WITH (UPDLOCK, HOLDLOCK)";
            using var cmd = Cmd(sql);
            return $"PO-{Convert.ToInt32(cmd.ExecuteScalar()):D5}";
        }

        public int InsertarPreOrden(PreOrdenCompraBE p, int idUsuario)
        {
            const string sql = @"INSERT INTO PreOrdenesCompra
                (NroPreOrden, IdCotizacion, IdDetalleCotizacion, IdInsumo, IdProveedor, Cantidad, PrecioUnitario,
                 PlazoDias, Prioridad, Estado, Observaciones, IdUsuarioRegistro)
                VALUES (@Nro, @Cot, @Det, @Ins, @Prov, @Cant, @Precio, @Plazo, @Prio, N'Pendiente', @Obs, @Usr);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@Nro", p.NroPreOrden);
            cmd.Parameters.AddWithValue("@Cot", p.IdCotizacion);
            cmd.Parameters.AddWithValue("@Det", p.IdDetalleCotizacion);
            cmd.Parameters.AddWithValue("@Ins", p.IdInsumo);
            cmd.Parameters.AddWithValue("@Prov", p.IdProveedor);
            cmd.Parameters.AddWithValue("@Cant", p.Cantidad);
            cmd.Parameters.AddWithValue("@Precio", p.PrecioUnitario);
            cmd.Parameters.AddWithValue("@Plazo", p.PlazoDias);
            cmd.Parameters.AddWithValue("@Prio", p.Prioridad);
            cmd.Parameters.AddWithValue("@Obs", V(string.IsNullOrWhiteSpace(p.Observaciones) ? null : p.Observaciones));
            cmd.Parameters.AddWithValue("@Usr", idUsuario);
            return (int)cmd.ExecuteScalar()!;
        }

        private const string SelectPreOrden = @"
            SELECT po.*, i.Nombre AS NombreInsumo, i.UnidadMedida, p.Nombre AS NombreProveedor,
                   p.CBU, p.CUIT, u.NombreUsuario
            FROM PreOrdenesCompra po
            INNER JOIN Insumos     i ON po.IdInsumo    = i.IdInsumo
            INNER JOIN Proveedores p ON po.IdProveedor = p.IdProveedor
            LEFT  JOIN Usuarios    u ON po.IdUsuarioRegistro = u.IdUsuario";

        public PreOrdenCompraBE? ObtenerPreOrdenPorId(int idPreOrden)
        {
            using var cmd = Cmd(SelectPreOrden + " WHERE po.IdPreOrden = @Id");
            cmd.Parameters.AddWithValue("@Id", idPreOrden);
            using var rd = cmd.ExecuteReader();
            return rd.Read() ? MapearPreOrden(rd) : null;
        }

        /// <summary>Pendientes: primero las más urgentes y, dentro de cada prioridad, las más antiguas.</summary>
        public List<PreOrdenCompraBE> ObtenerPreOrdenes(string? estado = null)
        {
            var lista = new List<PreOrdenCompraBE>();
            string sql = SelectPreOrden + (string.IsNullOrEmpty(estado) ? "" : " WHERE po.Estado = @E") + @"
                ORDER BY CASE po.Prioridad WHEN N'Urgente' THEN 1 WHEN N'Alta' THEN 2 WHEN N'Normal' THEN 3 ELSE 4 END,
                         po.FechaPreOrden, po.IdPreOrden";
            using var cmd = Cmd(sql);
            if (!string.IsNullOrEmpty(estado)) cmd.Parameters.AddWithValue("@E", estado);
            using var rd = cmd.ExecuteReader();
            while (rd.Read()) lista.Add(MapearPreOrden(rd));
            return lista;
        }

        public bool ActualizarEstadoPreOrden(int idPreOrden, string nuevoEstado, string estadoEsperado)
        {
            using var cmd = Cmd("UPDATE PreOrdenesCompra SET Estado = @N WHERE IdPreOrden = @Id AND Estado = @Esp");
            cmd.Parameters.AddWithValue("@N", nuevoEstado);
            cmd.Parameters.AddWithValue("@Id", idPreOrden);
            cmd.Parameters.AddWithValue("@Esp", estadoEsperado);
            return cmd.ExecuteNonQuery() == 1;
        }

        private static PreOrdenCompraBE MapearPreOrden(SqlDataReader rd) => new PreOrdenCompraBE
        {
            IdPreOrden = (int)rd["IdPreOrden"],
            NroPreOrden = S(rd["NroPreOrden"]),
            IdCotizacion = (int)rd["IdCotizacion"],
            IdDetalleCotizacion = (int)rd["IdDetalleCotizacion"],
            IdInsumo = (int)rd["IdInsumo"],
            NombreInsumo = S(rd["NombreInsumo"]),
            UnidadMedida = S(rd["UnidadMedida"]),
            IdProveedor = (int)rd["IdProveedor"],
            NombreProveedor = S(rd["NombreProveedor"]),
            Cantidad = (decimal)rd["Cantidad"],
            PrecioUnitario = (decimal)rd["PrecioUnitario"],
            PrecioTotal = (decimal)rd["PrecioTotal"],
            PlazoDias = (int)rd["PlazoDias"],
            Prioridad = S(rd["Prioridad"]),
            FechaPreOrden = (DateTime)rd["FechaPreOrden"],
            Estado = S(rd["Estado"]),
            Observaciones = S(rd["Observaciones"]),
            IdUsuarioRegistro = rd["IdUsuarioRegistro"] == DBNull.Value ? (int?)null : (int)rd["IdUsuarioRegistro"],
            NombreUsuario = S(rd["NombreUsuario"]),
            ProveedorNuevo = string.IsNullOrWhiteSpace(S(rd["CBU"])) || string.IsNullOrWhiteSpace(S(rd["CUIT"]))
        };

        // ══════════════════════════════════════════════════════
        // PAGOS A PROVEEDORES (CUN09)
        // ══════════════════════════════════════════════════════
        public int InsertarPagoProveedor(PagoProveedorBE p, int idUsuario)
        {
            const string sql = @"INSERT INTO PagosProveedor
                (IdPreOrden, IdProveedor, Monto, MedioPago, CBUDestino, NroOperacion, Estado, DetalleBanco, IdUsuarioRegistro)
                VALUES (@Pre, @Prov, @Monto, @Medio, @Cbu, @Nro, @Est, @Det, @Usr);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@Pre", p.IdPreOrden);
            cmd.Parameters.AddWithValue("@Prov", p.IdProveedor);
            cmd.Parameters.AddWithValue("@Monto", p.Monto);
            cmd.Parameters.AddWithValue("@Medio", p.MedioPago);
            cmd.Parameters.AddWithValue("@Cbu", p.CBUDestino);
            cmd.Parameters.AddWithValue("@Nro", V(string.IsNullOrWhiteSpace(p.NroOperacion) ? null : p.NroOperacion));
            cmd.Parameters.AddWithValue("@Est", p.Estado);
            cmd.Parameters.AddWithValue("@Det", V(p.DetalleBanco.Length > 200 ? p.DetalleBanco.Substring(0, 200) : p.DetalleBanco));
            cmd.Parameters.AddWithValue("@Usr", idUsuario);
            return (int)cmd.ExecuteScalar()!;
        }

        // ══════════════════════════════════════════════════════
        // FACTURAS DE PROVEEDOR (CUN10)
        // ══════════════════════════════════════════════════════
        public bool ExisteFacturaProveedor(int idProveedor, string nroFactura)
        {
            using var cmd = Cmd("SELECT COUNT(*) FROM FacturasProveedor WHERE IdProveedor = @P AND NroFactura = @N");
            cmd.Parameters.AddWithValue("@P", idProveedor);
            cmd.Parameters.AddWithValue("@N", nroFactura);
            return (int)cmd.ExecuteScalar()! > 0;
        }

        public int InsertarFacturaProveedor(FacturaProveedorBE f, int idUsuario)
        {
            const string sql = @"INSERT INTO FacturasProveedor
                (IdOrden, IdProveedor, NroFactura, FechaFactura, IdInsumo, CantidadFacturada, PrecioUnitario, IdUsuarioRegistro)
                VALUES (@Ord, @Prov, @Nro, @Fecha, @Ins, @Cant, @Precio, @Usr);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@Ord", f.IdOrden);
            cmd.Parameters.AddWithValue("@Prov", f.IdProveedor);
            cmd.Parameters.AddWithValue("@Nro", f.NroFactura);
            cmd.Parameters.AddWithValue("@Fecha", f.FechaFactura.Date);
            cmd.Parameters.AddWithValue("@Ins", f.IdInsumo);
            cmd.Parameters.AddWithValue("@Cant", f.CantidadFacturada);
            cmd.Parameters.AddWithValue("@Precio", f.PrecioUnitario);
            cmd.Parameters.AddWithValue("@Usr", idUsuario);
            return (int)cmd.ExecuteScalar()!;
        }

        // ══════════════════════════════════════════════════════
        // ÓRDENES DE COMPRA
        // ══════════════════════════════════════════════════════
        private const string SelectOrden = @"
            SELECT oc.*, p.Nombre AS NombreProveedor, p.Email AS EmailProveedor, i.Nombre AS NombreInsumo,
                   po.NroPreOrden, pg.NroOperacion AS NroOperacionPago, pg.FechaPago
            FROM OrdenesCompra oc
            INNER JOIN Proveedores p ON oc.IdProveedor = p.IdProveedor
            INNER JOIN Insumos     i ON oc.IdInsumo    = i.IdInsumo
            LEFT  JOIN PreOrdenesCompra po ON oc.IdPreOrden      = po.IdPreOrden
            LEFT  JOIN PagosProveedor   pg ON oc.IdPagoProveedor = pg.IdPagoProveedor";

        /// <summary>Próximo número "OC-00001". Dentro de la transacción toma un bloqueo para no repetir números.</summary>
        public string GenerarNroOrden()
        {
            const string sql = @"SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(NroOrden, 4, 20) AS INT)), 0) + 1
                                 FROM OrdenesCompra WITH (UPDLOCK, HOLDLOCK)";
            using var cmd = Cmd(sql);
            int nro = Convert.ToInt32(cmd.ExecuteScalar());
            return $"OC-{nro:D5}";
        }

        public int InsertarOrden(OrdenCompraBE o, int idUsuario)
        {
            const string sql = @"INSERT INTO OrdenesCompra
                (NroOrden, IdPreOrden, IdPagoProveedor, IdProveedor, IdInsumo, Cantidad, PrecioUnitario, PlazoDias, Estado,
                 DireccionEntrega, IdUsuarioRegistro)
                VALUES (@Nro, @Pre, @Pago, @Prov, @Ins, @Cant, @Precio, @Plazo, N'Emitida', @Dir, @Usr);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@Nro", o.NroOrden);
            cmd.Parameters.AddWithValue("@Pre", V(o.IdPreOrden));
            cmd.Parameters.AddWithValue("@Pago", V(o.IdPagoProveedor));
            cmd.Parameters.AddWithValue("@Prov", o.IdProveedor);
            cmd.Parameters.AddWithValue("@Ins", o.IdInsumo);
            cmd.Parameters.AddWithValue("@Cant", o.Cantidad);
            cmd.Parameters.AddWithValue("@Precio", o.PrecioUnitario);
            cmd.Parameters.AddWithValue("@Plazo", V(o.PlazoDias));
            cmd.Parameters.AddWithValue("@Dir", V(o.DireccionEntrega));
            cmd.Parameters.AddWithValue("@Usr", idUsuario);
            return (int)cmd.ExecuteScalar()!;
        }

        public void MarcarOrdenEnviada(int idOrden)
        {
            using var cmd = Cmd("UPDATE OrdenesCompra SET FechaEnvio = GETDATE() WHERE IdOrden = @Id");
            cmd.Parameters.AddWithValue("@Id", idOrden);
            cmd.ExecuteNonQuery();
        }

        public OrdenCompraBE? ObtenerOrdenPorId(int idOrden)
        {
            using var cmd = Cmd(SelectOrden + " WHERE oc.IdOrden = @Id");
            cmd.Parameters.AddWithValue("@Id", idOrden);
            using var rd = cmd.ExecuteReader();
            return rd.Read() ? MapearOrden(rd) : null;
        }

        public List<OrdenCompraBE> ObtenerOrdenes(string? estado = null)
        {
            var lista = new List<OrdenCompraBE>();
            string sql = SelectOrden + (string.IsNullOrEmpty(estado) ? "" : " WHERE oc.Estado = @E")
                       + " ORDER BY oc.FechaEmision DESC, oc.IdOrden DESC";
            using var cmd = Cmd(sql);
            if (!string.IsNullOrEmpty(estado)) cmd.Parameters.AddWithValue("@E", estado);
            using var rd = cmd.ExecuteReader();
            while (rd.Read()) lista.Add(MapearOrden(rd));
            return lista;
        }

        /// <summary>Cambia el estado de la orden; si se indica <paramref name="estadoEsperado"/>, solo si sigue en él.</summary>
        public bool ActualizarEstadoOrden(int idOrden, string nuevoEstado, string? estadoEsperado = null)
        {
            string sql = "UPDATE OrdenesCompra SET Estado = @N WHERE IdOrden = @Id"
                       + (estadoEsperado != null ? " AND Estado = @Esp" : "");
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@N", nuevoEstado);
            cmd.Parameters.AddWithValue("@Id", idOrden);
            if (estadoEsperado != null) cmd.Parameters.AddWithValue("@Esp", estadoEsperado);
            return cmd.ExecuteNonQuery() == 1;
        }

        private static OrdenCompraBE MapearOrden(SqlDataReader rd) => new OrdenCompraBE
        {
            IdOrden = (int)rd["IdOrden"],
            NroOrden = S(rd["NroOrden"]),
            IdSolicitud = rd["IdSolicitud"] == DBNull.Value ? (int?)null : (int)rd["IdSolicitud"],
            IdPreOrden = rd["IdPreOrden"] == DBNull.Value ? (int?)null : (int)rd["IdPreOrden"],
            NroPreOrden = S(rd["NroPreOrden"]),
            IdPagoProveedor = rd["IdPagoProveedor"] == DBNull.Value ? (int?)null : (int)rd["IdPagoProveedor"],
            NroOperacionPago = S(rd["NroOperacionPago"]),
            FechaPago = rd["FechaPago"] == DBNull.Value ? (DateTime?)null : (DateTime)rd["FechaPago"],
            IdProveedor = (int)rd["IdProveedor"],
            NombreProveedor = S(rd["NombreProveedor"]),
            IdInsumo = (int)rd["IdInsumo"],
            NombreInsumo = S(rd["NombreInsumo"]),
            Cantidad = (decimal)rd["Cantidad"],
            PrecioUnitario = (decimal)rd["PrecioUnitario"],
            PrecioTotal = (decimal)rd["PrecioTotal"],
            PlazoDias = rd["PlazoDias"] == DBNull.Value ? (int?)null : (int)rd["PlazoDias"],
            FechaEmision = (DateTime)rd["FechaEmision"],
            Estado = S(rd["Estado"]),
            DireccionEntrega = S(rd["DireccionEntrega"]),
            EmailProveedor = S(rd["EmailProveedor"]),
            FechaEnvio = rd["FechaEnvio"] == DBNull.Value ? (DateTime?)null : (DateTime)rd["FechaEnvio"]
        };

        // ══════════════════════════════════════════════════════
        // RECEPCIONES
        // ══════════════════════════════════════════════════════
        public int InsertarRecepcion(RecepcionInsumoBE r, int idUsuario)
        {
            const string sql = @"INSERT INTO RecepcionesInsumos
                (IdOrden, IdInsumo, CantidadRecibida, Estado, Observaciones, IdFacturaProveedor, IdUsuarioRegistro)
                VALUES (@Ord, @Ins, @Cant, @Est, @Obs, @Fac, @Usr);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@Ord", r.IdOrden);
            cmd.Parameters.AddWithValue("@Ins", r.IdInsumo);
            cmd.Parameters.AddWithValue("@Cant", r.CantidadRecibida);
            cmd.Parameters.AddWithValue("@Est", r.Estado);
            cmd.Parameters.AddWithValue("@Obs", V(string.IsNullOrWhiteSpace(r.Observaciones) ? null : r.Observaciones));
            cmd.Parameters.AddWithValue("@Fac", V(r.IdFacturaProveedor));
            cmd.Parameters.AddWithValue("@Usr", idUsuario);
            return (int)cmd.ExecuteScalar()!;
        }

        public decimal ObtenerCantidadRecibidaPorOrden(int idOrden)
        {
            using var cmd = Cmd("SELECT ISNULL(SUM(CantidadRecibida), 0) FROM RecepcionesInsumos WHERE IdOrden = @Id");
            cmd.Parameters.AddWithValue("@Id", idOrden);
            return Convert.ToDecimal(cmd.ExecuteScalar());
        }

        public List<RecepcionInsumoBE> ObtenerRecepciones()
        {
            var lista = new List<RecepcionInsumoBE>();
            const string sql = @"SELECT TOP 200 r.*, o.NroOrden, i.Nombre AS NombreInsumo, f.NroFactura
                FROM RecepcionesInsumos r
                INNER JOIN OrdenesCompra o ON r.IdOrden = o.IdOrden
                INNER JOIN Insumos i ON r.IdInsumo = i.IdInsumo
                LEFT  JOIN FacturasProveedor f ON r.IdFacturaProveedor = f.IdFacturaProveedor
                ORDER BY r.FechaRecepcion DESC, r.IdRecepcion DESC";
            using var cmd = Cmd(sql);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new RecepcionInsumoBE
                {
                    IdRecepcion = (int)rd["IdRecepcion"],
                    IdOrden = (int)rd["IdOrden"],
                    NroOrden = S(rd["NroOrden"]),
                    IdInsumo = (int)rd["IdInsumo"],
                    NombreInsumo = S(rd["NombreInsumo"]),
                    CantidadRecibida = (decimal)rd["CantidadRecibida"],
                    FechaRecepcion = (DateTime)rd["FechaRecepcion"],
                    Estado = S(rd["Estado"]),
                    Observaciones = S(rd["Observaciones"]),
                    IdFacturaProveedor = rd["IdFacturaProveedor"] == DBNull.Value ? (int?)null : (int)rd["IdFacturaProveedor"],
                    NroFactura = S(rd["NroFactura"])
                });
            return lista;
        }
    }
}
