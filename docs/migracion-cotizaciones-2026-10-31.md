# VT / Cotizaciones — 2026-10-31

Se migró de forma integral el módulo `/VT/Cotizacion`, ubicado en **Ventas → Procesos → Cotizaciones**, con el permiso de contexto `vt / cotizacion/index`. Sigue la arquitectura unificada de AspNet10ArcoERP (.NET 10, AdminLTE 4, Bootstrap 5.3, Dapper 2.1, ClosedXML y SweetAlert2).

## Pantalla y alcance

La pantalla principal implementa la bandeja comercial de cotizaciones:
- Cabecera con sede activa, búsqueda reactiva por cliente, documento RUC/DNI o correlativo.
- Filtros por vendedor, estado de cotización (Pendiente, Aprobado, Facturado, Anulado, Rechazado) y rango de fechas (Desde / Hasta).
- Tabla responsiva con ordenamiento de servidor, columna de totales formateados con símbolo monetario, estado estilizado con badges de AdminLTE 4 y columna de opciones contextual:
  - Ver / Editar detalle de cotización.
  - Exportación directa a Excel con ClosedXML.
  - Generación de Pedido de Venta directo (disponible solo para cotizaciones en estado pendiente/activo).
  - Anulación con confirmación SweetAlert2 y verificación de revisión concurrente.

### Modal Extendido de Cotización (`#cot-modal`)
1. **Pestaña 1: Datos Generales**:
   - Número de cotización autogenerado mediante correlativo secuencial `0015` de la sede.
   - Selector de fecha, vendedor, moneda, condición de pago y lista de precios.
   - Búsqueda predictiva con autocompletado en tiempo real de clientes (por RUC, DNI o razón social).
   - Resumen visual de totales en tarjeta compacta: Subtotal Bruto, Descuento, IGV y Total Neto.
2. **Pestaña 2: Productos y Servicios**:
   - Búsqueda predictiva de productos en la lista de precios seleccionada (con stock actual e indicador de impuestos).
   - Inserción de líneas con cantidad, precio unitario con IGV, valor unitario y % de descuento.
   - Recálculo automático de montos mediante procedimiento de base de datos (`sp_cotiza_actualizarMonto`).
   - Rejilla de productos con opción de eliminación por ítem y actualización en vivo de subtotales.
3. **Conversión a Pedido de Venta (`#cot-pedido-modal`)**:
   - Permite convertir directamente la cotización en Pedido de Venta asignando fecha de entrega estimada.
   - Cambia el indicador `es_pedido = 'S'` y genera la numeración correlativa correspondiente.

## Reglas contrastadas con el legacy y SQL

- **Almacenamiento**: Las cotizaciones residen en `cotiza` (`es_pedido = 'N'`) y sus ítems en `cotiza_prod`.
- **Correlativos**: Utiliza la tabla de secuencias `periodo_emp_corre` bajo el código de tabla `0015` para la sede y período activos, formateado con 7 dígitos numéricos (`0000000`).
- **Transaccionalidad**: Alta, actualización, anulación y conversión se realizan bajo nivel de aislamiento serializable con bloqueo optimista mediante SHA-256 (`Revision`).
- **Recálculo de totales**: Se ejecuta el procedimiento almacenado nativo `sp_cotiza_actualizarMonto @cotizacionId, @usuarioAccionId` garantizando que impuestos, descuentos y totales coincidan exactamente con la lógica comercial del ERP.
- **Exportación**: Genera libros Excel con formato contable mediante ClosedXML y streaming en memoria.

## Archivos creados e integrados

- `Models/CotizacionViewModels.cs`: ViewModels paginados, DTOs de items, filtros, solicitudes y validaciones DataAnnotations.
- `Services/IVentasService.cs`: Métodos del contrato para cotizaciones (`GetCotizacionesAsync`, `GetCotizacionDetalleAsync`, `BuscarClientesCotizacionAsync`, `BuscarProductosCotizacionAsync`, `SaveCotizacionAsync`, `AnularCotizacionAsync`, `GetCotizacionProductosAsync`, `SaveCotizacionProductoAsync`, `EliminarCotizacionProductoAsync`, `GenerarPedidoDesdeCotizacionAsync`, `ExportarCotizacionExcelAsync`).
- `Services/VentasService.Cotizaciones.cs`: Implementación Dapper y ClosedXML con SPs nativos.
- `Areas/VT/Controllers/CotizacionController.cs`: Controlador MVC con seguridad `ModuloContextController`, CSRF tokens y endpoints JSON.
- `Areas/VT/Views/Cotizacion/Index.cshtml`: Vista Razor moderna en AdminLTE 4 y Bootstrap 5.3.
- `wwwroot/js/cotizacion.js`: Script frontend ES6 con debounce, autocompletados, SweetAlert2 y manejo de eventos.
- `Models/SidebarNavigation.cs`: Enlace `Link("VT","Cotizacion","Cotizaciones","bi-file-earmark-text")` en Ventas → Procesos.

