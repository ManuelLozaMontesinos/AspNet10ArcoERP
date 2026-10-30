# Saldos de Inventario y existencias mínimas — 30/10/2026

Lote implementado en `AspNet10ArcoERP` (`master`) después de Apertura/cierre, Reproceso y Consulta de llegadas. Fuentes contrastadas con `ERP-Arco n5` (`main`). Se conservan los cambios anteriores; MG/TablaGeneral permanece excluido.

## Módulos incorporados

| Módulo | Alcance |
|---|---|
| `IV/RepoSaldo` | Consolidado general, matriz por almacenes con opción de descontar reservas y detalle por almacén al mes o al día. Producto opcional, búsqueda, composición del saldo, Excel e impresión. |
| `IV/RepoSaldoVal` | Saldo valorizado mensual de la empresa, con sede, cantidades, reservas y costos unitarios de ocho decimales. Consulta de valores registrados, detalle, Excel e impresión; no ejecuta valorizaciones. |
| `IV/VolumenExisten` | Consulta y edición individual del stock mínimo por sede, Excel con formato compatible, plantilla e importación XLSX con validación, vista previa y confirmación del lote. |

Las pantallas mantienen el patrón de Maestros: cabecera y acciones a la derecha, filtros compactos, orden global por columna antes de paginar, cantidades 25/50/100/500, contador/rango y Opciones al final. Filtros, orden y paginación utilizan AJAX; cabecera, enlaces de exportación y configuración se actualizan con el resultado. Modales extendidos, sin anidar ventanas, tokens de tema y SweetAlert2.

Estado: **implementación disponible, pendiente de aceptación funcional/visual y validación autorizada de escrituras reales en mínimos**. No se declara completado todo Inventario.

## Reglas de saldos verificadas

Se inspeccionaron controladores, vistas y RDL de los módulos y las definiciones SQL existentes mediante SELECT, sin ejecutar procedimientos.

- `sp_rptiv_saldoUnidadConso`: el consolidado suma anterior, ingreso y salida de todos los almacenes/sedes del período; no descuenta reservas.
- `sp_rptiv_saldoConsoAlmacen`: la matriz conserva la opción de descontar reservas. Las columnas se identifican por ID y se rotulan con sede/almacén, incluidos históricos con saldos; no se mezclan almacenes homónimos. Sus totales y el detalle usan el mismo conjunto filtrado.
- `sp_rptiv_saldoUnidad`: el detalle parte de los saldos mensuales y suma movimientos activos que afectan saldo. Conserva el redondeo de cada cantidad a dos decimales antes de sumar. El saldo no descuenta la reserva informativa. El corte por día incluye todas sus horas mediante `fecha < día siguiente`; el mensual utiliza el fin del mes del ejercicio seleccionado.
- Meses ordinarios 01–12 para unidades. La selección usa el código contable, sin confundirlo con el ID del catálogo.
- General y matriz abarcan toda la empresa. Para el detalle, el selector de almacenes conserva el alcance del legacy: administradores pueden seleccionar cualquiera de la empresa, otros usuarios los de su sede.
- `sp_rptiv_saldoValorizado`: lectura de `mov_alm_s_con`, por período y código de mes, incluidos 00 y 13. Saldo disponible = anterior + ingreso − salida − reserva; importe del saldo = saldo disponible × valor unitario registrado.
- El valorizado devuelve registros por sede/producto; no se inventa un promedio entre sedes. Se muestra la sede para distinguirlos. El controlador anterior no enviaba el almacén al SP; la pantalla nueva muestra explícitamente su alcance consolidado y rechaza ese filtro.
- Los costos anterior/actual conservan los ocho decimales de la base en consulta, modal, Excel e impresión. El modal recibe los costos formateados como texto para evitar pérdidas por la conversión a números de JavaScript; Excel utiliza texto para costos que superan sus 15 dígitos significativos. Se utilizan los campos del dataset; no se replica la fórmula de importe de ingresos inconsistente del RDL antiguo.
- Se exige pertenencia a la empresa tanto en almacenes/sedes como en la jerarquía del producto. Todas las consultas están parametrizadas; las columnas de orden y columnas dinámicas proceden de listas/IDs del servidor.
- Excel e impresión incluyen el conjunto filtrado completo, con límite explícito de 100.000 filas. La impresión permite PDF desde el navegador; no se migra el motor RDL ni su maquetación exacta.

## Reglas de mínimos y Excel

Fuentes: `VolumenExistenController`, sus vistas principal/importación, `AlmacenVolumenProductoBusiness/Repository`, esquema `alm_vol_prod`, TVP `AlmacenVolumenProductoType`, configuración de importación `0002` y procedimientos `sp_alm_vol_prod_*` / `sp_rptvt_volumenExistencia`.

- Mínimo por sede, independiente del período; el ejercicio determina la vigencia del catálogo mediante alta/baja del producto. Tipo `P` y grupos sin combos. La sede debe estar activa y pertenecer a la empresa de sesión.
- Se conserva la diferencia del legacy: el listado no excluye productos inactivos vigentes en el año; se señalan con badge. El Excel exporta sólo productos activos, como el SP del formato anterior. Esto se explica en pantalla.
- Sin configuración previa se muestra mínimo cero. Edición individual mediante `sp_alm_vol_prod_insertar` o `sp_alm_vol_prod_actualizar`, con identidad de producto/sede resuelta en el servidor y usuario autenticado para auditoría.
- Mínimos no negativos y hasta cuatro decimales, dentro de `DECIMAL(19,4)`. No se agregan bajas ni modificaciones de existencias físicas.
- El Excel mantiene en la primera hoja, fila 1, las seis columnas verificadas: `alm_vol_prod_id`, `producto_id`, `codigo`, `descripcion`, `unidad_medida`, `stock_minimo`. Metadatos de sede/ejercicio/búsqueda en una hoja separada para permitir la reimportación.
- Importación exclusivamente XLSX: hasta 10 MB, 5.000 filas y 50 MB descomprimidos. Rechaza estructura/encabezados distintos, fórmulas, decimales inválidos, negativos, exceso de precisión, referencias ajenas o ambiguas, identificadores contradictorios y productos duplicados.
- Resolución por ID de configuración, ID de producto o SKU, contrastando su concordancia y el catálogo vigente. Las celdas de descripción/unidad son informativas; no actualizan productos.
- La preparación no escribe. Enumera errores por fila y muestra los mínimos actuales/propuestos. Cualquier error impide habilitar la confirmación del archivo completo. La tabla de vista previa tiene orden y paginación 25/50/100/500.
- Las cantidades se transportan como texto decimal exacto entre preparación y confirmación. La exportación conserva como texto los mínimos que exceden la precisión numérica de Excel para evitar alterar sus dígitos al reimportar.
- Confirmación mediante el TVP y `sp_alm_vol_prod_actualizarLote`, dentro de transacción. Se conserva su regla: un mínimo nuevo igual a cero no crea fila en el lote; una edición individual sigue el SP de alta normal.

## Acceso y revisión de cambios

- Reutiliza `InventarioContextController`, incluida recuperación validada de sesión. Permisos legacy del área `iv`: `repoSaldo/index`, `repoSaldoVal/index`, `volumenExisten/index`.
- Permiso de lectura en consultas, detalles y descargas; los POST de edición, preparación y confirmación de importación exigen permiso de escritura y antiforgery. Los módulos de saldos no conceden acceso al detalle de movimientos de otros menús.
- Solicitudes de mínimos vinculadas al contexto de trabajo y a una revisión del valor/configuración. Se comprueban antes de abrir la transacción y otra vez bajo bloqueos dentro de una transacción serializable. Una fila modificada invalida todo el lote.
- La aplicación resuelve los IDs incluidos en el TVP y la auditoría. No confía en los IDs de sede/producto/registro de una hoja sin validarlos.

## Dependencia pendiente confirmada

El controlador/RDL de `IV/RepoKardexVal` referencia `sp_rptiv_kardexValorizado`. No se encontró ese procedimiento en la base local inspeccionada. No se publica un Kardex valorizado con otra fórmula ni se crea un SP sustituto. Se necesita recuperar/verificar esa definición para migrar ese módulo.

También siguen pendientes el selector de reposición/stock mínimo utilizado por procesos de Compras, el registro completo de movimientos, transferencias/recepción, reservas y los demás procesos de valorización. La gestión principal de mínimos y su importación/exportación sí están incluidas en este lote.

## Archivos principales

- Modelos `InventarioSaldosViewModels`, `InventarioMinimosViewModels`; ampliación acotada de filtros/resumen en `InventarioReportesViewModels`.
- Métodos de `IInventarioService`; nuevos parciales `SaldoUnidades`, `SaldoValorado`, `Minimos` y `MinimosImportacion`. El preparador existente admite los tipos nuevos sin alterar sus reglas anteriores.
- Controladores/vistas de los tres módulos y parcial `_SaldoFiltros`. Helper de exportación/impresión con precisión opcional para costos, conservando el formato anterior por defecto.
- `inventario-reportes.js` reutiliza selectores y detalles; nuevo `inventario-minimos.js`. Tres entradas nuevas en el menú Inventario.

## Validación realizada

- **1.086 comprobaciones agregadas de servicios y HTTP**, 157 nuevas respecto al lote de procesos mensuales. Se conservan las comprobaciones previas.
- SQL de saldos: todos los órdenes ascendentes/descendentes, variantes de unidades, comparación independiente de cantidades almacenadas, reconciliación de columnas/detalle con y sin reserva, corte por día, referencias ajenas y costos de ocho decimales leídos de la base.
- SQL de mínimos: órdenes, catálogo vigente, listado frente a exportación activa, sede/empresa y revisión actual. La importación prueba formato exportado, encabezados, fórmulas, negativos, precisión, valores no numéricos, referencias ajenas, identificadores contradictorios, duplicados y archivo corrupto.
- Mínimos mediante conexión simulada: selección del SP individual/lote, parámetros TVP resueltos en el servidor, auditoría, confirmación/reversión simuladas, cambio concurrente de revisión y fallo del SP. Las llamadas a los SP quedan registradas en memoria y nunca llegan a SQL Server.
- HTTP con controladores reales: entrada, sesión recuperada, permisos, variantes, detalles y descargas. El Excel de mínimos exportado se sube y valida nuevamente por HTTP. Se comprueban antiforgery, lectura sin acciones de escritura, JSON inválido y binding exacto del decimal máximo como texto; los guardados HTTP utilizan un servicio de escritura simulado.
- Excel de valorizados: formatos de ocho decimales para costos, filas filtradas completas e impresión. Un caso sintético adicional comprueba costo grande como texto exacto y costo ordinario como número. Excel de mínimos: seis encabezados originales y metadatos separados.
- Renderizado de las **47 vistas de listado**, vacías y con datos ficticios. Auditoría estructural: **1.277 comprobaciones**, 46 grillas y 55 modales en ambos estados, sin fallos. Los 109 métodos de escritura preexistentes comparados permanecen intactos.
- JavaScript externo con `allowJs/checkJs/noEmit` y nulabilidad: cero diagnósticos. 43 bloques de scripts de vistas sin errores de sintaxis. Sin eventos inline ni alert/confirm nativos en las pantallas nuevas.
- MG/TablaGeneral y exclusiones compartidas: 10 comprobaciones aprobadas. `git diff --check` sin errores. Arnés y registros temporales fuera del repositorio.
- Compilación .NET 10 correcta. Se utilizó `UseAppHost=false` en las últimas comprobaciones para evitar reemplazar el ejecutable abierto por el usuario; no se detuvo su proceso ni se modificó la configuración del proyecto. La recompilación mantiene únicamente CS8602 preexistente en FamiliaProducto:151.

## Límites de validación

Las consultas y los archivos se prueban con lecturas reales. Las escrituras de mínimos y su reversión se verifican con una conexión simulada que intercepta las llamadas a SP; no certifican los efectos de los procedimientos reales ni la concurrencia con otras aplicaciones. La ejecución real necesita una base de prueba y autorización explícita de escritura.

Queda pendiente la interacción visual en navegador, temas y pantallas pequeñas. El renderizado HTTP, la auditoría estructural y el chequeo de JavaScript no sustituyen esa revisión. No se ejecutaron DML, procedimientos de modificación, transacciones reales de escritura ni secuencias durante el lote. No se añadieron dependencias, commits ni cambios en MG/TablaGeneral.
