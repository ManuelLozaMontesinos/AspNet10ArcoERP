using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services
{
    public partial class AdministracionService
    {
        public async Task<EmpresaPageModel> GetEmpresasAsync(
            string? search = null,
            string? estado = null,
            int page = 1,
            int top = 25,
            string? sort = null,
            string? dir = null)
        {
            var columns = new Dictionary<string, string>
            {
                ["id"] = "e.empresa_id",
                ["ruc"] = "e.ruc",
                ["nombre"] = "e.nombre",
                ["nombreComercial"] = "e.nombre_com",
                ["sector"] = "s.nombre",
                ["estado"] = "e.estado"
            };

            var model = new EmpresaPageModel
            {
                Search = search,
                Estado = estado
            };

            model.Ordenar(sort, dir, columns.Keys, "nombre");
            model.Paginar(0, page, top);

            using var conn = _dbConnectionFactory.CreateConnection();

            var where = new List<string>();
            var args = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(search))
            {
                where.Add("(e.ruc LIKE @Search OR e.nombre LIKE @Search OR e.nombre_com LIKE @Search)");
                args.Add("Search", $"%{search.Trim()}%");
            }

            if (!string.IsNullOrWhiteSpace(estado))
            {
                where.Add("e.estado = @Estado");
                args.Add("Estado", estado.Trim().ToUpperInvariant());
            }

            string whereSql = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "";

            string countSql = $@"
                SELECT COUNT(1) 
                FROM dbo.empresa e 
                JOIN dbo.sector s ON s.sector_id = e.sector_id 
                {whereSql}";

            int total = await conn.ExecuteScalarAsync<int>(countSql, args);
            model.Paginar(total, page, top);

            string dataSql = $@"
                SELECT 
                    e.empresa_id AS Id,
                    RTRIM(e.ruc) AS Ruc,
                    RTRIM(e.nombre) AS Nombre,
                    RTRIM(ISNULL(e.nombre_com, '')) AS NombreComercial,
                    e.sector_id AS SectorId,
                    RTRIM(s.nombre) AS Sector,
                    LTRIM(RTRIM(CONCAT(ISNULL(tv.nombre, ''), ' ', ISNULL(e.nombre_via, ''), ' ', ISNULL(e.numero_via, ''), ' ', ISNULL(dis.nombre, '')))) AS Direccion,
                    e.estado AS Estado,
                    (SELECT COUNT(1) FROM dbo.empresa_sede es WHERE es.empresa_id = e.empresa_id AND es.estado = 'A') AS TotalSedes
                FROM dbo.empresa e
                JOIN dbo.sector s ON s.sector_id = e.sector_id
                LEFT JOIN dbo.tipo_via tv ON tv.tipo_via_id = e.tipo_via_id
                LEFT JOIN dbo.distrito dis ON dis.distrito_id = e.distrito_id
                {whereSql}
                ORDER BY {columns[model.Sort]} {model.Dir}
                OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";

            args.Add("Offset", (model.Page - 1) * model.PageSize);
            args.Add("Top", model.PageSize);

            model.Items = (await conn.QueryAsync<EmpresaItemModel>(dataSql, args)).AsList();
            model.VisibleCount = model.Items.Count;
            return model;
        }

        public async Task<EmpresaDetalleModel?> GetEmpresaDetalleAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();

            EmpresaDetalleModel model;

            if (id > 0)
            {
                const string empresaSql = @"
                    SELECT 
                        e.empresa_id AS Id,
                        e.sector_id AS SectorId,
                        RTRIM(e.ruc) AS Ruc,
                        RTRIM(e.nombre) AS Nombre,
                        RTRIM(ISNULL(e.nombre_com, '')) AS NombreComercial,
                        e.tipo_via_id AS TipoViaId,
                        RTRIM(ISNULL(e.nombre_via, '')) AS NombreVia,
                        RTRIM(ISNULL(e.numero_via, '')) AS NumeroVia,
                        RTRIM(ISNULL(e.dpto, '')) AS Dpto,
                        RTRIM(ISNULL(e.interior, '')) AS Interior,
                        RTRIM(ISNULL(e.manzana, '')) AS Manzana,
                        RTRIM(ISNULL(e.lote, '')) AS Lote,
                        RTRIM(ISNULL(e.kilometro, '')) AS Kilometro,
                        RTRIM(ISNULL(e.[block], '')) AS Block,
                        RTRIM(ISNULL(e.etapa, '')) AS Etapa,
                        e.tipo_zona_id AS TipoZonaId,
                        RTRIM(ISNULL(e.nombre_zona, '')) AS NombreZona,
                        e.distrito_id AS DistritoId,
                        e.banco_id AS BancoId,
                        RTRIM(ISNULL(e.num_cta_detrac, '')) AS NumCtaDetrac,
                        e.estado AS Estado
                    FROM dbo.empresa e
                    WHERE e.empresa_id = @Id";

                var item = await conn.QuerySingleOrDefaultAsync<EmpresaDetalleModel>(empresaSql, new { Id = id });
                if (item is null) return null;
                model = item;

                // Cargar Departamento y Provincia desde el Distrito asignado
                if (model.DistritoId > 0)
                {
                    const string ubigeoSql = @"
                        SELECT dis.distrito_id AS DistritoId, pro.provincia_id AS ProvinciaId, dep.departamento_id AS DepartamentoId
                        FROM dbo.distrito dis
                        JOIN dbo.provincia pro ON pro.provincia_id = dis.provincia_id
                        JOIN dbo.departamento dep ON dep.departamento_id = pro.departamento_id
                        WHERE dis.distrito_id = @DistritoId";

                    var ubigeo = await conn.QuerySingleOrDefaultAsync<(int DistritoId, int ProvinciaId, int DepartamentoId)?>(ubigeoSql, new { DistritoId = model.DistritoId });
                    if (ubigeo.HasValue)
                    {
                        model.DepartamentoId = ubigeo.Value.DepartamentoId;
                        model.ProvinciaId = ubigeo.Value.ProvinciaId;
                    }
                }
            }
            else
            {
                model = new EmpresaDetalleModel
                {
                    SectorId = 1,
                    TipoViaId = 1,
                    TipoZonaId = 1,
                    DepartamentoId = 15,
                    ProvinciaId = 128,
                    DistritoId = 1251,
                    Estado = "A"
                };
            }

            // Lookups
            model.Sectores = (await conn.QueryAsync<LookupItemModel>(
                "SELECT sector_id AS Id, RTRIM(nombre) AS Descripcion FROM dbo.sector WHERE estado = 'A' ORDER BY sector_id")).AsList();

            model.TiposVia = (await conn.QueryAsync<LookupItemModel>(
                "SELECT tipo_via_id AS Id, RTRIM(nombre) AS Descripcion FROM dbo.tipo_via WHERE estado = 'A' ORDER BY nombre")).AsList();

            model.TiposZona = (await conn.QueryAsync<LookupItemModel>(
                "SELECT tipo_zona_id AS Id, RTRIM(nombre) AS Descripcion FROM dbo.tipo_zona WHERE estado = 'A' ORDER BY nombre")).AsList();

            model.Departamentos = (await conn.QueryAsync<LookupItemModel>(
                "SELECT departamento_id AS Id, RTRIM(departamento) AS Codigo, RTRIM(nombre) AS Descripcion FROM dbo.departamento WHERE estado = 'A' ORDER BY nombre")).AsList();

            if (model.DepartamentoId > 0)
            {
                model.Provincias = await GetProvinciasPorDepartamentoAsync(model.DepartamentoId);
            }

            if (model.ProvinciaId > 0)
            {
                model.Distritos = await GetDistritosPorProvinciaAsync(model.ProvinciaId);
            }

            model.Bancos = (await conn.QueryAsync<LookupItemModel>(
                "SELECT banco_id AS Id, RTRIM(nombre) AS Descripcion FROM dbo.banco WHERE estado = 'A' ORDER BY nombre")).AsList();

            model.Periodos = (await conn.QueryAsync<LookupItemModel>(
                "SELECT periodo_id AS Id, CAST(periodo AS varchar(10)) AS Codigo, CAST(periodo AS varchar(10)) AS Descripcion FROM dbo.periodo WHERE estado = 'A' ORDER BY periodo DESC")).AsList();

            if (id > 0)
            {
                model.Sedes = await GetEmpresaSedesAsync(id);
            }

            return model;
        }

        public async Task<List<LookupItemModel>> GetProvinciasPorDepartamentoAsync(int departamentoId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            return (await conn.QueryAsync<LookupItemModel>(
                @"SELECT provincia_id AS Id, RTRIM(provincia) AS Codigo, RTRIM(nombre) AS Descripcion 
                  FROM dbo.provincia 
                  WHERE departamento_id = @DepId AND estado = 'A' 
                  ORDER BY nombre",
                new { DepId = departamentoId })).AsList();
        }

        public async Task<List<LookupItemModel>> GetDistritosPorProvinciaAsync(int provinciaId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            return (await conn.QueryAsync<LookupItemModel>(
                @"SELECT distrito_id AS Id, RTRIM(distrito) AS Codigo, RTRIM(nombre) AS Descripcion 
                  FROM dbo.distrito 
                  WHERE provincia_id = @ProvId AND estado = 'A' 
                  ORDER BY nombre",
                new { ProvId = provinciaId })).AsList();
        }

        public async Task<(bool success, string message, int id)> SaveEmpresaAsync(int userId, EmpresaGuardarSolicitud model)
        {
            if (string.IsNullOrWhiteSpace(model.Ruc) || model.Ruc.Trim().Length != 11)
                return (false, "El RUC debe tener exactamente 11 dígitos.", model.Id);

            if (string.IsNullOrWhiteSpace(model.Nombre))
                return (false, "La razón social es obligatoria.", model.Id);

            if (model.DistritoId <= 0)
                return (false, "Seleccione un distrito válido para la ubicación fiscal.", model.Id);

            using var conn = _dbConnectionFactory.CreateConnection();

            // Validar RUC único
            int rucExiste = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM dbo.empresa WHERE ruc = @Ruc AND empresa_id <> @Id",
                new { Ruc = model.Ruc.Trim(), Id = model.Id });

            if (rucExiste > 0)
                return (false, $"Ya existe una empresa registrada con el RUC {model.Ruc.Trim()}.", model.Id);

            if (model.Id == 0)
            {
                var p = new DynamicParameters();
                p.Add("sectorId", model.SectorId);
                p.Add("ruc", model.Ruc.Trim());
                p.Add("nombre", model.Nombre.Trim().ToUpperInvariant());
                p.Add("nombreComercial", string.IsNullOrWhiteSpace(model.NombreComercial) ? null : model.NombreComercial.Trim().ToUpperInvariant());
                p.Add("tipoViaId", model.TipoViaId);
                p.Add("nombreVia", model.NombreVia?.Trim().ToUpperInvariant());
                p.Add("numeroVia", model.NumeroVia?.Trim());
                p.Add("departamento", model.Dpto?.Trim());
                p.Add("interior", model.Interior?.Trim());
                p.Add("manzana", model.Manzana?.Trim().ToUpperInvariant());
                p.Add("lote", model.Lote?.Trim().ToUpperInvariant());
                p.Add("kilometro", model.Kilometro?.Trim());
                p.Add("block", model.Block?.Trim().ToUpperInvariant());
                p.Add("etapa", model.Etapa?.Trim().ToUpperInvariant());
                p.Add("tipoZonaId", model.TipoZonaId);
                p.Add("nombreZona", model.NombreZona?.Trim().ToUpperInvariant());
                p.Add("distritoId", model.DistritoId);
                p.Add("numeroCuentaDetraccion", model.NumCtaDetrac?.Trim());
                p.Add("usuarioAccionId", userId);
                p.Add("newID", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await conn.ExecuteAsync("dbo.sp_empresa_insertar", p, commandType: CommandType.StoredProcedure);
                int newId = p.Get<int>("newID");

                // Actualizar banco_id si fue seleccionado
                if (model.BancoId.HasValue && model.BancoId.Value > 0)
                {
                    await conn.ExecuteAsync(
                        "UPDATE dbo.empresa SET banco_id = @BancoId WHERE empresa_id = @Id",
                        new { BancoId = model.BancoId.Value, Id = newId });
                }

                return (true, "Empresa registrada exitosamente.", newId);
            }
            else
            {
                var p = new DynamicParameters();
                p.Add("empresaId", model.Id);
                p.Add("sectorId", model.SectorId);
                p.Add("ruc", model.Ruc.Trim());
                p.Add("nombre", model.Nombre.Trim().ToUpperInvariant());
                p.Add("nombreComercial", string.IsNullOrWhiteSpace(model.NombreComercial) ? null : model.NombreComercial.Trim().ToUpperInvariant());
                p.Add("tipoViaId", model.TipoViaId);
                p.Add("nombreVia", model.NombreVia?.Trim().ToUpperInvariant());
                p.Add("numeroVia", model.NumeroVia?.Trim());
                p.Add("departamento", model.Dpto?.Trim());
                p.Add("interior", model.Interior?.Trim());
                p.Add("manzana", model.Manzana?.Trim().ToUpperInvariant());
                p.Add("lote", model.Lote?.Trim().ToUpperInvariant());
                p.Add("kilometro", model.Kilometro?.Trim());
                p.Add("block", model.Block?.Trim().ToUpperInvariant());
                p.Add("etapa", model.Etapa?.Trim().ToUpperInvariant());
                p.Add("tipoZonaId", model.TipoZonaId);
                p.Add("nombreZona", model.NombreZona?.Trim().ToUpperInvariant());
                p.Add("distritoId", model.DistritoId);
                p.Add("bancoId", model.BancoId > 0 ? model.BancoId : null);
                p.Add("numeroCuentaDetraccion", model.NumCtaDetrac?.Trim());
                p.Add("usuarioAccionId", userId);

                await conn.ExecuteAsync("dbo.sp_empresa_actualizar", p, commandType: CommandType.StoredProcedure);

                return (true, "Empresa actualizada exitosamente.", model.Id);
            }
        }

        public async Task<(bool success, string message)> CambiarEstadoEmpresaAsync(int id, string nuevoEstado)
        {
            if (id <= 0) return (false, "Identificador de empresa inválido.");

            string estado = nuevoEstado == "A" ? "A" : "I";
            using var conn = _dbConnectionFactory.CreateConnection();

            int rows = await conn.ExecuteAsync(
                "UPDATE dbo.empresa SET estado = @Estado WHERE empresa_id = @Id",
                new { Estado = estado, Id = id });

            return rows > 0
                ? (true, $"Empresa {(estado == "A" ? "activada" : "desactivada")} correctamente.")
                : (false, "No se encontró el registro de la empresa.");
        }

        public async Task<List<EmpresaSedeModel>> GetEmpresaSedesAsync(int empresaId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();

            const string sql = @"
                SELECT 
                    es.empresa_sede_id AS Id,
                    es.empresa_id AS EmpresaId,
                    RTRIM(es.empresa_sede) AS Codigo,
                    RTRIM(es.nombre) AS Nombre,
                    RTRIM(ISNULL(es.codigo_sunat, '')) AS CodigoSunat,
                    es.es_central AS EsCentral,
                    es.tipo_via_id AS TipoViaId,
                    RTRIM(ISNULL(es.nombre_via, '')) AS NombreVia,
                    RTRIM(ISNULL(es.numero_via, '')) AS NumeroVia,
                    RTRIM(ISNULL(es.dpto, '')) AS Dpto,
                    RTRIM(ISNULL(es.interior, '')) AS Interior,
                    RTRIM(ISNULL(es.manzana, '')) AS Manzana,
                    RTRIM(ISNULL(es.lote, '')) AS Lote,
                    RTRIM(ISNULL(es.kilometro, '')) AS Kilometro,
                    RTRIM(ISNULL(es.[block], '')) AS Block,
                    RTRIM(ISNULL(es.etapa, '')) AS Etapa,
                    es.tipo_zona_id AS TipoZonaId,
                    RTRIM(ISNULL(es.nombre_zona, '')) AS NombreZona,
                    es.distrito_id AS DistritoId,
                    pro.provincia_id AS ProvinciaId,
                    dep.departamento_id AS DepartamentoId,
                    LTRIM(RTRIM(CONCAT(ISNULL(tv.nombre, ''), ' ', ISNULL(es.nombre_via, ''), ' ', ISNULL(es.numero_via, ''), ' ', ISNULL(dis.nombre, '')))) AS Direccion,
                    RTRIM(ISNULL(es.telefono, '')) AS Telefono,
                    RTRIM(ISNULL(es.telefono2, '')) AS Telefono2,
                    RTRIM(ISNULL(es.email, '')) AS Email,
                    es.estado AS Estado
                FROM dbo.empresa_sede es
                LEFT JOIN dbo.distrito dis ON dis.distrito_id = es.distrito_id
                LEFT JOIN dbo.provincia pro ON pro.provincia_id = dis.provincia_id
                LEFT JOIN dbo.departamento dep ON dep.departamento_id = pro.departamento_id
                LEFT JOIN dbo.tipo_via tv ON tv.tipo_via_id = es.tipo_via_id
                WHERE es.empresa_id = @EmpresaId
                ORDER BY es.es_central DESC, es.empresa_sede ASC";

            return (await conn.QueryAsync<EmpresaSedeModel>(sql, new { EmpresaId = empresaId })).AsList();
        }

        public async Task<(bool success, string message, int id)> SaveEmpresaSedeAsync(int userId, EmpresaSedeSolicitud model)
        {
            if (model.EmpresaId <= 0)
                return (false, "Debe especificar una empresa válida.", 0);

            if (string.IsNullOrWhiteSpace(model.Nombre))
                return (false, "El nombre de la sede es obligatorio.", model.Id);

            if (string.IsNullOrWhiteSpace(model.CodigoSunat) || model.CodigoSunat.Trim().Length != 4)
                return (false, "El código SUNAT debe tener exactamente 4 dígitos.", model.Id);

            if (model.DistritoId <= 0)
                return (false, "Seleccione un distrito válido para la sede.", model.Id);

            using var conn = _dbConnectionFactory.CreateConnection();

            string esCentral = model.EsCentral == "S" ? "S" : "N";

            if (model.Id == 0)
            {
                // Generar correlativo siguiente de 3 dígitos (ej. 001, 002)
                int maxNum = await conn.ExecuteScalarAsync<int>(
                    "SELECT ISNULL(MAX(TRY_CAST(empresa_sede AS int)), 0) FROM dbo.empresa_sede WHERE empresa_id = @EmpresaId",
                    new { EmpresaId = model.EmpresaId });

                string nuevoCodigo = (maxNum + 1).ToString("D3");

                var p = new DynamicParameters();
                p.Add("empresaId", model.EmpresaId);
                p.Add("empresaSede", nuevoCodigo);
                p.Add("nombre", model.Nombre.Trim().ToUpperInvariant());
                p.Add("codigoSunat", model.CodigoSunat.Trim());
                p.Add("esCentral", esCentral);
                p.Add("tipoViaId", model.TipoViaId);
                p.Add("nombreVia", model.NombreVia?.Trim().ToUpperInvariant());
                p.Add("numeroVia", model.NumeroVia?.Trim());
                p.Add("departamento", model.Dpto?.Trim());
                p.Add("interior", model.Interior?.Trim());
                p.Add("manzana", model.Manzana?.Trim().ToUpperInvariant());
                p.Add("lote", model.Lote?.Trim().ToUpperInvariant());
                p.Add("kilometro", model.Kilometro?.Trim());
                p.Add("block", model.Block?.Trim().ToUpperInvariant());
                p.Add("etapa", model.Etapa?.Trim().ToUpperInvariant());
                p.Add("tipoZonaId", model.TipoZonaId);
                p.Add("nombreZona", model.NombreZona?.Trim().ToUpperInvariant());
                p.Add("distritoId", model.DistritoId);
                p.Add("telefono", model.Telefono?.Trim());
                p.Add("telefono2", model.Telefono2?.Trim());
                p.Add("email", model.Email?.Trim());
                p.Add("usuarioAccionId", userId);
                p.Add("newID", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await conn.ExecuteAsync("dbo.sp_empresa_sede_insertar", p, commandType: CommandType.StoredProcedure);
                int newId = p.Get<int>("newID");

                // Si se marcó como sede central, desmarcar las otras sedes
                if (esCentral == "S")
                {
                    await conn.ExecuteAsync(
                        "UPDATE dbo.empresa_sede SET es_central = 'N' WHERE empresa_id = @EmpresaId AND empresa_sede_id <> @Id",
                        new { EmpresaId = model.EmpresaId, Id = newId });
                }

                return (true, $"Sede {nuevoCodigo} creada exitosamente.", newId);
            }
            else
            {
                var p = new DynamicParameters();
                p.Add("empresaSedeId", model.Id);
                p.Add("nombre", model.Nombre.Trim().ToUpperInvariant());
                p.Add("codigoSunat", model.CodigoSunat.Trim());
                p.Add("esCentral", esCentral);
                p.Add("tipoViaId", model.TipoViaId);
                p.Add("nombreVia", model.NombreVia?.Trim().ToUpperInvariant());
                p.Add("numeroVia", model.NumeroVia?.Trim());
                p.Add("departamento", model.Dpto?.Trim());
                p.Add("interior", model.Interior?.Trim());
                p.Add("manzana", model.Manzana?.Trim().ToUpperInvariant());
                p.Add("lote", model.Lote?.Trim().ToUpperInvariant());
                p.Add("kilometro", model.Kilometro?.Trim());
                p.Add("block", model.Block?.Trim().ToUpperInvariant());
                p.Add("etapa", model.Etapa?.Trim().ToUpperInvariant());
                p.Add("tipoZonaId", model.TipoZonaId);
                p.Add("nombreZona", model.NombreZona?.Trim().ToUpperInvariant());
                p.Add("distritoId", model.DistritoId);
                p.Add("telefono", model.Telefono?.Trim());
                p.Add("telefono2", model.Telefono2?.Trim());
                p.Add("email", model.Email?.Trim());
                p.Add("usuarioAccionId", userId);

                await conn.ExecuteAsync("dbo.sp_empresa_sede_actualizar", p, commandType: CommandType.StoredProcedure);

                // Si se marcó como sede central, desmarcar las otras sedes
                if (esCentral == "S")
                {
                    await conn.ExecuteAsync(
                        "UPDATE dbo.empresa_sede SET es_central = 'N' WHERE empresa_id = @EmpresaId AND empresa_sede_id <> @Id",
                        new { EmpresaId = model.EmpresaId, Id = model.Id });
                }

                return (true, "Sede actualizada exitosamente.", model.Id);
            }
        }

        public async Task<(bool success, string message)> CambiarEstadoEmpresaSedeAsync(int id, string nuevoEstado)
        {
            if (id <= 0) return (false, "Identificador de sede inválido.");

            string estado = nuevoEstado == "A" ? "A" : "I";
            using var conn = _dbConnectionFactory.CreateConnection();

            int rows = await conn.ExecuteAsync(
                "UPDATE dbo.empresa_sede SET estado = @Estado WHERE empresa_sede_id = @Id",
                new { Estado = estado, Id = id });

            return rows > 0
                ? (true, $"Sede {(estado == "A" ? "activada" : "desactivada")} correctamente.")
                : (false, "No se encontró el registro de la sede.");
        }

        public async Task<List<PeriodoSedeItemModel>> GetPeriodoSedesAsync(int empresaId, int periodoId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();

            const string sql = @"
                SELECT 
                    pes.periodo_emp_sede_id AS Id,
                    pes.periodo_id AS PeriodoId,
                    p.periodo AS Periodo,
                    pes.empresa_sede_id AS EmpresaSedeId,
                    RTRIM(es.empresa_sede) AS SedeCodigo,
                    RTRIM(es.nombre) AS SedeNombre,
                    pes.estado AS Estado
                FROM dbo.periodo_emp_sede pes
                JOIN dbo.periodo p ON p.periodo_id = pes.periodo_id
                JOIN dbo.empresa_sede es ON es.empresa_sede_id = pes.empresa_sede_id
                WHERE es.empresa_id = @EmpresaId 
                  AND (@PeriodoId <= 0 OR pes.periodo_id = @PeriodoId)
                ORDER BY p.periodo DESC, es.empresa_sede ASC";

            return (await conn.QueryAsync<PeriodoSedeItemModel>(sql, new { EmpresaId = empresaId, PeriodoId = periodoId })).AsList();
        }

        public async Task<(bool success, string message)> AsignarSedePeriodoAsync(int periodoId, int sedeId, int userId)
        {
            if (periodoId <= 0 || sedeId <= 0)
                return (false, "Seleccione un período y una sede válidos.");

            using var conn = _dbConnectionFactory.CreateConnection();

            int existenteId = await conn.ExecuteScalarAsync<int>(
                "SELECT ISNULL(periodo_emp_sede_id, 0) FROM dbo.periodo_emp_sede WHERE periodo_id = @PeriodoId AND empresa_sede_id = @SedeId",
                new { PeriodoId = periodoId, SedeId = sedeId });

            if (existenteId > 0)
            {
                await conn.ExecuteAsync(
                    "UPDATE dbo.periodo_emp_sede SET estado = 'A', usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE periodo_emp_sede_id = @Id",
                    new { Id = existenteId, UserId = userId });
                return (true, "La sede ya estaba vinculada al período y fue reactivada.");
            }

            var p = new DynamicParameters();
            p.Add("periodoId", periodoId);
            p.Add("empresaSedeId", sedeId);
            p.Add("usuarioAccionId", userId);
            p.Add("newID", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("dbo.sp_periodo_emp_sede_insertar", p, commandType: CommandType.StoredProcedure);
            return (true, "Sede asignada al período exitosamente.");
        }

        public async Task<(bool success, string message)> EliminarSedePeriodoAsync(int periodoEmpresaSedeId)
        {
            if (periodoEmpresaSedeId <= 0)
                return (false, "Identificador de asignación inválido.");

            using var conn = _dbConnectionFactory.CreateConnection();

            int rows = await conn.ExecuteAsync(
                "UPDATE dbo.periodo_emp_sede SET estado = 'I', fecha_modifica = GETDATE() WHERE periodo_emp_sede_id = @Id",
                new { Id = periodoEmpresaSedeId });

            return rows > 0
                ? (true, "Asignación de sede retirada del período correctamente.")
                : (false, "No se encontró el registro de asignación.");
        }
    }
}
