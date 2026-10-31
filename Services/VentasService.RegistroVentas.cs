using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class VentasService
{
    public async Task<RegistroVentasPageModel> GetRegistroVentasAsync(int empresaId,int contextoId,RegistroVentasFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null,bool exportar=false)
    {
        var model=new RegistroVentasPageModel{Filtro=filtro??new()};filtro=model.Filtro;
        filtro.Modo=Text(filtro.Modo).ToLowerInvariant();filtro.Vista=Text(filtro.Vista).ToLowerInvariant();filtro.Tipo=Text(filtro.Tipo).ToLowerInvariant();
        var columns=model.Columnas.ToDictionary(c=>c.Campo.ToLowerInvariant(),c=>c.Campo);model.Ordenar(sort,dir,columns.Keys,"fecha");model.Paginar(0,page,top);
        if(empresaId<=0||contextoId<=0||!Valid(filtro)||filtro.Modo is not ("mes" or "rango")||filtro.Vista is not ("consolidado" or "detallado")||filtro.Tipo is not ("general" or "oficial"))
        {model.Errores.Add("Seleccione un contexto y filtros de registro válidos.");return model;}
        if(filtro.Modo=="mes"&&(filtro.Desde.HasValue||filtro.Hasta.HasValue)||filtro.Modo=="rango"&&(filtro.Mes.HasValue||!filtro.Desde.HasValue||!filtro.Hasta.HasValue||filtro.Desde>filtro.Hasta||filtro.Desde?.Year is <1900 or >9998||filtro.Hasta?.Year is <1900 or >9998))
        {model.Errores.Add("Seleccione un mes o un rango de fechas válido, sin combinar ambos filtros.");return model;}
        using var conn=factory.CreateConnection();var scope=await GetContextoReporteAsync(conn,empresaId,contextoId);
        if(scope is null){model.Errores.Add("El período y sede no están habilitados para la empresa.");return model;}model.Contexto=scope;
        model.Meses=(await conn.QueryAsync<LookupItemModel>("SELECT TRY_CONVERT(int,mes) Id,RTRIM(mes) Codigo,nombre Nombre FROM mes WHERE estado='A' AND mes BETWEEN '01' AND '12' ORDER BY mes")).AsList();
        if(filtro.Modo=="mes"){if(!filtro.Consultar&&!exportar)filtro.Mes??=scope.Hoy.Month;if(!model.Meses.Any(m=>m.Id==filtro.Mes))model.Errores.Add("Seleccione un mes calendario activo.");}
        if(model.Errores.Count>0||!filtro.Consultar&&!exportar)return model;model.Consultada=true;
        var args=new DynamicParameters(new{EmpresaId=empresaId,ScopeId=contextoId,Oficial=filtro.Tipo=="oficial",Mes=filtro.Modo=="mes"?filtro.Mes:null,
            Desde=filtro.Modo=="rango"?filtro.Desde?.Date:null,Hasta=filtro.Modo=="rango"?filtro.Hasta?.Date.AddDays(1):null,Search=Search(filtro.Search)});
        string sql=filtro.Vista=="detallado"?RegistroVentaDetalleSql:RegistroVentaConsolidadoSql;
        string searchFields=filtro.Vista=="detallado"?"TipoDocumento,Documento,ClienteCodigo,Cliente,Identidad,Moneda,MedioPago,Codigo,Producto,Descripcion,Grupo,Clase,Familia,Estado":"TipoDocumento,Documento,ClienteCodigo,Cliente,Identidad,Moneda,CondicionPago,MedioPago,Estado";
        string where=" WHERE (@Search IS NULL OR "+string.Join(" OR ",searchFields.Split(',').Select(c=>c+" LIKE @Search"))+")";
        var totals=await conn.QuerySingleAsync<RegistroVentasTotales>(sql+" SELECT COUNT(*) Filas,COALESCE(SUM(CASE WHEN TipoCodigo='07' THEN -Bruto ELSE Bruto END),0) Bruto,COALESCE(SUM(CASE WHEN TipoCodigo='07' THEN -Neto ELSE Neto END),0) Neto,COALESCE(SUM(CASE WHEN TipoCodigo='07' THEN -Igv ELSE Igv END),0) Igv,COALESCE(SUM(CASE WHEN TipoCodigo='07' THEN -Total ELSE Total END),0) Total FROM datos"+where,args);
        model.Paginar(totals.Filas,page,top);model.Bruto=totals.Bruto;model.Neto=totals.Neto;model.Igv=totals.Igv;model.TotalLocal=totals.Total;
        if(exportar&&model.Total>VentaMaxExportRows){model.Errores.Add($"La exportación supera {VentaMaxExportRows:N0} registros. Aplique filtros para exportar el resultado completo.");return model;}
        args.Add("Offset",exportar?0:(model.Page-1)*model.PageSize);args.Add("Top",exportar?Math.Max(1,model.Total):model.PageSize);
        model.Items=(await conn.QueryAsync<RegistroVentasFila>(sql+" SELECT * FROM datos"+where+$" ORDER BY {columns[model.Sort]} {model.Dir},Origen,Id OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY",args)).AsList();model.VisibleCount=model.Items.Count;return model;
    }

    private sealed class RegistroVentasTotales{public int Filas{get;set;}public decimal Bruto{get;set;}public decimal Neto{get;set;}public decimal Igv{get;set;}public decimal Total{get;set;}}

    // Keep invoice and credit-note rows distinct even when their IDs coincide. Notes use
    // their own period/site and date, but inherit customer, currency and FX from the invoice.
    private const string RegistroVentaConsolidadoSql=@"WITH origen AS (
        SELECT c.comprobante_id Id,'C' Origen,c.periodo_emp_sede_id,c.tipo_doc_com_id,'' TipoReferencia,c.serie,c.numero,c.fecha,c.proveedor_id,c.moneda_id,c.monto_tc,
            c.monto_bruto,c.monto_neto,c.monto_igv,c.monto_total,mp.abrev MedioPago,c.tipo_c_pago_id,c.tipo_comprob_id,c.estado
        FROM comprobante c JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id=c.tipo_doc_com_id JOIN medio_pago mp ON mp.medio_pago_id=c.medio_pago_id
        JOIN tipo_comprob tc ON tc.tipo_comprob_id=c.tipo_comprob_id JOIN estado_compro ec ON ec.estado_compro_id=c.estado_compro_id
        WHERE c.periodo_emp_sede_id=@ScopeId AND mp.empresa_id=@EmpresaId AND c.emitido='S' AND ec.estado_compro IN ('3','9')
            AND ((@Oficial=0 AND tc.tipo_comprob='1') OR (@Oficial=1 AND tc.tipo_comprob<>'4' AND tdc.es_interno='N'))
            AND (@Mes IS NULL OR MONTH(c.fecha)=@Mes) AND (@Desde IS NULL OR c.fecha>=@Desde) AND (@Hasta IS NULL OR c.fecha<@Hasta)
        UNION ALL
        SELECT nc.nota_credito_id,'N',nc.periodo_emp_sede_id,(SELECT tipo_doc_com_id FROM tipo_doc_com WHERE codigo_sunat='07'),tdc.tipo_doc_com,
            nc.serie,nc.numero,nc.fecha,c.proveedor_id,c.moneda_id,c.monto_tc,nc.monto_bruto,nc.monto_neto,nc.monto_igv,nc.monto_total,'',0,0,nc.estado
        FROM nota_credito nc JOIN comprobante c ON c.comprobante_id=nc.comprobante_id JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id=c.tipo_doc_com_id
        JOIN estado_compro ec ON ec.estado_compro_id=nc.estado_compro_id
        WHERE nc.periodo_emp_sede_id=@ScopeId AND c.emitido='S' AND ec.estado_compro IN ('3','9')
            AND EXISTS(SELECT 1 FROM periodo_emp_sede cps JOIN empresa_sede cs ON cs.empresa_sede_id=cps.empresa_sede_id WHERE cps.periodo_emp_sede_id=c.periodo_emp_sede_id AND cs.empresa_id=@EmpresaId)
            AND (@Mes IS NULL OR MONTH(nc.fecha)=@Mes) AND (@Desde IS NULL OR nc.fecha>=@Desde) AND (@Hasta IS NULL OR nc.fecha<@Hasta)
    ), datos AS (
        SELECT a.Id,a.Origen,b.tipo_doc_com TipoCodigo,b.abrev TipoDocumento,CONCAT(RTRIM(a.serie),' - ',RTRIM(a.numero)) Documento,a.fecha Fecha,
            p.proveedor ClienteCodigo,CASE WHEN a.estado='A' THEN p.razon_social ELSE '***ANULADO***' END Cliente,
            CASE WHEN tnj.tipo_nat_juri='1' AND (b.tipo_doc_com='03' OR b.tipo_doc_com='07' AND a.TipoReferencia<>'01') THEN p.num_doc_iden ELSE p.numero_ruc END Identidad,
            m.moneda Moneda,m.simbolo Simbolo,a.monto_tc TipoCambio,COALESCE(tcp.nombre,'') CondicionPago,CASE WHEN tc.tipo_comprob='5' THEN 'Sí' ELSE 'No' END Anticipo,a.MedioPago,
            CASE WHEN a.estado='A' THEN 'Activo' ELSE 'Anulado' END Estado,
            CASE WHEN a.estado='A' THEN CASE WHEN m.es_local='S' THEN a.monto_bruto ELSE a.monto_bruto*a.monto_tc END ELSE 0 END Bruto,
            CASE WHEN a.estado='A' THEN CASE WHEN m.es_local='S' THEN a.monto_neto ELSE a.monto_neto*a.monto_tc END ELSE 0 END Neto,
            CASE WHEN a.estado='A' THEN CASE WHEN m.es_local='S' THEN a.monto_igv ELSE a.monto_igv*a.monto_tc END ELSE 0 END Igv,
            CASE WHEN a.estado='A' THEN CASE WHEN m.es_local='S' THEN a.monto_total ELSE a.monto_total*a.monto_tc END ELSE 0 END Total
        FROM origen a JOIN tipo_doc_com b ON b.tipo_doc_com_id=a.tipo_doc_com_id JOIN proveedor p ON p.proveedor_id=a.proveedor_id
        JOIN tipo_nat_juri tnj ON tnj.tipo_nat_juri_id=p.tipo_nat_juri_id JOIN moneda m ON m.moneda_id=a.moneda_id
        JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id=a.periodo_emp_sede_id JOIN empresa_sede s ON s.empresa_sede_id=pes.empresa_sede_id
        JOIN empresa e ON e.empresa_id=s.empresa_id JOIN tipo_via tv ON tv.tipo_via_id=e.tipo_via_id JOIN tipo_zona tz ON tz.tipo_zona_id=e.tipo_zona_id JOIN distrito d ON d.distrito_id=e.distrito_id
        LEFT JOIN tipo_c_pago tcp ON tcp.tipo_c_pago_id=a.tipo_c_pago_id LEFT JOIN tipo_comprob tc ON tc.tipo_comprob_id=a.tipo_comprob_id
        WHERE s.empresa_id=@EmpresaId AND p.empresa_id=@EmpresaId
    ) ";

    // Unlike the consolidated official report, the original official product report
    // includes every non-internal receipt type (also temporary) and only status 3.
    private const string RegistroVentaDetalleSql=@"WITH datos AS (
        SELECT cp.comprob_prod_id Id,'P' Origen,tdc.tipo_doc_com TipoCodigo,tdc.abrev TipoDocumento,CONCAT(RTRIM(c.serie),' - ',RTRIM(c.numero)) Documento,c.fecha Fecha,
            p.proveedor ClienteCodigo,p.razon_social Cliente,p.numero_ruc Identidad,m.moneda Moneda,m.simbolo Simbolo,c.monto_tc TipoCambio,mp.abrev MedioPago,
            CASE WHEN c.estado='A' THEN 'Activo' ELSE 'Anulado' END Estado,RTRIM(prd.producto) Codigo,prd.descripcion Producto,cp.descripcion Descripcion,um.abrev Unidad,
            gp.nombre Grupo,cl.nombre Clase,fp.nombre Familia,cp.cantidad Cantidad,
            CASE WHEN m.es_local='S' THEN cp.monto_bruto ELSE cp.monto_bruto*c.monto_tc END Bruto,
            CASE WHEN m.es_local='S' THEN cp.monto_neto ELSE cp.monto_neto*c.monto_tc END Neto,
            CASE WHEN m.es_local='S' THEN cp.monto_igv ELSE cp.monto_igv*c.monto_tc END Igv,
            CASE WHEN m.es_local='S' THEN cp.monto_total ELSE cp.monto_total*c.monto_tc END Total
        FROM comprob_prod cp JOIN producto prd ON prd.producto_id=cp.producto_id JOIN unidad_medida um ON um.unidad_medida_id=prd.unidad_medida_id
        "+VentaProductoEmpresa+@"
        JOIN comprobante c ON c.comprobante_id=cp.comprobante_id JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id=c.tipo_doc_com_id
        JOIN moneda m ON m.moneda_id=c.moneda_id JOIN proveedor p ON p.proveedor_id=c.proveedor_id
        JOIN medio_pago mp ON mp.medio_pago_id=c.medio_pago_id JOIN tipo_comprob tc ON tc.tipo_comprob_id=c.tipo_comprob_id JOIN estado_compro ec ON ec.estado_compro_id=c.estado_compro_id
        WHERE c.periodo_emp_sede_id=@ScopeId AND tp.empresa_id=@EmpresaId AND p.empresa_id=@EmpresaId AND mp.empresa_id=@EmpresaId
            AND c.emitido='S' AND ec.estado_compro='3' AND cp.estado='A'
            AND ((@Oficial=0 AND tc.tipo_comprob='1') OR (@Oficial=1 AND tdc.es_interno='N'))
            AND (@Mes IS NULL OR MONTH(c.fecha)=@Mes) AND (@Desde IS NULL OR c.fecha>=@Desde) AND (@Hasta IS NULL OR c.fecha<@Hasta)
    ) ";
}
