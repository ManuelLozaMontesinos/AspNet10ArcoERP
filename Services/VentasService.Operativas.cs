using System.ComponentModel.DataAnnotations;
using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class VentasService
{
    private const string CajaSelect = @"SELECT c.caja_id Id,c.empresa_sede_id EmpresaSedeId,s.nombre Sede,c.nombre Nombre,c.estado Estado,
        COALESCE(c.fecha_modifica,c.fecha_creacion) FechaRevision ";
    private const string CajaFrom = " FROM caja c {LOCK} JOIN empresa_sede s ON s.empresa_sede_id=c.empresa_sede_id ";
    private const string CajaWhere = " WHERE s.empresa_id=@EmpresaId AND c.empresa_sede_id=@SedeId ";
    private const string VendedorSelect = @"SELECT v.vendedor_id Id,RTRIM(v.vendedor) Codigo,v.proveedor_id ProveedorId,p.razon_social Nombre,
        COALESCE(NULLIF(p.numero_ruc,''),p.num_doc_iden,'') Documento,v.abrev Abreviatura,v.servidor_imp ServidorImpresion,v.estado Estado,
        COALESCE(v.fecha_modifica,v.fecha_creacion) FechaRevision ";
    private const string VendedorFrom = " FROM vendedor v {LOCK} JOIN proveedor p ON p.proveedor_id=v.proveedor_id ";
    private const string VendedorWhere = " WHERE p.empresa_id=@EmpresaId ";

    private static async Task<List<T>> PageVentaAsync<T>(IDbConnection conn,ListadoPaginadoModel model,string select,string from,DynamicParameters args,
        int page,int top,string? sort,string? dir,Dictionary<string,string> columns,string defaultSort,string tie)
    {
        model.Ordenar(sort,dir,columns.Keys,defaultSort);
        model.Paginar(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) "+from,args),page,top);
        args.Add("Offset",(model.Page-1)*model.PageSize);args.Add("Top",model.PageSize);
        var rows=(await conn.QueryAsync<T>(select+from+$" ORDER BY {columns[model.Sort]} {model.Dir},{tie} OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY",args)).AsList();
        model.VisibleCount=rows.Count;return rows;
    }

    public async Task<CajaPageModel> GetCajasAsync(int empresaId,int sedeId,string? search=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null)
    {
        var model=new CajaPageModel{Search=search,Estado=State(estado)};var columns=new Dictionary<string,string>{{"nombre","c.nombre"},{"estado","c.estado"}};
        model.Ordenar(sort,dir,columns.Keys,"nombre");model.Paginar(0,page,top);if(empresaId<=0||sedeId<=0)return model;
        using var conn=factory.CreateConnection();
        var args=new DynamicParameters(new{EmpresaId=empresaId,SedeId=sedeId,Search=Search(search),Estado=State(estado)});
        model.Items=await PageVentaAsync<CajaModel>(conn,model,CajaSelect,CajaFrom.Replace("{LOCK}","")+CajaWhere+" AND (@Search IS NULL OR c.nombre LIKE @Search) AND (@Estado IS NULL OR c.estado=@Estado)",args,page,top,sort,dir,columns,"nombre","c.caja_id");
        model.Filtros=await OperatingFiltersAsync(conn,empresaId,false);return model;
    }
    public async Task<CajaModel?> GetCajaAsync(int empresaId,int sedeId,int id)
    {if(empresaId<=0||sedeId<=0||id<=0)return null;using var conn=factory.CreateConnection();return await CajaAsync(conn,null,empresaId,sedeId,id);}
    private static Task<CajaModel?> CajaAsync(IDbConnection conn,IDbTransaction? tx,int empresaId,int sedeId,int id)
        =>conn.QuerySingleOrDefaultAsync<CajaModel>(CajaSelect+CajaFrom.Replace("{LOCK}",tx is null?"":"WITH (UPDLOCK,HOLDLOCK)")+CajaWhere+" AND c.caja_id=@Id",new{EmpresaId=empresaId,SedeId=sedeId,Id=id},tx);

    public async Task<(bool success,string message,int id)> SaveCajaAsync(int empresaId,int sedeId,int userId,MaestroEdicion<CajaModel> request)
    {
        if(request?.Datos is null)return Failure("Revise los datos de la caja.");
        var model=request.Datos;model.Nombre=Text(model.Nombre);
        if(empresaId<=0||sedeId<=0||userId<=0||!Valid(request)||!Valid(model))return Failure("Revise el nombre de la caja.");
        if(model.Id>0){var before=await GetCajaAsync(empresaId,sedeId,model.Id);if(before is null||before.Estado!="A"||before.Revision!=request.Revision)return Failure("La caja cambió o no pertenece a la sede seleccionada.");}
        using var conn=factory.CreateConnection();if(conn.State!=ConnectionState.Open)conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        if(!await LockCompanyAsync(conn,tx,empresaId))return Failure("La empresa no está habilitada.");
        if(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM empresa_sede WITH (UPDLOCK,HOLDLOCK) WHERE empresa_sede_id=@SedeId AND empresa_id=@EmpresaId AND estado='A'",new{EmpresaId=empresaId,SedeId=sedeId},tx)!=1)return Failure("La sede seleccionada no está habilitada.");
        if(model.Id>0){var old=await CajaAsync(conn,tx,empresaId,sedeId,model.Id);if(old is null||old.Estado!="A"||old.Revision!=request.Revision)return Failure("La caja cambió. Actualice el listado.");}
        var args=new DynamicParameters(new{model.Nombre,UsuarioAccionId=userId});int id=model.Id;
        if(id==0){args.Add("EmpresaSedeId",sedeId);id=await InsertAsync(conn,tx,"sp_caja_insertar",args);}
        else{args.Add("CajaId",id);await conn.ExecuteAsync("sp_caja_actualizar",args,tx,commandType:CommandType.StoredProcedure);}
        tx.Commit();return(true,"Caja guardada.",id);
    }
    public async Task<(bool success,string message,int id)> AnularCajaAsync(int empresaId,int sedeId,int userId,VentaEstadoSolicitud request)
    {
        if(request is null||!Valid(request)||request.Activar||userId<=0)return Failure("Revise la caja seleccionada.");
        var before=await GetCajaAsync(empresaId,sedeId,request.Id);if(before is null||before.Estado!="A"||before.Revision!=request.Revision)return Failure("La caja cambió o no está disponible.");
        using var conn=factory.CreateConnection();if(conn.State!=ConnectionState.Open)conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        if(!await LockCompanyAsync(conn,tx,empresaId))return Failure("La empresa no está habilitada.");
        var old=await CajaAsync(conn,tx,empresaId,sedeId,request.Id);if(old is null||old.Estado!="A"||old.Revision!=request.Revision)return Failure("La caja cambió. Actualice el listado.");
        await conn.ExecuteAsync("sp_caja_eliminar",new{CajaId=request.Id,UsuarioAccionId=userId},tx,commandType:CommandType.StoredProcedure);
        tx.Commit();return(true,"Caja anulada.",request.Id);
    }

    public async Task<VendedorPageModel> GetVendedoresAsync(int empresaId,string? search=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null)
    {
        var model=new VendedorPageModel{Search=search,Estado=State(estado)};
        var columns=new Dictionary<string,string>{{"codigo","v.vendedor"},{"nombre","p.razon_social"},{"abreviatura","v.abrev"},{"impresion","v.servidor_imp"},{"estado","v.estado"}};
        model.Ordenar(sort,dir,columns.Keys,"nombre");model.Paginar(0,page,top);if(empresaId<=0)return model;
        using var conn=factory.CreateConnection();var args=new DynamicParameters(new{EmpresaId=empresaId,Search=Search(search),Estado=State(estado)});
        model.Items=await PageVentaAsync<VendedorModel>(conn,model,VendedorSelect,VendedorFrom.Replace("{LOCK}","")+VendedorWhere+@" AND (@Search IS NULL OR v.vendedor LIKE @Search OR p.razon_social LIKE @Search OR v.abrev LIKE @Search OR p.numero_ruc LIKE @Search OR p.num_doc_iden LIKE @Search) AND (@Estado IS NULL OR v.estado=@Estado)",args,page,top,sort,dir,columns,"nombre","v.vendedor_id");
        model.Filtros=await OperatingFiltersAsync(conn,empresaId,true);return model;
    }
    public async Task<VendedorModel?> GetVendedorAsync(int empresaId,int id)
    {if(empresaId<=0||id<=0)return null;using var conn=factory.CreateConnection();return await VendedorAsync(conn,null,empresaId,id);}
    private static Task<VendedorModel?> VendedorAsync(IDbConnection conn,IDbTransaction? tx,int empresaId,int id)
        =>conn.QuerySingleOrDefaultAsync<VendedorModel>(VendedorSelect+VendedorFrom.Replace("{LOCK}",tx is null?"":"WITH (UPDLOCK,HOLDLOCK)")+VendedorWhere+" AND v.vendedor_id=@Id",new{EmpresaId=empresaId,Id=id},tx);

    public async Task<(bool success,string message,int id)> SaveVendedorAsync(int empresaId,int userId,MaestroEdicion<VendedorModel> request)
    {
        if(request?.Datos is null)return Failure("Revise los datos del vendedor.");var model=request.Datos;
        model.Abreviatura=Text(model.Abreviatura);model.ServidorImpresion=Text(model.ServidorImpresion);
        if(empresaId<=0||userId<=0||!Valid(request)||!Valid(model))return Failure("Seleccione personal y revise el nombre corto y servidor de impresión.");
        if(model.Id>0){var before=await GetVendedorAsync(empresaId,model.Id);if(before is null||before.Estado!="A"||before.Revision!=request.Revision)return Failure("El vendedor cambió o no está disponible.");}
        using var conn=factory.CreateConnection();if(conn.State!=ConnectionState.Open)conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        if(!await LockCompanyAsync(conn,tx,empresaId))return Failure("La empresa no está habilitada.");
        var old=model.Id>0?await VendedorAsync(conn,tx,empresaId,model.Id):null;
        if(model.Id>0&&(old is null||old.Estado!="A"||old.Revision!=request.Revision))return Failure("El vendedor cambió. Actualice el listado.");
        if(old is not null&&old.ProveedorId!=model.ProveedorId)return Failure("La persona asignada al vendedor no se modifica después del alta.");
        var args=new DynamicParameters(new{model.Abreviatura,model.ServidorImpresion,UsuarioAccionId=userId});int id=model.Id;
        if(id==0)
        {
            if(await conn.ExecuteScalarAsync<int>(PersonalProveedorCount,new{EmpresaId=empresaId,Id=model.ProveedorId},tx)!=1)return Failure("Seleccione personal activo de la empresa.");
            if(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM vendedor WITH (UPDLOCK,HOLDLOCK) WHERE proveedor_id=@Id",new{Id=model.ProveedorId},tx)>0)return Failure("La persona ya tiene un vendedor registrado. Si está anulado, utilice Activar.");
            string code;try{code=await NextCodeAsync(conn,tx,empresaId,userId,"0013");}catch(ValidationException ex){return Failure(ex.Message);}
            args.Add("Vendedor",code);args.Add("ProveedorId",model.ProveedorId);id=await InsertAsync(conn,tx,"sp_vendedor_insertar",args);
        }
        else{args.Add("VendedorId",id);args.Add("Estado","A");await conn.ExecuteAsync("sp_vendedor_actualizar",args,tx,commandType:CommandType.StoredProcedure);}
        tx.Commit();return(true,"Vendedor guardado.",id);
    }
    public async Task<(bool success,string message,int id)> CambiarEstadoVendedorAsync(int empresaId,int userId,VentaEstadoSolicitud request)
    {
        if(request is null||!Valid(request)||userId<=0)return Failure("Revise el vendedor seleccionado.");
        string expected=request.Activar?"I":"A";var before=await GetVendedorAsync(empresaId,request.Id);
        if(before is null||before.Estado!=expected||before.Revision!=request.Revision)return Failure("El vendedor cambió o no está disponible.");
        using var conn=factory.CreateConnection();if(conn.State!=ConnectionState.Open)conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        if(!await LockCompanyAsync(conn,tx,empresaId))return Failure("La empresa no está habilitada.");
        var old=await VendedorAsync(conn,tx,empresaId,request.Id);if(old is null||old.Estado!=expected||old.Revision!=request.Revision)return Failure("El vendedor cambió. Actualice el listado.");
        if(request.Activar)await conn.ExecuteAsync("sp_vendedor_actualizar",new{VendedorId=old.Id,Abreviatura=old.Abreviatura??"",ServidorImpresion=old.ServidorImpresion??"",Estado="A",UsuarioAccionId=userId},tx,commandType:CommandType.StoredProcedure);
        else await conn.ExecuteAsync("sp_vendedor_eliminar",new{VendedorId=old.Id,UsuarioAccionId=userId},tx,commandType:CommandType.StoredProcedure);
        tx.Commit();return(true,request.Activar?"Vendedor activado.":"Vendedor anulado.",old.Id);
    }
    private const string PersonalProveedorCount=@"SELECT COUNT(*) FROM proveedor p WHERE p.proveedor_id=@Id AND p.empresa_id=@EmpresaId AND p.estado='A'
        AND EXISTS(SELECT 1 FROM personal pe JOIN empresa_sede ps ON ps.empresa_sede_id=pe.empresa_sede_id WHERE pe.proveedor_id=p.proveedor_id AND ps.empresa_id=@EmpresaId AND pe.estado='A')";

    private static async Task<VentasOperativasFiltrosModel> OperatingFiltersAsync(IDbConnection conn,int empresaId,bool vendedor)
    {
        var filters=new VentasOperativasFiltrosModel();
        filters.Sedes=(await conn.QueryAsync<LookupItemModel>("SELECT empresa_sede_id Id,nombre Nombre FROM empresa_sede WHERE empresa_id=@EmpresaId AND estado='A' ORDER BY nombre,empresa_sede_id",new{EmpresaId=empresaId})).AsList();
        if(vendedor)
        {
            filters.TiposDocumento=(await conn.QueryAsync<LookupItemModel>("SELECT tipo_doc_com_id Id,nombre Nombre FROM tipo_doc_com WHERE estado='A' ORDER BY nombre,tipo_doc_com_id")).AsList();
            filters.Monedas=(await conn.QueryAsync<LookupItemModel>("SELECT moneda_id Id,nombre Nombre FROM moneda WHERE estado='A' ORDER BY nombre,moneda_id")).AsList();
        }
        return filters;
    }
    public async Task<AsignacionVentaPageModel> GetPersonalVendedorAsync(int empresaId,string? search=null,int page=1,int top=25,string? sort=null,string? dir=null,int? filtroSedeId=null)
    {
        var model=new AsignacionVentaPageModel();var columns=new Dictionary<string,string>{{"codigo","p.proveedor"},{"nombre","p.razon_social"},{"documento","Documento"},{"estado","p.estado"}};
        model.Ordenar(sort,dir,columns.Keys,"nombre");model.Paginar(0,page,top);if(empresaId<=0)return model;
        using var conn=factory.CreateConnection();
        const string fields=@"SELECT p.proveedor_id Id,p.proveedor_id DestinoId,RTRIM(p.proveedor) Codigo,p.razon_social Nombre,COALESCE(NULLIF(p.numero_ruc,''),p.num_doc_iden,'') Documento,p.estado Estado ";
        const string from=@" FROM proveedor p WHERE p.empresa_id=@EmpresaId AND p.estado='A'
            AND EXISTS(SELECT 1 FROM personal pe JOIN empresa_sede ps ON ps.empresa_sede_id=pe.empresa_sede_id WHERE pe.proveedor_id=p.proveedor_id AND ps.empresa_id=@EmpresaId AND pe.estado='A' AND (@FilterSite IS NULL OR pe.empresa_sede_id=@FilterSite))
            AND NOT EXISTS(SELECT 1 FROM vendedor v WHERE v.proveedor_id=p.proveedor_id)
            AND (@Search IS NULL OR p.proveedor LIKE @Search OR p.razon_social LIKE @Search OR p.numero_ruc LIKE @Search OR p.num_doc_iden LIKE @Search)";
        model.Items=await PageVentaAsync<AsignacionVentaModel>(conn,model,fields,from,new(new{EmpresaId=empresaId,Search=Search(search),FilterSite=filtroSedeId}),page,top,sort,dir,columns,"nombre","p.proveedor_id");return model;
    }
}
