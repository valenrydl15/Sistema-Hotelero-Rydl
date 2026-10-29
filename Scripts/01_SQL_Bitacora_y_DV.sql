USE [HotelRydlDB]
GO

/* =====================================================================
   1) MÓDULO 2 (Check-in / Check-out) EN BITÁCORA
   El código usa ID_MOD = 2. Verificá que el módulo exista con ese Id.
   ===================================================================== */
IF NOT EXISTS (SELECT 1 FROM Modulos WHERE IdModulo = 2)
    INSERT INTO Modulos (Nombre) VALUES ('Check-in/Check-out');
GO

SELECT IdModulo, Nombre FROM Modulos;   -- confirmá que Check-in/Check-out tenga IdModulo = 2
GO

/* =====================================================================
   2) TIPOS DE EVENTO DEL MÓDULO 2
   Los nombres deben coincidir EXACTO con las constantes de BitacoraCheck.cs
   (el código busca el Id por nombre, no hace falta copiar IDs).
   Criticidad: 1 = más crítica ... 5 = menos crítica
   ===================================================================== */
DECLARE @ev TABLE (Nombre NVARCHAR(100), Criticidad INT);
INSERT INTO @ev VALUES
 (N'Consulta de disponibilidad', 5),
 (N'Alta de huésped',            4),
 (N'Reserva creada',             4),
 (N'Pago autorizado',            3),
 (N'Pago rechazado',             2),
 (N'Reserva cancelada',          3),
 (N'Factura emitida',            4),
 (N'Check-in realizado',         3),
 (N'Check-in rechazado',         3),
 (N'Check-out realizado',        3),
 (N'Check-out rechazado',        3),
 (N'Error técnico check-in/out', 1);

INSERT INTO TiposEvento (IdModulo, Nombre, Criticidad)
SELECT 2, e.Nombre, e.Criticidad
FROM @ev e
WHERE NOT EXISTS (SELECT 1 FROM TiposEvento t
                  WHERE t.IdModulo = 2 AND t.Nombre = e.Nombre);
GO

SELECT IdTipoEvento, IdModulo, Nombre, Criticidad
FROM TiposEvento WHERE IdModulo = 2 ORDER BY IdTipoEvento;
GO

/* =====================================================================
   3) DÍGITO VERIFICADOR: nuevas tablas controladas
   Se inician en '0': Verificar() las inicializa solo en el próximo login.
   Si tu tabla DigitoVerificador tiene otras columnas obligatorias,
   agregalas en el INSERT.
   ===================================================================== */
INSERT INTO DigitoVerificador (NombreTabla, DVH, DVV, FechaCalculo, Valido)
SELECT v.t, '0', '0', GETDATE(), 1
FROM (VALUES ('Huespedes'),('Habitaciones'),('Reservas'),
             ('CheckIns'),('CheckOuts'),('Pagos'),('Facturas')) v(t)
WHERE v.t NOT IN (SELECT NombreTabla FROM DigitoVerificador);
GO

SELECT NombreTabla, DVH, DVV, Valido FROM DigitoVerificador;
GO
