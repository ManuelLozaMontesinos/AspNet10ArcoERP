# VT / Registro de ventas — 2026-10-31

Se incorporó `/VT/RepoRegVentas`, con el permiso exacto `vt / repoRegVentas/index`. Su enlace ocupa el primer lugar de **Ventas → Reportes**, conforme al menú legacy. Utiliza el servicio de Ventas y los componentes de listados, consulta de registros, Excel e impresión ya existentes. No se agregaron dependencias ni cambios de configuración o esquema.

## Pantalla

Se implementaron las cuatro combinaciones de los formularios originales: consolidado/general, consolidado/oficial, detallado/general y detallado/oficial. La consulta permite elegir un mes calendario activo o un rango de fechas y buscar documentos, clientes y, en el detalle, productos y clasificación.

La interfaz mantiene la base de Maestros: cabecera, barra de filtros y búsqueda, orden global por todas las columnas antes de paginar, tamaños 25/50/100/500, rango y total visible, pie, columna Opciones al final y actualización AJAX. Un único modal extendido permite consultar todos los campos de la fila. Los totales filtrados de bruto, neto, IGV y total local abarcan el resultado completo, independientemente de la página visible.

Excel e impresión/PDF conservan filtros, variante y orden, e incluyen la nota de cálculo y los totales. Exportan todos los registros filtrados; si superan el límite compartido de 50.000, se solicita acotar los filtros y no se entrega una salida truncada. La salida PDF utiliza la impresión del navegador y la plantilla compartida, sin incorporar un motor de informes nuevo.

## Reglas contrastadas

Fuentes: controlador y vista `RepoRegVentas`, procedimientos `sp_rptvt_registroVenta` y `sp_rptvt_registroVentaProducto`, y reportes `VT_RegistroVentaGeneral.rdl`, `VT_RegistroVentaOficial.rdl` y `VT_RegistroVentaProducto.rdl`. Se verificaron las columnas y relaciones de comprobantes, líneas, notas de crédito, monedas y clasificaciones en SQL Server.

- El consolidado general incluye comprobantes normales (`tipo_comprob = 1`), internos y externos. El consolidado oficial incluye los externos de todos los tipos excepto temporales (`4`). Ambos requieren `emitido = S` y estados de comprobante `3` o `9`.
- El consolidado incorpora las notas de crédito mediante `UNION ALL`, según la sede/período y fecha de la nota. Heredan cliente, moneda y T/C del comprobante de referencia. Se conservan los criterios propios de esta rama del reporte; no se le aplica silenciosamente el filtro de tipo de comprobante de la otra rama.
- La identidad del consolidado sigue la regla legacy: documento de identidad para persona natural con boleta o nota referida a un documento distinto de factura; RUC en los demás casos. En el detalle se conserva el RUC que devuelve su procedimiento, sin reemplazarlo por la regla del consolidado.
- Los importes se convierten con el T/C del comprobante cuando la moneda es extranjera; en moneda local se conservan sin multiplicar. Se mantienen los decimales calculados por SQL y se muestran cuatro decimales. Los valores de consulta viajan como cadenas exactas y Excel preserva los importes que exceden su precisión numérica.
- En el consolidado, un estado distinto de activo presenta `***ANULADO***` y los cuatro importes a cero. Las notas de crédito mantienen sus valores por fila; los totales las restan por código de documento `07`, tal como hacen las expresiones de los RDL. No se aplica una segunda negación al valor visible de la fila.
- El detalle general incluye solo comprobantes normales; el detalle oficial incluye todos los tipos externos, también temporales, porque su procedimiento elimina la restricción por tipo. Ambos usan estado de comprobante `3`, `emitido = S` y líneas activas. No incorporan la tabla separada de notas de crédito ni reemplazan los importes de línea por los de la cabecera.
- El detalle conserva cantidad, descripción de línea, producto, unidad, grupo, clase y familia. No añade filtros por estado del producto o de la cabecera que no existan en el procedimiento original. Se muestra el estado de la cabecera como información adicional.
- La búsqueda y paginación se aplican después de formar el resultado de cada reporte. Los IDs se distinguen por origen para impedir colisiones entre comprobantes y notas de crédito al ordenar y paginar.
- El rango incluye el día final completo mediante un límite superior exclusivo del día siguiente, coherente con los demás reportes migrados. Corrige la exclusión de horas posteriores a medianoche del filtro antiguo.

Todas las consultas verifican empresa, período y sede activos desde el contexto de acceso. Se comprueba además la pertenencia de cliente, medio de pago, producto y comprobante referenciado cuando corresponde. El módulo es exclusivamente de consulta y no ejecuta procesos de emisión ni modifica documentos.

## Validación

La validación funcional utiliza conexiones que permiten únicamente SELECT y rechazan procedimientos, DML y transacciones reales. Para contrastar el legacy se extrajeron las consultas SELECT de las definiciones, sustituyendo su lista de tipos por un SELECT equivalente; no se ejecutaron los procedimientos ni sus instrucciones internas de escritura.

- **288 comprobaciones de servicio aprobadas**. Se contrastaron todas las filas, campos e importes de marzo de 2026 de una sede con los SELECT originales:

  | Variante | Filas | Notas de crédito separadas |
  |---|---:|---:|
  | Consolidado general | 6.919 | 43 |
  | Consolidado oficial | 6.572 | 43 |
  | Detallado general | 26.579 | 0 |
  | Detallado oficial | 25.302 | 0 |

- Se verificaron los cuatro totales firmados conforme a los RDL, todas las columnas en ambos sentidos, orden numérico y por fecha antes de paginar, identidad estable entre páginas, tamaños, filtros y contexto inválidos, igualdad entre consulta mensual y rango completo, búsqueda, resultados vacíos y Excel completo. Los datos contrastados incluyen comprobantes en moneda extranjera y documentos anulados. El límite de exportación se comprobó con un recuento simulado de 50.001 filas, sin entregar resultados parciales.
- **44 comprobaciones HTTP aprobadas**: Razor real, filtros y TagHelpers, permiso exacto y denegación en las tres acciones, recuperación del contexto en GET, sesión inválida/AJAX 409, binding y longitud de búsqueda, resultados AJAX en las cuatro variantes, enlaces con filtros y orden, archivos Excel reales e impresión/PDF, y rechazo de exportaciones con filtros inválidos.
- **137 comprobaciones HTTP de regresión aprobadas**: Canjes/Liquidación y las pantallas iniciales de reportes anteriores (43), Bancarización (31) y Parámetros/navegación recursiva (63). Total funcional específico del lote: **469 comprobaciones aprobadas**.
- Auditoría estructural: **1.921 comprobaciones**, 139 tablas renderizadas, 157 modales y 1.011 cabeceras, sin fallos. Incluye cinco capturas HTML nuevas y las pantallas anteriores; no acredita una regresión funcional global ni una inspección visual en navegador.
- JavaScript compartido comprobado con TypeScript, cero diagnósticos. Se analizaron 64 scripts inline sin errores sintácticos y pasaron las diez comprobaciones de protección de TablaGeneral.
- Compilación correcta, cero errores; permanece únicamente la advertencia CS8602 preexistente de `Areas/MG/Views/FamiliaProducto/Index.cshtml:151`.

Queda pendiente la interacción visual en navegador. La regresión funcional global no se declara aprobada; su interrupción anterior consta en `migracion-procesos-produccion-2026-10-30.md`. No se modificó MG/TablaGeneral, no se ejecutaron escrituras en la base ni se reinició la aplicación del usuario.
