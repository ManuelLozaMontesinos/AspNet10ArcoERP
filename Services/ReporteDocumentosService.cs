using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public class ReporteDocumentosService(IDbConnectionFactory factory) : IReporteDocumentosService
{
    private const int MaxExportRows=50000;

    public async Task<DocumentoReportePageModel> GetReporteAsync(int empresaId,int contextoId,string tipo,DocumentoReporteFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null,bool exportar=false)
    {
        var model=new DocumentoReportePageModel{Tipo=tipo,Filtro=filtro??new()};filtro=model.Filtro;
        var columns=model.Columnas.ToDictionary(c=>c.Campo,StringComparer.OrdinalIgnoreCase);
        model.Ordenar(sort,dir,columns.Keys,tipo=="guias"?"fecha":"orden");model.Paginar(0,page,top);
        if(tipo is not ("guias" or "compras")||empresaId<=0||contextoId<=0){model.Errores.Add("El reporte o contexto no está disponible.");return model;}
        using var conn=factory.CreateConnection();
        var scope=await conn.QuerySingleOrDefaultAsync<DocumentoReporteContexto>(@"SELECT pes.periodo_emp_sede_id PeriodoEmpresaSedeId,pes.periodo_id PeriodoId,p.periodo Anio,
            e.nombre Empresa,s.nombre Sede,CAST(GETDATE() AS date) Hoy FROM periodo_emp_sede pes
            JOIN empresa_sede s ON s.empresa_sede_id=pes.empresa_sede_id JOIN empresa e ON e.empresa_id=s.empresa_id JOIN periodo p ON p.periodo_id=pes.periodo_id
            WHERE pes.periodo_emp_sede_id=@Id AND s.empresa_id=@EmpresaId AND pes.estado='A' AND s.estado='A' AND e.estado='A' AND p.estado='A'",new{Id=contextoId,EmpresaId=empresaId});
        if(scope is null){model.Errores.Add("El período y sede no están habilitados para la empresa.");return model;}model.Contexto=scope;
        // These two legacy reports use mes, not the accounting-month foreign key.
        model.Meses=(await conn.QueryAsync<LookupItemModel>("SELECT TRY_CONVERT(int,mes) Id,RTRIM(mes) Codigo,nombre Nombre FROM mes WHERE estado='A' AND mes BETWEEN '01' AND '12' ORDER BY mes")).AsList();
        if(tipo=="compras")model.Proveedores=(await conn.QueryAsync<LookupItemModel>(@"SELECT DISTINCT p.proveedor_id Id,RTRIM(p.proveedor) Codigo,CONCAT(RTRIM(p.proveedor),' · ',p.razon_social) Nombre
            FROM proveedor p JOIN orden_co oc ON oc.proveedor_id=p.proveedor_id JOIN orden_c_comp occ ON occ.orden_co_id=oc.orden_co_id
            JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id=oc.periodo_emp_sede_id JOIN empresa_sede s ON s.empresa_sede_id=pes.empresa_sede_id
            WHERE p.empresa_id=@EmpresaId AND s.empresa_id=@EmpresaId AND pes.periodo_id=@PeriodoId AND occ.estado='A' ORDER BY Nombre,p.proveedor_id",new{EmpresaId=empresaId,scope.PeriodoId})).AsList();
        filtro.Modo=Text(filtro.Modo).ToLowerInvariant();if(filtro.Modo.Length==0)filtro.Modo="mes";
        if((tipo=="compras"&&filtro.Modo!="mes")||(tipo=="guias"&&filtro.Modo is not ("mes" or "rango")))model.Errores.Add("Seleccione un modo de consulta válido.");
        if(filtro.Modo=="mes")
        {
            filtro.Mes??=scope.Hoy.Month;
            if(!model.Meses.Any(m=>m.Id==filtro.Mes))model.Errores.Add("Seleccione un mes vigente.");
        }
        if(filtro.Modo=="rango"&&(!filtro.Desde.HasValue||!filtro.Hasta.HasValue||filtro.Desde.Value.Year is <1900 or >9998||filtro.Hasta.Value.Year is <1900 or >9998||filtro.Desde.Value.Date>filtro.Hasta.Value.Date))model.Errores.Add("Seleccione un rango de fechas válido.");
        if(tipo=="compras"&&(filtro.Desde.HasValue||filtro.Hasta.HasValue))model.Errores.Add("La valorización se filtra por el mes de la orden de compra.");
        if(filtro.ProveedorId.HasValue&&(tipo!="compras"||!model.Proveedores.Any(p=>p.Id==filtro.ProveedorId)))model.Errores.Add("Seleccione un proveedor con comprobantes del ejercicio de esta empresa.");
        if(filtro.Search?.Length>150)model.Errores.Add("La búsqueda admite hasta 150 caracteres.");
        if(model.Errores.Count>0||(!filtro.Consultar&&!exportar))return model;model.Consultada=true;
        var args=new DynamicParameters(new{EmpresaId=empresaId,ScopeId=contextoId,scope.PeriodoId,Mes=filtro.Modo=="mes"?filtro.Mes:null,
            Desde=filtro.Modo=="rango"?filtro.Desde?.Date:null,Hasta=filtro.Modo=="rango"?filtro.Hasta?.Date.AddDays(1):null,filtro.ProveedorId,Search=Search(filtro.Search)});
        string query=tipo=="guias"?GuiasSql:ComprasSql;
        string search=tipo=="guias"?"Serie,Numero,Cliente,DocumentoCliente,Destinatario,DocumentoDestinatario,Placa,Motivo,Estado,Sunat,PuntoPartida,PuntoLlegada,Observaciones":"Orden,Serie,Numero,Proveedor,CodigoProveedor,DocumentoProveedor,Ruc,TipoDocumento,Moneda,CondicionPago";
        string where=" WHERE (@Search IS NULL OR "+string.Join(" OR ",search.Split(',').Select(c=>c+" LIKE @Search"))+")";
        int total=await conn.ExecuteScalarAsync<int>(query+" SELECT COUNT(*) FROM datos"+where,args);model.Paginar(total,exportar?1:page,top);
        if(exportar&&total>MaxExportRows){model.Errores.Add($"La exportación supera {MaxExportRows:N0} registros. Aplique filtros para exportar el resultado completo.");return model;}
        args.Add("Offset",exportar?0:(model.Page-1)*model.PageSize);args.Add("Top",exportar?Math.Max(1,total):model.PageSize);
        // Preserve the report's default secondary order and use Id to break remaining ties.
        string tie=tipo=="guias"?"Fecha,Serie,Numero,Id":"Orden,Proveedor,Id";
        var order=new[]{columns[model.Sort].Campo+" "+model.Dir}.Concat(tie.Split(',').Where(c=>!c.Equals(model.Sort,StringComparison.OrdinalIgnoreCase)));
        model.Items=(await conn.QueryAsync<DocumentoReporteFila>(query+" SELECT * FROM datos"+where+" ORDER BY "+string.Join(",",order)+" OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY",args)).AsList();
        model.VisibleCount=model.Items.Count;return model;
    }

    private const string GuiasSql=@"WITH datos AS (
        SELECT gr.guia_remision_id Id,RTRIM(gr.serie) Serie,RTRIM(gr.numero) Numero,gr.fecha Fecha,gr.fec_ini_tra Traslado,gr.costo_flete Flete,
            p.razon_social Cliente,p.num_doc_iden DocumentoCliente,CONCAT(dp.departamento,prv.provincia,d.distrito) UbigeoPartida,
            gr.punto_partida PuntoPartida,gr.cod_sunat_par EstablecimientoPartida,tdc.abrev TipoReferencia,gr.serie_d_ref SerieReferencia,gr.numero_d_ref NumeroReferencia,
            p2.razon_social Destinatario,p2.num_doc_iden DocumentoDestinatario,CONCAT(dp2.departamento,prv2.provincia,d2.distrito) UbigeoLlegada,
            gr.punto_llegada PuntoLlegada,gr.cod_sunat_lle EstablecimientoLlegada,tdi.abrev TipoDocumentoConductor,tc.num_doc_iden DocumentoConductor,
            tu.placa Placa,tu.placa_2 PlacaAdicional,gr.peso_b_total Peso,gr.observa Observaciones,mt.nombre Motivo,egr.nombre Estado,es.nombre Sunat
        FROM guia_remision gr JOIN proveedor p ON p.proveedor_id=gr.proveedor_id JOIN tipo_nat_juri tnj ON tnj.tipo_nat_juri_id=p.tipo_nat_juri_id
        JOIN distrito d ON d.distrito_id=gr.distrito_id JOIN provincia prv ON prv.provincia_id=d.provincia_id JOIN departamento dp ON dp.departamento_id=prv.departamento_id
        LEFT JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id=gr.tipo_d_com_ref_id
        JOIN proveedor p2 ON p2.proveedor_id=gr.proveedor2_id JOIN tipo_nat_juri tnj2 ON tnj2.tipo_nat_juri_id=p2.tipo_nat_juri_id
        JOIN distrito d2 ON d2.distrito_id=gr.distrito_llegada_id JOIN provincia prv2 ON prv2.provincia_id=d2.provincia_id JOIN departamento dp2 ON dp2.departamento_id=prv2.departamento_id
        JOIN transp_condu tc ON tc.transp_condu_id=gr.transp_condu_id JOIN tipo_doc_iden tdi ON tdi.tipo_doc_iden_id=tc.tipo_doc_iden_id
        JOIN transpo trc ON trc.transpo_id=tc.transpo_id JOIN transp_unid tu ON tu.transp_unid_id=gr.transp_unid_id JOIN transpo tru ON tru.transpo_id=tu.transpo_id
        JOIN motivo_trasla mt ON mt.motivo_trasla_id=gr.motivo_trasla_id JOIN estado_guia_r egr ON egr.estado_guia_r_id=gr.estado_guia_r_id
        LEFT JOIN estado_sunat es ON es.estado_sunat_id=gr.estado_sunat_id
        WHERE gr.periodo_emp_sede_id=@ScopeId AND p.empresa_id=@EmpresaId AND p2.empresa_id=@EmpresaId AND trc.empresa_id=@EmpresaId AND tru.empresa_id=@EmpresaId
            AND egr.estado_guia_r IN ('2','9') AND (@Mes IS NULL OR MONTH(gr.fecha)=@Mes)
            AND (@Desde IS NULL OR gr.fecha>=@Desde) AND (@Hasta IS NULL OR gr.fecha<@Hasta)
    ) ";

    private const string ComprasSql=@"WITH datos AS (
        SELECT occ.orden_c_comp_id Id,oc.proveedor_id ProveedorId,RTRIM(occ.serie) Serie,RTRIM(occ.numero) Numero,occ.fecha Fecha,occ.fecha_vcto Vencimiento,
            occ.monto_bruto Bruto,occ.monto_impuesto Impuesto,occ.monto_total Total,RTRIM(oc.orden_co) Orden,RTRIM(p.proveedor) CodigoProveedor,
            p.razon_social Proveedor,p.num_doc_iden DocumentoProveedor,p.numero_ruc Ruc,m.moneda Moneda,m.simbolo Simbolo,tdc.tipo_doc_com CodigoDocumento,tdc.abrev TipoDocumento,
            cp.cond_pago CodigoPago,cp.nombre CondicionPago
        FROM orden_c_comp occ JOIN orden_co oc ON oc.orden_co_id=occ.orden_co_id JOIN proveedor p ON p.proveedor_id=oc.proveedor_id
        JOIN moneda m ON m.moneda_id=oc.moneda_id JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id=oc.periodo_emp_sede_id JOIN empresa_sede s ON s.empresa_sede_id=pes.empresa_sede_id
        JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id=occ.tipo_doc_com_id LEFT JOIN cond_pago cp ON cp.cond_pago_id=occ.cond_pago_id AND cp.empresa_id=@EmpresaId
        WHERE pes.periodo_id=@PeriodoId AND s.empresa_id=@EmpresaId AND p.empresa_id=@EmpresaId AND MONTH(oc.fecha)=@Mes AND occ.estado='A'
            AND (@ProveedorId IS NULL OR oc.proveedor_id=@ProveedorId)
    ) ";
}
