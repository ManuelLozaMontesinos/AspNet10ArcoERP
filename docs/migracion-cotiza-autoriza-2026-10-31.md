# VT / Autorización de Cotizaciones y Pedidos — 2026-10-31

Se completó la migración integral del módulo `/VT/CotizaAutoriza`, ubicado en **Ventas → Procesos → Autorizaciones → Autorización cotización**, con el permiso de contexto `vt / cotizaAutoriza/index`.

## Pantalla y alcance

La bandeja de autorizaciones permite gestionar las solicitudes de descuento o condiciones especiales generadas desde cotizaciones o pedidos:
- **Filtros avanzados**: Búsqueda global (cliente, número, vendedor), selector de estado de aprobación (`Pendiente de Aprobación`, `Aprobada`, `Desaprobada`, `Cancelada`, `En Edición`) y rango de fechas.
- **Grilla de bandeja**: Muestra N° de Cotización, Tipo de Autorización (`01 - DSCTO EN PRECIO UNITARIO`, etc.), Fecha, Cliente, Vendedor, Total con moneda y estado de aprobación con badges de AdminLTE 4.
- **Modal Extendido de Revisión (`#auth-modal`)**:
  - Resumen visual de la cotización: datos comerciales, lista de precios aplicada, condición de pago, moneda, tipo de cambio y tasa de IGV.
  - Tabla de productos: muestra precios de lista, precios solicitados, cantidades y un campo editable de `P.U. Autorizado` en tiempo real para cada ítem que solicitó descuento.
  - Recálculo dinámico en el cliente del total por fila al ingresar o ajustar el precio autorizado.
  - Botones de acción contextual:
    - **Aprobar Solicitud**: Confirma los precios autorizados, actualiza `cotiza_prod` (`cant_bloqueada = 'S'`, `soli_auto_dcto = 'N'`, recálculo de valor neto, IGV y total), actualiza `cotiza_autoriza` mediante `sp_cotiza_autoriza_actualizar` (Estado 2 = Aprobada) y recalcula la cotización con `sp_cotiza_actualizarMonto`.
    - **Desaprobar Solicitud**: Permite ingresar un motivo/observación, desactiva la solicitud de descuento en los ítems, actualiza `cotiza_autoriza` (Estado 3 = Desaprobada) y recalcula los montos de la cotización.

## Reglas contrastadas con el legacy y SQL

- **Seguridad y Concurrencia**: Usa `IsolationLevel.Serializable` con bloqueos `UPDLOCK, HOLDLOCK` y validación de concurrencia optimista mediante hash SHA-256 (`CalculateAuthRevision`).
- **Procedimientos Almacenados**: Se apoya en `sp_cotiza_autoriza_actualizar` y `sp_cotiza_actualizarMonto`.
- **Integridad de Descuentos**: Al aprobar, los productos quedan marcados como bloqueados para evitar modificaciones no autorizadas en cotización posterior.

## Archivos creados e integrados

- `Models/CotizaAutorizaViewModels.cs`: Modelos de vista, DTOs, solicitudes de aprobación y desaprobación.
- `Services/IVentasService.cs`: Definición de métodos asíncronos para consulta y atención de autorizaciones.
- `Services/VentasService.CotizaAutoriza.cs`: Lógica de negocio Dapper y transacciones ACID.
- `Areas/VT/Controllers/CotizaAutorizaController.cs`: Controlador MVC con seguridad de contexto `ModuloContextController`.
- `Areas/VT/Views/CotizaAutoriza/Index.cshtml`: Vista Razor moderna en AdminLTE 4 y Bootstrap 5.3.
- `wwwroot/js/cotiza-autoriza.js`: Lógica cliente con SweetAlert2, recálculo reactivo y manejo de eventos.
- `Models/SidebarNavigation.cs`: Enlace en el menú bajo Ventas → Procesos → Autorizaciones.

