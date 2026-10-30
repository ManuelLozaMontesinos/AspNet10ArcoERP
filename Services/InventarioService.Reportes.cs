using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services;

public partial class InventarioService
{
    private const int MaxExportRows = 100000;
    private const string ProductoEmpresaJoin = @"
        JOIN familia_producto fp ON fp.familia_producto_id=prd.familia_producto_id
        JOIN clase_producto cp ON cp.clase_producto_id=fp.clase_producto_id
        JOIN grupo_producto gp ON gp.grupo_producto_id=cp.grupo_producto_id
        JOIN tipo_producto tp ON tp.tipo_producto_id=gp.tipo_producto_id ";

    private async Task<T> PrepareReportAsync<T>(int empresaId, int scopeId, int userId, InventarioReporteFiltro filtro, string tipo, string titulo) where T : InventarioReporteModel, new()
    {
        var model = new T { Tipo = tipo, Titulo = titulo, Filtro = filtro };
        if (empresaId <= 0 || scopeId <= 0 || userId <= 0) { model.Errores.Add("Seleccione un contexto de trabajo válido."); return model; }
        using var conn = factory.CreateConnection();
        var scope = await GetInventarioScopeAsync(conn, empresaId, scopeId, userId);
        if (scope is null) { model.Errores.Add("El período y la sede seleccionados no están habilitados."); return model; }
        model.Contexto = scope;
        bool allSites = tipo is "movimientos" or "saldo-valorado" || ((tipo is "kardex" or "saldo-unidades") && scope.Administrador);
        model.Almacenes = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT a.almacen_id Id, a.abrev Codigo, a.nombre Nombre FROM almacen a
            JOIN empresa_sede s ON s.empresa_sede_id=a.empresa_sede_id
            WHERE s.empresa_id=@EmpresaId AND s.estado='A' AND a.estado='A'
              AND (@AllSites=1 OR a.empresa_sede_id=@SiteId) ORDER BY a.nombre,a.almacen_id",
            new { EmpresaId = empresaId, AllSites = allSites, SiteId = scope.EmpresaSedeId })).AsList();
        model.Meses = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT TRY_CONVERT(int,mes_contable) Id, RTRIM(mes_contable) Codigo, nombre Nombre
            FROM mes_contable WHERE estado='A' AND mes_contable BETWEEN @FirstMonth AND @LastMonth ORDER BY mes_contable",
            new { FirstMonth = tipo == "saldo-valorado" ? "00" : "01", LastMonth = tipo == "saldo-valorado" ? "13" : "12" })).AsList();
        filtro.Modo = string.IsNullOrWhiteSpace(filtro.Modo) ? "mes" : filtro.Modo.Trim().ToLowerInvariant();
        filtro.Mes ??= DateTime.Today.Month;
        if (filtro.Modo is not ("mes" or "dia" or "anual") || (tipo != "kardex" && filtro.Modo == "anual") || (tipo == "saldos" && filtro.Modo != "mes"))
            model.Errores.Add("Seleccione un modo de consulta válido.");
        if (filtro.Modo == "mes" && !model.Meses.Any(m => m.Id == filtro.Mes)) model.Errores.Add("Seleccione un mes contable vigente.");
        if (filtro.Modo == "dia" && (!filtro.Fecha.HasValue || filtro.Fecha.Value.Year is < 1900 or > 9998)) model.Errores.Add("Seleccione una fecha de consulta válida.");
        if (scope.Anio is < 1900 or > 9998) model.Errores.Add("El ejercicio seleccionado no admite fechas de consulta.");
        if (filtro.AlmacenId.HasValue && !model.Almacenes.Any(a => a.Id == filtro.AlmacenId)) model.Errores.Add("El almacén no pertenece al ámbito habilitado.");
        if (filtro.ProductoId.HasValue)
        {
            model.ProductoNombre = await conn.QuerySingleOrDefaultAsync<string>("SELECT CONCAT(RTRIM(prd.producto),' · ',prd.descripcion) FROM producto prd " + ProductoEmpresaJoin +
                " WHERE prd.producto_id=@Id AND tp.empresa_id=@EmpresaId", new { Id = filtro.ProductoId, EmpresaId = empresaId }) ?? "";
            if (model.ProductoNombre.Length == 0) model.Errores.Add("El producto no pertenece a la empresa actual.");
        }
        if (filtro.ProveedorId.HasValue)
        {
            model.ProveedorNombre = await conn.QuerySingleOrDefaultAsync<string>("SELECT CONCAT(RTRIM(proveedor),' · ',ISNULL(razon_social,'')) FROM proveedor WHERE proveedor_id=@Id AND empresa_id=@EmpresaId", new { Id = filtro.ProveedorId, EmpresaId = empresaId }) ?? "";
            if (model.ProveedorNombre.Length == 0) model.Errores.Add("El tercero no pertenece a la empresa actual.");
        }
        if (filtro.Search?.Length > 150 || filtro.Codigo?.Length > 20 || filtro.Referencia?.Length > 20) model.Errores.Add("Reduzca la longitud de los filtros de texto.");
        ValidateRequiredReportFields(model);
        return model;
    }

    private static void PageReport(InventarioReporteModel model, int total, int page, int top, bool exportar)
    {
        model.Paginar(total, exportar ? 1 : page, top);
        if (exportar && total > MaxExportRows) model.Errores.Add($"La consulta supera {MaxExportRows:N0} filas. Reduzca los filtros antes de exportar o imprimir.");
    }

    private static DynamicParameters ReportArgs(int empresaId, InventarioReporteModel model)
        => new(new {
            EmpresaId = empresaId, ScopeId = model.Contexto.PeriodoEmpresaSedeId, PeriodoId = model.Contexto.PeriodoId,
            SiteId = model.Contexto.EmpresaSedeId, MesCodigo = model.Filtro.Mes?.ToString("D2"),
            model.Filtro.AlmacenId, model.Filtro.ProductoId, model.Filtro.TransaccionId, model.Filtro.ProveedorId,
            Search = Search(model.Filtro.Search), Codigo = Search(model.Filtro.Codigo), Referencia = Search(model.Filtro.Referencia)
        });

    public async Task<InventarioLookupPageModel> GetReporteLookupAsync(int empresaId, string tipo, string? search = null, int page = 1, string? sort = null, string? dir = null)
    {
        if (tipo is not ("producto" or "proveedor")) throw new ArgumentException("Tipo de selector no válido.", nameof(tipo));
        bool product = tipo == "producto";
        var fields = new Dictionary<string, string> { ["codigo"] = product ? "prd.producto" : "p.proveedor", ["nombre"] = product ? "prd.descripcion" : "ISNULL(NULLIF(p.razon_social,''),p.proveedor)" };
        var model = new InventarioLookupPageModel(); model.Ordenar(sort, dir, fields.Keys, "nombre");
        string from = product ? "FROM producto prd " + ProductoEmpresaJoin : "FROM proveedor p ";
        string filter = product
            ? "WHERE tp.empresa_id=@EmpresaId AND tp.tipo_producto='P' AND prd.estado='A' AND (@Search IS NULL OR prd.producto LIKE @Search OR prd.descripcion LIKE @Search OR prd.cod_referencial LIKE @Search)"
            : "WHERE p.empresa_id=@EmpresaId AND p.estado='A' AND (@Search IS NULL OR p.razon_social LIKE @Search OR p.num_doc_iden LIKE @Search OR p.numero_ruc LIKE @Search OR p.proveedor LIKE @Search)";
        string id = product ? "prd.producto_id" : "p.proveedor_id";
        using var conn = factory.CreateConnection();
        var args = new DynamicParameters(new { EmpresaId = empresaId, Search = Search(search) });
        model.Paginar(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) " + from + filter, args), page, 25);
        args.Add("Offset", (model.Page - 1) * model.PageSize);
        model.Items = (await conn.QueryAsync<LookupItemModel>($"SELECT {id} Id, RTRIM({fields["codigo"]}) Codigo, {fields["nombre"]} Nombre " + from + filter +
            $" ORDER BY {fields[model.Sort]} {model.Dir}, {id} ASC OFFSET @Offset ROWS FETCH NEXT 25 ROWS ONLY", args)).AsList();
        model.VisibleCount = model.Items.Count; return model;
    }

    private static void ValidateRequiredReportFields(InventarioReporteModel model)
    {
        var filter = model.Filtro;
        model.Consultada = model.Tipo == "saldos" || filter.Consultar;
        if (!model.Consultada) return;
        bool needsWarehouse = model.Tipo is "kardex" or "movimientos" || (model.Tipo == "saldo-unidades" && filter.Variante == "detalle");
        if (needsWarehouse && !filter.AlmacenId.HasValue) model.Errores.Add("Seleccione un almacén para consultar.");
        if (model.Tipo == "kardex" && filter.Modo == "anual" && !filter.ProductoId.HasValue) model.Errores.Add("Seleccione un producto para el Kardex anual.");
        if (model.Tipo == "movimientos" && !filter.TransaccionId.HasValue) model.Errores.Add("Seleccione una transacción para consultar.");
    }
}
