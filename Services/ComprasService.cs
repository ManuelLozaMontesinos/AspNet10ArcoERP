using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public class ComprasService(IDbConnectionFactory factory) : IComprasService
{
    public async Task<PuntoPedidoPageModel> GetPuntoPedidoAsync(int empresaId,int contextoId,PuntoPedidoFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null,bool exportar=false)
    {
        var model=new PuntoPedidoPageModel{Filtro=filtro??new()};filtro=model.Filtro;
        var columns=new Dictionary<string,string>{{"codigo","Codigo"},{"producto","Producto"},{"unidad","Unidad"},{"proveedor","Proveedor"},{"documento","Documento"},{"minimo","StockMinimo"},{"saldo","Saldo"}};
        model.Ordenar(sort,dir,columns.Keys,"codigo");model.Paginar(0,page,top);
        if(empresaId<=0||contextoId<=0){model.Contexto.Errores.Add("Seleccione un contexto de trabajo válido.");return model;}
        using var conn=factory.CreateConnection();model.Contexto=await ConsultaSedeSql.CargarAsync(conn,empresaId,contextoId,filtro.ConsultaId);
        if(filtro.Search?.Length>150||filtro.Codigo?.Length>12||filtro.Documento?.Length>35||filtro.RazonSocial?.Length>150)model.Contexto.Errores.Add("Reduzca la longitud de los filtros.");
        if(!filtro.ConsultaId.HasValue){if(exportar)model.Contexto.Errores.Add("Seleccione una sede para exportar.");return model;}
        if(model.Contexto.Errores.Count>0)return model;model.Consultada=true;
        var args=new DynamicParameters(new{EmpresaId=empresaId,ScopeId=filtro.ConsultaId,Search=Search(filtro.Search),Codigo=Search(filtro.Codigo),Documento=string.IsNullOrWhiteSpace(filtro.Documento)?null:filtro.Documento.Trim(),Proveedor=Search(filtro.RazonSocial)});
        int total=await conn.ExecuteScalarAsync<int>(PuntoPedidoSql+" SELECT COUNT(*) FROM datos",args);model.Paginar(total,exportar?1:page,top);
        if(exportar&&total>50000){model.Contexto.Errores.Add("La exportación supera 50.000 registros. Aplique filtros para exportar el resultado completo.");return model;}
        args.Add("Offset",exportar?0:(model.Page-1)*model.PageSize);args.Add("Top",exportar?Math.Max(1,total):model.PageSize);
        model.Items=(await conn.QueryAsync<PuntoPedidoFila>(PuntoPedidoSql+" SELECT * FROM datos ORDER BY "+columns[model.Sort]+" "+model.Dir+",ProductoId,VinculoId,Id OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY",args)).AsList();
        model.VisibleCount=model.Items.Count;return model;
    }

    private const string PuntoPedidoSql=@"WITH saldos AS (
        SELECT mas.producto_id,SUM((mas.cant_ant+mas.cant_ing)-(mas.cant_reserva+mas.cant_sal)) Saldo
        FROM mov_alm_saldo mas JOIN mov_alm_cierre mac ON mac.periodo_emp_sede_id=mas.periodo_emp_sede_id AND mac.almacen_id=mas.almacen_id AND mac.mes_contable_id=mas.mes_contable_id
        JOIN estado_ape_cie eac ON eac.estado_ape_cie_id=mac.estado_ape_cie_id
        WHERE mas.periodo_emp_sede_id=@ScopeId AND eac.estado_ape_cie='A' GROUP BY mas.producto_id
    ),datos AS (
        SELECT avp.alm_vol_prod_id Id,prd.producto_id ProductoId,pp.prove_prod_id VinculoId,p.proveedor_id ProveedorId,
            RTRIM(prd.producto) Codigo,prd.descripcion Producto,um.abrev Unidad,p.razon_social Proveedor,p.num_doc_iden Documento,
            prd.peso_neto PesoNeto,prd.peso_bruto PesoBruto,prd.master_pack MasterPack,avp.stock_min StockMinimo,sal.Saldo
        FROM alm_vol_prod avp JOIN periodo_emp_sede pes ON pes.empresa_sede_id=avp.empresa_sede_id AND pes.periodo_emp_sede_id=@ScopeId
        JOIN producto prd ON prd.producto_id=avp.producto_id JOIN unidad_medida um ON um.unidad_medida_id=prd.unidad_medida_id
        JOIN familia_producto fp ON fp.familia_producto_id=prd.familia_producto_id JOIN clase_producto cp ON cp.clase_producto_id=fp.clase_producto_id
        JOIN grupo_producto gp ON gp.grupo_producto_id=cp.grupo_producto_id JOIN tipo_producto tp ON tp.tipo_producto_id=gp.tipo_producto_id
        JOIN saldos sal ON sal.producto_id=prd.producto_id JOIN prove_prod pp ON pp.producto_id=prd.producto_id JOIN proveedor p ON p.proveedor_id=pp.proveedor_id
        WHERE tp.empresa_id=@EmpresaId AND p.empresa_id=@EmpresaId AND avp.stock_min>0 AND sal.Saldo<avp.stock_min
            AND (@Codigo IS NULL OR prd.producto LIKE @Codigo) AND p.num_doc_iden=COALESCE(@Documento,p.num_doc_iden)
            AND p.razon_social LIKE COALESCE(@Proveedor,'%')
            AND (@Search IS NULL OR prd.producto LIKE @Search OR prd.descripcion LIKE @Search OR p.razon_social LIKE @Search OR p.num_doc_iden LIKE @Search)
    ) ";
}
