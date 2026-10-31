# Barra lateral y reporte de producción — 2026-10-31

## Corrección solicitada de la barra lateral

La imagen mostraba las opciones expandidas fuera del fondo del menú y alargando el desplazamiento de la página. AdminLTE asignaba una altura a `.sidebar-wrapper`, pero no había un contenedor con desplazamiento vertical interno habilitado.

Se ajustó únicamente la distribución del contenedor lateral en `wwwroot/css/site.css`:

- Barra lateral en columna flex, limitada al alto de la ventana (`100dvh`, con alternativa `100vh`) y contenido exterior recortado.
- Logo sin encogerse; la zona de opciones ocupa el alto restante con `min-height: 0`, desplazamiento vertical automático y sin desplazamiento horizontal.
- La regla del contenedor iguala la especificidad de AdminLTE para `layout-fixed sidebar-expand-lg`; el CSS local se carga después del proveedor y sustituye su altura calculada.
- El desplazamiento no se propaga a la página al llegar al extremo del menú. Se conservan el estado de expansión de las ramas y el comportamiento del menú colapsado/móvil existente.

Se conservó la barra de desplazamiento nativa y su tema ya definido por AdminLTE. No se añadieron librerías ni JavaScript para medir o reconstruir el menú. Es una corrección del contenedor compartido solicitada expresamente; no modifica el contenido, enlace o implementación de TablaGeneral.

Se verificaron las reglas y su precedencia en las fuentes locales, la carga del CSS después de AdminLTE y la respuesta HTTP del archivo actualizado. Falta comprobar la interacción visual real con múltiples ramas abiertas en el navegador del usuario; las pruebas HTTP no certifican dimensiones ni desplazamiento visual.

## PD / Reporte de procesos de producción

Se incorporó `/PD/RepoProceProd`, accesible desde **Producción → Reporte de procesos**, con el permiso exacto `pd / repoProceProd/index`. Reutiliza el servicio de Producción registrado, la validación de contexto existente, los controles de listado y el modal genérico de consulta de reportes.

Ofrece vistas **Estadístico** y **Detallado**, mes calendario o rango de fechas, búsqueda, orden por todas las columnas, tamaños 25/50/100/500, total y rango visible, navegación AJAX y opciones por fila. Excel e impresión/PDF conservan los filtros, orden y resultado completo. Las exportaciones superiores a 50.000 filas se rechazan sin truncar.

### Compatibilidad con el legacy

Se contrastaron el controlador y vista legacy `PD/RepoProceProd`, las definiciones locales `sp_rptpd_procesosProduccion` y `sp_rptpd_procesosProduccionDetallado`, y el esquema y clasificaciones reales.

- Se conservan las operaciones con estado `3` de los subprocesos `1`, `9` y `10`: corte lineal de melamina, aglomerado y alta gama. El nombre «MDF» de un comentario del procedimiento no coincide con el nombre real del subproceso `9`; la pantalla utiliza **aglomerado**.
- El filtro mensual usa `mes` calendario y el mes de inicio de la operación. El detallado conserva la fecha del pedido como otra columna, junto con entrega, inicio, fin, operador, proceso, máquina, interrupciones, observación, piezas, planchas y estado.
- El estadístico conserva la agrupación por día de inicio, máquina y nombre abreviado del operador. Melamina/alta gama se calculan en una rama; aglomerado en otra. Las filas de ambas ramas se mantienen aunque coincidan los valores visibles, con una clave interna de grupo para paginar de forma estable.
- Piezas: productos de servicio `3083`, `3946` y `3947`. En el resumen se suman `3083` y `3947` para la primera rama; en el detallado se consideran por separado.
- Planchas: grupos `5` y `6`. El detallado de melamina conserva las exclusiones de familias `126/137` y clases `45/70`. Alta gama conserva el efecto del `UNION ALL`: si un producto pertenece a ambas selecciones, su cantidad cuenta dos veces. No se sustituyó por una unión que eliminara esa multiplicidad.
- Las cantidades sin origen permanecen nulas y se muestran vacías. El promedio conserva `piezas / planchas` redondeado a dos decimales; se usa `NULLIF` para que un denominador cero produzca una celda vacía en lugar de una excepción SQL.
- En modo mensual se conserva la condición legacy `inicio <= fin`. En modo por fechas, el día final se incluye completo mediante un límite exclusivo al día siguiente.
- Se verifica el contexto activo de empresa/sede/ejercicio. Se comprueba además la empresa del pedido, del personal/proveedor y de los productos utilizados en las cantidades, sin exigir que el pedido proceda del mismo ejercicio que la orden.

El módulo legacy de este lote es de consulta/exportación. La edición del catálogo de procesos continúa en su pantalla ya migrada.

### Rendimiento y formatos compartidos

Las cantidades se agregan una vez por orden desde un conjunto distinto de órdenes filtradas, conservando su repetición por operación al construir el resultado. Esto sustituyó una comprobación correlacionada que recorría repetidamente las operaciones.

En una muestra de noviembre de 2025, la consulta de 25 filas estadísticas ordenadas por fecha pasó de unos 19,8 segundos a unos 0,5 segundos con el cambio de consulta. Son mediciones locales, no una garantía para todos los contextos. No se crearon índices, tablas temporales ni configuraciones del servidor, ni se elevó el tiempo de espera.

El modal compartido ahora reconoce fechas con hora y mantiene su formato de fecha anterior para los reportes existentes. El Excel permite indicar columnas con hora; solo el nuevo reporte solicita ese formato para entrega/inicio/fin. Los demás reportes conservan su formato anterior. La impresión usa el formato actual del ERP, sin reproducir la maquetación de Reporting Services.

## Validación

- **100 comprobaciones de servicio aprobadas**: contexto, filtros, las dos vistas, orden global en ambos sentidos por sus 6 y 13 columnas, páginas estables, tamaños, filtros vacíos y exportación completa. Comparación de todos los valores y multiplicidad contra los SELECT extraídos de las definiciones reales: **208 filas estadísticas y 2.470 detalladas** de noviembre de 2025. No se ejecutaron los procedimientos; en el SELECT de contraste estadístico se protegió igualmente el denominador cero.
- **3 comprobaciones adicionales de SQL estadístico aprobadas**, con datos sintéticos declarados mediante `VALUES` y el fragmento SQL real del servicio: denominador cero, cantidades ausentes y promedio definido. Son exclusivamente SELECT, sin insertar datos.
- **30 comprobaciones HTTP aprobadas**: Razor real vacío y con datos, permiso exacto y denegado en las tres acciones, sesión/409 AJAX, model binding, filtros/orden, Excel completo de ambas variantes e impresión, precisión y formato de la fecha/hora del Excel, y consulta inicial del catálogo de procesos existente. Incluyen la carga y entrega del CSS del menú.
- **69 comprobaciones HTTP de regresión aprobadas**: Cuadro de costos (26) y Canjes/Liquidación/reportes iniciales anteriores (43), tras los cambios de formato compartido. Total específico de este lote: **202 comprobaciones aprobadas**.
- Auditoría estructural: 1.733 comprobaciones, 128 tablas, 146 modales y 878 cabeceras, sin fallos. Incluye capturas anteriores y tres nuevas; no acredita una regresión funcional global.
- JavaScript: cero diagnósticos TypeScript; 60 scripts inline analizados sin errores sintácticos. Diez comprobaciones de protección de TablaGeneral aprobadas.
- Compilación correcta; permanece la advertencia preexistente CS8602 de `Areas/MG/Views/FamiliaProducto/Index.cshtml:151`.

Las verificaciones usaron conexión que rechaza DML/procedimientos/transacciones. No se realizaron escrituras reales en la base, cambios de esquema o dependencias, ejecución de secuencias, ni reinicios de la aplicación del usuario. Se detuvo únicamente una ejecución propia de verificación durante el ajuste de rendimiento y se cerraron los servidores HTTP temporales al terminar. Continúa pendiente la comprobación visual en navegador. La regresión funcional global no se declara aprobada; la interrupción del lote previo permanece documentada en `migracion-procesos-produccion-2026-10-30.md`.
