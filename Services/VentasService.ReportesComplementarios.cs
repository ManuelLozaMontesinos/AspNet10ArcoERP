using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class VentasService
{
    private static Task<VentaReporteContexto?> GetContextoReporteAsync(IDbConnection conn,int empresaId,int contextoId,IDbTransaction? tx=null)
        =>conn.QuerySingleOrDefaultAsync<VentaReporteContexto>(@"SELECT pes.periodo_emp_sede_id PeriodoEmpresaSedeId,s.empresa_sede_id EmpresaSedeId,p.periodo Anio,
            e.nombre Empresa,s.nombre Sede,CAST(GETDATE() AS date) Hoy FROM periodo_emp_sede pes
            JOIN empresa_sede s ON s.empresa_sede_id=pes.empresa_sede_id JOIN empresa e ON e.empresa_id=s.empresa_id JOIN periodo p ON p.periodo_id=pes.periodo_id
            WHERE pes.periodo_emp_sede_id=@Id AND s.empresa_id=@EmpresaId AND pes.estado='A' AND s.estado='A' AND e.estado='A' AND p.estado='A'",new{Id=contextoId,EmpresaId=empresaId},tx);

    private async Task<VentaReportePageModel> GetReporteComplementarioAsync(int empresaId,int contextoId,string tipo,VentaReporteFiltro filtro,int page,int top,string? sort,string? dir,bool exportar)
    {
        bool caja=tipo=="caja";
        var model=new VentaReportePageModel{Tipo=tipo,Titulo=caja?"Liquidación de caja":"Canjes",Filtro=filtro??new()};filtro=model.Filtro;
        filtro.Modo=Text(filtro.Modo).ToLowerInvariant();if(filtro.Modo.Length==0)filtro.Modo=caja?"dia":"mes";
        var columns=model.Columnas.ToDictionary(c=>c.Campo.ToLowerInvariant(),c=>c.Campo);
        model.Ordenar(sort,dir,columns.Keys,"fecha","desc");model.Paginar(0,page,top);
        if(empresaId<=0||contextoId<=0){model.Errores.Add("Seleccione un contexto de trabajo válido.");return model;}
        if(filtro.Search?.Length>150){model.Errores.Add("La búsqueda admite hasta 150 caracteres.");return model;}
        if(filtro.VendedorId.HasValue||filtro.Agrupar!="producto"||(!caja&&(filtro.CajaId.HasValue||filtro.Fecha.HasValue)))model.Errores.Add("El filtro no corresponde a este reporte.");
        if(caja&&(filtro.Modo!="dia"||filtro.Mes.HasValue||filtro.Desde.HasValue||filtro.Hasta.HasValue)||!caja&&filtro.Modo is not ("mes" or "rango"))model.Errores.Add("Seleccione un modo de consulta válido.");
        if(model.Errores.Count>0)return model;
        using var conn=factory.CreateConnection();var scope=await GetContextoReporteAsync(conn,empresaId,contextoId);
        if(scope is null){model.Errores.Add("El período y sede no están habilitados para la empresa.");return model;}model.Contexto=scope;
        if(caja)
        {
            model.Cajas=(await conn.QueryAsync<LookupItemModel>("SELECT caja_id Id,nombre Nombre FROM caja WHERE empresa_sede_id=@SiteId AND estado='A' ORDER BY nombre,caja_id",new{SiteId=scope.EmpresaSedeId})).AsList();
            if(!filtro.Consultar&&!exportar)filtro.Fecha??=scope.Hoy;
        }
        else
        {
            // This report uses calendar months (mes), not the accounting month ID.
            model.Meses=(await conn.QueryAsync<LookupItemModel>("SELECT TRY_CONVERT(int,mes) Id,RTRIM(mes) Codigo,nombre Nombre FROM mes WHERE estado='A' AND mes BETWEEN '01' AND '12' ORDER BY mes")).AsList();
            if(filtro.Modo=="mes")filtro.Mes??=scope.Hoy.Month;
        }
        if(!filtro.Consultar&&!exportar)return model;
        if(caja)
        {
            if(!filtro.CajaId.HasValue||!model.Cajas.Any(c=>c.Id==filtro.CajaId))model.Errores.Add("Seleccione una caja activa de la sede de trabajo.");
            if(!filtro.Fecha.HasValue||filtro.Fecha.Value.Year is <1900 or >9998)model.Errores.Add("Seleccione una fecha válida.");
        }
        else if(filtro.Modo=="mes")
        {
            if(!model.Meses.Any(m=>m.Id==filtro.Mes))model.Errores.Add("Seleccione un mes calendario activo.");
            if(filtro.Desde.HasValue||filtro.Hasta.HasValue)model.Errores.Add("El modo mensual no admite un rango de fechas adicional.");
        }
        else
        {
            if(filtro.Mes.HasValue)model.Errores.Add("El modo por fechas no admite un mes adicional.");
            if(!filtro.Desde.HasValue||!filtro.Hasta.HasValue||filtro.Desde.Value.Year is <1900 or >9998||filtro.Hasta.Value.Year is <1900 or >9998||filtro.Desde.Value.Date>filtro.Hasta.Value.Date)model.Errores.Add("Seleccione un rango de fechas válido.");
        }
        if(model.Errores.Count>0)return model;model.Consultada=true;
        var args=new DynamicParameters(new{EmpresaId=empresaId,ScopeId=contextoId,SiteId=scope.EmpresaSedeId,filtro.CajaId,
            Mes=filtro.Modo=="mes"?filtro.Mes:null,Desde=caja?filtro.Fecha?.Date:filtro.Modo=="rango"?filtro.Desde?.Date:null,
            Hasta=caja?filtro.Fecha?.Date.AddDays(1):filtro.Modo=="rango"?filtro.Hasta?.Date.AddDays(1):null,Search=Search(filtro.Search)});
        string query=caja?LiquidacionCajaSql:CanjesSql;
        string fields=caja?"Documento,TipoDocumento,Cliente,Caja,Moneda,Estado":"Documento,TipoDocumento,Codigo,Producto,MesNombre,Moneda,Estado";
        string where=" WHERE (@Search IS NULL OR "+string.Join(" OR ",fields.Split(',').Select(c=>c+" LIKE @Search"))+")";
        int total=await conn.ExecuteScalarAsync<int>(query+" SELECT COUNT(*) FROM datos"+where,args);model.Paginar(total,page,top);
        if(exportar&&total>VentaMaxExportRows){model.Errores.Add($"La exportación supera {VentaMaxExportRows:N0} registros. Aplique filtros para exportar el resultado completo.");return model;}
        args.Add("Offset",exportar?0:(model.Page-1)*model.PageSize);args.Add("Top",exportar?Math.Max(1,total):model.PageSize);
        model.Items=(await conn.QueryAsync<VentaReporteRowModel>(query+" SELECT * FROM datos"+where+$" ORDER BY {columns[model.Sort]} {model.Dir},Id OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY",args)).AsList();
        model.VisibleCount=model.Items.Count;return model;
    }

    private const string CanjesSql=@"WITH datos AS (
        SELECT cp.comprob_prod_id Id,tdc.abrev TipoDocumento,CONCAT(c.serie,'-',c.numero) Documento,c.fecha Fecha,RTRIM(prd.producto) Codigo,prd.descripcion Producto,
            me.nombre MesNombre,m.moneda Moneda,c.monto_tc TipoCambio,cp.cantidad Cantidad,
            CASE WHEN c.estado='A' THEN CASE WHEN m.es_local='S' THEN cp.precio_unitario ELSE cp.precio_unitario*c.monto_tc END ELSE 0 END PrecioUnitario,
            CASE WHEN c.estado='A' THEN CASE WHEN m.es_local='S' THEN cp.monto_total ELSE cp.monto_total*c.monto_tc END ELSE 0 END Total,
            CASE WHEN c.estado='A' THEN 'Activo' ELSE 'Anulado' END Estado
        FROM comprob_prod cp JOIN comprobante c ON c.comprobante_id=cp.comprobante_id JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id=c.tipo_doc_com_id
        JOIN tipo_comprob tc ON tc.tipo_comprob_id=c.tipo_comprob_id JOIN estado_compro ec ON ec.estado_compro_id=c.estado_compro_id
        JOIN moneda m ON m.moneda_id=c.moneda_id JOIN producto prd ON prd.producto_id=cp.producto_id JOIN mes me ON TRY_CONVERT(int,me.mes)=MONTH(c.fecha)
        "+VentaProductoEmpresa+@"
        WHERE c.periodo_emp_sede_id=@ScopeId AND tp.empresa_id=@EmpresaId AND cp.reg_canje_id IS NULL
            AND ((tdc.es_interno='S' AND tc.tipo_comprob='1') OR (tdc.es_interno='N' AND tc.tipo_comprob='4'))
            AND ec.estado_compro='3' AND cp.estado='A' AND (@Mes IS NULL OR MONTH(c.fecha)=@Mes)
            AND (@Desde IS NULL OR c.fecha>=@Desde) AND (@Hasta IS NULL OR c.fecha<@Hasta)
    ) ";

    private const string LiquidacionCajaSql=@"WITH datos AS (
        SELECT c.comprobante_id Id,cj.nombre Caja,tdc.abrev TipoDocumento,CONCAT(c.serie,'-',c.numero) Documento,c.fecha Fecha,
            p.razon_social Cliente,m.codigo_iso Moneda,m.simbolo Simbolo,c.monto_tc TipoCambio,c.monto_total Total,
            CASE WHEN c.estado='A' THEN 'Activo' ELSE 'Anulado' END Estado
        FROM comprobante c JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id=c.tipo_doc_com_id JOIN proveedor p ON p.proveedor_id=c.proveedor_id
        JOIN moneda m ON m.moneda_id=c.moneda_id JOIN estado_compro ec ON ec.estado_compro_id=c.estado_compro_id JOIN caja cj ON cj.caja_id=c.caja_id
        WHERE c.periodo_emp_sede_id=@ScopeId AND c.caja_id=@CajaId AND cj.empresa_sede_id=@SiteId AND p.empresa_id=@EmpresaId
            AND c.fecha>=@Desde AND c.fecha<@Hasta AND ec.estado_compro='3'
    ) ";
}
