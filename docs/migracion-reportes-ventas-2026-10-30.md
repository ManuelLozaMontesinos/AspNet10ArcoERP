# Reportes de Ventas — 30/10/2026

Lote desarrollado en `AspNet10ArcoERP`, rama `master`, conservando el trabajo acumulado. Se contrastaron controladores/vistas de `ERP-Arco n5`, los RDL disponibles y las definiciones/esquema locales mediante SELECT. `MG/TablaGeneral` permanece excluido.

## Alcance disponible

| Ruta | Consulta |
|---|---|
| `VT/RepoDocuTemporal` | Documentos temporales emitidos, por mes, rango de fechas o todo el ejercicio seleccionado; importes en moneda local, estado y medio de pago. |
| `VT/RepoDocuReserva` | Productos/servicios y documentos pendientes vinculados con reservas; vendedor opcional y vistas por producto o documento. |
| `VT/RepoPedidoProd` | Líneas de pedidos por producto y mes contable, con cantidades, precios, moneda, lista, cliente, vendedor y estado. |

Las tres pantallas incluyen consulta, búsqueda, orden global por columna antes de paginar, tamaños 25/50/100/500, contador/rango y Opciones al final. Los filtros/orden/paginación usan AJAX, con el estilo de Maestros. La consulta ampliada del registro usa un modal único de lectura. Los decimales se envían al modal como cadenas para conservar su precisión.

Excel e impresión utilizan el resultado filtrado completo, con el orden seleccionado. La impresión abre una vista preparada para imprimir o guardar como PDF. La exportación rechaza resultados superiores a 50.000 filas con un mensaje para acotar filtros; no produce archivos truncados.

Estado: implementación disponible, pendiente de aceptación visual/funcional en navegador. Este lote incorpora consultas y exportaciones.

## Reglas verificadas

### Documentos temporales

- Fuente: `sp_rptvt_docTemporal`. Tipo de comprobante por código `4`, estados de documento `3`/`9` y `emitido='S'`, dentro del período/sede seleccionado.
- Se conserva el estado del registro: un registro anulado muestra `***ANULADO***` y sus importes son cero. Los activos usan los montos almacenados, convertidos con el T/C del propio documento cuando la moneda no es local.
- El total corresponde a todo el resultado filtrado, no solo a la página visible. Se identifica la moneda de origen y se rotulan los importes como locales.
- Los rangos incluyen el día final completo mediante un límite superior exclusivo del día siguiente. La consulta no depende de que la empresa tenga completos los datos de dirección/ubigeo utilizados por la antigua impresión.

### Documentos con reserva

- Fuente: `sp_rptvt_documentoSaldoReservado`. Se mantiene el **mes actual del servidor**, aplicado al ejercicio/sede del contexto. La cabecera distingue el ejercicio seleccionado del mes de referencia.
- Saldos de almacenes de venta positivos; pedidos en estados `02`/`04`, líneas activas y cantidad distinta de lo comprobado; comprobantes en estados `1`/`2` con líneas activas. Los pedidos de servicios se incorporan también por la rama específica del SELECT original.
- Se conservan `UNION` para el conjunto de documentos y `UNION ALL` para la rama de servicios, los signos de cantidades y las filas repetidas que produce la relación con saldos/almacenes.
- El saldo de referencia se repite por documento/almacén; se explica en pantalla y se evita presentarlo como una suma global. Las dos vistas cambian la disposición y orden inicial de columnas, manteniendo el orden global que elija el usuario.
- Se admiten vendedores históricos de la empresa para consultar sus documentos. Las referencias de clientes, vendedores, productos y almacenes se restringen al contexto/empresa correspondiente.
- Se detectó una espera de memoria `RESOURCE_SEMAPHORE` al probar distintos contextos. La consulta acota primero los saldos aplicables y sus productos; usa recompilación y límites locales de paralelismo/memoria, sin cambios de configuración ni índices. En seis ámbitos de prueba devolvió los resultados completos en aproximadamente 1,4–4,4 segundos. Es una medición local, no una garantía de tiempo para otros volúmenes/equipos.

### Pedidos por producto

- Fuente: `sp_rptvt_registroPedidoProducto`. Cabecera y líneas activas; exclusión de los identificadores de estado `1` y `5`, conforme a la definición local.
- Se corrigió el desfase de mes: el procedimiento antiguo convertía un identificador a código y luego comparaba ese código contra la FK del pedido. En la base inspeccionada enero tiene código `01` e ID `2`. La consulta moderna une `cotiza.mes_contable_id` con `mes_contable` y filtra por su código, sin confundirlo con el ID ni con el mes de la fecha.
- Cantidad, precio e importe proceden de la línea, sin recalcularlos. Se añade la moneda del pedido para interpretar los importes y se evita sumar monedas diferentes.

## Integración y archivos

- `Models/VentasReportesViewModels.cs` y `Services/VentasService.Reportes.cs`; ampliación de `IVentasService`, reutilizando el servicio registrado.
- Controlador base `VentaReporteController` y controladores/vistas `RepoDocuTemporal`, `RepoDocuReserva`, `RepoPedidoProd` en VT.
- Partial `_VentaReporte.cshtml` y `wwwroot/js/ventas-reportes.js`; navegación en Ventas. Los Tag Helpers de orden se registran dentro de esta nueva partial, sin cambiar los imports globales ni TablaGeneral.
- `Services/InformeExcel.cs` extrae el generador ya utilizado por Inventario; su controlador mantiene la firma anterior y delega en el helper común. Ventas reutiliza también el renderizador de impresión existente.
- Los libros conservan cabeceras, metadatos, filtros, fechas, números y panel inmovilizado. Las cantidades/importes que exceden la precisión numérica de Excel se preservan como texto exacto; los valores ordinarios siguen siendo numéricos. Se conserva el tratamiento previo de los costos de ocho decimales de Inventario.
- Permisos exactos `vt/repoDocuTemporal/index`, `vt/repoDocuReserva/index` y `vt/repoPedidoProd/index`, comprobados para consulta y exportaciones. La empresa/contexto proceden del servidor, con recuperación de sesión en GET y validación de la relación activa período/sede/empresa.

## Validación

Se amplió el arnés temporal externo con consultas reales y comprobaciones HTTP, incluyendo la regresión acumulada de los módulos anteriores. Las verificaciones cubren columnas en ambos sentidos, orden estable entre páginas, filtros, rangos, tamaños, contexto, permisos y exportaciones completas.

- Documentos temporales: muestra de **448 documentos**, cotejada con un conteo y total independiente aplicando la conversión local y los estados correspondientes.
- Pedidos: muestra con **26.184 líneas de origen**; conteo contrastado con joins y mes contable independientes.
- Reservas: muestra de **5 filas**; comparación de la proyección completa con el cuerpo SELECT del procedimiento local —sin ejecutarlo como procedimiento—, incluyendo documentos, cantidades, signos y duplicados. Filtro por vendedor contrastado sobre esos registros.
- Se comprueban JSON de detalle con decimales exactos, celdas Excel numéricas/textuales según precisión, impresión independiente y rechazo de exportaciones con filtros inválidos.
- Se renderizan las **55 vistas Index** revisadas, vacías y con datos ficticios, y se ejecutan las comprobaciones de protección de TablaGeneral y los métodos anteriores de escritura.

Resultados finales:

- **1.780 comprobaciones aprobadas** en el arnés, incluidas las regresiones anteriores y los nuevos reportes.
- Auditoría estructural: **1.441 comprobaciones**, 108 grids y 126 modales contando ambas variantes, sin fallos; 105 métodos previos de escritura comparados con la referencia temporal disponible. Protección de TablaGeneral: **10 comprobaciones aprobadas**.
- JavaScript externo: cero diagnósticos TypeScript. Scripts de vistas: 51 analizados, cero errores de sintaxis. Diff sin errores de espacios.
- `dotnet build --no-restore -p:UseAppHost=false`: compilación correcta, sin errores. La recompilación conserva la advertencia incidental CS8602 de `FamiliaProducto`; se mantiene sin cambios.

Los arneses y salidas permanecen en `%TEMP%/arco-planilla-validation` y `%TEMP%/arco-ui-ajustes`, fuera del repositorio. Registro completo del lote: `reportes-ventas-final.log`; mediciones de Reservas: `reservas-tiempos-optimizado.log`.

No se ejecutaron DML, procedimientos de escritura ni secuencias. La inspección y los reportes usan exclusivamente SELECT. Las comprobaciones estáticas y HTTP no sustituyen una revisión interactiva en navegador.

## Pendientes siguientes identificados

Continúan pendientes Lista de precios, Caja/Cobranza transaccional, comprobantes y los demás reportes de Ventas, además de Compras/Producción/Administración y brechas previas. En la revisión inicial de este lote se observó que `RepoUltimaVenta` referencia un RDL ausente en las fuentes inspeccionadas y no define el origen de su Excel; requiere localizar esa definición antes de replicar su cálculo. El Kardex valorizado mantiene su dependencia de un procedimiento ausente localmente.
