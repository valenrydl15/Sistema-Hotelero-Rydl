/* =====================================================================
   HRS-480 · Hotelería Rydl · PN2
   04 - Carga 20 insumos más en estado CRÍTICO (stock actual < stock mínimo)
   - Se puede ejecutar más de una vez: no repite un insumo que ya existe (por nombre).
   - Al final deja el Dígito Verificador de Insumos en estado inicial ('0'):
     la app lo recalcula sola en el próximo inicio de sesión (no aparece como alterada).
   ===================================================================== */
USE [HotelRydlDB]
GO

SET NOCOUNT ON;

DECLARE @nuevos TABLE (Nombre NVARCHAR(150), Descripcion NVARCHAR(300), UnidadMedida NVARCHAR(30),
                       StockActual DECIMAL(10,2), StockMinimo DECIMAL(10,2), Categoria NVARCHAR(50));

INSERT INTO @nuevos (Nombre, Descripcion, UnidadMedida, StockActual, StockMinimo, Categoria) VALUES
 (N'Sábanas king',              N'Juego de sábanas 200 hilos, cama king',    N'unidades', 8,   20,  N'Blancos'),
 (N'Fundas de almohada',        N'Funda 50x70 blanca',                       N'unidades', 35,  80,  N'Blancos'),
 (N'Acolchados',                N'Acolchado de pluma sintética 2 plazas',    N'unidades', 4,   12,  N'Blancos'),
 (N'Batas de baño',             N'Bata de toalla talle único',               N'unidades', 10,  30,  N'Blancos'),
 (N'Alfombras de baño',         N'Toalla de piso 50x80',                     N'unidades', 15,  40,  N'Blancos'),
 (N'Acondicionador 30 ml',      N'Dosificador individual',                   N'unidades', 45,  120, N'Amenities'),
 (N'Gel de ducha 30 ml',        N'Dosificador individual',                   N'unidades', 50,  120, N'Amenities'),
 (N'Kit dental',                N'Cepillo y pasta dental individual',        N'unidades', 20,  80,  N'Amenities'),
 (N'Gorras de baño',            N'Gorra descartable',                        N'unidades', 30,  100, N'Amenities'),
 (N'Pantuflas descartables',    N'Par de pantuflas de cortesía',             N'pares',    18,  60,  N'Amenities'),
 (N'Lavandina',                 N'Bidón de lavandina concentrada',           N'litros',   10,  40,  N'Limpieza'),
 (N'Limpiavidrios',             N'Rociador para vidrios y espejos',          N'litros',   4,   15,  N'Limpieza'),
 (N'Desinfectante de pisos',    N'Bidón aroma lavanda',                      N'litros',   12,  30,  N'Limpieza'),
 (N'Guantes de látex',          N'Caja x 100, talle M',                      N'cajas',    3,   10,  N'Limpieza'),
 (N'Papel de manos',            N'Rollo interfoliado para dispenser',        N'rollos',   25,  60,  N'Limpieza'),
 (N'Azúcar en sobres',          N'Sobre individual 6 g',                     N'unidades', 150, 500, N'Cafetería'),
 (N'Té en saquitos',            N'Caja surtida para habitaciones',           N'unidades', 80,  200, N'Cafetería'),
 (N'Gaseosa lata 354 ml',       N'Lata para frigobar',                       N'unidades', 40,  96,  N'Frigobar'),
 (N'Lámparas LED',              N'Lámpara LED E27 9 W luz cálida',           N'unidades', 12,  40,  N'Mantenimiento'),
 (N'Pilas AA',                  N'Pilas para controles remotos',             N'unidades', 20,  60,  N'Mantenimiento');

INSERT INTO dbo.Insumos (Nombre, Descripcion, UnidadMedida, StockActual, StockMinimo, Categoria)
SELECT n.Nombre, n.Descripcion, n.UnidadMedida, n.StockActual, n.StockMinimo, n.Categoria
FROM @nuevos n
WHERE NOT EXISTS (SELECT 1 FROM dbo.Insumos i WHERE i.Nombre = n.Nombre);

DECLARE @agregados INT = @@ROWCOUNT;

IF @agregados > 0
    UPDATE dbo.DigitoVerificador SET DVH = '0', DVV = '0', FechaCalculo = GETDATE(), Valido = 1
     WHERE NombreTabla = 'Insumos';

PRINT CONCAT('Insumos agregados: ', @agregados);
GO

/* Control: insumos en estado crítico */
SELECT IdInsumo, Nombre, Categoria, UnidadMedida, StockActual, StockMinimo, Estado
FROM dbo.Insumos
WHERE StockActual <= StockMinimo
ORDER BY Categoria, Nombre;
GO
