using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class VentasService
{
    private const int VentaMaxExportRows=50000;
    private const string VentaProductoEmpresa=@" JOIN familia_producto fp ON fp.familia_producto_id=prd.familia_producto_id
        JOIN clase_producto cl ON cl.clase_producto_id=fp.clase_producto_id JOIN grupo_producto gp ON gp.grupo_producto_id=cl.grupo_producto_id
        JOIN tipo_producto tp ON tp.tipo_producto_id=gp.tipo_producto_id ";

    public async Task<VentaReportePageModel> GetReporteVentaAsync(int empresaId,int contextoId,string tipo,VentaReporteFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null,bool exportar=false)
    {
        var model=new VentaReportePageModel{Tipo=tipo,Filtro=filtro??new(),Titulo=tipo switch{"temporales"=>"Documentos temporales","reservas"=>"Documentos con reserva","pedidos"=>"Pedidos por producto",_=>"Reporte de ventas"}};
        filtro=model.Filtro;filtro.Modo=Text(filtro.Modo).ToLowerInvariant();filtro.Agrupar=Text(filtro.Agrupar).ToLowerInvariant();
        if(filtro.Modo.Length==0)filtro.Modo=tipo=="reservas"?"actual":"mes";
        if(filtro.Agrupar.Length==0)filtro.Agrupar="producto";
        var columns=model.Columnas.ToDictionary(c=>c.Campo.ToLowerInvariant(),c=>c.Campo);
        model.Ordenar(sort,dir,columns.Keys,tipo=="temporales"?"fecha":tipo=="pedidos"?"pedido":filtro.Agrupar=="documento"?"documento":"codigo");model.Paginar(0,page,top);
        if(tipo is not ("temporales" or "reservas" or "pedidos")||empresaId<=0||contextoId<=0){model.Errores.Add("El reporte o contexto no está disponible.");return model;}
        using var conn=factory.CreateConnection();
        var scope=await conn.QuerySingleOrDefaultAsync<VentaReporteContexto>(@"SELECT pes.periodo_emp_sede_id PeriodoEmpresaSedeId,s.empresa_sede_id EmpresaSedeId,p.periodo Anio,
            e.nombre Empresa,s.nombre Sede,CAST(GETDATE() AS date) Hoy FROM periodo_emp_sede pes
            JOIN empresa_sede s ON s.empresa_sede_id=pes.empresa_sede_id JOIN empresa e ON e.empresa_id=s.empresa_id JOIN periodo p ON p.periodo_id=pes.periodo_id
            WHERE pes.periodo_emp_sede_id=@Id AND s.empresa_id=@EmpresaId AND pes.estado='A' AND s.estado='A' AND e.estado='A' AND p.estado='A'",new{Id=contextoId,EmpresaId=empresaId});
        if(scope is null){model.Errores.Add("El período y sede no están habilitados para la empresa.");return model;}model.Contexto=scope;
        model.Meses=(await conn.QueryAsync<LookupItemModel>("SELECT TRY_CONVERT(int,mes_contable) Id,RTRIM(mes_contable) Codigo,nombre Nombre FROM mes_contable WHERE estado='A' AND mes_contable BETWEEN '01' AND '12' ORDER BY mes_contable")).AsList();
        if(tipo=="reservas")model.Vendedores=(await conn.QueryAsync<LookupItemModel>("SELECT v.vendedor_id Id,CONCAT(RTRIM(v.vendedor),' · ',p.razon_social) Nombre FROM vendedor v JOIN proveedor p ON p.proveedor_id=v.proveedor_id WHERE p.empresa_id=@EmpresaId ORDER BY p.razon_social,v.vendedor_id",new{EmpresaId=empresaId})).AsList();
        if(filtro.Search?.Length>150)model.Errores.Add("La búsqueda admite hasta 150 caracteres.");
        if(filtro.VendedorId.HasValue&&(filtro.VendedorId<=0||tipo!="reservas"||!model.Vendedores.Any(v=>v.Id==filtro.VendedorId)))model.Errores.Add("Seleccione un vendedor de la empresa.");
        if(filtro.Agrupar is not ("producto" or "documento"))model.Errores.Add("Seleccione una vista de agrupación válida.");
        if(tipo=="reservas")
        {
            if(filtro.Modo!="actual"||filtro.Mes.HasValue||filtro.Desde.HasValue||filtro.Hasta.HasValue)model.Errores.Add("Las reservas se consultan con el mes actual del servidor.");
            if(!model.Meses.Any(m=>m.Id==scope.Hoy.Month))model.Errores.Add("El mes actual no está habilitado.");
        }
        else
        {
            if((tipo=="pedidos"&&filtro.Modo!="mes")||(tipo=="temporales"&&filtro.Modo is not ("mes" or "rango" or "anual")))model.Errores.Add("Seleccione un modo de consulta válido.");
            if(filtro.Modo=="mes"){filtro.Mes??=scope.Hoy.Month;if(!model.Meses.Any(m=>m.Id==filtro.Mes))model.Errores.Add("Seleccione un mes contable vigente.");}
            if(filtro.Modo=="rango"&&(!filtro.Desde.HasValue||!filtro.Hasta.HasValue||filtro.Desde.Value.Year is <1900 or >9998||filtro.Hasta.Value.Year is <1900 or >9998||filtro.Desde.Value.Date>filtro.Hasta.Value.Date))model.Errores.Add("Seleccione un rango de fechas válido.");
        }
        if(model.Errores.Count>0||(!filtro.Consultar&&!exportar))return model;model.Consultada=true;
        var args=new DynamicParameters(new{EmpresaId=empresaId,ScopeId=contextoId,SiteId=scope.EmpresaSedeId,Mes=filtro.Modo=="mes"?filtro.Mes:null,
            MesReserva=scope.Hoy.Month.ToString("00"),Desde=filtro.Modo=="rango"?filtro.Desde?.Date:null,Hasta=filtro.Modo=="rango"?filtro.Hasta?.Date.AddDays(1):null,
            filtro.VendedorId,Search=Search(filtro.Search)});
        string query=tipo switch{"temporales"=>TemporalesSql,"pedidos"=>PedidosSql,_=>ReservasSql};
        string searchFields=tipo switch{"temporales"=>"Codigo,Cliente,Ruc,Documento,TipoDocumento,MedioPago,Estado","pedidos"=>"Codigo,Producto,Detalle,Pedido,Cliente,Vendedor,ListaPrecio,Estado",_=>"Codigo,Producto,Documento,TipoDocumento,Cliente,Vendedor,Estado"};
        string where=" WHERE (@Search IS NULL OR "+string.Join(" OR ",searchFields.Split(',').Select(c=>c+" LIKE @Search"))+")";
        // Reservation scopes have very different cardinalities. Avoid both a reused scope plan and
        // oversized memory grants that can leave this read waiting on RESOURCE_SEMAPHORE.
        string plan=tipo=="reservas"?" OPTION (RECOMPILE, MAX_GRANT_PERCENT=1, MAXDOP 1)":"";
        int total=await conn.ExecuteScalarAsync<int>(query+" SELECT COUNT(*) FROM datos"+where+plan,args);model.Paginar(total,page,top);
        if(exportar&&total>VentaMaxExportRows){model.Errores.Add($"La exportación supera {VentaMaxExportRows:N0} registros. Aplique filtros para exportar el resultado completo.");return model;}
        if(tipo=="temporales")model.TotalLocal=await conn.ExecuteScalarAsync<decimal>(query+" SELECT ISNULL(SUM(Total),0) FROM datos"+where,args);
        args.Add("Offset",exportar?0:(model.Page-1)*model.PageSize);args.Add("Top",exportar?Math.Max(1,total):model.PageSize);
        string tie=tipo=="reservas"?"SaldoId,Id,ProductoId,Fecha,TipoDocumento,Documento,ClienteId,VendedorId,Cantidad,Estado":"Id";
        // Remove a repeated sort field from the tie list; the remaining projected fields provide stable pages.
        var order=new[]{columns[model.Sort]+" "+model.Dir}.Concat(tie.Split(',').Where(c=>!c.Equals(columns[model.Sort],StringComparison.OrdinalIgnoreCase)));
        model.Items=(await conn.QueryAsync<VentaReporteRowModel>(query+" SELECT * FROM datos"+where+" ORDER BY "+string.Join(",",order)+" OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY"+plan,args)).AsList();
        model.VisibleCount=model.Items.Count;return model;
    }

    private const string TemporalesSql=@"WITH datos AS (
        SELECT c.comprobante_id Id,c.fecha Fecha,tdc.abrev TipoDocumento,CONCAT(RTRIM(c.serie),' - ',RTRIM(c.numero)) Documento,RTRIM(p.proveedor) Codigo,
            CASE WHEN c.estado='A' THEN p.razon_social ELSE '***ANULADO***' END Cliente,ISNULL(p.numero_ruc,'') Ruc,m.moneda Moneda,c.monto_tc TipoCambio,
            CASE WHEN c.estado='A' THEN CASE WHEN m.es_local='S' THEN c.monto_bruto ELSE c.monto_bruto*c.monto_tc END ELSE 0 END Bruto,
            CASE WHEN c.estado='A' THEN CASE WHEN m.es_local='S' THEN c.monto_neto ELSE c.monto_neto*c.monto_tc END ELSE 0 END Neto,
            CASE WHEN c.estado='A' THEN CASE WHEN m.es_local='S' THEN c.monto_igv ELSE c.monto_igv*c.monto_tc END ELSE 0 END Igv,
            CASE WHEN c.estado='A' THEN CASE WHEN m.es_local='S' THEN c.monto_total ELSE c.monto_total*c.monto_tc END ELSE 0 END Total,
            mp.abrev MedioPago,CASE WHEN c.estado='A' THEN 'Activo' ELSE 'Anulado' END Estado
        FROM comprobante c JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id=c.tipo_doc_com_id JOIN tipo_comprob tc ON tc.tipo_comprob_id=c.tipo_comprob_id
        JOIN estado_compro ec ON ec.estado_compro_id=c.estado_compro_id JOIN medio_pago mp ON mp.medio_pago_id=c.medio_pago_id
        JOIN proveedor p ON p.proveedor_id=c.proveedor_id JOIN moneda m ON m.moneda_id=c.moneda_id
        WHERE c.periodo_emp_sede_id=@ScopeId AND p.empresa_id=@EmpresaId AND mp.empresa_id=@EmpresaId AND tc.tipo_comprob='4'
            AND ec.estado_compro IN ('3','9') AND c.emitido='S' AND (@Mes IS NULL OR MONTH(c.fecha)=@Mes)
            AND (@Desde IS NULL OR c.fecha>=@Desde) AND (@Hasta IS NULL OR c.fecha<@Hasta)
    ) ";

    private const string PedidosSql=@"WITH datos AS (
        SELECT cp.cotiza_prod_id Id,RTRIM(c.cotiza) Pedido,RTRIM(prd.producto) Codigo,prd.descripcion Producto,um.abrev Unidad,cp.descripcion Detalle,
            cp.cantidad Cantidad,cp.precio_unitario PrecioUnitario,cp.monto_total Total,lp.nombre ListaPrecio,m.moneda Moneda,
            p.razon_social Cliente,pv.razon_social Vendedor,ec.nombre Estado
        FROM cotiza_prod cp JOIN cotiza c ON c.cotiza_id=cp.cotiza_id JOIN producto prd ON prd.producto_id=cp.producto_id
        JOIN unidad_medida um ON um.unidad_medida_id=prd.unidad_medida_id JOIN lista_precio lp ON lp.lista_precio_id=c.lista_precio_id
        JOIN empresa_sede ls ON ls.empresa_sede_id=lp.empresa_sede_id JOIN proveedor p ON p.proveedor_id=c.proveedor_id
        JOIN vendedor v ON v.vendedor_id=c.vendedor_id JOIN proveedor pv ON pv.proveedor_id=v.proveedor_id JOIN moneda m ON m.moneda_id=c.moneda_id
        JOIN estado_cotiza ec ON ec.estado_cotiza_id=c.estado_cotiza_id JOIN mes_contable mc ON mc.mes_contable_id=c.mes_contable_id
        "+VentaProductoEmpresa+@"
        WHERE c.periodo_emp_sede_id=@ScopeId AND TRY_CONVERT(int,mc.mes_contable)=@Mes AND c.estado='A' AND cp.estado='A'
            AND c.estado_cotiza_id NOT IN (1,5) AND p.empresa_id=@EmpresaId AND pv.empresa_id=@EmpresaId AND ls.empresa_id=@EmpresaId AND tp.empresa_id=@EmpresaId
    ) ";

    private const string ReservasSql=@"WITH saldos AS (
        SELECT mas.mov_alm_saldo_id,mas.periodo_emp_sede_id,mas.producto_id,mas.cant_reserva
        FROM mov_alm_saldo mas JOIN almacen a ON a.almacen_id=mas.almacen_id JOIN mes_contable mc ON mc.mes_contable_id=mas.mes_contable_id
        WHERE mas.periodo_emp_sede_id=@ScopeId AND mc.mes_contable=@MesReserva AND a.es_venta='S' AND mas.cant_reserva>0 AND a.empresa_sede_id=@SiteId
    ),documentos AS (
        SELECT c.periodo_emp_sede_id ScopeId,cp.producto_id ProductoId,'PEDIDO' TipoDocumento,c.fecha Fecha,c.cotiza Documento,c.proveedor_id ClienteId,c.vendedor_id VendedorId,
            cp.cantidad-cp.cant_compro Cantidad,ec.nombre Estado
        FROM cotiza_prod cp JOIN cotiza c ON c.cotiza_id=cp.cotiza_id JOIN estado_cotiza ec ON ec.estado_cotiza_id=c.estado_cotiza_id
        WHERE c.periodo_emp_sede_id=@ScopeId AND ec.estado_cotiza IN ('02','04') AND cp.estado='A' AND cp.cantidad<>cp.cant_compro AND (@VendedorId IS NULL OR c.vendedor_id=@VendedorId)
            AND EXISTS(SELECT 1 FROM saldos sr WHERE sr.producto_id=cp.producto_id)
        UNION
        SELECT c.periodo_emp_sede_id,cp.producto_id,tdc.abrev,c.fecha,CONCAT(RTRIM(c.serie),' - ',RTRIM(c.numero)),c.proveedor_id,c.vendedor_id,cp.cantidad,ec.nombre
        FROM comprob_prod cp JOIN comprobante c ON c.comprobante_id=cp.comprobante_id JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id=c.tipo_doc_com_id
        JOIN estado_compro ec ON ec.estado_compro_id=c.estado_compro_id
        WHERE c.periodo_emp_sede_id=@ScopeId AND ec.estado_compro IN ('1','2') AND cp.estado='A' AND (@VendedorId IS NULL OR c.vendedor_id=@VendedorId)
            AND EXISTS(SELECT 1 FROM saldos sr WHERE sr.producto_id=cp.producto_id)
    ),datos AS (
        SELECT 0 Id,mas.mov_alm_saldo_id SaldoId,prd.producto_id ProductoId,RTRIM(prd.producto) Codigo,prd.descripcion Producto,
            mas.cant_reserva ReservaReferencia,d.Fecha,d.TipoDocumento,RTRIM(d.Documento) Documento,d.ClienteId,d.VendedorId,p.razon_social Cliente,pv.razon_social Vendedor,d.Cantidad,d.Estado
        FROM saldos mas
        JOIN documentos d ON d.ScopeId=mas.periodo_emp_sede_id AND d.ProductoId=mas.producto_id
        JOIN producto prd ON prd.producto_id=mas.producto_id "+VentaProductoEmpresa+@"
        JOIN vendedor v ON v.vendedor_id=d.VendedorId JOIN proveedor pv ON pv.proveedor_id=v.proveedor_id JOIN proveedor p ON p.proveedor_id=d.ClienteId
        WHERE p.empresa_id=@EmpresaId AND pv.empresa_id=@EmpresaId AND tp.empresa_id=@EmpresaId
        UNION ALL
        SELECT cp.cotiza_prod_id,0,prd.producto_id,RTRIM(prd.producto),prd.descripcion,0,c.fecha,'PEDIDO',RTRIM(c.cotiza),c.proveedor_id,c.vendedor_id,p.razon_social,pv.razon_social,
            cp.cantidad-cp.cant_compro,ec.nombre
        FROM cotiza_prod cp JOIN cotiza c ON c.cotiza_id=cp.cotiza_id JOIN estado_cotiza ec ON ec.estado_cotiza_id=c.estado_cotiza_id
        JOIN producto prd ON prd.producto_id=cp.producto_id "+VentaProductoEmpresa+@"
        JOIN vendedor v ON v.vendedor_id=c.vendedor_id JOIN proveedor pv ON pv.proveedor_id=v.proveedor_id JOIN proveedor p ON p.proveedor_id=c.proveedor_id
        WHERE c.periodo_emp_sede_id=@ScopeId AND ec.estado_cotiza IN ('02','04') AND cp.estado='A' AND cp.cantidad<>cp.cant_compro
            AND (@VendedorId IS NULL OR c.vendedor_id=@VendedorId) AND tp.tipo_producto='S' AND tp.empresa_id=@EmpresaId AND p.empresa_id=@EmpresaId AND pv.empresa_id=@EmpresaId
    ) ";
}
