# Cajas y Vendedores — 30/10/2026

Lote implementado en `AspNet10ArcoERP`, rama `master`, sobre el trabajo acumulado. Fuentes contrastadas con `ERP-Arco n5`; esquema, configuración de correlativos y procedimientos locales inspeccionados mediante SELECT. `MG/TablaGeneral` continúa excluido e intacto.

## Alcance disponible

| Ruta | Funciones |
|---|---|
| `VT/Caja` | Consulta de cajas de la sede seleccionada, alta, edición y anulación lógica; consulta, incorporación y retiro de personal dentro del mismo modal. |
| `VT/Vendedor` | Consulta por empresa, alta desde personal, edición, anulación y reactivación; asignación de series y listas de precios por lote, retiro y reactivación de vínculos. |

Las pantallas siguen Maestros: cabecera y acciones, buscador y estado, orden global por columna antes de paginar, tamaños 25/50/100/500, total/rango y Opciones al final. Los listados principales usan AJAX; las asignaciones se actualizan dentro del modal sin reemplazar la cabecera ni perder sus datos pendientes. Se utilizan tokens de tema, Bootstrap 5 y SweetAlert2, sin eventos inline ni ventanas anidadas.

Los selectores incluyen filtros combinables por sede, tipo de documento para series y moneda para listas de precios, además de búsqueda/orden/paginación. La selección de series/listas admite hasta 500 registros y se conserva entre páginas y filtros, con contador y limpieza explícita. Cambiar de tipo de asignación limpia esa selección.

Estado: implementación disponible, pendiente de aceptación visual/funcional en navegador y validación autorizada de escrituras reales. `CajaCobranza` y el mantenimiento completo de `ListaPrecio` continúan pendientes como módulos independientes.

## Reglas conservadas y ajustes de compatibilidad

### Cajas

- La sede procede del contexto validado en servidor. El alta ignora cualquier sede recibida en el formulario; la consulta y cada operación comprueban empresa y sede.
- `sp_caja_insertar`, `sp_caja_actualizar` y `sp_caja_eliminar` conservan la auditoría y baja lógica. La anulación de cabecera conserva los vínculos de personal, como en la fuente.
- Se selecciona personal activo de la empresa, validando también la pertenencia de su tercero. Se impiden asignaciones activas duplicadas.
- Los vínculos retirados permanecen consultables. Volver a asignar personal retirado crea un vínculo mediante `sp_caja_perso_insertar`; su retiro usa `sp_caja_perso_eliminar`. Se mantiene esa distinción frente a la reactivación explícita de vínculos de Vendedores.

### Vendedores

- La persona procede del selector de Personal y se almacena como `proveedor_id`, como en el legacy. Su empresa se verifica en el tercero y en Personal. La persona y el código se conservan después del alta.
- La generación del código utiliza la configuración existente `0013`: correlativo por empresa, formato `0000000`, sin prefijo. Correlativo y alta comparten transacción, con validaciones de agotamiento y colisión y sin modificar la configuración de TablaGeneral.
- Se respeta el tamaño real de `vendedor.abrev` y sus SP: **30 caracteres**, aunque la vista antigua permitía 50. El servidor de impresión conserva mayúsculas/minúsculas y admite 50 caracteres.
- `sp_vendedor_actualizar` permite reactivar: se recuperan y conservan nombre corto/servidor desde la base y se registra el usuario actual. Anular/activar cabecera conserva el estado de sus vínculos.

### Series y listas del vendedor

- Series de sedes de la empresa, con sede/almacén/documento/serie/correlativo; listas con sede/nombre/moneda. Se conservan los vínculos históricos y sus estados.
- Las altas por lote utilizan exactamente `sp_vend_serie_actualizarListaDocumentoComercialSerieId` y `sp_lista_p_vendedor_actualizarListaPrecioVendedorId`, con el TVP `dbo.ListInt`. El alta individual de lista-vendedor no estaba disponible localmente; se utiliza el procedimiento por lote existente.
- Se valida el lote completo antes de llamar al procedimiento: identificadores positivos, diferentes, máximo 500, referencias activas y de la empresa, sin vínculos previos. Una referencia inválida rechaza todo el lote.
- Los vínculos anteriores, incluidos los anulados, se excluyen del selector. Para activarlos se utilizan los procedimientos de actualización existentes, verificando estado/revisión, destino habilitado y ausencia de otra asignación activa.
- Las bajas usan `sp_vend_serie_eliminar` y `sp_lista_p_vendedor_eliminar`; la reactivación usa sus respectivos procedimientos de actualización. La asignación de series conserva la numeración del documento.

## Archivos e integración

- Nuevos `Models/VentasOperativasViewModels.cs`, `Services/VentasService.Operativas.cs` y `Services/VentasService.Asignaciones.cs`; ampliación del contrato `IVentasService`, reutilizando el registro existente.
- Controladores `Caja`, `Vendedor` y base `VentaAsignaciones` en `Areas/VT/Controllers`; vistas Index de ambos módulos; `_VentaOperativaModal.cshtml` y `wwwroot/js/ventas-operativas.js`.
- Nuevas entradas en Ventas. `ModuloContextController` expone la sede validada para evitar tomarla de datos enviados por el cliente.
- El helper cerrado de correlativos admite ahora `0013`, además de `0014` y `0016`; verifica los códigos del vendedor a través del tercero y su empresa.
- Permisos exactos `vt/caja/index` y `vt/vendedor/index`, antifalsificación en POST, contexto capturado al abrir el formulario y rechazo de operaciones ajenas al módulo. La recuperación de sesión se conserva exclusivamente en GET.
- Las escrituras comprueban propiedad, estado y revisión dentro de transacciones serializables. Las confirmaciones de cabecera y detalle se realizan mediante POST.

## Validación ejecutada

- **1.558 comprobaciones aprobadas** en el arnés temporal externo, incluyendo la regresión de RH/TS/AS/MK/IV y los módulos de Ventas del lote anterior.
- Consultas reales de cajas, vendedores, personal y las tres clases de asignación/candidatos, todas sus columnas en ambos sentidos, paginación, filtros combinados, conteos independientes, estados, búsquedas vacías y aislamiento. Muestra local: 4 cajas de la sede, 70 vendedores y 352 personas elegibles.
- Pruebas HTTP de controladores reales con permisos de lectura/escritura/denegación, recuperación de contexto, DTO anidados, lotes, contexto obsoleto, antifalsificación y rechazo de mutaciones mediante GET. Render de las **52 vistas Index** revisadas, tanto vacías como con datos ficticios.
- Escrituras **simuladas e interceptadas**: parámetros/auditoría, sede tomada del servidor, código `0000013`, alta con correlativo en una sola transacción, preservación de identidad, agotamiento/colisiones, bajas sin cascada, TVP de series/listas, reactivación y rechazo de revisiones concurrentes. Se verificó la reactivación de series con un estado inactivo simulado sobre una referencia existente.
- Se comprobó que un lote mixto válido/ajeno se rechaza antes de ejecutar el SP y que un fallo simulado del procedimiento revierte la operación. Las regresiones de correlativos `0014` y `0016` siguen pasando.
- Auditoría estructural: **1.373 comprobaciones**, 102 grids y 120 modales contando ambas variantes; 109 métodos previos de escritura conservados. Protección de TablaGeneral: **10 comprobaciones aprobadas**.
- JavaScript: cero diagnósticos TypeScript; 48 scripts de vistas analizados, cero errores de sintaxis. Diff sin errores de espacios.
- `dotnet build --no-restore -p:UseAppHost=false`: compilación correcta. Se conserva únicamente la advertencia previa CS8602 de `FamiliaProducto`; no se modifican SDK, dependencias ni el proceso de aplicación abierto por el usuario.

No se ejecutaron DML, secuencias, procedimientos de escritura ni transacciones reales en SQL Server. Las conexiones de validación dejan pasar solo SELECT; el adaptador de grabación simula SP, commit y rollback. Las comprobaciones estáticas/HTTP no sustituyen la revisión interactiva en navegador ni las pruebas autorizadas de escritura.

Arnés y resultados: `%TEMP%/arco-planilla-validation` y `%TEMP%/arco-ui-ajustes`, fuera del repositorio. Continúan pendientes el resto de módulos de Ventas/Compras/Producción/Administración y las brechas anteriores documentadas; el Kardex valorizado sigue dependiendo de un procedimiento ausente en la base local inspeccionada.
