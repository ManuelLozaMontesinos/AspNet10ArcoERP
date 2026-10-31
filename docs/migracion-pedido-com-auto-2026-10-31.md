# Migración Módulo Compras: Autorización de Requerimientos de Compra (CM/PedidoComAuto)
Fecha: 31/10/2026

## 1. Resumen
Se migró el módulo de **Autorización de Requerimientos de Compra** (`CM/PedidoComAuto`), encargado de la aprobación, desaprobación y auditoría de pedidos internos de compras para adquisición de insumos y mercadería, manteniendo la regla de negocio y procedimientos almacenados del sistema legacy (`ERP-Arco n5`) y adaptándolo a ASP.NET Core 10 con AdminLTE 4 / Bootstrap 5.3.

## 2. Componentes Migrados

1. **Modelos (`Models/PedidoComAutoViewModels.cs`)**:
   - `PedidoComAutoItemModel : MaestroEmpresaModel`: Encapsula datos de cabecera del requerimiento, proveedor, tipo de producto, estados de pedido y aprobación, y hash de concurrencia optimista (`Revision`).
   - `PedidoComAutoPageModel : ListadoPaginadoModel`: Listado con filtros por estado de aprobación (por defecto pendientes `1`), tipo de producto, rango de fechas y texto.
   - `PedidoComAutoDetalleModel`: Detalle con cabecera y lista de productos para el modal de revisión.
   - `AprobarPedidoComAutoSolicitud` y `DesaprobarPedidoComAutoSolicitud`: Solicitudes tipadas con validación de modelo y contexto.

2. **Capa de Servicios (`Services/IComprasService.cs` y `Services/ComprasService.PedidoComAuto.cs`)**:
   - `GetPedidosComAutoAsync`: Consulta filtrada sobre `pedido_co` con joins a `tipo_producto`, `proveedor`, `estado_pedido_co` y `estado_aproba`.
   - `GetPedidoComAutoDetalleAsync`: Recupera cabecera y lista completa de productos (`pedido_c_prod`).
   - `AprobarPedidoComAutoAsync`: Ejecuta `sp_pedido_co_actualizarEstadoAprobacion` con `EstadoAprobacionId = 2` (Aprobada) y fecha actual bajo `IsolationLevel.Serializable`.
   - `DesaprobarPedidoComAutoAsync`: Ejecuta `sp_pedido_co_actualizarEstadoAprobacion` con `EstadoAprobacionId = 3` (Desaprobada) y fecha actual.

3. **Controlador (`Areas/CM/Controllers/PedidoComAutoController.cs`)**:
   - Hereda de `ModuloContextController` con permiso `cm / pedidoComAuto/index`.
   - Endpoints seguros `Aprobar` y `Desaprobar` con validación de token anti-falsificación y contexto de sede.

4. **Vista y Cliente (`Areas/CM/Views/PedidoComAuto/Index.cshtml` y `wwwroot/js/pedido-com-auto.js`)**:
   - Bandeja AdminLTE 4 con badges semánticos para estados de pedido y aprobación.
   - Modal de revisión interactivo `#comauto-modal` que detalla información del proveedor y tabla de items con sus unidades y pesos.
   - Acciones de "Aprobar" y "Desaprobar" protegidas con diálogos SweetAlert2.

5. **Navegación (`Models/SidebarNavigation.cs`)**:
   - Vinculado bajo Compras → Autorizaciones → `Autorización requerimiento`.

## 3. Verificación
- Compilación `dotnet build`: **0 errores, 0 advertencias**.

