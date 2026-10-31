# Subniveles del menú y Parámetros del sistema — 2026-10-31

## Navegación por Áreas

Se reorganizó la barra lateral con la misma base de Maestros, Procesos y Reportes de Capital Humano. Se contrastaron las relaciones y posiciones del catálogo `menu` y el armado del árbol en el controlador legacy. Las ramas disponibles siguen este orden de Áreas: Maestros, Capital Humano, Compras, Inventario, Ventas, Tesorería, Producción, Marketing y Administración.

Solo se muestran grupos que contienen pantallas migradas. No se agregaron enlaces a módulos pendientes ni grupos vacíos. Se conservaron **todos los enlaces existentes**; el único enlace nuevo de este lote es Parámetros del sistema.

| Área | Organización de las pantallas disponibles |
|---|---|
| Maestros generales | Se conserva completo su bloque anterior, incluida la rama Productos y Tablas Generales. |
| Capital Humano | Se conserva completo su bloque anterior de Maestros, Procesos y Reportes, usado como referencia visual. |
| Compras | Procesos → Punto de pedido; Reportes → Valorización de comprobantes. |
| Inventario | Maestros → Transacciones, Almacenes y Volumen de existencias; Procesos → consultas y Apertura/Cierre → Reproceso/Apertura y cierre; Reportes → saldo valorizado, kardex, saldo de unidades, movimientos y guías. |
| Ventas | Maestros → Vendedores, Lista de precios → Grupos de clientes y Medios de pago; Procesos → Comprobantes → Detracciones, Cuadro de costos y Caja → Cajas; Reportes → Canjes, Reservas, Temporales, Liquidación de caja y Pedidos por producto. |
| Tesorería | Maestros → Cuentas bancarias. |
| Producción | Maestros → Procesos de producción; Reportes → Reporte de procesos. |
| Marketing | Maestros → Prospectos, Clientes, Tablas Generales → Tipos de profesión/Oportunidades/Origen de prospectos/Tipos de venta y Merchandising. Esta rama de Marketing es distinta de MG/TablaGeneral. |
| Administración | Maestros → Períodos, Series y Parámetros del sistema, agrupados para aplicar la base solicitada a sus pantallas disponibles. |

Se centralizó la definición de las siete Áreas reorganizadas en `Models/SidebarNavigation.cs` y su renderizado recursivo en `Views/Shared/_SidebarNavigationItem.cshtml`. Cada pantalla abre todos sus padres y queda marcada como actual. Se conservan iconos y colores de categoría, el comportamiento de expansión múltiple y el desplazamiento interno corregido en el lote anterior.

Los bloques existentes de MG y RH se preservaron completos, sin reescribir su contenido. Se mantiene Transportistas en su ubicación actual de Maestros generales. No se alteró la ruta ni la jerarquía interna de MG/TablaGeneral. Tampoco se modificó el catálogo SQL ni los permisos para reorganizar el menú.

## AS / Parámetros del sistema

Se incorporó `/AS/ParametroSistema`, accesible desde Administración → Maestros. Usa el permiso exacto `as / parametroSistema/index`, contexto activo y el servicio de Administración existente.

La pantalla sigue Maestros: cabecera, buscador por código/nombre/grupo, grupo y estado, orden global por las seis columnas, tamaños 25/50/100/500, total/rango y paginación AJAX. Las opciones finales permiten consultar y, con permiso de escritura, editar el valor en un único modal extendido.

Se contrastaron controlador, repositorio y formulario legacy, catálogo `tipo_dato`, esquema y definición de `sp_parametro_sistema_actualizar`:

- Los parámetros son **globales**, sin columna empresa. Se indica ese alcance en la pantalla y antes de guardar. Se exige un contexto de empresa/sede/período válido y el permiso específico, sin inventar una propiedad por empresa para las filas.
- Se editan solo parámetros activos con `tiene_mant='S'` y uno de los cinco editores existentes en el legacy: cadena `01`, entero `02`, decimal `03`, cadena oculta `07` y rango horario `08`. Los restantes tipos conservan el alcance de consulta sin agregar un editor que no exista en ese formulario.
- No se agregaron altas, eliminaciones, reactivaciones ni edición de código/nombre/tipo/grupo/habilitación de mantenimiento.
- El entero admite el rango de Int32; el decimal conserva la precisión `decimal(19,4)`. Su representación para el editor se envía también como texto decimal exacto, evitando redondearlo al pasar por un número JavaScript.
- Los rangos horarios se validan como `HH:mm` y conservan la representación numérica legacy `HH.mm`. Se permiten rangos nocturnos, sin inventar una restricción de orden entre inicio y fin.
- Los valores ocultos se excluyen de las proyecciones públicas en SQL. No aparecen en tabla, HTML, campos precargados ni JSON. Se muestran protegidos y únicamente se reemplazan al activar la opción e introducir un nuevo valor de hasta 50 caracteres. La búsqueda no consulta valores y el orden no utiliza el contenido de secretos.
- El procedimiento existente recibe `varchar(100)`, aunque la columna admite 300. Se limita la cadena editable a 100 y se rechaza cualquier actualización que truncaría el valor conservado. No se modificó el procedimiento para ampliar ese contrato.
- Se relee el registro bajo `UPDLOCK/HOLDLOCK` dentro de una transacción serializable, se verifica su revisión y mantenimiento habilitado y se llama al procedimiento existente con el usuario de sesión. Los campos que no corresponden al tipo editado se conservan del registro, incluyendo fecha y nombre.

## Verificación

- **122 comprobaciones aprobadas de servicio y definición de navegación**: catálogo global, proyección de secretos, consultas y detalles, seis columnas en ambos sentidos con páginas estables, filtros, tamaños, contexto y grupos inválidos; actualizaciones de los tipos habilitados con procedimiento simulado, auditoría, horas nocturnas, precisión, revisión obsoleta, concurrencia y rollback simulado. Incluyen reemplazo explícito de un secreto ficticio y conservación de todas las rutas anteriores.
- **60 comprobaciones HTTP/Razor aprobadas**: pantalla real con permisos de consulta y escritura, JSON protegido, antiforgery, contexto y sesión, errores de binding y POST simulado. Incluyen el orden completo de las Áreas y el renderizado recursivo de cada una de las 38 hojas del nuevo catálogo, comprobando URL, selección única y apertura de todos sus padres.
- Total específico del lote: **182 comprobaciones aprobadas**. Las escrituras se interceptaron y las transacciones de esas pruebas fueron simuladas; ningún procedimiento de actualización llegó a SQL Server.
- Auditoría estructural: 1.755 comprobaciones, 130 tablas, 148 modales y 890 cabeceras, sin fallos. Incluye dos capturas nuevas y las anteriores; no acredita una regresión funcional global.
- JavaScript, incluido el nuevo editor: cero diagnósticos TypeScript. 61 scripts inline analizados, sin errores sintácticos. Diez comprobaciones de protección de TablaGeneral aprobadas.
- Compilación correcta. Permanece la advertencia CS8602 preexistente en `Areas/MG/Views/FamiliaProducto/Index.cshtml:151`.

Las inspecciones reales de SQL fueron de lectura. No se ejecutaron escrituras, secuencias, procedimientos de modificación, cambios de esquema/dependencias ni reinicios de la aplicación del usuario. Los servidores temporales de verificación se cerraron al terminar. Queda pendiente comprobar visualmente la interacción en navegador y guardar contra una base de pruebas con autorización de escritura. La regresión global no se declara aprobada; su interrupción anterior permanece documentada en `migracion-procesos-produccion-2026-10-30.md`.
