using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class ComprasService
{
    private const string PedidoComAutoSelect = @"SELECT pc.pedido_co_id Id,
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
        JOIN estado_aproba ea ON ea.estado_aproba_id = pc.estado_aproba_id ";

    public async Task<PedidoComAutoPageModel> GetPedidosComAutoAsync(
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
        var model = new PedidoComAutoPageModel
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
            ["codigo"] = "pc.pedido_co",
            ["fecha"] = "pc.fecha",
            ["tipo"] = "tp.nombre",
            ["proveedor"] = "p.razon_social",
            ["aprobacion"] = "ea.nombre",
            ["estado"] = "epc.nombre"
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
            EstadoAprobacionId = estadoAprobacionId
        });

        if (!string.IsNullOrWhiteSpace(codigo)) where.Add("pc.pedido_co LIKE @Codigo");
        if (fechaDesde.HasValue) where.Add("pc.fecha >= @FechaDesde");
        if (fechaHasta.HasValue) where.Add("pc.fecha <= @FechaHasta");
        if (tipoProductoId.HasValue) where.Add("pc.tipo_producto_id = @TipoProductoId");

        if (estadoAprobacionId.HasValue)
        {
            where.Add("pc.estado_aproba_id = @EstadoAprobacionId");
        }
        else
        {
            // Por defecto, mostrar los que requieren atención (1 = Pendiente de Aprobación)
            where.Add("pc.estado_aproba_id = 1");
            model.EstadoAprobacionId = 1;
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add(@"(pc.pedido_co LIKE @Search
                OR p.razon_social LIKE @Search
                OR p.num_doc_iden LIKE @Search
                OR p.numero_ruc LIKE @Search
                OR pc.observa LIKE @Search)");
        }

        string whereSql = " WHERE " + string.Join(" AND ", where);
        string countSql = "SELECT COUNT(1) FROM pedido_co pc JOIN tipo_producto tp ON tp.tipo_producto_id = pc.tipo_producto_id JOIN proveedor p ON p.proveedor_id = pc.proveedor_id JOIN estado_pedido_co epc ON epc.estado_pedido_co_id = pc.estado_pedido_co_id JOIN estado_aproba ea ON ea.estado_aproba_id = pc.estado_aproba_id " + whereSql;

        int total = await conn.ExecuteScalarAsync<int>(countSql, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        string orderSql = $" ORDER BY {columns[model.Sort]} {model.Dir}, pc.pedido_co_id DESC ";
        string dataSql = PedidoComAutoSelect + whereSql + orderSql + " OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";

        model.Items = (await conn.QueryAsync<PedidoComAutoItemModel>(dataSql, args)).AsList();
        model.VisibleCount = model.Items.Count;
        return model;
    }

    public async Task<PedidoComAutoDetalleModel?> GetPedidoComAutoDetalleAsync(int empresaId, int contextoId, int id)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0) return null;
        using var conn = factory.CreateConnection();

        var cabecera = await conn.QuerySingleOrDefaultAsync<PedidoComAutoItemModel>(
            PedidoComAutoSelect + " WHERE pc.pedido_co_id = @Id AND pc.periodo_emp_sede_id = @ContextoId",
            new { Id = id, ContextoId = contextoId });

        if (cabecera is null) return null;

        var items = await GetPedidoCompraProductosAsync(empresaId, id);

        return new PedidoComAutoDetalleModel
        {
            Cabecera = cabecera,
            Productos = items
        };
    }

    public async Task<(bool success, string message, int id)> AprobarPedidoComAutoAsync(int empresaId, int contextoId, int userId, AprobarPedidoComAutoSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.Id <= 0)
            return (false, "Identificador de requerimiento no válido.", 0);

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId))
            return (false, "La empresa no se encuentra activa.", 0);

        var cabecera = await conn.QuerySingleOrDefaultAsync<PedidoComAutoItemModel>(
            PedidoComAutoSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE pc.pedido_co_id = @Id AND pc.periodo_emp_sede_id = @ContextoId",
            new { Id = model.Id, ContextoId = contextoId }, tx);

        if (cabecera is null || cabecera.Estado != "A" || (!string.IsNullOrEmpty(model.Revision) && cabecera.Revision != model.Revision))
            return (false, "El requerimiento no se encuentra disponible o fue modificado por otro usuario.", model.Id);

        if (cabecera.EstadoAprobacionId == 2)
            return (false, "El requerimiento ya se encuentra aprobado.", model.Id);

        // SP sp_pedido_co_actualizarEstadoAprobacion: 2 = APROBADA
        await conn.ExecuteAsync("sp_pedido_co_actualizarEstadoAprobacion", new
        {
            PedidoCompraId = model.Id,
            EstadoAprobacionId = 2,
            FechaAprobacion = DateTime.Now,
            UsuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, $"Requerimiento {cabecera.Codigo} aprobado correctamente.", model.Id);
    }

    public async Task<(bool success, string message, int id)> DesaprobarPedidoComAutoAsync(int empresaId, int contextoId, int userId, DesaprobarPedidoComAutoSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.Id <= 0)
            return (false, "Identificador de requerimiento no válido.", 0);

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId))
            return (false, "La empresa no se encuentra activa.", 0);

        var cabecera = await conn.QuerySingleOrDefaultAsync<PedidoComAutoItemModel>(
            PedidoComAutoSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE pc.pedido_co_id = @Id AND pc.periodo_emp_sede_id = @ContextoId",
            new { Id = model.Id, ContextoId = contextoId }, tx);

        if (cabecera is null || cabecera.Estado != "A" || (!string.IsNullOrEmpty(model.Revision) && cabecera.Revision != model.Revision))
            return (false, "El requerimiento no se encuentra disponible o fue modificado por otro usuario.", model.Id);

        if (cabecera.EstadoAprobacionId == 3)
            return (false, "El requerimiento ya se encuentra desaprobado.", model.Id);

        // SP sp_pedido_co_actualizarEstadoAprobacion: 3 = DESAPROBADA
        await conn.ExecuteAsync("sp_pedido_co_actualizarEstadoAprobacion", new
        {
            PedidoCompraId = model.Id,
            EstadoAprobacionId = 3,
            FechaAprobacion = DateTime.Now,
            UsuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, $"Requerimiento {cabecera.Codigo} desaprobado.", model.Id);
    }
}

