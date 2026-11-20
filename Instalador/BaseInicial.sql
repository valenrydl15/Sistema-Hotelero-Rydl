/* ═════════════════════════════════════════════════════════════════════
   HOTEL RYDL — HRS-480 · BASE INICIAL ("de fábrica")
   Lo ejecuta SOLO el instalador (modo INSTALAR), sobre la base recién creada.
   ¡NO ejecutarlo a mano sobre una base en uso: borra todos los datos cargados!

   SE CONSERVA (lo indispensable para que el sistema funcione):
     · Roles, Permisos, Familias y sus relaciones (perfiles / Composite)
     · Módulos y Tipos de evento de la bitácora
     · Tipos de habitación y Habitaciones (todas quedan Disponibles)
     · Catálogo de Insumos y Proveedores (configuración de la reposición)
     · Estructura completa: tablas, triggers, procedimientos
   SE VACÍA (datos cargados): usuarios, huéspedes/clientes, reservas, check-in,
     check-out, pagos, facturas, todo el circuito de compras, bitácora de eventos,
     bitácora de cambios (Insumos_C) y backups/inconsistencias del DV.
   SE CREA: el usuario administrador del primer ingreso (IdUsuario = 1, usuario maestro)
     Usuario: 12345678Admin (DNI + Nombre) · Contraseña inicial: 12345678Rydl (DNI + Apellido)
     Debe cambiar la contraseña en el primer ingreso.
   El DV de todas las tablas vuelve al estado inicial ('0'): el sistema lo calcula
   en el primer inicio de sesión.

   Es un solo lote (sin GO) dentro de una transacción: si algo falla, no cambia nada.
   ═════════════════════════════════════════════════════════════════════ */
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @tablas TABLE (Orden INT IDENTITY(1,1), Nombre SYSNAME);
INSERT INTO @tablas (Nombre) VALUES
 (N'Bitacora'), (N'InconsistenciasDV'), (N'BackupDV'),
 (N'Notificaciones'), (N'MovimientosStock'),
 (N'RecepcionesInsumosDetalle'), (N'RecepcionesInsumos'),
 (N'PagosOrdenCompra'), (N'OrdenesCompraDetalle'), (N'OrdenesCompra'),
 (N'CotizacionesDetalle'), (N'Cotizaciones'),
 (N'SolicitudesCotizacionDetalle'), (N'SolicitudesCotizacion'),
 (N'SolicitudesCompra'), (N'PreOrdenesCompra'), (N'FacturasProveedor'), (N'PagosProveedor'), (N'SolicitudesReembolso'),
 (N'Facturas'), (N'Pagos'), (N'CheckOuts'), (N'CheckIns'), (N'Reservas'),
 (N'Huespedes'), (N'Insumos_C'), (N'Usuarios');
-- solo las que existen en esta base
DELETE FROM @tablas WHERE OBJECT_ID(N'dbo.' + QUOTENAME(Nombre), N'U') IS NULL;

DECLARE @sql NVARCHAR(MAX);

BEGIN TRY
    BEGIN TRANSACTION;

    /* 1) Claves foráneas en pausa, así el orden de borrado no importa */
    SET @sql = N'';
    SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
                       + N' NOCHECK CONSTRAINT ALL;' + NCHAR(10)
    FROM sys.tables t
    WHERE t.is_ms_shipped = 0;
    EXEC sp_executesql @sql;

    /* 2) Vaciado de los datos cargados. Los triggers de cada tabla se apagan mientras se borra
          (Insumos_C rechaza los borrados por diseño: acá se vacía porque la base es nueva). */
    SET @sql = N'';
    SELECT @sql = @sql
        + N'DISABLE TRIGGER ALL ON dbo.' + QUOTENAME(Nombre) + N'; '
        + N'DELETE FROM dbo.' + QUOTENAME(Nombre) + N'; '
        + N'ENABLE TRIGGER ALL ON dbo.' + QUOTENAME(Nombre) + N';' + NCHAR(10)
    FROM @tablas;
    EXEC sp_executesql @sql;

    /* 3) Los Id vuelven a empezar desde 1 */
    SET @sql = N'';
    SELECT @sql = @sql + N'DBCC CHECKIDENT (''dbo.' + t.Nombre + N''', RESEED, 0) WITH NO_INFOMSGS;' + NCHAR(10)
    FROM @tablas t
    INNER JOIN sys.identity_columns ic ON ic.object_id = OBJECT_ID(N'dbo.' + QUOTENAME(t.Nombre))
    WHERE ic.last_value IS NOT NULL;
    EXEC sp_executesql @sql;

    /* 4) Administrador del primer ingreso (usuario maestro: IdUsuario = 1) */
    DECLARE @idRolAdmin INT = (SELECT TOP (1) IdRol FROM dbo.Roles WHERE Nombre = N'Administrador' ORDER BY IdRol);
    IF @idRolAdmin IS NULL
    BEGIN
        ;THROW 50010, N'No existe el rol Administrador en la base inicial.', 1;
    END

    SET IDENTITY_INSERT dbo.Usuarios ON;
    INSERT INTO dbo.Usuarios
        (IdUsuario, Nombre, Apellido, DNI, Email, NombreUsuario, ContrasenaHash, IdRol,
         Activo, Bloqueado, IntentosFallidos, MaxIntentos, DebeChangiarClave, FechaCreacion, FechaUltimoAcceso, SesionActiva)
    VALUES
        (1, N'Admin', N'Rydl', N'12345678', NULL, N'12345678Admin',
         N'9e8049d3b7429fdc4c0daf5518963ab24fb279294e04106e078db40ecc8559aa',   -- SHA-256 de "12345678Rydl"
         @idRolAdmin, 1, 0, 0, 3, 1, GETDATE(), NULL, 0);
    SET IDENTITY_INSERT dbo.Usuarios OFF;
    DBCC CHECKIDENT ('dbo.Usuarios', RESEED, 1) WITH NO_INFOMSGS;

    /* 5) Habitaciones: hotel sin huéspedes, todas disponibles */
    IF OBJECT_ID(N'dbo.Habitaciones', N'U') IS NOT NULL
        UPDATE dbo.Habitaciones SET Estado = N'Disponible' WHERE Estado <> N'Disponible';

    /* 6) Bitácora de cambios: cada insumo arranca con su versión "Carga inicial" vigente
          (si Insumos_C todavía no existe, la crea y la carga el sistema al iniciar) */
    IF OBJECT_ID(N'dbo.Insumos_C', N'U') IS NOT NULL
        EXEC sp_executesql N'
        INSERT INTO dbo.Insumos_C (IdInsumo, Fecha, Hora, Nombre, Descripcion, Categoria, UnidadMedida,
                                   StockActual, StockMinimo, Activo, Act, Operacion, IdUsuario)
        SELECT i.IdInsumo, CAST(GETDATE() AS DATE), CAST(GETDATE() AS TIME(0)), i.Nombre, i.Descripcion, i.Categoria,
               i.UnidadMedida, i.StockActual, i.StockMinimo, i.Activo, 1, N''Carga inicial'', NULL
        FROM dbo.Insumos i;';

    /* 7) Dígito verificador en estado inicial: se calcula en el primer inicio de sesión */
    IF OBJECT_ID(N'dbo.DigitoVerificador', N'U') IS NOT NULL
        UPDATE dbo.DigitoVerificador SET DVH = '0', DVV = '0', FechaCalculo = GETDATE(), Valido = 1;

    /* 8) Claves foráneas activas otra vez */
    SET @sql = N'';
    SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
                       + N' CHECK CONSTRAINT ALL;' + NCHAR(10)
    FROM sys.tables t
    WHERE t.is_ms_shipped = 0;
    EXEC sp_executesql @sql;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
