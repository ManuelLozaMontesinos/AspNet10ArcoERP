# Canjes y Liquidación de caja — 2026-10-31

Se incorporaron `/VT/RepoCanje` y `/VT/RepoLiquiCaja`, con sus entradas en Ventas y permisos `vt / repoCanje/index` y `vt / repoLiquiCaja/index`. Ambos reutilizan `VentaReporteController`, los modelos de reportes, la pantalla compartida, el modal y el JavaScript existentes. El servicio de Ventas ya está registrado; no fue necesaria otra dependencia.

## Pantalla común

Cabecera, buscador, filtros, tarjetas y pie según la base de Maestros. Tamaños 25/50/100/500, total, rango visible y paginación. Orden global por todas las columnas antes de paginar, con desempate por ID real de detalle o cabecera. Filtros, orden y páginas usan el componente AJAX existente. La columna final Opciones abre un modal de consulta con cabecera y pie del tema, valores tratados como texto y una nota del contexto y las reglas del reporte.

Excel e impresión/PDF conservan el resultado completo, filtros, orden y nota de interpretación. Las exportaciones superiores a 50.000 registros se rechazan sin truncar. Se reutiliza el generador Excel con preservación de decimales y el formato de impresión actual; la impresión no reproduce la maquetación de Reporting Services.

## Canjes

Fuente contrastada: controlador/vista legacy `VT/RepoCanje` y definición local de `sp_rptvt_documentoCanje`.

- Consulta por mes calendario activo de `mes`, o por rango de fechas; no usa el ID de `mes_contable`. El valor enviado por el selector es el número calendario, resuelto desde el código real del catálogo.
- Detalles activos con `reg_canje_id IS NULL` y estado de comprobante `3`. Se conserva la combinación de documento interno con tipo de comprobante `1`, o documento no interno con tipo `4`.
- El argumento `incluirInterno` del controlador legacy no se utilizaba en sus parámetros SQL. No se añadió un selector que aparentara cambiar esa regla: se conservan ambas ramas del procedimiento y se explican en pantalla.
- Cantidad original; precio unitario y total en moneda local usando el T/C del documento cuando su moneda es extranjera. Si la cabecera está anulada, esos dos importes quedan en cero, conservando la cantidad.
- Se muestran documento, fecha, producto, mes, moneda de origen, T/C, cantidad, importes locales y estado. Se verifica la empresa del producto mediante su jerarquía de tipo/grupo/clase/familia, además del contexto de la cabecera.

Se corrigió el modo por fechas: no intenta convertir un mes vacío a entero como hacía el controlador legacy. Mes y rango se validan como modos excluyentes; el día final incluye todas sus horas mediante un límite superior exclusivo al día siguiente.

## Liquidación de caja

Fuente contrastada: controlador/vista legacy `VT/RepoLiquiCaja` y definición local de `sp_rptvt_liquidacionCaja`.

- Cajas activas de la sede seleccionada y fecha obligatoria para consultar o exportar. La entrada inicial propone la fecha del servidor, como el legacy, sin cargar documentos hasta consultar.
- Cabeceras del contexto y caja seleccionados con estado de comprobante `3`. Se verifican adicionalmente la sede de la caja y la empresa del cliente.
- Se conservan los importes originales y la moneda de cada documento, incluso el monto de una cabecera anulada si sigue cumpliendo el estado de comprobante requerido. El procedimiento anterior no lo ponía en cero ni excluía esas cabeceras; la pantalla indica esta regla.
- Caja, tipo/documento, fecha, cliente, código ISO y símbolo de moneda, T/C, total y estado. No se mezclan monedas en un total general.
- La fecha incluye el día completo. Se corrigió la igualdad contra medianoche del procedimiento anterior, que omitía documentos con hora registrada.

Los dos controladores legacy de este lote ofrecen consultas y exportación. No contienen una operación de creación de canjes ni de cierre de caja; el lote conserva ese alcance funcional.

## Contexto compartido y validación

Se extrajo la consulta de contexto que ya usaban los reportes y Cuadro de costos a un único helper privado del servicio. Conserva los mismos datos y comprobaciones: empresa, sede y período activos, pertenencia a la empresa y fecha actual del servidor. Se evita mantener una tercera copia de esa consulta.

- **111 comprobaciones de servicio aprobadas** con conexión que rechaza DML, procedimientos y transacciones. Datos contrastados: 2.424 líneas de Canjes en septiembre de 2021 y 894 documentos de Liquidación el 27/06/2026, de contextos válidos de la base local.
- Comparación de todos los campos e importes de esas muestras contra los SELECT extraídos de las definiciones reales. No se ejecutaron los procedimientos. Para Liquidación se adaptó exclusivamente su predicado de fecha a día completo al realizar la comparación.
- Orden SQL en ambos sentidos por las 12 columnas de Canjes y 10 de Liquidación, páginas estables, tamaños, filtros, exportación completa y límites; contexto ajeno, caja ajena o inexistente, modos y fechas inválidos, y resultados vacíos. Se contrastó también mes completo frente al rango equivalente de días.
- **43 comprobaciones HTTP aprobadas** sobre los controladores y Razor reales: vistas iniciales y con datos, filtros específicos, permisos exactos y denegados en las tres acciones, recuperación de sesión y error AJAX 409, model binding inválido, Excel e impresión reales. Incluyen tres consultas iniciales de regresión de Documentos temporales, Reservas y Pedidos por producto.
- **26 comprobaciones HTTP de regresión de Cuadro de costos aprobadas**, incluyendo sus dos bases de cálculo, los Excel completos e impresión, tras extraer el helper de contexto. Total específico de este lote: **180 comprobaciones aprobadas**.
- Auditoría estructural: 1.693 comprobaciones, 125 tablas, 143 modales y 853 cabeceras, sin fallos. Incluye cuatro capturas nuevas y las anteriores; no acredita una regresión funcional global.
- JavaScript: cero diagnósticos TypeScript; 59 scripts inline analizados sin errores sintácticos. Diez comprobaciones de protección de TablaGeneral aprobadas.
- Compilación correcta. Permanece la advertencia preexistente CS8602 en `Areas/MG/Views/FamiliaProducto/Index.cshtml:151`.

No hubo escrituras en la base, cambios de esquema, ejecución de secuencias/procedimientos, dependencias nuevas, cambios en TablaGeneral ni reinicios de la aplicación del usuario. Los servidores temporales de verificación se detuvieron al finalizar. Continúa pendiente la revisión visual e interacción en navegador. La regresión global no se declara aprobada; su interrupción previa está documentada en `migracion-procesos-produccion-2026-10-30.md`.
