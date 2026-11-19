/* ═════════════════════════════════════════════════════════════════════
   HOTEL RYDL — HRS-480 · Entrega 3
   Encriptación reversible de datos sensibles de clientes

   1) Huespedes.TarjetaCifrada: número de tarjeta cifrado con AES-256 (lo cifra la aplicación;
      la clave NO está en la base).
   2) Rol "Supervisor" y permiso "Ver datos sensibles de clientes" (solo para el Supervisor).
      El Administrador NO lo tiene, y aunque se le asigne, el sistema no le muestra el dato.
   3) Eventos de bitácora.

   Idempotente: se puede ejecutar varias veces. El sistema lo aplica solo al iniciar.
   ═════════════════════════════════════════════════════════════════════ */
USE HotelRydlDB;
GO

/* 1) Columna cifrada (el DV de Huespedes vuelve a estado inicial: la tabla cambió de estructura) */
IF COL_LENGTH(N'dbo.Huespedes', N'TarjetaCifrada') IS NULL
BEGIN
    ALTER TABLE dbo.Huespedes ADD TarjetaCifrada NVARCHAR(200) NULL;
    UPDATE dbo.DigitoVerificador SET DVH = '0', DVV = '0', FechaCalculo = GETDATE(), Valido = 1
     WHERE NombreTabla = 'Huespedes';
END
GO

/* 2) Rol Supervisor + permiso de datos sensibles */
DECLARE @cambios BIT = 0;

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Supervisor')
BEGIN
    INSERT INTO dbo.Roles (Nombre, Descripcion)
    VALUES (N'Supervisor', N'Supervisa la operación; puede ver los datos sensibles de los clientes (tarjeta) en el Maestro de Clientes');
    SET @cambios = 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Permisos WHERE Nombre = N'Ver datos sensibles de clientes')
BEGIN
    INSERT INTO dbo.Permisos (Nombre, Descripcion, Activo)
    VALUES (N'Ver datos sensibles de clientes', N'Ver descifrado el número de tarjeta de los clientes (encriptación reversible)', 1);
    SET @cambios = 1;
END

INSERT INTO dbo.RolPermisos (IdRol, IdPermiso)
SELECT r.IdRol, p.IdPermiso
FROM dbo.Roles r CROSS JOIN dbo.Permisos p
WHERE r.Nombre = N'Supervisor' AND p.Nombre = N'Ver datos sensibles de clientes'
  AND NOT EXISTS (SELECT 1 FROM dbo.RolPermisos rp WHERE rp.IdRol = r.IdRol AND rp.IdPermiso = p.IdPermiso);
IF @@ROWCOUNT > 0 SET @cambios = 1;

IF @cambios = 1
    UPDATE dbo.DigitoVerificador SET DVH = '0', DVV = '0', FechaCalculo = GETDATE(), Valido = 1
     WHERE NombreTabla IN ('Roles', 'Permisos', 'RolPermisos');
GO

/* 3) Bitácora de eventos */
DECLARE @ev TABLE (Modulo NVARCHAR(100), Nombre NVARCHAR(100), Criticidad INT);
INSERT INTO @ev VALUES
 (N'Maestros', N'Datos sensibles de clientes consultados', 2),
 (N'Maestros', N'Error al descifrar dato sensible',        1);

INSERT INTO dbo.TiposEvento (IdModulo, Nombre, Criticidad)
SELECT m.IdModulo, e.Nombre, e.Criticidad
FROM @ev e
INNER JOIN dbo.Modulos m ON m.Nombre = e.Modulo
WHERE NOT EXISTS (SELECT 1 FROM dbo.TiposEvento t WHERE t.IdModulo = m.IdModulo AND t.Nombre = e.Nombre);
GO

/* Control */
SELECT r.Nombre AS Rol, p.Nombre AS Permiso
FROM dbo.RolPermisos rp JOIN dbo.Roles r ON r.IdRol = rp.IdRol JOIN dbo.Permisos p ON p.IdPermiso = rp.IdPermiso
WHERE p.Nombre = N'Ver datos sensibles de clientes';
GO
