# Migración de la base de Inventario — 29/10/2026

Se incorporan `IV/Almacen` e `IV/Transaccion` desde el legacy de `ERP-Arco n5`, conservando el estilo compartido de Maestros. Los cambios anteriores del proyecto se mantienen sin commits ni limpiezas.

## Funcionalidad implementada

| Módulo | Alcance |
|---|---|
| IV/Almacen | Consulta paginada por empresa, búsqueda, sede, estado y orden por columnas. Alta, edición y baja lógica; dirección desglosada, ubigeo, indicador de ventas, código SUNAT y servidor de impresión. |
| Transacciones del almacén | Vincular, editar y retirar relaciones. Valida pertenencia y duplicados activos. |
| Configuración del almacén | Campos visibles/requeridos por transacción vinculada. Se muestran también los atributos que aún no tienen configuración guardada. |
| Series del almacén | Consulta, asignación individual y retiro de series de documentos de la empresa. Conserva el correlativo del documento. |
| IV/Transaccion | Consulta paginada, filtros y orden global. Alta con código automático, edición, compra/operación y baja lógica. Tipo de movimiento protegido después del alta. |

La cabecera y los tres detalles de Almacenes comparten un modal extendido, sin modales anidados. Las operaciones de detalle tienen botones propios y mantienen los datos de cabecera pendientes de guardar. Los usuarios con permiso de lectura pueden consultar y seleccionar la configuración de cada transacción sin habilitar guardados.

Las pantallas reutilizan los componentes de cantidades/paginación/orden, estilos y actualizaciones AJAX existentes. Los datos recibidos por JavaScript se incorporan con nodos y `textContent`. Las tablas de detalle incorporan orden local por sus columnas de consulta.

## Reglas y dependencias comprobadas

Fuentes legacy: controladores y vistas de `Areas/IV/{Almacen,Transaccion}`, sus business y repositorios de Almacen, TransaccionAlmacen, AlmacenTransaccion, AlmacenTransaccionConfiguracion y AlmacenSerie. La numeración procede de `TablaBusiness.ObtenerCorrelativo`.

- Empresa y usuario desde la sesión seleccionada, contrastada con la identidad autenticada; se exige período/sede activo.
- Permisos de lectura/escritura del menú legacy `iv`, con URLs `almacen/index` y `transaccion/index`. Se reutiliza la consulta de roles existente mediante `GetPermisoModuloAsync`; `GetPermisoModuloRHAsync` conserva su contrato y delega con el área `rh`.
- Antiforgery en todos los POST; validación de modelos y pertenencia en el servicio.
- Sede del almacén y tipo de movimiento de la transacción no cambian después del alta, como en las pantallas legacy.
- Alta con origen usuario `02`, auditoría y estado activo; bajas lógicas mediante SP existentes.
- Las modificaciones de un detalle bloquean y verifican su cabecera dentro de la transacción. Los identificadores de configuración se resuelven desde el vínculo y atributo autorizados.
- Numeración de transacciones: se consulta la configuración existente de tabla `0014` y se utilizan los procedimientos de `empresa_corre`. Se comprobó configuración por empresa, formato `000` y longitud de tres caracteres. Una configuración de otro alcance, un código ocupado o un correlativo agotado producen un error explícito; no se reconfiguran ni renumeran datos automáticamente.
- No se modifica la implementación, configuración ni entrada de menú de MG/TablaGeneral.

Tablas verificadas: `almacen`, `empresa_sede`, `empresa`, `tipo_via`, `tipo_zona`, `departamento`, `provincia`, `distrito`, `origen_registro`, `trans_alma`, `tipo_mov_alma`, `tipo_operacion`, `alm_transa`, `alm_trans_config`, `atrib_config`, `almacen_serie`, `doc_com_serie`, `tipo_doc_com`, `tabla` y `empresa_corre`.

Se inspeccionaron las firmas y definiciones de los SP de alta/actualización/baja de cabeceras y detalles, así como sus secuencias. La asignación individual de serie utiliza `sp_almacen_serie_insertar`; no cambia el correlativo de `doc_com_serie`.

## Archivos

- `Models/InventarioViewModels.cs`.
- `Services/IInventarioService.cs` e `InventarioService*.cs`: servicio Dapper dividido por cabeceras y detalles.
- `Areas/IV/Controllers`: contexto, Almacen y Transaccion.
- `Areas/IV/Views`: imports/layout de área y las dos vistas.
- `wwwroot/js/inventario.js`: formularios, cascada de ubigeo y detalles, con delegación para las filas que actualiza AJAX.
- `Program.cs`: registro del servicio; `_Layout.cshtml`: grupo de navegación Inventario.
- `IRHService/RHService`: reutilización de la comprobación de permisos con área parametrizada.

## Validación y estado

Implementación disponible para revisión; la clasificación sigue siendo **migrado parcialmente**, pues no se ejecutaron escrituras reales ni interacción visual en navegador.

- Compilación correcta; se mantiene el aviso nullable preexistente de FamiliaProducto.
- 609 comprobaciones agregadas de servicios y HTTP en arnés temporal: consultas reales, filtros, todos los órdenes, paginación, cabeceras/detalles por empresa, aislamiento y entradas inválidas.
- Renderizado de las 38 vistas actuales vacías y con datos ficticios. Pruebas adicionales con los controladores reales de Inventario: lectura sin botones de escritura, acceso denegado, token ausente, sesión inconsistente y período/sede inactivo.
- Envíos HTTP con servicios de escritura simulados: binding de cabeceras, campos opcionales vacíos, referencias nulas y acciones de detalles; comprobación del uso de empresa de sesión y usuario autenticado. No son pruebas de los procedimientos de escritura.
- JavaScript con `allowJs/checkJs`, nulabilidad y sin emisión: cero diagnósticos. 38 scripts de vistas sin errores de sintaxis.
- Revisión estructural: 1.051 comprobaciones sin fallos; 37 grillas y 46 modales en ambos estados de renderizado. Los 109 métodos de escritura preexistentes revisados permanecen sin cambios.
- Consultas de inspección y pruebas de datos limitadas a SELECT; sin transacciones de escritura, ejecuciones de SP de modificación ni consumo de secuencias.

Pendiente para estos módulos: validación autorizada de altas/ediciones/bajas y concurrencia contra una base de prueba; interacción visual en ambos temas y tamaños; selección múltiple de series si se requiere la comodidad del popup legacy. Las consultas de saldos, Kardex de unidades y reporte de movimientos se desarrollaron en el [lote siguiente](migracion-reportes-inventario-2026-10-29.md). El registro de movimientos y los demás procesos operativos de Inventario siguen pendientes.

## Corrección de recuperación de sesión

El filtro inicial de Inventario exigía `USER_SESSION` en memoria. Una cookie de autenticación todavía válida podía sobrevivir a la pérdida de esa sesión, especialmente al reiniciar la aplicación durante el desarrollo. En ese caso, la navegación devolvía JSON en lugar de mostrar el módulo. Las pruebas iniciales siempre creaban la sesión y no cubrían este escenario.

- En consultas GET con sesión ausente, Inventario recupera los identificadores exactos de empresa, sede y período desde la identidad autenticada. Para cookies anteriores sin `PeriodoId`, resuelve el año únicamente si corresponde a un período activo inequívoco.
- Verifica los identificadores devueltos por autenticación, la relación activa período/sede/empresa y los permisos del módulo antes de almacenar la sesión recuperada. No utiliza IDs predeterminados ni recupera contexto automáticamente durante un POST.
- Si el contexto no se puede recuperar, la entrada normal a la pantalla redirige a Perfil de acceso. AJAX y los POST conservan un error estructurado con enlace al perfil.
- Login incorpora el ID del período a los claims. Perfil de acceso valida la combinación elegida y actualiza tanto la sesión como la cookie, conservando roles y propiedades de autenticación.
- Perfil preserva los valores seleccionados, muestra las combinaciones inválidas y admite un destino local de regreso al módulo con sus filtros. Rechaza destinos externos.

Validación posterior: 646 comprobaciones totales, incluidas 37 nuevas sobre pérdida de sesión, cookies actuales/anteriores/renovadas, contexto ambiguo o inválido, permisos, AJAX, POST sin contexto, actualización del perfil y retorno seguro. Se utilizaron servicios de contexto simulados para estos casos HTTP; las lecturas de datos del inventario siguieron utilizando conexiones de solo lectura. No se probó la sesión del navegador del usuario ni se ejecutaron escrituras SQL.
