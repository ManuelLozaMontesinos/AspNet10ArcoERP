using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services;

public partial class InventarioService
{
    private const string MinimoSelect = @"SELECT ISNULL(avp.alm_vol_prod_id,0) Id,@ScopeId ContextoId,@SedeId EmpresaSedeId,
        prd.producto_id ProductoId,RTRIM(prd.producto) Codigo,prd.descripcion Producto,um.nombre Unidad,prd.estado EstadoProducto,
        ISNULL(avp.stock_min,0) StockMinimo,COALESCE(avp.fecha_modifica,avp.fecha_creacion) FechaRevision ";
    private const string MinimoFrom = @" FROM producto prd JOIN unidad_medida um ON um.unidad_medida_id=prd.unidad_medida_id " + ProductoEmpresaJoin + @"
        LEFT JOIN alm_vol_prod avp ON avp.producto_id=prd.producto_id AND avp.empresa_sede_id=@SedeId
        WHERE tp.empresa_id=@EmpresaId AND tp.tipo_producto='P' AND gp.es_combo='N' AND (@Activos=0 OR prd.estado='A')
          AND @Anio BETWEEN YEAR(prd.fecha_alta) AND ISNULL(YEAR(prd.fecha_baja),@Anio) ";

    public async Task<VolumenExistenciaPageModel> GetVolumenExistenciasAsync(int empresaId, int scopeId, int userId, int? sedeId = null, string? search = null, int page = 1, int top = 25, string? sort = null, string? dir = null, bool exportar = false)
    {
        var model = new VolumenExistenciaPageModel { Tipo="minimos",Titulo="Volumen de existencias",EmpresaSedeId=sedeId,Filtro=new(){Search=search} }; model.Paginar(0, page, top);
        var columns = new Dictionary<string,string> { ["codigo"]="prd.producto",["producto"]="prd.descripcion",["unidad"]="um.nombre",["minimo"]="ISNULL(avp.stock_min,0)" };
        model.Ordenar(sort, dir, columns.Keys, "codigo");
        if (empresaId <= 0 || scopeId <= 0 || userId <= 0) { model.Errores.Add("Seleccione un contexto de trabajo válido."); return model; }
        using var conn = factory.CreateConnection(); var scope = await GetInventarioScopeAsync(conn, empresaId, scopeId, userId);
        if (scope is null) { model.Errores.Add("El período y la sede de trabajo no están habilitados."); return model; }
        model.Contexto = scope;
        model.Sedes = (await conn.QueryAsync<LookupItemModel>("SELECT empresa_sede_id Id,nombre Nombre FROM empresa_sede WHERE empresa_id=@EmpresaId AND estado='A' ORDER BY nombre,empresa_sede_id", new { EmpresaId=empresaId })).AsList();
        if (search?.Length > 150) model.Errores.Add("Reduzca la longitud de la búsqueda.");
        if (!sedeId.HasValue) { if (exportar) model.Errores.Add("Seleccione una sede para exportar."); return model; }
        var site = model.Sedes.FirstOrDefault(s => s.Id == sedeId);
        if (site is null) model.Errores.Add("Seleccione una sede activa de la empresa actual."); else model.SedeSeleccionada = site.Nombre;
        if (model.Errores.Count > 0) return model;
        model.Consultada = true;
        var args = new DynamicParameters(new { EmpresaId=empresaId,ScopeId=scopeId,SedeId=sedeId,Anio=scope.Anio,Search=Search(search),Activos=exportar });
        string from = MinimoFrom + " AND (@Search IS NULL OR prd.producto LIKE @Search OR prd.descripcion LIKE @Search)";
        PageReport(model, await conn.ExecuteScalarAsync<int>("SELECT COUNT(*)" + from, args), page, top, exportar); if (model.Errores.Count > 0) return model;
        args.Add("Offset", exportar ? 0 : (model.Page-1)*model.PageSize); args.Add("Top", exportar ? MaxExportRows : model.PageSize);
        model.Items = (await conn.QueryAsync<StockMinimoRowModel>(MinimoSelect + from + $" ORDER BY {columns[model.Sort]} {model.Dir},prd.producto_id,avp.alm_vol_prod_id OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY", args)).AsList();
        model.VisibleCount = model.Items.Count; return model;
    }

    public async Task<StockMinimoRowModel?> GetStockMinimoAsync(int empresaId, int scopeId, int userId, int sedeId, int productoId)
    {
        if (empresaId<=0 || scopeId<=0 || userId<=0 || sedeId<=0 || productoId<=0) return null;
        using var conn = factory.CreateConnection();
        var scope = await MinimoContextAsync(conn, null, empresaId, scopeId, userId, sedeId);
        if (scope is null) return null;
        return await conn.QuerySingleOrDefaultAsync<StockMinimoRowModel>(MinimoSelect + MinimoFrom + " AND prd.producto_id=@ProductoId", new { EmpresaId=empresaId,ScopeId=scopeId,SedeId=sedeId,Anio=scope.Anio,ProductoId=productoId,Activos=false });
    }

    private static async Task<InventarioReporteScope?> MinimoContextAsync(IDbConnection conn, IDbTransaction? tx, int empresaId, int scopeId, int userId, int sedeId)
    {
        if (empresaId <= 0 || scopeId <= 0 || userId <= 0 || sedeId <= 0) return null;
        var scope = await GetInventarioScopeAsync(conn, empresaId, scopeId, userId, tx, tx is not null); if (scope is null) return null;
        string? site = await conn.QuerySingleOrDefaultAsync<string>("SELECT nombre FROM empresa_sede " + (tx is null ? "" : "WITH (UPDLOCK,HOLDLOCK) ") + "WHERE empresa_sede_id=@SedeId AND empresa_id=@EmpresaId AND estado='A'", new { SedeId=sedeId,EmpresaId=empresaId }, tx);
        if (site is null) return null;
        // This scope object is local to this read; the authenticated session remains unchanged.
        scope.Sede = site; return scope;
    }

    private static async Task<List<StockMinimoRowModel>> MinimumRowsAsync(IDbConnection conn, IDbTransaction? tx, int empresaId, int scopeId, int sedeId, int anio, IEnumerable<int> products)
    {
        var result = new List<StockMinimoRowModel>();
        string from = tx is null ? MinimoFrom : MinimoFrom.Replace("alm_vol_prod avp", "alm_vol_prod avp WITH (UPDLOCK,HOLDLOCK)");
        foreach (var chunk in products.Distinct().Chunk(500))
            result.AddRange(await conn.QueryAsync<StockMinimoRowModel>(MinimoSelect + from + " AND prd.producto_id IN @Products", new { EmpresaId=empresaId,ScopeId=scopeId,SedeId=sedeId,Anio=anio,Products=chunk,Activos=false }, tx));
        return result;
    }

    public Task<(bool success, string message, int id)> SaveStockMinimoAsync(int empresaId, int scopeId, int userId, StockMinimoSolicitud solicitud)
        => SaveMinimumChangesAsync(empresaId, scopeId, userId, solicitud, false);

    public Task<(bool success, string message, int id)> SaveImportacionMinimosAsync(int empresaId, int scopeId, int userId, StockMinimoSolicitud solicitud)
        => SaveMinimumChangesAsync(empresaId, scopeId, userId, solicitud, true);

    private async Task<(bool success, string message, int id)> SaveMinimumChangesAsync(int empresaId, int scopeId, int userId, StockMinimoSolicitud request, bool lote)
    {
        if (!Valid(request) || request.ContextoId != scopeId || (!lote && request.Cambios.Count != 1) || request.Cambios.Any(c => c is null || !Valid(c) || decimal.Round(c.StockMinimo,4) != c.StockMinimo) || request.Cambios.Select(c=>c.ProductoId).Distinct().Count()!=request.Cambios.Count)
            return Failure("Revise el contexto, los productos y los mínimos: deben ser únicos, no negativos y tener hasta cuatro decimales.");
        using var conn = factory.CreateConnection();
        var scope = await MinimoContextAsync(conn, null, empresaId, scopeId, userId, request.EmpresaSedeId);
        if (scope is null) return Failure("Seleccione una sede activa de la empresa y un contexto de trabajo válido.");
        var products = request.Cambios.Select(c=>c.ProductoId).ToArray();
        var current = await MinimumRowsAsync(conn, null, empresaId, scopeId, request.EmpresaSedeId, scope.Anio, products);
        if (!MatchMinimumChanges(current, request.Cambios)) return Failure("Uno de los productos o mínimos cambió. Actualice la consulta o vuelva a preparar el archivo.");
        if (conn.State != ConnectionState.Open) conn.Open(); using var tx = conn.BeginTransaction(IsolationLevel.Serializable);
        scope = await MinimoContextAsync(conn, tx, empresaId, scopeId, userId, request.EmpresaSedeId);
        if (scope is null) return Failure("El contexto o la sede seleccionada cambió. Actualice la consulta.");
        current = await MinimumRowsAsync(conn, tx, empresaId, scopeId, request.EmpresaSedeId, scope.Anio, products);
        if (!MatchMinimumChanges(current, request.Cambios)) return Failure("Uno de los mínimos cambió durante la preparación. Actualice la consulta y confirme nuevamente.");
        int id = current[0].Id;
        if (lote)
        {
            var data = new DataTable(); data.Columns.Add("AlmacenVolumenProductoId",typeof(int)); data.Columns.Add("EmpresaSedeId",typeof(int)); data.Columns.Add("ProductoId",typeof(int)); data.Columns.Add("StockMinimo",typeof(decimal)); data.Columns.Add("Estado",typeof(string)); data.Columns.Add("UsuarioCreacionId",typeof(int));
            var byProduct = current.ToDictionary(r=>r.ProductoId);
            foreach (var change in request.Cambios) data.Rows.Add(byProduct[change.ProductoId].Id,request.EmpresaSedeId,change.ProductoId,change.StockMinimo,"A",userId);
            await conn.ExecuteAsync("sp_alm_vol_prod_actualizarLote", new { almacenVolumenProductoList=data.AsTableValuedParameter("dbo.AlmacenVolumenProductoType") }, tx, commandTimeout:180, commandType:CommandType.StoredProcedure);
        }
        else
        {
            var change = request.Cambios[0];
            if (id == 0) id = await InsertAsync(conn, tx, "sp_alm_vol_prod_insertar", new { empresaSedeId=request.EmpresaSedeId,productoId=change.ProductoId,stockMinimo=change.StockMinimo,usuarioAccionId=userId });
            else await conn.ExecuteAsync("sp_alm_vol_prod_actualizar",new { almacenVolumenProductoId=id,empresaSedeId=request.EmpresaSedeId,productoId=change.ProductoId,stockMinimo=change.StockMinimo,usuarioAccionId=userId },tx,commandType:CommandType.StoredProcedure);
        }
        tx.Commit(); return (true,lote ? "Importación de mínimos completada." : "Stock mínimo actualizado.",id);
    }

    private static bool MatchMinimumChanges(List<StockMinimoRowModel> current, List<StockMinimoCambio> changes)
    {
        if (current.Count != changes.Count || current.Select(r=>r.ProductoId).Distinct().Count()!=changes.Count) return false;
        var byProduct = current.ToDictionary(r=>r.ProductoId);
        return changes.All(c=>byProduct.TryGetValue(c.ProductoId,out var row) && row.Revision==c.Revision);
    }
}
