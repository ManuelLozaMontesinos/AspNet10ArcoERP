using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services;

public partial class InventarioService
{
    public async Task<LlegadaCompraPageModel> GetLlegadasCompraAsync(int empresaId, int scopeId, int userId, LlegadaCompraFiltro filtro, int page = 1, int top = 25, string? sort = null, string? dir = null)
    {
        var model = new LlegadaCompraPageModel { Filtro = filtro }; model.Paginar(0, page, top);
        if (empresaId <= 0 || scopeId <= 0 || userId <= 0) { model.Errores.Add("Seleccione un contexto de trabajo válido."); return model; }
        using var conn = factory.CreateConnection();
        var scope = await GetInventarioScopeAsync(conn, empresaId, scopeId, userId);
        if (scope is null) { model.Errores.Add("El período y la sede seleccionados no están habilitados."); return model; }
        model.Contexto = scope;
        model.SinProveedor = await conn.ExecuteScalarAsync<bool>(@"SELECT CAST(CASE WHEN EXISTS (
            SELECT 1 FROM usuario u JOIN rol_usuario ru ON ru.usuario_id=u.usuario_id JOIN rol r ON r.rol_id=ru.rol_id
            WHERE u.usuario_id=@UserId AND u.estado='A' AND r.estado='A' AND r.rol='00004'
              AND (u.empresa_id IS NULL OR u.empresa_id=@EmpresaId) AND (r.empresa_id IS NULL OR r.empresa_id=@EmpresaId)
        ) THEN 1 ELSE 0 END AS bit)", new { UserId = userId, EmpresaId = empresaId });
        var columns = new Dictionary<string, string> { ["codigo"]="prd.producto", ["producto"]="prd.descripcion", ["unidad"]="um.abrev", ["fecha"]="oce.fecha", ["cantidad"]="oce.cantidad" };
        if (!model.SinProveedor) columns["proveedor"] = "prv.razon_social";
        model.Ordenar(sort, dir, columns.Keys, "fecha", "desc");
        if (!Valid(filtro) || filtro.Fecha?.Year is < 1900 or > 9998) { model.Errores.Add("Revise la fecha y la longitud de los filtros de búsqueda."); return model; }
        if (model.SinProveedor) filtro.Proveedor = null;
        // The legacy arrival query spans all accounting periods of the selected company/site.
        // It filters pending purchase lines, not delivery quantities against each other.
        string from = @" FROM orden_c_ent oce JOIN orden_c_prod ocp ON ocp.orden_c_prod_id=oce.orden_c_prod_id
            JOIN orden_co oc ON oc.orden_co_id=ocp.orden_co_id
            JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id=oc.periodo_emp_sede_id
            JOIN empresa_sede es ON es.empresa_sede_id=pes.empresa_sede_id
            JOIN estado_aproba ea ON ea.estado_aproba_id=oc.estado_aproba_id
            JOIN estado_orden_co eoc ON eoc.estado_orden_co_id=oc.estado_orden_co_id
            JOIN proveedor prv ON prv.proveedor_id=oc.proveedor_id AND prv.empresa_id=@EmpresaId
            JOIN producto prd ON prd.producto_id=ocp.producto_id JOIN unidad_medida um ON um.unidad_medida_id=prd.unidad_medida_id " + ProductoEmpresaJoin + @"
            WHERE es.empresa_id=@EmpresaId AND pes.empresa_sede_id=@SiteId AND tp.empresa_id=@EmpresaId
              AND oce.estado='A' AND ea.estado_aproba='A' AND eoc.estado_orden_co IN ('2','3') AND ocp.cantidad>ocp.cant_ing
              AND (@Search IS NULL OR prd.descripcion LIKE @Search OR prd.producto LIKE @Search)
              AND (@Provider IS NULL OR prv.razon_social LIKE @Provider)
              AND (@Desde IS NULL OR (oce.fecha>=@Desde AND oce.fecha<@Hasta))";
        var args = new DynamicParameters(new { EmpresaId = empresaId, SiteId = scope.EmpresaSedeId, Search = Search(filtro.Search), Provider = Search(filtro.Proveedor), Desde = filtro.Fecha?.Date, Hasta = filtro.Fecha?.Date.AddDays(1) });
        model.Paginar(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*)" + from, args), page, top);
        args.Add("Offset", (model.Page - 1) * model.PageSize); args.Add("Top", model.PageSize);
        string provider = model.SinProveedor ? "''" : "ISNULL(prv.razon_social,'')";
        model.Items = (await conn.QueryAsync<LlegadaCompraRowModel>(@"SELECT oce.orden_c_ent_id Id," + provider + @" Proveedor,
            RTRIM(prd.producto) Codigo,prd.descripcion Producto,um.abrev Unidad,oce.fecha Fecha,oce.cantidad Cantidad,
            RTRIM(oc.orden_co) OrdenCompra,ocp.cantidad CantidadOrdenada,ocp.cant_ing CantidadIngresada" + from +
            $" ORDER BY {columns[model.Sort]} {model.Dir},oce.orden_c_ent_id ASC OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY", args)).AsList();
        model.VisibleCount = model.Items.Count; return model;
    }
}
