# Migración Módulo Compras: Órdenes de Compra y Autorización (CM/OrdenCompra y CM/OrdenComAuto)
Fecha: 31/10/2026

## 1. Resumen
Se completó la migración de los módulos **Órdenes de Compra** (`CM/OrdenCompra`) y **Autorización de Órdenes de Compra** (`CM/OrdenComAuto`), correspondientes a la emisión, valorización con cálculo automático de IGV, control de estado comercial, aprobación ejecutiva y exportación a Excel, migrados desde `ERP-Arco n5` (.NET 5, AdminLTE 3) a `AspNet10ArcoERP` (.NET 10, AdminLTE 4, Bootstrap 5.3).

## 2. Componentes Migrados

1. **Modelos (`Models/OrdenCompraViewModels.cs`)**:
   - `OrdenCompraItemModel : MaestroEmpresaModel`: Encapsula datos de cabecera con proveedor, moneda, tipo de cambio, fecha de entrega, plazo y lugar de entrega, condición de pago, montos bruto/impuesto/total, estados de orden (`1 = En Edición`, `2 = Orden`, `3 = Atendida`, `4 = Anulada`) y estados de aprobación (`1 = Pendiente`, `2 = Aprobada`, `3 = Desaprobada`).
   - `OrdenCompraPageModel : ListadoPaginadoModel`: Bandeja con filtros avanzados (`search`, `codigo`, `fechaDesde`, `fechaHasta`, `tipoProductoId`, `estadoOrdenCompraId`, `estadoAprobacionId`).
   - `OrdenCompraProductoItemModel : MaestroEmpresaModel`: Detalle de items de compra con cantidades, valor unitario, precio unitario, montos calculados y unidad de medida.
   - `OrdenCompraDetalleModel`: Agrupador de cabecera, lista de items y catálogos (monedas, impuestos, condiciones de pago, proveedores).
   - `OrdenCompraGuardarSolicitud`, `OrdenCompraProductoGuardarSolicitud` y `CambiarEstadoOrdenCompraSolicitud`.
   - `OrdenComAutoPageModel`, `AprobarOrdenComAutoSolicitud` y `DesaprobarOrdenComAutoSolicitud`.

2. **Capa de Servicios (`Services/IComprasService.cs` y `Services/ComprasService.OrdenCompra.cs`)**:
   - `GetOrdenesCompraAsync`: Consulta paginada multi-criterio sobre `orden_co` con joins a `tipo_producto`, `proveedor`, `moneda`, `estado_orden_co`, `estado_aproba`, `impuesto_tasa`, `cond_pago`.
   - `GetOrdenCompraDetalleAsync`: Recupera cabecera y lista de productos para el modal de edición y consulta.
   - `SaveOrdenCompraAsync`: Generación atómica del correlativo de 5 dígitos (`00000`) desde `empresa_corre` (tabla 20) y ejecución de `sp_orden_co_insertar` / `sp_orden_co_actualizar`. Transiciona automáticamente el requerimiento (`pedido_co`) a estado `3 = CON ORDEN DE COMPRA` si fue originada desde un pedido.
   - `AnularOrdenCompraAsync`: Ejecuta `sp_orden_co_eliminar` y restituye el requerimiento asociado si corresponde.
   - `EnviarAprobacionOrdenCompraAsync`: Valida existencia de items y transiciona la orden a `ORDEN` con aprobación `PENDIENTE`.
   - `CambiarEstadoOrdenCompraAsync`: Ejecuta `sp_orden_co_cambiarEstado`.
   - `GetOrdenCompraProductosAsync`, `SaveOrdenCompraProductoAsync`, `EliminarOrdenCompraProductoAsync`: Gestión de productos en `orden_c_prod` con recálculo automático de totales mediante `sp_orden_co_actualizarMonto`.
   - `ExportarOrdenCompraExcelAsync`: Exportación XLSX con ClosedXML y `InformeExcel.Crear`.
   - `GetOrdenesComAutoAsync`: Bandeja de órdenes pendientes de aprobación.
   - `AprobarOrdenComAutoAsync` y `DesaprobarOrdenComAutoAsync`: Ejecutan `sp_orden_co_actualizarEstadoAprobacion`.

3. **Controladores (`Areas/CM/Controllers/OrdenCompraController.cs` y `OrdenComAutoController.cs`)**:
   - Heredan de `ModuloContextController` con permisos `cm / ordenCompra/index` y `cm / ordenComAuto/index`.
   - Endpoints seguros para mutación con validación de tokens anti-falsificación (`@Html.AntiForgeryToken()`).

4. **Vistas y Scripts (`Index.cshtml` y scripts `orden-compra.js` y `orden-com-auto.js`)**:
   - Diseño AdminLTE 4 con badges semánticos, modales interactivos y recálculo bidireccional en tiempo real de valor unitario vs precio unitario con IGV.
   - Búsqueda predictiva de insumos/productos y proveedores.
   - Modales independientes para cambio de estado y aprobación con diálogos SweetAlert2.

5. **Navegación (`Models/SidebarNavigation.cs`)**:
   - Compras → Procesos → `Órdenes de compra`.
   - Compras → Autorizaciones → `Autorización orden de compra`.

## 3. Verificación
- Compilación `dotnet build`: **0 errores, 0 advertencias**.

