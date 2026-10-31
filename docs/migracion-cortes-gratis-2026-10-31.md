# VT / Cortes gratis — 2026-10-31

Se incorporó `/VT/CortesGratis`, ubicado en **Ventas → Maestros → Cortes gratis**, con el permiso exacto `vt / cortesGratis/index`. Mantiene la estructura y orden de categorías del menú y utiliza el servicio de Ventas ya registrado.

## Pantalla y alcance

La bandeja de clases sigue la base de Maestros: cabecera con sede actual, búsqueda por código/nombre, estado, orden por columna, tamaños 25/50/100/500, total y rango visible, pie y actualización AJAX.

Consultar o editar una clase abre un único modal extendido. Su cabecera identifica clase y sede; el cuerpo contiene filtro de familia, buscador de productos y una tabla con código, producto, familia, cantidad, estado de configuración y estado del producto. Todas sus columnas se ordenan en el servidor antes de paginar. La cantidad se consulta o edita en un panel dentro del mismo modal, con guardado individual y sin abrir otro modal. Los controles se bloquean durante la carga o edición para evitar actuar sobre una página que se está sustituyendo.

## Reglas contrastadas

Se inspeccionaron controlador, formularios, DTO/domain, business y repositorio legacy, el esquema de `cortes_gratis` y las definiciones de `sp_cortes_gratis_buscarPor` y `sp_cortes_gratis_actualizar`.

- Se conserva la clasificación explícita de clases del grupo `5`, añadiendo la verificación de empresa mediante la jerarquía del producto. La sede se resuelve desde el contexto activo de sesión, no desde un identificador de sede enviado por el cliente.
- Se consultan solo configuraciones ya existentes en `cortes_gratis`, igual que el procedimiento original. No se crean cantidades cero para productos sin configuración.
- Las familias seleccionables son las activas de la clase, como el lookup legacy. La consulta sin filtro de familia conserva todas sus configuraciones, incluidos productos inactivos; no se añadió un filtro que ocultara esos registros.
- Las cantidades son `decimal(10,2)`, no necesariamente enteros. Se conserva el rango representable del contrato existente, incluidos valores con signo; el legacy y el esquema no definen una restricción adicional de no negatividad. Se rechazan más de dos decimales en lugar de truncarlos. Los valores SQL nulos se muestran vacíos.
- Cada guardado usa el procedimiento existente con ID, cantidad, estado `A` y usuario de sesión. Ese estado conserva el efecto legacy de activar la configuración al guardar, incluso si antes estaba inactiva. La confirmación de pantalla explica ese efecto cuando corresponde.
- Se permite ajustar configuraciones de productos inactivos, como en el editor legacy. Una clase inactiva se conserva en consulta y no permite guardar, conforme al comportamiento del menú de edición original.
- Se comprueban empresa, sede, clase, familia y producto; el registro no puede trasladarse a otro producto o sede. La revisión incluye cantidad y pertenencia, además de estado y fecha de auditoría, y se valida nuevamente bajo `UPDLOCK/HOLDLOCK` dentro de una transacción serializable.

El módulo no ofrece altas o bajas de clases/productos. Los botones genéricos del legacy apuntaban a acciones de `FamiliaProducto`, y su `CortesGratisBusiness.Insert` no efectuaba un alta. No se trasladaron esos enlaces ajenos ni se inventó una operación nueva. El catálogo y los datos de productos permanecen intactos.

La modificación queda limitada a cantidad, estado y auditoría de la configuración por sede; el procedimiento no cambia stock ni precios y no hay triggers en esta tabla local. No se añadieron dependencias, tablas, secuencias o procedimientos.

El helper privado de contexto de los reportes ahora admite una transacción opcional, para reutilizar la misma validación dentro del guardado. Sus consultas anteriores conservan el comportamiento sin transacción.

## Validación

- **92 comprobaciones de servicio aprobadas**, con lecturas reales y guardados/transacciones simulados. Se contrastaron las **11 clases y 630 configuraciones de una sede**, incluidas **130 de productos inactivos**, con la proyección del procedimiento legacy. La otra sede devolvió configuraciones con IDs distintos, sin mezclar sus filas.
- Se verificaron metadatos/familias, filtros combinados, seis columnas de detalle en ambos sentidos, orden numérico antes de paginar, páginas estables, cuatro tamaños, resultados vacíos, contexto y pertenencia inválidos.
- Guardados simulados con el procedimiento y parámetros reales, dos decimales, límites, cantidad ausente, producto inactivo, activación de configuración retirada, clase inactiva, ID de otra sede, producto/clase incorrectos, revisión obsoleta, cambio concurrente de cantidad y rollback simulado.
- **27 comprobaciones HTTP aprobadas**: Razor real de consulta/escritura, permiso exacto y denegado en las tres acciones GET y en POST, antiforgery, contexto y sesión/409, model binding, metadatos y tabla de productos reales, filtros/orden AJAX y POST simulado con empresa/contexto/usuario de sesión. Se comprobó también su enlace activo en Ventas → Maestros.
- **69 comprobaciones HTTP de regresión aprobadas**: Cuadro de costos (26) y Canjes/Liquidación/reportes iniciales anteriores (43), tras ampliar el helper de contexto. Total específico de este lote: **188 comprobaciones aprobadas**.
- Auditoría estructural: 1.771 comprobaciones, 132 tablas, 150 modales y 896 cabeceras, sin fallos. Incluye dos capturas nuevas y las anteriores; no acredita una regresión funcional global.
- JavaScript comprobado con TypeScript, cero diagnósticos. 62 scripts inline analizados sin errores sintácticos. Diez comprobaciones de protección de TablaGeneral aprobadas.
- Compilación correcta. Permanece la advertencia CS8602 preexistente en `Areas/MG/Views/FamiliaProducto/Index.cshtml:151`.

No se ejecutaron escrituras ni procedimientos reales de modificación, cambios de esquema, secuencias o reinicios de la aplicación del usuario. Los servidores HTTP temporales se cerraron al terminar. Quedan pendientes la interacción visual en navegador y el guardado real en una base de pruebas con autorización de escritura. La regresión global no se declara aprobada; su interrupción anterior está documentada en `migracion-procesos-produccion-2026-10-30.md`.
