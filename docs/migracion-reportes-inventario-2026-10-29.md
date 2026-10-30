# Consultas y reportes de Inventario — 29/10/2026

Continuación de la migración de `ERP-Arco n5` a .NET 10. Se incorporan tres módulos de consulta a la navegación de Inventario; se conservan los cambios anteriores del worktree y la exclusión de MG/TablaGeneral.

## Alcance implementado

| Módulo | Consulta y filtros | Detalles |
|---|---|---|
| `IV/ConsultaSaldo` | Saldos mensuales por producto, descripción, SKU y referencia. Total físico, reserva y disponible; columnas físicas por almacén activo de la sede. | Composición por almacén, incluidos los inactivos con saldos. El detalle reconcilia los totales físicos, reservados y disponibles. |
| `IV/RepoKardex` | Kardex de unidades mensual, hasta fecha y anual; almacén y producto. El anual exige producto. Saldo inicial más movimientos con saldo acumulado. | Cabecera y productos del movimiento en modal. El saldo de cada fila conserva su valor cronológico aunque se cambie el orden de presentación. |
| `IV/RepoMoviAlma` | Movimientos por mes o día, almacén, transacción, tercero y búsqueda por producto/detalle/operación. Incluye solicitante, O/C, guía, documento y observación. | Cabecera y productos del movimiento en modal. Se conserva el valor unitario para ingresos y cero para salidas según el reporte anterior. |

Cada consulta ofrece Excel e impresión del conjunto filtrado completo, independiente de la página visible. El Excel conserva números y fechas como celdas tipadas, filtros de columnas, cabecera congelada y descripción de los filtros aplicados. La vista de impresión permite guardar PDF mediante el navegador.

El estado es **consulta implementada, pendiente de validación funcional y visual por usuarios**. No se declara migrado todo Inventario ni se agregan operaciones de escritura en movimientos, reservas o existencias.

## Interfaz y navegación

- Patrón de Maestros: cabecera con icono, título y resumen; acciones a la derecha; filtros compactos, tabla y pie de resultados.
- Cantidades 25/50/100/500, predeterminado 25, total filtrado y rango visible. Orden global en SQL antes de paginar, con desempate estable.
- Todas las columnas de datos de las grillas son ordenables, incluidas las columnas dinámicas de almacenes. Opciones permanece al final.
- Filtros, orden, tamaño y páginas utilizan el helper AJAX existente. La actualización conserva la pantalla principal, los controles y sus listeners; actualiza los enlaces de exportación, el resumen de cabecera y la configuración del módulo.
- Un modal `xl`, centrado y desplazable, con cabecera y pie fijos. El mismo modal sirve para detalles o selectores, sin anidar ventanas.
- Selectores de productos y terceros con búsqueda, orden por código/nombre, 25 registros por página, rango y Anterior/Siguiente. Los selectores no cargan el catálogo completo.
- Bootstrap con tokens de tema; SweetAlert2; datos incorporados al DOM mediante `textContent`. Razor codifica los textos en reportes y grillas.

## Fuentes y reglas contrastadas

Fuentes: controladores/vistas legacy de `ConsultaSaldo`, `RepoKardex` y `RepoMoviAlma`; reportes RDL de saldos, Kardex mensual/fecha y movimientos; definiciones SQL consultadas mediante SELECT de metadatos.

- Saldos: `mov_alm_saldo`, `periodo_emp_sede`, `mes_contable`, `almacen`, `empresa_sede`, jerarquía del producto y unidad de medida. Físico = anterior + ingresos − salidas; disponible = físico − reserva. Los totales incluyen almacenes inactivos con saldo, como la consulta anterior; las columnas principales representan almacenes activos de la sede.
- Kardex: `sp_rptiv_kardexUnidadMensual`, `sp_rptiv_kardexUnidadFecha` y `sp_rptiv_kardexUnidadAnual`, leídos sin ejecutarlos. La consulta usa período, almacén y meses contables ordinarios; incluye filas activas que afectan saldo. La cabecera de movimiento no recibe un filtro de estado adicional ausente en el reporte legacy.
- Los datos locales contienen movimientos cuya fecha calendario no coincide con el año del período. Mensual/anual conservan la selección por período y mes contable del legacy, sin omitirlos mediante un filtro adicional de año calendario. Hasta fecha toma el mes contable de la fecha elegida y el corte exclusivo del día siguiente. La pantalla muestra tanto el ejercicio seleccionado como la fecha del corte.
- El anual anterior elimina y rellena la tabla compartida `ProductoKardexAnual`. La implementación nueva obtiene el saldo inicial de enero y los movimientos de los meses 01–12 mediante SELECT, sin tabla intermedia ni escrituras. No acumula los saldos iniciales de cada mes.
- Movimientos: datos equivalentes a `sp_rptiv_movimientoAlmacen` y su variante `2`, con las columnas de ambas variantes disponibles. Conserva el período/sede exacto, transacción, almacén, tercero y condición de afectación de saldo; excluye S11 del selector como el legacy.
- Los cortes por día incluyen las horas del día seleccionado mediante `fecha < día siguiente`; evitan el recorte a medianoche de las comparaciones antiguas.

## Contexto, permisos y aislamiento

- Reutiliza `InventarioContextController`, incluida la recuperación validada de sesión desde la cookie en GET. La selección de contexto sigue perteneciendo a Perfil de acceso.
- Los permisos de lectura se comprueban también para Excel, impresión, selectores y detalles: área `iv`, URLs de menú `consultaSaldo/index`, `repoKardex/index` y `repoMoviAlma/index`.
- Empresa, usuario y período/sede proceden del contexto validado. Los identificadores de filtros se contrastan con catálogos y relaciones de la empresa.
- Kardex ofrece todos los almacenes de la empresa sólo a los roles administradores existentes; para los demás utiliza los de la sede. Saldos utiliza la sede seleccionada. Movimientos mantiene el ámbito y selector del reporte legacy.
- El SQL emplea parámetros; las expresiones de orden son listas permitidas. Los nombres de columnas dinámicas se construyen únicamente con IDs enteros de almacenes consultados en el servidor.
- Exportación e impresión tienen un límite explícito de 100.000 filas. Si se supera, se solicita reducir los filtros; no se entrega un archivo truncado aparentando éxito.
- No se modifican dependencias NuGet ni el módulo MG/TablaGeneral.

## Archivos principales

- `Models/InventarioReportesViewModels.cs` y ampliación de `IInventarioService`.
- `InventarioService.Reportes.cs`, `.Saldos.cs`, `.Kardex.cs` y `.MovimientosReporte.cs`.
- Controlador base de reportes y los tres controladores del área IV.
- Vistas `ConsultaSaldo`, `RepoKardex`, `RepoMoviAlma` y parciales comunes de reportes del área IV.
- `wwwroot/js/inventario-reportes.js`; actualización acotada de cabecera en `erp-listings.js`.
- Tres entradas nuevas en el grupo Inventario de `_Layout.cshtml`.

## Validación realizada

- `dotnet build --no-restore`: compilación correcta, cero errores. El build incremental final no emitió advertencias; la recompilación del lote conserva únicamente CS8602 preexistente en FamiliaProducto:151.
- 807 comprobaciones agregadas de servicios y HTTP, 161 adicionales respecto al cierre de la recuperación de sesión. Arnés temporal fuera del repositorio, con servicios reales de consulta y conexiones que rechazan DML, SP, transacciones y secuencias.
- SQL real: órdenes ascendentes/descendentes, columnas dinámicas de almacenes, páginas consecutivas, filtros, ámbitos inválidos y referencias ajenas. Reconciliación independiente de saldo inicial/movimientos del Kardex y de físicos/reservas/disponibles de la consulta de saldos.
- Rutas reales de los tres reportes: entrada inicial, recuperación de sesión, permisos de lectura, denegación de acciones sin permiso, binding de filtros, validaciones visibles, selectores y detalles.
- Excel abierto y comprobado con ClosedXML: todas las filas filtradas, incluidas consultas de más de 25 filas; cantidades numéricas, rango completo de autofiltro y descripción del contexto. Impresión verificada contra la misma cantidad de resultados.
- Renderizado HTTP de las 41 vistas de listado, vacías y con datos ficticios. Auditoría estructural: 1.143 comprobaciones, 40 grillas y 49 modales en ambos estados; sin fallos. Los 109 métodos de escritura preexistentes comparados permanecen intactos.
- JavaScript externo con TypeScript `allowJs/checkJs/noEmit` y nulabilidad: cero diagnósticos. 40 bloques de script de vistas sin errores de sintaxis.
- Comparación del módulo excluido y del helper compartido: 10 comprobaciones aprobadas. MG/TablaGeneral permanece sin cambios. `git diff --check` sin errores.
- Para HTTP se simularon autenticación, permisos/contexto y operaciones de escritura del lote anterior. Las lecturas de inventario utilizaron los servicios y SQL reales; no se atribuyen esas pruebas a la sesión activa del navegador del usuario.

## Límites pendientes

- Validar con usuarios los resultados y la presentación en ambos temas, tamaños de pantalla y navegación real. Las comprobaciones automáticas cubren SQL de lectura, HTTP, renderizado y estructura; no constituyen una prueba visual interactiva en navegador.
- La impresión/PDF utiliza el navegador. No se ha migrado el motor RDL ni la maquetación exacta de los PDF anteriores.
- No se han migrado en este lote el registro de movimientos de almacén, transferencias, reservas, Kardex valorizado ni los demás procesos operativos de Inventario.
- No se ejecutaron DML, SP de modificación, transacciones de escritura ni secuencias durante la inspección o las pruebas.
