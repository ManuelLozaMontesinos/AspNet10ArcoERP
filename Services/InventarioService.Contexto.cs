using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services;

public partial class InventarioService
{
    private static Task<InventarioReporteScope?> GetInventarioScopeAsync(IDbConnection conn, int empresaId, int scopeId, int userId, IDbTransaction? tx = null, bool bloquear = false)
        => conn.QuerySingleOrDefaultAsync<InventarioReporteScope>(@"
            SELECT pes.periodo_emp_sede_id PeriodoEmpresaSedeId, pes.periodo_id PeriodoId, s.empresa_sede_id EmpresaSedeId,
                   p.periodo Anio, ISNULL(NULLIF(e.nombre_com,''),e.nombre) Empresa, s.nombre Sede,
                   CAST(CASE WHEN EXISTS (
                     SELECT 1 FROM usuario u JOIN rol_usuario ru ON ru.usuario_id=u.usuario_id
                     JOIN rol r ON r.rol_id=ru.rol_id
                     WHERE u.usuario_id=@UserId AND u.estado='A' AND r.estado='A' AND r.rol IN ('S0001','S0002','00020')
                       AND (u.empresa_id IS NULL OR u.empresa_id=@EmpresaId) AND (r.empresa_id IS NULL OR r.empresa_id=@EmpresaId)
                   ) THEN 1 ELSE 0 END AS bit) Administrador
            FROM periodo_emp_sede pes " + (bloquear ? "WITH (UPDLOCK,HOLDLOCK) " : "") + @"
            JOIN empresa_sede s ON s.empresa_sede_id=pes.empresa_sede_id
            JOIN periodo p ON p.periodo_id=pes.periodo_id JOIN empresa e ON e.empresa_id=s.empresa_id
            WHERE pes.periodo_emp_sede_id=@ScopeId AND s.empresa_id=@EmpresaId
              AND pes.estado='A' AND s.estado='A' AND p.estado='A' AND e.estado='A'",
            new { EmpresaId = empresaId, ScopeId = scopeId, UserId = userId }, tx);
}
