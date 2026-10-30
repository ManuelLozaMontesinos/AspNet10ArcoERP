using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services;

public partial class InventarioService
{
    public async Task<SaldoValoradoPageModel> GetSaldoValoradoAsync(int empresaId, int scopeId, int userId, InventarioReporteFiltro filtro, int page = 1, int top = 25, string? sort = null, string? dir = null, bool exportar = false)
    {
        var model = await PrepareReportAsync<SaldoValoradoPageModel>(empresaId, scopeId, userId, filtro, "saldo-valorado", "Saldo valorizado"); model.Paginar(0, page, top);
        var columns = new Dictionary<string, string> { ["sede"]="Sede",["codigo"]="Codigo",["producto"]="Producto",["unidad"]="Unidad",["anterior"]="Anterior",["valoranterior"]="ValorUnitarioAnterior",["ingreso"]="Ingreso",["salida"]="Salida",["reserva"]="Reserva",["saldo"]="Saldo",["valor"]="ValorUnitario",["importe"]="Importe" };
        model.Ordenar(sort, dir, columns.Keys, "codigo");
        if (filtro.Modo != "mes") model.Errores.Add("El saldo valorizado se consulta por mes contable.");
        if (filtro.AlmacenId.HasValue) model.Errores.Add("El saldo valorizado es consolidado de la empresa. Quite el filtro de almacén.");
        if (model.Errores.Count > 0 || !model.Consultada) return model;
        using var conn = factory.CreateConnection(); var args = ReportArgs(empresaId, model);
        string cte = @"WITH saldos AS (
            SELECT mas.mov_alm_s_con_id Id,s.nombre Sede,RTRIM(prd.producto) Codigo,prd.descripcion Producto,um.nombre Unidad,
                mas.cant_ant Anterior,mas.valor_unit_ant ValorUnitarioAnterior,mas.cant_ing Ingreso,mas.cant_sal Salida,mas.cant_reserva Reserva,
                mas.cant_ant+mas.cant_ing-mas.cant_sal-mas.cant_reserva Saldo,mas.valor_unit ValorUnitario,
                (mas.cant_ant+mas.cant_ing-mas.cant_sal-mas.cant_reserva)*mas.valor_unit Importe
            FROM mov_alm_s_con mas JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id=mas.periodo_emp_sede_id
            JOIN empresa_sede s ON s.empresa_sede_id=pes.empresa_sede_id
            JOIN mes_contable mc ON mc.mes_contable_id=mas.mes_contable_id
            JOIN producto prd ON prd.producto_id=mas.producto_id JOIN unidad_medida um ON um.unidad_medida_id=prd.unidad_medida_id " + ProductoEmpresaJoin + @"
            WHERE pes.periodo_id=@PeriodoId AND s.empresa_id=@EmpresaId AND tp.empresa_id=@EmpresaId AND mc.mes_contable=@MesCodigo
              AND (@ProductoId IS NULL OR prd.producto_id=@ProductoId)
              AND (@Search IS NULL OR prd.producto LIKE @Search OR prd.descripcion LIKE @Search)
              AND (@Codigo IS NULL OR prd.producto LIKE @Codigo) AND (@Referencia IS NULL OR prd.cod_referencial LIKE @Referencia)
        ) ";
        PageReport(model, await conn.ExecuteScalarAsync<int>(cte + "SELECT COUNT(*) FROM saldos", args), page, top, exportar);
        if (model.Errores.Count > 0) return model;
        args.Add("Offset", exportar ? 0 : (model.Page - 1) * model.PageSize); args.Add("Top", exportar ? MaxExportRows : model.PageSize);
        model.Items = (await conn.QueryAsync<SaldoValoradoRowModel>(cte + $"SELECT * FROM saldos ORDER BY {columns[model.Sort]} {model.Dir},Id ASC OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY", args)).AsList();
        model.VisibleCount = model.Items.Count; return model;
    }
}
