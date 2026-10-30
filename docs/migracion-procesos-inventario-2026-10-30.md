# Procesos mensuales de Inventario y consulta de llegadas — 30/10/2026

Nuevo lote en `AspNet10ArcoERP` (`master`), contrastado con `ERP-Arco n5` (`main`). Se preservan los cambios anteriores y la exclusión de MG/TablaGeneral.

## Módulos incorporados

| Módulo | Alcance |
|---|---|
| `IV/ApeCierre` | Consulta de meses por almacén de la sede de trabajo; filtros por mes/estado, fecha de cierre e indicador de movimientos posteriores. Preparación y confirmación de apertura o cierre mensual. |
| `IV/Reproceso` | Misma consulta mensual; preparación y confirmación de reproceso mensual y de enero hasta el mes actual. |
| `IV/ConsultaLlegada` | Entregas de compras aprobadas, en estado orden de compra/ingreso parcial y con productos pendientes. Filtros de producto/SKU, proveedor y día programado. Detalle de cantidades de la entrega y del producto en la orden. |

Los tres módulos tienen navegación, controladores, modelos, servicio Dapper y vistas. Reutilizan cabecera, filtros, grillas, orden global, cantidades 25/50/100/500, rango visible, pie de página y AJAX de Maestros. Los detalles y la preparación utilizan un modal extendido con cabecera/pie fijos. No se incorporan librerías ni dependencias.

Estado: **implementación disponible, pendiente de validación de escrituras reales y revisión visual/funcional con usuarios**. Consulta de llegadas es una consulta en el legacy: no se inventan altas, recepciones o cambios de entrega desde esa pantalla.

## Reglas verificadas en el legacy y SQL

Fuentes inspeccionadas: controladores/vistas `MovAlmacen`, `TransfeSalida`, `TransfeIngreso`, `TransfeModifica`, `ApeCierre`, `Reproceso` y `ConsultaLlegada`; business de movimiento, producto de movimiento y cierre; repositorios de cierre, saldo y entregas de compra; constantes de estados. La complejidad y dependencias de movimientos/transferencias se mantienen como un lote operativo posterior.

Se verificaron por SELECT los esquemas de `mov_alm_cierre`, `mes_contable`, `estado_ape_cie`, `orden_co`, `orden_c_prod`, `orden_c_ent`, `estado_orden_co` y `estado_aproba`; también las definiciones de los SP existentes, sin ejecutarlos.

- **Apertura:** cambia al estado `A`, limpia la fecha de cierre y el indicador posterior mediante `sp_mov_alm_cierre_actualizar`. No reprocesa saldos. Utiliza el usuario autenticado para auditoría.
- **Cierre:** reprocesa el mes mediante `sp_mov_alm_cierre_reprocesar`, después establece el estado `C`, la fecha actual y el indicador posterior `N`, dentro de una transacción.
- **Reproceso mensual:** ejecuta el reproceso del mes; para almacenes de ventas ejecuta además `sp_mov_alm_saldo_reprocesarCantidadReserva`. La segunda operación tiene alcance de sede/mes, como el business anterior, y se explica en la preparación.
- **Reproceso anual:** aplica el reproceso de enero al mes actual en orden; conserva estados de apertura/cierre y no agrega el reproceso de reservas del flujo mensual. Resuelve los IDs desde los códigos contables; evita asumir que el ID del mes coincide con su número calendario.
- La consulta muestra los meses contables especiales 00 y 13 y las filas de configuración existentes. No crea configuraciones mensuales faltantes ni inventa reaperturas/cierres en cascada.
- **Llegadas:** conserva el ámbito empresa/sede de la consulta legacy, que abarca sus distintos períodos contables. No agrega un filtro del período seleccionado. Exige entrega activa, aprobación `A`, estado de orden `2`/`3` y cantidad ordenada mayor a ingresada. No agrega restricciones de estado de cabecera/producto ausentes en el SP original.
- La cantidad pendiente corresponde a la línea del producto en la orden; no se calcula restando ingresos a cada entrega programada. El modal distingue esas cantidades.
- La restricción del rol `00004` oculta el proveedor tanto en la interfaz como en los datos del servidor, y descarta su filtro/orden. Las consultas SQL emplean parámetros; las columnas de orden son listas permitidas con desempate estable.
- Fecha programada opcional, incluyendo el estado «Sin fecha». Un filtro por día comprende todas sus horas con un intervalo cerrado/abierto.

## Límite comprobado del mes 13

`sp_mov_alm_cierre_reprocesar` construye la fecha inicial concatenando ejercicio, mes y `01`. Sólo sustituye 00 por 01; para el mes 13 genera una fecha inválida. No se modifica ese SP ni se inventa un algoritmo de cierre anual.

La grilla conserva el mes 13 y su detalle, pero deshabilita cierre/reproceso para ese mes y lo explica. La validación del servidor también lo rechaza antes de ejecutar procedimientos. Apertura conserva el cambio de estado disponible en el legacy. El reproceso anual incluye solamente meses ordinarios hasta el actual.

## Contexto, permisos y concurrencia

- Los tres módulos reutilizan `InventarioContextController`, sus permisos `iv` y la recuperación validada de sesión. URLs legacy: `apeCierre/index`, `reproceso/index`, `consultaLlegada/index`.
- Todas las acciones de proceso son POST y exigen antiforgery y permiso de escritura; GET no ejecuta procesos. La preparación verifica las operaciones permitidas para cada menú.
- El almacén debe estar activo y pertenecer a la sede del contexto validado, incluso para administradores. Se verifica período/sede/empresa activo. Los filtros no sustituyen la empresa, usuario o período/sede de sesión.
- La preparación enumera almacén, sede, ejercicio y meses, y proporciona una revisión del estado. La solicitud se vincula al ID de contexto y a esa revisión; el servidor vuelve a comprobarlos.
- La ejecución revalida y bloquea el contexto, almacén y filas de cierre dentro de una transacción serializable. Una revisión modificada o un cambio de mes/contexto exige nueva preparación. Los fallos de un SP impiden confirmar la operación completa.
- El helper de lectura del contexto se comparte con los reportes de Inventario previos, conservando su consulta y reglas.

## Archivos principales

- `Models/InventarioProcesosViewModels.cs`.
- Ampliación de `IInventarioService`; nuevos `InventarioService.Contexto.cs`, `.Procesos.cs` y `.Llegadas.cs`. `Reportes.cs` reutiliza el helper de contexto.
- Controlador base de procesos, `ApeCierreController`, `ReprocesoController` y `ConsultaLlegadaController`.
- Vistas de los tres módulos y parciales `_ProcesoListado`, `_ProcesoModal`, `_ProcesoScript`.
- `wwwroot/js/inventario-procesos.js` y tres entradas en el grupo Inventario de `_Layout.cshtml`.

## Validación realizada

- `dotnet build --no-restore`: correcto, cero errores. El incremental final no emitió advertencias; la recompilación inicial conserva únicamente CS8602 preexistente en FamiliaProducto:151.
- **929 comprobaciones agregadas de servicios y HTTP**, 122 adicionales respecto al lote de reportes. Las lecturas usan SQL real con conexiones que rechazan DML, procedimientos, transacciones y secuencias.
- Consultas nuevas: todos los órdenes ascendentes/descendentes, filtros, meses especiales, pertenencia del almacén, ámbito de empresa/sede, preparación mensual/anual, estado inválido y mes 13. Llegadas verifica productos pendientes, búsqueda, fecha y aislamiento.
- Ejecución simulada del servicio real mediante una conexión que sólo deja llegar SELECT a SQL Server: comprueba el orden y parámetros de las llamadas a los tres SP, auditoría/fecha, meses anuales, confirmación de la transacción simulada y uso de bloqueos. Las llamadas a SP se registran en memoria y nunca se envían al servidor; los bloqueos también se retiran de esas lecturas de prueba.
- Casos simulados de cambio de revisión y fallo del reproceso: cero confirmaciones; reversión registrada y ausencia de actualización de cierre posterior al fallo. Estos casos prueban la coordinación del servicio, no los efectos de los procedimientos reales.
- Restricción de proveedor comprobada con resultado de rol simulado y datos reales: excluye el nombre del resultado, ignora su filtro y normaliza el orden. Las consultas ordinarias también utilizan la lectura real de roles.
- HTTP: controladores reales de los tres módulos, selección/filtros, permisos de lectura, sesión recuperada, preparación, referencias ajenas y operaciones no admitidas por el menú. Los POST usan servicio de ejecución simulado: binding, contexto/usuario, antiforgery, modelos inválidos, lectura sin escritura y rechazo de GET sobre acciones de proceso.
- Renderizado de las **44 vistas de listado**, vacías y con datos ficticios. Auditoría estructural: **1.201 comprobaciones**, 43 grillas y 52 modales en ambos estados, sin fallos. Los 109 métodos de escritura preexistentes comparados permanecen sin cambios.
- JavaScript externo con TypeScript `allowJs/checkJs/noEmit` y nulabilidad: cero diagnósticos. 42 bloques de scripts de vistas sin errores de sintaxis. Sin eventos inline, alert/confirm nativos ni inserción de datos mediante `innerHTML` en las pantallas nuevas.
- MG/TablaGeneral y sus exclusiones compartidas: 10 comprobaciones aprobadas. `git diff --check` sin errores. Arnés, registros y herramientas de validación permanecen fuera del repositorio.

## Pendientes y límites de validación

- Validar ejecución real, reversión y concurrencia de los procedimientos contra una base de prueba con autorización explícita de escritura. Las pruebas de intención y transacción simuladas no certifican sus efectos reales ni su convivencia con otras aplicaciones que escriban inventario.
- Revisar interacción visual, temas claro/oscuro y pantallas pequeñas en navegador. El renderizado HTTP y la auditoría estructural no son una prueba visual interactiva.
- Registro de movimientos, productos, órdenes vinculadas, transferencias y recepción; reservas, valorización y cierre anual especial continúan pendientes. No se declara migrado todo Inventario.
- No se ejecutaron escrituras SQL, SP de modificación, transacciones reales de escritura ni secuencias en la base durante este lote.
