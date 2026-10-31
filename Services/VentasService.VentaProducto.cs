using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class VentasService
{
    public async Task<VentaProductoPageModel> GetVentasPorProductoAsync(int empresaId,int contextoId,VentaProductoFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null,bool exportar=false)
    {
        var model=new VentaProductoPageModel{Filtro=filtro??new()};filtro=model.Filtro;filtro.Modo=Text(filtro.Modo).ToLowerInvariant();
        var columns=VentaProductoPageModel.Columnas.ToDictionary(c=>c.Campo,c=>c.Campo);model.Ordenar(sort,dir,columns.Keys,"codigo");model.Paginar(0,page,top);
        if(empresaId<=0||contextoId<=0||!Valid(filtro)||filtro.Modo is not ("mes" or "rango")){model.Errores.Add("Seleccione un contexto y filtros de ventas válidos.");return model;}
        if(filtro.Modo=="mes"&&(filtro.Desde.HasValue||filtro.Hasta.HasValue)||filtro.Modo=="rango"&&(filtro.Mes.HasValue||!filtro.Desde.HasValue||!filtro.Hasta.HasValue||filtro.Desde?.Date>filtro.Hasta?.Date||filtro.Desde?.Year is <1900 or >9998||filtro.Hasta?.Year is <1900 or >9998))
        {model.Errores.Add("Seleccione un mes o un rango de fechas válido, sin combinar ambos filtros.");return model;}
        using var conn=factory.CreateConnection();var scope=await GetContextoReporteAsync(conn,empresaId,contextoId);
        if(scope is null){model.Errores.Add("El período y sede no están habilitados para la empresa.");return model;}model.Contexto=scope;
        model.Meses=(await conn.QueryAsync<LookupItemModel>("SELECT TRY_CONVERT(int,mes) Id,RTRIM(mes) Codigo,nombre Nombre FROM mes WHERE estado='A' AND mes BETWEEN '01' AND '12' ORDER BY mes")).AsList();
        if(filtro.Modo=="mes"){if(!filtro.Consultar&&!exportar)filtro.Mes??=scope.Hoy.Month;if(!model.Meses.Any(m=>m.Id==filtro.Mes))model.Errores.Add("Seleccione un mes calendario activo.");}
        if(model.Errores.Count>0||!filtro.Consultar&&!exportar)return model;model.Consultada=true;
        var args=new DynamicParameters(new{EmpresaId=empresaId,ScopeId=contextoId,filtro.IncluirInterno,filtro.ExcluirObsequio,Mes=filtro.Modo=="mes"?filtro.Mes:null,
            Desde=filtro.Modo=="rango"?filtro.Desde?.Date:null,Hasta=filtro.Modo=="rango"?filtro.Hasta?.Date.AddDays(1):null,Search=Search(filtro.Search)});
        const string where=" WHERE (@Search IS NULL OR Codigo LIKE @Search OR Producto LIKE @Search OR Grupo LIKE @Search OR Clase LIKE @Search OR Familia LIKE @Search)";
        var sums=await conn.QuerySingleAsync<VentaProductoTotales>(VentaProductoSql+" SELECT COUNT(*) Filas,COALESCE(SUM(Bruto),0) Bruto,COALESCE(SUM(Igv),0) Igv,COALESCE(SUM(Importe),0) Importe FROM datos"+where,args);
        model.Paginar(sums.Filas,page,top);model.Bruto=sums.Bruto;model.Igv=sums.Igv;model.Importe=sums.Importe;
        if(exportar&&model.Total>VentaMaxExportRows){model.Errores.Add($"La exportación supera {VentaMaxExportRows:N0} registros. Aplique filtros para exportar el resultado completo.");return model;}
        args.Add("Offset",exportar?0:(model.Page-1)*model.PageSize);args.Add("Top",exportar?Math.Max(1,model.Total):model.PageSize);
        model.Items=(await conn.QueryAsync<VentaProductoFila>(VentaProductoSql+" SELECT * FROM datos"+where+$" ORDER BY {columns[model.Sort]} {model.Dir},Id OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY",args)).AsList();model.VisibleCount=model.Items.Count;return model;
    }
    private sealed class VentaProductoTotales{public int Filas{get;set;}public decimal Bruto{get;set;}public decimal Igv{get;set;}public decimal Importe{get;set;}}

    private const string VentaProductoSql=@"WITH cabeceras AS (
        SELECT c.* FROM comprobante c JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id=c.periodo_emp_sede_id JOIN empresa_sede es ON es.empresa_sede_id=pes.empresa_sede_id
        WHERE es.empresa_id=@EmpresaId AND (@ExcluirObsequio=0 OR (c.periodo_emp_sede_id=@ScopeId AND c.estado='A' AND c.tipo_c_pago_id<>3 AND c.estado_compro_id=3 AND c.emitido='S'))
    ), cambio AS (
        SELECT tcd.fecha,tcd.moneda_id,tcd.monto_c FROM tipo_camb_dia tcd JOIN tipo_cambio tc ON tc.tipo_cambio_id=tcd.tipo_cambio_id WHERE tc.tipo_cambio='02'
    ), lineas AS (
        SELECT c.comprobante_id ComprobanteId,c.fecha Fecha,c.moneda_id MonedaId,cp.producto_id ProductoId,cp.cantidad Cantidad,cp.monto_bruto Bruto,cp.monto_igv Igv,cp.monto_total Importe,
            b.es_interno Interno
        FROM comprob_prod cp JOIN cabeceras c ON c.comprobante_id=cp.comprobante_id JOIN tipo_doc_com b ON b.tipo_doc_com_id=c.tipo_doc_com_id
        LEFT JOIN tipo_comprob tc ON tc.tipo_comprob_id=c.tipo_comprob_id JOIN estado_compro ec ON ec.estado_compro_id=c.estado_compro_id
        WHERE c.periodo_emp_sede_id=@ScopeId AND c.estado='A' AND cp.estado='A' AND ec.estado_compro='3'
            AND ((b.es_interno='N' AND tc.tipo_comprob IN ('1','4')) OR (@IncluirInterno=1 AND b.es_interno='S'))
            AND (@Mes IS NULL OR MONTH(c.fecha)=@Mes) AND (@Desde IS NULL OR c.fecha>=@Desde) AND (@Hasta IS NULL OR c.fecha<@Hasta)
    ), notas AS (
        SELECT c.comprobante_id ComprobanteId,c.fecha FechaComprobante,nc.fecha Fecha,c.moneda_id MonedaId,cp.producto_id ProductoId,
            CASE WHEN nc.tipo_nota_c_id=5 THEN 0 ELSE ncp.cantidad END Cantidad,ncp.monto_bruto Bruto,ncp.monto_igv Igv,ncp.monto_total Importe
        FROM nota_c_prod ncp JOIN nota_credito nc ON nc.nota_credito_id=ncp.nota_credito_id JOIN comprob_prod cp ON cp.comprob_prod_id=ncp.comprob_prod_id
        JOIN cabeceras c ON c.comprobante_id=nc.comprobante_id JOIN estado_compro ec ON ec.estado_compro_id=nc.estado_compro_id
        WHERE nc.periodo_emp_sede_id=@ScopeId AND nc.estado='A' AND ncp.estado='A' AND ec.estado_compro='3' AND c.tipo_comprob_id IN (1,4)
            AND (@Mes IS NULL OR MONTH(nc.fecha)=@Mes) AND (@Desde IS NULL OR nc.fecha>=@Desde)
    ), movimientos AS (
        SELECT l.ProductoId,SUM(l.Cantidad) Cantidad,SUM(CASE WHEN m.es_local='S' THEN l.Bruto ELSE ROUND(l.Bruto*ISNULL(tc.monto_c,0),2) END) Bruto,
            SUM(CASE WHEN m.es_local='S' THEN l.Igv ELSE ROUND(l.Igv*ISNULL(tc.monto_c,0),2) END) Igv,SUM(CASE WHEN m.es_local='S' THEN l.Importe ELSE ROUND(l.Importe*ISNULL(tc.monto_c,0),2) END) Importe
        FROM lineas l JOIN moneda m ON m.moneda_id=l.MonedaId LEFT JOIN cambio tc ON tc.fecha=l.Fecha AND tc.moneda_id=l.MonedaId GROUP BY l.ProductoId
        UNION ALL
        SELECT n.ProductoId,SUM(n.Cantidad)*-1,SUM(CASE WHEN m.es_local='S' THEN n.Bruto ELSE ROUND(n.Bruto*ISNULL(tc.monto_c,0),2) END),
            SUM(CASE WHEN m.es_local='S' THEN n.Igv ELSE ROUND(n.Igv*ISNULL(tc.monto_c,0),2) END),SUM(CASE WHEN m.es_local='S' THEN n.Importe ELSE ROUND(n.Importe*ISNULL(tc.monto_c,0),2) END)*-1
        FROM notas n JOIN moneda m ON m.moneda_id=n.MonedaId LEFT JOIN cambio tc ON tc.fecha=n.Fecha AND tc.moneda_id=n.MonedaId
        WHERE (@Hasta IS NULL AND n.FechaComprobante<=n.Fecha) OR (@Hasta IS NOT NULL AND n.FechaComprobante<@Hasta) GROUP BY n.ProductoId
    ), importes AS (
        SELECT ProductoId,SUM(Cantidad) Cantidad,SUM(Bruto) Bruto,SUM(Igv) Igv,SUM(Importe) Importe FROM movimientos GROUP BY ProductoId
    ), tickets_normales AS (
        SELECT l.ProductoId,l.ComprobanteId,SUM(l.Cantidad) Cantidad FROM lineas l WHERE l.Interno='N' GROUP BY l.ProductoId,l.ComprobanteId
        UNION ALL
        SELECT n.ProductoId,n.ComprobanteId,SUM(n.Cantidad)*-1 FROM notas n WHERE @Hasta IS NULL OR n.Fecha<@Hasta GROUP BY n.ProductoId,n.ComprobanteId
    ), cantidades_ticket AS (
        SELECT ProductoId,ComprobanteId,SUM(Cantidad) Cantidad FROM tickets_normales GROUP BY ProductoId,ComprobanteId
        UNION ALL
        SELECT l.ProductoId,l.ComprobanteId,SUM(l.Cantidad) FROM lineas l WHERE l.Interno='S' GROUP BY l.ProductoId,l.ComprobanteId
    ), estadisticas AS (
        SELECT ProductoId,MIN(Cantidad) Minimo,MAX(Cantidad) Maximo,COUNT(*) Tickets FROM cantidades_ticket GROUP BY ProductoId
    ), datos AS (
        SELECT prd.producto_id Id,RTRIM(prd.producto) Codigo,prd.descripcion Producto,gp.nombre Grupo,cl.nombre Clase,fp.nombre Familia,
            ISNULL(t.Minimo,0) Minimo,ISNULL(t.Maximo,0) Maximo,ROUND(i.Cantidad/ISNULL(t.Tickets,1),2) Promedio,ISNULL(t.Tickets,0) Tickets,i.Cantidad,i.Bruto,i.Igv,i.Importe
        FROM importes i LEFT JOIN estadisticas t ON t.ProductoId=i.ProductoId JOIN producto prd ON prd.producto_id=i.ProductoId
        "+VentaProductoEmpresa+@"
        WHERE tp.empresa_id=@EmpresaId AND (i.Cantidad<>0 OR i.Importe<>0)
    ) ";
}
