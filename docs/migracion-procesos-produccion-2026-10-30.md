# Procesos de producción — 30/10/2026

## Funciones incorporadas

Se implementó `PD/ProceProduccion` con listado, alta, edición y anulación de procesos, alta/cambio de nombre de subprocesos, consulta de operadores y selección/asignación/retiro de personal. La cabecera y los detalles permanecen en un único modal extendido.

El listado principal sigue la base de Maestros: cabecera, buscador, estado, orden por columnas, 25/50/100/500 registros, contador/rango al pie y Opciones al final. Los filtros y la paginación utilizan el helper AJAX existente. Las tablas de subprocesos, operadores y candidatos incorporan búsqueda, orden global, tamaños de página y contador dentro del mismo modal, sin modales anidados.

La cabecera y el pie del modal son fijos; los colores utilizan tokens semánticos. Las confirmaciones utilizan SweetAlert2. Al guardar un subproceso se conserva el borrador no guardado del nombre de la cabecera, sin actualizar silenciosamente su revisión. Se impiden envíos repetidos durante el guardado.

## Compatibilidad comprobada

- Fuentes: controlador y vistas `PD/ProceProduccion`, repositorios/business de ProcesoProduccion, SubProcesoProduccion y SubProcesoProduccionPersonal; tablas y definiciones locales `proce_prod`, `sub_proce_p`, `sub_p_p_perso` y sus procedimientos.
- Procesos y subprocesos son **catálogos compartidos**: no tienen columna de empresa. Se conserva ese alcance, se identifica en la pantalla y se exige contexto activo y permiso del módulo. La anulación del proceso usa únicamente `sp_proce_prod_eliminar`; no anula subprocesos ni asignaciones en cascada.
- Las asignaciones de operadores se restringen a personal de la empresa actual, comprobando empresa de la sede y del proveedor asociado. Las asignaciones existentes admiten personal histórico; las nuevas requieren persona, sede y proveedor activos.
- Se conservan los procesos/subprocesos anulados para consulta, sin habilitar reactivaciones que los procedimientos no realizan. No se mueve un subproceso entre procesos.
- La UI anterior no conecta una operación de anulación de subprocesos en su manejador, aunque existe un procedimiento de eliminación. Se migran las altas y cambios de nombre que están conectados. Tampoco se ofrece una edición de asignación cuyo procedimiento únicamente actualiza auditoría.
- El retiro de operadores usa el procedimiento de anulación de la asignación. Una persona retirada puede volver a agregarse como nueva asignación; se rechazan duplicados activos.
- Se conservan secuencias y auditoría mediante los procedimientos existentes; no se generan identificadores con `MAX+1`. Los nombres admiten hasta 150 caracteres conforme a tablas y procedimientos.
- Los guardados validan contexto, jerarquía, estado y revisión; vuelven a comprobarlos bajo transacción serializable. Las revisiones incluyen nombre/relaciones además de estado y fecha, para detectar cambios concurrentes.

## Integración

- `Models/ProduccionViewModels.cs`, `IProduccionService` y `ProduccionService`, registrado en `Program.cs`.
- Controlador `Areas/PD/Controllers/ProceProduccionController.cs`, vista e imports/layout del área PD, y script `wwwroot/js/produccion.js`.
- Se reutilizan `ModuloContextController`, `ConsultaSedeSql`, modelos de revisión, componentes de listado y comportamiento AJAX existentes.
- Permiso exacto `pd/proceProduccion/index`; consulta separada de escritura, antiforgery en POST y rechazo del contexto cambiado. Los operadores se seleccionan dentro del mismo modal, con datos limitados a la empresa actual.
- Nueva sección Producción en la navegación, con Procesos de producción. TablaGeneral y su navegación permanecen intactos.

Este lote corresponde al maestro de procesos/subprocesos y operadores. Rutas, órdenes y operaciones transaccionales de producción continúan pendientes.

## Verificación

Se amplió el arnés temporal externo. Las conexiones reales rechazan DML, procedimientos y transacciones; las escrituras, secuencias y commit/rollback se simulan y registran sin ejecutarlos en SQL Server.

- Muestra contrastada: **5 procesos, 11 subprocesos y 178 asignaciones activas de operadores** de la empresa revisada.
- **117 comprobaciones del servicio aprobadas**: consultas y jerarquía, columnas en ambos sentidos, continuidad entre páginas, tamaños, candidatos, aislamiento de personal, llamadas/argumentos de escritura, revisión obsoleta, cambios bajo bloqueo y rollback simulado.
- **49 comprobaciones HTTP aprobadas** en un host aislado con el código real de las vistas/controladores: permisos de consulta/escritura, menús exactos, recuperación/conflicto de sesión, filtros, detalle, binding, antiforgery, contexto cambiado y las cinco acciones de escritura simuladas. Incluyen la regresión aislada de las pantallas de Almacenes y Transacciones. Total del lote: **166 comprobaciones específicas aprobadas**.
- Se corrigió el contrato del formulario para permitir una revisión ausente al crear procesos/subprocesos. MVC convierte el valor vacío a `null`; las ediciones siguen exigiendo una revisión válida y coincidente. Se verificaron ambos tipos de alta por HTTP.
- Renderizado de las **60 vistas Index** revisadas, vacías y con datos ficticios. Auditoría estructural: **1.587 comprobaciones**, 118 grids, 136 modales y 782 cabeceras de datos, sin fallos; 105 métodos anteriores de escritura comparados con la referencia temporal disponible.
- JavaScript: cero diagnósticos TypeScript. Scripts de vistas: 56 analizados, cero errores de sintaxis. Protección de TablaGeneral: 10 comprobaciones aprobadas.
- `dotnet build --no-restore -p:UseAppHost=false`: compilación correcta, cero errores y la advertencia CS8602 incidental preexistente en `FamiliaProducto`, sin cambios.

**Limitación de la regresión global:** no terminó completa en este lote. La primera ejecución llegó al renderizado general y falló en una comprobación HTTP de Almacenes; al aislarla, Almacenes y Transacciones respondieron HTTP 200 con los controles de escritura ocultos. La segunda ejecución se interrumpió antes del nuevo módulo con una `SqlException` en la consulta de Documentos temporales (`VentasService.Reportes.cs:63`): «Se produjo un error grave en el comando actual. Se deberían descartar los resultados, en el caso de haberlos». No se determinó su causa ni se modificó ese reporte. Las consultas de diagnóstico fueron exclusivamente SELECT; las bases consultadas aparecían ONLINE. No se presenta la regresión acumulada como aprobada.

Registros externos finales: `%TEMP%/arco-planilla-validation/produccion-servicio-final.log`, `produccion-http-completo.log`, `produccion-regresion-final.log`, `produccion-regresion-verificada.log` y `%TEMP%/arco-ui-ajustes/produccion-*`.

No se ejecutaron escrituras reales ni secuencias en la base. No se cambiaron dependencias, SDK o configuración SQL ni se detuvo la aplicación del usuario. Quedan pendientes la revisión visual interactiva y la ejecución real de los guardados.
