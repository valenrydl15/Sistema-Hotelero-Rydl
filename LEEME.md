# Hotel Rydl — solución completa (Recepción + DSS)

Abrir `Hoteleria Rydl Viejo.sln`, compilar (Ctrl+Shift+B) y ejecutar.

## Base de datos (funciona en cualquier PC)
- `DAL/Conexion.cs` detecta solo la instancia de SQL Server (la que ya tenga `HotelRydlDB`; si no, la primera disponible).
  Si querés forzar una: crear `conexion.txt` junto al .exe con `NOMBREPC\INSTANCIA` (o una cadena completa).
- Si la base **no existe**, al iniciar se restaura sola desde `Backups\HotelRydl011026_04.bak` (o ejecuta `HotelRydl_DB.sql` si lo ponés junto al .exe).
  Si SQL Server no puede leer esa carpeta, la app copia el .bak a la carpeta de backups de la instancia y reintenta.
- Al iniciar también asegura (sin duplicar) los eventos de bitácora del módulo 2 y las tablas del DV.
  Igual queda `Scripts/01_SQL_Bitacora_y_DV.sql` por si querés correrlo a mano.
- Backup/Restore del menú usan la carpeta de backups de la instancia (o `C:\HotelRydl\Backups`).
  Si falla por permisos en una PC ajena: abrir Visual Studio como administrador o `icacls C:\HotelRydl\Backups /grant Everyone:(OI)(CI)M`.

## Qué cambió respecto de tu versión
- Recepción en una sola pantalla (`FrmRecepcion`) con la lógica de los DSS (ver `CheckInBLL`: CUN01 → CUN02/03/04 y CUN05).
- Acceso a Recepción solo Recepcionista y Administrador (`BLL/ControlAcceso.cs`, validado en la BLL y en el menú).
- Menú y login rediseñados; el resto de las pantallas toma el estilo común de `Tema.cs`.
- Login: la contraseña ahora se muestra oculta (antes se veía en texto plano).
- Eliminados: `FrmCheckIn`, `FrmCheckOut`, `BitacoraCheck.cs` (reemplazados).
- Se quitó de `Backups/` el backup viejo `HotelRydl2606061941.bak`; queda el último (`HotelRydl011026_04.bak`).

## Para probar
1. Login Administrador → ve todo. Login Recepcionista → solo Recepción y Cuenta.
2. Check-IN completo → revisar Bitácora y que el DV siga válido al reloguear.
3. Pago rechazado → reserva Cancelada y eventos "Pago rechazado" + "Reserva cancelada".
4. Check-OUT → la habitación vuelve a Disponible.

## PN2 — Gestión de reposición de insumos (v3: solicitud de cotización → pre-orden → orden → pago → recepción)
Botón **Reposición de Insumos** del menú (`FrmReposicion`), una sección por actor:
Faltantes y solicitud (CUN06 + CUN07) · Cotizaciones y Pre-órdenes (CUN08) · Gerencia (CUN09 + CUN10 + CUN11) · Recepción (CUN12), más Órdenes y Avisos.

- **Flujo**: Compras ve los insumos faltantes (stock ≤ mínimo) → genera una solicitud de cotización por proveedor y se la envía
  (si el proveedor nunca le vendió al hotel lo pre-registra con empresa, contacto, teléfono y dirección) → registra la cotización
  que manda cada proveedor (cantidad disponible, precio, CUIT, banco, CBU, titular) → compara y elige una oferta por insumo: se generan
  las pre-órdenes (estado "Pre-orden") y se avisa al Gerente → el Gerente aprueba (orden "Generada") o cancela con motivo ("Cancelada")
  → si el proveedor está pre-registrado, lo registra con los datos de su cotización → paga por transferencia (BancoServicio, simulado):
  aprobado = orden "Pagada" y se envía al proveedor con el nro. de operación; rechazado = sigue "Generada" → Depósito registra la
  factura y lo recibido: la orden pasa a "Recibida" y el stock se actualiza solo. Si algo no coincide se avisa a Compras.
- **Roles**: `Encargado de Compras`, `Gerente de Compras` y `Encargado de Depósito` (el viejo "Encargado de Almacén" se renombra solo).
  El Administrador hace todo. Los usuarios se crean desde *Gestión de Usuarios* (clave inicial = DNI + Apellido).
- **Base de datos**: `Scripts/02_SQL_PN2_Reposicion.sql` (idempotente). La app lo ejecuta sola al iniciar y tras un Restore.
  **Atención:** al pasar de la v2 a la v3 se borran las tablas transaccionales viejas del PN2 (cotizaciones, pre-órdenes, órdenes,
  pagos, recepciones, movimientos y avisos); `Insumos` y `Proveedores` se conservan. Hacé un backup antes si querés guardar ese historial.
  Tablas: `SolicitudesCotizacion(+Detalle)`, `Cotizaciones(+Detalle)`, `OrdenesCompra(+Detalle)`, `PagosOrdenCompra`,
  `RecepcionesInsumos(+Detalle)`, `MovimientosStock`, `Notificaciones`; columnas nuevas en `Insumos` (Categoria) y `Proveedores`
  (CUIT, Banco, CBU, Titular, Estado Pre-registrado/Registrado).
- **Dígito Verificador**: controla las 12 tablas del PN2. Las que cambian de estructura vuelven al estado inicial ('0')
  y la app recalcula su DV en el próximo inicio de sesión.
- **Datos para probar el pago**: cualquier CUIT de 11 dígitos y CBU de 22 dígitos (se valida el formato). Ej.: CUIT `30-71234567-1` ·
  CBU `0070999000001234567891`. El banco simulado rechaza transferencias de más de $ 50.000.000 (sirve para mostrar el pago rechazado).
- **Correo al proveedor**: sin configurar es *simulado* (guarda el texto en `Correos_Enviados\`). Para enviarlo de verdad, `correo.txt` junto al .exe:
  `servidor=smtp.gmail.com` · `puerto=587` · `usuario=...` · `clave=...` · `ssl=true`.
- `InsumosDAL` quedó marcada obsoleta (la reemplaza `ReposicionDAL`, transaccional).

## Reportes PDF (un reporte por requisito funcional) — QuestPDF
- **PN1 Recepción**: botón **Reporte de facturas (PDF)** en la pantalla de Recepción (`FrmReporteFacturas`): lista facturas por período,
  exporta **una factura** o el **listado**. Al confirmar un Check-IN además se ofrece exportar la factura recién emitida.
- **PN2 Reposición**: sección *Órdenes* → **Exportar orden (PDF)** (orden seleccionada) y **Listado (PDF)** (según el filtro de estado).
- Código: `ReporteServicio` (documentos QuestPDF) · `ReporteBLL` (permisos, validación de ruta y bitácora) · `ReportesDAL` (consultas de solo lectura).
  Permisos: facturas → Recepcionista/Administrador; órdenes → Administrador, Compras, Gerente y Depósito.
- Paquete NuGet `QuestPDF` (licencia Community, gratuita para uso académico). Se restaura solo al compilar (requiere internet la primera vez).
- Bitácora: eventos `Reporte de factura exportado` y `Reporte de orden de compra exportado` (los agrega la app al iniciar; también `Scripts/03_SQL_Reportes.sql`).
- Los reportes no modifican tablas, así que no afectan los dígitos verificadores.
