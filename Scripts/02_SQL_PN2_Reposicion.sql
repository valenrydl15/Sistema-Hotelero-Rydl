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

/* Verificacion rapida */
SELECT Nombre FROM dbo.Modulos ORDER BY IdModulo;
SELECT NombreTabla, DVH, DVV, Valido FROM dbo.DigitoVerificador ORDER BY IdDV;
GO
