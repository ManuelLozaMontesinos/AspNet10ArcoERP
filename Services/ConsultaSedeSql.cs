using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services;

internal static class ConsultaSedeSql
{
    internal static async Task<ConsultaSedeContextoModel> CargarAsync(IDbConnection conn,int empresaId,int contextoId,int? consultaId,IDbTransaction? tx=null)
    {
        var result=new ConsultaSedeContextoModel();
        if(empresaId<=0||contextoId<=0){result.Errores.Add("Seleccione un contexto de trabajo válido.");return result;}
        string scopeFrom=@" FROM periodo_emp_sede pes {LOCK} JOIN empresa_sede s ON s.empresa_sede_id=pes.empresa_sede_id
            JOIN periodo p ON p.periodo_id=pes.periodo_id JOIN empresa e ON e.empresa_id=s.empresa_id
            WHERE s.empresa_id=@EmpresaId AND pes.estado='A' AND s.estado='A' AND p.estado='A' AND e.estado='A' ";
        scopeFrom=scopeFrom.Replace("{LOCK}",tx is null?"":"WITH (UPDLOCK,HOLDLOCK)");
        var scope=await conn.QuerySingleOrDefaultAsync<ConsultaSedeContextoModel>("SELECT pes.periodo_id PeriodoId,p.periodo Anio,e.nombre Empresa"+scopeFrom+" AND pes.periodo_emp_sede_id=@Id",new{EmpresaId=empresaId,Id=contextoId},tx);
        if(scope is null){result.Errores.Add("El período y sede de trabajo no están habilitados para la empresa.");return result;}
        result=scope;
        result.Sedes=(await conn.QueryAsync<LookupItemModel>("SELECT pes.periodo_emp_sede_id Id,s.nombre Nombre"+scopeFrom+" AND pes.periodo_id=@PeriodoId ORDER BY s.nombre,pes.periodo_emp_sede_id",new{EmpresaId=empresaId,result.PeriodoId},tx)).AsList();
        if(consultaId.HasValue&&!result.Sedes.Any(s=>s.Id==consultaId))result.Errores.Add("Seleccione una sede activa de la empresa y del mismo ejercicio.");
        return result;
    }
}
