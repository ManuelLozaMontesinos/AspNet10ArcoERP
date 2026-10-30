using System.ComponentModel.DataAnnotations;
using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class VentasService(IDbConnectionFactory factory) : IVentasService
{
    private const string MedioSelect=@"SELECT mp.medio_pago_id Id,RTRIM(mp.medio_pago) Codigo,mp.nombre Nombre,mp.abrev Abreviatura,
        mp.porcen_tarj PorcentajeTarjeta,mp.estado Estado,COALESCE(mp.fecha_modifica,mp.fecha_creacion) FechaRevision FROM medio_pago mp ";
    private const string GrupoSelect=@"SELECT g.grupo_prove_id Id,g.nombre Nombre,CAST(CASE WHEN g.ignora_inc_fracc='S' THEN 1 ELSE 0 END AS bit) IgnorarIncrementoFraccion,
        g.estado Estado,COALESCE(g.fecha_modifica,g.fecha_creacion) FechaRevision FROM grupo_prove g ";

    public async Task<MedioPagoPageModel> GetMediosPagoAsync(int empresaId,string? search=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null)
    {
        var model=new MedioPagoPageModel{Search=search,Estado=State(estado)};
        var columns=new Dictionary<string,string>{["codigo"]="mp.medio_pago",["nombre"]="mp.nombre",["abreviatura"]="mp.abrev",["comision"]="mp.porcen_tarj",["estado"]="mp.estado"};
        model.Ordenar(sort,dir,columns.Keys,"codigo"); model.Paginar(0,page,top); if(empresaId<=0)return model;
        using var conn=factory.CreateConnection();
        const string where="WHERE mp.empresa_id=@EmpresaId AND (@Search IS NULL OR mp.nombre LIKE @Search OR mp.medio_pago LIKE @Search OR mp.abrev LIKE @Search) AND (@Estado IS NULL OR mp.estado=@Estado)";
        var args=new DynamicParameters(new{EmpresaId=empresaId,Search=Search(search),Estado=State(estado)});
        model.Paginar(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM medio_pago mp "+where,args),page,top); args.Add("Offset",(model.Page-1)*model.PageSize);args.Add("Top",model.PageSize);
        model.Items=(await conn.QueryAsync<MedioPagoModel>(MedioSelect+where+$" ORDER BY {columns[model.Sort]} {model.Dir},mp.medio_pago_id OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY",args)).AsList();model.VisibleCount=model.Items.Count;return model;
    }
    public async Task<MedioPagoModel?> GetMedioPagoAsync(int empresaId,int id)
    {
        if(empresaId<=0||id<=0)return null;using var conn=factory.CreateConnection();return await MedioAsync(conn,null,empresaId,id);
    }
    private static Task<MedioPagoModel?> MedioAsync(IDbConnection conn,IDbTransaction? tx,int empresaId,int id)
        =>conn.QuerySingleOrDefaultAsync<MedioPagoModel>(MedioSelect+(tx is null?"":"WITH (UPDLOCK,HOLDLOCK) ")+"WHERE mp.empresa_id=@EmpresaId AND mp.medio_pago_id=@Id",new{EmpresaId=empresaId,Id=id},tx);
    public async Task<(bool success,string message,int id)> SaveMedioPagoAsync(int empresaId,int userId,MaestroEdicion<MedioPagoModel> request)
    {
        if(request?.Datos is null)return Failure("Revise los datos del medio de pago.");
        var model=request.Datos;model.Nombre=Text(model.Nombre);model.Abreviatura=Text(model.Abreviatura);
        if(empresaId<=0||userId<=0||!Valid(request)||!Valid(model)||decimal.Round(model.PorcentajeTarjeta,4)!=model.PorcentajeTarjeta)return Failure("Revise el nombre, abreviatura y comisión no negativa de hasta cuatro decimales.");
        if(model.Id>0){var before=await GetMedioPagoAsync(empresaId,model.Id);if(before is null||before.Estado!="A"||before.Revision!=request.Revision)return Failure("El medio de pago cambió o no está disponible. Actualice el listado.");}
        using var conn=factory.CreateConnection();if(conn.State!=ConnectionState.Open)conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        if(!await LockCompanyAsync(conn,tx,empresaId))return Failure("La empresa no está habilitada.");
        if(model.Id>0){var old=await MedioAsync(conn,tx,empresaId,model.Id);if(old is null||old.Estado!="A"||old.Revision!=request.Revision)return Failure("El medio de pago cambió. Actualice el listado.");}
        int id=model.Id;var args=new DynamicParameters(new{model.Nombre,PorcentajeTarjeta=model.PorcentajeTarjeta,model.Abreviatura,UsuarioAccionId=userId});
        if(id==0)
        {
            string code;try{code=await NextCodeAsync(conn,tx,empresaId,userId,"0016");}catch(ValidationException ex){return Failure(ex.Message);}
            args.Add("EmpresaId",empresaId);args.Add("MedioPago",code);id=await InsertAsync(conn,tx,"sp_medio_pago_insertar",args);
        }
        else{args.Add("MedioPagoId",id);await conn.ExecuteAsync("sp_medio_pago_actualizar",args,tx,commandType:CommandType.StoredProcedure);}
        tx.Commit();return(true,"Medio de pago guardado.",id);
    }
    public async Task<(bool success,string message,int id)> AnularMedioPagoAsync(int empresaId,int userId,int id,string revision)
    {
        var before=await GetMedioPagoAsync(empresaId,id);if(userId<=0||before is null||before.Estado!="A"||before.Revision!=revision)return Failure("El medio de pago no está disponible o cambió.");
        using var conn=factory.CreateConnection();if(conn.State!=ConnectionState.Open)conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        if(!await LockCompanyAsync(conn,tx,empresaId))return Failure("La empresa no está habilitada.");
        var old=await MedioAsync(conn,tx,empresaId,id);if(old is null||old.Estado!="A"||old.Revision!=revision)return Failure("El medio de pago cambió. Actualice el listado.");
        await conn.ExecuteAsync("sp_medio_pago_eliminar",new{MedioPagoId=id,UsuarioAccionId=userId},tx,commandType:CommandType.StoredProcedure);tx.Commit();return(true,"Medio de pago anulado.",id);
    }

    public async Task<GrupoClientePageModel> GetGruposClienteAsync(int empresaId,string? search=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null)
    {
        var model=new GrupoClientePageModel{Search=search,Estado=State(estado)};
        var columns=new Dictionary<string,string>{["nombre"]="g.nombre",["fraccion"]="g.ignora_inc_fracc",["estado"]="g.estado"};model.Ordenar(sort,dir,columns.Keys,"nombre");model.Paginar(0,page,top);if(empresaId<=0)return model;
        using var conn=factory.CreateConnection();const string where="WHERE g.empresa_id=@EmpresaId AND (@Search IS NULL OR g.nombre LIKE @Search) AND (@Estado IS NULL OR g.estado=@Estado)";
        var args=new DynamicParameters(new{EmpresaId=empresaId,Search=Search(search),Estado=State(estado)});model.Paginar(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM grupo_prove g "+where,args),page,top);args.Add("Offset",(model.Page-1)*model.PageSize);args.Add("Top",model.PageSize);
        model.Items=(await conn.QueryAsync<GrupoClienteModel>(GrupoSelect+where+$" ORDER BY {columns[model.Sort]} {model.Dir},g.grupo_prove_id OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY",args)).AsList();model.VisibleCount=model.Items.Count;return model;
    }
    public async Task<GrupoClienteModel?> GetGrupoClienteAsync(int empresaId,int id)
    {if(empresaId<=0||id<=0)return null;using var conn=factory.CreateConnection();return await GrupoAsync(conn,null,empresaId,id);}
    private static Task<GrupoClienteModel?> GrupoAsync(IDbConnection conn,IDbTransaction? tx,int empresaId,int id)
        =>conn.QuerySingleOrDefaultAsync<GrupoClienteModel>(GrupoSelect+(tx is null?"":"WITH (UPDLOCK,HOLDLOCK) ")+"WHERE g.empresa_id=@EmpresaId AND g.grupo_prove_id=@Id",new{EmpresaId=empresaId,Id=id},tx);
    public async Task<(bool success,string message,int id)> SaveGrupoClienteAsync(int empresaId,int userId,MaestroEdicion<GrupoClienteModel> request)
    {
        if(request?.Datos is null)return Failure("Revise los datos del grupo.");
        var model=request.Datos;model.Nombre=Text(model.Nombre);if(empresaId<=0||userId<=0||!Valid(request)||!Valid(model))return Failure("Revise el nombre del grupo.");
        if(model.Id>0){var before=await GetGrupoClienteAsync(empresaId,model.Id);if(before is null||before.Estado!="A"||before.Revision!=request.Revision)return Failure("El grupo cambió o no está disponible.");}
        using var conn=factory.CreateConnection();if(conn.State!=ConnectionState.Open)conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);if(!await LockCompanyAsync(conn,tx,empresaId))return Failure("La empresa no está habilitada.");
        if(model.Id>0){var old=await GrupoAsync(conn,tx,empresaId,model.Id);if(old is null||old.Estado!="A"||old.Revision!=request.Revision)return Failure("El grupo cambió. Actualice el listado.");}
        var args=new DynamicParameters(new{model.Nombre,IgnorarIncrementoFraccion=model.IgnorarIncrementoFraccion?"S":"N",UsuarioAccionId=userId});int id=model.Id;
        if(id==0){args.Add("EmpresaId",empresaId);id=await InsertAsync(conn,tx,"sp_grupo_prove_insertar",args);}else{args.Add("GrupoProveedorId",id);await conn.ExecuteAsync("sp_grupo_prove_actualizar",args,tx,commandType:CommandType.StoredProcedure);}
        tx.Commit();return(true,"Grupo guardado.",id);
    }
    public async Task<(bool success,string message,int id)> AnularGrupoClienteAsync(int empresaId,int userId,int id,string revision)
    {
        var before=await GetGrupoClienteAsync(empresaId,id);if(userId<=0||before is null||before.Estado!="A"||before.Revision!=revision)return Failure("El grupo no está disponible o cambió.");
        using var conn=factory.CreateConnection();if(conn.State!=ConnectionState.Open)conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);if(!await LockCompanyAsync(conn,tx,empresaId))return Failure("La empresa no está habilitada.");
        var old=await GrupoAsync(conn,tx,empresaId,id);if(old is null||old.Estado!="A"||old.Revision!=revision)return Failure("El grupo cambió. Actualice el listado.");
        await conn.ExecuteAsync("sp_grupo_prove_eliminar",new{GrupoProveedorId=id,UsuarioAccionId=userId},tx,commandType:CommandType.StoredProcedure);tx.Commit();return(true,"Grupo anulado.",id);
    }
}
