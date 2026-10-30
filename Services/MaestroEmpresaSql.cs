using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using Dapper;

namespace AspNet10ArcoERP.Services;

internal static class MaestroEmpresaSql
{
    internal static string Text(string? value) => value?.Trim() ?? "";
    internal static string? Search(string? value) => string.IsNullOrWhiteSpace(value) ? null : "%"+value.Trim()+"%";
    internal static string? State(string? value) => value is "A" or "I" ? value : null;
    internal static bool Valid(object value) => Validator.TryValidateObject(value,new ValidationContext(value),new List<ValidationResult>(),true);
    internal static (bool success,string message,int id) Failure(string message) => (false,message,0);
    internal static async Task<int> InsertAsync(IDbConnection conn,IDbTransaction tx,string procedure,object values)
    {
        var args=new DynamicParameters(values); args.Add("newID",dbType:DbType.Int32,direction:ParameterDirection.Output);
        await conn.ExecuteAsync(procedure,args,tx,commandType:CommandType.StoredProcedure);
        int id=args.Get<int>("newID");return id>0?id:throw new InvalidOperationException("El procedimiento no devolvió un identificador válido.");
    }
    internal static Task<bool> LockCompanyAsync(IDbConnection conn,IDbTransaction tx,int empresaId)
        => conn.ExecuteScalarAsync<bool>("SELECT CAST(CASE WHEN COUNT(*)=1 THEN 1 ELSE 0 END AS bit) FROM empresa WITH (UPDLOCK,HOLDLOCK) WHERE empresa_id=@EmpresaId AND estado='A'",new{EmpresaId=empresaId},tx);

    // Only these existing, company-scoped number sequences are supported. No table configuration is changed.
    internal static async Task<string> NextCodeAsync(IDbConnection conn,IDbTransaction tx,int empresaId,int userId,string tablaCodigo)
    {
        var target=tablaCodigo switch { "0013"=>(Table:"vendedor",Column:"vendedor",Length:7),"0014"=>(Table:"trans_alma",Column:"trans_alma",Length:3),"0016"=>(Table:"medio_pago",Column:"medio_pago",Length:2),_=>throw new ArgumentOutOfRangeException(nameof(tablaCodigo)) };
        var config=await conn.QuerySingleOrDefaultAsync<NumberConfig>(@"SELECT tabla_id Id,RTRIM(prefijo) Prefijo,RTRIM(formato_corre) Formato,corre_por_emp PorEmpresa,corre_por_emp_sede PorSede FROM tabla WHERE tabla=@Code AND estado='A'",new{Code=tablaCodigo},tx);
        if(config is null || config.PorEmpresa!="S" || config.PorSede!="N") throw new ValidationException("Revise la configuración existente del correlativo por empresa.");
        var counter=await conn.QuerySingleOrDefaultAsync<NumberCounter>("SELECT empresa_corre_id Id,correlativo Valor FROM empresa_corre WITH (UPDLOCK,HOLDLOCK) WHERE empresa_id=@EmpresaId AND tabla_id=@TablaId",new{EmpresaId=empresaId,TablaId=config.Id},tx);
        if(counter?.Valor==int.MaxValue)throw new ValidationException("Se agotó el correlativo configurado para este maestro.");
        int next=checked((counter?.Valor??0)+1);string code;
        try{code=config.Prefijo+next.ToString(config.Formato,CultureInfo.InvariantCulture);}catch(FormatException){throw new ValidationException("El formato del correlativo no es válido.");}
        if(code.Length>target.Length || code.Length==0)throw new ValidationException("Se agotó el correlativo configurado para este maestro.");
        string duplicate = tablaCodigo == "0013"
            ? "SELECT COUNT(*) FROM vendedor v JOIN proveedor p ON p.proveedor_id=v.proveedor_id WHERE p.empresa_id=@EmpresaId AND v.vendedor=@Codigo"
            : $"SELECT COUNT(*) FROM {target.Table} WHERE empresa_id=@EmpresaId AND {target.Column}=@Codigo";
        if(await conn.ExecuteScalarAsync<int>(duplicate,new{EmpresaId=empresaId,Codigo=code},tx)>0)throw new ValidationException("El correlativo configurado ya existe. Revise su configuración.");
        if(counter is null)await InsertAsync(conn,tx,"sp_empresa_corre_insertar",new{EmpresaId=empresaId,TablaId=config.Id,Correlativo=next,UsuarioAccionId=userId});
        else await conn.ExecuteAsync("sp_empresa_corre_actualizar",new{EmpresaCorrelativoId=counter.Id,Correlativo=next,UsuarioAccionId=userId},tx,commandType:CommandType.StoredProcedure);
        return code;
    }
    private sealed class NumberConfig {public int Id{get;set;}public string Prefijo{get;set;}="";public string Formato{get;set;}="";public string PorEmpresa{get;set;}="";public string PorSede{get;set;}="";}
    private sealed class NumberCounter {public int Id{get;set;}public int Valor{get;set;}}
}
