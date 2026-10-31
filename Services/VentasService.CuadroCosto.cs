using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class VentasService
{
    public async Task<CuadroCostoPageModel> GetCuadroCostoAsync(int empresaId,int contextoId,CuadroCostoFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null,bool exportar=false)
    {
        var model=new CuadroCostoPageModel{Filtro=filtro??new()};filtro=model.Filtro;
        filtro.Base=Text(filtro.Base).ToLowerInvariant();
        var columns=model.Columnas.ToDictionary(c=>c.Campo,c=>c.Campo);
        model.Ordenar(sort,dir,columns.Keys,"codigo");model.Paginar(0,page,top);
        if(empresaId<=0||contextoId<=0){model.Errores.Add("Seleccione un contexto de trabajo válido.");return model;}
        if(filtro.Base is not ("consulta" or "reporte")){model.Errores.Add("Seleccione una base de cálculo válida.");return model;}
        if(!Valid(filtro)){model.Errores.Add("El código admite 12 caracteres y la descripción 150.");return model;}
        using var conn=factory.CreateConnection();
        var scope=await GetContextoReporteAsync(conn,empresaId,contextoId);
        if(scope is null){model.Errores.Add("El período y sede no están habilitados para la empresa.");return model;}model.Contexto=scope;
        model.Listas=(await conn.QueryAsync<LookupItemModel>(@"SELECT lp.lista_precio_id Id,lp.nombre Nombre,m.moneda Codigo FROM lista_precio lp JOIN moneda m ON m.moneda_id=lp.moneda_id
            WHERE lp.empresa_sede_id=@SiteId AND lp.estado='A' ORDER BY lp.nombre,lp.lista_precio_id",new{SiteId=scope.EmpresaSedeId})).AsList();
        if(!filtro.Consultar&&!exportar)return model;
        var list=model.Listas.FirstOrDefault(l=>l.Id==filtro.ListaPrecioId);
        if(list is null){model.Errores.Add("Seleccione una lista de precios activa de la sede de trabajo.");return model;}
        model.Lista=list.Nombre;model.Moneda=list.Codigo;
        var months=(await conn.QueryAsync<int>("SELECT mes_contable_id FROM mes_contable WHERE mes_contable=@Mes AND estado='A'",new{Mes=scope.Hoy.Month.ToString("00")})).AsList();
        if(months.Count!=1){model.Errores.Add("El mes actual del servidor no tiene un mes contable activo único.");return model;}
        if(filtro.Base=="consulta")
        {
            var taxes=(await conn.QueryAsync<decimal>(@"SELECT CAST(1.00+(it.tasa/100) AS decimal(19,4)) FROM impuesto_tasa it JOIN tipo_impuesto ti ON ti.tipo_impuesto_id=it.tipo_impuesto_id WHERE ti.tipo_impuesto='01'")).AsList();
            if(taxes.Count!=1){model.Errores.Add("La consulta requiere una única tasa de IGV configurada para el tipo 01.");return model;}model.FactorIgv=taxes[0];
        }
        else
        {
            // The original report uses today's type-02 purchase rate without a currency filter.
            // Reject ambiguity rather than picking an arbitrary row from that configuration.
            var rates=(await conn.QueryAsync<decimal>(@"SELECT tcd.monto_c FROM tipo_camb_dia tcd JOIN tipo_cambio tc ON tc.tipo_cambio_id=tcd.tipo_cambio_id WHERE tc.tipo_cambio='02' AND tcd.fecha=@Hoy",new{scope.Hoy})).AsList();
            if(rates.Count>1){model.Errores.Add("Existe más de un T/C compra tipo 02 para hoy. Revise su configuración antes de emitir el reporte.");return model;}
            model.TipoCambio=rates.FirstOrDefault();
            bool foreign=await conn.ExecuteScalarAsync<bool>(@"SELECT CAST(CASE WHEN m.es_local='S' THEN 0 ELSE 1 END AS bit) FROM lista_precio lp JOIN moneda m ON m.moneda_id=lp.moneda_id WHERE lp.lista_precio_id=@Id",new{Id=list.Id});
            if(foreign&&model.TipoCambio==0)model.Avisos.Add("No hay T/C compra tipo 02 distinto de cero para hoy: el precio convertido y los márgenes quedan en cero, según el reporte legacy.");
        }
        var args=new DynamicParameters(new{EmpresaId=empresaId,ScopeId=contextoId,SiteId=scope.EmpresaSedeId,Anio=scope.Anio,ListaId=list.Id,MesId=months[0],Codigo=Search(filtro.Codigo),Descripcion=Search(filtro.Descripcion),Factor=model.FactorIgv,TC=model.TipoCambio});
        string query=CuadroCostoSql.Replace("{PROMEDIO}",filtro.Base=="consulta"?"ISNULL(masc.valor_unit,0)*CAST(@Factor AS decimal(19,4))":"ISNULL(masc.valor_unit,0)")
            .Replace("{COMPRA}",filtro.Base=="consulta"?"ISNULL(uc.valor_unitario,0)*CAST(@Factor AS decimal(19,4))":"ISNULL(uc.valor_unitario,0)")
            .Replace("{PRECIO}",filtro.Base=="consulta"?"lpp.precio_unitario":"CASE WHEN m.es_local='S' THEN lpp.precio_unitario ELSE ROUND(lpp.precio_unitario*CAST(@TC AS decimal(19,4)),2) END")
            .Replace("{FECHA}",filtro.Base=="consulta"?"('S01','S13')":"('S01')");
        int total=await conn.ExecuteScalarAsync<int>(query+" SELECT COUNT(*) FROM datos",args);model.Paginar(total,page,top);model.Consultada=true;
        if(exportar&&total>VentaMaxExportRows){model.Errores.Add($"La exportación supera {VentaMaxExportRows:N0} registros. Aplique filtros.");return model;}
        args.Add("Offset",exportar?0:(model.Page-1)*model.PageSize);args.Add("Top",exportar?Math.Max(1,total):model.PageSize);
        // Both keys retain the original row multiplicity if historical stock rows repeat a product.
        model.Items=(await conn.QueryAsync<CuadroCostoFila>(query+$" SELECT * FROM datos ORDER BY {columns[model.Sort]} {model.Dir},Id,SaldoId OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY",args)).AsList();
        model.VisibleCount=model.Items.Count;return model;
    }

    private static readonly string CuadroCostoSql=@"WITH compras AS (
        SELECT map.producto_id,map.valor_unitario,CASE WHEN ta.trans_alma IN {FECHA} THEN ma.fecha ELSE NULL END Fecha,
            ROW_NUMBER() OVER(PARTITION BY map.producto_id ORDER BY ma.fecha DESC,map.valor_unitario DESC,map.mov_alm_prod_id DESC) rn
        FROM mov_alm_prod map JOIN mov_almacen ma ON ma.mov_almacen_id=map.mov_almacen_id JOIN trans_alma ta ON ta.trans_alma_id=ma.trans_alma_id
        JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id=ma.periodo_emp_sede_id JOIN empresa_sede es ON es.empresa_sede_id=pes.empresa_sede_id
        WHERE es.empresa_id=@EmpresaId AND map.cantidad>0 AND map.estado='A' AND ta.es_compra='S'
            AND EXISTS(SELECT 1 FROM lista_p_prod l WHERE l.lista_precio_id=@ListaId AND l.producto_id=map.producto_id)
    ),valores AS (
        SELECT lpp.lista_p_prod_id Id,ISNULL(masc.mov_alm_s_con_id,0) SaldoId,p.producto_id ProductoId,RTRIM(p.producto) Codigo,p.descripcion Producto,
            {PROMEDIO} Promedio,uc.Fecha FechaCompra,{COMPRA} UltimaCompra,p.valor_repo Reposicion,{PRECIO} Precio
        FROM lista_p_prod lpp JOIN producto p ON p.producto_id=lpp.producto_id
        JOIN lista_precio lp ON lp.lista_precio_id=lpp.lista_precio_id JOIN moneda m ON m.moneda_id=lp.moneda_id
        "+VentaProductoEmpresa.Replace("prd.","p.")+@"
        LEFT JOIN mov_alm_s_con masc ON masc.producto_id=p.producto_id AND masc.periodo_emp_sede_id=@ScopeId AND masc.mes_contable_id=@MesId
        LEFT JOIN compras uc ON uc.producto_id=p.producto_id AND uc.rn=1
        WHERE lpp.lista_precio_id=@ListaId AND lp.empresa_sede_id=@SiteId AND lp.estado='A' AND tp.empresa_id=@EmpresaId
            AND @Anio BETWEEN YEAR(p.fecha_alta) AND ISNULL(YEAR(p.fecha_baja),@Anio)
            AND (@Codigo IS NULL OR p.producto LIKE @Codigo) AND (@Descripcion IS NULL OR p.descripcion LIKE @Descripcion)
    ),datos AS (
        SELECT *,CASE WHEN Precio=0 THEN 0 ELSE (Precio-Promedio)/Precio END MargenA,
            CASE WHEN Precio=0 THEN 0 ELSE (Precio-UltimaCompra)/Precio END MargenB FROM valores
    ) ";
}
