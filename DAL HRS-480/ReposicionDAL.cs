using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using ENTIDADES_HRS_480;

namespace DAL_HRS_480
{
    /// <summary>
    /// Acceso a datos del proceso de negocio PN2 "Gestión de reposición de insumos".
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
        // SOLICITUDES DE COMPRA
        // ══════════════════════════════════════════════════════
        private const string SelectSolicitud = @"
            SELECT sc.*, i.Nombre AS NombreInsumo, i.UnidadMedida,
                   us.NombreUsuario AS NombreUsuarioSolic,
                   uv.NombreUsuario AS NombreUsuarioValida
            FROM SolicitudesCompra sc
            INNER JOIN Insumos  i  ON sc.IdInsumo = i.IdInsumo
            LEFT  JOIN Usuarios us ON sc.IdUsuarioSolicita = us.IdUsuario
            LEFT  JOIN Usuarios uv ON sc.IdUsuarioValida   = uv.IdUsuario";

        public int InsertarSolicitud(SolicitudCompraBE s, int idUsuario)
        {
            const string sql = @"INSERT INTO SolicitudesCompra
                (IdInsumo, CantidadSolicitada, Prioridad, PresupuestoEstimado, Estado, IdUsuarioSolicita, Observaciones)
                VALUES (@Ins, @Cant, @Prio, @Pres, N'Pendiente', @Usr, @Obs);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@Ins", s.IdInsumo);
            cmd.Parameters.AddWithValue("@Cant", s.CantidadSolicitada);
            cmd.Parameters.AddWithValue("@Prio", s.Prioridad);
            cmd.Parameters.AddWithValue("@Pres", V(s.PresupuestoEstimado));
            cmd.Parameters.AddWithValue("@Usr", idUsuario);
            cmd.Parameters.AddWithValue("@Obs", V(string.IsNullOrWhiteSpace(s.Observaciones) ? null : s.Observaciones));
            return (int)cmd.ExecuteScalar()!;
        }

        public SolicitudCompraBE? ObtenerSolicitudPorId(int idSolicitud)
        {
            using var cmd = Cmd(SelectSolicitud + " WHERE sc.IdSolicitud = @Id");
            cmd.Parameters.AddWithValue("@Id", idSolicitud);
            using var rd = cmd.ExecuteReader();
            return rd.Read() ? MapearSolicitud(rd) : null;
        }

        public List<SolicitudCompraBE> ObtenerSolicitudes(string? estado = null)
        {
            var lista = new List<SolicitudCompraBE>();
            string sql = SelectSolicitud + (string.IsNullOrEmpty(estado) ? "" : " WHERE sc.Estado = @E")
                       + " ORDER BY sc.FechaSolicitud DESC, sc.IdSolicitud DESC";
            using var cmd = Cmd(sql);
            if (!string.IsNullOrEmpty(estado)) cmd.Parameters.AddWithValue("@E", estado);
            using var rd = cmd.ExecuteReader();
            while (rd.Read()) lista.Add(MapearSolicitud(rd));
            return lista;
        }

        /// <summary>Pendientes de validar: primero las más urgentes y, dentro de cada prioridad, las más antiguas.</summary>
        public List<SolicitudCompraBE> ObtenerSolicitudesPendientes()
        {
            var lista = new List<SolicitudCompraBE>();
            string sql = SelectSolicitud + @" WHERE sc.Estado = N'Pendiente'
                ORDER BY CASE sc.Prioridad WHEN N'Urgente' THEN 1 WHEN N'Alta' THEN 2 WHEN N'Normal' THEN 3 ELSE 4 END,
                         sc.FechaSolicitud, sc.IdSolicitud";
            using var cmd = Cmd(sql);
            using var rd = cmd.ExecuteReader();
            while (rd.Read()) lista.Add(MapearSolicitud(rd));
            return lista;
        }

        /// <summary>Aprobadas que todavía no tienen orden de compra.</summary>
        public List<SolicitudCompraBE> ObtenerSolicitudesAprobadas()
        {
            var lista = new List<SolicitudCompraBE>();
            using var cmd = Cmd(SelectSolicitud + " WHERE sc.Estado = N'Aprobada' ORDER BY sc.FechaValidacion, sc.IdSolicitud");
            using var rd = cmd.ExecuteReader();
            while (rd.Read()) lista.Add(MapearSolicitud(rd));
            return lista;
        }

        /// <summary>
        /// Cambia el estado solo si la solicitud sigue en <paramref name="estadoEsperado"/>
        /// (evita que dos usuarios la procesen a la vez). Devuelve false si ya cambió.
        /// </summary>
        public bool ActualizarEstadoSolicitud(int idSolicitud, string nuevoEstado, string estadoEsperado,
                                              int? idUsuarioValida = null, string? observaciones = null)
        {
            const string sql = @"UPDATE SolicitudesCompra
                SET Estado = @N,
                    IdUsuarioValida = ISNULL(@U, IdUsuarioValida),
                    FechaValidacion = CASE WHEN @U IS NULL THEN FechaValidacion ELSE GETDATE() END,
                    Observaciones   = ISNULL(@Obs, Observaciones)
                WHERE IdSolicitud = @Id AND Estado = @Esp";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@N", nuevoEstado);
            cmd.Parameters.AddWithValue("@U", V(idUsuarioValida));
            cmd.Parameters.AddWithValue("@Obs", V(observaciones));
            cmd.Parameters.AddWithValue("@Id", idSolicitud);
            cmd.Parameters.AddWithValue("@Esp", estadoEsperado);
            return cmd.ExecuteNonQuery() == 1;
        }

        private static SolicitudCompraBE MapearSolicitud(SqlDataReader rd) => new SolicitudCompraBE
        {
            IdSolicitud = (int)rd["IdSolicitud"],
            IdInsumo = (int)rd["IdInsumo"],
            NombreInsumo = S(rd["NombreInsumo"]),
            UnidadMedida = S(rd["UnidadMedida"]),
            CantidadSolicitada = (decimal)rd["CantidadSolicitada"],
            FechaSolicitud = (DateTime)rd["FechaSolicitud"],
            Prioridad = S(rd["Prioridad"]),
            PresupuestoEstimado = rd["PresupuestoEstimado"] == DBNull.Value ? (decimal?)null : (decimal)rd["PresupuestoEstimado"],
            Estado = S(rd["Estado"]),
            IdUsuarioSolicita = rd["IdUsuarioSolicita"] == DBNull.Value ? (int?)null : (int)rd["IdUsuarioSolicita"],
            NombreUsuarioSolic = S(rd["NombreUsuarioSolic"]),
            NombreUsuarioValida = S(rd["NombreUsuarioValida"]),
            FechaValidacion = rd["FechaValidacion"] == DBNull.Value ? (DateTime?)null : (DateTime)rd["FechaValidacion"],
            Observaciones = S(rd["Observaciones"])
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
            Activo = (bool)rd["Activo"]
        };

        // ══════════════════════════════════════════════════════
        // ÓRDENES DE COMPRA
        // ══════════════════════════════════════════════════════
        private const string SelectOrden = @"
            SELECT oc.*, p.Nombre AS NombreProveedor, p.Email AS EmailProveedor, i.Nombre AS NombreInsumo
            FROM OrdenesCompra oc
            INNER JOIN Proveedores p ON oc.IdProveedor = p.IdProveedor
            INNER JOIN Insumos     i ON oc.IdInsumo    = i.IdInsumo";

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
                (NroOrden, IdSolicitud, IdProveedor, IdInsumo, Cantidad, PrecioUnitario, PlazoDias, Estado,
                 DireccionEntrega, IdUsuarioRegistro)
                VALUES (@Nro, @Sol, @Prov, @Ins, @Cant, @Precio, @Plazo, N'Emitida', @Dir, @Usr);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@Nro", o.NroOrden);
            cmd.Parameters.AddWithValue("@Sol", o.IdSolicitud);
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
            IdSolicitud = (int)rd["IdSolicitud"],
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
                (IdOrden, IdInsumo, CantidadRecibida, Estado, Observaciones, IdUsuarioRegistro)
                VALUES (@Ord, @Ins, @Cant, @Est, @Obs, @Usr);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@Ord", r.IdOrden);
            cmd.Parameters.AddWithValue("@Ins", r.IdInsumo);
            cmd.Parameters.AddWithValue("@Cant", r.CantidadRecibida);
            cmd.Parameters.AddWithValue("@Est", r.Estado);
            cmd.Parameters.AddWithValue("@Obs", V(string.IsNullOrWhiteSpace(r.Observaciones) ? null : r.Observaciones));
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
            const string sql = @"SELECT TOP 200 r.*, o.NroOrden, i.Nombre AS NombreInsumo
                FROM RecepcionesInsumos r
                INNER JOIN OrdenesCompra o ON r.IdOrden = o.IdOrden
                INNER JOIN Insumos i ON r.IdInsumo = i.IdInsumo
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
                    Observaciones = S(rd["Observaciones"])
                });
            return lista;
        }

        // ══════════════════════════════════════════════════════
        // REEMBOLSOS
        // ══════════════════════════════════════════════════════
        public bool ExisteReembolso(int idOrden)
        {
            using var cmd = Cmd("SELECT COUNT(*) FROM SolicitudesReembolso WHERE IdOrden = @Id");
            cmd.Parameters.AddWithValue("@Id", idOrden);
            return (int)cmd.ExecuteScalar()! > 0;
        }

        public int InsertarReembolso(SolicitudReembolsoBE r, int idUsuario)
        {
            const string sql = @"INSERT INTO SolicitudesReembolso
                (IdOrden, CantidadComprada, CantidadRecibida, MontoReembolso, Motivo, Estado, IdUsuarioRegistro)
                VALUES (@Ord, @Comp, @Rec, @Monto, @Mot, N'Pendiente', @Usr);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var cmd = Cmd(sql);
            cmd.Parameters.AddWithValue("@Ord", r.IdOrden);
            cmd.Parameters.AddWithValue("@Comp", r.CantidadComprada);
            cmd.Parameters.AddWithValue("@Rec", r.CantidadRecibida);
            cmd.Parameters.AddWithValue("@Monto", r.MontoReembolso);
            cmd.Parameters.AddWithValue("@Mot", r.Motivo);
            cmd.Parameters.AddWithValue("@Usr", idUsuario);
            return (int)cmd.ExecuteScalar()!;
        }

        public List<SolicitudReembolsoBE> ObtenerReembolsos()
        {
            var lista = new List<SolicitudReembolsoBE>();
            const string sql = @"SELECT TOP 200 s.*, o.NroOrden, i.Nombre AS NombreInsumo, p.Nombre AS NombreProveedor
                FROM SolicitudesReembolso s
                INNER JOIN OrdenesCompra o ON s.IdOrden = o.IdOrden
                INNER JOIN Insumos i ON o.IdInsumo = i.IdInsumo
                INNER JOIN Proveedores p ON o.IdProveedor = p.IdProveedor
                ORDER BY s.FechaReembolso DESC, s.IdReembolso DESC";
            using var cmd = Cmd(sql);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new SolicitudReembolsoBE
                {
                    IdReembolso = (int)rd["IdReembolso"],
                    IdOrden = (int)rd["IdOrden"],
                    NroOrden = S(rd["NroOrden"]),
                    NombreInsumo = S(rd["NombreInsumo"]),
                    NombreProveedor = S(rd["NombreProveedor"]),
                    CantidadComprada = (decimal)rd["CantidadComprada"],
                    CantidadRecibida = (decimal)rd["CantidadRecibida"],
                    Diferencia = (decimal)rd["Diferencia"],
                    MontoReembolso = (decimal)rd["MontoReembolso"],
                    Motivo = S(rd["Motivo"]),
                    FechaReembolso = (DateTime)rd["FechaReembolso"],
                    Estado = S(rd["Estado"])
                });
            return lista;
        }
    }
}
