using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class VentasService
{
    private const string ClienteFields="RTRIM(p.proveedor) Codigo,p.razon_social Nombre,COALESCE(NULLIF(p.numero_ruc,''),p.num_doc_iden,'') Documento ";
    public Task<GrupoClienteDetalleModel?> GetGrupoClientesAsync(int empresaId,int grupoId,string? search=null,int page=1,int top=25,string? sort=null,string? dir=null)
        =>GroupPeopleAsync(empresaId,grupoId,search,page,top,sort,dir,false);
    public Task<GrupoClienteDetalleModel?> GetGrupoCandidatosAsync(int empresaId,int grupoId,string? search=null,int page=1,int top=25,string? sort=null,string? dir=null)
        =>GroupPeopleAsync(empresaId,grupoId,search,page,top,sort,dir,true);
    private async Task<GrupoClienteDetalleModel?> GroupPeopleAsync(int empresaId,int grupoId,string? search,int page,int top,string? sort,string? dir,bool candidates)
    {
        if(empresaId<=0||grupoId<=0||search?.Length>150)return null;
        using var conn=factory.CreateConnection();var group=await GrupoAsync(conn,null,empresaId,grupoId);if(group is null)return null;
        var columns=new Dictionary<string,string>{["codigo"]="p.proveedor",["nombre"]="p.razon_social",["documento"]="Documento"};if(!candidates)columns["estado"]="i.estado";
        var model=new GrupoClienteDetalleModel();model.Ordenar(sort,dir,columns.Keys,"nombre");
        string from=candidates?"FROM proveedor p WHERE p.empresa_id=@EmpresaId AND p.estado='A' AND NOT EXISTS(SELECT 1 FROM grupo_p_item i WHERE i.grupo_prove_id=@GroupId AND i.proveedor_id=p.proveedor_id)":@"FROM grupo_p_item i JOIN grupo_prove g ON g.grupo_prove_id=i.grupo_prove_id JOIN proveedor p ON p.proveedor_id=i.proveedor_id WHERE g.empresa_id=@EmpresaId AND p.empresa_id=@EmpresaId AND i.grupo_prove_id=@GroupId";
        from+=" AND (@Search IS NULL OR p.razon_social LIKE @Search OR p.proveedor LIKE @Search OR p.numero_ruc LIKE @Search OR p.num_doc_iden LIKE @Search)";
        var args=new DynamicParameters(new{EmpresaId=empresaId,GroupId=grupoId,Search=Search(search)});model.Paginar(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) "+from,args),page,top);args.Add("Offset",(model.Page-1)*model.PageSize);args.Add("Top",model.PageSize);
        string fields=candidates?"SELECT p.proveedor_id Id,p.proveedor_id ProveedorId,@GroupId GrupoId,'A' Estado,COALESCE(p.fecha_modifica,p.fecha_creacion) FechaRevision,":"SELECT i.grupo_p_item_id Id,i.grupo_prove_id GrupoId,i.proveedor_id ProveedorId,i.estado Estado,COALESCE(i.fecha_modifica,i.fecha_creacion) FechaRevision,";
        model.Items=(await conn.QueryAsync<GrupoClienteItemModel>(fields+ClienteFields+from+$" ORDER BY {columns[model.Sort]} {model.Dir},"+(candidates?"p.proveedor_id":"i.grupo_p_item_id")+" OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY",args)).AsList();model.VisibleCount=model.Items.Count;return model;
    }
    public async Task<(bool success,string message,int id)> AgregarGrupoClienteAsync(int empresaId,int userId,int grupoId,int proveedorId)
    {
        if(empresaId<=0||userId<=0||grupoId<=0||proveedorId<=0)return Failure("Seleccione un grupo y un tercero válidos.");
        var before=await GetGrupoClienteAsync(empresaId,grupoId);if(before is null||before.Estado!="A")return Failure("El grupo no está habilitado.");
        using var conn=factory.CreateConnection();
        if(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM proveedor WHERE proveedor_id=@Id AND empresa_id=@EmpresaId AND estado='A'",new{Id=proveedorId,EmpresaId=empresaId})!=1)return Failure("Seleccione un tercero activo de la empresa.");
        if(conn.State!=ConnectionState.Open)conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);if(!await LockCompanyAsync(conn,tx,empresaId))return Failure("La empresa no está habilitada.");
        var group=await GrupoAsync(conn,tx,empresaId,grupoId);if(group is null||group.Estado!="A")return Failure("El grupo no está habilitado.");
        if(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM proveedor WITH (UPDLOCK,HOLDLOCK) WHERE proveedor_id=@Id AND empresa_id=@EmpresaId AND estado='A'",new{Id=proveedorId,EmpresaId=empresaId},tx)!=1)return Failure("El tercero cambió o no pertenece a la empresa.");
        // The legacy picker prevents duplicates even when the previous link is inactive; it has no reactivation action.
        if(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM grupo_p_item WITH (UPDLOCK,HOLDLOCK) WHERE grupo_prove_id=@GroupId AND proveedor_id=@PersonId",new{GroupId=grupoId,PersonId=proveedorId},tx)>0)return Failure("El tercero ya pertenece o perteneció a este grupo. No se crea un vínculo duplicado.");
        int id=await InsertAsync(conn,tx,"sp_grupo_p_item_insertar",new{GrupoProveedorId=grupoId,ProveedorId=proveedorId,UsuarioAccionId=userId});tx.Commit();return(true,"Tercero agregado al grupo.",id);
    }
    public async Task<(bool success,string message,int id)> QuitarGrupoClienteAsync(int empresaId,int userId,int grupoId,int id,string revision)
    {
        if(empresaId<=0||userId<=0||grupoId<=0||id<=0||revision?.Length!=64)return Failure("Revise el vínculo seleccionado.");
        using var conn=factory.CreateConnection();
        const string read=@"SELECT i.grupo_p_item_id Id,i.estado Estado,COALESCE(i.fecha_modifica,i.fecha_creacion) FechaRevision FROM grupo_p_item i {LOCK}
            JOIN grupo_prove g ON g.grupo_prove_id=i.grupo_prove_id JOIN proveedor p ON p.proveedor_id=i.proveedor_id
            WHERE i.grupo_p_item_id=@Id AND i.grupo_prove_id=@GroupId AND g.empresa_id=@EmpresaId AND p.empresa_id=@EmpresaId AND g.estado='A'";
        var args=new{Id=id,GroupId=grupoId,EmpresaId=empresaId};var before=await conn.QuerySingleOrDefaultAsync<GrupoClienteItemModel>(read.Replace("{LOCK}",""),args);
        if(before is null||before.Estado!="A"||before.Revision!=revision)return Failure("El vínculo cambió o ya fue retirado.");
        if(conn.State!=ConnectionState.Open)conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);if(!await LockCompanyAsync(conn,tx,empresaId))return Failure("La empresa no está habilitada.");
        var group=await GrupoAsync(conn,tx,empresaId,grupoId);if(group is null||group.Estado!="A")return Failure("El grupo no está habilitado.");
        var old=await conn.QuerySingleOrDefaultAsync<GrupoClienteItemModel>(read.Replace("{LOCK}","WITH (UPDLOCK,HOLDLOCK)"),args,tx);
        if(old is null||old.Estado!="A"||old.Revision!=revision)return Failure("El vínculo cambió. Actualice los clientes del grupo.");
        await conn.ExecuteAsync("sp_grupo_p_item_eliminar",new{GrupoProveedorItemId=id,UsuarioAccionId=userId},tx,commandType:CommandType.StoredProcedure);tx.Commit();return(true,"Vínculo retirado del grupo.",id);
    }
}
