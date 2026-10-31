# Migración Módulo Compras: Valorización de Compras (CM/ValorizaCompra)
Fecha: 31/10/2026

## 1. Resumen
Se completó la migración del módulo **Valorización de Compras** (`CM/ValorizaCompra`), correspondiente al registro, edición, consulta y anulación de comprobantes tributarios (facturas, notas, recibos de proveedores) vinculados a órdenes de compra, migrado desde `ERP-Arco n5` (.NET 5, AdminLTE 3, jqGrid) a `AspNet10ArcoERP` (.NET 10, AdminLTE 4, Bootstrap 5.3, Dapper 2.1, SweetAlert2).

Con este módulo, **el Módulo CM (Compras) queda completamente migrado al 100%** (`PuntoPedido`, `PedidoCompra`, `PedidoComAuto`, `OrdenCompra`, `OrdenComAuto`, `ProveCompra`, `ValorizaCompra`, `RepoValorizaComprob`).

## 2. Componentes Migrados

1. **Modelos (`Models/ValorizaCompraViewModels.cs`)**:
   - `ValorizaCompraItemModel : MaestroEmpresaModel`: Encapsula datos de comprobante (`orden_c_comp`), orden de compra asociada (`orden_co`), tipo de documento comercial (`tipo_doc_com`), proveedor, serie, número, fechas (emisión y vencimiento), moneda, condición de pago y montos (bruto, impuesto, total).
   - `ValorizaCompraPageModel : ListadoPaginadoModel`: Bandeja paginada con filtros por búsqueda libre, código de orden, serie, número, tipo de comprobante comercial, condición de pago y rango de fechas.
   - `ValorizaCompraGuardarSolicitud`: Solicitud validada para alta/edición de comprobantes de compras.

2. **Capa de Servicios (`Services/IComprasService.cs` y `Services/ComprasService.ValorizaCompra.cs`)**:
   - `GetValorizacionesCompraAsync`: Consulta paginada multi-criterio con ordenamiento configurable y carga de catálogos (`tipo_doc_com`, `cond_pago`).
   - `BuscarOrdenesCompraParaComprobanteAsync`: Búsqueda predictiva de órdenes de compra aprobadas para vinculación inmediata.
   - `GetValorizacionCompraDetalleAsync`: Detalle del comprobante para edición modal.
   - `SaveValorizacionCompraAsync`: Alta y actualización atómica bajo transacción `IsolationLevel.Serializable` con bloqueo `UPDLOCK, HOLDLOCK`. Cálculo automático de base imponible e IGV según tasa asociada a la orden de compra, invocando `sp_orden_c_comp_insertar` y `sp_orden_c_comp_actualizar`.
   - `EliminarValorizacionCompraAsync`: Baja lógica del comprobante mediante `sp_orden_c_comp_eliminar`.

3. **Controlador (`Areas/CM/Controllers/ValorizaCompraController.cs`)**:
   - Hereda de `ModuloContextController` con permiso `cm / ValorizaCompra/index`.
   - Endpoints: `Index`, `Obtener`, `BuscarOrdenes`, `Guardar`, `Eliminar`.

4. **Vista y Cliente (`Areas/CM/Views/ValorizaCompra/Index.cshtml` y `wwwroot/js/valoriza-compra.js`)**:
   - Interfaz moderna en AdminLTE 4 con filtros compactos y paginación estándar.
   - Búsqueda predictiva asíncrona de órdenes de compra con selección rápida en modal `#valco-modal`.
   - Recálculo en tiempo real de subtotal e IGV al digitar el monto total.
   - Alertas y confirmaciones con SweetAlert2 y protección anti-falsificación CSRF.

5. **Navegación (`Models/SidebarNavigation.cs`)**:
   - Registrado en: Compras → Procesos → `Valorización de compras` (`bi-receipt`).

## 3. Verificación
- Compilación `dotnet build`: **0 errores**.

