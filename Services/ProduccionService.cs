using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public class ProduccionService(IDbConnectionFactory factory) : IProduccionService
{
    // Processes and subprocesses are shared catalogs in the legacy schema; only operators have a company through personal.
    private const string ProcesoSelect=@"SELECT pp.proce_prod_id Id,pp.proce_prod_id ProcesoId,pp.nombre Nombre,pp.estado Estado,COALESCE(pp.fecha_modifica,pp.fecha_creacion) FechaRevision FROM proce_prod pp {LOCK} ";
    private const string SubSelect=@"SELECT sp.sub_proce_p_id Id,sp.sub_proce_p_id SubProcesoId,sp.proce_prod_id ProcesoId,sp.nombre Nombre,sp.estado Estado,COALESCE(sp.fecha_modifica,sp.fecha_creacion) FechaRevision FROM sub_proce_p sp {LOCK} ";
    private const string PersonalFrom=@" JOIN personal pe ON pe.personal_id=i.personal_id JOIN empresa_sede s ON s.empresa_sede_id=pe.empresa_sede_id JOIN proveedor p ON p.proveedor_id=pe.proveedor_id ";
    private const string OperadorSelect=@"SELECT i.sub_p_p_perso_id Id,sp.proce_prod_id ProcesoId,i.sub_proce_p_id SubProcesoId,pe.personal_id PersonalId,
        RTRIM(pe.personal) Codigo,p.razon_social Nombre,COALESCE(NULLIF(p.numero_ruc,''),p.num_doc_iden,'') Documento,s.nombre Sede,pe.estado EstadoPersonal,
        i.estado Estado,COALESCE(i.fecha_modifica,i.fecha_creacion) FechaRevision FROM sub_p_p_perso i {LOCK}
        JOIN sub_proce_p sp ON sp.sub_proce_p_id=i.sub_proce_p_id "+PersonalFrom;

    private static string Locked(string sql,IDbTransaction? tx)=>sql.Replace("{LOCK}",tx is null?"":"WITH (UPDLOCK,HOLDLOCK)");
    private static async Task<bool> ContextAsync(IDbConnection conn,int empresaId,int contextoId,IDbTransaction? tx=null)
        =>(await ConsultaSedeSql.CargarAsync(conn,empresaId,contextoId,null,tx)).Errores.Count==0;
    private static Task<ProduccionRegistroModel?> ProcesoAsync(IDbConnection conn,IDbTransaction? tx,int id)
        =>conn.QuerySingleOrDefaultAsync<ProduccionRegistroModel>(Locked(ProcesoSelect,tx)+" WHERE pp.proce_prod_id=@Id",new{Id=id},tx);
    private static Task<ProduccionRegistroModel?> SubAsync(IDbConnection conn,IDbTransaction? tx,int procesoId,int id)
        =>conn.QuerySingleOrDefaultAsync<ProduccionRegistroModel>(Locked(SubSelect,tx)+" WHERE sp.proce_prod_id=@ProcesoId AND sp.sub_proce_p_id=@Id",new{ProcesoId=procesoId,Id=id},tx);
    private static bool NamedValid(ProduccionNombreSolicitud request,int contextoId,int userId)
        =>request is not null&&Valid(request)&&request.ContextoId==contextoId&&userId>0&&(request.Id==0||request.Revision?.Length==64)&&Text(request.Nombre).Length>0;

    private static async Task<List<ProduccionRegistroModel>> PageAsync(IDbConnection conn,ProduccionPageModel model,string select,string from,DynamicParameters args,int page,int top,string? sort,string? dir,Dictionary<string,string> columns,string tie)
    {
        model.Ordenar(sort,dir,columns.Keys,"nombre");model.Paginar(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*)"+from,args),page,top);
        args.Add("Offset",(model.Page-1)*model.PageSize);args.Add("Top",model.PageSize);
        var rows=(await conn.QueryAsync<ProduccionRegistroModel>(select+from+" ORDER BY "+columns[model.Sort]+" "+model.Dir+","+tie+" OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY",args)).AsList();
        model.VisibleCount=rows.Count;return rows;
    }

    public async Task<ProduccionPageModel> GetProcesosAsync(int empresaId,int contextoId,string? search=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null)
    {
        var model=new ProduccionPageModel{Search=search,Estado=State(estado)};model.Paginar(0,page,top);
        var columns=new Dictionary<string,string>{{"nombre","pp.nombre"},{"estado","pp.estado"}};model.Ordenar(sort,dir,columns.Keys,"nombre");
        if(empresaId<=0||contextoId<=0||search?.Length>150){model.Errores.Add("Revise el contexto y la longitud de búsqueda.");return model;}
        using var conn=factory.CreateConnection();if(!await ContextAsync(conn,empresaId,contextoId)){model.Errores.Add("El contexto de trabajo no está habilitado.");return model;}
        model.Items=await PageAsync(conn,model,"SELECT pp.proce_prod_id Id,pp.proce_prod_id ProcesoId,pp.nombre Nombre,pp.estado Estado,COALESCE(pp.fecha_modifica,pp.fecha_creacion) FechaRevision",
            " FROM proce_prod pp WHERE (@Search IS NULL OR pp.nombre LIKE @Search) AND (@Estado IS NULL OR pp.estado=@Estado)",new(new{Search=Search(search),Estado=State(estado)}),page,top,sort,dir,columns,"pp.proce_prod_id");return model;
    }

    public async Task<ProduccionProcesoDetalleModel?> GetProcesoAsync(int empresaId,int contextoId,int id)
    {
        if(empresaId<=0||contextoId<=0||id<=0)return null;
        using var conn=factory.CreateConnection();if(!await ContextAsync(conn,empresaId,contextoId))return null;
        var data=await ProcesoAsync(conn,null,id);if(data is null)return null;
        return new(){Datos=data,Subprocesos=(await conn.QueryAsync<LookupItemModel>("SELECT sub_proce_p_id Id,nombre Nombre,estado Codigo FROM sub_proce_p WHERE proce_prod_id=@Id ORDER BY nombre,sub_proce_p_id",new{Id=id})).AsList()};
    }

    public async Task<ProduccionPageModel?> GetDetallesProcesoAsync(int empresaId,int contextoId,int procesoId,string tipo,int subProcesoId=0,string? search=null,int page=1,int top=25,string? sort=null,string? dir=null)
    {
        if(empresaId<=0||contextoId<=0||procesoId<=0||tipo is not ("subprocesos" or "operadores" or "personas")||search?.Length>150)return null;
        using var conn=factory.CreateConnection();if(!await ContextAsync(conn,empresaId,contextoId)||await ProcesoAsync(conn,null,procesoId) is null)return null;
        bool sub=tipo=="subprocesos",candidates=tipo=="personas";
        if(!sub&&(subProcesoId<=0||await SubAsync(conn,null,procesoId,subProcesoId) is null))return null;
        string select,from,tie;
        var columns=new Dictionary<string,string>();
        if(sub)
        {
            select="SELECT sp.sub_proce_p_id Id,sp.sub_proce_p_id SubProcesoId,sp.proce_prod_id ProcesoId,sp.nombre Nombre,sp.estado Estado,COALESCE(sp.fecha_modifica,sp.fecha_creacion) FechaRevision";
            from=" FROM sub_proce_p sp WHERE sp.proce_prod_id=@ProcesoId AND (@Search IS NULL OR sp.nombre LIKE @Search)";tie="sp.sub_proce_p_id";columns=new(){{"nombre","sp.nombre"},{"estado","sp.estado"}};
        }
        else
        {
            string fields="RTRIM(pe.personal) Codigo,p.razon_social Nombre,COALESCE(NULLIF(p.numero_ruc,''),p.num_doc_iden,'') Documento,s.nombre Sede,pe.estado EstadoPersonal";
            select=candidates?"SELECT pe.personal_id Id,pe.personal_id PersonalId,"+fields+",pe.estado Estado,COALESCE(pe.fecha_modifica,pe.fecha_creacion) FechaRevision"
                :"SELECT i.sub_p_p_perso_id Id,sp.proce_prod_id ProcesoId,i.sub_proce_p_id SubProcesoId,pe.personal_id PersonalId,"+fields+",i.estado Estado,COALESCE(i.fecha_modifica,i.fecha_creacion) FechaRevision";
            from=candidates?" FROM personal pe JOIN empresa_sede s ON s.empresa_sede_id=pe.empresa_sede_id JOIN proveedor p ON p.proveedor_id=pe.proveedor_id "
                :" FROM sub_p_p_perso i JOIN sub_proce_p sp ON sp.sub_proce_p_id=i.sub_proce_p_id "+PersonalFrom;
            from+=" WHERE s.empresa_id=@EmpresaId AND p.empresa_id=@EmpresaId ";
            from+=candidates?" AND pe.estado='A' AND p.estado='A' AND s.estado='A' AND NOT EXISTS(SELECT 1 FROM sub_p_p_perso x WHERE x.sub_proce_p_id=@SubId AND x.personal_id=pe.personal_id AND x.estado='A')"
                :" AND i.sub_proce_p_id=@SubId AND sp.proce_prod_id=@ProcesoId AND i.estado='A'";
            from+=" AND (@Search IS NULL OR pe.personal LIKE @Search OR p.razon_social LIKE @Search OR p.num_doc_iden LIKE @Search OR p.numero_ruc LIKE @Search OR s.nombre LIKE @Search)";
            tie=candidates?"pe.personal_id":"i.sub_p_p_perso_id";columns=new(){{"codigo","pe.personal"},{"nombre","p.razon_social"},{"documento","Documento"},{"sede","s.nombre"}};
        }
        var model=new ProduccionPageModel();model.Items=await PageAsync(conn,model,select,from,new(new{EmpresaId=empresaId,ProcesoId=procesoId,SubId=subProcesoId,Search=Search(search)}),page,top,sort,dir,columns,tie);return model;
    }

    public Task<(bool success,string message,int id)> SaveProcesoAsync(int empresaId,int contextoId,int userId,ProduccionNombreSolicitud model)=>SaveNombreAsync(empresaId,contextoId,userId,model,false);
    public Task<(bool success,string message,int id)> SaveSubprocesoAsync(int empresaId,int contextoId,int userId,ProduccionNombreSolicitud model)=>SaveNombreAsync(empresaId,contextoId,userId,model,true);
    private async Task<(bool success,string message,int id)> SaveNombreAsync(int empresaId,int contextoId,int userId,ProduccionNombreSolicitud request,bool sub)
    {
        if(empresaId<=0||contextoId<=0||!NamedValid(request,contextoId,userId)||(!sub&&request.ProcesoId!=request.Id)||(sub&&request.ProcesoId<=0))return Failure("Revise el nombre, registro y contexto de trabajo.");
        using var conn=factory.CreateConnection();if(!await ContextAsync(conn,empresaId,contextoId))return Failure("El contexto no está habilitado.");
        if(request.Id>0){var before=sub?await SubAsync(conn,null,request.ProcesoId,request.Id):await ProcesoAsync(conn,null,request.Id);if(before is null||before.Estado!="A"||before.Revision!=request.Revision)return Failure("El registro cambió. Vuelva a abrirlo antes de guardar.");}
        if(conn.State!=ConnectionState.Open)conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        if(!await LockCompanyAsync(conn,tx,empresaId)||!await ContextAsync(conn,empresaId,contextoId,tx))return Failure("El contexto de trabajo cambió.");
        var parent=request.ProcesoId>0?await ProcesoAsync(conn,tx,request.ProcesoId):null;
        if((sub||request.Id>0)&&(parent is null||parent.Estado!="A"))return Failure("El proceso no está activo.");
        if(request.Id>0){var current=sub?await SubAsync(conn,tx,request.ProcesoId,request.Id):parent;if(current is null||current.Estado!="A"||current.Revision!=request.Revision)return Failure("El registro cambió durante la edición.");}
        var args=new DynamicParameters(new{nombre=Text(request.Nombre),usuarioAccionId=userId});int id=request.Id;
        string prefix=sub?"sp_sub_proce_p":"sp_proce_prod";
        if(id==0){if(sub)args.Add("procesoProduccionId",request.ProcesoId);id=await InsertAsync(conn,tx,prefix+"_insertar",args);}
        else{args.Add(sub?"subProcesoProduccionId":"procesoProduccionId",id);await conn.ExecuteAsync(prefix+"_actualizar",args,tx,commandType:CommandType.StoredProcedure);}
        tx.Commit();return(true,sub?"Subproceso guardado.":"Proceso guardado.",id);
    }

    public async Task<(bool success,string message,int id)> AnularProcesoAsync(int empresaId,int contextoId,int userId,ProduccionEstadoSolicitud request)
    {
        if(request is null||!Valid(request)||request.ContextoId!=contextoId||request.Id!=request.ProcesoId||request.SubProcesoId!=0||empresaId<=0||userId<=0)return Failure("Revise el proceso seleccionado.");
        using var conn=factory.CreateConnection();if(!await ContextAsync(conn,empresaId,contextoId))return Failure("El contexto no está habilitado.");
        var before=await ProcesoAsync(conn,null,request.Id);if(before is null||before.Estado!="A"||before.Revision!=request.Revision)return Failure("El proceso cambió o ya está anulado.");
        if(conn.State!=ConnectionState.Open)conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        if(!await LockCompanyAsync(conn,tx,empresaId)||!await ContextAsync(conn,empresaId,contextoId,tx))return Failure("El contexto cambió.");
        var current=await ProcesoAsync(conn,tx,request.Id);if(current is null||current.Estado!="A"||current.Revision!=request.Revision)return Failure("El proceso cambió durante la preparación.");
        await conn.ExecuteAsync("sp_proce_prod_eliminar",new{procesoProduccionId=request.Id,usuarioAccionId=userId},tx,commandType:CommandType.StoredProcedure);
        tx.Commit();return(true,"Proceso anulado.",request.Id);
    }

    public async Task<(bool success,string message,int id)> AgregarOperadorAsync(int empresaId,int contextoId,int userId,ProduccionOperadorSolicitud request)
    {
        if(request is null||!Valid(request)||request.ContextoId!=contextoId||empresaId<=0||userId<=0)return Failure("Seleccione un subproceso y una persona válidos.");
        using var conn=factory.CreateConnection();if(!await ContextAsync(conn,empresaId,contextoId))return Failure("El contexto no está habilitado.");
        if(conn.State!=ConnectionState.Open)conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        if(!await LockCompanyAsync(conn,tx,empresaId)||!await ContextAsync(conn,empresaId,contextoId,tx))return Failure("El contexto cambió.");
        var parent=await ProcesoAsync(conn,tx,request.ProcesoId);var sub=await SubAsync(conn,tx,request.ProcesoId,request.SubProcesoId);
        if(parent?.Estado!="A"||sub?.Estado!="A")return Failure("El proceso o subproceso no está activo.");
        if(await conn.ExecuteScalarAsync<int>(@"SELECT COUNT(*) FROM personal pe WITH (UPDLOCK,HOLDLOCK) JOIN empresa_sede s ON s.empresa_sede_id=pe.empresa_sede_id JOIN proveedor p ON p.proveedor_id=pe.proveedor_id
            WHERE pe.personal_id=@Id AND s.empresa_id=@EmpresaId AND p.empresa_id=@EmpresaId AND pe.estado='A' AND s.estado='A' AND p.estado='A'",new{Id=request.PersonalId,EmpresaId=empresaId},tx)!=1)return Failure("Seleccione personal activo de la empresa actual.");
        if(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sub_p_p_perso WITH (UPDLOCK,HOLDLOCK) WHERE sub_proce_p_id=@SubId AND personal_id=@PersonId AND estado='A'",new{SubId=request.SubProcesoId,PersonId=request.PersonalId},tx)>0)return Failure("La persona ya está asignada a este subproceso.");
        int id=await InsertAsync(conn,tx,"sp_sub_p_p_perso_insertar",new{subProcesoProduccionId=request.SubProcesoId,personalId=request.PersonalId,usuarioAccionId=userId});
        tx.Commit();return(true,"Operador asignado.",id);
    }

    public async Task<(bool success,string message,int id)> QuitarOperadorAsync(int empresaId,int contextoId,int userId,ProduccionEstadoSolicitud request)
    {
        if(request is null||!Valid(request)||request.ContextoId!=contextoId||request.SubProcesoId<=0||empresaId<=0||userId<=0)return Failure("Revise el operador seleccionado.");
        using var conn=factory.CreateConnection();if(!await ContextAsync(conn,empresaId,contextoId))return Failure("El contexto no está habilitado.");
        if(conn.State!=ConnectionState.Open)conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        if(!await LockCompanyAsync(conn,tx,empresaId)||!await ContextAsync(conn,empresaId,contextoId,tx))return Failure("El contexto cambió.");
        var parent=await ProcesoAsync(conn,tx,request.ProcesoId);var sub=await SubAsync(conn,tx,request.ProcesoId,request.SubProcesoId);
        if(parent?.Estado!="A"||sub?.Estado!="A")return Failure("El proceso o subproceso no está activo.");
        var current=await conn.QuerySingleOrDefaultAsync<ProduccionRegistroModel>(Locked(OperadorSelect,tx)+@" WHERE i.sub_p_p_perso_id=@Id AND i.sub_proce_p_id=@SubId AND sp.proce_prod_id=@ProcesoId
            AND s.empresa_id=@EmpresaId AND p.empresa_id=@EmpresaId AND i.estado='A'",new{request.Id,SubId=request.SubProcesoId,request.ProcesoId,EmpresaId=empresaId},tx);
        if(current is null||current.Revision!=request.Revision)return Failure("La asignación cambió o no pertenece a la empresa actual.");
        await conn.ExecuteAsync("sp_sub_p_p_perso_eliminar",new{subProcesoProduccionPersonalId=request.Id,usuarioAccionId=userId},tx,commandType:CommandType.StoredProcedure);
        tx.Commit();return(true,"Operador retirado del subproceso.",request.Id);
    }
}
