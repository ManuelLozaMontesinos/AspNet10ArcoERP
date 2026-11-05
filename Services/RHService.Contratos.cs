using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services;

public partial class RHService
{
    public async Task<ContratoPersonalPageModel> GetContratosPersonalAsync(int empresaId, string? search = null, int? tipoContratoId = null, int page = 1, int top = 25)
    {
        var model = new ContratoPersonalPageModel
        {
            Search = search,
            TipoContratoId = tipoContratoId
        };
        model.Paginar(0, page, top);

        using var conn = _dbConnectionFactory.CreateConnection();

        model.TiposContrato = (await conn.QueryAsync<LookupItemModel>(
            "SELECT tipo_contrato_id Id, tipo_contrato Codigo, nombre Nombre FROM tipo_contrato ORDER BY tipo_contrato_id")).AsList();

        var where = new List<string> { "p.empresa_id = @EmpresaId", "per.estado = 'A'" };
        var args = new DynamicParameters();
        args.Add("EmpresaId", empresaId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(p.razon_social LIKE @Search OR p.num_doc_iden LIKE @Search OR per.personal LIKE @Search)");
            args.Add("Search", $"%{search.Trim()}%");
        }
        if (tipoContratoId.HasValue && tipoContratoId.Value > 0)
        {
            where.Add("per.tipo_contrato_id = @TipoContratoId");
            args.Add("TipoContratoId", tipoContratoId.Value);
        }

        var filter = string.Join(" AND ", where);
        var fromSql = @"FROM personal per
            INNER JOIN proveedor p ON p.proveedor_id = per.proveedor_id
            LEFT JOIN tipo_contrato tc ON tc.tipo_contrato_id = per.tipo_contrato_id
            LEFT JOIN cargo c ON c.cargo_id = per.cargo_id
            WHERE " + filter;

        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) " + fromSql, args);
        model.Paginar(count, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        var selectSql = $@"SELECT
            per.personal_id AS PersonalId,
            RTRIM(per.personal) AS Codigo,
            p.razon_social AS NombreCompleto,
            p.num_doc_iden AS NumeroDocumento,
            ISNULL(c.nombre, '') AS Cargo,
            tc.tipo_contrato_id AS TipoContratoId,
            ISNULL(tc.tipo_contrato, '') AS TipoContratoCodigo,
            ISNULL(tc.nombre, 'Sin contrato asignado') AS TipoContratoNombre,
            per.fecha_ingreso AS FechaIngreso,
            per.fecha_cese AS FechaCese,
            per.estado AS Estado
            {fromSql}
            ORDER BY p.razon_social
            OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";

        model.Items = (await conn.QueryAsync<ContratoPersonalItemModel>(selectSql, args)).AsList();
        model.VisibleCount = model.Items.Count;
        return model;
    }

    public async Task<(bool success, string message)> ActualizarContratoPersonalAsync(int empresaId, int userId, ActualizarContratoSolicitud model)
    {
        if (model.PersonalId <= 0 || model.TipoContratoId <= 0)
            return (false, "Datos del contrato inválidos.");

        using var conn = _dbConnectionFactory.CreateConnection();
        try
        {
            await conn.ExecuteAsync(@"UPDATE personal SET
                tipo_contrato_id = @TipoContratoId,
                fecha_cese = @FechaCese,
                usuario_modifica_id = @UserId,
                fecha_modifica = GETDATE()
                WHERE personal_id = @PersonalId", new
            {
                model.TipoContratoId,
                model.FechaCese,
                UserId = userId,
                model.PersonalId
            });

            return (true, "Contrato laboral actualizado exitosamente.");
        }
        catch (Exception ex)
        {
            return (false, "Error al actualizar contrato: " + ex.Message);
        }
    }

    public async Task<TRegistroExportModel> GetTRegistroDataAsync(int empresaId)
    {
        using var conn = _dbConnectionFactory.CreateConnection();
        var sql = @"SELECT
            per.personal_id AS PersonalId,
            RTRIM(per.personal) AS Codigo,
            p.razon_social AS NombreCompleto,
            p.num_doc_iden AS NumeroDocumento,
            ISNULL(c.nombre, '') AS Cargo,
            tc.tipo_contrato_id AS TipoContratoId,
            ISNULL(tc.tipo_contrato, '01') AS TipoContratoCodigo,
            ISNULL(tc.nombre, 'A PLAZO INDET - D.LEG. 728') AS TipoContratoNombre,
            per.fecha_ingreso AS FechaIngreso,
            per.fecha_cese AS FechaCese,
            per.estado AS Estado
            FROM personal per
            INNER JOIN proveedor p ON p.proveedor_id = per.proveedor_id
            LEFT JOIN tipo_contrato tc ON tc.tipo_contrato_id = per.tipo_contrato_id
            LEFT JOIN cargo c ON c.cargo_id = per.cargo_id
            WHERE p.empresa_id = @EmpresaId AND per.estado = 'A'
            ORDER BY p.razon_social";

        var list = (await conn.QueryAsync<ContratoPersonalItemModel>(sql, new { EmpresaId = empresaId })).AsList();

        return new TRegistroExportModel
        {
            Periodo = DateTime.Today.ToString("yyyyMM"),
            TotalTrabajadores = list.Count,
            Trabajadores = list
        };
    }
}

