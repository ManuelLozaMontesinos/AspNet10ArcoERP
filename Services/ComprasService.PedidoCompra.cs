using System.Data;
using System.Globalization;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class ComprasService
{
    private const string PedidoCompraSelect = @"SELECT pc.pedido_co_id Id,
        pc.periodo_emp_sede_id PeriodoEmpresaSedeId,
        RTRIM(pc.pedido_co) Codigo,
        pc.fecha Fecha,
        pc.tipo_producto_id TipoProductoId,
        RTRIM(tp.nombre) TipoProductoNombre,
        pc.proveedor_id ProveedorId,
        RTRIM(p.razon_social) ProveedorNombre,
        COALESCE(NULLIF(p.numero_ruc,''), p.num_doc_iden, '') ProveedorDocumento,
        pc.observa Observacion,
        pc.es_modifica EsModificacion,
        pc.estado_aproba_id EstadoAprobacionId,
        RTRIM(ea.estado_aproba) EstadoAprobacionCodigo,
        RTRIM(ea.nombre) EstadoAprobacionNombre,
        pc.estado_pedido_co_id EstadoPedidoCompraId,
        RTRIM(epc.estado_pedido_co) EstadoPedidoCompraCodigo,
        RTRIM(epc.nombre) EstadoPedidoCompraNombre,
        pc.estado Estado,
        COALESCE(pc.fecha_modifica, pc.fecha_creacion) FechaRevision,
        (SELECT COUNT(1) FROM pedido_c_prod pcp WHERE pcp.pedido_co_id = pc.pedido_co_id AND pcp.estado = 'A') CantidadItems
        FROM pedido_co pc
        JOIN tipo_producto tp ON tp.tipo_producto_id = pc.tipo_producto_id
        JOIN proveedor p ON p.proveedor_id = pc.proveedor_id
        JOIN estado_pedido_co epc ON epc.estado_pedido_co_id = pc.estado_pedido_co_id
        LEFT JOIN estado_aproba ea ON ea.estado_aproba_id = pc.estado_aproba_id ";

    public async Task<PedidoCompraPageModel> GetPedidosCompraAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        string? codigo = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? tipoProductoId = null,
        int? estadoPedidoCompraId = null,
        int? estadoAprobacionId = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new PedidoCompraPageModel
        {
            Search = search,
            Codigo = codigo,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            TipoProductoId = tipoProductoId,
            EstadoPedidoCompraId = estadoPedidoCompraId,
            EstadoAprobacionId = estadoAprobacionId
        };

        var columns = new Dictionary<string, string>
        {
            ["codigo"] = "pc.pedido_co",
            ["fecha"] = "pc.fecha",
            ["tipo"] = "tp.nombre",
            ["proveedor"] = "p.razon_social",
            ["estado"] = "epc.nombre",
            ["aprobacion"] = "ea.nombre"
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
            SELECT tipo_producto_id Id, RTRIM(tipo_producto) Codigo, RTRIM(nombre) Descripcion
            FROM tipo_producto WHERE empresa_id = @EmpresaId AND estado = 'A' ORDER BY nombre",
            new { EmpresaId = empresaId })).AsList();

        model.EstadosPedidoCompra = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT estado_pedido_co_id Id, RTRIM(estado_pedido_co) Codigo, RTRIM(nombre) Descripcion
            FROM estado_pedido_co WHERE estado = 'A' ORDER BY estado_pedido_co_id")).AsList();

        model.EstadosAprobacion = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT estado_aproba_id Id, RTRIM(estado_aproba) Codigo, RTRIM(nombre) Descripcion
            FROM estado_aproba WHERE estado = 'A' ORDER BY estado_aproba_id")).AsList();

        var where = new List<string> { "pc.periodo_emp_sede_id = @ContextoId", "pc.estado = 'A'" };
        var args = new DynamicParameters(new
        {
            EmpresaId = empresaId,
            ContextoId = contextoId,
            Codigo = string.IsNullOrWhiteSpace(codigo) ? null : $"%{codigo.Trim()}%",
            Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%",
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            TipoProductoId = tipoProductoId,
            EstadoPedidoCompraId = estadoPedidoCompraId,
            EstadoAprobacionId = estadoAprobacionId
        });

        if (!string.IsNullOrWhiteSpace(codigo)) where.Add("pc.pedido_co LIKE @Codigo");
        if (fechaDesde.HasValue) where.Add("pc.fecha >= @FechaDesde");
        if (fechaHasta.HasValue) where.Add("pc.fecha <= @FechaHasta");
        if (tipoProductoId.HasValue) where.Add("pc.tipo_producto_id = @TipoProductoId");
        if (estadoPedidoCompraId.HasValue) where.Add("pc.estado_pedido_co_id = @EstadoPedidoCompraId");
        if (estadoAprobacionId.HasValue) where.Add("pc.estado_aproba_id = @EstadoAprobacionId");

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add(@"(pc.pedido_co LIKE @Search
                OR p.razon_social LIKE @Search
                OR p.num_doc_iden LIKE @Search
                OR p.numero_ruc LIKE @Search
                OR pc.observa LIKE @Search)");
        }

        string whereSql = " WHERE " + string.Join(" AND ", where);
        string countSql = "SELECT COUNT(1) FROM pedido_co pc JOIN tipo_producto tp ON tp.tipo_producto_id = pc.tipo_producto_id JOIN proveedor p ON p.proveedor_id = pc.proveedor_id JOIN estado_pedido_co epc ON epc.estado_pedido_co_id = pc.estado_pedido_co_id LEFT JOIN estado_aproba ea ON ea.estado_aproba_id = pc.estado_aproba_id " + whereSql;

        int total = await conn.ExecuteScalarAsync<int>(countSql, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        string orderSql = $" ORDER BY {columns[model.Sort]} {model.Dir}, pc.pedido_co_id DESC ";
        string dataSql = PedidoCompraSelect + whereSql + orderSql + " OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";

        model.Items = (await conn.QueryAsync<PedidoCompraItemModel>(dataSql, args)).AsList();
        model.VisibleCount = model.Items.Count;
        return model;
    }

    public async Task<PedidoCompraDetalleModel?> GetPedidoCompraDetalleAsync(int empresaId, int contextoId, int id)
    {
        if (empresaId <= 0 || contextoId <= 0) return null;
        using var conn = factory.CreateConnection();

        var detalle = new PedidoCompraDetalleModel();

        detalle.TiposProducto = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT tipo_producto_id Id, RTRIM(tipo_producto) Codigo, RTRIM(nombre) Descripcion
            FROM tipo_producto WHERE empresa_id = @EmpresaId AND estado = 'A' ORDER BY nombre",
            new { EmpresaId = empresaId })).AsList();

        detalle.Proveedores = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT TOP 50 proveedor_id Id, RTRIM(COALESCE(NULLIF(numero_ruc,''), num_doc_iden, '')) Codigo, RTRIM(razon_social) Descripcion
            FROM proveedor WHERE empresa_id = @EmpresaId AND estado = 'A' ORDER BY razon_social",
            new { EmpresaId = empresaId })).AsList();

        if (id <= 0)
        {
            var defaultTipo = detalle.TiposProducto.FirstOrDefault(t => t.Codigo == "01") ?? detalle.TiposProducto.FirstOrDefault();
            var defaultProv = detalle.Proveedores.FirstOrDefault(p => p.Codigo == "00000000000" || p.Descripcion.Contains("GENERICO", StringComparison.OrdinalIgnoreCase)) ?? detalle.Proveedores.FirstOrDefault();

            detalle.Cabecera = new PedidoCompraItemModel
            {
                Id = 0,
                PeriodoEmpresaSedeId = contextoId,
                Fecha = DateTime.Today,
                TipoProductoId = defaultTipo?.Id ?? 0,
                TipoProductoNombre = defaultTipo?.Descripcion ?? "",
                ProveedorId = defaultProv?.Id ?? 0,
                ProveedorNombre = defaultProv?.Descripcion ?? "",
                EstadoPedidoCompraId = 1,
                EstadoPedidoCompraNombre = "EN EDICIÓN",
                EsModificacion = "N"
            };
            return detalle;
        }

        var cabecera = await conn.QuerySingleOrDefaultAsync<PedidoCompraItemModel>(
            PedidoCompraSelect + " WHERE pc.pedido_co_id = @Id AND pc.periodo_emp_sede_id = @ContextoId",
            new { Id = id, ContextoId = contextoId });

        if (cabecera is null) return null;
        detalle.Cabecera = cabecera;
        detalle.Productos = await GetPedidoCompraProductosAsync(empresaId, id);

        return detalle;
    }

    public async Task<List<LookupItemModel>> BuscarProveedoresCompraAsync(int empresaId, string? search)
    {
        if (empresaId <= 0) return new();
        using var conn = factory.CreateConnection();
        return (await conn.QueryAsync<LookupItemModel>(@"
            SELECT TOP 30 proveedor_id Id,
                RTRIM(COALESCE(NULLIF(numero_ruc,''), num_doc_iden, '')) Codigo,
                RTRIM(razon_social) Descripcion
            FROM proveedor
            WHERE empresa_id = @EmpresaId AND estado = 'A'
              AND (@Search IS NULL OR razon_social LIKE @Search OR num_doc_iden LIKE @Search OR numero_ruc LIKE @Search)
            ORDER BY razon_social",
            new { EmpresaId = empresaId, Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%" })).AsList();
    }

    public async Task<List<LookupItemModel>> BuscarProductosCompraAsync(int empresaId, int? tipoProductoId, string? search)
    {
        if (empresaId <= 0) return new();
        using var conn = factory.CreateConnection();
        return (await conn.QueryAsync<LookupItemModel>(@"
            SELECT TOP 30 prd.producto_id Id,
                RTRIM(prd.producto) Codigo,
                RTRIM(prd.descripcion) Descripcion,
                RTRIM(ISNULL(um.abrev, 'UND')) Extra
            FROM producto prd
            JOIN unidad_medida um ON um.unidad_medida_id = prd.unidad_medida_id
            JOIN familia_producto fp ON fp.familia_producto_id = prd.familia_producto_id
            JOIN clase_producto cp ON cp.clase_producto_id = fp.clase_producto_id
            JOIN grupo_producto gp ON gp.grupo_producto_id = cp.grupo_producto_id
            JOIN tipo_producto tp ON tp.tipo_producto_id = gp.tipo_producto_id
            WHERE tp.empresa_id = @EmpresaId AND prd.estado = 'A'
              AND (@TipoProductoId IS NULL OR tp.tipo_producto_id = @TipoProductoId)
              AND (@Search IS NULL OR prd.producto LIKE @Search OR prd.descripcion LIKE @Search)
            ORDER BY prd.descripcion",
            new
            {
                EmpresaId = empresaId,
                TipoProductoId = tipoProductoId,
                Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%"
            })).AsList();
    }

    public async Task<(bool success, string message, int id)> SavePedidoCompraAsync(int empresaId, int contextoId, int userId, PedidoCompraGuardarSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.TipoProductoId <= 0 || model.ProveedorId <= 0)
            return (false, "Complete los datos obligatorios del requerimiento de compra.", 0);

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId))
            return (false, "La empresa no se encuentra activa.", 0);

        if (model.Id > 0)
        {
            var old = await conn.QuerySingleOrDefaultAsync<PedidoCompraItemModel>(
                PedidoCompraSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE pc.pedido_co_id = @Id AND pc.periodo_emp_sede_id = @ContextoId",
                new { Id = model.Id, ContextoId = contextoId }, tx);

            if (old is null || old.Estado != "A" || (!string.IsNullOrEmpty(model.Revision) && old.Revision != model.Revision))
                return (false, "El requerimiento no se encuentra disponible o fue modificado por otro usuario.", model.Id);

            if (old.EstadoPedidoCompraId is 3 or 4) // Con orden de compra o Anulado
                return (false, "No se puede editar un requerimiento cerrado o anulado.", model.Id);

            await conn.ExecuteAsync("sp_pedido_co_actualizar", new
            {
                PedidoCompraId = model.Id,
                ProveedorId = model.ProveedorId,
                EsModificacion = old.EsModificacion,
                Observacion = model.Observacion?.Trim() ?? "",
                EstadoPedidoCompraId = old.EstadoPedidoCompraId,
                EstadoPedidoCompraAnteriorId = (int?)null,
                UsuarioAccionId = userId
            }, tx, commandType: CommandType.StoredProcedure);

            tx.Commit();
            return (true, $"Requerimiento {old.Codigo} actualizado correctamente.", model.Id);
        }
        else
        {
            // Correlativo 0024 (PEDIDO DE COMPRA) por empresa
            var counter = await conn.QuerySingleOrDefaultAsync<(int Id, int Correlativo)?>(@"
                SELECT empresa_corre_id Id, correlativo Correlativo
                FROM empresa_corre WITH (UPDLOCK,HOLDLOCK)
                WHERE empresa_id = @EmpresaId AND tabla_id = 24",
                new { EmpresaId = empresaId }, tx);

            int nextVal = ((counter?.Correlativo) ?? 0) + 1;
            string code = nextVal.ToString("00000", CultureInfo.InvariantCulture);

            if (counter is null)
            {
                await InsertAsync(conn, tx, "sp_empresa_corre_insertar", new
                {
                    EmpresaId = empresaId,
                    TablaId = 24,
                    Correlativo = nextVal,
                    UsuarioAccionId = userId
                });
            }
            else
            {
                await conn.ExecuteAsync("sp_empresa_corre_actualizar", new
                {
                    EmpresaCorrelativoId = counter.Value.Id,
                    Correlativo = nextVal,
                    UsuarioAccionId = userId
                }, tx, commandType: CommandType.StoredProcedure);
            }

            int newId = await InsertAsync(conn, tx, "sp_pedido_co_insertar", new
            {
                PeriodoEmpresaSedeId = contextoId,
                PedidoCompra = code,
                Fecha = model.Fecha,
                TipoProductoId = model.TipoProductoId,
                ProveedorId = model.ProveedorId,
                EsModificacion = "N",
                Observacion = model.Observacion?.Trim() ?? "",
                EstadoPedidoCompraId = 1, // En Edición
                UsuarioAccionId = userId
            });

            tx.Commit();
            return (true, $"Requerimiento N° {code} registrado con éxito.", newId);
        }
    }

    public async Task<(bool success, string message, int id)> AnularPedidoCompraAsync(int empresaId, int contextoId, int userId, int id, string revision)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0) return (false, "Identificador no válido.", 0);
        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return (false, "La empresa no está activa.", 0);

        var old = await conn.QuerySingleOrDefaultAsync<PedidoCompraItemModel>(
            PedidoCompraSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE pc.pedido_co_id = @Id AND pc.periodo_emp_sede_id = @ContextoId",
            new { Id = id, ContextoId = contextoId }, tx);

        if (old is null || old.Estado != "A" || (!string.IsNullOrEmpty(revision) && old.Revision != revision))
            return (false, "El requerimiento no se encuentra disponible o fue modificado.", id);

        if (old.EstadoPedidoCompraId is 3 or 4)
            return (false, "No se puede anular un requerimiento con orden de compra o ya anulado.", id);

        await conn.ExecuteAsync("sp_pedido_co_eliminar", new
        {
            PedidoCompraId = id,
            UsuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, $"Requerimiento {old.Codigo} anulado correctamente.", id);
    }

    public async Task<(bool success, string message, int id)> EnviarAprobacionPedidoCompraAsync(int empresaId, int contextoId, int userId, int id, string revision)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0) return (false, "Identificador no válido.", 0);
        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return (false, "La empresa no está activa.", 0);

        var old = await conn.QuerySingleOrDefaultAsync<PedidoCompraItemModel>(
            PedidoCompraSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE pc.pedido_co_id = @Id AND pc.periodo_emp_sede_id = @ContextoId",
            new { Id = id, ContextoId = contextoId }, tx);

        if (old is null || old.Estado != "A" || (!string.IsNullOrEmpty(revision) && old.Revision != revision))
            return (false, "El requerimiento no se encuentra disponible o fue modificado.", id);

        var items = await conn.QueryAsync<PedidoCompraProductoItemModel>(@"
            SELECT pedido_c_prod_id Id, cantidad Cantidad
            FROM pedido_c_prod WHERE pedido_co_id = @Id AND estado = 'A'",
            new { Id = id }, tx);

        var itemList = items.AsList();
        if (itemList.Count == 0)
            return (false, "No puede enviar a aprobación un requerimiento sin productos.", id);

        if (itemList.All(i => i.Cantidad <= 0m))
            return (false, "Los productos del requerimiento deben tener cantidades mayores a cero.", id);

        // Estado 2 = PEDIDO, EstadoAprobacion 1 = PENDIENTE DE APROBACIÓN
        await conn.ExecuteAsync(@"
            UPDATE pedido_co
            SET estado_pedido_co_id = 2,
                estado_aproba_id = 1,
                fecha_aproba = NULL,
                usuario_modifica_id = @UserId,
                fecha_modifica = GETDATE()
            WHERE pedido_co_id = @Id",
            new { Id = id, UserId = userId }, tx);

        tx.Commit();
        return (true, $"Requerimiento {old.Codigo} enviado a aprobación con éxito.", id);
    }

    public async Task<List<PedidoCompraProductoItemModel>> GetPedidoCompraProductosAsync(int empresaId, int pedidoCompraId)
    {
        if (empresaId <= 0 || pedidoCompraId <= 0) return new();
        using var conn = factory.CreateConnection();

        return (await conn.QueryAsync<PedidoCompraProductoItemModel>(@"
            SELECT pcp.pedido_c_prod_id Id,
                pcp.pedido_co_id PedidoCompraId,
                pcp.producto_id ProductoId,
                RTRIM(ISNULL(p.producto, '')) ProductoCodigo,
                RTRIM(pcp.descripcion) Descripcion,
                RTRIM(ISNULL(um.abrev, 'UND')) UnidadMedida,
                pcp.cantidad Cantidad,
                pcp.peso_neto PesoNeto,
                pcp.peso_bruto PesoBruto,
                pcp.stock_prove StockEnProveedor,
                pcp.es_adicional EsAdicional,
                pcp.estado Estado,
                COALESCE(pcp.fecha_modifica, pcp.fecha_creacion) FechaRevision
            FROM pedido_c_prod pcp
            LEFT JOIN producto p ON p.producto_id = pcp.producto_id
            LEFT JOIN unidad_medida um ON um.unidad_medida_id = p.unidad_medida_id
            WHERE pcp.pedido_co_id = @PedidoCompraId AND pcp.estado = 'A'
            ORDER BY pcp.pedido_c_prod_id",
            new { PedidoCompraId = pedidoCompraId })).AsList();
    }

    public async Task<(bool success, string message, int id)> SavePedidoCompraProductoAsync(int empresaId, int contextoId, int userId, PedidoCompraProductoGuardarSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.PedidoCompraId <= 0 || string.IsNullOrWhiteSpace(model.Descripcion) || model.Cantidad <= 0m)
            return (false, "Ingrese una descripción y cantidad válidas para el producto.", 0);

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return (false, "La empresa no se encuentra activa.", 0);

        var cabecera = await conn.QuerySingleOrDefaultAsync<PedidoCompraItemModel>(
            PedidoCompraSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE pc.pedido_co_id = @Id AND pc.periodo_emp_sede_id = @ContextoId",
            new { Id = model.PedidoCompraId, ContextoId = contextoId }, tx);

        if (cabecera is null || cabecera.Estado != "A")
            return (false, "El requerimiento no se encuentra disponible.", 0);

        if (cabecera.EstadoPedidoCompraId is 3 or 4)
            return (false, "No se pueden modificar productos en un requerimiento cerrado o anulado.", 0);

        decimal pesoNeto = model.PesoNeto;
        decimal pesoBruto = model.PesoBruto;

        if (model.ProductoId.HasValue && model.ProductoId.Value > 0 && pesoNeto == 0m && pesoBruto == 0m)
        {
            var pData = await conn.QuerySingleOrDefaultAsync<(decimal Neto, decimal Bruto)?>(@"
                SELECT peso_neto Neto, peso_bruto Bruto FROM producto WHERE producto_id = @ProductoId",
                new { ProductoId = model.ProductoId.Value }, tx);

            if (pData.HasValue)
            {
                pesoNeto = pData.Value.Neto * model.Cantidad;
                pesoBruto = pData.Value.Bruto * model.Cantidad;
            }
        }

        if (model.Id > 0)
        {
            await conn.ExecuteAsync("sp_pedido_c_prod_actualizar", new
            {
                PedidoCompraProductoId = model.Id,
                Descripcion = model.Descripcion.Trim(),
                Cantidad = model.Cantidad,
                PesoNeto = pesoNeto,
                PesoBruto = pesoBruto,
                StockEnProveedor = model.StockEnProveedor == "S" ? "S" : "N",
                UsuarioAccionId = userId
            }, tx, commandType: CommandType.StoredProcedure);

            tx.Commit();
            return (true, "Item actualizado correctamente.", model.Id);
        }
        else
        {
            int newId = await InsertAsync(conn, tx, "sp_pedido_c_prod_insertar", new
            {
                PedidoCompraId = model.PedidoCompraId,
                ProductoId = model.ProductoId > 0 ? model.ProductoId : (int?)null,
                Descripcion = model.Descripcion.Trim(),
                Cantidad = model.Cantidad,
                PesoNeto = pesoNeto,
                PesoBruto = pesoBruto,
                StockEnProveedor = model.StockEnProveedor == "S" ? "S" : "N",
                EsAdicional = model.EsAdicional == "S" ? "S" : "N",
                UsuarioAccionId = userId
            });

            tx.Commit();
            return (true, "Item agregado al requerimiento.", newId);
        }
    }

    public async Task<(bool success, string message, int id)> EliminarPedidoCompraProductoAsync(int empresaId, int contextoId, int userId, int id)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0) return (false, "Identificador no válido.", 0);
        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return (false, "La empresa no se encuentra activa.", 0);

        var item = await conn.QuerySingleOrDefaultAsync<PedidoCompraProductoItemModel>(@"
            SELECT pcp.pedido_c_prod_id Id, pcp.pedido_co_id PedidoCompraId
            FROM pedido_c_prod pcp WITH (UPDLOCK,HOLDLOCK)
            WHERE pcp.pedido_c_prod_id = @Id AND pcp.estado = 'A'",
            new { Id = id }, tx);

        if (item is null) return (false, "El item no existe o ya fue eliminado.", id);

        var cabecera = await conn.QuerySingleOrDefaultAsync<PedidoCompraItemModel>(
            PedidoCompraSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE pc.pedido_co_id = @Id AND pc.periodo_emp_sede_id = @ContextoId",
            new { Id = item.PedidoCompraId, ContextoId = contextoId }, tx);

        if (cabecera is null || cabecera.Estado != "A")
            return (false, "El requerimiento asociado no está disponible.", id);

        if (cabecera.EstadoPedidoCompraId is 3 or 4)
            return (false, "No se pueden eliminar items en requerimientos con orden o anulados.", id);

        await conn.ExecuteAsync("sp_pedido_c_prod_eliminar", new
        {
            PedidoCompraProductoId = id,
            UsuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, "Item eliminado del requerimiento.", id);
    }

    public async Task<(string Titulo, byte[] Contenido)> ExportarPedidoCompraExcelAsync(int empresaId, int pedidoCompraId)
    {
        if (empresaId <= 0 || pedidoCompraId <= 0) throw new ArgumentException("Parámetros no válidos.");
        using var conn = factory.CreateConnection();

        var pc = await conn.QuerySingleOrDefaultAsync<PedidoCompraItemModel>(PedidoCompraSelect + @"
            JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id = pc.periodo_emp_sede_id
            JOIN empresa_sede es ON es.empresa_sede_id = pes.empresa_sede_id
            WHERE pc.pedido_co_id = @Id AND es.empresa_id = @EmpresaId",
            new { Id = pedidoCompraId, EmpresaId = empresaId })
            ?? throw new KeyNotFoundException("No se encontró el requerimiento de compra.");

        var items = await GetPedidoCompraProductosAsync(empresaId, pedidoCompraId);

        var columns = new List<(string Label, Func<PedidoCompraProductoItemModel, object?> Value)>
        {
            ("Código", r => r.ProductoCodigo),
            ("Descripción", r => r.Descripcion),
            ("Unidad", r => r.UnidadMedida),
            ("Cantidad", r => r.Cantidad),
            ("Peso Neto (kg)", r => r.PesoNeto),
            ("Peso Bruto (kg)", r => r.PesoBruto),
            ("Stock en Prov.", r => r.StockEnProveedor == "S" ? "SÍ" : "NO"),
            ("Es Adicional", r => r.EsAdicional == "S" ? "SÍ" : "NO")
        };

        string resumen = $"Requerimiento N°: {pc.Codigo} | Fecha: {pc.FechaTexto} | Tipo: {pc.TipoProductoNombre} | Proveedor: {pc.ProveedorNombre} ({pc.ProveedorDocumento}) | Estado: {pc.EstadoPedidoCompraNombre} / {pc.EstadoAprobacionNombre ?? "PENDIENTE"}";
        byte[] excelBytes = InformeExcel.Crear("REQUERIMIENTO DE COMPRA - " + pc.Codigo, "ARCO ERP", resumen, items, columns);
        return ("Requerimiento_Compra_" + pc.Codigo, excelBytes);
    }
}

