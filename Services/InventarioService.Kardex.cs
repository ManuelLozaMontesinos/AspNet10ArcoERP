using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services;

public partial class InventarioService
{
    public async Task<KardexUnidadPageModel> GetKardexUnidadesAsync(int empresaId, int periodoEmpresaSedeId, int usuarioId, InventarioReporteFiltro filtro, int page = 1, int top = 25, string? sort = null, string? dir = null, bool exportar = false)
    {
        var model = await PrepareReportAsync<KardexUnidadPageModel>(empresaId, periodoEmpresaSedeId, usuarioId, filtro, "kardex", "Kardex de unidades");
        var columns = new Dictionary<string, string> {
            ["codigo"]="Codigo", ["producto"]="Producto", ["fecha"]="Fecha", ["operacion"]="Operacion", ["tipo"]="TipoMovimiento",
            ["transaccion"]="Transaccion", ["unidad"]="Unidad", ["ingreso"]="Ingreso", ["salida"]="Salida", ["saldo"]="Saldo"
        };
        model.Ordenar(sort, dir, columns.Keys, "fecha");
        if (model.Errores.Count > 0 || !model.Consultada) return model;
        int month = filtro.Modo == "anual" ? 1 : filtro.Modo == "dia" ? filtro.Fecha!.Value.Month : filtro.Mes!.Value;
        var start = new DateTime(model.Contexto.Anio, month, 1);
        DateTime? end = filtro.Modo == "dia" ? filtro.Fecha!.Value.Date.AddDays(1) : null;
        var args = ReportArgs(empresaId, model); args.Add("Desde", start); args.Add("Hasta", end); args.Add("MesInicial", month.ToString("D2"));
        // Running totals are computed in the complete chronological set before display sorting/paging.
        // Annual mode uses January's opening balance plus all ordinary monthly movements, without a shared staging table.
        string cte = @"WITH movimientos AS (
            SELECT map.mov_alm_prod_id Id, ma.mov_almacen_id MovimientoId, prd.producto_id ProductoId,
                   RTRIM(prd.producto) Codigo, prd.descripcion Producto, um.nombre Unidad, ma.fecha Fecha,
                   RTRIM(ma.mov_almacen) Operacion, tma.tipo_mov_alma TipoMovimiento, ta.nombre Transaccion,
                   ma.nro_guia_rem Guia, " + DocumentoMovimientoSql + @" Documento, map.descripcion Descripcion,
                   CASE WHEN tma.tipo_mov_alma='I' THEN map.cantidad ELSE 0 END Ingreso,
                   CASE WHEN tma.tipo_mov_alma='S' THEN map.cantidad ELSE 0 END Salida,
                   CASE WHEN tma.tipo_mov_alma='I' THEN map.cantidad ELSE -map.cantidad END Variacion,
                   1 OrdenRegistro
            FROM mov_alm_prod map JOIN mov_almacen ma ON ma.mov_almacen_id=map.mov_almacen_id
            JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id=ma.periodo_emp_sede_id
            JOIN almacen alm ON alm.almacen_id=ma.almacen_id JOIN empresa_sede sede ON sede.empresa_sede_id=alm.empresa_sede_id
            JOIN mes_contable mc ON mc.mes_contable_id=ma.mes_contable_id
            JOIN tipo_mov_alma tma ON tma.tipo_mov_alma_id=ma.tipo_mov_alma_id
            JOIN trans_alma ta ON ta.trans_alma_id=ma.trans_alma_id AND ta.empresa_id=@EmpresaId
            LEFT JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id=ma.tipo_doc_com_id
            JOIN producto prd ON prd.producto_id=map.producto_id JOIN unidad_medida um ON um.unidad_medida_id=prd.unidad_medida_id " + ProductoEmpresaJoin + @"
            WHERE pes.periodo_id=@PeriodoId AND sede.empresa_id=@EmpresaId AND tp.empresa_id=@EmpresaId
              AND ma.almacen_id=@AlmacenId AND map.estado='A' AND ma.afecta_saldo='S'
              AND mc.mes_contable BETWEEN @MesInicial AND @MesFinal AND (@Hasta IS NULL OR ma.fecha<@Hasta)
              AND (@ProductoId IS NULL OR prd.producto_id=@ProductoId)
            UNION ALL
            SELECT mas.mov_alm_saldo_id, 0, prd.producto_id, RTRIM(prd.producto), prd.descripcion, um.nombre,
                   @Desde, '', 'I', 'SALDO INICIAL', '', '', '', 0, 0, mas.cant_ant, 0
            FROM mov_alm_saldo mas JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id=mas.periodo_emp_sede_id
            JOIN almacen alm ON alm.almacen_id=mas.almacen_id JOIN empresa_sede sede ON sede.empresa_sede_id=alm.empresa_sede_id
            JOIN mes_contable mc ON mc.mes_contable_id=mas.mes_contable_id
            JOIN producto prd ON prd.producto_id=mas.producto_id JOIN unidad_medida um ON um.unidad_medida_id=prd.unidad_medida_id " + ProductoEmpresaJoin + @"
            WHERE pes.periodo_id=@PeriodoId AND sede.empresa_id=@EmpresaId AND tp.empresa_id=@EmpresaId
              AND mas.almacen_id=@AlmacenId AND mc.mes_contable=@MesInicial
              AND (@ProductoId IS NULL OR prd.producto_id=@ProductoId)
        ), kardex AS (
            SELECT *, SUM(Variacion) OVER(PARTITION BY ProductoId ORDER BY Fecha,TipoMovimiento,OrdenRegistro,MovimientoId,Id ROWS UNBOUNDED PRECEDING) Saldo
            FROM movimientos
        ) ";
        args.Add("MesFinal", filtro.Modo == "anual" ? "12" : month.ToString("D2"));
        using var conn = factory.CreateConnection();
        int total = await conn.ExecuteScalarAsync<int>(cte + "SELECT COUNT(*) FROM kardex", args);
        PageReport(model, total, page, top, exportar); if (model.Errores.Count > 0) return model;
        args.Add("Offset", exportar ? 0 : (model.Page - 1) * model.PageSize); args.Add("Top", exportar ? MaxExportRows : model.PageSize);
        string ties = string.Join(",", new[] { "ProductoId", "Fecha", "TipoMovimiento", "OrdenRegistro", "MovimientoId", "Id" }.Where(c => c != columns[model.Sort]));
        model.Items = (await conn.QueryAsync<KardexUnidadRowModel>(cte + $"SELECT * FROM kardex ORDER BY {columns[model.Sort]} {model.Dir},{ties} OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY", args)).AsList();
        model.VisibleCount = model.Items.Count; return model;
    }
}
