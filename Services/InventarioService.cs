using System.ComponentModel.DataAnnotations;
using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services;

public partial class InventarioService(IDbConnectionFactory factory, IMaestrosService maestros) : IInventarioService
{
    private static string Text(string? value) => value?.Trim() ?? "";
    private static string? Search(string? value) => string.IsNullOrWhiteSpace(value) ? null : $"%{value.Trim()}%";
    private static string? State(string? value) => value is "A" or "I" ? value : null;
    private static (bool success, string message, int id) Failure(string message) => (false, message, 0);
    private static bool Valid(object model) => Validator.TryValidateObject(model, new ValidationContext(model), new List<ValidationResult>(), true);

    private static Task<int> InsertAsync(IDbConnection conn, IDbTransaction tx, string procedure, object values)
        => MaestroEmpresaSql.InsertAsync(conn, tx, procedure, values);

    private static async Task<bool> LockCompanyAsync(IDbConnection conn, IDbTransaction tx, int empresaId)
        => await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM empresa WITH (UPDLOCK,HOLDLOCK) WHERE empresa_id=@EmpresaId AND estado='A'", new { EmpresaId = empresaId }, tx) == 1;

    private static async Task<AlmacenModel?> LockAlmacenAsync(IDbConnection conn, IDbTransaction tx, int almacenId, int empresaId)
        => await conn.QuerySingleOrDefaultAsync<AlmacenModel>(@"
            SELECT a.almacen_id Id, a.empresa_sede_id EmpresaSedeId
            FROM almacen a WITH (UPDLOCK,HOLDLOCK)
            JOIN empresa_sede s ON s.empresa_sede_id=a.empresa_sede_id
            WHERE a.almacen_id=@AlmacenId AND s.empresa_id=@EmpresaId AND a.estado='A' AND s.estado='A'",
            new { AlmacenId = almacenId, EmpresaId = empresaId }, tx);

    private static async Task<bool> OwnsAlmacenAsync(IDbConnection conn, int almacenId, int empresaId)
        => await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM almacen a JOIN empresa_sede s ON s.empresa_sede_id=a.empresa_sede_id
            WHERE a.almacen_id=@AlmacenId AND s.empresa_id=@EmpresaId", new { AlmacenId = almacenId, EmpresaId = empresaId }) == 1;
}
