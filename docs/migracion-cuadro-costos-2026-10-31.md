# VT / Cuadro de costos — 2026-10-31

Se incorporó `/VT/CuadroCosto`, accesible desde Ventas y protegido por el permiso `vt / cuadroCosto/index`. El controlador usa la resolución de empresa, sede y ejercicio de `ModuloContextController`. El módulo legacy ofrece consultas y Excel; no tiene una operación funcional de edición de costos en su controlador. La implementación moderna tampoco agrega escrituras.

## Pantalla y resultados

- Cabecera, filtros, tamaños 25/50/100/500, total y rango, pie de paginación y columna final de opciones según Maestros. Orden global por las nueve columnas, con desempate por registro de precio y saldo antes de paginar.
- Lista de precios activa de la sede de trabajo; búsquedas combinables por código y descripción. El campo Código busca ahora el código del producto: el procedimiento anterior aplicaba por error ambos filtros sobre descripción.
- Actualización con el componente AJAX existente, sin navegación completa al filtrar, ordenar o paginar. Consulta de cada fila en un modal extendido con cabecera/pie del tema y valores tratados como texto.
- Excel completo e impresión/PDF con los mismos filtros, orden y base de cálculo elegida. Se rechazan exportaciones de más de 50.000 filas, sin truncarlas. Excel conserva los valores decimales; los que exceden 15 cifras significativas se guardan como texto para evitar la pérdida de precisión de Excel.

## Dos bases legacy explícitas

Las definiciones SQL locales `sp_cuadro_costo_buscarPor` y `sp_rptvt_cuadroCosto` difieren. No se mezclaron sus fórmulas:

| Regla | Consulta, seleccionada inicialmente | Reporte |
|---|---|---|
| Costo promedio y última compra | Multiplicados por `1 + tasa IGV / 100`, tipo de impuesto `01` | Sin multiplicar por IGV |
| Precio de venta | Precio registrado en moneda de la lista | Moneda local; si la lista es extranjera, precio por T/C compra tipo `02` de hoy, redondeado a dos decimales |
| Fecha de última compra visible | Transacción `S01` o `S13` | Transacción `S01` |
| Reposición | Valor original del producto | Valor original del producto |
| Márgenes A / B | `(precio − costo) / precio`; cero si precio es cero | La misma fórmula, sobre los valores de esta base |

Los márgenes se identifican como fracciones, sin multiplicarlos por 100. La base seleccionada, su tratamiento de moneda/IGV y los filtros se incluyen en las salidas y en la nota del modal. A diferencia del legacy, exportar no cambia silenciosamente la base de cálculo: para obtener la base de su antiguo Excel se elige **Reporte**.

El mes se obtiene del calendario actual del servidor y se resuelve a su identificador real en `mes_contable`, dentro del ejercicio y sede seleccionados. La vigencia del producto conserva el intervalo de años de alta/baja del legacy. Los costos promedio proceden de `mov_alm_s_con`; la última compra considera movimientos con cantidad positiva, detalle activo y transacción de compra en todas las sedes y ejercicios de la empresa. Se conserva la selección por fecha y valor descendentes, agregando el ID del detalle para resolver empates. No se agregaron filtros de estado de cabecera o de registro de precio que el legacy no aplicaba.

Se comprueba adicionalmente que el producto pertenezca a la empresa mediante su jerarquía de tipo/grupo/clase/familia. Una lista de otra sede o un contexto de otra empresa se rechazan. `CuadroCosto` es un resultado calculado: no existe una tabla `cuadro_costo` en esta base, ni hace falta crearla.

No se adivina una tasa cuando falta o hay más de una configuración de IGV. El reporte tampoco elige arbitrariamente entre varios T/C tipo `02` del día. Si falta el T/C y la lista es extranjera, se conservan los ceros del reporte legacy con una advertencia visible e incluida en las salidas.

## Validación

- **114 comprobaciones de servicio aprobadas**, con una conexión que rechaza DML, procedimientos y transacciones. Comparación de cada importe, fecha y margen con los SELECT extraídos de las definiciones legacy; no se ejecutaron los procedimientos. Se respetaron también los tipos `decimal(19,4)` de sus variables para contrastar la precisión SQL.
- Se verificaron ambas bases, las nueve columnas en ambos sentidos, orden antes de paginar y páginas estables, cuatro tamaños, filtros combinados, límites y exportación completa, contexto y listas ajenas, y advertencia por falta de T/C.
- **26 comprobaciones HTTP aprobadas** en un servidor temporal separado: Razor real vacío y con datos en ambas bases, TagHelpers de orden, sesión, permiso exacto y denegado en las tres acciones, validación del model binding y los Excel e impresiones reales. El servidor temporal se detuvo al terminar.
- Auditoría estructural de HTML: 1.629 comprobaciones, 121 tablas, 139 modales y 809 cabeceras, sin fallos. Incluye las tres capturas nuevas y capturas anteriores; no acredita una regresión funcional global.
- JavaScript comprobado con TypeScript; sin diagnósticos. Scripts inline analizados estáticamente; sin errores sintácticos. Protección de TablaGeneral comprobada sin cambios.
- Compilación sin errores. Permanece la advertencia preexistente CS8602 en `Areas/MG/Views/FamiliaProducto/Index.cshtml:151`.

No se realizaron escrituras en la base, cambios de esquema, ejecución de secuencias o procedimientos, cambios de dependencias ni reinicios de la aplicación del usuario. No se certificó la interacción visual en navegador ni los temas mediante automatización. La regresión funcional global no se presenta como aprobada: continúa documentada la interrupción del lote anterior en Documentos temporales en `migracion-procesos-produccion-2026-10-30.md`.
