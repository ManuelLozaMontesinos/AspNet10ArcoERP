using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services;

public partial class InventarioService
{
    private async Task<SaldoUnidadesPageModel> PrepareUnitBalanceAsync(int empresaId, int scopeId, int userId, InventarioReporteFiltro filter)
    {
        var model = await PrepareReportAsync<SaldoUnidadesPageModel>(empresaId, scopeId, userId, filter, "saldo-unidades", "Saldo de unidades");
        if (filter.Variante is not ("general" or "almacenes" or "detalle")) model.Errores.Add("Seleccione una modalidad de saldo válida.");
        if (filter.Modo != "mes" && !(filter.Variante == "detalle" && filter.Modo == "dia")) model.Errores.Add("La consulta al día está disponible en la modalidad detallada.");
        if (filter.Variante != "detalle" && filter.AlmacenId.HasValue) model.Errores.Add("El filtro de almacén corresponde a la modalidad detallada.");
        if (filter.Variante != "almacenes" && filter.IncluirReserva == true) model.Errores.Add("El descuento de reservas corresponde a la modalidad por almacenes.");
        filter.IncluirReserva ??= filter.Variante == "almacenes";
        return model;
    }

    private static (string Sql, DynamicParameters Args) UnitBalanceSql(int empresaId, SaldoUnidadesPageModel model)
    {
        var filter = model.Filtro; bool detail = filter.Variante == "detalle";
        var args = ReportArgs(empresaId, model);
        args.Add("MesCodigo", (filter.Modo == "dia" ? filter.Fecha!.Value.Month : filter.Mes!.Value).ToString("D2"));
        args.Add("DescontarReserva", filter.Variante == "almacenes" && filter.IncluirReserva == true);
        string movements = "WITH ";
        if (detail)
        {
            DateTime cutoff = filter.Modo == "dia" ? filter.Fecha!.Value.Date.AddDays(1) : new DateTime(model.Contexto.Anio, filter.Mes!.Value, 1).AddMonths(1);
            args.Add("Hasta", cutoff);
            // Preserve the legacy rounding of each movement to two decimals before summing.
            movements += @"movimientos AS (
                SELECT ma.periodo_emp_sede_id,ma.almacen_id,map.producto_id,ma.mes_contable_id,
                    SUM(CASE WHEN tm.tipo_mov_alma='I' THEN ROUND(map.cantidad,2) ELSE 0 END) Ingreso,
                    SUM(CASE WHEN tm.tipo_mov_alma='S' THEN ROUND(map.cantidad,2) ELSE 0 END) Salida
                FROM mov_alm_prod map JOIN mov_almacen ma ON ma.mov_almacen_id=map.mov_almacen_id
                JOIN tipo_mov_alma tm ON tm.tipo_mov_alma_id=ma.tipo_mov_alma_id
                JOIN mes_contable m ON m.mes_contable_id=ma.mes_contable_id
                JOIN periodo_emp_sede p ON p.periodo_emp_sede_id=ma.periodo_emp_sede_id
                WHERE p.periodo_id=@PeriodoId AND ma.almacen_id=@AlmacenId AND m.mes_contable=@MesCodigo
                  AND map.estado='A' AND ma.afecta_saldo='S' AND ma.fecha<@Hasta
                  AND (@ProductoId IS NULL OR map.producto_id=@ProductoId)
                GROUP BY ma.periodo_emp_sede_id,ma.almacen_id,map.producto_id,ma.mes_contable_id
            ), ";
        }
        string query = movements + @"stock AS (
            SELECT prd.producto_id ProductoId,RTRIM(prd.producto) Codigo,prd.descripcion Producto,um.nombre Unidad,
                alm.almacen_id AlmacenId,alm.nombre Almacen,alm.estado Estado,sede.nombre Sede,
                mas.cant_ant Anterior," + (detail ? "ISNULL(mov.Ingreso,0) Ingreso,ISNULL(mov.Salida,0) Salida," : "mas.cant_ing Ingreso,mas.cant_sal Salida,") + @"mas.cant_reserva Reserva " +
            SaldoFrom + ProductoEmpresaJoin + (detail ? @" LEFT JOIN movimientos mov ON mov.periodo_emp_sede_id=mas.periodo_emp_sede_id
                AND mov.almacen_id=mas.almacen_id AND mov.producto_id=mas.producto_id AND mov.mes_contable_id=mas.mes_contable_id " : "") + @"
            WHERE pes.periodo_id=@PeriodoId AND sede.empresa_id=@EmpresaId AND tp.empresa_id=@EmpresaId AND mc.mes_contable=@MesCodigo
              AND (@AlmacenId IS NULL OR mas.almacen_id=@AlmacenId) AND (@ProductoId IS NULL OR prd.producto_id=@ProductoId)
              AND (@Search IS NULL OR prd.producto LIKE @Search OR prd.descripcion LIKE @Search)
              AND (@Codigo IS NULL OR prd.producto LIKE @Codigo) AND (@Referencia IS NULL OR prd.cod_referencial LIKE @Referencia)
        ) ";
        return (query, args);
    }

    public async Task<SaldoUnidadesPageModel> GetSaldoUnidadesAsync(int empresaId, int scopeId, int userId, InventarioReporteFiltro filtro, int page = 1, int top = 25, string? sort = null, string? dir = null, bool exportar = false)
    {
        var model = await PrepareUnitBalanceAsync(empresaId, scopeId, userId, filtro); model.Paginar(0, page, top);
        var columns = new Dictionary<string, string> { ["codigo"]="Codigo", ["producto"]="Producto", ["unidad"]="Unidad", ["anterior"]="Anterior", ["ingreso"]="Ingreso", ["salida"]="Salida", ["saldo"]="Saldo" };
        model.Ordenar(sort, dir, columns.Keys, "codigo");
        if (model.Errores.Count > 0 || !model.Consultada) return model;
        using var conn = factory.CreateConnection();
        var (cte, args) = UnitBalanceSql(empresaId, model);
        if (filtro.Variante == "almacenes")
        {
            model.ColumnasAlmacen = (await conn.QueryAsync<LookupItemModel>(cte + "SELECT DISTINCT AlmacenId Id,CONCAT(Sede,' · ',Almacen) Nombre FROM stock ORDER BY Nombre,AlmacenId", args)).AsList();
            columns = new() { ["codigo"]="Codigo", ["producto"]="Producto", ["unidad"]="Unidad", ["saldo"]="Saldo" };
            foreach (var warehouse in model.ColumnasAlmacen) columns["almacen_" + warehouse.Id] = "[Almacen_" + warehouse.Id + "]";
        }
        model.Ordenar(sort, dir, columns.Keys, "codigo");
        int total = await conn.ExecuteScalarAsync<int>(cte + "SELECT COUNT(DISTINCT ProductoId) FROM stock", args);
        PageReport(model, total, page, top, exportar); if (model.Errores.Count > 0) return model;
        args.Add("Offset", exportar ? 0 : (model.Page - 1) * model.PageSize); args.Add("Top", exportar ? MaxExportRows : model.PageSize);
        string value = "Anterior+Ingreso-Salida-CASE WHEN @DescontarReserva=1 THEN Reserva ELSE 0 END";
        string warehouseColumns = string.Concat(model.ColumnasAlmacen.Select(a => $",SUM(CASE WHEN AlmacenId={a.Id} THEN {value} ELSE 0 END) [Almacen_{a.Id}]"));
        string query = cte + @"SELECT ProductoId,Codigo,Producto,Unidad,SUM(Anterior) Anterior,SUM(Ingreso) Ingreso,SUM(Salida) Salida,SUM(Reserva) Reserva,SUM(" + value + ") Saldo" + warehouseColumns +
            $" FROM stock GROUP BY ProductoId,Codigo,Producto,Unidad ORDER BY {columns[model.Sort]} {model.Dir},ProductoId ASC OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";
        foreach (IDictionary<string, object> row in await conn.QueryAsync(query, args))
        {
            var item = new SaldoUnidadesRowModel { ProductoId=(int)row["ProductoId"],Codigo=(string)row["Codigo"],Producto=(string)row["Producto"],Unidad=(string)row["Unidad"],
                Anterior=(decimal)row["Anterior"],Ingreso=(decimal)row["Ingreso"],Salida=(decimal)row["Salida"],Reserva=(decimal)row["Reserva"],Saldo=(decimal)row["Saldo"] };
            foreach (var warehouse in model.ColumnasAlmacen) item.PorAlmacen[warehouse.Id] = (decimal)row["Almacen_" + warehouse.Id];
            model.Items.Add(item);
        }
        model.VisibleCount = model.Items.Count; return model;
    }

    public async Task<List<SaldoAlmacenDetalleModel>?> GetSaldoUnidadesDetalleAsync(int empresaId, int scopeId, int userId, InventarioReporteFiltro filtro, int productoId)
    {
        if (productoId <= 0) return null;
        filtro.ProductoId = productoId; filtro.Consultar = true;
        var model = await PrepareUnitBalanceAsync(empresaId, scopeId, userId, filtro);
        if (model.Errores.Count > 0) return null;
        var (cte, args) = UnitBalanceSql(empresaId, model); using var conn = factory.CreateConnection();
        return (await conn.QueryAsync<SaldoAlmacenDetalleModel>(cte + @"SELECT CONCAT(Sede,' · ',Almacen) Almacen,Estado,
            SUM(Anterior) Anterior,SUM(Ingreso) Ingreso,SUM(Salida) Salida,SUM(Anterior+Ingreso-Salida) Fisico,
            SUM(Reserva) Reserva,SUM(Anterior+Ingreso-Salida-Reserva) Disponible FROM stock
            GROUP BY AlmacenId,Sede,Almacen,Estado ORDER BY Sede,Almacen,AlmacenId", args)).AsList();
    }
}
