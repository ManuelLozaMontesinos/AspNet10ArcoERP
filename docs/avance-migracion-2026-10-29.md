# Avance de migración — 29/10/2026

Este lote continúa el inventario del 28/10/2026 y aplica el patrón de UI indicado por el usuario. Rama master; worktree limpio al iniciar. Cambios sin commit.

## Módulos atendidos

| Módulo | Desarrollo funcional | UI del listado |
|---|---|---|
| RH/Planilla | Agregar conceptos, editar secuencia, retirar mediante baja lógica, validar pertenencia/duplicados y auditoría. Cabecera y conceptos en un único modal. Alta/edición/anulación de cabecera mediante SP legacy y empresa de sesión. | Filtros por texto/estado, orden global por columnas, paginación real, opciones al final y contadores. |
| RH/PlanillaProceso | Conserva el lote anterior; comparte validación de sesión/permisos con Planilla. El motor de cálculo sigue pendiente. | Orden global por ID, planilla, mes, número, fechas, moneda, estado, trabajadores y neto. Nuevo patrón común. |
| TS/CuentaBanco | Sustituye TOP y orden local por consulta paginada y ordenada. Búsqueda también por CCI y moneda. | Mismos controles, filtro banco/estado, todas las columnas de datos ordenables. |
| AS/Periodo | Recupera la regla legacy que impide crear otro período batch habilitado cuando ya existe uno. | Orden por ID/año/batch/estado, controles comunes y componentes MVC/layout del área restaurados. |

El estado continúa siendo **migrado parcialmente**: las consultas y el renderizado están comprobados; no se validaron escrituras reales ni interacción visual en navegador.

## Patrón visual para los siguientes lotes

Referencia inspeccionada: MG/Proveedor, Producto y CuentaBanco existentes.

- Cabecera con icono, título y subtítulo; acción de creación alineada a la derecha.
- app-content-header py-3 border-bottom; app-content py-4 y container-fluid.
- Tarjeta compacta de filtros: búsqueda ancha e icono a la izquierda, filtros relacionados y botón Buscar/Limpiar. Total y dropdown «Mostrando» a la derecha.
- Cantidades exclusivamente 25/50/100/500; cambiar cantidad, orden o filtros vuelve a página 1.
- Cabeceras compactas, altura 40px, enlaces con indicador del orden actual y aria-sort. ORDER BY se aplica en SQL antes de OFFSET/FETCH, con clave de desempate estable.
- Tabla striped/hover/align-middle, encabezado fijo al desplazarse y columna «Opciones» al final con botones e iconos contextuales.
- Pie: «Viendo X–Y de N registros», número de página y Anterior/Siguiente. N es el total filtrado; X–Y corresponde a las filas de la página, no al límite TOP.
- Los enlaces preservan filtros, orden y tamaño. Valores de orden y dirección se validan contra listas permitidas; no se interpolan columnas recibidas del usuario.
- Colores semánticos Bootstrap para claro/oscuro. CSS limitado al contenedor erp-listing, cargado sólo en las pantallas atendidas.
- Modal único xl, centrado y desplazable; formulario directamente bajo modal-dialog, cabecera/pie sticky y SweetAlert2.
- Planilla guarda la cabecera y cada operación de concepto con su botón dentro del mismo modal. No hay modales de detalle anidados.

## Componentes y archivos

- Models/ListadoViewModels.cs: normalización de páginas/orden y cálculo del rango visible.
- TagHelpers/ListadoOrdenTagHelper.cs: enlaces de orden y conservación de filtros.
- Views/Shared/_ListadoControles.cshtml y _ListadoPaginacion.cshtml.
- wwwroot/css/listados.css, limitado a erp-listing.
- RH: PlanillaController, PlanillaProcesoController, nuevo RHContextController; vistas Planilla/PlanillaProceso; RHViewModels; IRHService/RHService; nuevo wwwroot/js/planilla.js.
- TS: CuentaBancoController/vista; CuentaBancoPageModel, IMaestrosService/MaestrosService únicamente en la región de cuentas bancarias.
- AS: PeriodoController/vista, modelos y servicio/contrato; nuevos Views/_ViewImports.cshtml y _ViewStart.cshtml.
- No fue necesario cambiar dependencias, registro DI ni navegación.

## Paridad legacy y SQL

Fuentes de Planilla: ESD.ERP.Web.v6/Areas/RH/Controllers/PlanillaController.cs y Views/Planilla/CreateEdit.cshtml; Business/Impl/PlanillaConceptoRemunerativoBusiness.cs y Repository/Impl/PlanillaConceptoRemunerativoRepository.cs.

Esquema comprobado con SELECT/metadatos:

- planilla, plan_con_rem, concep_remu, tipo_con_remu, tipo_trabaja.
- sp_planilla_insertar/actualizar/eliminar y planilla_seq.
- sp_plan_con_rem_insertar/actualizar/eliminar y plan_con_rem_seq.
- empresa_cta, banco y moneda; período y estados ya comprobados.

El SP actualizar de concepto sólo modifica orden y auditoría: la identidad del concepto se protege tanto en UI como en servidor. El SP eliminar local exige usuarioAccionId, aunque el repositorio antiguo envía estado. Se sigue la firma SQL real. La baja es lógica I; no se añade reactivación.

Se permite la secuencia 0, utilizada al agregar un concepto en el legacy, y el rango SMALLINT de la tabla. No se inventa unicidad de secuencia; sí se impide repetir un concepto activo, como hace la pantalla legacy. La pertenencia y el estado activo de la planilla se validan dentro de la transacción; el bloqueo de la cabecera coordina las modificaciones de este módulo.

La exclusividad batch de Períodos se recupera en **alta**, dentro de la transacción serializable existente y antes de consumir secuencia. Corresponde a PeriodoBusiness.Insert. No se redefine la edición batch fuera del comportamiento existente.

## Comprobaciones

- 155 comprobaciones aprobadas con proyecto temporal fuera del repositorio que referencia el ensamblado ERP real.
- Fábrica de conexiones de prueba que rechaza DML, SP y transacciones: todas las consultas de inspección fueron SELECT. No se ejecutaron secuencias ni procedimientos de escritura.
- Consultas reales: todos los campos de orden de los cuatro módulos, ascendente/descendente, filtros, esquema de conceptos, aislamiento, normalización, entradas negativas y permisos del contexto RH.
- El conjunto local tiene pocas filas: la prueba de orden entre páginas con datos reales mayores de 25 es condicional. El rango visual intermedio se probó con 73 registros ficticios y 25 filas de página 2.
- Renderizado HTTP 200 de las cuatro vistas con datos ficticios: layout AdminLTE/Bootstrap, componentes de orden, paginador, tamaño 25/50/100/500, rango 26–50, enlaces conservando filtros y ausencia de atributos de evento inline.
- TypeScript allowJs/checkJs/noEmit y nulabilidad: planilla.js y planilla-proceso.js, 0 diagnósticos; declaraciones externas mínimas para Bootstrap/SweetAlert2. No se desactivaron diagnósticos ni se instalaron paquetes.
- No se dispone de diagnósticos del IDE conectado. Los scripts inline preexistentes de Cuentas/Períodos no se certifican mediante chequeo TypeScript.
- dotnet build --no-restore: 0 errores. La recompilación completa mostró únicamente CS8602 preexistente en FamiliaProducto:170; el último build incremental terminó con 0 advertencias. No se modificó esa advertencia.
- git diff --check sin errores.
- El bloque de servicio MG/TablaGeneral se comparó literalmente con HEAD: idéntico. Sus vistas/controlador, layout global, site.css, configuración y navegación permanecen intactos.

## Pendientes

- Validar altas, ediciones, bajas y concurrencia con autorización expresa para escribir en una base de prueba.
- Probar interacción visual y cambio claro/oscuro en navegador. Se mantiene la limitación de lanzamiento de navegador informada en el lote anterior.
- Continuar aplicando este patrón por lotes a los demás módulos del inventario, sin declarar uniformidad de los 39 controladores por haber atendido cuatro.
- Completar motor de cálculo, fórmulas, esquemas/comisiones pensionarias y los pendientes funcionales del inventario anterior.
- Permisos/contexto de sesión fuera de los módulos RH atendidos siguen requiriendo revisión; este lote no resuelve la autorización transversal del ERP.
