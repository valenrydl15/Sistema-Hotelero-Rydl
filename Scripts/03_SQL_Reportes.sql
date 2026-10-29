/* =====================================================================
   Hotelería Rydl — Reportes PDF (factura de check-in y orden de compra)
   Solo agrega 2 tipos de evento a la Bitácora. Es idempotente.
   NOTA: la aplicación ya lo hace sola al iniciar (ReposicionSetup); este script
   es para correrlo a mano si preferís.
   ===================================================================== */
IF EXISTS (SELECT 1 FROM dbo.Modulos WHERE IdModulo = 2)
   AND NOT EXISTS (SELECT 1 FROM dbo.TiposEvento WHERE IdModulo = 2 AND Nombre = N'Reporte de factura exportado')
    INSERT INTO dbo.TiposEvento (IdModulo, Nombre, Criticidad) VALUES (2, N'Reporte de factura exportado', 4);
GO

DECLARE @mod INT = (SELECT TOP 1 IdModulo FROM dbo.Modulos WHERE Nombre = N'Reposición de insumos');
IF @mod IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.TiposEvento WHERE IdModulo = @mod AND Nombre = N'Reporte de orden de compra exportado')
    INSERT INTO dbo.TiposEvento (IdModulo, Nombre, Criticidad) VALUES (@mod, N'Reporte de orden de compra exportado', 4);
GO

SELECT m.Nombre AS Modulo, t.Nombre AS Evento, t.Criticidad
FROM dbo.TiposEvento t JOIN dbo.Modulos m ON m.IdModulo = t.IdModulo
WHERE t.Nombre LIKE N'Reporte de%exportado';
GO
