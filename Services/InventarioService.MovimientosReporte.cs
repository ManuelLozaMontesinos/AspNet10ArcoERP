using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services;

public partial class InventarioService
{
    private const string DocumentoMovimientoSql = @"
        CONCAT(CASE WHEN ma.tipo_doc_com_id IS NULL THEN '' ELSE tdc.abrev END,
               CASE WHEN ma.serie_doc<>'' THEN CONCAT(CASE WHEN ma.tipo_doc_com_id IS NULL THEN '' ELSE ':' END,ma.serie_doc) ELSE '' END,
               CASE WHEN ma.numero_doc<>'' THEN CONCAT(CASE WHEN ma.serie_doc='' THEN '' ELSE '-' END,ma.numero_doc) ELSE '' END) ";

    private const string ReporteMovimientoSelect = @"
        SELECT map.mov_alm_prod_id Id, ma.mov_almacen_id MovimientoId, ma.fecha Fecha, RTRIM(ma.mov_almacen) Operacion,
               ISNULL(solicitante.razon_social,'') Solicitante, tma.tipo_mov_alma TipoMovimiento,
               ma.orden_cs OrdenCompra, ISNULL(prv.razon_social,'') Tercero, ma.nro_guia_rem Guia, " + DocumentoMovimientoSql + @" Documento,
               ma.observacion Observacion, RTRIM(prd.producto) Codigo, prd.descripcion Producto, um.abrev Unidad,
               map.descripcion Descripcion, map.cantidad Cantidad,
               CASE WHEN tma.tipo_mov_alma='I' THEN map.valor_unitario ELSE 0 END ValorUnitario ";
    private const string ReporteMovimientoFrom = @"
        FROM mov_alm_prod map JOIN mov_almacen ma ON ma.mov_almacen_id=map.mov_almacen_id
        JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id=ma.periodo_emp_sede_id
        JOIN almacen alm ON alm.almacen_id=ma.almacen_id JOIN empresa_sede sede ON sede.empresa_sede_id=alm.empresa_sede_id
        JOIN mes_contable mc ON mc.mes_contable_id=ma.mes_contable_id
        JOIN trans_alma ta ON ta.trans_alma_id=ma.trans_alma_id AND ta.empresa_id=@EmpresaId
        JOIN tipo_mov_alma tma ON tma.tipo_mov_alma_id=ma.tipo_mov_alma_id
        LEFT JOIN proveedor prv ON prv.proveedor_id=ma.proveedor_id AND prv.empresa_id=@EmpresaId
        LEFT JOIN personal personal ON personal.personal_id=ma.personal_id
        LEFT JOIN proveedor solicitante ON solicitante.proveedor_id=personal.proveedor_id AND solicitante.empresa_id=@EmpresaId
        LEFT JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id=ma.tipo_doc_com_id
        JOIN producto prd ON prd.producto_id=map.producto_id JOIN unidad_medida um ON um.unidad_medida_id=prd.unidad_medida_id ";

    public async Task<MovimientoReportePageModel> GetReporteMovimientosAsync(int empresaId, int periodoEmpresaSedeId, int usuarioId, InventarioReporteFiltro filtro, int page = 1, int top = 25, string? sort = null, string? dir = null, bool exportar = false)
    {
        var model = await PrepareReportAsync<MovimientoReportePageModel>(empresaId, periodoEmpresaSedeId, usuarioId, filtro, "movimientos", "Reporte de movimientos");
        var columns = new Dictionary<string, string> {
            ["fecha"]="ma.fecha", ["operacion"]="ma.mov_almacen", ["solicitante"]="solicitante.razon_social", ["tipo"]="tma.tipo_mov_alma",
            ["orden"]="ma.orden_cs", ["tercero"]="prv.razon_social", ["guia"]="ma.nro_guia_rem", ["documento"]="Documento",
            ["codigo"]="prd.producto", ["producto"]="prd.descripcion", ["unidad"]="um.abrev", ["cantidad"]="map.cantidad",
            ["valor"]="ValorUnitario", ["observacion"]="ma.observacion"
        };
        model.Ordenar(sort, dir, columns.Keys, "fecha");
        if (model.Contexto.PeriodoId <= 0) return model;
        using var conn = factory.CreateConnection();
        model.Transacciones = (await conn.QueryAsync<LookupItemModel>("SELECT trans_alma_id Id,RTRIM(trans_alma) Codigo,nombre Nombre FROM trans_alma WHERE empresa_id=@EmpresaId AND estado='A' AND trans_alma<>'S11' ORDER BY nombre,trans_alma_id", new { EmpresaId = empresaId })).AsList();
        if (filtro.TransaccionId.HasValue && !model.Transacciones.Any(t => t.Id == filtro.TransaccionId)) model.Errores.Add("Seleccione una transacción habilitada de la empresa actual.");
        if (model.Errores.Count > 0 || !model.Consultada) return model;
        var args = ReportArgs(empresaId, model);
        args.Add("FechaDesde", filtro.Modo == "dia" ? filtro.Fecha!.Value.Date : (DateTime?)null);
        args.Add("FechaHasta", filtro.Modo == "dia" ? filtro.Fecha!.Value.Date.AddDays(1) : (DateTime?)null);
        args.Add("MesFiltro", filtro.Modo == "mes" ? filtro.Mes!.Value.ToString("D2") : null);
        string from = ReporteMovimientoFrom + ProductoEmpresaJoin + @"
            WHERE ma.periodo_emp_sede_id=@ScopeId AND sede.empresa_id=@EmpresaId AND tp.empresa_id=@EmpresaId
              AND ma.almacen_id=@AlmacenId AND ma.trans_alma_id=@TransaccionId AND map.estado='A' AND ma.afecta_saldo='S'
              AND (@MesFiltro IS NULL OR mc.mes_contable=@MesFiltro)
              AND (@FechaDesde IS NULL OR (ma.fecha>=@FechaDesde AND ma.fecha<@FechaHasta))
              AND (@ProveedorId IS NULL OR ma.proveedor_id=@ProveedorId)
              AND (@Search IS NULL OR prd.producto LIKE @Search OR prd.descripcion LIKE @Search OR map.descripcion LIKE @Search OR ma.mov_almacen LIKE @Search) ";
        int total = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) " + from, args);
        PageReport(model, total, page, top, exportar); if (model.Errores.Count > 0) return model;
        args.Add("Offset", exportar ? 0 : (model.Page - 1) * model.PageSize); args.Add("Top", exportar ? MaxExportRows : model.PageSize);
        model.Items = (await conn.QueryAsync<MovimientoReporteRowModel>(ReporteMovimientoSelect + from +
            $" ORDER BY {columns[model.Sort]} {model.Dir},map.mov_alm_prod_id ASC OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY", args)).AsList();
        model.VisibleCount = model.Items.Count; return model;
    }

    public async Task<InventarioMovimientoDetalleModel?> GetReporteMovimientoDetalleAsync(int empresaId, int periodoEmpresaSedeId, int usuarioId, int id, bool todasSedes)
    {
        if (id <= 0) return null;
        var model = await PrepareReportAsync<InventarioReporteModel>(empresaId, periodoEmpresaSedeId, usuarioId, new(), todasSedes ? "kardex" : "movimientos", "Detalle");
        if (model.Errores.Count > 0) return null;
        using var conn = factory.CreateConnection();
        var args = ReportArgs(empresaId, model); args.Add("Id", id);
        // Kardex selects the accounting period and an authorized warehouse; use the same scope for its detail.
        // The warehouse ID is checked against the user's allowed list below.
        string scope = todasSedes ? "pes.periodo_id=@PeriodoId" : "ma.periodo_emp_sede_id=@ScopeId";
        var detail = await conn.QuerySingleOrDefaultAsync<InventarioMovimientoDetalleModel>(@"
            SELECT ma.mov_almacen_id Id,ma.almacen_id AlmacenId,RTRIM(ma.mov_almacen) Operacion,ma.fecha Fecha,alm.nombre Almacen,ta.nombre Transaccion,
                   ISNULL(prv.razon_social,'') Tercero,ma.nro_guia_rem Guia," + DocumentoMovimientoSql + @" Documento,ma.observacion Observacion
            FROM mov_almacen ma JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id=ma.periodo_emp_sede_id
            JOIN almacen alm ON alm.almacen_id=ma.almacen_id JOIN empresa_sede sede ON sede.empresa_sede_id=alm.empresa_sede_id
            JOIN trans_alma ta ON ta.trans_alma_id=ma.trans_alma_id AND ta.empresa_id=@EmpresaId
            LEFT JOIN proveedor prv ON prv.proveedor_id=ma.proveedor_id AND prv.empresa_id=@EmpresaId
            LEFT JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id=ma.tipo_doc_com_id
            WHERE ma.mov_almacen_id=@Id AND sede.empresa_id=@EmpresaId AND ma.afecta_saldo='S' AND " + scope, args);
        if (detail is null || !model.Almacenes.Any(a => a.Id == detail.AlmacenId)) return null;
        detail.Productos = (await conn.QueryAsync<MovimientoReporteRowModel>(ReporteMovimientoSelect + ReporteMovimientoFrom + ProductoEmpresaJoin +
            "WHERE ma.mov_almacen_id=@Id AND sede.empresa_id=@EmpresaId AND tp.empresa_id=@EmpresaId AND map.estado='A' ORDER BY map.mov_alm_prod_id", args)).AsList();
        return detail;
    }
}
