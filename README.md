# Hotelería Rydl

Sistema de gestión hotelera de escritorio desarrollado como proyecto académico de Ingeniería en Sistemas Informáticos (UAI).
Hecho en **C# / Windows Forms (.NET 8)** sobre **SQL Server**, con arquitectura en capas y análisis y diseño en UML (Enterprise Architect).

## Funcionalidades

**Seguridad (base del sistema)**
- Login con bloqueo por intentos fallidos, cambio de clave, gestión de usuarios y roles/permisos (familias y patentes).
- Bitácora de eventos con filtros por módulo, evento, criticidad y usuario.
- Dígitos verificadores (DVH/DVV) con detección y reparación de inconsistencias.
- Copias de seguridad y restauración de la base desde el menú.
- Multi-idioma (Español, Inglés, Alemán) con traducción dinámica mediante archivos JSON.

**PN1 — Recepción** (Recepcionista y Administrador)
- CUN01 Registrar Check-IN (incluye CUN02 Seleccionar habitación, CUN03 Registrar datos del cliente y CUN04 Realizar cobro con factura) y CUN05 Realizar Check-OUT.
- Consulta de disponibilidad, reserva, cobro con validación bancaria simulada y emisión de factura.
- **Reportes PDF:** factura de estadía, y reporte de todas las facturas generadas (con resumen por medio de pago).

**PN2 — Gestión de reposición de insumos**
- CUN06 Generar solicitud de cotización (con los insumos faltantes) · CUN07 Pre-registrar proveedor («extend» de CUN06) · CUN08 Generar pre-orden de compra (registra y compara las cotizaciones) · CUN09 Generar orden de compra (aprobar o cancelar la pre-orden) · CUN10 Registrar proveedor («extend» de CUN09: CUIT, banco, CBU y titular) · CUN11 Pagar orden de compra («include» de CUN09: transferencia bancaria y envío al proveedor) · CUN12 Registrar recepción de insumos (contra la factura; actualiza el stock).
- Estados de la orden: Pre-orden → Generada → Pagada → Recibida (o Cancelada). Avisos entre áreas y control de diferencias orden/factura/mercadería.
- Roles: *Encargado de Compras* (CUN06, CUN07, CUN08), *Gerente de Compras* (CUN09, CUN10, CUN11), *Encargado de Depósito* (CUN12) y *Administrador* (todo).
- **Reportes PDF:** orden de compra y listado de órdenes de compra.

## Arquitectura

| Proyecto | Responsabilidad |
|---|---|
| `Hoteleria Rydl` (GUI) | Formularios Windows Forms |
| `BLL` | Reglas de negocio, control de acceso por rol, bitácora |
| `DAL HRS-480` | Acceso a datos (SQL Server, transacciones) |
| `ENTIDADES HRS-480` | Entidades de negocio |
| `SEGURIDAD HRS-480` | Sesión, hashing y servicios de seguridad |
| `SERVICIOS HRS-480` | Correo, bancos, dígito verificador, backup, reportes PDF (QuestPDF) |

## Requisitos
- Windows con **Visual Studio 2022** y el SDK de **.NET 8**.
- **SQL Server** (Express o superior) en el equipo.
- Conexión a internet la primera vez que se compila (restaura paquetes NuGet: Microsoft.Data.SqlClient, Newtonsoft.Json y QuestPDF).

## Cómo ejecutarlo
1. Abrir `Hoteleria Rydl Viejo.sln` en Visual Studio y compilar.
2. Ejecutar. Si la base `HotelRydlDB` no existe, la aplicación la restaura sola desde `Backups\HotelRydl011026_04.bak`.
3. Al iniciar, la app aplica automáticamente los scripts de `Scripts\` (idempotentes): eventos de bitácora, tablas, roles y datos de ejemplo del PN2.
   También se pueden correr a mano: `01_SQL_Bitacora_y_DV.sql`, `02_SQL_PN2_Reposicion.sql` y `03_SQL_Reportes.sql`.
4. Si hace falta indicar el servidor: crear `conexion.txt` junto al `.exe` con `NOMBREPC\INSTANCIA` (o una cadena de conexión completa).

**Correo al proveedor:** sin configurar es *simulado* (guarda el texto en `Correos_Enviados\`). Para enviarlo de verdad, crear `correo.txt` junto al `.exe`:

```
servidor=smtp.gmail.com
puerto=587
usuario=tu_usuario
clave=tu_clave
ssl=true
```

## Documentación
Los diagramas de casos de uso, DSS, actividad y clases y las especificaciones de los casos de uso se mantuvieron en Enterprise Architect (no están en el repositorio).
Ver también `LEEME.md` con el detalle de cambios por entrega.

## Licencias
Los reportes PDF usan [QuestPDF](https://www.questpdf.com/) bajo licencia Community (gratuita para uso académico y proyectos pequeños).
