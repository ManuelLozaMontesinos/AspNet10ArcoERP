# VT / Listas de precios — 2026-10-31

Se incorporó `/VT/ListaPrecio`, ubicado en **Ventas → Maestros → Listas de precios**, con el permiso exacto `vt / listaPrecio/index`. Se ubica dentro de la categoría **Lista de precios** del menú junto a Grupos de clientes, completando el catálogo maestro comercial. Utiliza el servicio de Ventas ya registrado, Dapper y ClosedXML, sin dependencias externas nuevas.

## Pantalla y alcance

La bandeja principal adopta el estándar de Maestros:
- Cabecera con sede activa, búsqueda por nombre de lista, filtros por sede, moneda y estado.
- Orden global en el servidor antes de paginar por todas las columnas (`nombre`, `sede`, `moneda`, `tasa`, `estado`, `id`).
- Selector de tamaños 25/50/100/500 registros, contador total y rango visible, pie y actualización AJAX.
- Columna Opciones al final con botones para administrar, descargar Excel formateado y anular.

Al consultar o editar una lista se abre un **único modal extendido** (`#lp-modal`) con cuatro pestañas integradas:
1. **Datos Generales**: Sede, Nombre, Tipo de Afectación de Impuesto (Impuesto Tasa) y Moneda.
2. **Productos y Precios**:
   - Paginación server-side (25, 50, 100, 500) sobre los miles de productos de la lista.
   - Búsqueda por código o descripción y filtro de estado (Activos / Inactivos).
   - Panel de edición rápida inline para ajustar `Valor Unitario` (sin IGV), `Precio Unitario` (con IGV), `Permite Descuento` y `Estado`.
   - Cálculo automático bidireccional de IGV/tasa: al modificar el valor unitario se recalcula el precio unitario según la tasa configurada en la lista, y viceversa.
3. **Grupos de Clientes**:
   - Muestra los grupos de clientes (`grupo_prove`) vinculados a esta lista de precios y la cantidad de descuentos configurados.
   - Selector para incorporar nuevos grupos activos de la empresa que aún no estén asignados.
   - Acciones para retirar vínculos y botón directo para saltar a configurar sus precios.
4. **Precios por Grupo**:
   - Selector del grupo de clientes y listado de sus productos con `% Precio Base`.
   - Editor para configurar o retirar porcentajes de descuento sobre el precio base.

## Reglas contrastadas con el legacy y SQL

Se inspeccionaron el controlador `ListaPrecioController` legacy, `ListaPrecioBusiness`, `ListaPrecioRepository`, las tablas `lista_precio`, `lista_p_prod`, `lista_p_prove`, `lista_p_p_prod`, `impuesto_tasa` y `moneda`, y los procedimientos almacenados correspondientes:

- Las listas de precios pertenecen a sedes de la empresa autenticada. Cada consulta y mutación comprueba la pertenencia de empresa y sede.
- Alta, actualización y anulación lógica de la cabecera usan respectivamente `sp_lista_precio_insertar`, `sp_lista_precio_actualizar` y `sp_lista_precio_eliminar`.
- La actualización de precios de producto utiliza `sp_lista_p_prod_actualizar`, preservando auditoría y validación de concurrencia optimista mediante hash SHA-256 (`Revision`).
- La vinculación y desvinculación de grupos de clientes utiliza `sp_lista_p_prove_insertar` y `sp_lista_p_prove_eliminar`.
- Los precios especiales por grupo utilizan `sp_lista_p_p_prod_insertar`, `sp_lista_p_p_prod_actualizar` y `sp_lista_p_p_prod_eliminar`.
- La exportación a Excel utiliza ClosedXML mediante `InformeExcel.Crear`, generando un libro formateado con cabecera de la lista, monedas, tasas y la tabla de productos con 4 decimales.

## Archivos e integración

- `Models/ListaPrecioViewModels.cs`: ViewModels paginados, detalle, solicitudes y contratos con validaciones DataAnnotations.
- `Services/IVentasService.cs`: Ampliación del contrato del servicio de ventas.
- `Services/VentasService.ListaPrecio.cs`: Implementación parcial con Dapper, consultas optimizadas, aislamiento por empresa y SP transaccionales con `IsolationLevel.Serializable` y bloqueos `UPDLOCK/HOLDLOCK`.
- `Areas/VT/Controllers/ListaPrecioController.cs`: Controlador MVC con seguridad de contexto `ModuloContextController`, antiforgery y respuestas estándar JSON/File.
- `Areas/VT/Views/ListaPrecio/Index.cshtml`: Vista Razor moderna en AdminLTE 4, Bootstrap 5.3, badges semánticos y modal extendido.
- `wwwroot/js/lista-precio.js`: Script cliente modular en JavaScript ES6 con SweetAlert2 y cálculo en tiempo real de impuestos.
- `Models/SidebarNavigation.cs`: Incorporación del enlace a `ListaPrecio` en el menú de Ventas.

