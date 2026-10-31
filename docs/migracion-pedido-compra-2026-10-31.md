# Migración Módulo Compras: Requerimientos de Compra (CM/PedidoCompra)
Fecha: 31/10/2026

## 1. Resumen
Se migró integralmente el módulo de **Requerimientos de Compra** (`CM/PedidoCompra`), correspondiente a la gestión de pedidos internos de insumos y productos para adquisición y reposición, desde la versión legacy (`ERP-Arco n5`) a .NET 10 y AdminLTE 4 / Bootstrap 5.3.

## 2. Componentes Migrados

1. **Modelos (`Models/PedidoCompraViewModels.cs`)**:
   - `PedidoCompraItemModel`: Cabecera con datos de proveedor, tipo de producto, correlativo de 5 dígitos (`00000`), estado del pedido (`1 = En Edición`, `2 = Pedido`, `3 = Con Orden`, `4 = Anulado`, `5 = Modificación`), y estado de aprobación (`1 = Pendiente`, `2 = Aprobado`, `3 = Desaprobado`). Concurrencia optimista (`Revision`).
   - `PedidoCompraPageModel`: Listado paginado con filtros avanzados (`search`, `codigo`, `fechaDesde`, `fechaHasta`, `tipoProductoId`, `estadoPedidoCompraId`, `estadoAprobacionId`).
   - `PedidoCompraProductoItemModel`: Detalle de items/insumos con producto, descripción, unidad de medida, cantidad, peso neto, peso bruto, flag de stock en proveedor y es adicional.
   - `PedidoCompraDetalleModel`: Agrupador de cabecera, items y listas desplegables para modal interactivo.
   - `PedidoCompraGuardarSolicitud` y `PedidoCompraProductoGuardarSolicitud`.

2. **Capa de Servicios (`Services/IComprasService.cs` y `Services/ComprasService.PedidoCompra.cs`)**:
   - `GetPedidosCompraAsync`: Consulta paginada con filtrado dinámico sobre `pedido_co`, `tipo_producto`, `proveedor`, `estado_pedido_co`, `estado_aproba`.
   - `GetPedidoCompraDetalleAsync`: Carga cabecera e items para visualización y edición en ventana modal.
   - `BuscarProveedoresCompraAsync` y `BuscarProductosCompraAsync`: Búsqueda predictiva con autocompletado rápido.
   - `SavePedidoCompraAsync`: Generación atómica del correlativo por empresa (`empresa_corre` con `tabla_id = 24`, longitud 5 dígitos `00000`) y ejecución de `sp_pedido_co_insertar` / `sp_pedido_co_actualizar`.
   - `AnularPedidoCompraAsync`: Anulación controlada mediante `sp_pedido_co_eliminar`.
   - `EnviarAprobacionPedidoCompraAsync`: Valida existencia de items y transiciona el requerimiento a estado `PEDIDO` (2) con aprobación `PENDIENTE` (1).
   - `GetPedidoCompraProductosAsync`, `SavePedidoCompraProductoAsync`, `EliminarPedidoCompraProductoAsync`: Gestión completa de items en `pedido_c_prod`.
   - `ExportarPedidoCompraExcelAsync`: Exportación XLSX estilizada con ClosedXML mediante `InformeExcel.Crear`.

3. **Controlador (`Areas/CM/Controllers/PedidoCompraController.cs`)**:
   - Hereda de `ModuloContextController` con ruta de permiso `cm / pedidoCompra/index`.
   - Validación de contexto de trabajo de empresa y sede.
   - Verificación de tokens anti-falsificación en todos los endpoints de mutación (`Guardar`, `Anular`, `SolicitarAprobacion`, `GuardarProducto`, `EliminarProducto`).

4. **Vista y Cliente (`Areas/CM/Views/PedidoCompra/Index.cshtml` y `wwwroot/js/pedido-compra.js`)**:
   - Interfaz AdminLTE 4 con badges de estado y aprobación con colores semánticos.
   - Modal de dos pestañas (Datos Generales y Detalle de Productos) con validación en tiempo real.
   - Búsqueda predictiva de proveedores y productos/insumos con cálculo automático de pesos.
   - Confirmaciones y notificaciones toast con SweetAlert2.
   - Enlace directo con Punto de Pedido (`PuntoPedido/Index`).

5. **Navegación (`Models/SidebarNavigation.cs`)**:
   - Registrado bajo Compras → Procesos → `Requerimientos de compra`.

## 3. Verificación
- Compilación `dotnet build`: **0 errores, 0 advertencias**.

