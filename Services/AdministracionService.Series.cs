using System.Data;
using System.Globalization;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class AdministracionService
{
    private const string SerieSelect=@"SELECT d.doc_com_serie_id Id,d.empresa_sede_id EmpresaSedeId,s.nombre Sede,d.tipo_doc_com_id TipoDocumentoId,t.nombre TipoDocumento,
        RTRIM(d.serie) Serie,d.correlativo Correlativo,CAST(CASE WHEN t.es_electro='S' THEN 1 ELSE 0 END AS bit) Electronico,
        CAST(CASE WHEN t.es_comp_pago='S' THEN 1 ELSE 0 END AS bit) ComprobantePago,CAST(CASE WHEN d.es_contingencia='S' THEN 1 ELSE 0 END AS bit) EsContingencia,
        CAST(CASE WHEN d.envio_en_linea='S' THEN 1 ELSE 0 END AS bit) EnvioEnLinea,d.formato Formato,d.max_item_imp MaximoItems,d.archivo_formato ArchivoFormato,
        d.tipo_d_c_rel_id TipoRelacionadoId,ISNULL(r.nombre,'') TipoRelacionado,d.almacen_id AlmacenId,ISNULL(a.nombre,'') Almacen,
        d.estado Estado,COALESCE(d.fecha_modifica,d.fecha_creacion) FechaRevision ";
    private const string SerieFrom=@" FROM doc_com_serie d {LOCK} JOIN empresa_sede s ON s.empresa_sede_id=d.empresa_sede_id
        JOIN tipo_doc_com t ON t.tipo_doc_com_id=d.tipo_doc_com_id LEFT JOIN tipo_doc_com r ON r.tipo_doc_com_id=d.tipo_d_c_rel_id
        LEFT JOIN almacen a ON a.almacen_id=d.almacen_id AND a.empresa_sede_id=d.empresa_sede_id ";
    public async Task<DocumentoSeriePageModel> GetDocumentoSeriesAsync(int empresaId,string? search=null,int? sedeId=null,int? tipoDocumentoId=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null)
    {
        var model=new DocumentoSeriePageModel{Search=search,SedeId=sedeId,TipoDocumentoId=tipoDocumentoId,Estado=State(estado)};
        var columns=new Dictionary<string,string>{["sede"]="s.nombre",["documento"]="t.nombre",["electronico"]="t.es_electro",["serie"]="d.serie",["correlativo"]="d.correlativo",["formato"]="d.formato",["almacen"]="a.nombre",["estado"]="d.estado"};model.Ordenar(sort,dir,columns.Keys,"sede");model.Paginar(0,page,top);if(empresaId<=0)return model;
        using var conn=_dbConnectionFactory.CreateConnection();
        const string where=@" WHERE s.empresa_id=@EmpresaId AND (@Search IS NULL OR d.serie LIKE @Search OR t.nombre LIKE @Search OR s.nombre LIKE @Search)
            AND (@SiteId IS NULL OR d.empresa_sede_id=@SiteId) AND (@TypeId IS NULL OR d.tipo_doc_com_id=@TypeId) AND (@Estado IS NULL OR d.estado=@Estado)";
        var args=new DynamicParameters(new{EmpresaId=empresaId,Search=Search(search),SiteId=sedeId,TypeId=tipoDocumentoId,Estado=State(estado)});
        string from=SerieFrom.Replace("{LOCK}","");model.Paginar(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*)"+from+where,args),page,top);args.Add("Offset",(model.Page-1)*model.PageSize);args.Add("Top",model.PageSize);
        model.Items=(await conn.QueryAsync<DocumentoSerieModel>(SerieSelect+from+where+$" ORDER BY {columns[model.Sort]} {model.Dir},d.doc_com_serie_id OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY",args)).AsList();model.VisibleCount=model.Items.Count;
        model.Sedes=(await conn.QueryAsync<LookupItemModel>("SELECT empresa_sede_id Id,nombre Nombre FROM empresa_sede WHERE empresa_id=@EmpresaId AND estado='A' ORDER BY nombre,empresa_sede_id",new{EmpresaId=empresaId})).AsList();
        model.TiposDocumento=(await conn.QueryAsync<TipoDocumentoSerieLookup>(@"SELECT tipo_doc_com_id Id,tipo_doc_com Codigo,nombre Nombre,CAST(CASE WHEN es_electro='S' THEN 1 ELSE 0 END AS bit) Electronico,CAST(CASE WHEN es_comp_pago='S' THEN 1 ELSE 0 END AS bit) ComprobantePago FROM tipo_doc_com WHERE estado='A' ORDER BY nombre,tipo_doc_com_id")).AsList();return model;
    }
    public async Task<DocumentoSerieModel?> GetDocumentoSerieAsync(int empresaId,int id)
    {if(empresaId<=0||id<=0)return null;using var conn=_dbConnectionFactory.CreateConnection();return await SerieAsync(conn,null,empresaId,id);}
    private static Task<DocumentoSerieModel?> SerieAsync(IDbConnection conn,IDbTransaction? tx,int empresaId,int id)
        =>conn.QuerySingleOrDefaultAsync<DocumentoSerieModel>(SerieSelect+SerieFrom.Replace("{LOCK}",tx is null?"":"WITH (UPDLOCK,HOLDLOCK)")+" WHERE d.doc_com_serie_id=@Id AND s.empresa_id=@EmpresaId",new{Id=id,EmpresaId=empresaId},tx);
    public async Task<List<LookupItemModel>> GetSerieAlmacenesAsync(int empresaId,int sedeId)
    {
        if(empresaId<=0||sedeId<=0)return new();using var conn=_dbConnectionFactory.CreateConnection();return (await conn.QueryAsync<LookupItemModel>(@"SELECT a.almacen_id Id,a.nombre Nombre FROM almacen a JOIN empresa_sede s ON s.empresa_sede_id=a.empresa_sede_id WHERE s.empresa_id=@EmpresaId AND s.empresa_sede_id=@SiteId AND s.estado='A' AND a.estado='A' AND a.es_venta='S' ORDER BY a.nombre,a.almacen_id",new{EmpresaId=empresaId,SiteId=sedeId})).AsList();
    }
    public async Task<(bool success,string message,int id)> SaveDocumentoSerieAsync(int empresaId,int userId,MaestroEdicion<DocumentoSerieModel> request)
    {
        if(request?.Datos is null)return Failure("Revise los datos de la serie.");
        var model=request.Datos;model.Serie=Text(model.Serie).ToUpperInvariant();model.Formato=Text(model.Formato);model.ArchivoFormato=Text(model.ArchivoFormato);
        if(empresaId<=0||userId<=0||!Valid(request)||!Valid(model)||model.AlmacenId is <=0||model.TipoRelacionadoId is <=0)return Failure("Revise la sede, documento, serie y formato.");
        try{_ = model.Correlativo.ToString(model.Formato,CultureInfo.InvariantCulture);}catch(FormatException){return Failure("El formato de numeración no es válido.");}
        DocumentoSerieModel? before=null;
        if(model.Id>0){before=await GetDocumentoSerieAsync(empresaId,model.Id);if(before is null||before.Estado!="A"||before.Revision!=request.Revision)return Failure("La serie cambió o no está disponible. Actualice el listado.");}
        using var conn=_dbConnectionFactory.CreateConnection();if(conn.State!=ConnectionState.Open)conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);if(!await LockCompanyAsync(conn,tx,empresaId))return Failure("La empresa no está habilitada.");
        var old=model.Id==0?null:await SerieAsync(conn,tx,empresaId,model.Id);
        if(model.Id>0 && (old is null||old.Estado!="A"||old.Revision!=request.Revision))return Failure("La serie o su correlativo cambió. Actualice el listado.");
        if(old is not null && (model.EmpresaSedeId!=old.EmpresaSedeId||model.TipoDocumentoId!=old.TipoDocumentoId||model.Serie!=old.Serie||model.Correlativo!=old.Correlativo))return Failure("Sede, tipo, serie y correlativo no se modifican después del alta.");
        if(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM empresa_sede WHERE empresa_sede_id=@Id AND empresa_id=@EmpresaId AND (@Existing=1 OR estado='A')",new{Id=model.EmpresaSedeId,EmpresaId=empresaId,Existing=old is not null},tx)!=1)return Failure("Seleccione una sede activa de la empresa.");
        var type=await conn.QuerySingleOrDefaultAsync<TipoDocumentoSerieLookup>(@"SELECT tipo_doc_com_id Id,CAST(CASE WHEN es_electro='S' THEN 1 ELSE 0 END AS bit) Electronico,CAST(CASE WHEN es_comp_pago='S' THEN 1 ELSE 0 END AS bit) ComprobantePago FROM tipo_doc_com WHERE tipo_doc_com_id=@Id AND (@Existing=1 OR estado='A')",new{Id=model.TipoDocumentoId,Existing=old is not null},tx);
        if(type is null)return Failure("Seleccione un tipo de documento habilitado.");
        if(old is null && type.Electronico && model.Correlativo!=0)return Failure("El correlativo inicial de una serie electrónica debe ser cero.");
        if(type.ComprobantePago && !model.AlmacenId.HasValue)return Failure("Seleccione un almacén de ventas para el comprobante de pago.");
        if(model.AlmacenId.HasValue && await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM almacen WHERE almacen_id=@Id AND empresa_sede_id=@SiteId AND (@Existing=1 OR (estado='A' AND es_venta='S'))",new{Id=model.AlmacenId,SiteId=model.EmpresaSedeId,Existing=old?.AlmacenId==model.AlmacenId},tx)!=1)return Failure("Seleccione un almacén de ventas activo de la sede.");
        if(model.TipoRelacionadoId.HasValue && (old is null||old.TipoRelacionadoId!=model.TipoRelacionadoId) && await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM tipo_doc_com WHERE tipo_doc_com_id=@Id AND estado='A'",new{Id=model.TipoRelacionadoId},tx)!=1)return Failure("Seleccione un documento relacionado habilitado.");
        if(old is null && await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM doc_com_serie WITH (UPDLOCK,HOLDLOCK) WHERE empresa_sede_id=@SiteId AND tipo_doc_com_id=@TypeId AND serie=@Serie",new{SiteId=model.EmpresaSedeId,TypeId=model.TipoDocumentoId,model.Serie},tx)>0)return Failure("La sede y tipo de documento ya tienen esa serie.");
        int id=model.Id;var args=new DynamicParameters(new{EsContingencia=model.EsContingencia?"S":"N",EnvioEnLinea=model.EnvioEnLinea?"S":"N",model.Formato,MaximoItemImpresion=old?.MaximoItems??0,ArchivoFormato=model.ArchivoFormato??"",TipoDocumentoComercialRelacionadoId=model.TipoRelacionadoId,model.AlmacenId,UsuarioAccionId=userId});
        if(id==0){args.Add("EmpresaSedeId",model.EmpresaSedeId);args.Add("TipoDocumentoComercialId",model.TipoDocumentoId);args.Add("Serie",model.Serie);args.Add("Correlativo",model.Correlativo);id=await InsertAsync(conn,tx,"sp_doc_com_serie_insertar",args);}
        else{args.Add("DocumentoComercialSerieId",id);await conn.ExecuteAsync("sp_doc_com_serie_actualizar",args,tx,commandType:CommandType.StoredProcedure);}
        tx.Commit();return(true,"Serie guardada.",id);
    }
    public async Task<(bool success,string message,int id)> AnularDocumentoSerieAsync(int empresaId,int userId,int id,string revision)
    {
        var before=await GetDocumentoSerieAsync(empresaId,id);if(userId<=0||before is null||before.Estado!="A"||before.Revision!=revision)return Failure("La serie cambió o ya está anulada.");
        using var conn=_dbConnectionFactory.CreateConnection();if(conn.State!=ConnectionState.Open)conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);if(!await LockCompanyAsync(conn,tx,empresaId))return Failure("La empresa no está habilitada.");
        var old=await SerieAsync(conn,tx,empresaId,id);if(old is null||old.Estado!="A"||old.Revision!=revision)return Failure("La serie o su correlativo cambió. Actualice el listado.");
        await conn.ExecuteAsync("sp_doc_com_serie_eliminar",new{DocumentoComercialSerieId=id,UsuarioAccionId=userId},tx,commandType:CommandType.StoredProcedure);tx.Commit();return(true,"Serie anulada.",id);
    }
}
