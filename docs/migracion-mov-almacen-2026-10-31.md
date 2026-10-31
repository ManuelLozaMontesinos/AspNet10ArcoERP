# Migración Módulo Inventario: Movimientos de Almacén (IV/MovAlmacen)
Fecha: 31/10/2026

## 1. Resumen
Se completó la migración del módulo central operativo **Movimientos de Almacén** (`IV/MovAlmacen`), correspondiente al registro, edición, consulta, anulación y exportación de operaciones físicas de entrada (ingresos), salida y transferencias entre almacenes, migrado desde `ERP-Arco n5` (.NET 5, AdminLTE 3, jqGrid) a `AspNet10ArcoERP` (.NET 10, AdminLTE 4, Bootstrap 5.3, Dapper 2.1, ClosedXML 0.105, SweetAlert2).

## 2. Componentes Migrados

1. **Modelos (`Models/MovAlmacenViewModels.cs`)**:
   - `MovAlmacenItemModel : MaestroEmpresaModel`: Encapsula datos de cabecera de movimiento (`mov_almacen`), tipo de movimiento (`I` = Ingreso, `S` = Salida), almacén origen, transacción (`trans_alma`), almacén destino, proveedor/cliente, orden de compra, guía de remisión, documento referencial, estado (`1 = MOVIMIENTO`, `2 = EN TRANSITO`, `3 = ANULADO`), usuario, total de items y valorización acumulada.
   - `MovAlmacenPageModel : ListadoPaginadoModel`: Bandeja paginada con filtros múltiples (`search`, `codigo`, `almacenId`, `tipoMovimientoAlmacenId`, `transaccionAlmacenId`, `estadoMovimientoAlmacenId`, `fechaDesde`, `fechaHasta`) y catálogos de soporte.
   - `MovAlmacenProductoItemModel : MaestroEmpresaModel`: Detalle de items de movimiento (`mov_alm_prod`) con cantidad, cantidad documento, valor unitario, total, unidad de medida, pesos neto y bruto.
   - `MovAlmacenDetalleModel`: Cabecera y detalle para visualización y edición modal.
   - `MovAlmacenGuardarSolicitud` y `MovAlmacenProductoGuardarSolicitud`: Solicitudes validadas para alta/edición de cabecera e items.

2. **Capa de Servicios (`Services/IInventarioService.cs` y `Services/InventarioService.MovAlmacen.cs`)**:
   - `GetMovimientosAlmacenAsync`: Consulta paginada multi-criterio con ordenamiento configurable sobre `mov_almacen` con joins a `almacen`, `tipo_mov_alma`, `trans_alma`, `estado_mov_alm`, `proveedor`, `usuario` y agregados de items.
   - `GetMovimientoAlmacenDetalleAsync`: Detalle completo para la vista modal interactiva.
   - `GetTransaccionesPorAlmacenTipoAsync`: Filtra dinámicamente las transacciones permitidas para la combinación Almacén - Tipo Movimiento (`alm_transa` y `trans_alma`).
   - `BuscarProductosAlmacenAsync` y `BuscarProveedoresAlmacenAsync`: Consultas predictivas asíncronas para selección ágil en modal.
   - `SaveMovimientoAlmacenAsync`: Generación segura y concurrente del correlativo (`I000000001` o `S000000001`) en `mov_alm_corre` con bloqueo `UPDLOCK, HOLDLOCK`, vinculación a `mes_contable` según fecha de operación, e invocación a `sp_mov_almacen_insertar` / `sp_mov_almacen_actualizar` y `sp_mov_almacen_actualizarMesContable`.
   - `AnularMovimientoAlmacenAsync`: Ejecuta `sp_mov_almacen_eliminar` transicionando al estado `3 = ANULADO`.
   - `GetMovimientoAlmacenProductosAsync`, `SaveMovimientoAlmacenProductoAsync` y `EliminarMovimientoAlmacenProductoAsync`: Gestión atómica de items en `mov_alm_prod` mediante `sp_mov_alm_prod_insertar`, `sp_mov_alm_prod_actualizar` y `sp_mov_alm_prod_eliminar`.
   - `ExportarMovimientoAlmacenExcelAsync`: Generación de reporte en formato Excel XLSX con ClosedXML y `InformeExcel.Crear`.

3. **Controlador (`Areas/IV/Controllers/MovAlmacenController.cs`)**:
   - Hereda de `InventarioContextController` con permiso `iv / movAlmacen/index`.
   - Endpoints: `Index`, `Obtener`, `Transacciones`, `BuscarProductos`, `BuscarProveedores`, `Guardar`, `Anular`, `Productos`, `GuardarProducto`, `EliminarProducto`, `Exportar`.

4. **Vista y Cliente (`Areas/IV/Views/MovAlmacen/Index.cshtml` y `wwwroot/js/mov-almacen.js`)**:
   - Interfaz en AdminLTE 4 con modal en 2 pestañas (`#mov-modal`): Datos de Operación y Productos del Movimiento.
   - Carga dinámica de transacciones según almacén y tipo de movimiento seleccionado.
   - Autocompletado predictivo para búsqueda de proveedores y productos.
   - Grid de items con totales dinámicos, confirmaciones SweetAlert2 y tokens anti-falsificación CSRF.

5. **Navegación (`Models/SidebarNavigation.cs`)**:
   - Registrado en: Inventario → Procesos → `Movimientos de almacén` (`bi-box-arrow-in-down-right`).

## 3. Verificación
- Compilación `dotnet build`: **0 errores**.

