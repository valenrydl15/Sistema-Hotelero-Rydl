/* =====================================================================
   HRS-480 · Hotelería Rydl · Entrega 3 / Final — Menú MAESTROS
   - CUN13 Maestro de Clientes (Huéspedes): columna Direccion + eventos
   - Maestro de Insumos (alta, modificación, baja lógica)
   - CUN14 Bitácora de cambios de Insumos: tabla Insumos_C + TRIGGERS
   - Eventos de bitácora del Dígito Verificador (recálculo, backup, restore)
   Idempotente: se puede ejecutar más de una vez. La aplicación lo ejecuta
   sola al iniciar (MaestrosSetup) y después de restaurar un backup.
   ===================================================================== */
USE [HotelRydlDB]
GO
SET NOCOUNT ON;
GO

/* ─────────────────────────────────────────────────────────────────────
   1) HUÉSPEDES (= CLIENTES del hotel): se agrega Dirección
   Cambia la estructura de una tabla con DV: su DV vuelve al estado
   inicial ('0') y la app lo recalcula en el próximo inicio de sesión.
   ───────────────────────────────────────────────────────────────────── */
IF COL_LENGTH('dbo.Huespedes', 'Direccion') IS NULL
BEGIN
    ALTER TABLE dbo.Huespedes ADD Direccion NVARCHAR(200) NULL;
    UPDATE dbo.DigitoVerificador SET DVH = '0', DVV = '0', FechaCalculo = GETDATE(), Valido = 1
     WHERE NombreTabla = 'Huespedes';
END
GO

/* ─────────────────────────────────────────────────────────────────────
   2) INSUMOS_C — historial de cambios de Insumos (PRODUCTOS_C del enunciado)
   Un registro por cada cambio (alta, modificación, baja lógica o cambio de
   stock del PN2). Act = 1 marca la versión vigente de cada insumo: hay una
   sola por insumo. Nunca se borra un registro; solo se agregan o cambia Act.
   ───────────────────────────────────────────────────────────────────── */
IF OBJECT_ID('dbo.Insumos_C') IS NULL
BEGIN
    CREATE TABLE dbo.Insumos_C (
        IdCambio      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Insumos_C PRIMARY KEY,
        IdInsumo      INT            NOT NULL CONSTRAINT FK_Insumos_C_Insumos REFERENCES dbo.Insumos(IdInsumo),
        Fecha         DATE           NOT NULL CONSTRAINT DF_Insumos_C_Fecha DEFAULT (CAST(GETDATE() AS DATE)),
        Hora          TIME(0)        NOT NULL CONSTRAINT DF_Insumos_C_Hora  DEFAULT (CAST(GETDATE() AS TIME(0))),
        Nombre        NVARCHAR(150)  NOT NULL,
        Descripcion   NVARCHAR(300)  NULL,
        Categoria     NVARCHAR(50)   NULL,
        UnidadMedida  NVARCHAR(30)   NULL,
        StockActual   DECIMAL(10,2)  NOT NULL,
        StockMinimo   DECIMAL(10,2)  NOT NULL,
        Activo        BIT            NOT NULL,          -- borrado lógico del insumo en esa versión
        Act           BIT            NOT NULL,          -- 1 = versión vigente del insumo
        Operacion     NVARCHAR(20)   NOT NULL,          -- Carga inicial / Alta / Modificación / Baja / Reactivación
        IdUsuario     INT            NULL               -- quién hizo el cambio (SESSION_CONTEXT)
    );
    CREATE INDEX IX_Insumos_C_Insumo ON dbo.Insumos_C (IdInsumo, Act);
    CREATE INDEX IX_Insumos_C_Fecha  ON dbo.Insumos_C (Fecha, Hora);
    -- Una sola versión vigente por insumo
    CREATE UNIQUE INDEX UX_Insumos_C_Vigente ON dbo.Insumos_C (IdInsumo) WHERE Act = 1;
END
GO

/* ─────────────────────────────────────────────────────────────────────
   3) TRIGGERS
   ───────────────────────────────────────────────────────────────────── */

/* 3.a) TR_Insumos_RegistrarCambio — DÓNDE: tabla Insumos.
        CUÁNDO: AFTER INSERT, UPDATE (alta, modificación, baja lógica y cambios de stock).
        CÓMO: pasa a Act = 0 la versión vigente del insumo e inserta la nueva con Act = 1.
        Si el UPDATE no cambió ningún dato, no registra nada.
        Cuando se está ACTIVANDO una versión vieja (sp_ActivarVersionInsumo) no inserta:
        el procedimiento solo mueve el Act a la versión elegida. */
CREATE OR ALTER TRIGGER dbo.TR_Insumos_RegistrarCambio
ON dbo.Insumos
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted) RETURN;
    IF CAST(SESSION_CONTEXT(N'ActivandoVersion') AS INT) = 1 RETURN;

    DECLARE @usuario INT = TRY_CAST(SESSION_CONTEXT(N'IdUsuario') AS INT);

    -- Filas que realmente cambiaron (o son altas)
    DECLARE @cambios TABLE (IdInsumo INT PRIMARY KEY, Operacion NVARCHAR(20));
    INSERT INTO @cambios (IdInsumo, Operacion)
    SELECT i.IdInsumo,
           CASE WHEN d.IdInsumo IS NULL                 THEN N'Alta'
                WHEN d.Activo = 1 AND i.Activo = 0      THEN N'Baja'
                WHEN d.Activo = 0 AND i.Activo = 1      THEN N'Reactivación'
                ELSE N'Modificación' END
    FROM inserted i
    LEFT JOIN deleted d ON d.IdInsumo = i.IdInsumo
    WHERE d.IdInsumo IS NULL
       OR EXISTS (SELECT i.Nombre, i.Descripcion, i.Categoria, i.UnidadMedida, i.StockActual, i.StockMinimo, i.Activo
                  EXCEPT
                  SELECT d.Nombre, d.Descripcion, d.Categoria, d.UnidadMedida, d.StockActual, d.StockMinimo, d.Activo);

    IF NOT EXISTS (SELECT 1 FROM @cambios) RETURN;

    UPDATE c SET Act = 0
    FROM dbo.Insumos_C c
    INNER JOIN @cambios x ON x.IdInsumo = c.IdInsumo
    WHERE c.Act = 1;

    INSERT INTO dbo.Insumos_C (IdInsumo, Nombre, Descripcion, Categoria, UnidadMedida,
                               StockActual, StockMinimo, Activo, Act, Operacion, IdUsuario)
    SELECT i.IdInsumo, i.Nombre, i.Descripcion, i.Categoria, i.UnidadMedida,
           i.StockActual, i.StockMinimo, i.Activo, 1, x.Operacion, @usuario
    FROM inserted i
    INNER JOIN @cambios x ON x.IdInsumo = i.IdInsumo;
END
GO

/* 3.b) TR_Insumos_BorradoLogico — DÓNDE: tabla Insumos. CUÁNDO: INSTEAD OF DELETE.
        CÓMO: en lugar de borrar, pone Activo = 0 (borrado lógico). Ese UPDATE dispara
        TR_Insumos_RegistrarCambio y queda la versión "Baja" en Insumos_C. */
CREATE OR ALTER TRIGGER dbo.TR_Insumos_BorradoLogico
ON dbo.Insumos
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE i SET Activo = 0
    FROM dbo.Insumos i
    INNER JOIN deleted d ON d.IdInsumo = i.IdInsumo
    WHERE i.Activo = 1;
END
GO

/* 3.c) TR_Insumos_C_SinBorrado — DÓNDE: tabla Insumos_C. CUÁNDO: INSTEAD OF DELETE.
        CÓMO: rechaza cualquier borrado: el historial no se elimina nunca. */
CREATE OR ALTER TRIGGER dbo.TR_Insumos_C_SinBorrado
ON dbo.Insumos_C
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 50001, N'La bitácora de cambios (Insumos_C) no admite borrados.', 1;
END
GO

/* 3.d) TR_Insumos_C_SoloAct — DÓNDE: tabla Insumos_C. CUÁNDO: AFTER UPDATE.
        CÓMO: solo se permite cambiar la columna Act; si se tocó otro dato, deshace el UPDATE. */
CREATE OR ALTER TRIGGER dbo.TR_Insumos_C_SoloAct
ON dbo.Insumos_C
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i INNER JOIN deleted d ON d.IdCambio = i.IdCambio
               WHERE EXISTS (SELECT i.IdInsumo, i.Fecha, i.Hora, i.Nombre, i.Descripcion, i.Categoria, i.UnidadMedida,
                                    i.StockActual, i.StockMinimo, i.Activo, i.Operacion, i.IdUsuario
                             EXCEPT
                             SELECT d.IdInsumo, d.Fecha, d.Hora, d.Nombre, d.Descripcion, d.Categoria, d.UnidadMedida,
                                    d.StockActual, d.StockMinimo, d.Activo, d.Operacion, d.IdUsuario))
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50002, N'En la bitácora de cambios (Insumos_C) solo se puede modificar la columna Act.', 1;
    END
END
GO

/* ─────────────────────────────────────────────────────────────────────
   4) sp_ActivarVersionInsumo — botón ACTIVAR de la bitácora de cambios.
   Devuelve el insumo a los datos de la versión elegida y le pasa el Act = 1.
   No crea un registro nuevo: la versión elegida vuelve a ser la vigente.
   ───────────────────────────────────────────────────────────────────── */
CREATE OR ALTER PROCEDURE dbo.sp_ActivarVersionInsumo
    @IdCambio  INT,
    @IdUsuario INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @IdInsumo INT = (SELECT IdInsumo FROM dbo.Insumos_C WHERE IdCambio = @IdCambio);
    IF @IdInsumo IS NULL THROW 50003, N'La versión indicada no existe.', 1;

    BEGIN TRANSACTION;
        EXEC sp_set_session_context @key = N'ActivandoVersion', @value = 1;

        UPDATE i SET Nombre = c.Nombre, Descripcion = c.Descripcion, Categoria = c.Categoria,
                     UnidadMedida = c.UnidadMedida, StockActual = c.StockActual,
                     StockMinimo = c.StockMinimo, Activo = c.Activo
        FROM dbo.Insumos i
        INNER JOIN dbo.Insumos_C c ON c.IdCambio = @IdCambio
        WHERE i.IdInsumo = @IdInsumo;

        UPDATE dbo.Insumos_C SET Act = 0 WHERE IdInsumo = @IdInsumo AND Act = 1;
        UPDATE dbo.Insumos_C SET Act = 1 WHERE IdCambio = @IdCambio;

        EXEC sp_set_session_context @key = N'ActivandoVersion', @value = 0;
    COMMIT TRANSACTION;
END
GO

/* ─────────────────────────────────────────────────────────────────────
   5) Carga inicial: cada insumo sin historial recibe su primera versión vigente
   ───────────────────────────────────────────────────────────────────── */
INSERT INTO dbo.Insumos_C (IdInsumo, Fecha, Hora, Nombre, Descripcion, Categoria, UnidadMedida,
                           StockActual, StockMinimo, Activo, Act, Operacion, IdUsuario)
SELECT i.IdInsumo, CAST(GETDATE() AS DATE), CAST(GETDATE() AS TIME(0)), i.Nombre, i.Descripcion, i.Categoria,
       i.UnidadMedida, i.StockActual, i.StockMinimo, i.Activo, 1, N'Carga inicial', NULL
FROM dbo.Insumos i
WHERE NOT EXISTS (SELECT 1 FROM dbo.Insumos_C c WHERE c.IdInsumo = i.IdInsumo);
GO

/* ─────────────────────────────────────────────────────────────────────
   6) BITÁCORA DE EVENTOS: módulos y tipos de evento nuevos
   (el código busca los Ids por NOMBRE)
   ───────────────────────────────────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Nombre = N'Maestros')
    INSERT INTO dbo.Modulos (Nombre) VALUES (N'Maestros');
IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Nombre = N'Dígito Verificador')
    INSERT INTO dbo.Modulos (Nombre) VALUES (N'Dígito Verificador');
GO

DECLARE @ev TABLE (Modulo NVARCHAR(100), Nombre NVARCHAR(100), Criticidad INT);
INSERT INTO @ev VALUES
 (N'Maestros', N'Alta de cliente',                         4),
 (N'Maestros', N'Modificación de cliente',                 4),
 (N'Maestros', N'Baja de cliente',                         3),
 (N'Maestros', N'Clientes serializados a XML',             4),
 (N'Maestros', N'Clientes des-serializados desde XML',     4),
 (N'Maestros', N'Alta de insumo',                          4),
 (N'Maestros', N'Modificación de insumo',                  4),
 (N'Maestros', N'Baja de insumo',                          3),
 (N'Maestros', N'Consulta de bitácora de cambios',         5),
 (N'Maestros', N'Versión de insumo activada',              2),
 (N'Maestros', N'Operación rechazada en maestros',         3),
 (N'Maestros', N'Error técnico en maestros',               1),
 (N'Dígito Verificador', N'Inconsistencia de DV detectada', 1),
 (N'Dígito Verificador', N'DV recalculado',                 1),
 (N'Dígito Verificador', N'Backup generado',                2),
 (N'Dígito Verificador', N'Restore de backup ejecutado',    1),
 (N'Dígito Verificador', N'Reparación de DV cancelada',     2);

INSERT INTO dbo.TiposEvento (IdModulo, Nombre, Criticidad)
SELECT m.IdModulo, e.Nombre, e.Criticidad
FROM @ev e
INNER JOIN dbo.Modulos m ON m.Nombre = e.Modulo
WHERE NOT EXISTS (SELECT 1 FROM dbo.TiposEvento t WHERE t.IdModulo = m.IdModulo AND t.Nombre = e.Nombre);
GO

/* Control */
SELECT TOP 20 c.IdCambio, c.IdInsumo, c.Fecha, c.Hora, c.Nombre, c.StockActual, c.Activo, c.Act, c.Operacion
FROM dbo.Insumos_C c ORDER BY c.IdCambio DESC;
GO
