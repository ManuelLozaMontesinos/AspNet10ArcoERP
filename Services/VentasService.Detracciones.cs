using System.Data;
using System.Globalization;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class VentasService
{
    private const string DetraccionSelect=@"SELECT c.comprobante_id Id,c.periodo_emp_sede_id ConsultaId,tdc.abrev TipoDocumento,
        CONCAT(RTRIM(c.serie),' - ',RTRIM(c.numero)) Documento,RTRIM(c.numero_opera) NumeroOperacion,c.fecha Fecha,p.razon_social Cliente,p.num_doc_iden DocumentoCliente,
        m.simbolo Moneda,c.monto_total Total,td.nombre TipoDetraccion,td.tasa Tasa,c.monto_detrac MontoDetraccion,c.num_opera_pago NumeroOperacionPago,
        c.fecha_pago FechaPago,c.monto_pago MontoPago,c.monto_cargo MontoCargo,COALESCE(c.fecha_modifica,c.fecha_creacion) FechaRevision ";
    private const string DetraccionFrom=@" FROM comprobante c {LOCK} JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id=c.tipo_doc_com_id
        JOIN proveedor p ON p.proveedor_id=c.proveedor_id JOIN vendedor v ON v.vendedor_id=c.vendedor_id JOIN proveedor pv ON pv.proveedor_id=v.proveedor_id
        JOIN moneda m ON m.moneda_id=c.moneda_id JOIN tipo_c_pago tcp ON tcp.tipo_c_pago_id=c.tipo_c_pago_id JOIN medio_pago mp ON mp.medio_pago_id=c.medio_pago_id
        JOIN tipo_comprob tc ON tc.tipo_comprob_id=c.tipo_comprob_id JOIN tipo_detrac td ON td.tipo_detrac_id=c.tipo_detrac_id JOIN estado_compro ec ON ec.estado_compro_id=c.estado_compro_id
        WHERE c.periodo_emp_sede_id=@ScopeId AND p.empresa_id=@EmpresaId AND pv.empresa_id=@EmpresaId AND mp.empresa_id=@EmpresaId
            AND tdc.tipo_doc_com='01' AND ec.estado_compro='3' AND td.tipo_detrac<>'000' AND c.estado='A' ";

    public async Task<DetraccionPageModel> GetDetraccionesAsync(int empresaId,int contextoId,DetraccionFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null)
    {
        var model=new DetraccionPageModel{Filtro=filtro??new()};filtro=model.Filtro;filtro.ConsultaId??=contextoId;
        var columns=new Dictionary<string,string>{{"tipo","tdc.abrev"},{"documento","Documento"},{"fecha","c.fecha"},{"cliente","p.razon_social"},{"identidad","p.num_doc_iden"},{"moneda","m.simbolo"},{"total","c.monto_total"},
            {"detraccion","td.nombre"},{"tasa","td.tasa"},{"monto","c.monto_detrac"},{"operacion","c.num_opera_pago"},{"fechapago","c.fecha_pago"},{"pago","c.monto_pago"},{"cargo","c.monto_cargo"}};
        model.Ordenar(sort,dir,columns.Keys,"fecha","desc");model.Paginar(0,page,top);
        if(empresaId<=0||contextoId<=0){model.Contexto.Errores.Add("Seleccione un contexto de trabajo válido.");return model;}
        using var conn=factory.CreateConnection();model.Contexto=await ConsultaSedeSql.CargarAsync(conn,empresaId,contextoId,filtro.ConsultaId);
        if(filtro.Search?.Length>150||filtro.Numero?.Length>8||filtro.Documento?.Length>35||filtro.RazonSocial?.Length>150)model.Contexto.Errores.Add("Reduzca la longitud de los filtros.");
        if((filtro.Desde.HasValue&&filtro.Desde.Value.Year is <1900 or >9998)||(filtro.Hasta.HasValue&&filtro.Hasta.Value.Year is <1900 or >9998)||filtro.Desde?.Date>filtro.Hasta?.Date)model.Contexto.Errores.Add("Seleccione un rango de fechas válido.");
        if(model.Contexto.Errores.Count>0)return model;
        string from=DetraccionFrom.Replace("{LOCK}","")+@" AND (@Numero IS NULL OR c.numero=@Numero) AND (@Documento IS NULL OR p.num_doc_iden=@Documento)
            AND (@Cliente IS NULL OR p.razon_social LIKE @Cliente) AND (@Desde IS NULL OR c.fecha>=@Desde) AND (@Hasta IS NULL OR c.fecha<@Hasta)
            AND (@Search IS NULL OR c.serie LIKE @Search OR c.numero LIKE @Search OR p.razon_social LIKE @Search OR p.num_doc_iden LIKE @Search OR c.num_opera_pago LIKE @Search)";
        var args=new DynamicParameters(new{EmpresaId=empresaId,ScopeId=filtro.ConsultaId,Numero=string.IsNullOrWhiteSpace(filtro.Numero)?null:filtro.Numero.Trim(),Documento=string.IsNullOrWhiteSpace(filtro.Documento)?null:filtro.Documento.Trim(),
            Cliente=Search(filtro.RazonSocial),Search=Search(filtro.Search),Desde=filtro.Desde?.Date,Hasta=filtro.Hasta?.Date.AddDays(1)});
        model.Paginar(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*)"+from,args),page,top);args.Add("Offset",(model.Page-1)*model.PageSize);args.Add("Top",model.PageSize);
        model.Items=(await conn.QueryAsync<DetraccionFila>(DetraccionSelect+from+" ORDER BY "+columns[model.Sort]+" "+model.Dir+",c.numero_opera DESC,c.comprobante_id OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY",args)).AsList();
        model.VisibleCount=model.Items.Count;return model;
    }

    public async Task<DetraccionFila?> GetDetraccionAsync(int empresaId,int contextoId,int consultaId,int id)
    {
        if(empresaId<=0||contextoId<=0||consultaId<=0||id<=0)return null;
        using var conn=factory.CreateConnection();var context=await ConsultaSedeSql.CargarAsync(conn,empresaId,contextoId,consultaId);
        return context.Errores.Count>0?null:await DetraccionAsync(conn,null,empresaId,consultaId,id);
    }

    private static Task<DetraccionFila?> DetraccionAsync(IDbConnection conn,IDbTransaction? tx,int empresaId,int consultaId,int id)
        =>conn.QuerySingleOrDefaultAsync<DetraccionFila>(DetraccionSelect+DetraccionFrom.Replace("{LOCK}",tx is null?"":"WITH (UPDLOCK,HOLDLOCK)")+" AND c.comprobante_id=@Id",new{EmpresaId=empresaId,ScopeId=consultaId,Id=id},tx);

    public async Task<(bool success,string message,int id)> SaveDetraccionPagoAsync(int empresaId,int contextoId,int userId,DetraccionPagoSolicitud request)
    {
        if(request is null||!Valid(request)||empresaId<=0||contextoId<=0||userId<=0||request.ContextoId!=contextoId||request.FechaPago?.Year is <1900 or >9998)
            return Failure("Revise el contexto, la operación, fecha y montos de pago.");
        request.NumeroOperacionPago=Text(request.NumeroOperacionPago);
        if(request.NumeroOperacionPago.Length==0||!decimal.TryParse(request.MontoPagoTexto,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var pago)||pago<=0||
            !decimal.TryParse(request.MontoCargoTexto,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var cargo)||cargo<0)return Failure("Ingrese un pago mayor que cero y un cargo no negativo, con hasta cuatro decimales.");
        var before=await GetDetraccionAsync(empresaId,contextoId,request.ConsultaId,request.Id);
        if(before is null||before.Revision!=request.Revision)return Failure("El comprobante cambió o no está habilitado en la sede y ejercicio seleccionados.");
        using var conn=factory.CreateConnection();if(conn.State!=ConnectionState.Open)conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        if(!await LockCompanyAsync(conn,tx,empresaId))return Failure("La empresa no está habilitada.");
        var context=await ConsultaSedeSql.CargarAsync(conn,empresaId,contextoId,request.ConsultaId,tx);
        if(context.Errores.Count>0)return Failure("El contexto o la sede de consulta cambió. Actualice el listado.");
        var current=await DetraccionAsync(conn,tx,empresaId,request.ConsultaId,request.Id);
        if(current is null||current.Revision!=request.Revision)return Failure("Los datos del comprobante cambiaron. Vuelva a abrirlo antes de guardar.");
        await conn.ExecuteAsync("sp_comprobante_actualizarDetraccion",new{comprobanteId=request.Id,numeroOperacionPago=request.NumeroOperacionPago,montoPago=pago,fechaPago=request.FechaPago!.Value.Date,montoCargo=cargo,usuarioAccionId=userId},tx,commandType:CommandType.StoredProcedure);
        tx.Commit();return(true,"Pago de detracción actualizado.",request.Id);
    }
}
