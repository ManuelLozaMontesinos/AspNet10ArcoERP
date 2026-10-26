using System;
using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services
{
    public class MKService : IMKService
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public MKService(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<LeadSourcePageModel> GetLeadSourcesAsync(
            string? search = null,
            string? estado = null,
            int page = 1,
            int pageSize = 25)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var safePageSize = pageSize is 25 or 50 or 100 or 500 ? pageSize : 25;
            var safePage = Math.Max(page, 1);
            var safeState = estado?.Trim().ToUpperInvariant();
            if (safeState is not ("A" or "I"))
                safeState = null;

            const string where = @"
                FROM lead_source
                WHERE (@Search IS NULL OR nombre LIKE '%' + @Search + '%')
                  AND (@Estado IS NULL OR estado = @Estado)";
            var parameters = new
            {
                Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                Estado = safeState
            };
            var total = await conn.QuerySingleAsync<int>($"SELECT COUNT(*) {where}", parameters);
            var lastPage = Math.Max(1, (int)Math.Ceiling(total / (double)safePageSize));
            safePage = Math.Min(safePage, lastPage);
            var items = await conn.QueryAsync<LeadSourceModel>(
                $@"SELECT lead_source_id AS Id, nombre AS Nombre, estado AS Estado
                   {where}
                   ORDER BY nombre, lead_source_id
                   OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
                new
                {
                    parameters.Search,
                    parameters.Estado,
                    Offset = (safePage - 1) * safePageSize,
                    PageSize = safePageSize
                });

            return new LeadSourcePageModel
            {
                Items = items.AsList(),
                Total = total,
                Page = safePage,
                PageSize = safePageSize,
                Search = parameters.Search ?? string.Empty,
                Estado = safeState ?? string.Empty
            };
        }

        public async Task<LeadSourceModel?> GetLeadSourceByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<LeadSourceModel>(
                @"SELECT lead_source_id AS Id, nombre AS Nombre, estado AS Estado
                  FROM lead_source
                  WHERE lead_source_id = @Id",
                new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveLeadSourceAsync(
            LeadSourceModel model,
            int userId)
        {
            var name = model.Nombre?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return (false, "Ingrese el nombre del origen.", 0);
            if (name.Length > 100)
                return (false, "El nombre no puede superar los 100 caracteres.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            using var transaction = conn.BeginTransaction();
            try
            {
                if (model.Id <= 0)
                {
                    var id = await conn.QuerySingleAsync<int>(
                        "SELECT NEXT VALUE FOR dbo.lead_source_seq",
                        transaction: transaction);
                    await conn.ExecuteAsync(
                        @"INSERT INTO lead_source
                              (lead_source_id, nombre, estado, usuario_creacion_id, fecha_creacion)
                          VALUES
                              (@Id, @Nombre, 'A', @UserId, GETDATE())",
                        new { Id = id, Nombre = name, UserId = userId },
                        transaction);
                    transaction.Commit();
                    return (true, "Origen de prospecto creado correctamente.", id);
                }

                var changed = await conn.ExecuteAsync(
                    @"UPDATE lead_source
                      SET nombre = @Nombre,
                          usuario_modifica_id = @UserId,
                          fecha_modifica = GETDATE()
                      WHERE lead_source_id = @Id",
                    new { model.Id, Nombre = name, UserId = userId },
                    transaction);
                if (changed == 0)
                {
                    transaction.Rollback();
                    return (false, "El origen de prospecto ya no existe.", 0);
                }

                transaction.Commit();
                return (true, "Origen de prospecto actualizado correctamente.", model.Id);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleLeadSourceAsync(
            int id,
            int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var currentState = await conn.QueryFirstOrDefaultAsync<string>(
                "SELECT estado FROM lead_source WHERE lead_source_id = @Id",
                new { Id = id });
            if (currentState == null)
                return (false, "El origen de prospecto ya no existe.", string.Empty);

            var newState = currentState == "A" ? "I" : "A";
            var changed = await conn.ExecuteAsync(
                @"UPDATE lead_source
                  SET estado = @Estado,
                      usuario_modifica_id = @UserId,
                      fecha_modifica = GETDATE()
                  WHERE lead_source_id = @Id AND estado = @EstadoAnterior",
                new
                {
                    Id = id,
                    Estado = newState,
                    EstadoAnterior = currentState,
                    UserId = userId
                });
            return changed > 0
                ? (true, newState == "A" ? "Origen de prospecto activado." : "Origen de prospecto desactivado.", newState)
                : (false, "El estado del registro cambió; actualice la bandeja e inténtelo nuevamente.", string.Empty);
        }

        public async Task<TipoProfesionPageModel> GetTipoProfesionesAsync(
            string? search = null,
            string? estado = null,
            int page = 1,
            int pageSize = 25)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var safePageSize = pageSize is 25 or 50 or 100 or 500 ? pageSize : 25;
            var safePage = Math.Max(page, 1);
            var safeState = estado?.Trim().ToUpperInvariant();
            if (safeState is not ("A" or "I"))
                safeState = null;

            const string where = @"
                FROM dbo.tipo_prof_ofi
                WHERE (@Search IS NULL OR nombre LIKE '%' + @Search + '%')
                  AND (@Estado IS NULL OR estado = @Estado)";
            var parameters = new
            {
                Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                Estado = safeState
            };
            var total = await conn.QuerySingleAsync<int>($"SELECT COUNT(*) {where}", parameters);
            var lastPage = Math.Max(1, (int)Math.Ceiling(total / (double)safePageSize));
            safePage = Math.Min(safePage, lastPage);
            var items = await conn.QueryAsync<TipoProfesionModel>(
                $@"SELECT tipo_prof_ofi_id AS Id, nombre AS Nombre, estado AS Estado
                   {where}
                   ORDER BY nombre, tipo_prof_ofi_id
                   OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
                new
                {
                    parameters.Search,
                    parameters.Estado,
                    Offset = (safePage - 1) * safePageSize,
                    PageSize = safePageSize
                });

            return new TipoProfesionPageModel
            {
                Items = items.AsList(),
                Total = total,
                Page = safePage,
                PageSize = safePageSize,
                Search = parameters.Search ?? string.Empty,
                Estado = safeState ?? string.Empty
            };
        }

        public async Task<TipoProfesionModel?> GetTipoProfesionByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<TipoProfesionModel>(
                @"SELECT tipo_prof_ofi_id AS Id, nombre AS Nombre, estado AS Estado
                  FROM dbo.tipo_prof_ofi
                  WHERE tipo_prof_ofi_id = @Id",
                new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveTipoProfesionAsync(
            TipoProfesionModel model,
            int userId)
        {
            var name = model.Nombre?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return (false, "Ingrese el nombre de la profesión u oficio.", 0);
            if (name.Length > 50)
                return (false, "El nombre no puede superar los 50 caracteres.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            using var transaction = conn.BeginTransaction();
            if (model.Id <= 0)
            {
                var id = await conn.QuerySingleAsync<int>(
                    "SELECT NEXT VALUE FOR dbo.tipo_prof_ofi_seq",
                    transaction: transaction);
                await conn.ExecuteAsync(
                    @"INSERT INTO dbo.tipo_prof_ofi
                          (tipo_prof_ofi_id, nombre, estado, usuario_creacion_id, fecha_creacion)
                      VALUES
                          (@Id, @Nombre, 'A', @UserId, GETDATE())",
                    new { Id = id, Nombre = name, UserId = userId },
                    transaction);
                transaction.Commit();
                return (true, "Tipo de profesión u oficio creado correctamente.", id);
            }

            var changed = await conn.ExecuteAsync(
                @"UPDATE dbo.tipo_prof_ofi
                  SET nombre = @Nombre,
                      usuario_modifica_id = @UserId,
                      fecha_modifica = GETDATE()
                  WHERE tipo_prof_ofi_id = @Id AND estado = 'A'",
                new { model.Id, Nombre = name, UserId = userId },
                transaction);
            if (changed == 0)
            {
                transaction.Rollback();
                return (false, "El tipo de profesión u oficio ya no existe o está inactivo.", 0);
            }

            transaction.Commit();
            return (true, "Tipo de profesión u oficio actualizado correctamente.", model.Id);
        }

        public async Task<(bool success, string message)> InactivarTipoProfesionAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var changed = await conn.ExecuteAsync(
                @"UPDATE dbo.tipo_prof_ofi
                  SET estado = 'I',
                      usuario_modifica_id = @UserId,
                      fecha_modifica = GETDATE()
                  WHERE tipo_prof_ofi_id = @Id AND estado = 'A'",
                new { Id = id, UserId = userId });
            return changed > 0
                ? (true, "Tipo de profesión u oficio anulado correctamente.")
                : (false, "El registro ya no existe o está inactivo.");
        }

        public async Task<OportunidadVentaPageModel> GetOportunidadesVentaAsync(
            string? search = null,
            string? estado = null,
            int page = 1,
            int pageSize = 25)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var safePageSize = pageSize is 25 or 50 or 100 or 500 ? pageSize : 25;
            var safePage = Math.Max(page, 1);
            var safeState = estado?.Trim().ToUpperInvariant();
            if (safeState is not ("A" or "I"))
                safeState = null;

            const string where = @"
                FROM dbo.oportu_venta
                WHERE (@Search IS NULL OR nombre LIKE '%' + @Search + '%')
                  AND (@Estado IS NULL OR estado = @Estado)";
            var parameters = new
            {
                Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                Estado = safeState
            };
            var total = await conn.QuerySingleAsync<int>($"SELECT COUNT(*) {where}", parameters);
            var lastPage = Math.Max(1, (int)Math.Ceiling(total / (double)safePageSize));
            safePage = Math.Min(safePage, lastPage);
            var items = await conn.QueryAsync<OportunidadVentaModel>(
                $@"SELECT oportu_venta_id AS Id, nombre AS Nombre, estado AS Estado
                   {where}
                   ORDER BY nombre, oportu_venta_id
                   OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
                new
                {
                    parameters.Search,
                    parameters.Estado,
                    Offset = (safePage - 1) * safePageSize,
                    PageSize = safePageSize
                });

            return new OportunidadVentaPageModel
            {
                Items = items.AsList(),
                Total = total,
                Page = safePage,
                PageSize = safePageSize,
                Search = parameters.Search ?? string.Empty,
                Estado = safeState ?? string.Empty
            };
        }

        public async Task<OportunidadVentaModel?> GetOportunidadVentaByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<OportunidadVentaModel>(
                @"SELECT oportu_venta_id AS Id, nombre AS Nombre, estado AS Estado
                  FROM dbo.oportu_venta
                  WHERE oportu_venta_id = @Id",
                new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveOportunidadVentaAsync(
            OportunidadVentaModel model,
            int userId)
        {
            var name = model.Nombre?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return (false, "Ingrese el nombre de la oportunidad.", 0);
            if (name.Length > 100)
                return (false, "El nombre no puede superar los 100 caracteres.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            using var transaction = conn.BeginTransaction();
            try
            {
                if (model.Id <= 0)
                {
                    var id = await conn.QuerySingleAsync<int>(
                        "SELECT NEXT VALUE FOR dbo.oportu_venta_seq",
                        transaction: transaction);
                    await conn.ExecuteAsync(
                        @"INSERT INTO dbo.oportu_venta
                              (oportu_venta_id, nombre, estado, usuario_creacion_id, fecha_creacion)
                          VALUES
                              (@Id, @Nombre, 'A', @UserId, GETDATE())",
                        new { Id = id, Nombre = name, UserId = userId },
                        transaction);
                    transaction.Commit();
                    return (true, "Oportunidad de venta creada correctamente.", id);
                }

                var changed = await conn.ExecuteAsync(
                    @"UPDATE dbo.oportu_venta
                      SET nombre = @Nombre,
                          usuario_modifica_id = @UserId,
                          fecha_modifica = GETDATE()
                      WHERE oportu_venta_id = @Id AND estado = 'A'",
                    new { model.Id, Nombre = name, UserId = userId },
                    transaction);
                if (changed == 0)
                {
                    transaction.Rollback();
                    return (false, "La oportunidad ya no existe o está inactiva.", 0);
                }

                transaction.Commit();
                return (true, "Oportunidad de venta actualizada correctamente.", model.Id);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<(bool success, string message)> InactivarOportunidadVentaAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var changed = await conn.ExecuteAsync(
                @"UPDATE dbo.oportu_venta
                  SET estado = 'I',
                      usuario_modifica_id = @UserId,
                      fecha_modifica = GETDATE()
                  WHERE oportu_venta_id = @Id AND estado = 'A'",
                new { Id = id, UserId = userId });
            return changed > 0
                ? (true, "Oportunidad de venta anulada correctamente.")
                : (false, "La oportunidad ya no existe o está inactiva.");
        }

        public async Task<TipoVentaClientePageModel> GetTiposVentaClienteAsync(
            string? search = null,
            string? estado = null,
            int page = 1,
            int pageSize = 25)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var safePageSize = pageSize is 25 or 50 or 100 or 500 ? pageSize : 25;
            var safePage = Math.Max(page, 1);
            var safeState = estado?.Trim().ToUpperInvariant();
            if (safeState is not ("A" or "I"))
                safeState = null;

            const string where = @"
                FROM dbo.tipo_venta_c
                WHERE (@Search IS NULL OR nombre LIKE '%' + @Search + '%')
                  AND (@Estado IS NULL OR estado = @Estado)";
            var parameters = new
            {
                Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                Estado = safeState
            };
            var total = await conn.QuerySingleAsync<int>($"SELECT COUNT(*) {where}", parameters);
            var lastPage = Math.Max(1, (int)Math.Ceiling(total / (double)safePageSize));
            safePage = Math.Min(safePage, lastPage);
            var items = await conn.QueryAsync<TipoVentaClienteModel>(
                $@"SELECT tipo_venta_c_id AS Id, nombre AS Nombre, estado AS Estado
                   {where}
                   ORDER BY nombre, tipo_venta_c_id
                   OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
                new
                {
                    parameters.Search,
                    parameters.Estado,
                    Offset = (safePage - 1) * safePageSize,
                    PageSize = safePageSize
                });

            return new TipoVentaClientePageModel
            {
                Items = items.AsList(),
                Total = total,
                Page = safePage,
                PageSize = safePageSize,
                Search = parameters.Search ?? string.Empty,
                Estado = safeState ?? string.Empty
            };
        }

        public async Task<TipoVentaClienteModel?> GetTipoVentaClienteByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<TipoVentaClienteModel>(
                @"SELECT tipo_venta_c_id AS Id, nombre AS Nombre, estado AS Estado
                  FROM dbo.tipo_venta_c
                  WHERE tipo_venta_c_id = @Id",
                new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveTipoVentaClienteAsync(
            TipoVentaClienteModel model,
            int userId)
        {
            var name = model.Nombre?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return (false, "Ingrese el nombre del tipo de venta.", 0);
            if (name.Length > 50)
                return (false, "El nombre no puede superar los 50 caracteres.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            using var transaction = conn.BeginTransaction();
            try
            {
                if (model.Id <= 0)
                {
                    var id = await conn.QuerySingleAsync<int>(
                        "SELECT NEXT VALUE FOR dbo.tipo_venta_c_seq",
                        transaction: transaction);
                    await conn.ExecuteAsync(
                        @"INSERT INTO dbo.tipo_venta_c
                              (tipo_venta_c_id, nombre, estado, usuario_creacion_id, fecha_creacion)
                          VALUES
                              (@Id, @Nombre, 'A', @UserId, GETDATE())",
                        new { Id = id, Nombre = name, UserId = userId },
                        transaction);
                    transaction.Commit();
                    return (true, "Tipo de venta creado correctamente.", id);
                }

                var changed = await conn.ExecuteAsync(
                    @"UPDATE dbo.tipo_venta_c
                      SET nombre = @Nombre,
                          usuario_modifica_id = @UserId,
                          fecha_modifica = GETDATE()
                      WHERE tipo_venta_c_id = @Id AND estado = 'A'",
                    new { model.Id, Nombre = name, UserId = userId },
                    transaction);
                if (changed == 0)
                {
                    transaction.Rollback();
                    return (false, "El tipo de venta ya no existe o está inactivo.", 0);
                }

                transaction.Commit();
                return (true, "Tipo de venta actualizado correctamente.", model.Id);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<(bool success, string message)> InactivarTipoVentaClienteAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var used = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM dbo.proveedor WHERE tipo_venta_c_id = @Id AND estado = 'A'",
                new { Id = id });
            if (used > 0)
                return (false, "No se puede anular: el tipo de venta está asignado a clientes o prospectos activos.");

            var changed = await conn.ExecuteAsync(
                @"UPDATE dbo.tipo_venta_c
                  SET estado = 'I',
                      usuario_modifica_id = @UserId,
                      fecha_modifica = GETDATE()
                  WHERE tipo_venta_c_id = @Id AND estado = 'A'",
                new { Id = id, UserId = userId });
            return changed > 0
                ? (true, "Tipo de venta anulado correctamente.")
                : (false, "El tipo de venta ya no existe o está inactivo.");
        }

        public async Task<List<LookupItemModel>> GetLeadSourceLookupsAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var rows = await conn.QueryAsync<LookupItemModel>(
                @"SELECT lead_source_id AS Id, CAST(lead_source_id AS VARCHAR) AS Codigo, nombre AS Nombre
                  FROM dbo.lead_source
                  WHERE estado = 'A'
                  ORDER BY nombre, lead_source_id");
            return rows.AsList();
        }

        public async Task<List<LookupItemModel>> GetOportunidadVentaLookupsAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var rows = await conn.QueryAsync<LookupItemModel>(
                @"SELECT oportu_venta_id AS Id, CAST(oportu_venta_id AS VARCHAR) AS Codigo, nombre AS Nombre
                  FROM dbo.oportu_venta
                  WHERE estado = 'A'
                  ORDER BY nombre, oportu_venta_id");
            return rows.AsList();
        }

        public async Task<List<int>?> GetProspectOpportunityIdsAsync(int prospectoId, int empresaId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var isProspect = await conn.ExecuteScalarAsync<int>(
                @"SELECT COUNT(1)
                  FROM dbo.proveedor
                  WHERE proveedor_id = @ProspectoId AND empresa_id = @EmpresaId AND es_prospecto = 'S'",
                new { ProspectoId = prospectoId, EmpresaId = empresaId });
            if (isProspect == 0)
                return null;

            var ids = await conn.QueryAsync<int>(
                @"SELECT oportu_venta_id
                  FROM dbo.prove_opo
                  WHERE proveedor_id = @ProspectoId AND estado = 'A'
                  ORDER BY oportu_venta_id",
                new { ProspectoId = prospectoId });
            return ids.AsList();
        }

        public async Task<(bool success, string message)> SaveProspectOpportunityIdsAsync(
            int prospectoId,
            int empresaId,
            IEnumerable<int> oportunidadIds,
            int userId)
        {
            var selectedIds = oportunidadIds.Where(id => id > 0).Distinct().ToArray();
            if (selectedIds.Length > 100)
                return (false, "Un prospecto no puede tener más de 100 oportunidades asociadas.");

            using var conn = _dbConnectionFactory.CreateConnection();
            using var transaction = conn.BeginTransaction();
            try
            {
                var isProspect = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(1)
                      FROM dbo.proveedor
                      WHERE proveedor_id = @ProspectoId AND empresa_id = @EmpresaId AND es_prospecto = 'S'",
                    new { ProspectoId = prospectoId, EmpresaId = empresaId },
                    transaction);
                if (isProspect == 0)
                {
                    transaction.Rollback();
                    return (false, "El prospecto no existe o pertenece a otra empresa.");
                }

                var validIds = await conn.QueryAsync<int>(
                    @"SELECT oportu_venta_id
                      FROM dbo.oportu_venta
                      WHERE estado = 'A' AND oportu_venta_id IN @Ids",
                    new { Ids = selectedIds.Length == 0 ? new[] { 0 } : selectedIds },
                    transaction);
                var validSet = validIds.ToHashSet();
                if (validSet.Count != selectedIds.Length)
                {
                    transaction.Rollback();
                    return (false, "Una o más oportunidades no existen o están inactivas.");
                }

                var existingLinks = (await conn.QueryAsync<(int Id, int OportunidadId, string Estado)>(
                    @"SELECT prove_opo_id AS Id, oportu_venta_id AS OportunidadId, estado AS Estado
                      FROM dbo.prove_opo
                      WHERE proveedor_id = @ProspectoId",
                    new { ProspectoId = prospectoId },
                    transaction)).ToList();

                await conn.ExecuteAsync(
                    @"UPDATE dbo.prove_opo
                      SET estado = 'I', usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                      WHERE proveedor_id = @ProspectoId AND estado = 'A'
                        AND oportu_venta_id NOT IN @Ids",
                    new
                    {
                        ProspectoId = prospectoId,
                        UserId = userId,
                        Ids = selectedIds.Length == 0 ? new[] { 0 } : selectedIds
                    },
                    transaction);

                foreach (var opportunityId in selectedIds)
                {
                    var existing = existingLinks.FirstOrDefault(link => link.OportunidadId == opportunityId);
                    if (existing.Id > 0)
                    {
                        if (existing.Estado != "A")
                        {
                            await conn.ExecuteAsync(
                                @"UPDATE dbo.prove_opo
                                  SET estado = 'A', usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                                  WHERE prove_opo_id = @Id",
                                new { existing.Id, UserId = userId },
                                transaction);
                        }
                        continue;
                    }

                    var linkId = await conn.QuerySingleAsync<int>(
                        "SELECT NEXT VALUE FOR dbo.prove_opo_seq",
                        transaction: transaction);
                    await conn.ExecuteAsync(
                        @"INSERT INTO dbo.prove_opo
                              (prove_opo_id, proveedor_id, oportu_venta_id, estado, usuario_creacion_id, fecha_creacion)
                          VALUES
                              (@Id, @ProspectoId, @OportunidadId, 'A', @UserId, GETDATE())",
                        new { Id = linkId, ProspectoId = prospectoId, OportunidadId = opportunityId, UserId = userId },
                        transaction);
                }

                transaction.Commit();
                return (true, "Oportunidades del prospecto actualizadas correctamente.");
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<MerchandisingMovementPageModel> GetMerchandisingMovementsAsync(
            string? search,
            int? transaccionId,
            DateTime? desde,
            DateTime? hasta,
            int periodoAnio,
            int empresaSedeId,
            int page,
            int pageSize)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var safePageSize = pageSize is 25 or 50 or 100 or 500 ? pageSize : 25;
            var safePage = Math.Max(page, 1);
            var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
            var hastaExclusivo = hasta?.Date.AddDays(1);
            const string filters = @"
                FROM dbo.mov_almacen m
                INNER JOIN dbo.trans_alma ta ON ta.trans_alma_id = m.trans_alma_id
                INNER JOIN dbo.tipo_mov_alma tma ON tma.tipo_mov_alma_id = m.tipo_mov_alma_id
                INNER JOIN dbo.estado_mov_alm ema ON ema.estado_mov_alm_id = m.estado_mov_alm_id
                INNER JOIN dbo.almacen a ON a.almacen_id = m.almacen_id
                INNER JOIN dbo.periodo_emp_sede pes ON pes.periodo_emp_sede_id = m.periodo_emp_sede_id
                INNER JOIN dbo.periodo per ON per.periodo_id = pes.periodo_id
                LEFT JOIN dbo.proveedor p ON p.proveedor_id = m.proveedor_id
                LEFT JOIN dbo.usuario u ON u.usuario_id = m.usuario_creacion_id
                WHERE m.almacen_id = 4
                  AND pes.empresa_sede_id = @EmpresaSedeId
                  AND per.periodo = @PeriodoAnio
                  AND (@Search IS NULL OR m.mov_almacen LIKE '%' + @Search + '%'
                       OR p.razon_social LIKE '%' + @Search + '%'
                       OR p.numero_ruc LIKE '%' + @Search + '%'
                       OR ta.nombre LIKE '%' + @Search + '%')
                  AND (@TransaccionId IS NULL OR m.trans_alma_id = @TransaccionId)
                  AND (@Desde IS NULL OR m.fecha >= @Desde)
                  AND (@Hasta IS NULL OR m.fecha < @Hasta)";
            var parameters = new
            {
                Search = normalizedSearch,
                TransaccionId = transaccionId > 0 ? transaccionId : null,
                Desde = desde?.Date,
                Hasta = hastaExclusivo,
                PeriodoAnio = periodoAnio,
                EmpresaSedeId = empresaSedeId
            };

            var total = await conn.QuerySingleAsync<int>($"SELECT COUNT(1) {filters}", parameters);
            var lastPage = Math.Max(1, (int)Math.Ceiling(total / (double)safePageSize));
            safePage = Math.Min(safePage, lastPage);
            var items = await conn.QueryAsync<MerchandisingMovementModel>(
                $@"SELECT m.mov_almacen_id AS Id,
                          m.mov_almacen AS Codigo,
                          m.fecha AS Fecha,
                          ta.nombre AS Transaccion,
                          tma.nombre AS TipoMovimiento,
                          a.nombre AS Origen,
                          m.observacion AS Observacion,
                          COALESCE(p.razon_social, LTRIM(RTRIM(CONCAT(p.nombres, ' ', p.apellido_pat, ' ', p.apellido_mat))), '') AS Tercero,
                          COALESCE(p.numero_ruc, '') AS Ruc,
                          ema.nombre AS EstadoMovimiento,
                          m.estado AS Estado,
                          COALESCE(u.usuario_logon, '') AS Usuario
                   {filters}
                   ORDER BY m.fecha DESC, m.mov_almacen_id DESC
                   OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
                new
                {
                    parameters.Search,
                    parameters.TransaccionId,
                    parameters.Desde,
                    parameters.Hasta,
                    parameters.PeriodoAnio,
                    parameters.EmpresaSedeId,
                    Offset = (safePage - 1) * safePageSize,
                    PageSize = safePageSize
                });

            return new MerchandisingMovementPageModel
            {
                Items = items.AsList(),
                Total = total,
                Page = safePage,
                PageSize = safePageSize,
                Search = normalizedSearch ?? string.Empty,
                TransaccionId = parameters.TransaccionId,
                Desde = desde?.Date,
                Hasta = hasta?.Date,
                Transacciones = await GetMerchandisingTransactionsAsync(periodoAnio, empresaSedeId)
            };
        }

        public async Task<MerchandisingMovementDetailModel?> GetMerchandisingMovementDetailAsync(
            int id, int periodoAnio, int empresaSedeId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var movement = await conn.QueryFirstOrDefaultAsync<MerchandisingMovementModel>(
                @"SELECT m.mov_almacen_id AS Id,
                         m.mov_almacen AS Codigo,
                         m.fecha AS Fecha,
                         ta.nombre AS Transaccion,
                         tma.nombre AS TipoMovimiento,
                         a.nombre AS Origen,
                         m.observacion AS Observacion,
                         COALESCE(p.razon_social, LTRIM(RTRIM(CONCAT(p.nombres, ' ', p.apellido_pat, ' ', p.apellido_mat))), '') AS Tercero,
                         COALESCE(p.numero_ruc, '') AS Ruc,
                         ema.nombre AS EstadoMovimiento,
                         m.estado AS Estado,
                         COALESCE(u.usuario_logon, '') AS Usuario
                  FROM dbo.mov_almacen m
                  INNER JOIN dbo.trans_alma ta ON ta.trans_alma_id = m.trans_alma_id
                  INNER JOIN dbo.tipo_mov_alma tma ON tma.tipo_mov_alma_id = m.tipo_mov_alma_id
                  INNER JOIN dbo.estado_mov_alm ema ON ema.estado_mov_alm_id = m.estado_mov_alm_id
                  INNER JOIN dbo.almacen a ON a.almacen_id = m.almacen_id
                  INNER JOIN dbo.periodo_emp_sede pes ON pes.periodo_emp_sede_id = m.periodo_emp_sede_id
                  INNER JOIN dbo.periodo per ON per.periodo_id = pes.periodo_id
                  LEFT JOIN dbo.proveedor p ON p.proveedor_id = m.proveedor_id
                  LEFT JOIN dbo.usuario u ON u.usuario_id = m.usuario_creacion_id
                  WHERE m.mov_almacen_id = @Id
                    AND m.almacen_id = 4
                    AND pes.empresa_sede_id = @EmpresaSedeId
                    AND per.periodo = @PeriodoAnio",
                new { Id = id, PeriodoAnio = periodoAnio, EmpresaSedeId = empresaSedeId });
            if (movement is null)
                return null;

            var lines = await conn.QueryAsync<MerchandisingMovementLineModel>(
                @"SELECT mp.mov_alm_prod_id AS Id,
                         COALESCE(pr.producto, '') AS CodigoProducto,
                         COALESCE(mp.descripcion, pr.descripcion, '') AS Producto,
                         COALESCE(um.abrev, '') AS UnidadMedida,
                         mp.cantidad AS Cantidad,
                         mp.valor_unitario AS ValorUnitario,
                         mp.cantidad * mp.valor_unitario AS Total
                  FROM dbo.mov_alm_prod mp
                  LEFT JOIN dbo.producto pr ON pr.producto_id = mp.producto_id
                  LEFT JOIN dbo.unidad_medida um ON um.unidad_medida_id = pr.unidad_medida_id
                  WHERE mp.mov_almacen_id = @Id AND mp.estado = 'A'
                  ORDER BY COALESCE(mp.descripcion, pr.descripcion), mp.mov_alm_prod_id",
                new { Id = id });

            return new MerchandisingMovementDetailModel
            {
                Movimiento = movement,
                Detalle = lines.AsList()
            };
        }

        public async Task<List<LookupItemModel>> GetMerchandisingTransactionsAsync(int periodoAnio, int empresaSedeId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var rows = await conn.QueryAsync<LookupItemModel>(
                @"SELECT DISTINCT ta.trans_alma_id AS Id,
                         ta.trans_alma AS Codigo,
                         ta.nombre AS Nombre
                  FROM dbo.mov_almacen m
                  INNER JOIN dbo.trans_alma ta ON ta.trans_alma_id = m.trans_alma_id
                  INNER JOIN dbo.periodo_emp_sede pes ON pes.periodo_emp_sede_id = m.periodo_emp_sede_id
                  INNER JOIN dbo.periodo per ON per.periodo_id = pes.periodo_id
                  WHERE m.almacen_id = 4
                    AND pes.empresa_sede_id = @EmpresaSedeId
                    AND per.periodo = @PeriodoAnio
                    AND ta.estado = 'A'
                  ORDER BY ta.nombre");
            return rows.AsList();
        }
    }
}
