# TS / Bancarización — 2026-10-31

Se incorporó `/TS/Bancariza` en **Tesorería → Procesos → Bancarización**, después de la categoría Maestros, conservando el orden del menú legacy. El permiso requerido es `ts / bancariza/index`. Se registró `ITesoreriaService` con su implementación Dapper, sin dependencias nuevas ni cambios de configuración o esquema.

## Pantalla y alcance

La bandeja usa la base de Maestros: cabecera, búsqueda, filtros por sede, número exacto, identidad, cliente, fechas e indicador de bancarización, orden global por columna antes de paginar, tamaños 25/50/100/500, total y rango visible, pie y actualización AJAX. El cambio de sede se limita a las sedes activas de la empresa y período del contexto de acceso.

Consultar o administrar abre un único modal extendido. La cabecera del comprobante es de solo lectura; el cuerpo contiene las cobranzas, buscador, cinco columnas ordenables, paginación y un editor interno para agregar, editar o consultar una cobranza. La columna Opciones queda al final de ambas tablas. El modo consulta y los permisos de lectura ocultan las acciones de modificación.

La fecha de una cobranza se establece al crearla y permanece de solo lectura al editarla. Se muestran los registros anulados para consulta histórica. La cabecera del comprobante no admite cambios de cliente, precios, importes, condición ni medio de pago.

## Reglas contrastadas con el legacy y SQL

Se inspeccionaron el controlador y los formularios de Bancariza, la capa business/repositorio de ComprobanteCobranza, el esquema de `compro_cobra` y `empresa_cta`, y las definiciones de los cuatro procedimientos utilizados.

- Se consultan facturas y boletas (`01`/`03`) activas con estado de comprobante `3`. Se conserva ese criterio del legacy sin agregar filtros por el campo `emitido` ni por `tipo_comprob`.
- El importe mínimo proviene del parámetro activo **0024 — Ventas / Bancarización**. La aplicación requiere un único valor numérico configurado y muestra un error explícito si falta o es ambiguo; no incorpora un mínimo fijo ni sustituye una configuración faltante por cero.
- La comparación utiliza el total y T/C del comprobante cuando la moneda es extranjera. En moneda local, el factor es **1**, incluso cuando el documento guarda T/C cero. No se consulta un tipo de cambio diario ni se reemplaza el T/C histórico.
- Las cuentas seleccionables deben estar activas y pertenecer a la empresa y moneda del comprobante. Las cobranzas históricas conservan su cuenta, aunque esté inactiva; no se exponen datos de cuentas de otra empresa o moneda.
- El número de operación admite hasta 50 caracteres, conforme al procedimiento existente. El monto positivo se conserva como `decimal(19,4)` y se rechazan más de cuatro decimales. Los valores de edición viajan como cadenas decimales exactas, sin conversión de importes a `Number` en JavaScript.
- El indicador se calcula por **existencia de al menos una cobranza activa**, como en el business original. No depende de cubrir el total de la factura ni se añade un límite por el saldo del comprobante.
- Alta, actualización y anulación lógica utilizan respectivamente `sp_compro_cobra_insertar`, `sp_compro_cobra_actualizar` y `sp_compro_cobra_eliminar`; después se recalcula el indicador con `sp_comprobante_actualizarBancarizado`, todo dentro de la misma transacción serializable.
- La actualización existente no recibe fecha. Se rechaza un intento de cambiarla, en lugar de ignorarlo silenciosamente. El procedimiento de indicador no modifica la fecha de auditoría del comprobante; por eso su revisión incluye también el indicador, además de pertenencia, moneda, fecha e importes.
- Se validan permisos, antiforgery, contexto, empresa, sede, comprobante y cobranza. Usuario y contexto proceden de la sesión. Los registros y sus revisiones se verifican de nuevo bajo `UPDLOCK/HOLDLOCK` al modificar, y se rechazan cobranzas anuladas o revisiones obsoletas.

No se trasladó la edición genérica de cabecera copiada en la vista antigua: no tenía una operación de guardado válida de este módulo. Tampoco se agregaron operaciones ajenas de pago o conciliación.

## Validación

- **77 comprobaciones de servicio aprobadas**. Las lecturas reales contrastaron los 2.008 comprobantes elegibles de la sede seleccionada con el criterio legacy y las cobranzas de un comprobante existente. Se verificaron nueve columnas principales en ambos sentidos, páginas estables, orden numérico, cinco columnas del detalle, tamaños, filtros combinados, resultados vacíos y pertenencias inválidas.
- Los guardados se probaron exclusivamente mediante una conexión simulada: procedimientos y parámetros exactos, auditoría de sesión, cuatro decimales, alta y actualización, fecha inmutable, anulación de la última cobranza y de una entre varias, cuentas de moneda incorrecta, IDs ajenos, revisiones obsoletas y registro anulado. Una falla simulada al actualizar el indicador comprobó el rollback de toda la operación.
- **31 comprobaciones HTTP aprobadas** usando Razor y los controladores reales: permisos de lectura/escritura, antiforgery, contexto/sesión y respuestas 409, binding y límites, metadatos, filtros y orden AJAX, cobranzas reales, POST simulado y navegación bajo Tesorería → Procesos.
- **89 comprobaciones HTTP de regresión aprobadas**: Cortes gratis (27) y Parámetros del sistema/navegación recursiva (62). Total funcional específico del lote: **197 comprobaciones aprobadas**.
- Auditoría estructural: **1.799 comprobaciones**, 134 tablas renderizadas, 152 modales y 914 cabeceras, sin fallos. Incluye las pantallas conservadas y las dos variantes nuevas de Bancarización; no equivale a una regresión funcional global.
- JavaScript comprobado con TypeScript, cero diagnósticos; 63 scripts inline analizados sin errores sintácticos. Diez comprobaciones de protección de TablaGeneral aprobadas.
- Compilación correcta, sin errores. En la compilación inicial permaneció únicamente la advertencia CS8602 preexistente de `Areas/MG/Views/FamiliaProducto/Index.cshtml:151`; la compilación incremental final no emitió advertencias.

No se ejecutaron escrituras, procedimientos de modificación, secuencias ni transacciones reales de modificación en SQL Server. No se reinició la aplicación del usuario; los servidores HTTP temporales se cerraron al terminar. Quedan pendientes la interacción visual en navegador y las escrituras reales en una base de pruebas autorizada. La regresión funcional global sigue sin declararse aprobada; su interrupción anterior consta en `migracion-procesos-produccion-2026-10-30.md`.
