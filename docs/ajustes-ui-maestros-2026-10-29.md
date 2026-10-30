# Ajustes de interfaz con Maestros como referencia

Fecha: 29 de octubre de 2026.

Se ajustaron las 36 vistas de listado existentes de MG, MK, RH, TS y AS. Este trabajo se limita a presentación, filtros, cantidades y ordenamiento; no incorpora módulos nuevos.

## Base compartida

- Cabecera con título, icono, descripción y acciones; contenido con contenedor fluido y tarjetas compactas.
- Buscadores y filtros pequeños en una barra adaptable, con cantidades y selector de registros a la derecha cuando corresponde.
- Grillas con cabeceras compactas, ordenamiento ascendente/descendente y columna final «Opciones».
- Selector de 25, 50, 100 y 500 registros; 25 como valor inicial. Se eliminaron las opciones «Ver todos» restantes de cuatro listados de RR. HH.
- Un pie por tabla. Las páginas con total completo muestran rango y paginación; las consultas que devuelven una lista muestran cuántas filas se ven de las filas recibidas, sin atribuirles un total global inexistente.
- Modales centrados y desplazables, con cabecera y pie fijos. Formularios simples compactos y formularios complejos con `modal-xl`.
- Los formularios de los modales tienen `form.modal-content` directamente bajo el diálogo. En Personal se separó el pie del cuerpo desplazable; en Familia de Productos el guardado quedó en el pie.
- Colores de superficie y texto basados en las variables del tema.

Los estilos se centralizan en `wwwroot/css/listados.css`. `wwwroot/js/erp-module-ui.js` completa el ordenamiento local, los indicadores y el resumen de los listados clásicos. El mecanismo AJAX existente continúa actualizando el contenido al filtrar, ordenar o cambiar la cantidad, conservando el marco de la aplicación y los formularios. Boletas actualiza también sus enlaces de impresión al cambiar la consulta.

Los cuatro catálogos de Marketing, Merchandising y Accesos extendidos incorporan ordenamiento mediante columnas SQL permitidas y desempate por identificador antes de paginar. No cambian sus operaciones de escritura.

## Alcance preservado

Tabla General está excluida de la clase, hoja de estilos y scripts compartidos. Su vista y controlador no tienen modificaciones. Se conservaron los campos, identificadores y manejadores de envío de los modales de las 36 vistas. Los 109 métodos públicos de escritura revisados permanecen iguales respecto al estado del proyecto al comenzar estos ajustes.

La boleta sigue siendo un documento: sus tablas de importes no reciben controles de ordenamiento de grilla. Clientes y Prospectos continúan reutilizando la vista de Proveedores.

## Validación

- Compilación y renderizado Razor correctos. Permanece el aviso nullable preexistente de Familia de Productos.
- 419 verificaciones de consultas, ordenamiento, paginación, exclusión de Tabla General y renderizado HTTP. Las 36 vistas se renderizaron vacías y con datos ficticios.
- 1.007 comprobaciones estructurales y de conservación de formularios/métodos, sin fallos: 35 grillas y 44 modales, revisados en ambos estados de renderizado.
- JavaScript compartido: cero diagnósticos; 36 scripts de vistas analizados, sin errores de sintaxis.
- `git diff --check` sin incidencias.

Las verificaciones de base de datos utilizaron conexiones que rechazan comandos distintos de SELECT/WITH, transacciones y consumo de secuencias. No se ejecutaron guardados ni anulaciones. El arnés de comprobación es temporal, fuera del repositorio.

La validación cubre código, estructura HTML y respuestas HTTP; no certifica la apariencia ni la interacción real en un navegador. Falta esa comprobación visual en anchos de escritorio/móvil y en ambos temas.
