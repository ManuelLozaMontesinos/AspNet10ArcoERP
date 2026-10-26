using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services
{
    public class AdministracionService : IAdministracionService
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public AdministracionService(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<PeriodoSistemaPageModel> GetPeriodosAsync(
            int? searchYear = null, int page = 1, int pageSize = 25)
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var safePageSize = pageSize is 25 or 50 or 100 or 500 ? pageSize : 25;
            var safePage = Math.Max(page, 1);
            const string fromWhere = @"
                FROM dbo.periodo
                WHERE (@SearchYear IS NULL OR periodo = @SearchYear)";
            var total = await connection.QuerySingleAsync<int>(
                $"SELECT COUNT(1) {fromWhere}",
                new { SearchYear = searchYear });
            var pageCount = Math.Max(1, (int)Math.Ceiling(total / (double)safePageSize));
            safePage = Math.Min(safePage, pageCount);

            var items = await connection.QueryAsync<PeriodoSistemaModel>(
                $@"SELECT periodo_id AS Id,
                          periodo AS Anio,
                          CASE WHEN habi_proc_bat = 'S' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS HabilitarProcesoBatch,
                          estado AS Estado
                   {fromWhere}
                   ORDER BY periodo DESC, periodo_id DESC
                   OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
                new
                {
                    SearchYear = searchYear,
                    Offset = (safePage - 1) * safePageSize,
                    PageSize = safePageSize
                });

            return new PeriodoSistemaPageModel
            {
                Items = items.AsList(),
                Total = total,
                Page = safePage,
                PageSize = safePageSize,
                SearchYear = searchYear
            };
        }

        public async Task<PeriodoSistemaModel?> GetPeriodoAsync(int id)
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<PeriodoSistemaModel>(
                @"SELECT periodo_id AS Id,
                         periodo AS Anio,
                         CASE WHEN habi_proc_bat = 'S' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS HabilitarProcesoBatch,
                         estado AS Estado
                  FROM dbo.periodo
                  WHERE periodo_id = @Id",
                new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SavePeriodoAsync(
            PeriodoSistemaModel model, int userId)
        {
            if (model.Anio is < 1900 or > 9999)
                return (false, "El período debe ser un año de cuatro dígitos entre 1900 y 9999.", 0);
            if (userId <= 0)
                return (false, "No se pudo identificar al usuario que realiza el cambio.", 0);

            using var connection = _dbConnectionFactory.CreateConnection();
            using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                if (model.Id <= 0)
                {
                    var existing = await connection.QueryFirstOrDefaultAsync<int?>(
                        @"SELECT periodo_id
                          FROM dbo.periodo WITH (UPDLOCK, HOLDLOCK)
                          WHERE periodo = @Anio",
                        new { model.Anio },
                        transaction);
                    if (existing.HasValue)
                    {
                        transaction.Rollback();
                        return (false, "Ya existe un período para ese año.", existing.Value);
                    }

                    var id = await connection.QuerySingleAsync<int>(
                        "SELECT NEXT VALUE FOR dbo.periodo_seq",
                        transaction: transaction);
                    await connection.ExecuteAsync(
                        @"INSERT INTO dbo.periodo
                              (periodo_id, periodo, habi_proc_bat, estado, usuario_creacion_id, fecha_creacion)
                          VALUES
                              (@Id, @Anio, @BatchEnabled, 'A', @UserId, GETDATE())",
                        new
                        {
                            Id = id,
                            model.Anio,
                            BatchEnabled = model.HabilitarProcesoBatch ? "S" : "N",
                            UserId = userId
                        },
                        transaction);
                    transaction.Commit();
                    return (true, $"Período {model.Anio} creado correctamente.", id);
                }

                var current = await connection.QueryFirstOrDefaultAsync<PeriodoSistemaModel>(
                    @"SELECT periodo AS Anio, estado AS Estado
                      FROM dbo.periodo WITH (UPDLOCK, HOLDLOCK)
                      WHERE periodo_id = @Id",
                    new { model.Id },
                    transaction);
                if (current is null)
                {
                    transaction.Rollback();
                    return (false, "No se encontró el período solicitado.", 0);
                }
                if (current.Anio != model.Anio)
                {
                    transaction.Rollback();
                    return (false, "El año de un período existente no se puede modificar.", model.Id);
                }
                if (current.Estado != "A")
                {
                    transaction.Rollback();
                    return (false, "No se puede modificar un período inactivo.", model.Id);
                }

                await connection.ExecuteAsync(
                    @"UPDATE dbo.periodo
                      SET habi_proc_bat = @BatchEnabled,
                          usuario_modifica_id = @UserId,
                          fecha_modifica = GETDATE()
                      WHERE periodo_id = @Id AND estado = 'A'",
                    new
                    {
                        model.Id,
                        BatchEnabled = model.HabilitarProcesoBatch ? "S" : "N",
                        UserId = userId
                    },
                    transaction);
                transaction.Commit();
                return (true, $"Período {model.Anio} actualizado correctamente.", model.Id);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
