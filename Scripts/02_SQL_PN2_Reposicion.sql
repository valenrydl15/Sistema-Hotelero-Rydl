/* =====================================================================
   PN2 - GESTION DE REPOSICION DE INSUMOS
   Script IDEMPOTENTE: se puede ejecutar mas de una vez sin duplicar nada.
   (La aplicacion tambien lo ejecuta sola al iniciar; correrlo a mano es opcional.)
   ===================================================================== */
USE [HotelRydlDB]
GO

/* ---------------------------------------------------------------------
   1) TABLAS NUEVAS
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.Notificaciones', N'U') IS NULL
CREATE TABLE dbo.Notificaciones (
    IdNotificacion     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdUsuarioDestino   INT NULL,
    IdProveedorDestino INT NULL,
    IdSolicitud        INT NULL,
    IdOrden            INT NULL,
    Tipo               NVARCHAR(40)  NOT NULL,
    Mensaje            NVARCHAR(400) NOT NULL,
    Fecha              DATETIME      NOT NULL CONSTRAINT DF_Notificaciones_Fecha DEFAULT (GETDATE()),
    Leida              BIT           NOT NULL CONSTRAINT DF_Notificaciones_Leida DEFAULT (0),
    CONSTRAINT FK_Notif_Usuario   FOREIGN KEY (IdUsuarioDestino)   REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT FK_Notif_Proveedor FOREIGN KEY (IdProveedorDestino) REFERENCES dbo.Proveedores(IdProveedor),
    CONSTRAINT FK_Notif_Solicitud FOREIGN KEY (IdSolicitud)        REFERENCES dbo.SolicitudesCompra(IdSolicitud),
    CONSTRAINT FK_Notif_Orden     FOREIGN KEY (IdOrden)            REFERENCES dbo.OrdenesCompra(IdOrden),
    CONSTRAINT CK_Notif_Destino   CHECK (IdUsuarioDestino IS NOT NULL OR IdProveedorDestino IS NOT NULL)
);
GO

IF OBJECT_ID(N'dbo.MovimientosStock', N'U') IS NULL
CREATE TABLE dbo.MovimientosStock (
    IdMovimiento       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdInsumo           INT NOT NULL,
    IdRecepcion        INT NULL,
    Tipo               NVARCHAR(20)  NOT NULL CONSTRAINT DF_MovStock_Tipo DEFAULT (N'Ingreso'),
    CantidadAnterior   DECIMAL(10,2) NOT NULL,
    CantidadMovimiento DECIMAL(10,2) NOT NULL,
    CantidadNueva      DECIMAL(10,2) NOT NULL,
    FechaMovimiento    DATETIME      NOT NULL CONSTRAINT DF_MovStock_Fecha DEFAULT (GETDATE()),
    IdUsuarioRegistro  INT NULL,
    CONSTRAINT FK_MovStock_Insumo    FOREIGN KEY (IdInsumo)          REFERENCES dbo.Insumos(IdInsumo),
    CONSTRAINT FK_MovStock_Recepcion FOREIGN KEY (IdRecepcion)       REFERENCES dbo.RecepcionesInsumos(IdRecepcion),
    CONSTRAINT FK_MovStock_Usuario   FOREIGN KEY (IdUsuarioRegistro) REFERENCES dbo.Usuarios(IdUsuario)
);
GO

IF OBJECT_ID(N'dbo.SolicitudesReembolso', N'U') IS NULL
CREATE TABLE dbo.SolicitudesReembolso (
    IdReembolso        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdOrden            INT NOT NULL,
    CantidadComprada   DECIMAL(10,2) NOT NULL,
    CantidadRecibida   DECIMAL(10,2) NOT NULL,
    Diferencia         AS (CantidadComprada - CantidadRecibida) PERSISTED,
    MontoReembolso     DECIMAL(12,2) NOT NULL,
    Motivo             NVARCHAR(300) NOT NULL,
    FechaReembolso     DATETIME      NOT NULL CONSTRAINT DF_Reembolso_Fecha DEFAULT (GETDATE()),
    Estado             NVARCHAR(20)  NOT NULL CONSTRAINT DF_Reembolso_Estado DEFAULT (N'Pendiente'),
    IdUsuarioRegistro  INT NULL,
    CONSTRAINT FK_Reembolso_Orden   FOREIGN KEY (IdOrden)           REFERENCES dbo.OrdenesCompra(IdOrden),
    CONSTRAINT FK_Reembolso_Usuario FOREIGN KEY (IdUsuarioRegistro) REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT UQ_Reembolso_Orden   UNIQUE (IdOrden)
);
GO

/* ---------------------------------------------------------------------
   2) COLUMNAS NUEVAS EN OrdenesCompra (direccion de entrega y fecha de envio al proveedor)
   --------------------------------------------------------------------- */
IF COL_LENGTH(N'dbo.OrdenesCompra', N'DireccionEntrega') IS NULL
    ALTER TABLE dbo.OrdenesCompra ADD DireccionEntrega NVARCHAR(300) NULL;
GO
IF COL_LENGTH(N'dbo.OrdenesCompra', N'FechaEnvio') IS NULL
    ALTER TABLE dbo.OrdenesCompra ADD FechaEnvio DATETIME NULL;
GO

/* ---------------------------------------------------------------------
   3) RESTRICCIONES DE INTEGRIDAD (WITH NOCHECK: no revisan filas viejas, si controlan las nuevas)
   --------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Insumos_Stock')
    ALTER TABLE dbo.Insumos WITH NOCHECK
        ADD CONSTRAINT CK_Insumos_Stock CHECK (StockActual >= 0 AND StockMinimo >= 0);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Solicitud_Cantidad')
    ALTER TABLE dbo.SolicitudesCompra WITH NOCHECK
        ADD CONSTRAINT CK_Solicitud_Cantidad CHECK (CantidadSolicitada > 0);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Solicitud_Prioridad')
    ALTER TABLE dbo.SolicitudesCompra WITH NOCHECK
        ADD CONSTRAINT CK_Solicitud_Prioridad CHECK (Prioridad IN (N'Baja', N'Normal', N'Alta', N'Urgente'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Solicitud_Estado')
    ALTER TABLE dbo.SolicitudesCompra WITH NOCHECK
        ADD CONSTRAINT CK_Solicitud_Estado CHECK (Estado IN (N'Pendiente', N'Aprobada', N'Rechazada', N'Ordenada'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Orden_Importes')
    ALTER TABLE dbo.OrdenesCompra WITH NOCHECK
        ADD CONSTRAINT CK_Orden_Importes CHECK (Cantidad > 0 AND PrecioUnitario > 0);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Orden_Estado')
    ALTER TABLE dbo.OrdenesCompra WITH NOCHECK
        ADD CONSTRAINT CK_Orden_Estado CHECK (Estado IN (N'Emitida', N'Recibida', N'Con diferencia', N'Con reembolso'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Recepcion_Cantidad')
    ALTER TABLE dbo.RecepcionesInsumos WITH NOCHECK
        ADD CONSTRAINT CK_Recepcion_Cantidad CHECK (CantidadRecibida >= 0);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Recepcion_Estado')
    ALTER TABLE dbo.RecepcionesInsumos WITH NOCHECK
        ADD CONSTRAINT CK_Recepcion_Estado CHECK (Estado IN (N'Aceptado', N'Con diferencia'));
GO

/* Una orden por solicitud y una recepcion por orden (recepcion unica, opcion A del proceso).
   Solo se crean si no hay duplicados previos en los datos. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Ordenes_Solicitud' AND object_id = OBJECT_ID(N'dbo.OrdenesCompra'))
   AND NOT EXISTS (SELECT IdSolicitud FROM dbo.OrdenesCompra GROUP BY IdSolicitud HAVING COUNT(*) > 1)
   /* solo en bases sin migrar al flujo v2 (seccion 8): despues IdSolicitud admite NULL */
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompra') AND name = N'IdSolicitud' AND is_nullable = 0)
    CREATE UNIQUE INDEX UX_Ordenes_Solicitud ON dbo.OrdenesCompra (IdSolicitud);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Recepciones_Orden' AND object_id = OBJECT_ID(N'dbo.RecepcionesInsumos'))
   AND NOT EXISTS (SELECT IdOrden FROM dbo.RecepcionesInsumos GROUP BY IdOrden HAVING COUNT(*) > 1)
    CREATE UNIQUE INDEX UX_Recepciones_Orden ON dbo.RecepcionesInsumos (IdOrden);
GO

/* Indices de consulta */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Solicitudes_Estado' AND object_id = OBJECT_ID(N'dbo.SolicitudesCompra'))
    CREATE INDEX IX_Solicitudes_Estado ON dbo.SolicitudesCompra (Estado, FechaSolicitud);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Ordenes_Estado' AND object_id = OBJECT_ID(N'dbo.OrdenesCompra'))
    CREATE INDEX IX_Ordenes_Estado ON dbo.OrdenesCompra (Estado, FechaEmision);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Notif_Usuario' AND object_id = OBJECT_ID(N'dbo.Notificaciones'))
    CREATE INDEX IX_Notif_Usuario ON dbo.Notificaciones (IdUsuarioDestino, Leida, Fecha);
GO

/* ---------------------------------------------------------------------
   4) BITACORA: modulo y tipos de evento (la app busca los Id por NOMBRE)
      Criticidad: 1 = mas critica ... 5 = menos critica
   --------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Nombre = N'Reposición de insumos')
    INSERT INTO dbo.Modulos (Nombre) VALUES (N'Reposición de insumos');
GO

DECLARE @mod INT = (SELECT TOP 1 IdModulo FROM dbo.Modulos WHERE Nombre = N'Reposición de insumos');
DECLARE @ev TABLE (Nombre NVARCHAR(150), Criticidad INT);
INSERT INTO @ev VALUES
 (N'Consulta de stock',                        5),
 (N'Solicitud de compra generada',             4),
 (N'Solicitud de compra aprobada',             3),
 (N'Solicitud de compra rechazada',            3),
 (N'Orden de compra emitida y enviada',        3),
 (N'Error al enviar orden al proveedor',       2),
 (N'Recepción de insumos registrada',          3),
 (N'Recepción con diferencia',                 2),
 (N'Stock actualizado',                        4),
 (N'Solicitud de reembolso registrada',        3),
 (N'Operación de reposición rechazada',        3),
 (N'Acceso denegado a reposición',             2),
 (N'Error técnico de reposición',              1),
 (N'Reporte de orden de compra exportado',    4);

INSERT INTO dbo.TiposEvento (IdModulo, Nombre, Criticidad)
SELECT @mod, e.Nombre, e.Criticidad
FROM @ev e
WHERE NOT EXISTS (SELECT 1 FROM dbo.TiposEvento t WHERE t.IdModulo = @mod AND t.Nombre = e.Nombre);
GO

/* ---------------------------------------------------------------------
   Reportes PDF: evento de bitácora del módulo Check-in/Check-out (factura)
   --------------------------------------------------------------------- */
IF EXISTS (SELECT 1 FROM dbo.Modulos WHERE IdModulo = 2)
   AND NOT EXISTS (SELECT 1 FROM dbo.TiposEvento WHERE IdModulo = 2 AND Nombre = N'Reporte de factura exportado')
    INSERT INTO dbo.TiposEvento (IdModulo, Nombre, Criticidad) VALUES (2, N'Reporte de factura exportado', 4);
GO

/* ---------------------------------------------------------------------
   5) ROLES Y PERMISOS DEL PROCESO
      Cambian Roles / Permisos / RolPermisos: si hubo cambios, sus DV se ponen en '0'
      (estado inicial) y la aplicacion los recalcula sola en el proximo inicio de sesion.
   --------------------------------------------------------------------- */
DECLARE @cambios BIT = 0;

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Encargado de Compras')
BEGIN
    INSERT INTO dbo.Roles (Nombre, Descripcion)
    VALUES (N'Encargado de Compras', N'Valida solicitudes de compra, elige proveedor y emite órdenes de compra');
    SET @cambios = 1;
END;
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Encargado de Almacén')
BEGIN
    INSERT INTO dbo.Roles (Nombre, Descripcion)
    VALUES (N'Encargado de Almacén', N'Controla el stock, genera solicitudes de compra y registra recepciones de insumos');
    SET @cambios = 1;
END;

DECLARE @perm TABLE (Nombre NVARCHAR(100), Descripcion NVARCHAR(300));
INSERT INTO @perm VALUES
 (N'Consultar stock de insumos',        N'Ver el stock actual y los insumos en estado crítico'),
 (N'Generar solicitud de compra',       N'Crear solicitudes de compra de insumos'),
 (N'Validar solicitud de compra',       N'Aprobar o rechazar solicitudes de compra'),
 (N'Realizar compra de insumos',        N'Seleccionar proveedor y emitir la orden de compra'),
 (N'Registrar recepción de insumos',    N'Registrar la mercadería recibida y actualizar el stock'),
 (N'Registrar solicitud de reembolso',  N'Pedir reembolso por diferencias en una recepción');

INSERT INTO dbo.Permisos (Nombre, Descripcion, Activo)
SELECT p.Nombre, p.Descripcion, 1 FROM @perm p
WHERE NOT EXISTS (SELECT 1 FROM dbo.Permisos x WHERE x.Nombre = p.Nombre);
IF @@ROWCOUNT > 0 SET @cambios = 1;

/* Matriz Rol -> Permiso */
DECLARE @mat TABLE (Rol NVARCHAR(50), Permiso NVARCHAR(100));
INSERT INTO @mat VALUES
 (N'Administrador',        N'Consultar stock de insumos'),
 (N'Administrador',        N'Generar solicitud de compra'),
 (N'Administrador',        N'Validar solicitud de compra'),
 (N'Administrador',        N'Realizar compra de insumos'),
 (N'Administrador',        N'Registrar recepción de insumos'),
 (N'Administrador',        N'Registrar solicitud de reembolso'),
 (N'Encargado de Compras', N'Consultar stock de insumos'),
 (N'Encargado de Compras', N'Validar solicitud de compra'),
 (N'Encargado de Compras', N'Realizar compra de insumos'),
 (N'Encargado de Compras', N'Registrar solicitud de reembolso'),
 (N'Encargado de Almacén', N'Consultar stock de insumos'),
 (N'Encargado de Almacén', N'Generar solicitud de compra'),
 (N'Encargado de Almacén', N'Registrar recepción de insumos');

INSERT INTO dbo.RolPermisos (IdRol, IdPermiso)
SELECT r.IdRol, p.IdPermiso
FROM @mat m
JOIN dbo.Roles    r ON r.Nombre = m.Rol
JOIN dbo.Permisos p ON p.Nombre = m.Permiso
WHERE NOT EXISTS (SELECT 1 FROM dbo.RolPermisos rp WHERE rp.IdRol = r.IdRol AND rp.IdPermiso = p.IdPermiso);
IF @@ROWCOUNT > 0 SET @cambios = 1;

IF @cambios = 1
    UPDATE dbo.DigitoVerificador
       SET DVH = '0', DVV = '0', FechaCalculo = GETDATE(), Valido = 1
     WHERE NombreTabla IN ('Roles', 'Permisos', 'RolPermisos');
GO

/* ---------------------------------------------------------------------
   6) DIGITO VERIFICADOR: tablas nuevas controladas (arrancan en '0' = estado inicial)
   --------------------------------------------------------------------- */
INSERT INTO dbo.DigitoVerificador (NombreTabla, DVH, DVV, FechaCalculo, Valido)
SELECT v.t, '0', '0', GETDATE(), 1
FROM (VALUES ('Insumos'), ('Proveedores'), ('SolicitudesCompra'), ('OrdenesCompra'),
             ('RecepcionesInsumos'), ('MovimientosStock'), ('SolicitudesReembolso')) v(t)
WHERE v.t NOT IN (SELECT NombreTabla FROM dbo.DigitoVerificador);
GO

/* ---------------------------------------------------------------------
   7) DATOS DE EJEMPLO (solo si las tablas estan vacias; borralos si no los queres)
   --------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Insumos)
BEGIN
    INSERT INTO dbo.Insumos (Nombre, Descripcion, UnidadMedida, StockActual, StockMinimo) VALUES
     (N'Sábanas queen',          N'Juego de sábanas 200 hilos',        N'unidades', 14,  20),
     (N'Toallas de baño',        N'Toalla 70x140 blanca',              N'unidades', 85,  60),
     (N'Toallas de mano',        N'Toalla 40x70 blanca',               N'unidades', 40,  50),
     (N'Jabón de tocador',       N'Jabón individual 30 g',             N'unidades', 220, 150),
     (N'Shampoo 30 ml',          N'Dosificador individual',            N'unidades', 90,  120),
     (N'Papel higiénico',        N'Rollo institucional 30 m',          N'rollos',   300, 200),
     (N'Detergente multiuso',    N'Bidón para limpieza de habitaciones', N'litros', 18,  25),
     (N'Cápsulas de café',       N'Cápsulas para cafetera de cortesía', N'unidades', 400, 250),
     (N'Agua mineral 500 ml',    N'Botella para frigobar',             N'unidades', 60,  80),
     (N'Bolsas de residuos',     N'Bolsa 60x90 consorcio',             N'unidades', 500, 300);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Proveedores)
BEGIN
    INSERT INTO dbo.Proveedores (Nombre, Contacto, Telefono, Email, Direccion, CondicionesPago, Activo) VALUES
     (N'Textil del Plata S.A.',       N'Marcela Ibarra', N'011 4555-1020', N'ventas@textildelplata.example',  N'Av. Belgrano 1450, CABA',        N'30 días fecha factura', 1),
     (N'Distribuidora Limpia Hotel',  N'Rodrigo Paz',    N'011 4777-8800', N'pedidos@limpiahotel.example',    N'Ruta 8 km 32, Pilar',            N'Contado contra entrega', 1),
     (N'Insumos Gastronómicos Norte', N'Lucía Fernández',N'011 4111-3030', N'compras@gastronorte.example',    N'Av. Cabildo 2200, CABA',         N'15 días',                1),
     (N'Proveedor Histórico (inactivo)', N'—',           N'—',             N'baja@proveedor.example',         N'—',                              N'—',                      0);
END;
GO

/* =====================================================================
   8) PN2 v2 - FLUJO CON COTIZACION, PRE-ORDEN, PAGO Y FACTURA DEL PROVEEDOR
      CUN06 Realizar cotizacion            (Encargado de Deposito)
      CUN07 Generar pre-orden de compra    (Encargado de Deposito)
      CUN08 Registrar proveedor nuevo      (Encargado de Compras, extiende a CUN09)
      CUN09 Realizar compra de insumos     (Encargado de Compras: pago por banco + orden)
      CUN10 Registrar insumos recibidos    (Encargado de Deposito, contra la factura)
      Las tablas viejas (SolicitudesCompra, SolicitudesReembolso) quedan con su historial.
   ===================================================================== */

/* 8.1) Proveedores: datos para pagarle (destinatario bancario). CBU NULL = proveedor nuevo */
DECLARE @cProv BIT = 0;
IF COL_LENGTH(N'dbo.Proveedores', N'CUIT') IS NULL
BEGIN ALTER TABLE dbo.Proveedores ADD CUIT NVARCHAR(13) NULL; SET @cProv = 1; END;
IF COL_LENGTH(N'dbo.Proveedores', N'CBU') IS NULL
BEGIN ALTER TABLE dbo.Proveedores ADD CBU NVARCHAR(22) NULL; SET @cProv = 1; END;
IF COL_LENGTH(N'dbo.Proveedores', N'Banco') IS NULL
BEGIN ALTER TABLE dbo.Proveedores ADD Banco NVARCHAR(100) NULL; SET @cProv = 1; END;
IF COL_LENGTH(N'dbo.Proveedores', N'FechaRegistroPago') IS NULL
BEGIN ALTER TABLE dbo.Proveedores ADD FechaRegistroPago DATETIME NULL; SET @cProv = 1; END;
/* Cambiaron las columnas: el DV vuelve al estado inicial y la aplicacion lo recalcula */
IF @cProv = 1
    UPDATE dbo.DigitoVerificador SET DVH = '0', DVV = '0', FechaCalculo = GETDATE(), Valido = 1
     WHERE NombreTabla = 'Proveedores';
GO

/* 8.2) Cotizacion: un insumo y una cantidad, cotizados con varios proveedores */
IF OBJECT_ID(N'dbo.Cotizaciones', N'U') IS NULL
CREATE TABLE dbo.Cotizaciones (
    IdCotizacion      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdInsumo          INT           NOT NULL,
    Cantidad          DECIMAL(10,2) NOT NULL,
    FechaCotizacion   DATETIME      NOT NULL CONSTRAINT DF_Cotizaciones_Fecha DEFAULT (GETDATE()),
    Estado            NVARCHAR(20)  NOT NULL CONSTRAINT DF_Cotizaciones_Estado DEFAULT (N'Abierta'),
    Observaciones     NVARCHAR(300) NULL,
    IdUsuarioRegistro INT NULL,
    CONSTRAINT FK_Cotizaciones_Insumo  FOREIGN KEY (IdInsumo)          REFERENCES dbo.Insumos(IdInsumo),
    CONSTRAINT FK_Cotizaciones_Usuario FOREIGN KEY (IdUsuarioRegistro) REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT CK_Cotizaciones_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT CK_Cotizaciones_Estado   CHECK (Estado IN (N'Abierta', N'Con pre-orden', N'Anulada'))
);
GO

IF OBJECT_ID(N'dbo.CotizacionesDetalle', N'U') IS NULL
CREATE TABLE dbo.CotizacionesDetalle (
    IdDetalleCotizacion INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdCotizacion        INT           NOT NULL,
    IdProveedor         INT           NOT NULL,
    PrecioUnitario      DECIMAL(10,2) NOT NULL,
    PlazoDias           INT           NOT NULL,
    CONSTRAINT FK_CotDetalle_Cotizacion FOREIGN KEY (IdCotizacion) REFERENCES dbo.Cotizaciones(IdCotizacion),
    CONSTRAINT FK_CotDetalle_Proveedor  FOREIGN KEY (IdProveedor)  REFERENCES dbo.Proveedores(IdProveedor),
    CONSTRAINT UQ_CotDetalle_Proveedor  UNIQUE (IdCotizacion, IdProveedor),
    CONSTRAINT CK_CotDetalle_Precio     CHECK (PrecioUnitario > 0),
    CONSTRAINT CK_CotDetalle_Plazo      CHECK (PlazoDias BETWEEN 0 AND 365)
);
GO

/* 8.3) Pre-orden de compra: la cotizacion elegida, lista para que Compras la pague */
IF OBJECT_ID(N'dbo.PreOrdenesCompra', N'U') IS NULL
CREATE TABLE dbo.PreOrdenesCompra (
    IdPreOrden          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    NroPreOrden         NVARCHAR(20)  NOT NULL,
    IdCotizacion        INT           NOT NULL,
    IdDetalleCotizacion INT           NOT NULL,
    IdInsumo            INT           NOT NULL,
    IdProveedor         INT           NOT NULL,
    Cantidad            DECIMAL(10,2) NOT NULL,
    PrecioUnitario      DECIMAL(10,2) NOT NULL,
    PrecioTotal         AS (Cantidad * PrecioUnitario) PERSISTED,
    PlazoDias           INT           NOT NULL,
    Prioridad           NVARCHAR(20)  NOT NULL CONSTRAINT DF_PreOrden_Prioridad DEFAULT (N'Normal'),
    FechaPreOrden       DATETIME      NOT NULL CONSTRAINT DF_PreOrden_Fecha DEFAULT (GETDATE()),
    Estado              NVARCHAR(20)  NOT NULL CONSTRAINT DF_PreOrden_Estado DEFAULT (N'Pendiente'),
    Observaciones       NVARCHAR(300) NULL,
    IdUsuarioRegistro   INT NULL,
    CONSTRAINT UQ_PreOrden_Nro        UNIQUE (NroPreOrden),
    CONSTRAINT UQ_PreOrden_Cotizacion UNIQUE (IdCotizacion),
    CONSTRAINT FK_PreOrden_Cotizacion FOREIGN KEY (IdCotizacion)        REFERENCES dbo.Cotizaciones(IdCotizacion),
    CONSTRAINT FK_PreOrden_Detalle    FOREIGN KEY (IdDetalleCotizacion) REFERENCES dbo.CotizacionesDetalle(IdDetalleCotizacion),
    CONSTRAINT FK_PreOrden_Insumo     FOREIGN KEY (IdInsumo)            REFERENCES dbo.Insumos(IdInsumo),
    CONSTRAINT FK_PreOrden_Proveedor  FOREIGN KEY (IdProveedor)         REFERENCES dbo.Proveedores(IdProveedor),
    CONSTRAINT FK_PreOrden_Usuario    FOREIGN KEY (IdUsuarioRegistro)   REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT CK_PreOrden_Importes   CHECK (Cantidad > 0 AND PrecioUnitario > 0),
    CONSTRAINT CK_PreOrden_Prioridad  CHECK (Prioridad IN (N'Baja', N'Normal', N'Alta', N'Urgente')),
    CONSTRAINT CK_PreOrden_Estado     CHECK (Estado IN (N'Pendiente', N'Comprada'))
);
GO

/* 8.4) Pago al proveedor por transferencia bancaria (se guardan tambien los rechazados) */
IF OBJECT_ID(N'dbo.PagosProveedor', N'U') IS NULL
CREATE TABLE dbo.PagosProveedor (
    IdPagoProveedor   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdPreOrden        INT           NOT NULL,
    IdProveedor       INT           NOT NULL,
    Monto             DECIMAL(12,2) NOT NULL,
    MedioPago         NVARCHAR(40)  NOT NULL CONSTRAINT DF_PagosProv_Medio DEFAULT (N'Transferencia bancaria'),
    CBUDestino        NVARCHAR(22)  NOT NULL,
    NroOperacion      NVARCHAR(40)  NULL,
    FechaPago         DATETIME      NOT NULL CONSTRAINT DF_PagosProv_Fecha DEFAULT (GETDATE()),
    Estado            NVARCHAR(20)  NOT NULL,
    DetalleBanco      NVARCHAR(200) NULL,
    IdUsuarioRegistro INT NULL,
    CONSTRAINT FK_PagosProv_PreOrden  FOREIGN KEY (IdPreOrden)        REFERENCES dbo.PreOrdenesCompra(IdPreOrden),
    CONSTRAINT FK_PagosProv_Proveedor FOREIGN KEY (IdProveedor)       REFERENCES dbo.Proveedores(IdProveedor),
    CONSTRAINT FK_PagosProv_Usuario   FOREIGN KEY (IdUsuarioRegistro) REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT CK_PagosProv_Monto     CHECK (Monto > 0),
    CONSTRAINT CK_PagosProv_Estado    CHECK (Estado IN (N'Aprobado', N'Rechazado'))
);
GO

/* 8.5) OrdenesCompra: ahora nace de una pre-orden pagada (IdSolicitud queda solo para las viejas) */
DECLARE @cOrd BIT = 0, @sql NVARCHAR(MAX) = N'';
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompra') AND name = N'IdSolicitud' AND is_nullable = 0)
BEGIN
    /* Se quitan el indice unico y la FK sobre IdSolicitud para poder dejarla NULL */
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Ordenes_Solicitud' AND object_id = OBJECT_ID(N'dbo.OrdenesCompra'))
        DROP INDEX UX_Ordenes_Solicitud ON dbo.OrdenesCompra;
    SELECT @sql = @sql + N'ALTER TABLE dbo.OrdenesCompra DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';'
    FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fc ON fc.constraint_object_id = fk.object_id
    JOIN sys.columns c ON c.object_id = fc.parent_object_id AND c.column_id = fc.parent_column_id
    WHERE fk.parent_object_id = OBJECT_ID(N'dbo.OrdenesCompra') AND c.name = N'IdSolicitud';
    IF LEN(@sql) > 0 EXEC sp_executesql @sql;
    ALTER TABLE dbo.OrdenesCompra ALTER COLUMN IdSolicitud INT NULL;
    ALTER TABLE dbo.OrdenesCompra WITH CHECK
        ADD CONSTRAINT FK_Ordenes_Solicitud FOREIGN KEY (IdSolicitud) REFERENCES dbo.SolicitudesCompra(IdSolicitud);
    SET @cOrd = 1;
END;
IF COL_LENGTH(N'dbo.OrdenesCompra', N'IdPreOrden') IS NULL
BEGIN ALTER TABLE dbo.OrdenesCompra ADD IdPreOrden INT NULL; SET @cOrd = 1; END;
IF COL_LENGTH(N'dbo.OrdenesCompra', N'IdPagoProveedor') IS NULL
BEGIN ALTER TABLE dbo.OrdenesCompra ADD IdPagoProveedor INT NULL; SET @cOrd = 1; END;
IF @cOrd = 1
    UPDATE dbo.DigitoVerificador SET DVH = '0', DVV = '0', FechaCalculo = GETDATE(), Valido = 1
     WHERE NombreTabla = 'OrdenesCompra';
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Ordenes_PreOrden')
    ALTER TABLE dbo.OrdenesCompra WITH CHECK
        ADD CONSTRAINT FK_Ordenes_PreOrden FOREIGN KEY (IdPreOrden) REFERENCES dbo.PreOrdenesCompra(IdPreOrden);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Ordenes_PagoProveedor')
    ALTER TABLE dbo.OrdenesCompra WITH CHECK
        ADD CONSTRAINT FK_Ordenes_PagoProveedor FOREIGN KEY (IdPagoProveedor) REFERENCES dbo.PagosProveedor(IdPagoProveedor);
GO

/* 8.6) Factura que manda el proveedor junto con los productos */
IF OBJECT_ID(N'dbo.FacturasProveedor', N'U') IS NULL
CREATE TABLE dbo.FacturasProveedor (
    IdFacturaProveedor INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdOrden            INT           NOT NULL,
    IdProveedor        INT           NOT NULL,
    NroFactura         NVARCHAR(30)  NOT NULL,
    FechaFactura       DATETIME      NOT NULL,
    IdInsumo           INT           NOT NULL,
    CantidadFacturada  DECIMAL(10,2) NOT NULL,
    PrecioUnitario     DECIMAL(10,2) NOT NULL,
    Total              AS (CantidadFacturada * PrecioUnitario) PERSISTED,
    FechaRegistro      DATETIME      NOT NULL CONSTRAINT DF_FactProv_Fecha DEFAULT (GETDATE()),
    IdUsuarioRegistro  INT NULL,
    CONSTRAINT UQ_FactProv_Orden     UNIQUE (IdOrden),
    CONSTRAINT UQ_FactProv_Nro       UNIQUE (IdProveedor, NroFactura),
    CONSTRAINT FK_FactProv_Orden     FOREIGN KEY (IdOrden)           REFERENCES dbo.OrdenesCompra(IdOrden),
    CONSTRAINT FK_FactProv_Proveedor FOREIGN KEY (IdProveedor)       REFERENCES dbo.Proveedores(IdProveedor),
    CONSTRAINT FK_FactProv_Insumo    FOREIGN KEY (IdInsumo)          REFERENCES dbo.Insumos(IdInsumo),
    CONSTRAINT FK_FactProv_Usuario   FOREIGN KEY (IdUsuarioRegistro) REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT CK_FactProv_Importes  CHECK (CantidadFacturada > 0 AND PrecioUnitario > 0)
);
GO

/* 8.7) La recepcion queda ligada a la factura del proveedor */
DECLARE @cRec BIT = 0;
IF COL_LENGTH(N'dbo.RecepcionesInsumos', N'IdFacturaProveedor') IS NULL
BEGIN ALTER TABLE dbo.RecepcionesInsumos ADD IdFacturaProveedor INT NULL; SET @cRec = 1; END;
IF @cRec = 1
    UPDATE dbo.DigitoVerificador SET DVH = '0', DVV = '0', FechaCalculo = GETDATE(), Valido = 1
     WHERE NombreTabla = 'RecepcionesInsumos';
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Recepcion_FacturaProv')
    ALTER TABLE dbo.RecepcionesInsumos WITH CHECK
        ADD CONSTRAINT FK_Recepcion_FacturaProv FOREIGN KEY (IdFacturaProveedor) REFERENCES dbo.FacturasProveedor(IdFacturaProveedor);
GO

/* Indices de consulta */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Cotizaciones_Estado' AND object_id = OBJECT_ID(N'dbo.Cotizaciones'))
    CREATE INDEX IX_Cotizaciones_Estado ON dbo.Cotizaciones (Estado, FechaCotizacion);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PreOrdenes_Estado' AND object_id = OBJECT_ID(N'dbo.PreOrdenesCompra'))
    CREATE INDEX IX_PreOrdenes_Estado ON dbo.PreOrdenesCompra (Estado, FechaPreOrden);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PagosProv_PreOrden' AND object_id = OBJECT_ID(N'dbo.PagosProveedor'))
    CREATE INDEX IX_PagosProv_PreOrden ON dbo.PagosProveedor (IdPreOrden, Estado);
GO

/* 8.8) Bitacora: eventos del flujo nuevo */
DECLARE @mod2 INT = (SELECT TOP 1 IdModulo FROM dbo.Modulos WHERE Nombre = N'Reposición de insumos');
DECLARE @ev2 TABLE (Nombre NVARCHAR(150), Criticidad INT);
INSERT INTO @ev2 VALUES
 (N'Cotización registrada',               4),
 (N'Cotización anulada',                  3),
 (N'Pre-orden de compra generada',        4),
 (N'Proveedor nuevo registrado',          3),
 (N'Pago a proveedor realizado',          3),
 (N'Pago a proveedor rechazado',          2),
 (N'Factura de proveedor registrada',     4);
INSERT INTO dbo.TiposEvento (IdModulo, Nombre, Criticidad)
SELECT @mod2, e.Nombre, e.Criticidad FROM @ev2 e
WHERE @mod2 IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.TiposEvento t WHERE t.IdModulo = @mod2 AND t.Nombre = e.Nombre);
GO

/* 8.9) Permisos del flujo nuevo (los de validar y reembolso dejan de usarse) */
DECLARE @cambios2 BIT = 0;
DECLARE @perm2 TABLE (Nombre NVARCHAR(100), Descripcion NVARCHAR(300));
INSERT INTO @perm2 VALUES
 (N'Realizar cotización',              N'Cotizar un insumo con varios proveedores y comparar precios'),
 (N'Generar pre-orden de compra',      N'Generar la pre-orden con el proveedor elegido de la cotización'),
 (N'Registrar proveedor nuevo',        N'Registrar los datos bancarios de un proveedor al que nunca se le pagó'),
 (N'Registrar insumos recibidos',      N'Registrar los productos y la factura recibidos y actualizar el stock');
INSERT INTO dbo.Permisos (Nombre, Descripcion, Activo)
SELECT p.Nombre, p.Descripcion, 1 FROM @perm2 p
WHERE NOT EXISTS (SELECT 1 FROM dbo.Permisos x WHERE x.Nombre = p.Nombre);
IF @@ROWCOUNT > 0 SET @cambios2 = 1;

UPDATE dbo.Permisos SET Descripcion = N'Pagar al proveedor por transferencia bancaria y emitir la orden de compra'
 WHERE Nombre = N'Realizar compra de insumos' AND Descripcion <> N'Pagar al proveedor por transferencia bancaria y emitir la orden de compra';
IF @@ROWCOUNT > 0 SET @cambios2 = 1;

DECLARE @mat2 TABLE (Rol NVARCHAR(50), Permiso NVARCHAR(100));
INSERT INTO @mat2 VALUES
 (N'Administrador',        N'Realizar cotización'),
 (N'Administrador',        N'Generar pre-orden de compra'),
 (N'Administrador',        N'Registrar proveedor nuevo'),
 (N'Administrador',        N'Registrar insumos recibidos'),
 (N'Encargado de Almacén', N'Realizar cotización'),
 (N'Encargado de Almacén', N'Generar pre-orden de compra'),
 (N'Encargado de Almacén', N'Registrar insumos recibidos'),
 (N'Encargado de Compras', N'Registrar proveedor nuevo');
INSERT INTO dbo.RolPermisos (IdRol, IdPermiso)
SELECT r.IdRol, p.IdPermiso
FROM @mat2 m
JOIN dbo.Roles    r ON r.Nombre = m.Rol
JOIN dbo.Permisos p ON p.Nombre = m.Permiso
WHERE NOT EXISTS (SELECT 1 FROM dbo.RolPermisos rp WHERE rp.IdRol = r.IdRol AND rp.IdPermiso = p.IdPermiso);
IF @@ROWCOUNT > 0 SET @cambios2 = 1;

IF @cambios2 = 1
    UPDATE dbo.DigitoVerificador
       SET DVH = '0', DVV = '0', FechaCalculo = GETDATE(), Valido = 1
     WHERE NombreTabla IN ('Roles', 'Permisos', 'RolPermisos');
GO

/* 8.10) Digito verificador de las tablas nuevas (arrancan en '0' = estado inicial) */
INSERT INTO dbo.DigitoVerificador (NombreTabla, DVH, DVV, FechaCalculo, Valido)
SELECT v.t, '0', '0', GETDATE(), 1
FROM (VALUES ('Cotizaciones'), ('CotizacionesDetalle'), ('PreOrdenesCompra'),
             ('PagosProveedor'), ('FacturasProveedor')) v(t)
WHERE v.t NOT IN (SELECT NombreTabla FROM dbo.DigitoVerificador);
GO

/* Verificacion rapida */
SELECT Nombre FROM dbo.Modulos ORDER BY IdModulo;
SELECT NombreTabla, DVH, DVV, Valido FROM dbo.DigitoVerificador ORDER BY IdDV;
SELECT name FROM sys.tables WHERE name IN (N'Cotizaciones', N'CotizacionesDetalle', N'PreOrdenesCompra', N'PagosProveedor', N'FacturasProveedor');
GO
