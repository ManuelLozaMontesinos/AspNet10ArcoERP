using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class ProduccionService
{
    public async Task<ProduccionReportePageModel> GetReporteProcesosAsync(int empresaId,int contextoId,ProduccionReporteFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null,bool exportar=false)
    {
        var model=new ProduccionReportePageModel{Filtro=filtro??new(),ContextoId=contextoId};filtro=model.Filtro;
        filtro.Vista=Text(filtro.Vista).ToLowerInvariant();filtro.Modo=Text(filtro.Modo).ToLowerInvariant();
        var columns=model.Columnas.ToDictionary(c=>c.Campo,c=>c.Campo);model.Ordenar(sort,dir,columns.Keys,"fecha","desc");model.Paginar(0,page,top);
        if(empresaId<=0||contextoId<=0||!Valid(filtro)||filtro.Vista is not ("estadistico" or "detallado")||filtro.Modo is not ("mes" or "rango"))
        {model.Errores.Add("Revise el contexto, el tipo de reporte, el modo de consulta y la longitud de búsqueda (150 caracteres).");return model;}
        using var conn=factory.CreateConnection();model.Contexto=await ConsultaSedeSql.CargarAsync(conn,empresaId,contextoId,null);
        if(model.Contexto.Errores.Count>0){model.Errores.AddRange(model.Contexto.Errores);return model;}
        model.Meses=(await conn.QueryAsync<LookupItemModel>("SELECT TRY_CONVERT(int,mes) Id,RTRIM(mes) Codigo,nombre Nombre FROM mes WHERE estado='A' AND mes BETWEEN '01' AND '12' ORDER BY mes")).AsList();
        if(filtro.Modo=="mes")filtro.Mes??=await conn.ExecuteScalarAsync<int>("SELECT MONTH(GETDATE())");
        if(!filtro.Consultar&&!exportar)return model;
        if(filtro.Modo=="mes")
        {
            if(!model.Meses.Any(m=>m.Id==filtro.Mes)||filtro.Desde.HasValue||filtro.Hasta.HasValue)model.Errores.Add("Seleccione un mes calendario activo, sin fechas adicionales.");
        }
        else if(filtro.Mes.HasValue||!filtro.Desde.HasValue||!filtro.Hasta.HasValue||filtro.Desde.Value.Year is <1900 or >9998||filtro.Hasta.Value.Year is <1900 or >9998||filtro.Desde.Value.Date>filtro.Hasta.Value.Date)
            model.Errores.Add("Seleccione un rango de fechas válido, sin mes adicional.");
        if(model.Errores.Count>0)return model;model.Consultada=true;
        bool detail=filtro.Vista=="detallado";
        var args=new DynamicParameters(new{EmpresaId=empresaId,ScopeId=contextoId,Mes=filtro.Modo=="mes"?filtro.Mes:null,
            Desde=filtro.Modo=="rango"?filtro.Desde?.Date:null,Hasta=filtro.Modo=="rango"?filtro.Hasta?.Date.AddDays(1):null,Search=Search(filtro.Search)});
        string query=ReporteOperacionesSql+(detail?ReporteDetalladoSql:ReporteEstadisticoSql);
        string fields=detail?"Pedido,Operador,Proceso,Maquina,Observacion,Estado":"Operador,Maquina";
        string where=" WHERE (@Search IS NULL OR "+string.Join(" OR ",fields.Split(',').Select(c=>c+" LIKE @Search"))+")";
        int total=await conn.ExecuteScalarAsync<int>(query+" SELECT COUNT(*) FROM datos"+where,args);model.Paginar(total,page,top);
        if(exportar&&total>50000){model.Errores.Add("La exportación supera 50.000 registros. Aplique filtros para exportar el resultado completo.");return model;}
        args.Add("Offset",exportar?0:(model.Page-1)*model.PageSize);args.Add("Top",exportar?Math.Max(1,total):model.PageSize);
        string tie=detail?"Id":"Grupo,Fecha,Maquina,Operador";
        var order=new[]{columns[model.Sort]+" "+model.Dir}.Concat(tie.Split(',').Where(c=>!c.Equals(columns[model.Sort],StringComparison.OrdinalIgnoreCase)));
        model.Items=(await conn.QueryAsync<ProduccionReporteFila>(query+" SELECT * FROM datos"+where+" ORDER BY "+string.Join(",",order)+" OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY",args)).AsList();
        model.VisibleCount=model.Items.Count;return model;
    }

    // These product, group, family and process IDs are the explicit classifications of the two legacy reports.
    // Keep their different summary/detail rules, including the duplicate membership in the high-range UNION ALL.
    private const string ReporteOperacionesSql=@"WITH operaciones AS (
        SELECT opsp.orden_p_s_p_id Id,opsp.orden_pro_id OrdenId,opsp.sub_proce_p_id SubId,c.cotiza Pedido,c.fecha FechaPedido,
            CONVERT(date,opsp.fecha_ini) Fecha,m.nombre Maquina,CONCAT(LEFT(pvd.nombres,CHARINDEX(' ',pvd.nombres+' ')-1),' ',pvd.apellido_pat) Operador,
            spp.nombre Proceso,op.fecha_ent FechaEntrega,opsp.fecha_ini FechaInicio,opsp.fecha_fin FechaFin,opsp.interrump Interrupciones,opsp.observa Observacion,z.nombre Estado
        FROM orden_p_s_p opsp JOIN orden_pro op ON op.orden_pro_id=opsp.orden_pro_id JOIN cotiza c ON c.cotiza_id=op.cotiza_id
        JOIN periodo_emp_sede pc ON pc.periodo_emp_sede_id=c.periodo_emp_sede_id JOIN empresa_sede sc ON sc.empresa_sede_id=pc.empresa_sede_id
        JOIN personal pe ON pe.personal_id=opsp.personal_id JOIN empresa_sede sp ON sp.empresa_sede_id=pe.empresa_sede_id
        JOIN proveedor pvd ON pvd.proveedor_id=pe.proveedor_id JOIN sub_proce_p spp ON spp.sub_proce_p_id=opsp.sub_proce_p_id
        JOIN estado_o_p_s_p z ON z.estado_o_p_s_p_id=opsp.estado_o_p_s_p_id LEFT JOIN maquina m ON m.maquina_id=opsp.maquina_real_id
        WHERE op.periodo_emp_sede_id=@ScopeId AND sc.empresa_id=@EmpresaId AND sp.empresa_id=@EmpresaId AND pvd.empresa_id=@EmpresaId
            AND opsp.sub_proce_p_id IN (1,9,10) AND opsp.estado_o_p_s_p_id=3 AND (@Mes IS NULL OR MONTH(opsp.fecha_ini)=@Mes)
            AND (@Desde IS NULL OR opsp.fecha_ini>=@Desde)
            AND ((@Hasta IS NULL AND opsp.fecha_ini<=opsp.fecha_fin) OR (@Hasta IS NOT NULL AND opsp.fecha_ini<@Hasta))
    ),cantidades AS (
        SELECT a.orden_pro_id OrdenId,
            SUM(CASE WHEN a.producto_id IN (3083,3947) THEN a.cantidad END) PiezasResumen,
            SUM(CASE WHEN a.producto_id=3083 THEN a.cantidad END) PiezasMelamina,
            SUM(CASE WHEN a.producto_id=3946 THEN a.cantidad END) PiezasAglomerado,
            SUM(CASE WHEN a.producto_id=3947 THEN a.cantidad END) PiezasAltaGama,
            SUM(CASE WHEN gp.grupo_producto_id=5 THEN a.cantidad END) PlanchasResumen,
            SUM(CASE WHEN gp.grupo_producto_id=6 THEN a.cantidad END) PlanchasAglomerado,
            SUM(CASE WHEN gp.grupo_producto_id=5 AND p.familia_producto_id NOT IN (126,137) AND cp.clase_producto_id NOT IN (70,45) THEN a.cantidad END) PlanchasMelamina,
            SUM(CASE WHEN cp.clase_producto_id IN (45,70) OR p.familia_producto_id IN (126,137) THEN
                a.cantidad*(CASE WHEN cp.clase_producto_id IN (45,70) THEN 1 ELSE 0 END+CASE WHEN p.familia_producto_id IN (126,137) THEN 1 ELSE 0 END) END) PlanchasAltaGama
        FROM (SELECT DISTINCT OrdenId FROM operaciones) o JOIN orden_p_prod a ON a.orden_pro_id=o.OrdenId
        JOIN producto p ON p.producto_id=a.producto_id JOIN familia_producto fp ON fp.familia_producto_id=p.familia_producto_id
        JOIN clase_producto cp ON cp.clase_producto_id=fp.clase_producto_id JOIN grupo_producto gp ON gp.grupo_producto_id=cp.grupo_producto_id
        JOIN tipo_producto tp ON tp.tipo_producto_id=gp.tipo_producto_id
        WHERE a.estado='A' AND tp.empresa_id=@EmpresaId
        GROUP BY a.orden_pro_id
    ) ";

    private const string ReporteEstadisticoSql=@",agrupados AS (
        SELECT CASE WHEN op.SubId=9 THEN 2 ELSE 1 END Grupo,op.Fecha,op.Maquina,op.Operador,
            SUM(CASE WHEN op.SubId=9 THEN q.PiezasAglomerado ELSE q.PiezasResumen END) Piezas,
            SUM(CASE WHEN op.SubId=9 THEN q.PlanchasAglomerado ELSE q.PlanchasResumen END) Planchas
        FROM operaciones op LEFT JOIN cantidades q ON q.OrdenId=op.OrdenId
        GROUP BY CASE WHEN op.SubId=9 THEN 2 ELSE 1 END,op.Fecha,op.Maquina,op.Operador
    ),datos AS (SELECT *,CAST(Piezas/NULLIF(Planchas,0) AS decimal(20,2)) Promedio FROM agrupados) ";

    private const string ReporteDetalladoSql=@",datos AS (
        SELECT op.Id,op.Pedido,op.FechaPedido Fecha,op.Operador,op.Proceso,op.Maquina,op.FechaEntrega,op.FechaInicio,op.FechaFin,op.Interrupciones,op.Observacion,op.Estado,
            CASE op.SubId WHEN 1 THEN q.PiezasMelamina WHEN 9 THEN q.PiezasAglomerado ELSE q.PiezasAltaGama END Piezas,
            CASE op.SubId WHEN 1 THEN q.PlanchasMelamina WHEN 9 THEN q.PlanchasAglomerado ELSE q.PlanchasAltaGama END Planchas
        FROM operaciones op LEFT JOIN cantidades q ON q.OrdenId=op.OrdenId
    ) ";
}
