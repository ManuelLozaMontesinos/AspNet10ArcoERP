using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services;

public partial class InventarioService
{
    private const string SaldoFrom = @"
        FROM mov_alm_saldo mas JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id=mas.periodo_emp_sede_id
        JOIN almacen alm ON alm.almacen_id=mas.almacen_id JOIN empresa_sede sede ON sede.empresa_sede_id=alm.empresa_sede_id
        JOIN mes_contable mc ON mc.mes_contable_id=mas.mes_contable_id
        JOIN producto prd ON prd.producto_id=mas.producto_id
        JOIN unidad_medida um ON um.unidad_medida_id=prd.unidad_medida_id ";
    private const string SaldoWhere = @"
        WHERE pes.periodo_id=@PeriodoId AND pes.empresa_sede_id=@SiteId AND sede.empresa_id=@EmpresaId
          AND tp.empresa_id=@EmpresaId AND mc.mes_contable=@MesCodigo
          AND (@Search IS NULL OR prd.descripcion LIKE @Search)
          AND (@Codigo IS NULL OR prd.producto LIKE @Codigo)
          AND (@Referencia IS NULL OR prd.cod_referencial LIKE @Referencia) ";

    public async Task<SaldoConsultaPageModel> GetConsultaSaldosAsync(int empresaId, int periodoEmpresaSedeId, int usuarioId, InventarioReporteFiltro filtro, int page = 1, int top = 25, string? sort = null, string? dir = null, bool exportar = false)
    {
        var model = await PrepareReportAsync<SaldoConsultaPageModel>(empresaId, periodoEmpresaSedeId, usuarioId, filtro, "saldos", "Consulta de saldos");
        var columns = new Dictionary<string, string> {
            ["codigo"]="Codigo", ["descripcion"]="Descripcion", ["referencia"]="Referencia", ["unidad"]="Unidad",
            ["disponible"]="Disponible", ["reserva"]="Reserva", ["fisico"]="Fisico"
        };
        foreach (var warehouse in model.Almacenes) columns["almacen_" + warehouse.Id] = "[Almacen_" + warehouse.Id + "]";
        model.Ordenar(sort, dir, columns.Keys, "codigo");
        if (model.Errores.Count > 0) return model;
        using var conn = factory.CreateConnection();
        var args = ReportArgs(empresaId, model);
        string from = SaldoFrom + ProductoEmpresaJoin + SaldoWhere;
        int total = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM (SELECT mas.producto_id " + from + " GROUP BY mas.producto_id) resumen", args);
        PageReport(model, total, page, top, exportar);
        if (model.Errores.Count > 0) return model;
        string perWarehouse = string.Concat(model.Almacenes.Select(a => $", SUM(CASE WHEN mas.almacen_id={a.Id} THEN mas.cant_ant+mas.cant_ing-mas.cant_sal ELSE 0 END) AS [Almacen_{a.Id}]"));
        string query = @"SELECT prd.producto_id ProductoId, RTRIM(prd.producto) Codigo, prd.descripcion Descripcion,
                ISNULL(prd.cod_referencial,'') Referencia, um.abrev Unidad,
                SUM(mas.cant_ant+mas.cant_ing-mas.cant_sal) Fisico, SUM(mas.cant_reserva) Reserva,
                SUM(mas.cant_ant+mas.cant_ing-mas.cant_sal-mas.cant_reserva) Disponible " + perWarehouse + from + @"
            GROUP BY prd.producto_id,prd.producto,prd.descripcion,prd.cod_referencial,um.abrev " +
            $" ORDER BY {columns[model.Sort]} {model.Dir}, prd.producto_id ASC OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";
        args.Add("Offset", exportar ? 0 : (model.Page - 1) * model.PageSize); args.Add("Top", exportar ? MaxExportRows : model.PageSize);
        foreach (IDictionary<string, object> data in await conn.QueryAsync(query, args))
        {
            var row = new SaldoConsultaRowModel {
                ProductoId = (int)data["ProductoId"], Codigo = (string)data["Codigo"], Descripcion = (string)data["Descripcion"],
                Referencia = (string)data["Referencia"], Unidad = (string)data["Unidad"], Fisico = (decimal)data["Fisico"],
                Reserva = (decimal)data["Reserva"], Disponible = (decimal)data["Disponible"]
            };
            foreach (var warehouse in model.Almacenes) row.PorAlmacen[warehouse.Id] = (decimal)data["Almacen_" + warehouse.Id];
            model.Items.Add(row);
        }
        model.VisibleCount = model.Items.Count; return model;
    }

    public async Task<List<SaldoAlmacenDetalleModel>?> GetSaldoDetalleAsync(int empresaId, int periodoEmpresaSedeId, int usuarioId, int mes, int productoId)
    {
        if (productoId <= 0) return null;
        var model = await PrepareReportAsync<SaldoConsultaPageModel>(empresaId, periodoEmpresaSedeId, usuarioId,
            new InventarioReporteFiltro { Mes = mes, ProductoId = productoId }, "saldos", "Consulta de saldos");
        if (model.Errores.Count > 0) return null;
        using var conn = factory.CreateConnection();
        return (await conn.QueryAsync<SaldoAlmacenDetalleModel>(@"
            SELECT alm.nombre Almacen, alm.estado Estado, SUM(mas.cant_ant) Anterior, SUM(mas.cant_ing) Ingreso,
                   SUM(mas.cant_sal) Salida, SUM(mas.cant_ant+mas.cant_ing-mas.cant_sal) Fisico,
                   SUM(mas.cant_reserva) Reserva, SUM(mas.cant_ant+mas.cant_ing-mas.cant_sal-mas.cant_reserva) Disponible " +
            SaldoFrom + ProductoEmpresaJoin + SaldoWhere + " AND prd.producto_id=@ProductoId GROUP BY alm.almacen_id,alm.nombre,alm.estado ORDER BY alm.nombre,alm.almacen_id", ReportArgs(empresaId, model))).AsList();
    }
}
