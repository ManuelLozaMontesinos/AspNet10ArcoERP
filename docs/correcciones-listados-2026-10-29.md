# Correcciones de Merchandising y actualización parcial — 29/10/2026

## Error de acceso a Merchandising

GetMerchandisingTransactionsAsync usaba @EmpresaSedeId y @PeriodoAnio sin enviar el objeto de parámetros a Dapper. Se agregaron ambos parámetros en Services/MKService.cs. No se cambiaron las consultas de negocio, el almacén ni los datos.

Se verificaron con SQL de solo lectura: transacciones, listado y rechazo de sede ajena. La vista Merchandising devolvió HTTP 200 usando el servicio real. La base local no tiene movimientos de Marketing en el contexto seleccionado; no se certifica el detalle con filas reales.

## Filtros, orden y paginación sin recargar la aplicación

Nuevo wwwroot/js/erp-listings.js, cargado desde el layout para las áreas y excluido expresamente de MG/TablaGeneral.

- Intercepta formularios GET y enlaces del mismo módulo dentro del contenido; usa fetch con cookies de la misma procedencia.
- Actualiza el contenido y los contadores del módulo. Mantiene el layout, el menú, su desplazamiento, el tema y las instancias de modal.
- Conserva los formularios y controles existentes con sus escuchas de eventos/validación, actualizando valores y opciones recibidos.
- No vuelve a ejecutar los scripts de la vista. Los scripts de las respuestas se descartan.
- Actualiza las configuraciones JSON registradas; Proveedor y Producto siguen usando el rol/tipo del filtro actual al abrir un alta.
- Cancela peticiones anteriores y comprueba su versión para evitar que una respuesta anterior sustituya resultados recientes.
- Mantiene filtros en la URL con History API y atiende Atrás/Adelante mediante consulta parcial.
- Muestra progreso, conserva las filas ante errores y comunica sesión expirada/errores sin reemplazar el ERP por HTML de error.
- No intercepta POST, formularios/enlaces de modales, navegación externa, descarga, apertura en otra pestaña ni enlaces del menú.

Se adaptaron 20 manejadores de botones de filas a delegación, para que funcionen también después de sustituir resultados: Planillas, Procesos, Horas, Regularizaciones, Accesos, Cuentas, Períodos y los cinco módulos MK con botones de bandeja. Los eventos de edición de los modales no se registran de nuevo.

Producto, Combo y Transportista ahora usan requestSubmit en los filtros que antes llamaban submit directamente; TipoCambio usa la consulta parcial en sus dos accesos de búsqueda/limpieza. Los filtros/órdenes que ya trabajan localmente siguen haciéndolo.

## Archivos de este ajuste

- Services/MKService.cs.
- wwwroot/js/erp-listings.js y erp-listings.d.ts.
- Views/Shared/_Layout.cshtml: inclusión condicional del recurso antes de los scripts del módulo.
- JS Planilla/PlanillaProceso/Proveedor: delegación o configuración compartida.
- Vistas de Producto/Combo/Transportista/TipoCambio: envío de filtros/contexto actual.
- Vistas de LeadSource/Oportunidad/TipoProfesion/TipoVentaCliente/Merchandising, AccesoExt/PlanillaHora/PlanillaRegula, CuentaBanco y Periodo: delegación de botones.

Se conservaron los cambios acumulados del lote anterior. No se hicieron commits ni cambios en dependencias del ERP.

## Verificación y límites

- 166 comprobaciones .NET/SQL/renderizado aprobadas en el verificador temporal. Incluyen Merchandising real, aislamiento, listados existentes y recurso AJAX incluido en las cuatro vistas del lote anterior.
- TablaGeneral renderiza sin el recurso AJAX; su controlador, vista e implementación de servicio permanecen intactos.
- TypeScript allowJs/checkJs/noEmit y nulabilidad: ERP Listings, Planilla y PlanillaProceso, 0 diagnósticos. Declaraciones mínimas para Bootstrap/SweetAlert2 en el verificador.
- 36 bloques JavaScript inline revisados: 0 errores de sintaxis. Esto no certifica todo el tipado de los scripts legacy.
- dotnet build --no-restore: 0 errores; únicamente CS8602 preexistente en FamiliaProducto:170 en la recompilación completa.
- git diff --check sin errores.
- Sin DML, ejecución de procedimientos de escritura ni consumo de secuencias.

La revisión automática rechazó tanto la preparación de una biblioteca temporal de DOM como la ejecución de la prueba de eventos simulados, con motivo «blocked by policy». Esas pruebas no se ejecutaron ni se contabilizaron como aprobadas. Quedan pendientes las comprobaciones de interacción, peticiones consecutivas, botones tras refrescar y navegación Atrás/Adelante en un navegador real.
