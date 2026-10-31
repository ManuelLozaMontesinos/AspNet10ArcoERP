using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class VentasService
{
    // Group ID 5 is the explicit classification used by the legacy module.
    private const string CorteClaseFrom=@" FROM clase_producto cp JOIN grupo_producto gp ON gp.grupo_producto_id=cp.grupo_producto_id
        JOIN tipo_producto tp ON tp.tipo_producto_id=gp.tipo_producto_id WHERE cp.grupo_producto_id=5 AND tp.empresa_id=@EmpresaId ";
    private const string CorteClaseSelect=@"SELECT cp.clase_producto_id Id,RTRIM(cp.clase_producto) Codigo,cp.nombre Nombre,COALESCE(cp.estado,'') Estado,
        COALESCE(cp.fecha_modifica,cp.fecha_creacion) FechaRevision ";
    private const string CorteSelect=@"SELECT cg.cortes_gratis_id Id,cg.empresa_sede_id EmpresaSedeId,cg.producto_id ProductoId,fp.clase_producto_id ClaseId,p.familia_producto_id FamiliaId,
        RTRIM(p.producto) Codigo,p.descripcion Producto,fp.nombre Familia,COALESCE(p.estado,'') ProductoEstado,COALESCE(cp.estado,'') ClaseEstado,
        cg.cantidad Cantidad,COALESCE(cg.estado,'') Estado,COALESCE(cg.fecha_modifica,cg.fecha_creacion) FechaRevision ";
    private const string CorteFrom=@" FROM cortes_gratis cg {LOCK} JOIN producto p ON p.producto_id=cg.producto_id
        JOIN familia_producto fp ON fp.familia_producto_id=p.familia_producto_id JOIN clase_producto cp ON cp.clase_producto_id=fp.clase_producto_id
        JOIN grupo_producto gp ON gp.grupo_producto_id=cp.grupo_producto_id JOIN tipo_producto tp ON tp.tipo_producto_id=gp.tipo_producto_id
        JOIN empresa_sede es ON es.empresa_sede_id=cg.empresa_sede_id
        WHERE cg.empresa_sede_id=@SedeId AND es.empresa_id=@EmpresaId AND tp.empresa_id=@EmpresaId AND cp.grupo_producto_id=5 AND cp.clase_producto_id=@ClaseId ";

    public async Task<CortesGratisPageModel> GetCortesGratisClasesAsync(int empresaId,int contextoId,string? search=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null)
    {
        var model=new CortesGratisPageModel{Search=search,Estado=State(estado)};
        var columns=new Dictionary<string,string>{{"codigo","cp.clase_producto"},{"nombre","cp.nombre"},{"estado","cp.estado"}};
        model.Ordenar(sort,dir,columns.Keys,"nombre");model.Paginar(0,page,top);
        if(empresaId<=0||contextoId<=0||search?.Length>150){model.Errores.Add("Revise el contexto y la longitud de búsqueda.");return model;}
        using var conn=factory.CreateConnection();if(await GetContextoReporteAsync(conn,empresaId,contextoId) is null){model.Errores.Add("El contexto de trabajo no está habilitado.");return model;}
        string query=CorteClaseFrom+" AND (@Search IS NULL OR cp.nombre LIKE @Search OR cp.clase_producto LIKE @Search) AND (@Estado IS NULL OR cp.estado=@Estado)";
        var args=new DynamicParameters(new{EmpresaId=empresaId,Search=Search(search),Estado=State(estado)});
        model.Paginar(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*)"+query,args),page,top);args.Add("Offset",(model.Page-1)*model.PageSize);args.Add("Top",model.PageSize);
        model.Items=(await conn.QueryAsync<CorteGratisClaseModel>(CorteClaseSelect+query+$" ORDER BY {columns[model.Sort]} {model.Dir},cp.clase_producto_id OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY",args)).AsList();model.VisibleCount=model.Items.Count;return model;
    }

    public async Task<CortesGratisDetalleModel?> GetCortesGratisClaseAsync(int empresaId,int contextoId,int id)
    {
        if(empresaId<=0||contextoId<=0||id<=0)return null;using var conn=factory.CreateConnection();var scope=await GetContextoReporteAsync(conn,empresaId,contextoId);if(scope is null)return null;
        var data=await conn.QuerySingleOrDefaultAsync<CorteGratisClaseModel>(CorteClaseSelect+CorteClaseFrom+" AND cp.clase_producto_id=@Id",new{EmpresaId=empresaId,Id=id});if(data is null)return null;
        return new(){Datos=data,Sede=scope.Sede,Familias=(await conn.QueryAsync<LookupItemModel>("SELECT familia_producto_id Id,nombre Nombre FROM familia_producto WHERE clase_producto_id=@Id AND estado='A' ORDER BY nombre,familia_producto_id",new{Id=id})).AsList()};
    }

    public async Task<CortesGratisProductoPageModel?> GetCortesGratisProductosAsync(int empresaId,int contextoId,int claseId,int? familiaId=null,string? search=null,int page=1,int top=25,string? sort=null,string? dir=null)
    {
        if(empresaId<=0||contextoId<=0||claseId<=0||familiaId is <=0||search?.Length>150)return null;
        using var conn=factory.CreateConnection();var scope=await GetContextoReporteAsync(conn,empresaId,contextoId);if(scope is null)return null;
        if(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*)"+CorteClaseFrom+" AND cp.clase_producto_id=@ClaseId",new{EmpresaId=empresaId,ClaseId=claseId})!=1)return null;
        if(familiaId.HasValue&&await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM familia_producto WHERE familia_producto_id=@Id AND clase_producto_id=@ClaseId AND estado='A'",new{Id=familiaId,ClaseId=claseId})!=1)return null;
        var model=new CortesGratisProductoPageModel();var columns=new Dictionary<string,string>{{"codigo","p.producto"},{"producto","p.descripcion"},{"familia","fp.nombre"},{"cantidad","cg.cantidad"},{"estado","cg.estado"},{"productoestado","p.estado"}};
        model.Ordenar(sort,dir,columns.Keys,"producto");
        string query=CorteFrom.Replace("{LOCK}","")+" AND (@FamiliaId IS NULL OR p.familia_producto_id=@FamiliaId) AND (@Search IS NULL OR p.producto LIKE @Search OR p.descripcion LIKE @Search OR fp.nombre LIKE @Search)";
        var args=new DynamicParameters(new{EmpresaId=empresaId,SedeId=scope.EmpresaSedeId,ClaseId=claseId,FamiliaId=familiaId,Search=Search(search)});
        model.Paginar(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*)"+query,args),page,top);args.Add("Offset",(model.Page-1)*model.PageSize);args.Add("Top",model.PageSize);
        model.Items=(await conn.QueryAsync<CorteGratisModel>(CorteSelect+query+$" ORDER BY {columns[model.Sort]} {model.Dir},cg.cortes_gratis_id OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY",args)).AsList();model.VisibleCount=model.Items.Count;return model;
    }

    public async Task<(bool success,string message,int id)> SaveCorteGratisAsync(int empresaId,int contextoId,int userId,CorteGratisSolicitud model)
    {
        if(model is null||!Valid(model)||model.ContextoId!=contextoId||empresaId<=0||contextoId<=0||userId<=0||decimal.Round(model.Cantidad!.Value,2)!=model.Cantidad.Value)return Failure("Ingrese una cantidad válida con hasta dos decimales y revise el contexto.");
        using var conn=factory.CreateConnection();if(conn.State!=ConnectionState.Open)conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        var scope=await GetContextoReporteAsync(conn,empresaId,contextoId,tx);if(scope is null)return Failure("El contexto de trabajo no está habilitado.");
        var current=await conn.QuerySingleOrDefaultAsync<CorteGratisModel>(CorteSelect+CorteFrom.Replace("{LOCK}","WITH (UPDLOCK,HOLDLOCK)")+" AND cg.cortes_gratis_id=@Id AND cg.producto_id=@ProductoId",new{EmpresaId=empresaId,SedeId=scope.EmpresaSedeId,model.ClaseId,model.Id,model.ProductoId},tx);
        if(current is null||current.ClaseEstado!="A")return Failure("No se encontró una configuración de la sede y clase activa seleccionadas.");
        if(!string.Equals(current.Revision,model.Revision,StringComparison.Ordinal))return Failure("La configuración cambió. Actualice el detalle antes de guardar.");
        await conn.ExecuteAsync("sp_cortes_gratis_actualizar",new{CortesGratisId=current.Id,Cantidadcortes=model.Cantidad.Value,Estado="A",UsuarioAccionId=userId},tx,commandType:CommandType.StoredProcedure);
        tx.Commit();return(true,"Cantidad de cortes gratis actualizada.",current.Id);
    }
}
