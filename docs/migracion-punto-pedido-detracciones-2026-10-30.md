# Punto de pedido y Detracciones — 30/10/2026

## Implementación

| Módulo | Funciones implementadas | Fuente legacy |
|---|---|---|
| CM/PuntoPedido | Consulta por sede, filtros de producto/proveedor, detalle y Excel | `PuntoPedidoController`, su vista y `VolumenExisten/ListarStockMinimoListPopup`, repositorio y definición local de `sp_alm_vol_prod_listarStockMinimo` |
| VT/Detraccion | Consulta de facturas con detracción, detalle y edición del pago | Controlador/vistas de Detraccion, constantes, búsqueda de Comprobante y `sp_comprobante_actualizarDetraccion` |

Ambas pantallas siguen la base de Maestros: cabecera y subtítulo de contexto, buscador ancho, filtros específicos, orden global por columnas, 25/50/100/500 registros, rango/total al pie y Opciones al final. La búsqueda, filtros, orden y paginación utilizan el helper AJAX existente. Cada módulo dispone de un único modal extendido con cabecera/pie fijos y contenido desplazable; los controles respetan los temas Bootstrap.

## Punto de pedido

- Requiere seleccionar una sede activa de la empresa, dentro del ejercicio de la sesión. No cambia la sede de trabajo del usuario.
- Se conserva el cálculo del procedimiento: suma `(cant_ant + cant_ing) - (cant_reserva + cant_sal)` de los registros relacionados con cierres de almacén cuyo estado de apertura/cierre tiene código `A`.
- Se muestran mínimos registrados mayores que cero, exclusivamente cuando el saldo es **menor** que el mínimo. Se conservan saldos negativos.
- Se mantiene una fila por relación producto–proveedor y las repeticiones de saldo que eso genera. No se suma el grid ni se infiere una cantidad a comprar.
- Se conservan los filtros por código de producto contenido, documento exacto del proveedor y razón social contenida. Se añade un buscador conjunto. La consulta anterior excluía las filas sin proveedor mediante sus condiciones de texto/documento; se mantiene ese comportamiento.
- Se conservan las asociaciones históricas y la ausencia de filtros adicionales de estado de producto, mínimo y asociación. Se restringen productos y proveedores a la empresa actual.
- El detalle incluye pesos neto/bruto y master pack del producto, conforme a la proyección anterior. Excel contiene todas las filas filtradas en el orden seleccionado y esos campos adicionales. Si supera 50.000 filas se rechaza la exportación para acotar filtros.
- La pantalla anterior era una consulta. No se inventa una operación automática de generación de pedidos de compra.

## Detracciones

- Muestra facturas por código de tipo de documento `01`, estado de comprobante `3` (emitido), tipo de detracción distinto de `000` y registro activo, conservando las relaciones de la consulta anterior.
- La sede de consulta se inicia con la sede de trabajo y admite otras sedes activas de la misma empresa/ejercicio. La factura consultada/editada debe pertenecer a esa sede y seguir cumpliendo los criterios del módulo.
- Filtros por número exacto de factura, documento exacto del cliente, razón social, buscador conjunto y fechas. El día final se incluye completo mediante un límite superior exclusivo; se admiten también límites de fecha independientes.
- Se muestran importe y moneda del comprobante, tipo/tasa de catálogo e importe de detracción registrados. No se recalcula la detracción ni se mezclan importes de distintas monedas.
- El modal permite modificar solamente número de operación, fecha, monto de pago y cargo, los cuatro campos que actualiza el procedimiento legacy. La tasa y el monto de detracción se conservan.
- Operación requerida de hasta 50 caracteres, fecha válida, pago mayor que cero y cargo no negativo. Se verificaron los controles legacy: el monto exige valor no cero y los controles numéricos no habilitan signo negativo. Los parámetros del procedimiento son `DECIMAL(12,4)` aunque las columnas admiten más precisión; se valida el límite de **`99999999.9999`** y hasta cuatro decimales antes de invocarlo.
- Los importes editables viajan como cadenas decimales invariantes y se convierten con `decimal`, sin pasar por números JavaScript. El detalle conserva también importes de mayor precisión como texto exacto.
- El procedimiento no actualiza `fecha_modifica`, aunque recibe usuario de acción. Se conserva su comportamiento y se pasa el usuario autenticado; no se agregan escrituras de auditoría ajenas a esa operación. Para evitar sobrescrituras, la revisión incluye los valores del pago además de la fecha de revisión y datos del comprobante.
- El guardado vuelve a validar empresa/contexto, sede, elegibilidad y revisión dentro de una transacción serializable, antes de llamar al procedimiento. Un pago modificado por otra operación se detecta incluso si no cambió la fecha de modificación.

## Integración

- `Models/ComprasVentasConsultaViewModels.cs`: modelos de consulta/sede, filtros, filas y solicitud de pago.
- `Services/ConsultaSedeSql.cs`: validación compartida de contextos activos y sedes del mismo ejercicio/empresa, con soporte para la comprobación dentro del guardado.
- `IComprasService`/`ComprasService`, registrado en `Program.cs`, y ampliación del servicio Ventas mediante `VentasService.Detracciones.cs` e `IVentasService`.
- Controladores/vistas `CM/PuntoPedido` y `VT/Detraccion`; script `ventas-detracciones.js`. Punto de pedido reutiliza la presentación de detalle de los reportes y el generador Excel existentes.
- Permisos exactos `cm/puntoPedido/index` y `vt/detraccion/index`. La edición exige permiso de escritura, antiforgery y coincidencia con el contexto autenticado; el permiso de consulta presenta los datos en modo de lectura.
- Enlaces incorporados en Compras y Ventas. TablaGeneral permanece intacto.

## Verificación

El arnés temporal externo usa conexiones que rechazan DML, procedimientos y transacciones reales. Los guardados se verifican con un registrador que simula la llamada, commit/rollback y cambios concurrentes; no ejecuta esas operaciones en SQL Server.

- Punto de pedido: muestra de **143 filas**, cotejando toda la proyección con el cuerpo SELECT del procedimiento original, ejecutado únicamente como SELECT.
- Detracciones: muestra de **585 facturas**, contrastando elegibilidad, orden original y campos del grid/pago con una consulta independiente basada en la búsqueda legacy.
- Se probaron filtros combinados, aislamiento por empresa/sede/ejercicio, continuidad entre páginas, columnas en ambos sentidos, rangos de fechas, cantidades de página y resultados vacíos.
- Las comprobaciones de guardado incluyen parámetros decimales exactos, usuario autenticado, rechazo de datos inválidos/revisión obsoleta, cambio de pago concurrente sin actualización del timestamp y rollback simulado ante error.
- **140 comprobaciones del nuevo servicio aprobadas** y **2.323 comprobaciones acumuladas aprobadas**, incluyendo regresiones anteriores y HTTP. Se verificaron permisos exactos, acceso de consulta, antiforgery, contexto cambiado, errores de binding, detalle con decimales exactos y Excel completo. El endpoint de guardado se comprobó con un servicio simulado para validar el binding y el uso de empresa/contexto/usuario del servidor.
- Se renderizaron las **59 vistas Index** revisadas en variantes vacía y con datos ficticios. Auditoría estructural: **1.573 comprobaciones**, 116 grids, 134 modales y 778 cabeceras de datos, sin fallos; 105 métodos anteriores de escritura comparados con la referencia temporal disponible.
- JavaScript: cero diagnósticos TypeScript. Scripts de vistas: 55 analizados, cero errores de sintaxis. Protección de TablaGeneral: 10 comprobaciones aprobadas.
- `dotnet build --no-restore -p:UseAppHost=false`: compilación correcta, cero errores; permanece la advertencia CS8602 incidental en `FamiliaProducto` sin cambios. Diff y archivos nuevos sin errores de espacios.

Registros finales en `%TEMP%/arco-planilla-validation/punto-detraccion-regresion-final.log` y `%TEMP%/arco-ui-ajustes/punto-detraccion-*`. En la comprobación preliminar se corrigió el uso de una tupla como parámetro de Dapper en el arnés, sustituyéndola por un objeto de parámetros con propiedades; la ejecución final pasó completa.

No se ejecutaron escrituras reales, procedimientos de escritura ni secuencias en la base. No se cambiaron dependencias, SDK ni configuración SQL, ni se detuvo la aplicación del usuario. La revisión visual interactiva en navegador y la ejecución real del guardado quedan pendientes.
