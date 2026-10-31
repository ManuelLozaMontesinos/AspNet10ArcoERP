using System.ComponentModel.DataAnnotations;
using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public class TesoreriaService(IDbConnectionFactory factory) : ITesoreriaService
{
    private const string DocumentoSelect=@"SELECT c.comprobante_id Id,c.periodo_emp_sede_id ConsultaId,c.moneda_id MonedaId,tdc.abrev Tipo,
        CONCAT(RTRIM(c.serie),' - ',RTRIM(c.numero)) Documento,c.fecha Fecha,p.razon_social Cliente,p.num_doc_iden Identidad,m.simbolo Moneda,c.monto_tc TipoCambio,
        c.monto_neto Neto,c.monto_igv Igv,c.monto_total Total,(CASE WHEN m.es_local='N' THEN c.monto_tc ELSE 1 END)*c.monto_total TotalLocal,
        COALESCE(c.bancariza,'') Bancarizado,COALESCE(co.cotiza,'') Pedido,pv.razon_social Vendedor,COALESCE(lp.nombre,'') ListaPrecio,tcp.nombre CondicionPago,mp.nombre MedioPago,
        COALESCE(c.observa,'') Observacion,c.estado Estado,COALESCE(c.fecha_modifica,c.fecha_creacion) FechaRevision ";
    private const string DocumentoFrom=@" FROM comprobante c {LOCK} JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id=c.tipo_doc_com_id
        JOIN estado_compro ec ON ec.estado_compro_id=c.estado_compro_id JOIN proveedor p ON p.proveedor_id=c.proveedor_id JOIN moneda m ON m.moneda_id=c.moneda_id
        JOIN vendedor v ON v.vendedor_id=c.vendedor_id JOIN proveedor pv ON pv.proveedor_id=v.proveedor_id JOIN tipo_c_pago tcp ON tcp.tipo_c_pago_id=c.tipo_c_pago_id
        JOIN medio_pago mp ON mp.medio_pago_id=c.medio_pago_id LEFT JOIN cotiza co ON co.cotiza_id=c.cotiza_id AND EXISTS(
            SELECT 1 FROM periodo_emp_sede cps JOIN empresa_sede cs ON cs.empresa_sede_id=cps.empresa_sede_id WHERE cps.periodo_emp_sede_id=co.periodo_emp_sede_id AND cs.empresa_id=@EmpresaId)
        LEFT JOIN lista_precio lp ON lp.lista_precio_id=c.lista_precio_id AND EXISTS(SELECT 1 FROM empresa_sede ls WHERE ls.empresa_sede_id=lp.empresa_sede_id AND ls.empresa_id=@EmpresaId)
        WHERE c.periodo_emp_sede_id=@ScopeId AND p.empresa_id=@EmpresaId AND pv.empresa_id=@EmpresaId AND mp.empresa_id=@EmpresaId
            AND tdc.tipo_doc_com IN ('01','03') AND ec.estado_compro='3' AND c.estado='A'
            AND (CASE WHEN m.es_local='N' THEN c.monto_tc ELSE 1 END)*c.monto_total>=@Umbral ";
    private const string CobranzaSelect=@"SELECT cc.compro_cobra_id Id,cc.comprobante_id ComprobanteId,cc.empresa_cta_id CuentaId,cc.fecha Fecha,cc.numero_opera Operacion,cc.monto Monto,
        CASE WHEN cta.empresa_cta_id IS NULL THEN 'Cuenta no disponible' ELSE CONCAT(b.nombre,' · ',cta.nombre,' · ',cta.cuenta) END Cuenta,
        cc.estado Estado,COALESCE(cc.fecha_modifica,cc.fecha_creacion) FechaRevision ";
    private const string CobranzaFrom=@" FROM compro_cobra cc {LOCK} LEFT JOIN empresa_cta cta ON cta.empresa_cta_id=cc.empresa_cta_id AND cta.empresa_id=@EmpresaId AND cta.moneda_id=@MonedaId
        LEFT JOIN banco b ON b.banco_id=cta.banco_id WHERE cc.comprobante_id=@ComprobanteId ";
    private static string Locked(string sql,IDbTransaction? tx)=>sql.Replace("{LOCK}",tx is null?"":"WITH (UPDLOCK,HOLDLOCK)");
    private static async Task<decimal> UmbralAsync(IDbConnection conn,IDbTransaction? tx=null)
    {
        var values=(await conn.QueryAsync<decimal?>("SELECT valor_numerico FROM parametro_sistema WHERE parametro_sistema='0024' AND estado='A'",transaction:tx)).AsList();
        if(values.Count!=1||!values[0].HasValue)throw new ValidationException("Configure un único parámetro activo Ventas - Bancarización (0024) con un importe válido.");
        return values[0]!.Value;
    }
    private static async Task<BancarizacionDocumentoModel?> DocumentoAsync(IDbConnection conn,IDbTransaction? tx,int empresaId,int consultaId,int id)
        =>await conn.QuerySingleOrDefaultAsync<BancarizacionDocumentoModel>(DocumentoSelect+Locked(DocumentoFrom,tx)+" AND c.comprobante_id=@Id",new{EmpresaId=empresaId,ScopeId=consultaId,Id=id,Umbral=await UmbralAsync(conn,tx)},tx);
    private static Task<BancarizacionCobranzaModel?> CobranzaAsync(IDbConnection conn,IDbTransaction? tx,int empresaId,BancarizacionDocumentoModel documento,int id)
        =>conn.QuerySingleOrDefaultAsync<BancarizacionCobranzaModel>(CobranzaSelect+Locked(CobranzaFrom,tx)+" AND cc.compro_cobra_id=@Id",new{EmpresaId=empresaId,documento.MonedaId,ComprobanteId=documento.Id,Id=id},tx);

    public async Task<BancarizacionPageModel> GetBancarizacionAsync(int empresaId,int contextoId,BancarizacionFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null)
    {
        var model=new BancarizacionPageModel{Filtro=filtro??new()};filtro=model.Filtro;filtro.ConsultaId??=contextoId;
        var columns=new Dictionary<string,string>{{"tipo","tdc.abrev"},{"documento","Documento"},{"fecha","c.fecha"},{"cliente","p.razon_social"},{"identidad","p.num_doc_iden"},{"moneda","m.simbolo"},{"total","c.monto_total"},{"local","TotalLocal"},{"bancarizado","c.bancariza"}};
        model.Ordenar(sort,dir,columns.Keys,"fecha","desc");model.Paginar(0,page,top);
        if(empresaId<=0||contextoId<=0||!Valid(filtro)||filtro.Bancarizado is not (null or "" or "S" or "N")) {model.Contexto.Errores.Add("Revise el contexto y los filtros de consulta.");return model;}
        if(filtro.Desde?.Date>filtro.Hasta?.Date||filtro.Desde?.Year is <1900 or >9998||filtro.Hasta?.Year is <1900 or >9998){model.Contexto.Errores.Add("Seleccione un rango de fechas válido.");return model;}
        using var conn=factory.CreateConnection();model.Contexto=await ConsultaSedeSql.CargarAsync(conn,empresaId,contextoId,filtro.ConsultaId);if(model.Contexto.Errores.Count>0)return model;
        try{model.Umbral=await UmbralAsync(conn);}catch(ValidationException ex){model.Contexto.Errores.Add(ex.Message);return model;}
        string from=Locked(DocumentoFrom,null)+@" AND (@Numero IS NULL OR c.numero=@Numero) AND (@Identidad IS NULL OR p.num_doc_iden=@Identidad) AND (@Cliente IS NULL OR p.razon_social LIKE @Cliente)
            AND (@Desde IS NULL OR c.fecha>=@Desde) AND (@Hasta IS NULL OR c.fecha<@Hasta) AND (@Bancarizado IS NULL OR c.bancariza=@Bancarizado)
            AND (@Search IS NULL OR c.serie LIKE @Search OR c.numero LIKE @Search OR p.razon_social LIKE @Search OR p.num_doc_iden LIKE @Search)";
        var args=new DynamicParameters(new{EmpresaId=empresaId,ScopeId=filtro.ConsultaId,Umbral=model.Umbral,Numero=Text(filtro.Numero).Length==0?null:Text(filtro.Numero),Identidad=Text(filtro.Identidad).Length==0?null:Text(filtro.Identidad),Cliente=Search(filtro.Cliente),Search=Search(filtro.Search),Desde=filtro.Desde?.Date,Hasta=filtro.Hasta?.Date.AddDays(1),Bancarizado=string.IsNullOrEmpty(filtro.Bancarizado)?null:filtro.Bancarizado});
        model.Paginar(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*)"+from,args),page,top);args.Add("Offset",(model.Page-1)*model.PageSize);args.Add("Top",model.PageSize);
        model.Items=(await conn.QueryAsync<BancarizacionDocumentoModel>(DocumentoSelect+from+$" ORDER BY {columns[model.Sort]} {model.Dir},c.comprobante_id OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY",args)).AsList();model.VisibleCount=model.Items.Count;return model;
    }

    public async Task<BancarizacionDetalleModel?> GetBancarizacionDocumentoAsync(int empresaId,int contextoId,int consultaId,int id)
    {
        if(empresaId<=0||contextoId<=0||consultaId<=0||id<=0)return null;using var conn=factory.CreateConnection();var scope=await ConsultaSedeSql.CargarAsync(conn,empresaId,contextoId,consultaId);if(scope.Errores.Count>0)return null;
        var data=await DocumentoAsync(conn,null,empresaId,consultaId,id);if(data is null)return null;
        return new(){Datos=data,Sede=scope.NombreSede(consultaId),Cuentas=(await conn.QueryAsync<LookupItemModel>(@"SELECT cta.empresa_cta_id Id,CONCAT(b.nombre,' · ',cta.nombre,' · ',cta.cuenta) Nombre FROM empresa_cta cta JOIN banco b ON b.banco_id=cta.banco_id
            WHERE cta.empresa_id=@EmpresaId AND cta.moneda_id=@MonedaId AND cta.estado='A' ORDER BY b.nombre,cta.nombre,cta.empresa_cta_id",new{EmpresaId=empresaId,data.MonedaId})).AsList()};
    }
    public async Task<BancarizacionCobranzaPageModel?> GetBancarizacionCobranzasAsync(int empresaId,int contextoId,int consultaId,int comprobanteId,string? search=null,int page=1,int top=25,string? sort=null,string? dir=null)
    {
        if(empresaId<=0||contextoId<=0||consultaId<=0||comprobanteId<=0||search?.Length>150)return null;using var conn=factory.CreateConnection();if((await ConsultaSedeSql.CargarAsync(conn,empresaId,contextoId,consultaId)).Errores.Count>0)return null;
        var doc=await DocumentoAsync(conn,null,empresaId,consultaId,comprobanteId);if(doc is null)return null;
        var model=new BancarizacionCobranzaPageModel();var columns=new Dictionary<string,string>{{"fecha","cc.fecha"},{"operacion","cc.numero_opera"},{"monto","cc.monto"},{"cuenta","Cuenta"},{"estado","cc.estado"}};model.Ordenar(sort,dir,columns.Keys,"fecha");
        string from=Locked(CobranzaFrom,null)+" AND (@Search IS NULL OR cc.numero_opera LIKE @Search OR cta.nombre LIKE @Search OR b.nombre LIKE @Search OR cta.cuenta LIKE @Search)";
        var args=new DynamicParameters(new{EmpresaId=empresaId,doc.MonedaId,ComprobanteId=comprobanteId,Search=Search(search)});model.Paginar(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*)"+from,args),page,top);args.Add("Offset",(model.Page-1)*model.PageSize);args.Add("Top",model.PageSize);
        model.Items=(await conn.QueryAsync<BancarizacionCobranzaModel>(CobranzaSelect+from+$" ORDER BY {columns[model.Sort]} {model.Dir},cc.compro_cobra_id OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY",args)).AsList();model.VisibleCount=model.Items.Count;return model;
    }
    private static async Task ActualizarIndicadorAsync(IDbConnection conn,IDbTransaction tx,int comprobanteId,int userId)
    {
        int active=await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM compro_cobra WHERE comprobante_id=@Id AND estado='A'",new{Id=comprobanteId},tx);
        await conn.ExecuteAsync("sp_comprobante_actualizarBancarizado",new{ComprobanteId=comprobanteId,Bancarizado=active>0?"S":"N",UsuarioAccionId=userId},tx,commandType:CommandType.StoredProcedure);
    }
    public async Task<(bool success,string message,int id)> SaveBancarizacionCobranzaAsync(int empresaId,int contextoId,int userId,BancarizacionCobranzaSolicitud model)
    {
        if(model is null||!Valid(model)||model.ContextoId!=contextoId||empresaId<=0||userId<=0||model.Id>0&&model.Revision?.Length!=64||model.Fecha!.Value.Year is <1900 or >9998||decimal.Round(model.Monto!.Value,4)!=model.Monto.Value||Text(model.Operacion).Length==0)return Failure("Revise los datos de la cobranza, fecha y monto de hasta cuatro decimales.");
        using var conn=factory.CreateConnection();if(conn.State!=ConnectionState.Open)conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        try{
            if((await ConsultaSedeSql.CargarAsync(conn,empresaId,contextoId,model.ConsultaId,tx)).Errores.Count>0)return Failure("El contexto o la sede de consulta cambiaron.");
            var doc=await DocumentoAsync(conn,tx,empresaId,model.ConsultaId,model.ComprobanteId);if(doc is null||doc.Revision!=model.DocumentoRevision)return Failure("El comprobante cambió o no cumple los filtros de bancarización. Vuelva a abrirlo.");
            if(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM empresa_cta WHERE empresa_cta_id=@Id AND empresa_id=@EmpresaId AND moneda_id=@MonedaId AND estado='A'",new{Id=model.CuentaId,EmpresaId=empresaId,doc.MonedaId},tx)!=1)return Failure("Seleccione una cuenta activa de la empresa y moneda del comprobante.");
            int id=model.Id;
            if(id==0)id=await InsertAsync(conn,tx,"sp_compro_cobra_insertar",new{ComprobanteId=doc.Id,Fecha=model.Fecha.Value.Date,NumeroOperacion=Text(model.Operacion),Monto=model.Monto.Value,EmpresaCuentaBancariaId=model.CuentaId,UsuarioAccionId=userId});
            else{
                var current=await CobranzaAsync(conn,tx,empresaId,doc,id);if(current is null||current.Estado!="A"||current.Revision!=model.Revision)return Failure("La cobranza cambió o no está activa. Actualice el detalle.");
                if(model.Fecha.Value.Date!=current.Fecha.Date)return Failure("La fecha de una cobranza existente no se modifica.");
                await conn.ExecuteAsync("sp_compro_cobra_actualizar",new{ComprobanteCobranzaId=id,NumeroOperacion=Text(model.Operacion),Monto=model.Monto.Value,EmpresaCuentaBancariaId=model.CuentaId,UsuarioAccionId=userId},tx,commandType:CommandType.StoredProcedure);
            }
            await ActualizarIndicadorAsync(conn,tx,doc.Id,userId);tx.Commit();return(true,"Cobranza guardada e indicador de bancarización actualizado.",id);
        }catch(ValidationException ex){return Failure(ex.Message);}
    }
    public async Task<(bool success,string message,int id)> AnularBancarizacionCobranzaAsync(int empresaId,int contextoId,int userId,BancarizacionAnularSolicitud model)
    {
        if(model is null||!Valid(model)||model.ContextoId!=contextoId||empresaId<=0||userId<=0)return Failure("Revise los datos enviados y el contexto.");
        using var conn=factory.CreateConnection();if(conn.State!=ConnectionState.Open)conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        try{
            if((await ConsultaSedeSql.CargarAsync(conn,empresaId,contextoId,model.ConsultaId,tx)).Errores.Count>0)return Failure("El contexto o la sede de consulta cambiaron.");
            var doc=await DocumentoAsync(conn,tx,empresaId,model.ConsultaId,model.ComprobanteId);if(doc is null||doc.Revision!=model.DocumentoRevision)return Failure("El comprobante cambió. Vuelva a abrirlo.");
            var current=await CobranzaAsync(conn,tx,empresaId,doc,model.Id);if(current is null||current.Estado!="A"||current.Revision!=model.Revision)return Failure("La cobranza cambió o ya fue anulada.");
            await conn.ExecuteAsync("sp_compro_cobra_eliminar",new{ComprobanteCobranzaId=current.Id,UsuarioAccionId=userId},tx,commandType:CommandType.StoredProcedure);
            await ActualizarIndicadorAsync(conn,tx,doc.Id,userId);tx.Commit();return(true,"Cobranza anulada e indicador de bancarización actualizado.",current.Id);
        }catch(ValidationException ex){return Failure(ex.Message);}
    }
}
