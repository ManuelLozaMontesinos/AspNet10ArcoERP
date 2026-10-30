# Reportes de guías y valorización de compras — 30/10/2026

## Alcance entregado

| Área / ruta | Estado | Fuente verificada |
|---|---|---|
| IV/RepoGuiaRemi | Consulta, detalle, Excel e impresión implementados | Controlador/vista legacy, `IV_GuiaRemision.rdl`, definición local de `sp_rptiv_guiaRemision` |
| CM/RepoValorizaComprob | Consulta, detalle, Excel e impresión implementados | Controlador/vista legacy y definición local de `sp_rptcm_valorizaComprobante` |

Este lote migra los dos módulos de **reportes**. Las operaciones transaccionales de creación y emisión de guías, órdenes y valorizaciones continúan pendientes. El Kardex valorizado mantiene su dependencia de un procedimiento ausente localmente.

Ambas pantallas siguen la base de Maestros: cabecera, filtros y buscador, orden por columna antes de paginar, cantidades 25/50/100/500, contador/rango al pie y Opciones al final. Los filtros, orden y paginación utilizan el helper AJAX existente. El detalle se consulta en un único modal extendido, con cabecera y pie fijos, colores semánticos y contenido desplazable.

Excel contiene todos los campos del detalle y todas las filas filtradas en el orden seleccionado. La impresión utiliza las columnas principales y todas las filas filtradas, en una vista independiente preparada para imprimir o guardar como PDF. El límite de exportación/impresión es 50.000 filas; si se supera, se solicita acotar los filtros, sin generar archivos truncados. Los decimales del modal se envían como texto exacto; Excel reutiliza la preservación de precisión del helper común.

## Reglas del reporte de guías

- Contexto exacto de período/sede de la sesión. Estados de guía por código `2` (EMITIDO) y `9` (ANULADO), conservando las relaciones obligatorias de la consulta anterior. No se incorpora un filtro adicional por estado del registro.
- Consulta mensual sobre la fecha de la guía o rango de fechas. El extremo final incluye el día completo mediante un límite exclusivo del día siguiente; se validan fechas, orden del rango y meses disponibles.
- Se conserva el flete registrado, peso, fecha de traslado, cliente/destinatario, direcciones y establecimientos, referencias, documentos del conductor, placas, motivo, estado SUNAT y observaciones. No se inventa una moneda o conversión para el flete.
- Se corrige un error de proyección del procedimiento: el ubigeo de llegada utilizaba los alias de partida aunque ya tenía las relaciones de llegada. Ahora usa departamento/provincia/distrito del destino registrado.
- Las referencias a clientes, destinatarios y transportistas se restringen a la empresa del contexto. Las relaciones opcionales del documento referido y del estado SUNAT conservan `LEFT JOIN`.

## Reglas de valorización de comprobantes

- El reporte anterior filtra por **período y mes de la fecha de la orden de compra**, incluyendo todas las sedes del período. Se conserva ese alcance, acotándolo expresamente a la empresa actual y rotulándolo en pantalla.
- Comprobantes de compra con `estado='A'`. Se conserva la ausencia de un filtro adicional por estado de la orden.
- Filtro opcional por proveedor, incluidos proveedores históricos con comprobantes en ese ejercicio. El selector se limita a la empresa y período; se rechazan identificadores ajenos.
- Se muestran fechas de comprobante/vencimiento, orden, tipo/serie/número, identidad del proveedor, moneda/símbolo, bruto, impuesto, total y condición de pago. Los importes son los almacenados, en la moneda de la orden; no se suman monedas distintas.
- La condición de pago conserva su relación opcional y se acota a la empresa.
- Estos reportes utilizan la tabla `mes`; los valores de los filtros se obtienen de su **código**, sin confundirlos con la FK de `mes_contable`.
- La impresión legacy de Compras apuntaba a `CM_OrdenCompra`, aunque la exportación de datos utilizaba el procedimiento de valorización. La nueva impresión comparte el resultado filtrado de la consulta de valorización que se muestra en pantalla.

## Integración

- `Models/DocumentoReporteViewModels.cs`: filtros, contexto, filas y definiciones cerradas de columnas para ambos reportes.
- `Services/IReporteDocumentosService.cs` y `ReporteDocumentosService.cs`: servicio Dapper de consulta registrado en `Program.cs`. Orden SQL limitado a columnas conocidas, conservación del orden secundario original y desempate final por ID, parámetros para filtros y validación del contexto activo.
- `Controllers/DocumentoReporteController.cs` y controladores por área. Se reutiliza `ModuloContextController`, con permisos exactos `iv/repoGuiaRemi/index` y `cm/repoValorizaComprob/index` para consulta, Excel e impresión. La recuperación de sesión en GET sigue el comportamiento compartido.
- Vistas de ambos módulos, imports/layout del área CM y partial `_DocumentoReporte.cshtml`. Se reutilizan el script de presentación de reportes, los controles de listado, el generador Excel y la vista de impresión existentes, sin alterar su comportamiento.
- Navegación: enlace en Inventario y sección Compras con su reporte. TablaGeneral y su navegación permanecen intactos.

## Validación

La verificación usa el arnés temporal externo con una conexión que rechaza DML, procedimientos y transacciones reales. Se contrastaron todos los campos con el cuerpo SELECT de cada procedimiento, extraído de su definición y ejecutado únicamente como SELECT. Para Guías se aplicó a esa referencia la corrección documentada del ubigeo de destino; en Compras se acotó la referencia a la empresa.

- Muestra de Guías: **481 filas** en el contexto/mes seleccionado.
- Muestra de Compras: **150 filas**, incluyendo las sedes de la empresa en el ejercicio.
- **312 comprobaciones del nuevo servicio aprobadas**: columnas en ambos sentidos, continuidad entre páginas y orden global, orden secundario original, tamaños permitidos, resultados completos, fechas, búsqueda, contexto, proveedor y equivalencia de todas las proyecciones.
- **2.142 comprobaciones acumuladas aprobadas**: regresión anterior, servicio nuevo y HTTP de ambos módulos. Se comprobaron permisos exactos y denegaciones, recuperación de sesión, respuesta de conflicto para sesiones AJAX inválidas, filtros/orden renderizados, Excel con todas las filas y campos de detalle, impresión independiente, precisión decimal y rechazo de exportaciones inválidas.
- Renderizado de las **57 vistas Index** revisadas, vacías y con datos ficticios. Auditoría estructural: **1.511 comprobaciones**, 112 grids, 130 modales y 736 cabeceras de datos, sin fallos; 105 métodos anteriores de escritura comparados con la referencia temporal disponible.
- JavaScript compartido: cero diagnósticos TypeScript; 53 scripts de vistas analizados, cero errores de sintaxis. Protección de TablaGeneral: 10 comprobaciones aprobadas.
- `dotnet build --no-restore -p:UseAppHost=false`: compilación final correcta, cero errores y cero advertencias en la compilación incremental. La recompilación anterior mostró la advertencia CS8602 preexistente en `Areas/MG/Views/FamiliaProducto/Index.cshtml:151`, que permanece sin cambios. Diff sin errores de espacios.

Registros externos: `%TEMP%/arco-planilla-validation/documentos-regresion-final.log`; auditorías en `%TEMP%/arco-ui-ajustes/documentos-*`. El primer intento HTTP detectó que el cliente de prueba seguía automáticamente la redirección por sesión inválida; se ajustó el arnés para comprobar el conflicto AJAX y la ejecución final pasó completa.

No se ejecutaron escrituras en la base, procedimientos de escritura ni secuencias. No se alteraron dependencias, SDK, configuración SQL ni el proceso de la aplicación del usuario. La revisión visual interactiva en navegador queda pendiente; el renderizado HTTP y la auditoría estructural no la sustituyen.
