/* =====================================================================
   PN2 - GESTION DE REPOSICION DE INSUMOS  (version 3, segun la catedra)

   CUN06 Generar solicitud de cotizacion   (Encargado de Compras)
   CUN07 Pre-registrar proveedor           (Encargado de Compras)  <<extend>> CUN06
   CUN08 Generar pre-orden de compra       (Encargado de Compras)
   CUN09 Generar orden de compra           (Gerente de Compras)
   CUN10 Registrar proveedor               (Gerente de Compras)    <<extend>> CUN09
   CUN11 Pagar orden de compra             (Gerente de Compras)    <<include>> desde CUN09
   CUN12 Registrar recepcion de insumos    (Encargado de Deposito)

   Script IDEMPOTENTE: se puede ejecutar mas de una vez sin duplicar nada.
   La aplicacion lo ejecuta sola al iniciar; correrlo a mano es opcional.

   IMPORTANTE: la primera vez que se ejecuta sobre una base con el esquema anterior del PN2
   (SolicitudesCompra, OrdenesCompra de un solo insumo, reembolsos, pre-ordenes, etc.)
   ELIMINA esas tablas transaccionales y las vuelve a crear con la estructura nueva.
   Insumos y Proveedores se conservan. Hacer un backup antes si se quieren guardar los datos viejos.
   ===================================================================== */
USE [HotelRydlDB]
GO

/* ---------------------------------------------------------------------
   1) MIGRACION: si la base no tiene el esquema v3, se borran las tablas transaccionales viejas del PN2
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.SolicitudesCotizacion', N'U') IS NULL
BEGIN
    DECLARE @t NVARCHAR(128), @sql NVARCHAR(MAX);
    DECLARE @viejas TABLE (orden INT, nombre NVARCHAR(128));
    INSERT INTO @viejas VALUES
     (1, N'Notificaciones'), (2, N'MovimientosStock'), (3, N'SolicitudesReembolso'), (4, N'RecepcionesInsumos'),
     (5, N'FacturasProveedor'), (6, N'OrdenesCompra'), (7, N'PagosProveedor'), (8, N'PreOrdenesCompra'),
     (9, N'CotizacionesDetalle'), (10, N'Cotizaciones'), (11, N'SolicitudesCompra');

    /* primero se quitan todas las FK que apuntan a esas tablas o salen de ellas */
    SET @sql = N'';
    SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
                + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';'
    FROM sys.foreign_keys fk
    WHERE OBJECT_NAME(fk.referenced_object_id) IN (SELECT nombre FROM @viejas)
       OR OBJECT_NAME(fk.parent_object_id) IN (SELECT nombre FROM @viejas);
    IF LEN(@sql) > 0 EXEC sp_executesql @sql;

    DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT nombre FROM @viejas ORDER BY orden;
    OPEN c; FETCH NEXT FROM c INTO @t;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF OBJECT_ID(N'dbo.' + @t, N'U') IS NOT NULL
        BEGIN
            SET @sql = N'DROP TABLE dbo.' + QUOTENAME(@t) + N';';
            EXEC sp_executesql @sql;
        END;
        FETCH NEXT FROM c INTO @t;
    END;
    CLOSE c; DEALLOCATE c;

    /* filas del DV de tablas que ya no existen */
    DELETE FROM dbo.DigitoVerificador
     WHERE NombreTabla IN (N'SolicitudesCompra', N'SolicitudesReembolso', N'PreOrdenesCompra', N'PagosProveedor', N'FacturasProveedor');
END;
GO

/* ---------------------------------------------------------------------
   2) INSUMOS Y PROVEEDORES (se conservan; se agregan columnas)
   --------------------------------------------------------------------- */
DECLARE @cIns BIT = 0;
IF COL_LENGTH(N'dbo.Insumos', N'Categoria') IS NULL
BEGIN ALTER TABLE dbo.Insumos ADD Categoria NVARCHAR(50) NOT NULL CONSTRAINT DF_Insumos_Categoria DEFAULT (N'General'); SET @cIns = 1; END;
IF @cIns = 1
    UPDATE dbo.DigitoVerificador SET DVH = '0', DVV = '0', FechaCalculo = GETDATE(), Valido = 1 WHERE NombreTabla = 'Insumos';
GO

DECLARE @cProv BIT = 0;
IF COL_LENGTH(N'dbo.Proveedores', N'CUIT') IS NULL BEGIN ALTER TABLE dbo.Proveedores ADD CUIT NVARCHAR(13) NULL; SET @cProv = 1; END;
IF COL_LENGTH(N'dbo.Proveedores', N'CBU') IS NULL BEGIN ALTER TABLE dbo.Proveedores ADD CBU NVARCHAR(22) NULL; SET @cProv = 1; END;
IF COL_LENGTH(N'dbo.Proveedores', N'Banco') IS NULL BEGIN ALTER TABLE dbo.Proveedores ADD Banco NVARCHAR(100) NULL; SET @cProv = 1; END;
IF COL_LENGTH(N'dbo.Proveedores', N'FechaRegistroPago') IS NULL BEGIN ALTER TABLE dbo.Proveedores ADD FechaRegistroPago DATETIME NULL; SET @cProv = 1; END;
IF COL_LENGTH(N'dbo.Proveedores', N'Titular') IS NULL BEGIN ALTER TABLE dbo.Proveedores ADD Titular NVARCHAR(150) NULL; SET @cProv = 1; END;
IF COL_LENGTH(N'dbo.Proveedores', N'Estado') IS NULL
BEGIN ALTER TABLE dbo.Proveedores ADD Estado NVARCHAR(20) NOT NULL CONSTRAINT DF_Proveedores_Estado DEFAULT (N'Pre-registrado'); SET @cProv = 1; END;
IF @cProv = 1
    UPDATE dbo.DigitoVerificador SET DVH = '0', DVV = '0', FechaCalculo = GETDATE(), Valido = 1 WHERE NombreTabla = 'Proveedores';
GO
/* Un proveedor con CUIT y CBU ya cargados queda Registrado; el resto, Pre-registrado */
UPDATE dbo.Proveedores SET Estado = N'Registrado'
 WHERE Estado <> N'Registrado' AND CUIT IS NOT NULL AND CBU IS NOT NULL AND Titular IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Proveedores_Estado')
    ALTER TABLE dbo.Proveedores WITH NOCHECK ADD CONSTRAINT CK_Proveedores_Estado CHECK (Estado IN (N'Pre-registrado', N'Registrado'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Insumos_Stock')
    ALTER TABLE dbo.Insumos WITH NOCHECK ADD CONSTRAINT CK_Insumos_Stock CHECK (StockActual >= 0 AND StockMinimo >= 0);
GO

/* ---------------------------------------------------------------------
   3) TABLAS DEL PROCESO
   --------------------------------------------------------------------- */
/* CUN06: solicitud de cotizacion (una por proveedor) */
IF OBJECT_ID(N'dbo.SolicitudesCotizacion', N'U') IS NULL
CREATE TABLE dbo.SolicitudesCotizacion (
    IdSolicitudCotizacion INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    NroSolicitud          NVARCHAR(20)  NOT NULL,
    FechaSolicitud        DATETIME      NOT NULL CONSTRAINT DF_SolCot_Fecha DEFAULT (GETDATE()),
    IdProveedor           INT           NOT NULL,
    Estado                NVARCHAR(20)  NOT NULL CONSTRAINT DF_SolCot_Estado DEFAULT (N'Enviada'),
    FechaEnvio            DATETIME      NULL,
    IdUsuarioRegistro     INT           NULL,
    CONSTRAINT UQ_SolCot_Nro       UNIQUE (NroSolicitud),
    CONSTRAINT FK_SolCot_Proveedor FOREIGN KEY (IdProveedor)       REFERENCES dbo.Proveedores(IdProveedor),
    CONSTRAINT FK_SolCot_Usuario   FOREIGN KEY (IdUsuarioRegistro) REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT CK_SolCot_Estado    CHECK (Estado IN (N'Enviada', N'Cotizada'))
);
GO
IF OBJECT_ID(N'dbo.SolicitudesCotizacionDetalle', N'U') IS NULL
CREATE TABLE dbo.SolicitudesCotizacionDetalle (
    IdDetalleSolicitud    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdSolicitudCotizacion INT           NOT NULL,
    IdInsumo              INT           NOT NULL,
    Cantidad              DECIMAL(10,2) NOT NULL,
    CONSTRAINT FK_SolCotDet_Solicitud FOREIGN KEY (IdSolicitudCotizacion) REFERENCES dbo.SolicitudesCotizacion(IdSolicitudCotizacion),
    CONSTRAINT FK_SolCotDet_Insumo    FOREIGN KEY (IdInsumo)              REFERENCES dbo.Insumos(IdInsumo),
    CONSTRAINT UQ_SolCotDet           UNIQUE (IdSolicitudCotizacion, IdInsumo),
    CONSTRAINT CK_SolCotDet_Cantidad  CHECK (Cantidad > 0)
);
GO

/* CUN08: cotizacion que responde el proveedor (con sus datos bancarios) */
IF OBJECT_ID(N'dbo.Cotizaciones', N'U') IS NULL
CREATE TABLE dbo.Cotizaciones (
    IdCotizacion          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdSolicitudCotizacion INT           NOT NULL,
    IdProveedor           INT           NOT NULL,
    FechaCotizacion       DATETIME      NOT NULL CONSTRAINT DF_Cot_Fecha DEFAULT (GETDATE()),
    CUIT                  NVARCHAR(13)  NOT NULL,
    Banco                 NVARCHAR(100) NOT NULL,
    CBU                   NVARCHAR(22)  NOT NULL,
    Titular               NVARCHAR(150) NOT NULL,
    Estado                NVARCHAR(20)  NOT NULL CONSTRAINT DF_Cot_Estado DEFAULT (N'Recibida'),
    IdUsuarioRegistro     INT           NULL,
    CONSTRAINT UQ_Cot_Solicitud  UNIQUE (IdSolicitudCotizacion),
    CONSTRAINT FK_Cot_Solicitud  FOREIGN KEY (IdSolicitudCotizacion) REFERENCES dbo.SolicitudesCotizacion(IdSolicitudCotizacion),
    CONSTRAINT FK_Cot_Proveedor  FOREIGN KEY (IdProveedor)           REFERENCES dbo.Proveedores(IdProveedor),
    CONSTRAINT FK_Cot_Usuario    FOREIGN KEY (IdUsuarioRegistro)     REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT CK_Cot_Estado     CHECK (Estado IN (N'Recibida', N'Procesada'))
);
GO
IF OBJECT_ID(N'dbo.CotizacionesDetalle', N'U') IS NULL
CREATE TABLE dbo.CotizacionesDetalle (
    IdDetalleCotizacion INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdCotizacion        INT           NOT NULL,
    IdInsumo            INT           NOT NULL,
    CantidadDisponible  DECIMAL(10,2) NOT NULL,
    PrecioUnitario      DECIMAL(10,2) NOT NULL,
    CONSTRAINT FK_CotDet_Cotizacion FOREIGN KEY (IdCotizacion) REFERENCES dbo.Cotizaciones(IdCotizacion),
    CONSTRAINT FK_CotDet_Insumo     FOREIGN KEY (IdInsumo)     REFERENCES dbo.Insumos(IdInsumo),
    CONSTRAINT UQ_CotDet            UNIQUE (IdCotizacion, IdInsumo),
    CONSTRAINT CK_CotDet_Valores    CHECK (CantidadDisponible > 0 AND PrecioUnitario > 0)
);
GO

/* CUN08/CUN09/CUN11/CUN12: la orden de compra recorre los estados Pre-orden -> Generada -> Pagada -> Recibida (o Cancelada) */
IF OBJECT_ID(N'dbo.OrdenesCompra', N'U') IS NULL
CREATE TABLE dbo.OrdenesCompra (
    IdOrden            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    NroOrden           NVARCHAR(20)  NOT NULL,
    FechaPreOrden      DATETIME      NOT NULL CONSTRAINT DF_Orden_Fecha DEFAULT (GETDATE()),
    IdProveedor        INT           NOT NULL,
    IdCotizacion       INT           NOT NULL,
    Total              DECIMAL(12,2) NOT NULL,
    Estado             NVARCHAR(20)  NOT NULL CONSTRAINT DF_Orden_Estado DEFAULT (N'Pre-orden'),
    FechaGeneracion    DATETIME      NULL,
    MotivoCancelacion  NVARCHAR(250) NULL,
    DireccionEntrega   NVARCHAR(300) NULL,
    FechaEnvio         DATETIME      NULL,
    IdUsuarioRegistro  INT           NULL,
    IdUsuarioGerente   INT           NULL,
    CONSTRAINT UQ_Orden_Nro        UNIQUE (NroOrden),
    CONSTRAINT FK_Orden_Proveedor  FOREIGN KEY (IdProveedor)       REFERENCES dbo.Proveedores(IdProveedor),
    CONSTRAINT FK_Orden_Cotizacion FOREIGN KEY (IdCotizacion)      REFERENCES dbo.Cotizaciones(IdCotizacion),
    CONSTRAINT FK_Orden_Usuario    FOREIGN KEY (IdUsuarioRegistro) REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT FK_Orden_Gerente    FOREIGN KEY (IdUsuarioGerente)  REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT CK_Orden_Total      CHECK (Total > 0),
    CONSTRAINT CK_Orden_Estado     CHECK (Estado IN (N'Pre-orden', N'Generada', N'Cancelada', N'Pagada', N'Recibida'))
);
GO
IF OBJECT_ID(N'dbo.OrdenesCompraDetalle', N'U') IS NULL
CREATE TABLE dbo.OrdenesCompraDetalle (
    IdDetalleOrden  INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdOrden         INT           NOT NULL,
    IdInsumo        INT           NOT NULL,
    Cantidad        DECIMAL(10,2) NOT NULL,
    PrecioUnitario  DECIMAL(10,2) NOT NULL,
    Subtotal        AS (Cantidad * PrecioUnitario) PERSISTED,
    CONSTRAINT FK_OrdenDet_Orden  FOREIGN KEY (IdOrden)  REFERENCES dbo.OrdenesCompra(IdOrden),
    CONSTRAINT FK_OrdenDet_Insumo FOREIGN KEY (IdInsumo) REFERENCES dbo.Insumos(IdInsumo),
    CONSTRAINT UQ_OrdenDet        UNIQUE (IdOrden, IdInsumo),
    CONSTRAINT CK_OrdenDet_Valores CHECK (Cantidad > 0 AND PrecioUnitario > 0)
);
GO

/* CUN11: pago de la orden por transferencia (se guardan tambien los rechazados) */
IF OBJECT_ID(N'dbo.PagosOrdenCompra', N'U') IS NULL
CREATE TABLE dbo.PagosOrdenCompra (
    IdPago             INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdOrden            INT           NOT NULL,
    Monto              DECIMAL(12,2) NOT NULL,
    Banco              NVARCHAR(100) NOT NULL,
    CBU                NVARCHAR(22)  NOT NULL,
    Titular            NVARCHAR(150) NOT NULL,
    NroOperacion       NVARCHAR(40)  NULL,
    Estado             NVARCHAR(20)  NOT NULL,
    FechaPago          DATETIME      NOT NULL CONSTRAINT DF_Pago_Fecha DEFAULT (GETDATE()),
    DetalleBanco       NVARCHAR(200) NULL,
    IdUsuarioRegistro  INT           NULL,
    CONSTRAINT FK_Pago_Orden   FOREIGN KEY (IdOrden)           REFERENCES dbo.OrdenesCompra(IdOrden),
    CONSTRAINT FK_Pago_Usuario FOREIGN KEY (IdUsuarioRegistro) REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT CK_Pago_Monto   CHECK (Monto > 0),
    CONSTRAINT CK_Pago_Estado  CHECK (Estado IN (N'Aprobado', N'Rechazado'))
);
GO

/* CUN12: recepcion de la mercaderia con la factura del proveedor */
IF OBJECT_ID(N'dbo.RecepcionesInsumos', N'U') IS NULL
CREATE TABLE dbo.RecepcionesInsumos (
    IdRecepcion        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdOrden            INT           NOT NULL,
    NroFactura         NVARCHAR(30)  NOT NULL,
    TotalFactura       DECIMAL(12,2) NOT NULL,
    FechaRecepcion     DATETIME      NOT NULL CONSTRAINT DF_Recep_Fecha DEFAULT (GETDATE()),
    Estado             NVARCHAR(20)  NOT NULL,
    Observaciones      NVARCHAR(300) NULL,
    IdUsuarioRegistro  INT           NULL,
    CONSTRAINT UQ_Recep_Orden   UNIQUE (IdOrden),
    CONSTRAINT FK_Recep_Orden   FOREIGN KEY (IdOrden)           REFERENCES dbo.OrdenesCompra(IdOrden),
    CONSTRAINT FK_Recep_Usuario FOREIGN KEY (IdUsuarioRegistro) REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT CK_Recep_Total   CHECK (TotalFactura > 0),
    CONSTRAINT CK_Recep_Estado  CHECK (Estado IN (N'Completa', N'Con diferencia'))
);
GO
IF OBJECT_ID(N'dbo.RecepcionesInsumosDetalle', N'U') IS NULL
CREATE TABLE dbo.RecepcionesInsumosDetalle (
    IdDetalleRecepcion INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdRecepcion        INT           NOT NULL,
    IdInsumo           INT           NOT NULL,
    CantidadPedida     DECIMAL(10,2) NOT NULL,
    CantidadRecibida   DECIMAL(10,2) NOT NULL,
    CONSTRAINT FK_RecepDet_Recepcion FOREIGN KEY (IdRecepcion) REFERENCES dbo.RecepcionesInsumos(IdRecepcion),
    CONSTRAINT FK_RecepDet_Insumo    FOREIGN KEY (IdInsumo)    REFERENCES dbo.Insumos(IdInsumo),
    CONSTRAINT UQ_RecepDet           UNIQUE (IdRecepcion, IdInsumo),
    CONSTRAINT CK_RecepDet_Cantidad  CHECK (CantidadRecibida >= 0)
);
GO

/* Trazabilidad del stock (cantidad anterior / recibida / disponible) */
IF OBJECT_ID(N'dbo.MovimientosStock', N'U') IS NULL
CREATE TABLE dbo.MovimientosStock (
    IdMovimiento       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdInsumo           INT           NOT NULL,
    IdRecepcion        INT           NULL,
    Tipo               NVARCHAR(20)  NOT NULL CONSTRAINT DF_MovStock_Tipo DEFAULT (N'Ingreso'),
    CantidadAnterior   DECIMAL(10,2) NOT NULL,
    CantidadMovimiento DECIMAL(10,2) NOT NULL,
    CantidadNueva      DECIMAL(10,2) NOT NULL,
    FechaMovimiento    DATETIME      NOT NULL CONSTRAINT DF_MovStock_Fecha DEFAULT (GETDATE()),
    IdUsuarioRegistro  INT           NULL,
    CONSTRAINT FK_MovStock_Insumo    FOREIGN KEY (IdInsumo)          REFERENCES dbo.Insumos(IdInsumo),
    CONSTRAINT FK_MovStock_Recepcion FOREIGN KEY (IdRecepcion)       REFERENCES dbo.RecepcionesInsumos(IdRecepcion),
    CONSTRAINT FK_MovStock_Usuario   FOREIGN KEY (IdUsuarioRegistro) REFERENCES dbo.Usuarios(IdUsuario)
);
GO

/* Avisos internos entre roles y registro de envios a proveedores */
IF OBJECT_ID(N'dbo.Notificaciones', N'U') IS NULL
CREATE TABLE dbo.Notificaciones (
    IdNotificacion     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdUsuarioDestino   INT           NULL,
    IdProveedorDestino INT           NULL,
    IdOrden            INT           NULL,
    Tipo               NVARCHAR(40)  NOT NULL,
    Mensaje            NVARCHAR(400) NOT NULL,
    Fecha              DATETIME      NOT NULL CONSTRAINT DF_Notificaciones_Fecha DEFAULT (GETDATE()),
    Leida              BIT           NOT NULL CONSTRAINT DF_Notificaciones_Leida DEFAULT (0),
    CONSTRAINT FK_Notif_Usuario   FOREIGN KEY (IdUsuarioDestino)   REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT FK_Notif_Proveedor FOREIGN KEY (IdProveedorDestino) REFERENCES dbo.Proveedores(IdProveedor),
    CONSTRAINT FK_Notif_Orden     FOREIGN KEY (IdOrden)            REFERENCES dbo.OrdenesCompra(IdOrden),
    CONSTRAINT CK_Notif_Destino   CHECK (IdUsuarioDestino IS NOT NULL OR IdProveedorDestino IS NOT NULL)
);
GO

/* Indices de consulta */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SolCot_Estado' AND object_id = OBJECT_ID(N'dbo.SolicitudesCotizacion'))
    CREATE INDEX IX_SolCot_Estado ON dbo.SolicitudesCotizacion (Estado, FechaSolicitud);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Ordenes_Estado' AND object_id = OBJECT_ID(N'dbo.OrdenesCompra'))
    CREATE INDEX IX_Ordenes_Estado ON dbo.OrdenesCompra (Estado, FechaPreOrden);
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
 (N'Consulta de stock',                         5),
 (N'Solicitud de cotización generada',          4),
 (N'Proveedor pre-registrado',                  4),
 (N'Cotización registrada',                     4),
 (N'Pre-orden de compra generada',              4),
 (N'Orden de compra generada',                  3),
 (N'Orden de compra cancelada',                 3),
 (N'Proveedor registrado',                      3),
 (N'Pago de orden de compra aprobado',          3),
 (N'Pago de orden de compra rechazado',         2),
 (N'Orden de compra enviada al proveedor',      4),
 (N'Error al enviar al proveedor',              2),
 (N'Recepción de insumos registrada',           3),
 (N'Recepción con diferencia',                  2),
 (N'Stock actualizado',                         4),
 (N'Operación de reposición rechazada',         3),
 (N'Acceso denegado a reposición',              2),
 (N'Error técnico de reposición',               1),
 (N'Reporte de orden de compra exportado',      4);
INSERT INTO dbo.TiposEvento (IdModulo, Nombre, Criticidad)
SELECT @mod, e.Nombre, e.Criticidad FROM @ev e
WHERE NOT EXISTS (SELECT 1 FROM dbo.TiposEvento t WHERE t.IdModulo = @mod AND t.Nombre = e.Nombre);
GO
/* Reportes PDF del modulo de Check-in/Check-out (factura) */
IF EXISTS (SELECT 1 FROM dbo.Modulos WHERE IdModulo = 2)
   AND NOT EXISTS (SELECT 1 FROM dbo.TiposEvento WHERE IdModulo = 2 AND Nombre = N'Reporte de factura exportado')
    INSERT INTO dbo.TiposEvento (IdModulo, Nombre, Criticidad) VALUES (2, N'Reporte de factura exportado', 4);
GO

/* ---------------------------------------------------------------------
   5) ROLES Y PERMISOS
      Encargado de Compras · Gerente de Compras · Encargado de Depósito (antes "Encargado de Almacén")
   --------------------------------------------------------------------- */
DECLARE @cambios BIT = 0;

IF EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Encargado de Almacén')
   AND NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Encargado de Depósito')
BEGIN
    UPDATE dbo.Roles SET Nombre = N'Encargado de Depósito', Descripcion = N'Recibe la mercadería con la factura y registra la recepción de insumos'
     WHERE Nombre = N'Encargado de Almacén';
    SET @cambios = 1;
END;
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Encargado de Depósito')
BEGIN
    INSERT INTO dbo.Roles (Nombre, Descripcion) VALUES (N'Encargado de Depósito', N'Recibe la mercadería con la factura y registra la recepción de insumos');
    SET @cambios = 1;
END;
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Encargado de Compras')
BEGIN
    INSERT INTO dbo.Roles (Nombre, Descripcion) VALUES (N'Encargado de Compras', N'Analiza faltantes, pide cotizaciones, pre-registra proveedores y genera pre-órdenes');
    SET @cambios = 1;
END;
ELSE IF EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Encargado de Compras' AND ISNULL(Descripcion, N'') <> N'Analiza faltantes, pide cotizaciones, pre-registra proveedores y genera pre-órdenes')
BEGIN
    UPDATE dbo.Roles SET Descripcion = N'Analiza faltantes, pide cotizaciones, pre-registra proveedores y genera pre-órdenes' WHERE Nombre = N'Encargado de Compras';
    SET @cambios = 1;
END;
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Gerente de Compras')
BEGIN
    INSERT INTO dbo.Roles (Nombre, Descripcion) VALUES (N'Gerente de Compras', N'Aprueba pre-órdenes, registra proveedores y paga las órdenes de compra');
    SET @cambios = 1;
END;

DECLARE @perm TABLE (Nombre NVARCHAR(100), Descripcion NVARCHAR(300));
INSERT INTO @perm VALUES
 (N'Consultar stock de insumos',            N'Ver el stock y los insumos faltantes (por debajo del mínimo)'),
 (N'Generar solicitud de cotización',       N'Pedir cotización de los insumos faltantes a un proveedor'),
 (N'Pre-registrar proveedor',               N'Dar de alta un proveedor nuevo con sus datos básicos'),
 (N'Generar pre-orden de compra',           N'Registrar cotizaciones, compararlas y generar pre-órdenes'),
 (N'Generar orden de compra',               N'Aprobar o cancelar pre-órdenes de compra'),
 (N'Registrar proveedor',                   N'Completar CUIT, banco, CBU y titular de un proveedor pre-registrado'),
 (N'Pagar orden de compra',                 N'Pagar la orden por transferencia bancaria y enviarla al proveedor'),
 (N'Registrar recepción de insumos',        N'Registrar la mercadería recibida con su factura y actualizar el stock');
INSERT INTO dbo.Permisos (Nombre, Descripcion, Activo)
SELECT p.Nombre, p.Descripcion, 1 FROM @perm p
WHERE NOT EXISTS (SELECT 1 FROM dbo.Permisos x WHERE x.Nombre = p.Nombre);
IF @@ROWCOUNT > 0 SET @cambios = 1;

DECLARE @mat TABLE (Rol NVARCHAR(50), Permiso NVARCHAR(100));
INSERT INTO @mat
SELECT N'Administrador', Nombre FROM @perm
UNION ALL SELECT N'Encargado de Compras', N'Consultar stock de insumos'
UNION ALL SELECT N'Encargado de Compras', N'Generar solicitud de cotización'
UNION ALL SELECT N'Encargado de Compras', N'Pre-registrar proveedor'
UNION ALL SELECT N'Encargado de Compras', N'Generar pre-orden de compra'
UNION ALL SELECT N'Gerente de Compras',   N'Generar orden de compra'
UNION ALL SELECT N'Gerente de Compras',   N'Registrar proveedor'
UNION ALL SELECT N'Gerente de Compras',   N'Pagar orden de compra'
UNION ALL SELECT N'Encargado de Depósito', N'Consultar stock de insumos'
UNION ALL SELECT N'Encargado de Depósito', N'Registrar recepción de insumos';
INSERT INTO dbo.RolPermisos (IdRol, IdPermiso)
SELECT r.IdRol, p.IdPermiso
FROM @mat m
JOIN dbo.Roles    r ON r.Nombre = m.Rol
JOIN dbo.Permisos p ON p.Nombre = m.Permiso
WHERE NOT EXISTS (SELECT 1 FROM dbo.RolPermisos rp WHERE rp.IdRol = r.IdRol AND rp.IdPermiso = p.IdPermiso);
IF @@ROWCOUNT > 0 SET @cambios = 1;

IF @cambios = 1
    UPDATE dbo.DigitoVerificador SET DVH = '0', DVV = '0', FechaCalculo = GETDATE(), Valido = 1
     WHERE NombreTabla IN ('Roles', 'Permisos', 'RolPermisos');
GO

/* ---------------------------------------------------------------------
   6) DIGITO VERIFICADOR: tablas del proceso (las nuevas o recreadas arrancan en '0' = estado inicial)
   --------------------------------------------------------------------- */
DECLARE @dv TABLE (t NVARCHAR(128));
INSERT INTO @dv VALUES ('Insumos'), ('Proveedores'), ('SolicitudesCotizacion'), ('SolicitudesCotizacionDetalle'),
 ('Cotizaciones'), ('CotizacionesDetalle'), ('OrdenesCompra'), ('OrdenesCompraDetalle'), ('PagosOrdenCompra'),
 ('RecepcionesInsumos'), ('RecepcionesInsumosDetalle'), ('MovimientosStock');
INSERT INTO dbo.DigitoVerificador (NombreTabla, DVH, DVV, FechaCalculo, Valido)
SELECT d.t, '0', '0', GETDATE(), 1 FROM @dv d
WHERE d.t NOT IN (SELECT NombreTabla FROM dbo.DigitoVerificador);
/* tablas recreadas vacias: su DV vuelve al estado inicial */
UPDATE dbo.DigitoVerificador SET DVH = '0', DVV = '0', FechaCalculo = GETDATE(), Valido = 1
 WHERE NombreTabla IN ('OrdenesCompra', 'RecepcionesInsumos', 'MovimientosStock', 'Cotizaciones', 'CotizacionesDetalle')
   AND NOT EXISTS (SELECT 1 FROM dbo.OrdenesCompra) AND NOT EXISTS (SELECT 1 FROM dbo.Cotizaciones)
   AND NOT EXISTS (SELECT 1 FROM dbo.RecepcionesInsumos) AND NOT EXISTS (SELECT 1 FROM dbo.MovimientosStock);
GO

/* ---------------------------------------------------------------------
   7) DATOS DE EJEMPLO (solo si las tablas estan vacias)
   --------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Insumos)
BEGIN
    INSERT INTO dbo.Insumos (Nombre, Descripcion, UnidadMedida, StockActual, StockMinimo, Categoria) VALUES
     (N'Sábanas queen',          N'Juego de sábanas 200 hilos',          N'unidades', 14,  20,  N'Blancos'),
     (N'Toallas de baño',        N'Toalla 70x140 blanca',                N'unidades', 85,  60,  N'Blancos'),
     (N'Toallas de mano',        N'Toalla 40x70 blanca',                 N'unidades', 40,  50,  N'Blancos'),
     (N'Jabón de tocador',       N'Jabón individual 30 g',               N'unidades', 220, 150, N'Amenities'),
     (N'Shampoo 30 ml',          N'Dosificador individual',              N'unidades', 90,  120, N'Amenities'),
     (N'Papel higiénico',        N'Rollo institucional 30 m',            N'rollos',   300, 200, N'Limpieza'),
     (N'Detergente multiuso',    N'Bidón para limpieza de habitaciones', N'litros',   18,  25,  N'Limpieza'),
     (N'Cápsulas de café',       N'Cápsulas para cafetera de cortesía',  N'unidades', 400, 250, N'Cafetería'),
     (N'Agua mineral 500 ml',    N'Botella para frigobar',               N'unidades', 60,  80,  N'Cafetería'),
     (N'Bolsas de residuos',     N'Bolsa 60x90 consorcio',               N'unidades', 500, 300, N'Limpieza');
END;
GO
/* Categoria de los insumos de ejemplo que ya existian */
UPDATE dbo.Insumos SET Categoria = CASE
        WHEN Nombre LIKE N'%sábana%' OR Nombre LIKE N'%toalla%' THEN N'Blancos'
        WHEN Nombre LIKE N'%jabón%' OR Nombre LIKE N'%shampoo%' THEN N'Amenities'
        WHEN Nombre LIKE N'%café%' OR Nombre LIKE N'%agua%' THEN N'Cafetería'
        WHEN Nombre LIKE N'%papel%' OR Nombre LIKE N'%detergente%' OR Nombre LIKE N'%bolsa%' THEN N'Limpieza'
        ELSE Categoria END
 WHERE Categoria = N'General';
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Proveedores)
BEGIN
    INSERT INTO dbo.Proveedores (Nombre, Contacto, Telefono, Email, Direccion, CondicionesPago, Activo, Estado) VALUES
     (N'Textil del Plata S.A.',       N'Marcela Ibarra',  N'011 4555-1020', N'ventas@textildelplata.example', N'Av. Belgrano 1450, CABA', N'Transferencia', 1, N'Pre-registrado'),
     (N'Distribuidora Limpia Hotel',  N'Rodrigo Paz',     N'011 4777-8800', N'pedidos@limpiahotel.example',   N'Ruta 8 km 32, Pilar',     N'Transferencia', 1, N'Pre-registrado'),
     (N'Insumos Gastronómicos Norte', N'Lucía Fernández', N'011 4111-3030', N'compras@gastronorte.example',   N'Av. Cabildo 2200, CABA',  N'Transferencia', 1, N'Pre-registrado');
END;
GO

/* Verificacion rapida */
SELECT name FROM sys.tables WHERE name IN (N'SolicitudesCotizacion', N'SolicitudesCotizacionDetalle', N'Cotizaciones', N'CotizacionesDetalle',
    N'OrdenesCompra', N'OrdenesCompraDetalle', N'PagosOrdenCompra', N'RecepcionesInsumos', N'RecepcionesInsumosDetalle', N'MovimientosStock', N'Notificaciones');
SELECT Nombre FROM dbo.Roles ORDER BY IdRol;
GO
