# Series de documentos y maestros de Ventas — 30/10/2026

Lote implementado en `AspNet10ArcoERP`, rama `master`, contrastado con las fuentes de `ERP-Arco n5` y con el esquema/procedimientos locales mediante SELECT. Se conservan los cambios acumulados y la exclusión de `MG/TablaGeneral`.

## Alcance disponible

| Ruta | Operaciones |
|---|---|
| `AS/DocuComSerie` | Consulta por sede/documento/estado, alta, edición de configuración y anulación lógica de series. |
| `VT/MedioPago` | Consulta, alta con código automático por empresa, edición y anulación lógica. |
| `VT/GrupoCliente` | Consulta, alta, edición y anulación lógica de grupos; consulta, incorporación y retiro lógico de terceros dentro del mismo modal. |

Las tres pantallas siguen Maestros: cabecera, filtros y buscador, orden global por columna antes de paginar, cantidades 25/50/100/500, total/rango y Opciones al final. Filtros, orden y paginación usan `erp-listings.js`. Los formularios comparten un modal extendido, controles Bootstrap con tokens de tema y SweetAlert2. La búsqueda, orden y paginación de integrantes/candidatos actualiza solo el detalle y conserva los datos de cabecera pendientes.

Estado: implementación disponible; pendientes la aceptación visual/funcional en navegador y la validación autorizada de escrituras reales. No se declara completada la migración de Administración o Ventas.

## Reglas legacy conservadas

### Series

- Sede y tipo de documento activos al crear; las series se restringen a sedes de la empresa seleccionada. Sede, tipo, serie y correlativo se protegen después del alta, en cliente y servidor.
- Las series electrónicas nuevas comienzan en cero. Las demás permiten un correlativo inicial no negativo. El mantenimiento no llama al procedimiento que consume numeración.
- `sp_doc_com_serie_actualizar` no recibe serie, sede, tipo ni correlativo. El máximo de ítems de impresión se conserva al editar y es cero al crear, conforme al formulario anterior.
- Los comprobantes requieren almacén de ventas. Las nuevas referencias deben estar activas y pertenecer a la sede. Se pueden conservar referencias históricas sin permitir cambiar a un almacén ajeno. Los tipos relacionados históricos se conservan si no cambian.
- Contingencia/envío en línea se traducen a S/N; se respetan formato, archivo y referencia a documento relacionado.
- La revisión incluye el correlativo, además del estado y fecha de modificación, para rechazar una edición si la numeración cambió mientras el formulario estaba abierto.

### Medios de pago

- Se utiliza la configuración existente de la tabla `0016`: código de dos caracteres y correlativo por empresa. Se validan agotamiento y colisiones, sin sustituir el correlativo por MAX+1 ni cambiar su configuración.
- Correlativo y alta se ejecutan dentro de una misma transacción. Código y empresa no se modifican al editar.
- Nombre de 100 caracteres, abreviatura de 5 y comisión no negativa con hasta cuatro decimales (`decimal(12,4)`). No se introduce un límite de 100 % ausente en la fuente.

### Grupos de clientes

- Se conservan `grupo_prove` y `grupo_p_item`, los procedimientos existentes y la opción de ignorar incremento por fracción, habilitada inicialmente como en el formulario legacy.
- El selector utiliza terceros de la empresa —clientes/proveedores— sin inventar una restricción por subtipo.
- La consulta incluye vínculos retirados. Se excluyen del selector los terceros que ya tienen cualquier vínculo previo con ese grupo, incluso inactivo. No se implementa una reactivación inexistente en la fuente.
- Retirar un integrante y anular un grupo son bajas lógicas separadas. Anular el grupo no elimina ni anula sus integrantes en cascada.

## Integración y permisos

Archivos principales: controladores/vistas `DocuComSerie` en AS y `MedioPago`/`GrupoCliente` en VT; `Models/VentasMaestrosViewModels.cs`; `Services/AdministracionService.Series.cs`, `IVentasService.cs`, `VentasService.cs` y `VentasService.Grupos.cs`; `Controllers/ModuloContextController.cs`; `Services/MaestroEmpresaSql.cs`; `Views/Shared/_MaestroEmpresaModal.cshtml`; `wwwroot/js/maestros-empresa.js`. Se ajustaron los contratos/registro, imports de AS/VT, navegación y los adaptadores internos de contexto/correlativos de Inventario.

- Se registró `IVentasService` y se incorporaron las entradas de menú en Administración y Ventas.
- `ModuloContextController` reutiliza la resolución de contexto, recuperación de sesión mediante cookie en GET, autorización por menú y antifalsificación que ya utilizaba Inventario. `InventarioContextController` conserva su contrato como adaptador para IV.
- Las nuevas rutas verifican exactamente `as/docuComSerie/index`, `vt/medioPago/index` y `vt/grupoCliente/index`. Los POST requieren escritura, token antifalsificación y el contexto capturado al abrir el formulario; no reconstruyen una sesión perdida durante una escritura.
- Las escrituras verifican empresa, estado y revisión dentro de transacciones serializables, con auditoría del usuario autenticado. Se usan exclusivamente los procedimientos existentes.
- Se extrajo el helper de correlativos de Transacciones de almacén, con una lista cerrada para `0014` y `0016`. No se modifican TablaGeneral ni las operaciones públicas anteriores de Inventario.

## Validación ejecutada

- **1.309 comprobaciones aprobadas** en el arnés temporal externo al repositorio, incluyendo la regresión anterior de RH/TS/AS/MK/IV y los nuevos módulos.
- Lecturas reales para los tres listados, todas sus columnas en ambos sentidos, tamaños, filtros, contexto, consultas individuales, integrantes y candidatos. La muestra de la empresa de prueba tenía 23 series, 4 medios de pago y 10 grupos.
- Render HTTP de las **50 vistas Index** incluidas en la revisión, con datos ficticios y vacías. Pruebas de los controladores nuevos con permisos de lectura/escritura/denegación, recuperación de contexto, DTO anidados, decimales, casillas desmarcadas, antifalsificación y rechazo de cancelaciones mediante GET.
- Altas, ediciones, bajas y vínculos con llamadas a procedimientos **interceptadas y simuladas**: parámetros, auditoría, orden de llamadas, preservación de numeración, colisiones/agotamiento, reversión ante fallo y rechazo de revisiones desactualizadas. Se verificó también que el helper compartido conserva el código `013` para la configuración `0014`.
- Auditoría estructural: **1.339 comprobaciones**, 98 grids y 116 modales renderizados contando ambas variantes; 109 métodos previos de escritura conservados. Exclusión de TablaGeneral: **10 comprobaciones aprobadas**.
- JavaScript externo: cero diagnósticos en la comprobación TypeScript. Scripts de las vistas: 46 analizados, cero errores de sintaxis. Revisión de espacios del diff sin errores.
- Compilación `dotnet build --no-restore -p:UseAppHost=false` correcta. Se conserva la advertencia previa CS8602 de `FamiliaProducto`; no se modifica el proceso de la aplicación que está abierto en el equipo.

No se ejecutaron DML, procedimientos de escritura, secuencias ni transacciones reales en SQL Server. El adaptador de grabación deja pasar solo SELECT y simula los procedimientos y el commit/rollback. Las verificaciones estáticas y HTTP no sustituyen la revisión interactiva en navegador. Los arneses y sus resultados están en `%TEMP%/arco-planilla-validation` y `%TEMP%/arco-ui-ajustes`, fuera del proyecto.

Continúan pendientes los demás módulos de Ventas/Administración y el Kardex valorizado de Inventario, cuyo procedimiento `sp_rptiv_kardexValorizado` no estaba disponible en la base local inspeccionada.
