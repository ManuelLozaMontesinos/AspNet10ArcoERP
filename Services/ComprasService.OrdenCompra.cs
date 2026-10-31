using System.Data;
using System.Globalization;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class ComprasService
{
    private const string OrdenCompraSelect = @"SELECT oc.orden_co_id Id,
        oc.periodo_emp_sede_id PeriodoEmpresaSedeId,
        RTRIM(oc.orden_co) Codigo,
        oc.fecha Fecha,
        oc.tipo_producto_id TipoProductoId,
        RTRIM(tp.nombre) TipoProductoNombre,
        oc.pedido_co_id PedidoCompraId,
        RTRIM(ISNULL(pc.pedido_co, '')) PedidoCompraCodigo,
        oc.proveedor_id ProveedorId,
        RTRIM(p.razon_social) ProveedorNombre,
        COALESCE(NULLIF(p.numero_ruc,''), p.num_doc_iden, '') ProveedorDocumento,
        oc.fecha_entrega FechaEntrega,
        oc.moneda_id MonedaId,
        RTRIM(m.simbolo) MonedaSimbolo,
        RTRIM(m.nombre) MonedaNombre,
        oc.tipo_cambio TipoCambio,
        oc.impuesto_tasa_id ImpuestoTasaId,
        ISNULL(it.tasa, 18.0) ImpuestoTasa,
        oc.monto_bruto MontoBruto,
        oc.monto_impuesto MontoImpuesto,
        oc.monto_total MontoTotal,
        RTRIM(ISNULL(oc.plazo_entrega, '')) PlazoEntrega,
        RTRIM(ISNULL(oc.lugar_entrega, '')) LugarEntrega,
        oc.cond_pago_id CondicionPagoId,
        RTRIM(ISNULL(cp.nombre, '')) CondicionPagoNombre,
        RTRIM(ISNULL(oc.observa, '')) Observacion,
        oc.permite_pago_p PermitePagoParcial,
        oc.es_modifica EsModificacion,
        oc.estado_aproba_id EstadoAprobacionId,
        RTRIM(ea.estado_aproba) EstadoAprobacionCodigo,
        RTRIM(ea.nombre) EstadoAprobacionNombre,
        oc.estado_orden_co_id EstadoOrdenCompraId,
        RTRIM(eoc.estado_orden_co) EstadoOrdenCompraCodigo,
        RTRIM(eoc.nombre) EstadoOrdenCompraNombre,
        oc.estado Estado,
        COALESCE(oc.fecha_modifica, oc.fecha_creacion) FechaRevision,
        (SELECT COUNT(1) FROM orden_c_prod ocp WHERE ocp.orden_co_id = oc.orden_co_id AND ocp.estado = 'A') CantidadItems
        FROM orden_co oc
        JOIN tipo_producto tp ON tp.tipo_producto_id = oc.tipo_producto_id
        JOIN proveedor p ON p.proveedor_id = oc.proveedor_id
        JOIN moneda m ON m.moneda_id = oc.moneda_id
        JOIN estado_orden_co eoc ON eoc.estado_orden_co_id = oc.estado_orden_co_id
        LEFT JOIN estado_aproba ea ON ea.estado_aproba_id = oc.estado_aproba_id
        LEFT JOIN impuesto_tasa it ON it.impuesto_tasa_id = oc.impuesto_tasa_id
        LEFT JOIN cond_pago cp ON cp.cond_pago_id = oc.cond_pago_id
        LEFT JOIN pedido_co pc ON pc.pedido_co_id = oc.pedido_co_id ";

    public async Task<OrdenCompraPageModel> GetOrdenesCompraAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        string? codigo = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? tipoProductoId = null,
        int? estadoOrdenCompraId = null,
        int? estadoAprobacionId = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new OrdenCompraPageModel
        {
            Search = search,
            Codigo = codigo,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            TipoProductoId = tipoProductoId,
            EstadoOrdenCompraId = estadoOrdenCompraId,
            EstadoAprobacionId = estadoAprobacionId
        };

        var columns = new Dictionary<string, string>
        {
            ["codigo"] = "oc.orden_co",
            ["fecha"] = "oc.fecha",
            ["tipo"] = "tp.nombre",
            ["proveedor"] = "p.razon_social",
            ["total"] = "oc.monto_total",
            ["aprobacion"] = "ea.nombre",
            ["estado"] = "eoc.nombre"
        };
        model.Ordenar(sort, dir, columns.Keys, "codigo");
        model.Paginar(0, page, top);

        if (empresaId <= 0 || contextoId <= 0)
        {
            model.Errores.Add("Seleccione una empresa y sede válidas.");
            return model;
        }

        using var conn = factory.CreateConnection();

        model.TiposProducto = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT tipo_producto_id Id, RTRIM(tipo_producto) Codigo, RTRIM(nombre) Nombre
            FROM tipo_producto WHERE empresa_id = @EmpresaId AND estado = 'A' ORDER BY nombre",
            new { EmpresaId = empresaId })).AsList();

        model.EstadosOrdenCompra = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT estado_orden_co_id Id, RTRIM(estado_orden_co) Codigo, RTRIM(nombre) Nombre
            FROM estado_orden_co WHERE estado = 'A' ORDER BY estado_orden_co_id")).AsList();

        model.EstadosAprobacion = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT estado_aproba_id Id, RTRIM(estado_aproba) Codigo, RTRIM(nombre) Nombre
            FROM estado_aproba WHERE estado = 'A' ORDER BY estado_aproba_id")).AsList();

        model.Monedas = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT moneda_id Id, RTRIM(simbolo) Codigo, RTRIM(nombre) Nombre
            FROM moneda WHERE estado = 'A' ORDER BY moneda_id")).AsList();

        model.CondicionesPago = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT cond_pago_id Id, RTRIM(cond_pago) Codigo, RTRIM(nombre) Nombre
            FROM cond_pago WHERE empresa_id = @EmpresaId AND estado = 'A' ORDER BY nombre",
            new { EmpresaId = empresaId })).AsList();

        var where = new List<string> { "oc.periodo_emp_sede_id = @ContextoId", "oc.estado = 'A'" };
        var args = new DynamicParameters(new
        {
            EmpresaId = empresaId,
            ContextoId = contextoId,
            Codigo = string.IsNullOrWhiteSpace(codigo) ? null : $"%{codigo.Trim()}%",
            Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%",
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            TipoProductoId = tipoProductoId,
            EstadoOrdenCompraId = estadoOrdenCompraId,
            EstadoAprobacionId = estadoAprobacionId
        });

        if (!string.IsNullOrWhiteSpace(codigo)) where.Add("oc.orden_co LIKE @Codigo");
        if (fechaDesde.HasValue) where.Add("oc.fecha >= @FechaDesde");
        if (fechaHasta.HasValue) where.Add("oc.fecha <= @FechaHasta");
        if (tipoProductoId.HasValue) where.Add("oc.tipo_producto_id = @TipoProductoId");
        if (estadoOrdenCompraId.HasValue) where.Add("oc.estado_orden_co_id = @EstadoOrdenCompraId");
        if (estadoAprobacionId.HasValue) where.Add("oc.estado_aproba_id = @EstadoAprobacionId");

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add(@"(oc.orden_co LIKE @Search
                OR p.razon_social LIKE @Search
                OR p.num_doc_iden LIKE @Search
                OR p.numero_ruc LIKE @Search
                OR oc.observa LIKE @Search)");
        }

        string whereSql = " WHERE " + string.Join(" AND ", where);
        string countSql = "SELECT COUNT(1) FROM orden_co oc JOIN tipo_producto tp ON tp.tipo_producto_id = oc.tipo_producto_id JOIN proveedor p ON p.proveedor_id = oc.proveedor_id JOIN moneda m ON m.moneda_id = oc.moneda_id JOIN estado_orden_co eoc ON eoc.estado_orden_co_id = oc.estado_orden_co_id LEFT JOIN estado_aproba ea ON ea.estado_aproba_id = oc.estado_aproba_id " + whereSql;

        int total = await conn.ExecuteScalarAsync<int>(countSql, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        string orderSql = $" ORDER BY {columns[model.Sort]} {model.Dir}, oc.orden_co_id DESC ";
        string dataSql = OrdenCompraSelect + whereSql + orderSql + " OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";

        model.Items = (await conn.QueryAsync<OrdenCompraItemModel>(dataSql, args)).AsList();
        model.VisibleCount = model.Items.Count;
        return model;
    }

    public async Task<OrdenCompraDetalleModel?> GetOrdenCompraDetalleAsync(int empresaId, int contextoId, int id)
    {
        if (empresaId <= 0 || contextoId <= 0) return null;
        using var conn = factory.CreateConnection();

        var detalle = new OrdenCompraDetalleModel();

        detalle.TiposProducto = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT tipo_producto_id Id, RTRIM(tipo_producto) Codigo, RTRIM(nombre) Nombre
            FROM tipo_producto WHERE empresa_id = @EmpresaId AND estado = 'A' ORDER BY nombre",
            new { EmpresaId = empresaId })).AsList();

        detalle.Monedas = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT moneda_id Id, RTRIM(simbolo) Codigo, RTRIM(nombre) Nombre
            FROM moneda WHERE estado = 'A' ORDER BY moneda_id")).AsList();

        detalle.CondicionesPago = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT cond_pago_id Id, RTRIM(cond_pago) Codigo, RTRIM(nombre) Nombre
            FROM cond_pago WHERE empresa_id = @EmpresaId AND estado = 'A' ORDER BY nombre",
            new { EmpresaId = empresaId })).AsList();

        detalle.TasasImpuesto = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT impuesto_tasa_id Id, CAST(tasa AS varchar(20)) Codigo, RTRIM(nombre) Nombre
            FROM impuesto_tasa WHERE estado = 'A' ORDER BY impuesto_tasa_id")).AsList();

        detalle.Proveedores = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT TOP 50 proveedor_id Id, RTRIM(COALESCE(NULLIF(numero_ruc,''), num_doc_iden, '')) Codigo, RTRIM(razon_social) Nombre
            FROM proveedor WHERE empresa_id = @EmpresaId AND estado = 'A' ORDER BY razon_social",
            new { EmpresaId = empresaId })).AsList();

        if (id <= 0)
        {
            var defaultTipo = detalle.TiposProducto.FirstOrDefault(t => t.Codigo == "01") ?? detalle.TiposProducto.FirstOrDefault();
            var defaultProv = detalle.Proveedores.FirstOrDefault(p => p.Codigo == "00000000000" || p.Nombre.Contains("GENERICO", StringComparison.OrdinalIgnoreCase)) ?? detalle.Proveedores.FirstOrDefault();
            var defaultMoneda = detalle.Monedas.FirstOrDefault(m => m.Codigo == "S/") ?? detalle.Monedas.FirstOrDefault();
            var defaultCond = detalle.CondicionesPago.FirstOrDefault() ?? new LookupItemModel { Id = 1 };
            var defaultTasa = detalle.TasasImpuesto.FirstOrDefault(t => t.Nombre.Contains("IGV", StringComparison.OrdinalIgnoreCase)) ?? detalle.TasasImpuesto.FirstOrDefault();

            detalle.Cabecera = new OrdenCompraItemModel
            {
                Id = 0,
                PeriodoEmpresaSedeId = contextoId,
                Fecha = DateTime.Today,
                TipoProductoId = defaultTipo?.Id ?? 0,
                TipoProductoNombre = defaultTipo?.Nombre ?? "",
                ProveedorId = defaultProv?.Id ?? 0,
                ProveedorNombre = defaultProv?.Nombre ?? "",
                MonedaId = defaultMoneda?.Id ?? 1,
                MonedaSimbolo = defaultMoneda?.Codigo ?? "S/",
                TipoCambio = 1.0m,
                CondicionPagoId = defaultCond.Id,
                ImpuestoTasaId = defaultTasa?.Id ?? 1,
                ImpuestoTasa = 18m,
                EstadoOrdenCompraId = 1,
                EstadoOrdenCompraNombre = "EN EDICIÓN",
                EsModificacion = "N",
                PermitePagoParcial = "S"
            };
            return detalle;
        }

        var cabecera = await conn.QuerySingleOrDefaultAsync<OrdenCompraItemModel>(
            OrdenCompraSelect + " WHERE oc.orden_co_id = @Id AND oc.periodo_emp_sede_id = @ContextoId",
            new { Id = id, ContextoId = contextoId });

        if (cabecera is null) return null;
        detalle.Cabecera = cabecera;
        detalle.Productos = await GetOrdenCompraProductosAsync(empresaId, id);

        return detalle;
    }

    public async Task<(bool success, string message, int id)> SaveOrdenCompraAsync(int empresaId, int contextoId, int userId, OrdenCompraGuardarSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.TipoProductoId <= 0 || model.ProveedorId <= 0 || model.MonedaId <= 0 || model.CondicionPagoId <= 0 || model.ImpuestoTasaId <= 0)
            return (false, "Complete los datos requeridos de la orden de compra.", 0);

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId))
            return (false, "La empresa no se encuentra activa.", 0);

        if (model.Id > 0)
        {
            var old = await conn.QuerySingleOrDefaultAsync<OrdenCompraItemModel>(
                OrdenCompraSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE oc.orden_co_id = @Id AND oc.periodo_emp_sede_id = @ContextoId",
                new { Id = model.Id, ContextoId = contextoId }, tx);

            if (old is null || old.Estado != "A" || (!string.IsNullOrEmpty(model.Revision) && old.Revision != model.Revision))
                return (false, "La orden de compra no se encuentra disponible o fue modificada por otro usuario.", model.Id);

            if (old.EstadoOrdenCompraId is 4) // Anulada
                return (false, "No se puede editar una orden de compra anulada.", model.Id);

            await conn.ExecuteAsync("sp_orden_co_actualizar", new
            {
                OrdenCompraId = model.Id,
                Fecha = model.Fecha,
                ProveedorId = model.ProveedorId,
                FechaEntrega = model.FechaEntrega,
                MonedaId = model.MonedaId,
                TipoCambio = model.TipoCambio > 0 ? model.TipoCambio : 1.0m,
                ImpuestoTasaId = model.ImpuestoTasaId,
                PlazoEntrega = model.PlazoEntrega?.Trim() ?? "",
                LugarEntrega = model.LugarEntrega?.Trim() ?? "",
                CondicionPagoId = model.CondicionPagoId,
                Observacion = model.Observacion?.Trim() ?? "",
                EsModificacion = old.EsModificacion,
                EstadoOrdenCompraId = old.EstadoOrdenCompraId,
                EstadoOrdenCompraAnteriorId = (int?)null,
                UsuarioAccionId = userId
            }, tx, commandType: CommandType.StoredProcedure);

            tx.Commit();
            return (true, $"Orden de compra {old.Codigo} actualizada correctamente.", model.Id);
        }
        else
        {
            // Correlativo 0020 (ORDEN COMPRA) por empresa
            var counter = await conn.QuerySingleOrDefaultAsync<(int Id, int Correlativo)?>(@"
                SELECT empresa_corre_id Id, correlativo Correlativo
                FROM empresa_corre WITH (UPDLOCK,HOLDLOCK)
                WHERE empresa_id = @EmpresaId AND tabla_id = 20",
                new { EmpresaId = empresaId }, tx);

            int nextVal;
            if (counter is null)
            {
                int maxOrden = await conn.ExecuteScalarAsync<int>(@"
                    SELECT ISNULL(MAX(TRY_CAST(orden_co AS int)), 0) FROM orden_co WHERE ISNUMERIC(orden_co) = 1");
                nextVal = maxOrden + 1;

                await InsertAsync(conn, tx, "sp_empresa_corre_insertar", new
                {
                    EmpresaId = empresaId,
                    TablaId = 20,
                    Correlativo = nextVal,
                    UsuarioAccionId = userId
                });
            }
            else
            {
                nextVal = counter.Value.Correlativo + 1;
                await conn.ExecuteAsync("sp_empresa_corre_actualizar", new
                {
                    EmpresaCorrelativoId = counter.Value.Id,
                    Correlativo = nextVal,
                    UsuarioAccionId = userId
                }, tx, commandType: CommandType.StoredProcedure);
            }

            string code = nextVal.ToString("00000", CultureInfo.InvariantCulture);

            int newId = await InsertAsync(conn, tx, "sp_orden_co_insertar", new
            {
                PeriodoEmpresaSedeId = contextoId,
                OrdenCompra = code,
                Fecha = model.Fecha,
                TipoProductoId = model.TipoProductoId,
                PedidoCompraId = model.PedidoCompraId,
                ProveedorId = model.ProveedorId,
                FechaEntrega = model.FechaEntrega,
                MonedaId = model.MonedaId,
                TipoCambio = model.TipoCambio > 0 ? model.TipoCambio : 1.0m,
                ImpuestoTasaId = model.ImpuestoTasaId,
                MontoBruto = 0m,
                MontoImpuesto = 0m,
                MontoTotal = 0m,
                PlazoEntrega = model.PlazoEntrega?.Trim() ?? "",
                LugarEntrega = model.LugarEntrega?.Trim() ?? "",
                CondicionPagoId = model.CondicionPagoId,
                Observacion = model.Observacion?.Trim() ?? "",
                PermitePagoParcial = "S",
                EsModificacion = "N",
                EstadoOrdenCompraId = 1, // En Edición
                UsuarioAccionId = userId
            });

            // Si venía de un pedido_co, actualizar su estado a 3 = CON ORDEN DE COMPRA
            if (model.PedidoCompraId.HasValue && model.PedidoCompraId.Value > 0)
            {
                await conn.ExecuteAsync(@"
                    UPDATE pedido_co
                    SET estado_pedido_co_id = 3,
                        usuario_modifica_id = @UserId,
                        fecha_modifica = GETDATE()
                    WHERE pedido_co_id = @PedidoId",
                    new { PedidoId = model.PedidoCompraId.Value, UserId = userId }, tx);
            }

            tx.Commit();
            return (true, $"Orden de Compra N° {code} registrada con éxito.", newId);
        }
    }

    public async Task<(bool success, string message, int id)> AnularOrdenCompraAsync(int empresaId, int contextoId, int userId, int id, string revision)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0) return (false, "Identificador no válido.", 0);
        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return (false, "La empresa no está activa.", 0);

        var old = await conn.QuerySingleOrDefaultAsync<OrdenCompraItemModel>(
            OrdenCompraSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE oc.orden_co_id = @Id AND oc.periodo_emp_sede_id = @ContextoId",
            new { Id = id, ContextoId = contextoId }, tx);

        if (old is null || old.Estado != "A" || (!string.IsNullOrEmpty(revision) && old.Revision != revision))
            return (false, "La orden de compra no se encuentra disponible o fue modificada.", id);

        if (old.EstadoOrdenCompraId is 3 or 4) // Atendida o Anulada
            return (false, "No se puede anular una orden de compra atendida o ya anulada.", id);

        await conn.ExecuteAsync("sp_orden_co_eliminar", new
        {
            OrdenCompraId = id,
            UsuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        // Si tenía un requerimiento de compra asociado, devolver el requerimiento a estado PEDIDO (2)
        if (old.PedidoCompraId.HasValue && old.PedidoCompraId.Value > 0)
        {
            await conn.ExecuteAsync(@"
                UPDATE pedido_co
                SET estado_pedido_co_id = 2,
                    usuario_modifica_id = @UserId,
                    fecha_modifica = GETDATE()
                WHERE pedido_co_id = @PedidoId",
                new { PedidoId = old.PedidoCompraId.Value, UserId = userId }, tx);
        }

        tx.Commit();
        return (true, $"Orden de compra {old.Codigo} anulada correctamente.", id);
    }

    public async Task<(bool success, string message, int id)> EnviarAprobacionOrdenCompraAsync(int empresaId, int contextoId, int userId, int id, string revision)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0) return (false, "Identificador no válido.", 0);
        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return (false, "La empresa no está activa.", 0);

        var old = await conn.QuerySingleOrDefaultAsync<OrdenCompraItemModel>(
            OrdenCompraSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE oc.orden_co_id = @Id AND oc.periodo_emp_sede_id = @ContextoId",
            new { Id = id, ContextoId = contextoId }, tx);

        if (old is null || old.Estado != "A" || (!string.IsNullOrEmpty(revision) && old.Revision != revision))
            return (false, "La orden de compra no se encuentra disponible o fue modificada.", id);

        var countItems = await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1) FROM orden_c_prod WHERE orden_co_id = @Id AND estado = 'A'",
            new { Id = id }, tx);

        if (countItems == 0)
            return (false, "No puede enviar a aprobación una orden de compra sin productos.", id);

        // Estado 2 = ORDEN, EstadoAprobacion 1 = PENDIENTE DE APROBACIÓN
        await conn.ExecuteAsync(@"
            UPDATE orden_co
            SET estado_orden_co_id = 2,
                estado_aproba_id = 1,
                fecha_aproba = NULL,
                usuario_modifica_id = @UserId,
                fecha_modifica = GETDATE()
            WHERE orden_co_id = @Id",
            new { Id = id, UserId = userId }, tx);

        tx.Commit();
        return (true, $"Orden de compra {old.Codigo} enviada a aprobación con éxito.", id);
    }

    public async Task<(bool success, string message, int id)> CambiarEstadoOrdenCompraAsync(int empresaId, int contextoId, int userId, CambiarEstadoOrdenCompraSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.Id <= 0 || model.EstadoOrdenCompraId <= 0)
            return (false, "Datos no válidos.", 0);

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return (false, "La empresa no está activa.", 0);

        var old = await conn.QuerySingleOrDefaultAsync<OrdenCompraItemModel>(
            OrdenCompraSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE oc.orden_co_id = @Id AND oc.periodo_emp_sede_id = @ContextoId",
            new { Id = model.Id, ContextoId = contextoId }, tx);

        if (old is null || old.Estado != "A" || (!string.IsNullOrEmpty(model.Revision) && old.Revision != model.Revision))
            return (false, "La orden no se encuentra disponible o fue modificada.", model.Id);

        await conn.ExecuteAsync("sp_orden_co_cambiarEstado", new
        {
            OrdenCompraId = model.Id,
            EstadoOrdenCompraId = model.EstadoOrdenCompraId,
            UsuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, $"Estado de la orden {old.Codigo} actualizado correctamente.", model.Id);
    }

    public async Task<List<OrdenCompraProductoItemModel>> GetOrdenCompraProductosAsync(int empresaId, int ordenCompraId)
    {
        if (empresaId <= 0 || ordenCompraId <= 0) return new();
        using var conn = factory.CreateConnection();

        return (await conn.QueryAsync<OrdenCompraProductoItemModel>(@"
            SELECT ocp.orden_c_prod_id Id,
                ocp.orden_co_id OrdenCompraId,
                ocp.producto_id ProductoId,
                RTRIM(ISNULL(p.producto, '')) ProductoCodigo,
                RTRIM(ocp.descripcion) Descripcion,
                RTRIM(ISNULL(um.abrev, 'UND')) UnidadMedida,
                ocp.cantidad Cantidad,
                ocp.cant_ing CantidadIngresada,
                ocp.valor_unitario ValorUnitario,
                ocp.precio_unitario PrecioUnitario,
                ocp.monto_bruto MontoBruto,
                ocp.monto_impuesto MontoImpuesto,
                ocp.monto_total MontoTotal,
                ocp.es_adicional EsAdicional,
                ocp.estado Estado,
                COALESCE(ocp.fecha_modifica, ocp.fecha_creacion) FechaRevision
            FROM orden_c_prod ocp
            LEFT JOIN producto p ON p.producto_id = ocp.producto_id
            LEFT JOIN unidad_medida um ON um.unidad_medida_id = p.unidad_medida_id
            WHERE ocp.orden_co_id = @OrdenCompraId AND ocp.estado = 'A'
            ORDER BY ocp.orden_c_prod_id",
            new { OrdenCompraId = ordenCompraId })).AsList();
    }

    public async Task<(bool success, string message, int id)> SaveOrdenCompraProductoAsync(int empresaId, int contextoId, int userId, OrdenCompraProductoGuardarSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.OrdenCompraId <= 0 || model.ProductoId <= 0 || model.Cantidad <= 0m)
            return (false, "Seleccione un producto y cantidad válida.", 0);

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return (false, "La empresa no se encuentra activa.", 0);

        var cab = await conn.QuerySingleOrDefaultAsync<OrdenCompraItemModel>(
            OrdenCompraSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE oc.orden_co_id = @Id AND oc.periodo_emp_sede_id = @ContextoId",
            new { Id = model.OrdenCompraId, ContextoId = contextoId }, tx);

        if (cab is null || cab.Estado != "A")
            return (false, "La orden de compra no se encuentra disponible.", 0);

        if (cab.EstadoOrdenCompraId is 3 or 4)
            return (false, "No se pueden modificar productos en una orden atendida o anulada.", 0);

        decimal valorUnitario = model.ValorUnitario;
        decimal precioUnitario = model.PrecioUnitario;

        decimal tasaFactor = 1m + (cab.ImpuestoTasa / 100m);
        if (precioUnitario > 0m && valorUnitario == 0m)
        {
            valorUnitario = Math.Round(precioUnitario / tasaFactor, 4);
        }
        else if (valorUnitario > 0m && precioUnitario == 0m)
        {
            precioUnitario = Math.Round(valorUnitario * tasaFactor, 4);
        }

        decimal montoBruto = Math.Round(model.Cantidad * valorUnitario, 2);
        decimal montoTotal = Math.Round(model.Cantidad * precioUnitario, 2);
        decimal montoImpuesto = Math.Round(montoTotal - montoBruto, 2);

        int itemId;
        if (model.Id > 0)
        {
            await conn.ExecuteAsync("sp_orden_c_prod_actualizar", new
            {
                OrdenCompraProductoId = model.Id,
                Descripcion = model.Descripcion.Trim(),
                Cantidad = model.Cantidad,
                ValorUnitario = valorUnitario,
                PrecioUnitario = precioUnitario,
                MontoBruto = montoBruto,
                MontoImpuesto = montoImpuesto,
                MontoTotal = montoTotal,
                UsuarioAccionId = userId
            }, tx, commandType: CommandType.StoredProcedure);
            itemId = model.Id;
        }
        else
        {
            itemId = await InsertAsync(conn, tx, "sp_orden_c_prod_insertar", new
            {
                OrdenCompraId = model.OrdenCompraId,
                ProductoId = model.ProductoId,
                Descripcion = model.Descripcion.Trim(),
                Cantidad = model.Cantidad,
                CantidadIngresada = 0m,
                ValorUnitario = valorUnitario,
                PrecioUnitario = precioUnitario,
                MontoBruto = montoBruto,
                MontoImpuesto = montoImpuesto,
                MontoTotal = montoTotal,
                EsAdicional = model.EsAdicional == "S" ? "S" : "N",
                UsuarioAccionId = userId
            });
        }

        // Recalcular montos de cabecera automáticamente
        await conn.ExecuteAsync("sp_orden_co_actualizarMonto", new
        {
            OrdenCompraId = model.OrdenCompraId,
            UsuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, "Item de orden de compra guardado.", itemId);
    }

    public async Task<(bool success, string message, int id)> EliminarOrdenCompraProductoAsync(int empresaId, int contextoId, int userId, int id)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0) return (false, "Identificador no válido.", 0);
        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return (false, "La empresa no está activa.", 0);

        var item = await conn.QuerySingleOrDefaultAsync<OrdenCompraProductoItemModel>(@"
            SELECT ocp.orden_c_prod_id Id, ocp.orden_co_id OrdenCompraId
            FROM orden_c_prod ocp WITH (UPDLOCK,HOLDLOCK)
            WHERE ocp.orden_c_prod_id = @Id AND ocp.estado = 'A'",
            new { Id = id }, tx);

        if (item is null) return (false, "El item no existe o ya fue eliminado.", id);

        var cab = await conn.QuerySingleOrDefaultAsync<OrdenCompraItemModel>(
            OrdenCompraSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE oc.orden_co_id = @Id AND oc.periodo_emp_sede_id = @ContextoId",
            new { Id = item.OrdenCompraId, ContextoId = contextoId }, tx);

        if (cab is null || cab.Estado != "A")
            return (false, "La orden asociada no se encuentra disponible.", id);

        if (cab.EstadoOrdenCompraId is 3 or 4)
            return (false, "No se pueden eliminar productos en una orden atendida o anulada.", id);

        await conn.ExecuteAsync("sp_orden_c_prod_eliminar", new
        {
            OrdenCompraProductoId = id,
            UsuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        // Recalcular montos de cabecera
        await conn.ExecuteAsync("sp_orden_co_actualizarMonto", new
        {
            OrdenCompraId = item.OrdenCompraId,
            UsuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, "Item eliminado de la orden de compra.", id);
    }

    public async Task<(string Titulo, byte[] Contenido)> ExportarOrdenCompraExcelAsync(int empresaId, int ordenCompraId)
    {
        if (empresaId <= 0 || ordenCompraId <= 0) throw new ArgumentException("Parámetros no válidos.");
        using var conn = factory.CreateConnection();

        var oc = await conn.QuerySingleOrDefaultAsync<OrdenCompraItemModel>(OrdenCompraSelect + @"
            JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id = oc.periodo_emp_sede_id
            JOIN empresa_sede es ON es.empresa_sede_id = pes.empresa_sede_id
            WHERE oc.orden_co_id = @Id AND es.empresa_id = @EmpresaId",
            new { Id = ordenCompraId, EmpresaId = empresaId })
            ?? throw new KeyNotFoundException("No se encontró la orden de compra.");

        var items = await GetOrdenCompraProductosAsync(empresaId, ordenCompraId);

        var columns = new List<(string Label, Func<OrdenCompraProductoItemModel, object?> Value)>
        {
            ("Código", r => r.ProductoCodigo),
            ("Descripción", r => r.Descripcion),
            ("Unidad", r => r.UnidadMedida),
            ("Cantidad", r => r.Cantidad),
            ("Valor Unitario", r => r.ValorUnitario),
            ("Precio Unitario", r => r.PrecioUnitario),
            ("Monto Bruto", r => r.MontoBruto),
            ("IGV", r => r.MontoImpuesto),
            ("Total", r => r.MontoTotal)
        };

        string resumen = $"Orden de Compra N°: {oc.Codigo} | Proveedor: {oc.ProveedorNombre} ({oc.ProveedorDocumento}) | Fecha: {oc.FechaTexto} | Moneda: {oc.MonedaNombre} ({oc.MonedaSimbolo}) | Total: {oc.MonedaSimbolo} {oc.MontoTotal:N2}";
        byte[] excelBytes = InformeExcel.Crear("ORDEN DE COMPRA - " + oc.Codigo, "ARCO ERP", resumen, items, columns);
        return ("Orden_Compra_" + oc.Codigo, excelBytes);
    }

    public async Task<OrdenComAutoPageModel> GetOrdenesComAutoAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        string? codigo = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? tipoProductoId = null,
        int? estadoAprobacionId = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new OrdenComAutoPageModel
        {
            Search = search,
            Codigo = codigo,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            TipoProductoId = tipoProductoId,
            EstadoAprobacionId = estadoAprobacionId
        };

        var columns = new Dictionary<string, string>
        {
            ["codigo"] = "oc.orden_co",
            ["fecha"] = "oc.fecha",
            ["tipo"] = "tp.nombre",
            ["proveedor"] = "p.razon_social",
            ["total"] = "oc.monto_total",
            ["aprobacion"] = "ea.nombre",
            ["estado"] = "eoc.nombre"
        };
        model.Ordenar(sort, dir, columns.Keys, "fecha");
        model.Paginar(0, page, top);

        if (empresaId <= 0 || contextoId <= 0)
        {
            model.Errores.Add("Seleccione una empresa y sede válidas.");
            return model;
        }

        using var conn = factory.CreateConnection();

        model.TiposProducto = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT tipo_producto_id Id, RTRIM(tipo_producto) Codigo, RTRIM(nombre) Nombre
            FROM tipo_producto WHERE empresa_id = @EmpresaId AND estado = 'A' ORDER BY nombre",
            new { EmpresaId = empresaId })).AsList();

        model.EstadosAprobacion = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT estado_aproba_id Id, RTRIM(estado_aproba) Codigo, RTRIM(nombre) Nombre
            FROM estado_aproba WHERE estado = 'A' ORDER BY estado_aproba_id")).AsList();

        var where = new List<string> { "oc.periodo_emp_sede_id = @ContextoId", "oc.estado = 'A'" };
        var args = new DynamicParameters(new
        {
            EmpresaId = empresaId,
            ContextoId = contextoId,
            Codigo = string.IsNullOrWhiteSpace(codigo) ? null : $"%{codigo.Trim()}%",
            Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%",
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            TipoProductoId = tipoProductoId,
            EstadoAprobacionId = estadoAprobacionId
        });

        if (!string.IsNullOrWhiteSpace(codigo)) where.Add("oc.orden_co LIKE @Codigo");
        if (fechaDesde.HasValue) where.Add("oc.fecha >= @FechaDesde");
        if (fechaHasta.HasValue) where.Add("oc.fecha <= @FechaHasta");
        if (tipoProductoId.HasValue) where.Add("oc.tipo_producto_id = @TipoProductoId");

        if (estadoAprobacionId.HasValue)
        {
            where.Add("oc.estado_aproba_id = @EstadoAprobacionId");
        }
        else
        {
            where.Add("oc.estado_aproba_id = 1"); // 1 = Pendiente
            model.EstadoAprobacionId = 1;
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add(@"(oc.orden_co LIKE @Search
                OR p.razon_social LIKE @Search
                OR p.num_doc_iden LIKE @Search
                OR p.numero_ruc LIKE @Search
                OR oc.observa LIKE @Search)");
        }

        string whereSql = " WHERE " + string.Join(" AND ", where);
        string countSql = "SELECT COUNT(1) FROM orden_co oc JOIN tipo_producto tp ON tp.tipo_producto_id = oc.tipo_producto_id JOIN proveedor p ON p.proveedor_id = oc.proveedor_id JOIN moneda m ON m.moneda_id = oc.moneda_id JOIN estado_orden_co eoc ON eoc.estado_orden_co_id = oc.estado_orden_co_id JOIN estado_aproba ea ON ea.estado_aproba_id = oc.estado_aproba_id " + whereSql;

        int total = await conn.ExecuteScalarAsync<int>(countSql, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        string orderSql = $" ORDER BY {columns[model.Sort]} {model.Dir}, oc.orden_co_id DESC ";
        string dataSql = OrdenCompraSelect + whereSql + orderSql + " OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";

        model.Items = (await conn.QueryAsync<OrdenCompraItemModel>(dataSql, args)).AsList();
        model.VisibleCount = model.Items.Count;
        return model;
    }

    public async Task<(bool success, string message, int id)> AprobarOrdenComAutoAsync(int empresaId, int contextoId, int userId, AprobarOrdenComAutoSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.Id <= 0)
            return (false, "Identificador de orden no válido.", 0);

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId))
            return (false, "La empresa no se encuentra activa.", 0);

        var cab = await conn.QuerySingleOrDefaultAsync<OrdenCompraItemModel>(
            OrdenCompraSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE oc.orden_co_id = @Id AND oc.periodo_emp_sede_id = @ContextoId",
            new { Id = model.Id, ContextoId = contextoId }, tx);

        if (cab is null || cab.Estado != "A" || (!string.IsNullOrEmpty(model.Revision) && cab.Revision != model.Revision))
            return (false, "La orden no se encuentra disponible o fue modificada por otro usuario.", model.Id);

        if (cab.EstadoAprobacionId == 2)
            return (false, "La orden ya se encuentra aprobada.", model.Id);

        // SP sp_orden_co_actualizarEstadoAprobacion: 2 = APROBADA
        await conn.ExecuteAsync("sp_orden_co_actualizarEstadoAprobacion", new
        {
            OrdenCompraId = model.Id,
            EstadoAprobacionId = 2,
            FechaAprobacion = DateTime.Now,
            UsuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, $"Orden de compra {cab.Codigo} aprobada correctamente.", model.Id);
    }

    public async Task<(bool success, string message, int id)> DesaprobarOrdenComAutoAsync(int empresaId, int contextoId, int userId, DesaprobarOrdenComAutoSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.Id <= 0)
            return (false, "Identificador de orden no válido.", 0);

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId))
            return (false, "La empresa no se encuentra activa.", 0);

        var cab = await conn.QuerySingleOrDefaultAsync<OrdenCompraItemModel>(
            OrdenCompraSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE oc.orden_co_id = @Id AND oc.periodo_emp_sede_id = @ContextoId",
            new { Id = model.Id, ContextoId = contextoId }, tx);

        if (cab is null || cab.Estado != "A" || (!string.IsNullOrEmpty(model.Revision) && cab.Revision != model.Revision))
            return (false, "La orden no se encuentra disponible o fue modificada por otro usuario.", model.Id);

        if (cab.EstadoAprobacionId == 3)
            return (false, "La orden ya se encuentra desaprobada.", model.Id);

        // SP sp_orden_co_actualizarEstadoAprobacion: 3 = DESAPROBADA
        await conn.ExecuteAsync("sp_orden_co_actualizarEstadoAprobacion", new
        {
            OrdenCompraId = model.Id,
            EstadoAprobacionId = 3,
            FechaAprobacion = DateTime.Now,
            UsuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, $"Orden de compra {cab.Codigo} desaprobada.", model.Id);
    }
}

