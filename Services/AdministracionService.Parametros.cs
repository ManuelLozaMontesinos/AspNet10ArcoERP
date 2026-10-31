using System.Data;
using System.Globalization;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class AdministracionService
{
    // Parameter values are global in the legacy schema. Secret strings never leave the read projection.
    private const string ParametroSelect=@"SELECT ps.parametro_sistema_id Id,ps.grupo_param_sistema_id GrupoId,gps.nombre Grupo,RTRIM(ps.parametro_sistema) Codigo,ps.nombre Nombre,
        td.tipo_dato Tipo,td.nombre TipoNombre,CAST(CASE WHEN ps.tiene_mant='S' THEN 1 ELSE 0 END AS bit) TieneMantenimiento,
        CASE WHEN td.tipo_dato='07' THEN NULL ELSE ps.valor_cadena END Cadena,ps.valor_numerico Numero,ps.rango_num_desde RangoDesde,ps.rango_num_hasta RangoHasta,
        ps.estado Estado,COALESCE(ps.fecha_modifica,ps.fecha_creacion) FechaRevision ";
    private const string ParametroFrom=@" FROM parametro_sistema ps {LOCK} JOIN grupo_param_sistema gps ON gps.grupo_param_sistema_id=ps.grupo_param_sistema_id
        JOIN tipo_dato td ON td.tipo_dato_id=ps.tipo_dato_id ";
    public async Task<ParametroSistemaPageModel> GetParametrosSistemaAsync(int empresaId,int contextoId,string? search=null,int? grupoId=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null)
    {
        var model=new ParametroSistemaPageModel{Search=search,GrupoId=grupoId,Estado=State(estado)};
        var columns=new Dictionary<string,string>{{"grupo","gps.nombre"},{"codigo","ps.parametro_sistema"},{"nombre","ps.nombre"},{"tipo","td.nombre"},{"valor","valor"},{"estado","ps.estado"}};
        model.Ordenar(sort,dir,columns.Keys,"grupo");model.Paginar(0,page,top);
        if(empresaId<=0||contextoId<=0||search?.Length>150||grupoId is <=0){model.Errores.Add("Revise el contexto y los filtros.");return model;}
        using var conn=_dbConnectionFactory.CreateConnection();var context=await ConsultaSedeSql.CargarAsync(conn,empresaId,contextoId,null);
        if(context.Errores.Count>0){model.Errores.AddRange(context.Errores);return model;}
        model.Grupos=(await conn.QueryAsync<LookupItemModel>("SELECT grupo_param_sistema_id Id,nombre Nombre FROM grupo_param_sistema ORDER BY nombre,grupo_param_sistema_id")).AsList();
        if(grupoId.HasValue&&!model.Grupos.Any(g=>g.Id==grupoId)){model.Errores.Add("Seleccione un grupo válido.");return model;}
        var args=new DynamicParameters(new{Search=Search(search),GrupoId=grupoId,Estado=State(estado)});
        string from=ParametroFrom.Replace("{LOCK}","");
        const string where=" WHERE (@Search IS NULL OR ps.nombre LIKE @Search OR ps.parametro_sistema LIKE @Search OR gps.nombre LIKE @Search) AND (@GrupoId IS NULL OR ps.grupo_param_sistema_id=@GrupoId) AND (@Estado IS NULL OR ps.estado=@Estado)";
        model.Paginar(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*)"+from+where,args),page,top);args.Add("Offset",(model.Page-1)*model.PageSize);args.Add("Top",model.PageSize);
        string order=model.Sort=="valor"
            ? $"CASE WHEN td.tipo_dato IN ('02','03') THEN 0 WHEN td.tipo_dato='08' THEN 1 ELSE 2 END {model.Dir},CASE WHEN td.tipo_dato IN ('02','03') THEN ps.valor_numerico WHEN td.tipo_dato='08' THEN ps.rango_num_desde END {model.Dir},CASE WHEN td.tipo_dato='08' THEN ps.rango_num_hasta END {model.Dir},CASE WHEN td.tipo_dato='07' THEN NULL ELSE ps.valor_cadena END {model.Dir}"
            : columns[model.Sort]+" "+model.Dir;
        string tie=string.Join(",",new[]{"gps.nombre","ps.nombre","ps.parametro_sistema_id"}.Where(c=>c!=columns[model.Sort]));
        model.Items=(await conn.QueryAsync<ParametroSistemaModel>(ParametroSelect+from+where+" ORDER BY "+order+","+tie+" OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY",args)).AsList();
        model.VisibleCount=model.Items.Count;return model;
    }

    public async Task<ParametroSistemaModel?> GetParametroSistemaAsync(int empresaId,int contextoId,int id)
    {
        if(empresaId<=0||contextoId<=0||id<=0)return null;using var conn=_dbConnectionFactory.CreateConnection();
        if((await ConsultaSedeSql.CargarAsync(conn,empresaId,contextoId,null)).Errores.Count>0)return null;
        return await conn.QuerySingleOrDefaultAsync<ParametroSistemaModel>(ParametroSelect+ParametroFrom.Replace("{LOCK}","")+" WHERE ps.parametro_sistema_id=@Id",new{Id=id});
    }

    public async Task<(bool success,string message,int id)> SaveParametroSistemaAsync(int empresaId,int contextoId,int userId,ParametroSistemaSolicitud model)
    {
        if(model is null||!Valid(model)||model.ContextoId!=contextoId||empresaId<=0||contextoId<=0||userId<=0)return Failure("Revise los datos enviados y el contexto.");
        using var conn=_dbConnectionFactory.CreateConnection();if(conn.State!=ConnectionState.Open)conn.Open();
        using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        if((await ConsultaSedeSql.CargarAsync(conn,empresaId,contextoId,null,tx)).Errores.Count>0)return Failure("El contexto de trabajo no está habilitado.");
        var current=await conn.QuerySingleOrDefaultAsync<ParametroGuardado>(ParametroSelect+",ps.valor_cadena CadenaGuardada,ps.valor_fecha FechaGuardada"+ParametroFrom.Replace("{LOCK}","WITH (UPDLOCK,HOLDLOCK)")+" WHERE ps.parametro_sistema_id=@Id",new{model.Id},tx);
        if(current is null||!current.Editable)return Failure("El parámetro no permite mantenimiento o no está activo.");
        if(!string.Equals(current.Revision,model.Revision,StringComparison.Ordinal))return Failure("El parámetro cambió. Actualice el listado antes de guardar.");
        string? cadena=current.CadenaGuardada;decimal numero=current.Numero,desde=current.RangoDesde,hasta=current.RangoHasta;
        switch(current.Tipo)
        {
            case "01":cadena=model.Cadena??"";break;
            case "07":
                if(!model.CambiarSecreto||string.IsNullOrEmpty(model.Cadena)||model.Cadena.Length>50)return Failure("Indique explícitamente un nuevo valor oculto de hasta 50 caracteres.");
                cadena=model.Cadena;break;
            case "02":
                if(!model.Numero.HasValue||model.Numero<int.MinValue||model.Numero>int.MaxValue||decimal.Truncate(model.Numero.Value)!=model.Numero)return Failure("Ingrese un entero válido.");
                numero=model.Numero.Value;break;
            case "03":
                if(!model.Numero.HasValue||model.Numero< -999999999999999.9999m||model.Numero>999999999999999.9999m||decimal.Round(model.Numero.Value,4)!=model.Numero)return Failure("Ingrese un número válido con hasta cuatro decimales.");
                numero=model.Numero.Value;break;
            case "08":
                if(!TimeOnly.TryParseExact(model.Desde,"HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out var start)||!TimeOnly.TryParseExact(model.Hasta,"HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out var end))return Failure("Ingrese horas válidas con formato HH:mm.");
                // Legacy stores HH.mm, not decimal hours or elapsed minutes; overnight ranges remain allowed.
                desde=start.Hour+start.Minute/100m;hasta=end.Hour+end.Minute/100m;break;
        }
        // The existing update SP accepts varchar(100), although the table permits 300.
        if(cadena?.Length>100)return Failure("El procedimiento actual admite hasta 100 caracteres; no se puede guardar sin truncar el valor.");
        await conn.ExecuteAsync("sp_parametro_sistema_actualizar",new{ParametroSistemaId=current.Id,Nombre=current.Nombre,RangoNumericoDesde=desde,RangoNumericoHasta=hasta,
            ValorNumerico=numero,ValorFecha=current.FechaGuardada,ValorCadena=cadena,UsuarioAccionId=userId},tx,commandType:CommandType.StoredProcedure);
        tx.Commit();return(true,"Parámetro actualizado.",current.Id);
    }
    private sealed class ParametroGuardado : ParametroSistemaModel
    {
        public string? CadenaGuardada { get; set; }
        public DateTime? FechaGuardada { get; set; }
    }
}
