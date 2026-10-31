# Migración Módulo Inventario: Transferencias entre Almacenes (IV/TransfeSalida e IV/TransfeIngreso)
Fecha: 31/10/2026

## 1. Resumen
Se completó la migración de los módulos **Transferencias de Salida** (`IV/TransfeSalida`) y **Recepciones de Transferencias** (`IV/TransfeIngreso`), correspondientes al ciclo completo de traslado de mercadería entre almacenes de la empresa:
1. **Envío / Salida**: Registra la salida en estado `2 = EN TRÁNSITO` con origen, destino, guía de remisión e items.
2. **Recepción / Ingreso**: El almacén de destino visualiza los traslados entrantes en tránsito, revisa los productos y ejecuta la recepción física, generando automáticamente un movimiento de `INGRESO` atómico y cerrando el tránsito (`MOVIMIENTO`).

## 2. Componentes Migrados

1. **Modelos (`Models/TransferenciaViewModels.cs`)**:
   - `TransfeSalidaItemModel : MaestroEmpresaModel`: Encapsula envío con almacén origen, destino, guía, estado (`2 = TRANSITO`, `1 = RECEPCIONADA`, `3 = ANULADA`), items y valorización.
   - `TransfeSalidaPageModel : ListadoPaginadoModel` y `TransfeSalidaGuardarSolicitud`.
   - `TransfeIngresoItemModel : MaestroEmpresaModel`: Bandeja de recepciones con filtros por origen y estado.
   - `TransfeIngresoPageModel : ListadoPaginadoModel` y `RecepcionarTransferenciaSolicitud`.

2. **Capa de Servicios (`Services/IInventarioService.cs` y `Services/InventarioService.Transferencia.cs`)**:
   - `GetTransferenciasSalidaAsync`: Consulta de salidas de transferencia (`tipo_mov_alma_id = 2`, `trans_alma = 'S11'`).
   - `SaveTransferenciaSalidaAsync`: Validación estricta origen != destino, correlativo atómico `S000000000`, asignación de `mes_contable` e inserción con `sp_mov_almacen_insertar`.
   - `AnularTransferenciaSalidaAsync`: Bloqueo `UPDLOCK, HOLDLOCK` y baja lógica con `sp_mov_almacen_eliminar` únicamente si sigue en tránsito.
   - `GetTransferenciasIngresoAsync`: Consulta de transferencias dirigidas a almacenes de la empresa/sede activa.
   - `RecepcionarTransferenciaAsync`: Transacción Serializable que genera correlativo `I000000000` en el almacén destino, crea movimiento de ingreso (`sp_mov_almacen_insertar`), replica items con `sp_mov_alm_prod_insertar` y actualiza la salida a estado `1 = MOVIMIENTO`.

3. **Controladores (`Areas/IV/Controllers/TransfeSalidaController.cs` y `TransfeIngresoController.cs`)**:
   - Heredan de `InventarioContextController` con permisos `iv / transfeSalida/index` e `iv / transfeIngreso/index`.
   - Endpoints seguros para mutación con validación anti-falsificación CSRF.

4. **Vistas y Clientes (`Index.cshtml` en ambas áreas y scripts `transfe-salida.js` y `transfe-ingreso.js`)**:
   - Modal `#tsal-modal` con pestañas para cabecera e items.
   - Modal `#ting-modal` para confirmación de recepción con fecha y observación.
   - Alertas y confirmaciones mediante SweetAlert2.

5. **Navegación (`Models/SidebarNavigation.cs`)**:
   - Registrado en: Inventario → Procesos → `Transferencias` → `Transferencia de salida` y `Recepción de transferencia`.

## 3. Verificación
- Compilación `dotnet build`: **0 errores**.

