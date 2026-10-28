# Estado de migración del ERP Arco — 28/10/2026

## Alcance y evidencia

- Moderno: D:\DevArco\erp_arco\AspNet10ArcoERP, rama master, objetivo net10.0.
- Fuente de negocio: D:\DevArco\erp_arco\ERP-Arco n5, rama main. Su proyecto ESD.ERP.Web.v6 **ya apunta a net5.0**, aunque el antecedente lo describa como .NET Framework.
- Ambos worktrees estaban limpios al iniciar. Este lote queda sin commit.
- Arquitectura moderna: MVC por áreas, Razor, Dapper; IMaestrosService, IRHService, IMKService e IAdministracionService registrados en Program.cs. Menú estático en Views/Shared/_Layout.cshtml.
- Inventario: 126 archivos de controladores de áreas legacy y 39 modernos. No equivale a porcentaje funcional: algunos reúnen varias funciones; otros sólo consultan.
- Comparación estática de controladores, vistas, contratos y servicios; revisión profunda de PlanillaProceso y las brechas concretas descritas. En módulos ausentes se verificó ausencia de superficie moderna; su lógica transaccional debe inspeccionarse antes de migrarlos.
- No se encontró infraestructura de pruebas en la solución. SQL se inspeccionó mediante SELECT y metadatos; **no se ejecutó DML, procedimientos de escritura ni consumo de secuencias**.
- MG/TablaGeneral queda excluido e intacto, incluidas sus dependencias, navegación y estilos. Ningún archivo MG, configuración ni menú fue modificado.

**Clasificación:** «Migrado parcialmente» indica implementación sin paridad o validación completa; «Solo consulta», lecturas identificadas; «Pendiente», ausencia de equivalente; «Bloqueado», fuente legacy sin comportamiento operativo definido. No se certifica un módulo completo como «migrado y funcional» sólo por tener CRUD o compilar: las escrituras no se probaron por la restricción del usuario.

## Primer lote: RH/PlanillaProceso

### Fuentes y dependencias

Legacy: ESD.ERP.Web.v6/Areas/RH/Controllers/PlanillaProcesoController.cs y vistas Index.cshtml/CreateEdit.cshtml; ESD.ERP.Business.v6/Impl/PlanillaProcesoBusiness.cs; ESD.ERP.Repository.v6/Impl/PlanillaProcesoRepository.cs. Permisos: Infrastructure/DataController.cs y ESD.ERP.Common.v6/Constant.cs.

Moderno: controlador y vista PlanillaProceso; RHViewModels.cs; IRHService/RHService; nuevo wwwroot/js/planilla-proceso.js. Servicio, ruta y entrada de menú ya existentes, sin cambios de registro/navegación.

Esquema comprobado: plan_proce, planilla, periodo_emp_sede, periodo, empresa_sede, mes, moneda, peri_remu, estado_plan_pro; plan_calculo, personal, proveedor, cargo_perso, concep_remu, tipo_con_remu; usuario, rol_usuario, rol, rol_menu, menu.

Secuencia y procedimientos: plan_proce_seq, sp_plan_proce_insertar, sp_plan_proce_actualizar y sp_plan_proce_eliminar. El SP local de alta no admite fechaProceso, aunque el repositorio legacy lo envía. Se sigue la definición real. Actualizar conserva planilla/número/mes y permite moneda/T-C/periodicidad/fechas/auditoría. Eliminar realiza baja lógica I.

### Cambios

1. proc_anual es **número de proceso**, no año. El año procede de periodo.periodo. No se modifican datos históricos que contengan años en proc_anual.
2. Estados reales por código: 01 ABIERTA, 02 CERRADA. Se retira la interpretación de tres estados PENDIENTE/PROCESADO/CERRADO.
3. Detalle corregido a concep_remu.tipo_con_remu_id y tipo_con_remu; antes usaba nombres inexistentes. Totales por código 01 remuneración y 03 retención; aportación 02 no aumenta el neto.
4. Contexto desde USER_SESSION, contrastado con usuario autenticado y relación activa período/sede/empresa. Sin respaldo silencioso a ID 1. PerfilAcceso actualiza sesión, pero actualmente no renueva claims.
5. Consultas del controlador filtradas por empresa y período/sede. Permisos de lectura/escritura desde roles y menú; administradores según constantes legacy, sin confiar en IsAdmin predeterminado. Antiforgery en POST.
6. Alta/edición/anulación con SP verificados, referencias y fechas validadas, precisión de T/C, bloqueo de fila, auditoría y fecha de proceso preservadas. Sólo procesos activos/abiertos modificables. Campos de identidad y periodicidad protegidos tras alta; la periodicidad también se bloquea en la pantalla legacy.
7. Se retira el falso cálculo que sólo cerraba la planilla. Procesar responde HTTP 501 y mensaje explícito; su servicio no abre conexión ni modifica estado.
8. Modal único para cabecera y resultados, tema Bootstrap, eventos externos sin atributos inline, datos renderizados con textContent, SweetAlert2, paginación 25/50/100/500 y total real.
9. Ajuste puntual de etiquetas en PlanillaHora/PlanillaRegula para distinguir año y número; GetPlanillaHorasAsync incluye el año. No se migran otros flujos de esos módulos.

### Pendientes de este flujo

El motor legacy **sí existe**: PlanillaProcesoBusiness.Calcular crea base, ordena conceptos, resuelve tokens/variables, calcula cada concepto, agrega préstamos y regularizaciones y limpia la base dentro de una transacción. Usa VariableProcesoType y varios SP. No es un UPDATE de estado ni un flujo sin definición: requiere otro lote con pruebas de cálculo y permiso para validar escrituras.

Otros controladores RH aún usan claims antiguos y autorización genérica. Se conserva GetPlanillaProcesoByIdAsync(id) para Regularizaciones, cuyo controlador comprueba pertenencia antes de invocarlo. No se considera resuelta la seguridad transversal.

## Matriz de módulos modernos

Rutas relativas a la raíz moderna salvo indicación legacy. «CRUD presente» describe código, no escrituras probadas. Todos tienen controlador/vista o reutilización de vista documentada y entrada de menú; los lookups no tienen mantenimiento propio identificado.

| Módulo | Estado | Evidencia / pendiente |
|---|---|---|
| MG/Color | Migrado parcialmente | MaestrosService: CRUD; UPDATE sin comprobar filas. |
| MG/Uit | Migrado parcialmente | CRUD monto/vigencias; UPDATE sin comprobar filas. |
| MG/RemMinVital | Migrado parcialmente | CRUD monto/vigencias; UPDATE sin comprobar filas. |
| MG/CentroCosto | Migrado parcialmente | CRUD con empresa/filas; escritura y permisos sin validar. |
| MG/CondPago | Migrado parcialmente | Código desde días en moderno; legacy CondicionPagoBusiness.Insert usa correlativo por empresa. |
| MG/TipoCambio | Migrado parcialmente | Maestro/detalle diario; validación integral/permisos pendientes. |
| MG/FamiliaProducto | Migrado parcialmente | CRUD integrado de jerarquía; falta filtro de empresa y protección del cambio de clase. |
| MG/GrupoProducto | Migrado parcialmente | Integrado en FamiliaProducto; misma brecha de empresa. |
| MG/ClaseProducto | Migrado parcialmente | Integrado en FamiliaProducto; revisar coherencia de jerarquía. |
| MG/Producto y Servicios | Migrado parcialmente | SKU y detalles presentes; falta alta en listas de precios y aislamiento por empresa. |
| MG/ComboProducto | Migrado parcialmente | Cabecera/detalle/SKU; mismo efecto de listas de precios faltante. |
| MG/Proveedor | Migrado parcialmente | CRUD por empresa/ubigeo; falta consulta RUC externa y certificar JS. |
| MG/Transportista | Migrado parcialmente | Cabecera/unidades/conductores con pertenencia; escritura/permisos pendientes. |
| MG/TipoProducto | Solo consulta | GetTiposProductoLookupAsync; sin mantenimiento propio identificado. |
| MG/UnidadMedida | Solo consulta | GetUnidadesMedidaLookupAsync; sin mantenimiento propio identificado. |
| MG/TipoIGV | Solo consulta | GetTiposIgvLookupAsync; sin mantenimiento propio identificado. |
| MG/TablaGeneral | Excluido | Intacto por instrucción explícita. |
| AS/Periodo | Migrado parcialmente | SavePeriodoAsync omite regla legacy de un único período batch habilitado en alta. |
| TS/CuentaBanco | Migrado parcialmente | CRUD por empresa; listado TOP sin avance real de páginas. |
| MK/LeadSource | Migrado parcialmente | MKService CRUD/paginación; permisos/escritura no validados. |
| MK/TipoProfesion | Migrado parcialmente | CRUD/inactivación; permisos/escritura no validados. |
| MK/Oportunidad | Migrado parcialmente | CRUD/inactivación; permisos/escritura no validados. |
| MK/TipoVentaCliente | Migrado parcialmente | CRUD moderno; fuente legacy WebApi/TipoVentaCliente, sin pantalla homónima en áreas. |
| MK/Prospecto | Migrado parcialmente | Reutiliza Proveedor y agrega oportunidades; falta consulta RUC externa. |
| MK/Cliente | Migrado parcialmente | Reutiliza Proveedor; contactos/redes sociales legacy sin equivalente completo identificado. |
| MK/Merchandising | Solo consulta | Index/Detalle; faltan movimientos/productos/transacciones/compras del legacy. |
| RH/Personal | Migrado parcialmente | CRUD, remuneraciones, períodos, dependientes y planillas; revisar bajas, aislamiento y permisos. |
| RH/CargoPersonal | Migrado parcialmente | CRUD presente; escritura y permisos no validados. |
| RH/Reloj | Migrado parcialmente | CRUD presente; escritura y permisos no validados. |
| RH/Feriado | Migrado parcialmente | CRUD presente; escritura y permisos no validados. |
| RH/RegPensionario | Migrado parcialmente | Maestro; faltan esquemas y comisiones del legacy. |
| RH/TipoRemunera | Migrado parcialmente | CRUD presente; escritura y permisos no validados. |
| RH/HoraRemunerativa | Migrado parcialmente | CRUD presente; escritura y permisos no validados. |
| RH/Planilla | Migrado parcialmente | Maestro; faltan Listar/Grabar/EliminarPlanillaConceptoRemunerativo. |
| RH/VariableCalc | Migrado parcialmente | CRUD presente; motor consumidor y verificación de expresiones pendientes. |
| RH/ConceptoRemu | Migrado parcialmente | CRUD; faltan verificación de fórmulas y bases imponibles legacy. |
| RH/Marcacion | Migrado parcialmente | Edición/estado/verificado; faltan sincronización/exportación/relojes. IDs mediante MAX+1. |
| RH/Prestamo | Migrado parcialmente | Cabecera/cuotas; validar correlativo, moneda, reglas y permisos. |
| RH/RegVacacion | Migrado parcialmente | CRUD moderno; legacy incluye acciones heredadas de Personal que requieren aclaración. |
| RH/PlanillaProceso | Migrado parcialmente | Lote actual: lecturas verificadas, escrituras implementadas sin ejecutar, cálculo pendiente. |
| RH/PlanillaHora | Migrado parcialmente | Horas/importación/formato/personal/marcaciones; escritura/permisos no validados. |
| RH/PlanillaRegula | Migrado parcialmente | Regularizaciones con empresa/estado; escritura/permisos no validados. |
| RH/AccesoExt | Migrado parcialmente | Aprobar/desaprobar con estados/auditoría; escritura/permisos no validados. |
| RH/ReporteBoleta | Solo consulta | Consulta/impresión; revisar aislamiento y valores de empresa/días/horas predeterminados. |
| RH/TablaGen | Solo consulta | Sin controlador equivalente; lookups de país/ocupación en RHService. No es MG/TablaGeneral. |

## Prioridades siguientes

1. Motor de cálculo y configuración: Planilla-conceptos, fórmulas y esquemas/comisiones pensionarias. Validar resultados con autorización de escritura.
2. Seguridad transversal: AuthService contiene accesos alternativos y asignación incondicional de administrador; otros controladores sólo usan Authorize. No se usaron esos accesos para pruebas ni se reproducen sus valores aquí.
3. Producto/Combo: legacy ProductoBusiness.Insert crea lista_precio_producto inactiva, sin descuento, para listas activas de la empresa del tipo de producto, en la misma transacción. SaveProductoAsync/SaveComboCabeceraAsync omiten ese efecto. Conservar SKU {Grupo}{Clase}{Familia}{Correlativo:D3} existente.
4. CondPago: recuperar correlativo sin tocar TablaGeneral. Periodo: exclusividad de batch en alta.
5. Marcaciones/maestros RH incompletos; luego Inventario/Compras/Ventas/Producción, contrastando SP y transacciones antes de escribir lógica de stock.
6. Proveedor: script separado/configuración JSON presentes, sin eventos inline y requestSubmit protegido con HTMLFormElement. Pero @ts-nocheck desactiva diagnósticos y hay DOM sin guardas. No hay acceso a diagnósticos del IDE conectado. Módulo intacto.

## Validación

- Build base y final: dotnet build --no-restore, 0 errores y CS8602 preexistente en Areas/MG/Views/FamiliaProducto/Index.cshtml:170.
- SDK 11.0.100-rc.3.26522.148; target net10.0 y runtime 10 instalado. Sin cambios de SDK/dependencias.
- Proyecto temporal fuera del repo referenciando ensamblado real, con conexión que rechaza DML/SP/transacciones: **52 comprobaciones aprobadas** de consultas, totales, paginación, año, aislamiento, validaciones sin DB, permisos y sesión frente a claims obsoletos.
- Sólo se restauró el proyecto temporal de verificación; el ERP no necesitó restauración adicional.
- planilla-proceso.js: TypeScript allowJs/checkJs/noEmit y nulabilidad, 0 diagnósticos; declaraciones externas mínimas de Bootstrap/SweetAlert2. Runtime/TypeScript incluidos en VS Code, sin instalar paquetes.
- Razor con datos ficticios y endpoints de escritura simulados: HTTP 200; formulario hijo del diálogo, JSON y script presentes.
- Revisión automática bloqueó el navegador de prueba: «blocked by policy». Interacción visual y temas claro/oscuro no certificados.
- Pendiente: alta/edición/anulación contra base de prueba con autorización; concurrencia real, sesión completa, interacción visual y cálculo. Las comprobaciones no ejecutaron escrituras.

## Controladores legacy sin equivalente moderno

«Pendiente» acredita ausencia de implementación moderna, no que cada operación legacy esté completa. Seguir cada controlador a sus proxies/business/repositorios antes del siguiente lote.

| Módulo | Estado | Fuente legacy |
|---|---|---|
| AS/DocuComSerie | Pendiente | ESD.ERP.Web.v6/Areas/AS/Controllers/DocuComSerieController.cs |
| AS/Empresa | Pendiente | ESD.ERP.Web.v6/Areas/AS/Controllers/EmpresaController.cs |
| AS/IntegraCapital | Pendiente | ESD.ERP.Web.v6/Areas/AS/Controllers/IntegraCapitalController.cs |
| AS/IntegraInventa | Pendiente | ESD.ERP.Web.v6/Areas/AS/Controllers/IntegraInventaController.cs |
| AS/IntegraVenta | Pendiente | ESD.ERP.Web.v6/Areas/AS/Controllers/IntegraVentaController.cs |
| AS/ParametroSistema | Pendiente | ESD.ERP.Web.v6/Areas/AS/Controllers/ParametroSistemaController.cs |
| AS/Rol | Pendiente | ESD.ERP.Web.v6/Areas/AS/Controllers/RolController.cs |
| AS/Usuario | Pendiente | ESD.ERP.Web.v6/Areas/AS/Controllers/UsuarioController.cs |
| CM/OrdenComAuto | Pendiente | ESD.ERP.Web.v6/Areas/CM/Controllers/OrdenComAutoController.cs |
| CM/OrdenCompra | Pendiente | ESD.ERP.Web.v6/Areas/CM/Controllers/OrdenCompraController.cs |
| CM/PedidoComAuto | Pendiente | ESD.ERP.Web.v6/Areas/CM/Controllers/PedidoComAutoController.cs |
| CM/PedidoCompra | Pendiente | ESD.ERP.Web.v6/Areas/CM/Controllers/PedidoCompraController.cs |
| CM/ProveCompra | Pendiente | ESD.ERP.Web.v6/Areas/CM/Controllers/ProveCompraController.cs |
| CM/PuntoPedido | Pendiente | ESD.ERP.Web.v6/Areas/CM/Controllers/PuntoPedidoController.cs |
| CM/RepoValorizaComprob | Pendiente | ESD.ERP.Web.v6/Areas/CM/Controllers/RepoValorizaComprobController.cs |
| CM/ValorizaCompra | Pendiente | ESD.ERP.Web.v6/Areas/CM/Controllers/ValorizaCompraController.cs |
| FE/DocEmitido | Pendiente | ESD.ERP.Web.v6/Areas/FE/Controllers/DocEmitidoController.cs |
| FE/ResumenDiario | Pendiente | ESD.ERP.Web.v6/Areas/FE/Controllers/ResumenDiarioController.cs |
| IV/Almacen | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/AlmacenController.cs |
| IV/ApeCierre | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/ApeCierreController.cs |
| IV/ConsultaLlegada | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/ConsultaLlegadaController.cs |
| IV/ConsultaSaldo | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/ConsultaSaldoController.cs |
| IV/ConsumoIntAuto | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/ConsumoIntAutoController.cs |
| IV/ConsumoInt | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/ConsumoIntController.cs |
| IV/ControlMermaAuto | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/ControlMermaAutoController.cs |
| IV/ControlMerma | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/ControlMermaController.cs |
| IV/GuiaRemiAuto | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/GuiaRemiAutoController.cs |
| IV/GuiaRemi | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/GuiaRemiController.cs |
| IV/GuiaRemOtro | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/GuiaRemOtroController.cs |
| IV/MovAlmacen | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/MovAlmacenController.cs |
| IV/MovAlmacenSaldo | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/MovAlmacenSaldoController.cs |
| IV/ReclasItemAuto | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/ReclasItemAutoController.cs |
| IV/ReclasItem | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/ReclasItemController.cs |
| IV/RepoGuiaRemi | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/RepoGuiaRemiController.cs |
| IV/RepoKardex | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/RepoKardexController.cs |
| IV/RepoKardexVal | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/RepoKardexValController.cs |
| IV/RepoMoviAlma | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/RepoMoviAlmaController.cs |
| IV/RepoSaldo | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/RepoSaldoController.cs |
| IV/RepoSaldoVal | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/RepoSaldoValController.cs |
| IV/Reproceso | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/ReprocesoController.cs |
| IV/Transaccion | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/TransaccionController.cs |
| IV/TransfeIngreso | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/TransfeIngresoController.cs |
| IV/TransfeModifica | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/TransfeModificaController.cs |
| IV/TransfeSalida | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/TransfeSalidaController.cs |
| IV/Valoriza | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/ValorizaController.cs |
| IV/VolumenExisten | Pendiente | ESD.ERP.Web.v6/Areas/IV/Controllers/VolumenExistenController.cs |
| MK/EntregaCatalogo | Bloqueado: acciones comentadas; carpeta MK declara área VT | ESD.ERP.Web.v6/Areas/MK/Controllers/EntregaCatalogoController.cs |
| PD/AsigProd | Pendiente | ESD.ERP.Web.v6/Areas/PD/Controllers/AsigProdController.cs |
| PD/Despacho | Pendiente | ESD.ERP.Web.v6/Areas/PD/Controllers/DespachoController.cs |
| PD/OrdenProdu | Pendiente | ESD.ERP.Web.v6/Areas/PD/Controllers/OrdenProduController.cs |
| PD/ProceProduccion | Pendiente | ESD.ERP.Web.v6/Areas/PD/Controllers/ProceProduccionController.cs |
| PD/RepoProceProd | Pendiente | ESD.ERP.Web.v6/Areas/PD/Controllers/RepoProceProdController.cs |
| PD/RequeInsumo | Pendiente | ESD.ERP.Web.v6/Areas/PD/Controllers/RequeInsumoController.cs |
| PD/RutaProduccion | Pendiente | ESD.ERP.Web.v6/Areas/PD/Controllers/RutaProduccionController.cs |
| RH/PlanillaCalculo | Pendiente: motor real existe; sustituto falso retirado | ESD.ERP.Web.v6/Areas/RH/Controllers/PlanillaCalculoController.cs |
| TS/Bancariza | Pendiente | ESD.ERP.Web.v6/Areas/TS/Controllers/BancarizaController.cs |
| VT/AnticipoCliente | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/AnticipoClienteController.cs |
| VT/CajaCobranza | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/CajaCobranzaController.cs |
| VT/Caja | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/CajaController.cs |
| VT/CambioBoleta | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/CambioBoletaController.cs |
| VT/CanjeDocu | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/CanjeDocuController.cs |
| VT/CanjeSelectivo | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/CanjeSelectivoController.cs |
| VT/ComproAnticipo | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/ComproAnticipoController.cs |
| VT/ComproAutoriza | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/ComproAutorizaController.cs |
| VT/Comprobante | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/ComprobanteController.cs |
| VT/CortesGratis | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/CortesGratisController.cs |
| VT/CotizaAutoriza | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/CotizaAutorizaController.cs |
| VT/Cotizacion | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/CotizacionController.cs |
| VT/CuadroCosto | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/CuadroCostoController.cs |
| VT/Detraccion | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/DetraccionController.cs |
| VT/GrupoCliente | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/GrupoClienteController.cs |
| VT/GuiaRemVta | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/GuiaRemVtaController.cs |
| VT/ListaPrecio | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/ListaPrecioController.cs |
| VT/MedioPago | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/MedioPagoController.cs |
| VT/NotaCredito | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/NotaCreditoController.cs |
| VT/NotaCreditoInterno | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/NotaCreditoInternoController.cs |
| VT/Pedido | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/PedidoController.cs |
| VT/RepoCanje | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/RepoCanjeController.cs |
| VT/RepoDocuReserva | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/RepoDocuReservaController.cs |
| VT/RepoDocuTemporal | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/RepoDocuTemporalController.cs |
| VT/RepoLiquiCaja | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/RepoLiquiCajaController.cs |
| VT/RepoPedidoProd | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/RepoPedidoProdController.cs |
| VT/RepoRegVentas | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/RepoRegVentasController.cs |
| VT/RepoUltimaVenta | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/RepoUltimaVentaController.cs |
| VT/RepoVentaPorCliente | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/RepoVentaPorClienteController.cs |
| VT/RepoVentaPorProd | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/RepoVentaPorProdController.cs |
| VT/RepoVentaPorVend | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/RepoVentaPorVendController.cs |
| VT/Vendedor | Pendiente | ESD.ERP.Web.v6/Areas/VT/Controllers/VendedorController.cs |
