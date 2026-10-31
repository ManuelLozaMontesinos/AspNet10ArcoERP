using System.Data;
using System.Globalization;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class InventarioService
{
    private const string ControlMermaSelect = @"SELECT ma.mov_almacen_id Id,
        ma.periodo_emp_sede_id PeriodoEmpresaSedeId,
        RTRIM(ma.mov_almacen) Codigo,
        ma.fecha Fecha,
        ma.tipo_mov_alma_id TipoMovimientoAlmacenId,
        RTRIM(tma.tipo_mov_alma) TipoMovimientoAlmacenCodigo,
        RTRIM(tma.nombre) TipoMovimientoAlmacenNombre,
        ma.trans_alma_id TransaccionAlmacenId,
        RTRIM(ta.trans_alma) TransaccionAlmacenCodigo,
        RTRIM(ta.nombre) TransaccionAlmacenNombre,
        ma.almacen_id AlmacenId,
        RTRIM(a.nombre) AlmacenNombre,
        ma.estado_aproba_id EstadoAprobacionId,
        RTRIM(ISNULL(ea.estado_aproba, '')) EstadoAprobacionCodigo,
        RTRIM(ISNULL(ea.nombre, '')) EstadoAprobacionNombre,
        ma.perso_auto_id PersonalAutorizaId,
        RTRIM(ISNULL(pau.razon_social, '')) PersonalAutorizaNombre,
        ma.fecha_auto FechaAutorizacion,
        RTRIM(ma.observacion) Observacion,
        ma.estado_mov_alm_id EstadoMovimientoAlmacenId,
        RTRIM(ema.nombre) EstadoMovimientoAlmacenNombre,
        RTRIM(u.usuario_logon) UsuarioLogon,
        (SELECT COUNT(1) FROM mov_alm_prod map WHERE map.mov_almacen_id = ma.mov_almacen_id AND map.estado = 'A') CantidadItems,
        ISNULL((SELECT SUM(map.cantidad * map.valor_unitario) FROM mov_alm_prod map WHERE map.mov_almacen_id = ma.mov_almacen_id AND map.estado = 'A'), 0) MontoTotal,
        ma.estado Estado,
        COALESCE(ma.fecha_modifica, ma.fecha_creacion) FechaRevision
        FROM mov_almacen ma
        JOIN almacen a ON a.almacen_id = ma.almacen_id
        JOIN trans_alma ta ON ta.trans_alma_id = ma.trans_alma_id
        JOIN tipo_mov_alma tma ON tma.tipo_mov_alma_id = ma.tipo_mov_alma_id
        JOIN estado_mov_alm ema ON ema.estado_mov_alm_id = ma.estado_mov_alm_id
        JOIN usuario u ON u.usuario_id = ma.usuario_creacion_id
        LEFT JOIN estado_aproba ea ON ea.estado_aproba_id = ma.estado_aproba_id
        LEFT JOIN personal per_au ON per_au.personal_id = ma.perso_auto_id
        LEFT JOIN proveedor pau ON pau.proveedor_id = per_au.proveedor_id ";

    public async Task<ControlMermaPageModel> GetControlMermasAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        string? codigo = null,
        int? almacenId = null,
        int? tipoMovimientoAlmacenId = null,
        int? estadoAprobacionId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new ControlMermaPageModel
        {
            Search = search,
            Codigo = codigo,
            AlmacenId = almacenId,
            TipoMovimientoAlmacenId = tipoMovimientoAlmacenId,
            EstadoAprobacionId = estadoAprobacionId,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        };

        var columns = new Dictionary<string, string>
        {
            ["codigo"] = "ma.mov_almacen",
            ["fecha"] = "ma.fecha",
            ["tipo"] = "tma.nombre",
            ["transaccion"] = "ta.nombre",
            ["almacen"] = "a.nombre",
            ["total"] = "MontoTotal",
            ["aprobacion"] = "ea.nombre"
        };
        model.Ordenar(sort, dir, columns.Keys, "fecha", "desc");
        model.Paginar(0, page, top);

        if (empresaId <= 0 || contextoId <= 0)
        {
            model.Errores.Add("Seleccione una empresa y sede válidas.");
            return model;
        }

        using var conn = factory.CreateConnection();

        model.Almacenes = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT a.almacen_id Id, RTRIM(a.abrev) Codigo, RTRIM(a.nombre) Nombre
            FROM almacen a
            JOIN empresa_sede es ON es.empresa_sede_id = a.empresa_sede_id
            WHERE es.empresa_id = @empresaId AND a.estado = 'A'
            ORDER BY a.nombre",
            new { empresaId })).AsList();

        model.TiposMovimiento = new List<LookupItemModel>
        {
            new() { Id = 1, Codigo = "I", Nombre = "Ingreso por merma (S07)" },
            new() { Id = 2, Codigo = "S", Nombre = "Salida por merma (S08)" }
        };

        model.EstadosAprobacion = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT estado_aproba_id Id, RTRIM(estado_aproba) Codigo, RTRIM(nombre) Nombre
            FROM estado_aproba
            WHERE estado = 'A'
            ORDER BY estado_aproba_id")).AsList();

        var where = new List<string>
        {
            "ma.periodo_emp_sede_id = @contextoId",
            "ta.trans_alma IN ('S07', 'S08')"
        };
        var p = new DynamicParameters();
        p.Add("@contextoId", contextoId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(ma.mov_almacen LIKE @search OR a.nombre LIKE @search OR ma.observacion LIKE @search)");
            p.Add("@search", $"%{search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(codigo))
        {
            where.Add("ma.mov_almacen LIKE @codigo");
            p.Add("@codigo", $"%{codigo.Trim()}%");
        }
        if (almacenId.HasValue && almacenId.Value > 0)
        {
            where.Add("ma.almacen_id = @almacenId");
            p.Add("@almacenId", almacenId.Value);
        }
        if (tipoMovimientoAlmacenId.HasValue && tipoMovimientoAlmacenId.Value > 0)
        {
            where.Add("ma.tipo_mov_alma_id = @tipoMovimientoAlmacenId");
            p.Add("@tipoMovimientoAlmacenId", tipoMovimientoAlmacenId.Value);
        }
        if (estadoAprobacionId.HasValue && estadoAprobacionId.Value > 0)
        {
            where.Add("ma.estado_aproba_id = @estadoAprobacionId");
            p.Add("@estadoAprobacionId", estadoAprobacionId.Value);
        }
        if (fechaDesde.HasValue)
        {
            where.Add("ma.fecha >= @fechaDesde");
            p.Add("@fechaDesde", fechaDesde.Value.Date);
        }
        if (fechaHasta.HasValue)
        {
            where.Add("ma.fecha <= @fechaHasta");
            p.Add("@fechaHasta", fechaHasta.Value.Date.AddDays(1).AddTicks(-1));
        }

        var whereSql = string.Join(" AND ", where);
        var countSql = $@"SELECT COUNT(1)
            FROM mov_almacen ma
            JOIN almacen a ON a.almacen_id = ma.almacen_id
            JOIN trans_alma ta ON ta.trans_alma_id = ma.trans_alma_id
            WHERE {whereSql}";

        var total = await conn.ExecuteScalarAsync<int>(countSql, p);
        model.Paginar(total, page, top);

        var sql = $@"{ControlMermaSelect}
            WHERE {whereSql}
            ORDER BY {columns[model.Sort]} {model.Dir}
            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

        p.Add("@offset", (model.Page - 1) * model.PageSize);
        p.Add("@pageSize", model.PageSize);

        model.Items = (await conn.QueryAsync<ControlMermaItemModel>(sql, p)).AsList();
        return model;
    }

    public async Task<MovAlmacenDetalleModel?> GetControlMermaDetalleAsync(int empresaId, int contextoId, int id)
        => await GetMovimientoAlmacenDetalleAsync(empresaId, contextoId, id);

    public async Task<(bool success, string message, int id)> SaveControlMermaAsync(
        int empresaId,
        int contextoId,
        int userId,
        ControlMermaGuardarSolicitud model)
    {
        if (model.AlmacenId <= 0)
            return (false, "Seleccione un almacén.", 0);

        if (model.TipoMovimientoAlmacenId != 1 && model.TipoMovimientoAlmacenId != 2)
            return (false, "El tipo de movimiento por merma debe ser Ingreso o Salida.", 0);

        using var conn = factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            var periodoInfo = await conn.QueryFirstOrDefaultAsync<(int periodoId, int sedeId, string estadoPeriodo)>(@"
                SELECT pes.periodo_id, pes.emp_sede_id, p.estado
                FROM periodo_emp_sede pes
                JOIN periodo p ON p.periodo_id = pes.periodo_id
                WHERE pes.periodo_emp_sede_id = @contextoId",
                new { contextoId }, tx);

            if (periodoInfo.periodoId == 0)
                return (false, "No se encontró el período de trabajo.", 0);

            var mesCod = model.Fecha.ToString("yyyyMM", CultureInfo.InvariantCulture);
            var mesContable = await conn.QueryFirstOrDefaultAsync<(int id, string estado)>(@"
                SELECT mes_contable_id, estado
                FROM mes_contable
                WHERE periodo_id = @periodoId AND mes_contable = @mesCod",
                new { periodoInfo.periodoId, mesCod }, tx);

            if (mesContable.id == 0)
                return (false, $"No existe el mes contable {mesCod} para la fecha indicada.", 0);

            string transAlmaCod = model.TipoMovimientoAlmacenId == 1 ? "S07" : "S08";
            var transAlma = await conn.QueryFirstOrDefaultAsync<(int id, string nombre)>(@"
                SELECT trans_alma_id, nombre
                FROM trans_alma
                WHERE trans_alma = @transAlmaCod AND empresa_id = @empresaId AND estado = 'A'",
                new { transAlmaCod, empresaId }, tx);

            if (transAlma.id == 0)
                return (false, $"No está configurada la transacción de merma {transAlmaCod}.", 0);

            if (model.Id <= 0)
            {
                // Generar correlativo atómico
                var prefix = model.TipoMovimientoAlmacenId == 1 ? "I" : "S";
                var correRow = await conn.QueryFirstOrDefaultAsync<(int id, int correlativo)>(@"
                    SELECT mov_alm_corre_id, correlativo
                    FROM mov_alm_corre WITH (UPDLOCK, HOLDLOCK)
                    WHERE periodo_emp_sede_id = @contextoId
                      AND almacen_id = @almacenId
                      AND tipo_mov_alma_id = @tipoMovId",
                    new { contextoId, almacenId = model.AlmacenId, tipoMovId = model.TipoMovimientoAlmacenId }, tx);

                int nextNum = (correRow.correlativo <= 0 ? 0 : correRow.correlativo) + 1;
                string newCodigo = $"{prefix}{nextNum:D9}";

                if (correRow.id == 0)
                {
                    await conn.ExecuteAsync(@"
                        INSERT INTO mov_alm_corre (periodo_emp_sede_id, almacen_id, tipo_mov_alma_id, correlativo, estado, usuario_creacion_id, fecha_creacion)
                        VALUES (@contextoId, @almacenId, @tipoMovId, @nextNum, 'A', @userId, GETDATE())",
                        new { contextoId, almacenId = model.AlmacenId, tipoMovId = model.TipoMovimientoAlmacenId, nextNum, userId }, tx);
                }
                else
                {
                    await conn.ExecuteAsync(@"
                        UPDATE mov_alm_corre
                        SET correlativo = @nextNum, usuario_modifica_id = @userId, fecha_modifica = GETDATE()
                        WHERE mov_alm_corre_id = @id",
                        new { nextNum, userId, id = correRow.id }, tx);
                }

                var spParams = new DynamicParameters();
                spParams.Add("@periodoEmpresaSedeId", contextoId);
                spParams.Add("@movimientoAlmacen", newCodigo);
                spParams.Add("@fecha", model.Fecha.Date);
                spParams.Add("@tipoMovimientoAlmacenId", model.TipoMovimientoAlmacenId);
                spParams.Add("@transaccionAlmacenId", transAlma.id);
                spParams.Add("@almacenId", model.AlmacenId);
                spParams.Add("@almacenDestinoId", null);
                spParams.Add("@mesContableId", mesContable.id);
                spParams.Add("@afectaSaldo", "N");
                spParams.Add("@proveedorId", null);
                spParams.Add("@personalId", null);
                spParams.Add("@centroCostoId", null);
                spParams.Add("@tipoComprobanteId", null);
                spParams.Add("@serieDocumento", string.Empty);
                spParams.Add("@numeroDocumento", string.Empty);
                spParams.Add("@numeroGuiaRemision", string.Empty);
                spParams.Add("@numeroOrdenCompra", string.Empty);
                spParams.Add("@observacion", (model.Observacion ?? string.Empty).Trim());
                spParams.Add("@tieneGuiaRemision", "N");
                spParams.Add("@guiaRemisionEnModificacion", "N");
                spParams.Add("@comprobanteId", null);
                spParams.Add("@estadoMovimientoAlmacenId", 1); // 1 = MOVIMIENTO
                spParams.Add("@usuarioAccionId", userId);
                spParams.Add("@newID", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await conn.ExecuteAsync("sp_mov_almacen_insertar", spParams, tx, commandType: CommandType.StoredProcedure);
                int newId = spParams.Get<int>("@newID");

                tx.Commit();
                return (true, $"Control de merma {newCodigo} creado satisfactoriamente.", newId);
            }
            else
            {
                var existing = await conn.QueryFirstOrDefaultAsync<ControlMermaItemModel>(@"
                    SELECT ma.mov_almacen_id Id,
                           ma.periodo_emp_sede_id PeriodoEmpresaSedeId,
                           RTRIM(ma.mov_almacen) Codigo,
                           ma.almacen_id AlmacenId,
                           ma.tipo_mov_alma_id TipoMovimientoAlmacenId,
                           ma.trans_alma_id TransaccionAlmacenId,
                           ma.estado_mov_alm_id EstadoMovimientoAlmacenId,
                           ma.estado_aproba_id EstadoAprobacionId,
                           ma.estado Estado,
                           COALESCE(ma.fecha_modifica, ma.fecha_creacion) FechaRevision
                    FROM mov_almacen ma WITH (UPDLOCK, HOLDLOCK)
                    WHERE ma.mov_almacen_id = @id",
                    new { id = model.Id }, tx);

                if (existing is null)
                    return (false, "El control de merma solicitado no existe.", 0);

                if (existing.PeriodoEmpresaSedeId != contextoId)
                    return (false, "El movimiento pertenece a otro período.", 0);

                if (existing.EsAnulado)
                    return (false, "No se puede modificar un control de merma anulado.", 0);

                if (existing.EsAprobado)
                    return (false, "No se puede modificar un control de merma ya aprobado.", 0);

                if (!string.IsNullOrEmpty(model.Revision) && existing.Revision != model.Revision)
                    return (false, "El registro fue modificado concurrentemente por otro usuario.", 0);

                var spParams = new DynamicParameters();
                spParams.Add("@movimientoAlmacenId", model.Id);
                spParams.Add("@periodoEmpresaSedeId", contextoId);
                spParams.Add("@movimientoAlmacen", existing.Codigo);
                spParams.Add("@fecha", model.Fecha.Date);
                spParams.Add("@tipoMovimientoAlmacenId", existing.TipoMovimientoAlmacenId);
                spParams.Add("@transaccionAlmacenId", existing.TransaccionAlmacenId);
                spParams.Add("@almacenId", existing.AlmacenId);
                spParams.Add("@almacenDestinoId", null);
                spParams.Add("@mesContableId", mesContable.id);
                spParams.Add("@afectaSaldo", "N");
                spParams.Add("@proveedorId", null);
                spParams.Add("@personalId", null);
                spParams.Add("@centroCostoId", null);
                spParams.Add("@tipoComprobanteId", null);
                spParams.Add("@serieDocumento", string.Empty);
                spParams.Add("@numeroDocumento", string.Empty);
                spParams.Add("@numeroGuiaRemision", string.Empty);
                spParams.Add("@numeroOrdenCompra", string.Empty);
                spParams.Add("@observacion", (model.Observacion ?? string.Empty).Trim());
                spParams.Add("@tieneGuiaRemision", "N");
                spParams.Add("@guiaRemisionEnModificacion", "N");
                spParams.Add("@comprobanteId", null);
                spParams.Add("@estadoMovimientoAlmacenId", existing.EstadoMovimientoAlmacenId);
                spParams.Add("@usuarioAccionId", userId);

                await conn.ExecuteAsync("sp_mov_almacen_actualizar", spParams, tx, commandType: CommandType.StoredProcedure);

                tx.Commit();
                return (true, $"Control de merma {existing.Codigo} actualizado correctamente.", model.Id);
            }
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, $"Error al guardar control de merma: {ex.Message}", 0);
        }
    }

    public async Task<(bool success, string message, int id)> AnularControlMermaAsync(
        int empresaId,
        int contextoId,
        int userId,
        int id,
        string revision)
    {
        if (id <= 0) return (false, "Identificador inválido.", 0);

        using var conn = factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            var row = await conn.QueryFirstOrDefaultAsync<(int id, int pesId, string codigo, int estadoMovId, int? estadoAprobaId, DateTime? fMod, DateTime fCre)>(@"
                SELECT mov_almacen_id, periodo_emp_sede_id, RTRIM(mov_almacen), estado_mov_alm_id, estado_aproba_id, fecha_modifica, fecha_creacion
                FROM mov_almacen WITH (UPDLOCK, HOLDLOCK)
                WHERE mov_almacen_id = @id",
                new { id }, tx);

            if (row.id == 0) return (false, "No se encontró el registro.", 0);
            if (row.pesId != contextoId) return (false, "El registro pertenece a otro contexto.", 0);
            if (row.estadoMovId == 3) return (false, "El control de merma ya está anulado.", 0);
            if (row.estadoAprobaId == 2) return (false, "No se puede anular un registro ya aprobado.", 0);

            var spParams = new DynamicParameters();
            spParams.Add("@movimientoAlmacenId", id);
            spParams.Add("@usuarioAccionId", userId);
            await conn.ExecuteAsync("sp_mov_almacen_eliminar", spParams, tx, commandType: CommandType.StoredProcedure);

            tx.Commit();
            return (true, $"Control de merma {row.Item3} anulado exitosamente.", id);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, $"Error al anular: {ex.Message}", 0);
        }
    }

    public async Task<(bool success, string message, int id)> SolicitarAprobacionControlMermaAsync(
        int empresaId,
        int contextoId,
        int userId,
        int id,
        string revision)
    {
        if (id <= 0) return (false, "Identificador inválido.", 0);

        using var conn = factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            var row = await conn.QueryFirstOrDefaultAsync<(int id, int pesId, string codigo, int estadoMovId, int? estadoAprobaId)>(@"
                SELECT mov_almacen_id, periodo_emp_sede_id, RTRIM(mov_almacen), estado_mov_alm_id, estado_aproba_id
                FROM mov_almacen WITH (UPDLOCK, HOLDLOCK)
                WHERE mov_almacen_id = @id",
                new { id }, tx);

            if (row.id == 0) return (false, "No se encontró el control de merma.", 0);
            if (row.pesId != contextoId) return (false, "El registro pertenece a otro contexto.", 0);
            if (row.estadoMovId == 3) return (false, "No se puede solicitar aprobación de un registro anulado.", 0);

            var itemsCount = await conn.ExecuteScalarAsync<int>(@"
                SELECT COUNT(1) FROM mov_alm_prod WHERE mov_almacen_id = @id AND estado = 'A'",
                new { id }, tx);

            if (itemsCount == 0)
                return (false, "Debe agregar al menos un insumo o producto antes de solicitar aprobación.", 0);

            var estadoPendienteId = await conn.ExecuteScalarAsync<int>(@"
                SELECT estado_aproba_id FROM estado_aproba WHERE estado_aproba IN ('01', 'P')");

            await conn.ExecuteAsync(@"
                UPDATE mov_almacen
                SET estado_aproba_id = @estadoPendienteId,
                    usuario_modifica_id = @userId,
                    fecha_modifica = GETDATE()
                WHERE mov_almacen_id = @id",
                new { estadoPendienteId, userId, id }, tx);

            tx.Commit();
            return (true, $"Control de merma {row.Item3} enviado a aprobación exitosamente.", id);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, $"Error al solicitar aprobación: {ex.Message}", 0);
        }
    }

    public async Task<ControlMermaAutoPageModel> GetControlMermasAutoAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        string? codigo = null,
        int? almacenId = null,
        int? tipoMovimientoAlmacenId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new ControlMermaAutoPageModel
        {
            Search = search,
            Codigo = codigo,
            AlmacenId = almacenId,
            TipoMovimientoAlmacenId = tipoMovimientoAlmacenId,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        };

        var columns = new Dictionary<string, string>
        {
            ["codigo"] = "ma.mov_almacen",
            ["fecha"] = "ma.fecha",
            ["tipo"] = "tma.nombre",
            ["transaccion"] = "ta.nombre",
            ["almacen"] = "a.nombre",
            ["total"] = "MontoTotal"
        };
        model.Ordenar(sort, dir, columns.Keys, "fecha", "desc");
        model.Paginar(0, page, top);

        if (empresaId <= 0 || contextoId <= 0)
        {
            model.Errores.Add("Seleccione una empresa y sede válidas.");
            return model;
        }

        using var conn = factory.CreateConnection();

        model.Almacenes = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT a.almacen_id Id, RTRIM(a.abrev) Codigo, RTRIM(a.nombre) Nombre
            FROM almacen a
            JOIN empresa_sede es ON es.empresa_sede_id = a.empresa_sede_id
            WHERE es.empresa_id = @empresaId AND a.estado = 'A'
            ORDER BY a.nombre",
            new { empresaId })).AsList();

        model.TiposMovimiento = new List<LookupItemModel>
        {
            new() { Id = 1, Codigo = "I", Nombre = "Ingreso por merma (S07)" },
            new() { Id = 2, Codigo = "S", Nombre = "Salida por merma (S08)" }
        };

        var where = new List<string>
        {
            "ma.periodo_emp_sede_id = @contextoId",
            "ta.trans_alma IN ('S07', 'S08')",
            "ma.estado = 'A'",
            "(ea.estado_aproba IN ('01', 'P') OR ma.estado_aproba_id = 1)" // PENDIENTE DE APROBACION
        };
        var p = new DynamicParameters();
        p.Add("@contextoId", contextoId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(ma.mov_almacen LIKE @search OR a.nombre LIKE @search OR ma.observacion LIKE @search)");
            p.Add("@search", $"%{search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(codigo))
        {
            where.Add("ma.mov_almacen LIKE @codigo");
            p.Add("@codigo", $"%{codigo.Trim()}%");
        }
        if (almacenId.HasValue && almacenId.Value > 0)
        {
            where.Add("ma.almacen_id = @almacenId");
            p.Add("@almacenId", almacenId.Value);
        }
        if (tipoMovimientoAlmacenId.HasValue && tipoMovimientoAlmacenId.Value > 0)
        {
            where.Add("ma.tipo_mov_alma_id = @tipoMovimientoAlmacenId");
            p.Add("@tipoMovimientoAlmacenId", tipoMovimientoAlmacenId.Value);
        }
        if (fechaDesde.HasValue)
        {
            where.Add("ma.fecha >= @fechaDesde");
            p.Add("@fechaDesde", fechaDesde.Value.Date);
        }
        if (fechaHasta.HasValue)
        {
            where.Add("ma.fecha <= @fechaHasta");
            p.Add("@fechaHasta", fechaHasta.Value.Date.AddDays(1).AddTicks(-1));
        }

        var whereSql = string.Join(" AND ", where);
        var countSql = $@"SELECT COUNT(1)
            FROM mov_almacen ma
            JOIN almacen a ON a.almacen_id = ma.almacen_id
            JOIN trans_alma ta ON ta.trans_alma_id = ma.trans_alma_id
            JOIN tipo_mov_alma tma ON tma.tipo_mov_alma_id = ma.tipo_mov_alma_id
            LEFT JOIN estado_aproba ea ON ea.estado_aproba_id = ma.estado_aproba_id
            WHERE {whereSql}";

        var total = await conn.ExecuteScalarAsync<int>(countSql, p);
        model.Paginar(total, page, top);

        var sql = $@"{ControlMermaSelect}
            WHERE {whereSql}
            ORDER BY {columns[model.Sort]} {model.Dir}
            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

        p.Add("@offset", (model.Page - 1) * model.PageSize);
        p.Add("@pageSize", model.PageSize);

        model.Items = (await conn.QueryAsync<ControlMermaItemModel>(sql, p)).AsList();
        return model;
    }

    public async Task<(bool success, string message, int id)> AprobarControlMermaAsync(
        int empresaId,
        int contextoId,
        int userId,
        AprobarControlMermaSolicitud model)
    {
        if (model.Id <= 0) return (false, "Identificador inválido.", 0);

        using var conn = factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            var row = await conn.QueryFirstOrDefaultAsync<(int id, int pesId, string codigo, int? estadoAprobaId)>(@"
                SELECT mov_almacen_id, periodo_emp_sede_id, RTRIM(mov_almacen), estado_aproba_id
                FROM mov_almacen WITH (UPDLOCK, HOLDLOCK)
                WHERE mov_almacen_id = @id",
                new { id = model.Id }, tx);

            if (row.id == 0) return (false, "No se encontró el registro.", 0);
            if (row.pesId != contextoId) return (false, "El registro pertenece a otro contexto.", 0);

            var estadoAprobadoId = await conn.ExecuteScalarAsync<int>(@"
                SELECT estado_aproba_id FROM estado_aproba WHERE estado_aproba IN ('02', 'A')");

            var personalId = await conn.ExecuteScalarAsync<int?>(@"
                SELECT TOP 1 personal_id FROM personal WHERE usuario_id = @userId AND estado = 'A'",
                new { userId }, tx);

            await conn.ExecuteAsync(@"
                UPDATE mov_almacen
                SET estado_aproba_id = @estadoAprobadoId,
                    perso_auto_id = @personalId,
                    fecha_auto = GETDATE(),
                    usuario_modifica_id = @userId,
                    fecha_modifica = GETDATE()
                WHERE mov_almacen_id = @id",
                new { estadoAprobadoId, personalId, userId, id = model.Id }, tx);

            tx.Commit();
            return (true, $"Control de merma {row.Item3} aprobado satisfactoriamente.", model.Id);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, $"Error al aprobar control de merma: {ex.Message}", 0);
        }
    }

    public async Task<(bool success, string message, int id)> DesaprobarControlMermaAsync(
        int empresaId,
        int contextoId,
        int userId,
        DesaprobarControlMermaSolicitud model)
    {
        if (model.Id <= 0) return (false, "Identificador inválido.", 0);

        using var conn = factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            var row = await conn.QueryFirstOrDefaultAsync<(int id, int pesId, string codigo, int? estadoAprobaId)>(@"
                SELECT mov_almacen_id, periodo_emp_sede_id, RTRIM(mov_almacen), estado_aproba_id
                FROM mov_almacen WITH (UPDLOCK, HOLDLOCK)
                WHERE mov_almacen_id = @id",
                new { id = model.Id }, tx);

            if (row.id == 0) return (false, "No se encontró el registro.", 0);
            if (row.pesId != contextoId) return (false, "El registro pertenece a otro contexto.", 0);

            var estadoDesaprobadoId = await conn.ExecuteScalarAsync<int>(@"
                SELECT estado_aproba_id FROM estado_aproba WHERE estado_aproba IN ('03', 'D')");

            await conn.ExecuteAsync(@"
                UPDATE mov_almacen
                SET estado_aproba_id = @estadoDesaprobadoId,
                    usuario_modifica_id = @userId,
                    fecha_modifica = GETDATE()
                WHERE mov_almacen_id = @id",
                new { estadoDesaprobadoId, userId, id = model.Id }, tx);

            tx.Commit();
            return (true, $"Control de merma {row.Item3} rechazado/desaprobado.", model.Id);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, $"Error al desaprobar control de merma: {ex.Message}", 0);
        }
    }
}
