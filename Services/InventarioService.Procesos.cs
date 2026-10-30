using System.Data;
using System.Security.Cryptography;
using System.Text;
using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services;

public partial class InventarioService
{
    private const string CierreSelect = @"SELECT mac.mov_alm_cierre_id Id,mac.mes_contable_id MesContableId,
        mc.mes_contable MesCodigo,mc.nombre MesNombre,mac.fecha_cierre FechaCierre,
        eac.estado_ape_cie Estado,eac.nombre EstadoNombre,mac.tiene_mov_p_cierre MovimientoPostCierre,
        COALESCE(mac.fecha_modifica,mac.fecha_creacion) FechaRevision ";
    private const string CierreFrom = @" FROM mov_alm_cierre mac
        JOIN mes_contable mc ON mc.mes_contable_id=mac.mes_contable_id
        JOIN estado_ape_cie eac ON eac.estado_ape_cie_id=mac.estado_ape_cie_id ";

    public async Task<InventarioProcesoPageModel> GetProcesosInventarioAsync(int empresaId, int scopeId, int userId, int? almacenId = null, string? search = null, string? estado = null, int page = 1, int top = 25, string? sort = null, string? dir = null)
    {
        var model = new InventarioProcesoPageModel { AlmacenId = almacenId, Search = search, Estado = estado };
        var columns = new Dictionary<string, string> { ["mes"] = "mc.mes_contable", ["fecha"] = "mac.fecha_cierre", ["estado"] = "eac.nombre", ["posterior"] = "mac.tiene_mov_p_cierre" };
        model.Ordenar(sort, dir, columns.Keys, "mes"); model.Paginar(0, page, top);
        if (empresaId <= 0 || scopeId <= 0 || userId <= 0) { model.Errores.Add("Seleccione un contexto de trabajo válido."); return model; }
        using var conn = factory.CreateConnection();
        var scope = await GetInventarioScopeAsync(conn, empresaId, scopeId, userId);
        if (scope is null) { model.Errores.Add("El período y la sede seleccionados no están habilitados."); return model; }
        model.Contexto = scope;
        model.Almacenes = (await conn.QueryAsync<LookupItemModel>(@"SELECT almacen_id Id,abrev Codigo,nombre Nombre FROM almacen
            WHERE empresa_sede_id=@SiteId AND estado='A' ORDER BY nombre,almacen_id", new { SiteId = scope.EmpresaSedeId })).AsList();
        if (search?.Length > 150 || (!string.IsNullOrEmpty(estado) && estado is not ("A" or "C"))) model.Errores.Add("Revise los filtros de búsqueda y estado.");
        if (!almacenId.HasValue) return model;
        if (!model.Almacenes.Any(a => a.Id == almacenId)) model.Errores.Add("Seleccione un almacén activo de la sede de trabajo.");
        if (model.Errores.Count > 0) return model;
        string from = CierreFrom + @" WHERE mac.periodo_emp_sede_id=@ScopeId AND mac.almacen_id=@Warehouse
            AND (@Search IS NULL OR mc.nombre LIKE @Search OR mc.mes_contable LIKE @Search)
            AND (@Estado IS NULL OR eac.estado_ape_cie=@Estado)";
        var args = new DynamicParameters(new { ScopeId = scopeId, Warehouse = almacenId, Search = Search(search), Estado = string.IsNullOrEmpty(estado) ? null : estado });
        model.Paginar(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*)" + from, args), page, top);
        args.Add("Offset", (model.Page - 1) * model.PageSize); args.Add("Top", model.PageSize);
        model.Items = (await conn.QueryAsync<InventarioCierreRowModel>(CierreSelect + from + $" ORDER BY {columns[model.Sort]} {model.Dir},mac.mov_alm_cierre_id OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY", args)).AsList();
        model.VisibleCount = model.Items.Count; return model;
    }

    public async Task<InventarioProcesoPreparacionModel> GetPreparacionProcesoAsync(int empresaId, int scopeId, int userId, string operacion, int almacenId, int? mesContableId = null)
    {
        if (!ValidProcessInput(empresaId, scopeId, userId, operacion, almacenId, mesContableId))
            return new() { Errores = new() { "Seleccione una operación, almacén y mes válidos." } };
        using var conn = factory.CreateConnection();
        return await PrepareProcessAsync(conn, null, empresaId, scopeId, userId, operacion, almacenId, mesContableId);
    }

    private static bool ValidProcessInput(int empresaId, int scopeId, int userId, string operation, int warehouse, int? month)
        => empresaId > 0 && scopeId > 0 && userId > 0 && warehouse > 0 && operation is "apertura" or "cierre" or "reproceso" or "anual"
            && (operation == "anual" ? !month.HasValue : month is > 0);

    private static async Task<InventarioProcesoPreparacionModel> PrepareProcessAsync(IDbConnection conn, IDbTransaction? tx, int empresaId, int scopeId, int userId, string operation, int warehouseId, int? monthId)
    {
        var model = new InventarioProcesoPreparacionModel { ContextoId = scopeId, Operacion = operation, AlmacenId = warehouseId, MesContableId = monthId, HastaMes = DateTime.Today.Month };
        var scope = await GetInventarioScopeAsync(conn, empresaId, scopeId, userId, tx, tx is not null);
        if (scope is null || scope.Anio is < 1900 or > 9998) { model.Errores.Add("El período y la sede seleccionados no están habilitados para este proceso."); return model; }
        model.Anio = scope.Anio; model.Sede = scope.Sede;
        var warehouse = await conn.QuerySingleOrDefaultAsync<(string Nombre, string Venta)?>(@"SELECT nombre,es_venta FROM almacen " + (tx is null ? "" : "WITH (UPDLOCK,HOLDLOCK) ") + @"
            WHERE almacen_id=@Warehouse AND empresa_sede_id=@SiteId AND estado='A'", new { Warehouse = warehouseId, SiteId = scope.EmpresaSedeId }, tx);
        if (!warehouse.HasValue) { model.Errores.Add("Seleccione un almacén activo de la sede de trabajo."); return model; }
        model.Almacen = warehouse.Value.Nombre; model.EsVenta = warehouse.Value.Venta == "S";
        string from = tx is null ? CierreFrom : CierreFrom.Replace("mov_alm_cierre mac", "mov_alm_cierre mac WITH (UPDLOCK,HOLDLOCK)");
        var rows = (await conn.QueryAsync<InventarioCierreRowModel>(CierreSelect + from + @"
            WHERE mac.periodo_emp_sede_id=@ScopeId AND mac.almacen_id=@Warehouse
              AND (@Month IS NULL OR mac.mes_contable_id=@Month) ORDER BY mc.mes_contable,mac.mov_alm_cierre_id",
            new { ScopeId = scopeId, Warehouse = warehouseId, Month = monthId }, tx)).AsList();
        if (operation == "anual")
        {
            model.Meses = rows.Where(r => string.CompareOrdinal(r.MesCodigo, "01") >= 0 && string.CompareOrdinal(r.MesCodigo, model.HastaMes.ToString("D2")) <= 0).ToList();
            if (model.Meses.Count != model.HastaMes || model.Meses.Select(r => r.MesCodigo).Distinct().Count() != model.HastaMes)
                model.Errores.Add("Falta la configuración mensual del almacén o existen meses duplicados. Revísela antes de reprocesar.");
        }
        else
        {
            model.Meses = rows;
            if (rows.Count != 1) { model.Errores.Add("No se encontró una configuración única para el almacén y mes seleccionados."); return model; }
            var row = rows[0];
            if (operation == "apertura" && row.Estado == "A") model.Errores.Add("El mes ya está abierto. Actualice el listado.");
            if (operation == "cierre" && row.Estado != "A") model.Errores.Add("Sólo se puede cerrar un mes abierto. Actualice el listado.");
            if (operation != "apertura" && !row.AdmiteReproceso) model.Errores.Add("El mes contable de cierre anual (13) no está disponible para este proceso.");
        }
        model.Revision = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{scopeId}|{warehouseId}|{operation}|{model.HastaMes}|" + string.Join(";", model.Meses.Select(r => r.Revision)))));
        return model;
    }

    public async Task<(bool success, string message, int id)> EjecutarProcesoInventarioAsync(int empresaId, int scopeId, int userId, string operacion, InventarioProcesoSolicitud solicitud)
    {
        if (!Valid(solicitud) || !ValidProcessInput(empresaId, scopeId, userId, operacion, solicitud.AlmacenId, solicitud.MesContableId)) return Failure("Revise la operación y los datos seleccionados.");
        if (solicitud.ContextoId != scopeId || string.IsNullOrEmpty(solicitud.Revision)) return Failure("Prepare nuevamente la operación en el contexto de trabajo actual antes de confirmar.");
        var preview = await GetPreparacionProcesoAsync(empresaId, scopeId, userId, operacion, solicitud.AlmacenId, solicitud.MesContableId);
        if (preview.Errores.Count > 0) return Failure(string.Join(" ", preview.Errores));
        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);
        var plan = await PrepareProcessAsync(conn, tx, empresaId, scopeId, userId, operacion, solicitud.AlmacenId, solicitud.MesContableId);
        if (plan.Errores.Count > 0) return Failure(string.Join(" ", plan.Errores));
        if (solicitud.Revision != plan.Revision || (operacion == "anual" && solicitud.HastaMes != plan.HastaMes))
            return Failure("El contexto de la operación cambió. Actualice la preparación y confirme nuevamente.");
        int? stateId = null;
        if (operacion is "apertura" or "cierre")
        {
            stateId = await conn.QuerySingleOrDefaultAsync<int?>("SELECT estado_ape_cie_id FROM estado_ape_cie WHERE estado_ape_cie=@Code AND estado='A'",
                new { Code = operacion == "apertura" ? "A" : "C" }, tx);
            if (!stateId.HasValue) return Failure("No se encontró el estado de apertura/cierre habilitado.");
        }
        foreach (var month in plan.Meses)
        {
            if (operacion != "apertura")
                await conn.ExecuteAsync("sp_mov_alm_cierre_reprocesar", new { periodoEmpresaSedeId = scopeId, almacenId = plan.AlmacenId, mesContableId = month.MesContableId }, tx, commandTimeout: 180, commandType: CommandType.StoredProcedure);
            // Annual mode matches the legacy annual operation: no additional reservation recalculation.
            if (operacion == "reproceso" && plan.EsVenta)
                await conn.ExecuteAsync("sp_mov_alm_saldo_reprocesarCantidadReserva", new { periodoEmpresaSedeId = scopeId, mesContableId = month.MesContableId }, tx, commandTimeout: 180, commandType: CommandType.StoredProcedure);
            if (stateId.HasValue)
                await conn.ExecuteAsync("sp_mov_alm_cierre_actualizar", new { movimientoAlmacenCierreId = month.Id, estadoAperturaCierreId = stateId.Value,
                    fechaCierre = operacion == "cierre" ? DateTime.Today : (DateTime?)null, tieneMovimientoPostCierre = "N", usuarioAccionId = userId }, tx, commandType: CommandType.StoredProcedure);
        }
        tx.Commit();
        return (true, operacion switch { "apertura" => "Mes abierto correctamente.", "cierre" => "Mes cerrado correctamente.", "anual" => "Reproceso de enero al mes actual completado.", _ => "Mes reprocesado correctamente." }, plan.Meses[0].Id);
    }
}
