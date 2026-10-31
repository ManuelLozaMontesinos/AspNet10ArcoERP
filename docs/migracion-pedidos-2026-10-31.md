# VT / Pedidos de venta — 2026-10-31

Se completó la migración completa del módulo `/VT/Pedido`, ubicado en **Ventas → Procesos → Pedidos**, con el permiso de contexto `vt / pedido/index`.

## Pantalla y alcance

La pantalla gestiona las órdenes y pedidos comerciales de clientes:
- **Bandeja de pedidos**: Búsqueda interactiva por cliente, documento o correlativo de pedido, filtro por vendedor, estado de pedido (`Pedido`, `Facturado Parcial`, `Facturado`, `Anulado`) y rango de fechas.
- **Grilla de datos**:
  - Código formateado (7 dígitos), Fecha de emisión y Fecha estimada de entrega.
  - Datos de cliente (Razón social y RUC/DNI) y Vendedor.
  - Anticipo recibido, Total de pedido y Saldo pendiente calculado en vivo.
  - Indicador de estado de producción y badges semánticos de estado comercial (AdminLTE 4).
  - Columna de opciones:
    - **Consultar / Editar**: Abre el modal extendido `#ped-modal`.
    - **Descargar Excel**: Exportación ClosedXML con cabecera completa, anticipos y detalle de ítems.
    - **Revertir a Cotización**: Acción disponible para pedidos en estado `02` sin anticipos ni avance de producción, regresando el documento a estado cotización (`es_pedido = 'N'`).
    - **Cambiar Estado**: Modal rápido `#ped-estado-modal` para actualización de estado comercial por roles autorizados.
    - **Anulación**: Confirmación SweetAlert2 y anulación lógica (`estado_cotiza_id = 4`).

### Modal Extendido de Pedido (`#ped-modal`)
- **Pestaña 1: Datos Generales**: Fecha, Fecha de entrega, Vendedor, Cliente predictivo, Moneda, Condición de pago, Lista de precios y Observaciones. Tarjeta resumen con Subtotal, Descuento, IGV, Total, Anticipo y Saldo a cobrar.
- **Pestaña 2: Productos y Servicios**: Búsqueda reactiva de productos en la lista de precios, ingreso de cantidades, precios con IGV y porcentajes de descuento, con recálculo automático en tiempo real (`sp_cotiza_actualizarMonto`).

## Reglas contrastadas con el legacy y SQL

- **Persistencia**: Comparte tabla `cotiza` (`es_pedido = 'S'`) y `cotiza_prod`.
- **Correlativo**: Comparte la secuencia de tabla `0015` de la sede en `periodo_emp_corre`.
- **Integridad comercial**: No permite revertir a cotización pedidos con anticipos o enviados a producción.

## Archivos creados e integrados

- `Models/PedidoViewModels.cs`: ViewModels paginados, DTOs y modelos de solicitud.
- `Services/IVentasService.cs`: Métodos del contrato para pedidos.
- `Services/VentasService.Pedidos.cs`: Implementación Dapper y ClosedXML.
- `Areas/VT/Controllers/PedidoController.cs`: Controlador MVC con seguridad `ModuloContextController`.
- `Areas/VT/Views/Pedido/Index.cshtml`: Vista Razor moderna en AdminLTE 4 y Bootstrap 5.3.
- `wwwroot/js/pedido.js`: Script cliente modular en ES6 con SweetAlert2.
- `Models/SidebarNavigation.cs`: Enlace en el menú bajo Ventas → Procesos.

