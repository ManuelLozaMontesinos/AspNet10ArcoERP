using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class VentasService
{
    // Closed descriptors: route/form values are never used as SQL identifiers or procedure names.
    private sealed record AsignacionSpec(string Table,string Id,string Owner,string Target,string From,string TargetId,string Fields,string Search,string Eligible,Dictionary<string,string> Columns);
    private static AsignacionSpec? Assignment(string tipo) => tipo switch
    {
        "personal" => new("caja_perso","caja_perso_id","caja_id","personal_id",
            "personal x JOIN empresa_sede s ON s.empresa_sede_id=x.empresa_sede_id JOIN proveedor p ON p.proveedor_id=x.proveedor_id", "personal_id",
            "RTRIM(x.personal) Codigo,p.razon_social Nombre,COALESCE(NULLIF(p.numero_ruc,''),p.num_doc_iden,'') Documento,s.nombre Sede",
            "x.personal LIKE @Search OR p.razon_social LIKE @Search OR p.numero_ruc LIKE @Search OR p.num_doc_iden LIKE @Search OR s.nombre LIKE @Search",
            "x.estado='A' AND s.estado='A' AND p.estado='A' AND p.empresa_id=@EmpresaId",
            new(){{"codigo","x.personal"},{"nombre","p.razon_social"},{"documento","Documento"},{"sede","s.nombre"}}),
        "series" => new("vend_serie","vend_serie_id","vendedor_id","doc_com_serie_id",
            "doc_com_serie x JOIN empresa_sede s ON s.empresa_sede_id=x.empresa_sede_id JOIN tipo_doc_com t ON t.tipo_doc_com_id=x.tipo_doc_com_id LEFT JOIN almacen a ON a.almacen_id=x.almacen_id AND a.empresa_sede_id=x.empresa_sede_id", "doc_com_serie_id",
            "RTRIM(x.serie) Codigo,t.nombre Nombre,s.nombre Sede,ISNULL(a.nombre,'') Almacen,x.correlativo Correlativo",
            "x.serie LIKE @Search OR t.nombre LIKE @Search OR s.nombre LIKE @Search OR a.nombre LIKE @Search",
            "x.estado='A' AND s.estado='A' AND t.estado='A'",
            new(){{"sede","s.nombre"},{"almacen","a.nombre"},{"nombre","t.nombre"},{"codigo","x.serie"},{"correlativo","x.correlativo"}}),
        "listas" => new("lista_p_vendedor","lista_p_vendedor_id","vendedor_id","lista_precio_id",
            "lista_precio x JOIN empresa_sede s ON s.empresa_sede_id=x.empresa_sede_id JOIN moneda m ON m.moneda_id=x.moneda_id", "lista_precio_id",
            "x.nombre Nombre,s.nombre Sede,m.nombre Moneda",
            "x.nombre LIKE @Search OR s.nombre LIKE @Search OR m.nombre LIKE @Search",
            "x.estado='A' AND s.estado='A'",
            new(){{"sede","s.nombre"},{"nombre","x.nombre"},{"moneda","m.nombre"}}),
        _ => null
    };
    private static async Task<MaestroEmpresaModel?> AssignmentOwnerAsync(IDbConnection conn,IDbTransaction? tx,int empresaId,int sedeId,int ownerId,string tipo)
        =>tipo=="personal"?await CajaAsync(conn,tx,empresaId,sedeId,ownerId):await VendedorAsync(conn,tx,empresaId,ownerId);
    private static string AssignmentCompany(string tipo) => "s.empresa_id=@EmpresaId"+(tipo=="personal"?" AND p.empresa_id=@EmpresaId":"");

    public async Task<AsignacionVentaPageModel?> GetAsignacionesVentaAsync(int empresaId,int sedeId,int ownerId,string tipo,bool candidatos=false,string? search=null,int page=1,int top=25,string? sort=null,string? dir=null,int? filtroSedeId=null,int? monedaId=null,int? tipoDocumentoId=null)
    {
        var spec=Assignment(tipo);if(spec is null||empresaId<=0||sedeId<=0||ownerId<=0||search?.Length>150)return null;
        using var conn=factory.CreateConnection();if(await AssignmentOwnerAsync(conn,null,empresaId,sedeId,ownerId,tipo) is null)return null;
        string alias=candidatos?"x":"i", idColumn=candidatos?spec.TargetId:spec.Id;
        string select=$"SELECT {alias}.{idColumn} Id,x.{spec.TargetId} DestinoId,{alias}.estado Estado,COALESCE({alias}.fecha_modifica,{alias}.fecha_creacion) FechaRevision,{spec.Fields} ";
        string from=$" FROM {spec.From} ";
        if(!candidatos)from+=$" JOIN {spec.Table} i ON i.{spec.Target}=x.{spec.TargetId} AND i.{spec.Owner}=@OwnerId ";
        from+=" WHERE "+AssignmentCompany(tipo);
        if(candidatos)from+=$" AND {spec.Eligible} AND NOT EXISTS(SELECT 1 FROM {spec.Table} i WHERE i.{spec.Owner}=@OwnerId AND i.{spec.Target}=x.{spec.TargetId}"+(tipo=="personal"?" AND i.estado='A'":"")+")";
        from+=$" AND (@Search IS NULL OR {spec.Search})";
        from+=" AND (@FilterSite IS NULL OR x.empresa_sede_id=@FilterSite)";
        if(tipo=="series")from+=" AND (@TypeId IS NULL OR x.tipo_doc_com_id=@TypeId)";
        if(tipo=="listas")from+=" AND (@CurrencyId IS NULL OR x.moneda_id=@CurrencyId)";
        var columns=spec.Columns;columns["estado"]=alias+".estado";
        var model=new AsignacionVentaPageModel();
        model.Items=await PageVentaAsync<AsignacionVentaModel>(conn,model,select,from,new(new{EmpresaId=empresaId,OwnerId=ownerId,Search=Search(search),FilterSite=filtroSedeId,TypeId=tipoDocumentoId,CurrencyId=monedaId}),page,top,sort,dir,columns,tipo=="series"?"sede":"nombre",alias+"."+idColumn);return model;
    }
    public async Task<(bool success,string message,int id)> AgregarAsignacionesVentaAsync(int empresaId,int sedeId,int userId,VentaVinculoSolicitud request)
    {
        if(request is null||!Valid(request)||empresaId<=0||sedeId<=0||userId<=0||request.DestinoIds.Any(id=>id<=0)||request.DestinoIds.Distinct().Count()!=request.DestinoIds.Length)return Failure("Seleccione entre uno y 500 registros diferentes.");
        var spec=Assignment(request.Tipo);if(spec is null)return Failure("El tipo de asignación no está disponible.");
        // Caja uses its existing individual insert; batch TVPs are exclusive to seller series/price lists.
        if(request.Tipo=="personal"&&request.DestinoIds.Length!=1)return Failure("Seleccione una persona para asignar a la caja.");
        using var conn=factory.CreateConnection();var before=await AssignmentOwnerAsync(conn,null,empresaId,sedeId,request.OwnerId,request.Tipo);
        if(before is null||before.Estado!="A")return Failure("La caja o vendedor no está habilitado.");
        if(conn.State!=ConnectionState.Open)conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        if(!await LockCompanyAsync(conn,tx,empresaId))return Failure("La empresa no está habilitada.");
        var owner=await AssignmentOwnerAsync(conn,tx,empresaId,sedeId,request.OwnerId,request.Tipo);if(owner is null||owner.Estado!="A")return Failure("La caja o vendedor cambió o no pertenece al contexto seleccionado.");
        var args=new{EmpresaId=empresaId,OwnerId=request.OwnerId,Ids=request.DestinoIds};
        if(await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {spec.From} WHERE {AssignmentCompany(request.Tipo)} AND {spec.Eligible} AND x.{spec.TargetId} IN @Ids",args,tx)!=request.DestinoIds.Length)return Failure("Algún registro no está activo o no pertenece a la empresa.");
        if(await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {spec.Table} WITH (UPDLOCK,HOLDLOCK) WHERE {spec.Owner}=@OwnerId AND {spec.Target} IN @Ids"+(request.Tipo=="personal"?" AND estado='A'":""),args,tx)>0)return Failure(request.Tipo=="personal"?"La persona ya está asignada a esta caja.":"Alguna asignación ya existe. Para una asignación anulada, utilice Activar.");
        int id;
        if(request.Tipo=="personal")id=await InsertAsync(conn,tx,"sp_caja_perso_insertar",new{CajaId=request.OwnerId,PersonalId=request.DestinoIds[0],UsuarioAccionId=userId});
        else
        {
            var table=new DataTable();table.Columns.Add("value",typeof(int));foreach(int target in request.DestinoIds)table.Rows.Add(target);
            var values=new DynamicParameters(new{VendedorId=request.OwnerId,UsuarioAccionId=userId});
            values.Add(request.Tipo=="series"?"DocumentoComercialSerieIdList":"ListaPrecioIdList",table.AsTableValuedParameter("dbo.ListInt"));
            await conn.ExecuteAsync(request.Tipo=="series"?"sp_vend_serie_actualizarListaDocumentoComercialSerieId":"sp_lista_p_vendedor_actualizarListaPrecioVendedorId",values,tx,commandType:CommandType.StoredProcedure);id=request.OwnerId;
        }
        tx.Commit();return(true,"Asignación guardada.",id);
    }
    private static Task<AsignacionVentaModel?> AssignmentRowAsync(IDbConnection conn,IDbTransaction? tx,AsignacionSpec spec,string tipo,int empresaId,int ownerId,int id)
        =>conn.QuerySingleOrDefaultAsync<AsignacionVentaModel>($@"SELECT i.{spec.Id} Id,x.{spec.TargetId} DestinoId,i.estado Estado,COALESCE(i.fecha_modifica,i.fecha_creacion) FechaRevision
            FROM {spec.From} JOIN {spec.Table} i {(tx is null?"":"WITH (UPDLOCK,HOLDLOCK)")} ON i.{spec.Target}=x.{spec.TargetId}
            WHERE {AssignmentCompany(tipo)} AND i.{spec.Owner}=@OwnerId AND i.{spec.Id}=@Id",new{EmpresaId=empresaId,OwnerId=ownerId,Id=id},tx);

    public async Task<(bool success,string message,int id)> CambiarEstadoAsignacionVentaAsync(int empresaId,int sedeId,int userId,VentaVinculoEstadoSolicitud request)
    {
        if(request is null||!Valid(request)||empresaId<=0||sedeId<=0||userId<=0)return Failure("Revise la asignación seleccionada.");
        var spec=Assignment(request.Tipo);if(spec is null||(request.Tipo=="personal"&&request.Activar))return Failure("La operación no existe para esta asignación.");
        using var conn=factory.CreateConnection();var beforeOwner=await AssignmentOwnerAsync(conn,null,empresaId,sedeId,request.OwnerId,request.Tipo);
        if(beforeOwner is null||beforeOwner.Estado!="A")return Failure("La caja o vendedor no está habilitado.");
        string expected=request.Activar?"I":"A";
        var before=await AssignmentRowAsync(conn,null,spec,request.Tipo,empresaId,request.OwnerId,request.Id);
        if(before is null||before.Estado!=expected||before.Revision!=request.Revision)return Failure("La asignación cambió o no está disponible.");
        if(conn.State!=ConnectionState.Open)conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        if(!await LockCompanyAsync(conn,tx,empresaId))return Failure("La empresa no está habilitada.");
        var owner=await AssignmentOwnerAsync(conn,tx,empresaId,sedeId,request.OwnerId,request.Tipo);if(owner is null||owner.Estado!="A")return Failure("La caja o vendedor cambió.");
        var old=await AssignmentRowAsync(conn,tx,spec,request.Tipo,empresaId,request.OwnerId,request.Id);
        if(old is null||old.Estado!=expected||old.Revision!=request.Revision)return Failure("La asignación cambió. Actualice el detalle.");
        if(request.Activar)
        {
            if(await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {spec.From} WHERE {AssignmentCompany(request.Tipo)} AND {spec.Eligible} AND x.{spec.TargetId}=@Id",new{EmpresaId=empresaId,Id=old.DestinoId},tx)!=1)return Failure("La serie o lista de precios ya no está habilitada para esta empresa.");
            if(await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {spec.Table} WITH (UPDLOCK,HOLDLOCK) WHERE {spec.Owner}=@OwnerId AND {spec.Target}=@TargetId AND estado='A' AND {spec.Id}<>@Id",new{request.OwnerId,TargetId=old.DestinoId,Id=old.Id},tx)>0)return Failure("Ya existe una asignación activa para ese registro.");
        }
        string procedure=request.Tipo switch {"personal"=>"sp_caja_perso_eliminar","series"=>request.Activar?"sp_vend_serie_actualizar":"sp_vend_serie_eliminar",_=>request.Activar?"sp_lista_p_vendedor_actualizar":"sp_lista_p_vendedor_eliminar"};
        var parameters=new DynamicParameters(new{UsuarioAccionId=userId});parameters.Add(request.Tipo switch{"personal"=>"CajaPersonalId","series"=>"VendedorSerieId",_=>"ListaPrecioVendedorId"},old.Id);
        if(request.Activar)parameters.Add("Estado","A");
        await conn.ExecuteAsync(procedure,parameters,tx,commandType:CommandType.StoredProcedure);tx.Commit();return(true,request.Activar?"Asignación activada.":"Asignación retirada.",old.Id);
    }
}
