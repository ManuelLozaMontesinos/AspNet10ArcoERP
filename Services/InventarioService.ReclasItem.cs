using System.Data;
using System.Globalization;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class InventarioService
{
    private const string ReclasItemSelect = @"SELECT mr.mov_reclasi_id Id,
        mr.periodo_emp_sede_id PeriodoEmpresaSedeId,
        RTRIM(mr.mov_reclasi) Codigo,
        mr.fecha Fecha,
        mr.mes_contable_id MesContableId,
        mr.mov_almacen_id MovimientoAlmacenId,
        RTRIM(ma.mov_almacen) MovimientoAlmacenCodigo,
        ma.almacen_id AlmacenId,
        RTRIM(a.nombre) AlmacenNombre,
        RTRIM(ISNULL(p.razon_social, '')) ClienteProveedorNombre,
        RTRIM(mr.observacion) Observacion,
        mr.estado_mov_rec_id EstadoMovimientoReclasificacionId,
        RTRIM(emr.estado_mov_rec) EstadoMovimientoReclasificacionCodigo,
        RTRIM(emr.nombre) EstadoMovimientoReclasificacionNombre,
        mr.usuario_autoriza_id UsuarioAutorizaId,
        RTRIM(ISNULL(u_auto.nombre_completo, '')) UsuarioAutorizaNombre,
        mr.fecha_auto FechaAutorizacion,
        (SELECT COUNT(1) FROM mov_alm_prod map WHERE map.mov_almacen_id = mr.mov_almacen_id AND map.estado = 'A') CantidadItems,
        mr.estado Estado,
        COALESCE(mr.fecha_modifica, mr.fecha_creacion) FechaRevision
        FROM mov_reclasi mr
        JOIN mov_almacen ma ON ma.mov_almacen_id = mr.mov_almacen_id
        JOIN almacen a ON a.almacen_id = ma.almacen_id
        JOIN estado_mov_rec emr ON emr.estado_mov_rec_id = mr.estado_mov_rec_id
        LEFT JOIN proveedor p ON p.proveedor_id = ma.proveedor_id
        LEFT JOIN usuario u_auto ON u_auto.usuario_id = mr.usuario_autoriza_id ";

    public async Task<ReclasItemPageModel> GetReclasItemsAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        string? codigo = null,
        int? almacenId = null,
        int? estadoId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new ReclasItemPageModel
        {
            Search = search,
            Codigo = codigo,
            AlmacenId = almacenId,
            EstadoId = estadoId,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        };

        var columns = new Dictionary<string, string>
        {
            ["codigo"] = "mr.mov_reclasi",
            ["fecha"] = "mr.fecha",
            ["almacen"] = "a.nombre",
            ["movimiento"] = "ma.mov_almacen",
            ["cliente"] = "p.razon_social",
            ["estado"] = "emr.nombre"
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

        model.Estados = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT estado_mov_rec_id Id, RTRIM(estado_mov_rec) Codigo, RTRIM(nombre) Nombre
            FROM estado_mov_rec
            ORDER BY estado_mov_rec_id")).AsList();

        var where = new List<string> { "mr.periodo_emp_sede_id = @contextoId" };
        var p = new DynamicParameters();
        p.Add("@contextoId", contextoId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(mr.mov_reclasi LIKE @search OR ma.mov_almacen LIKE @search OR a.nombre LIKE @search OR p.razon_social LIKE @search OR mr.observacion LIKE @search)");
            p.Add("@search", $"%{search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(codigo))
        {
            where.Add("mr.mov_reclasi LIKE @codigo");
            p.Add("@codigo", $"%{codigo.Trim()}%");
        }
        if (almacenId.HasValue && almacenId.Value > 0)
        {
            where.Add("ma.almacen_id = @almacenId");
            p.Add("@almacenId", almacenId.Value);
        }
        if (estadoId.HasValue && estadoId.Value > 0)
        {
            where.Add("mr.estado_mov_rec_id = @estadoId");
            p.Add("@estadoId", estadoId.Value);
        }
        if (fechaDesde.HasValue)
        {
            where.Add("mr.fecha >= @fechaDesde");
            p.Add("@fechaDesde", fechaDesde.Value.Date);
        }
        if (fechaHasta.HasValue)
        {
            where.Add("mr.fecha <= @fechaHasta");
            p.Add("@fechaHasta", fechaHasta.Value.Date.AddDays(1).AddTicks(-1));
        }

        var whereSql = string.Join(" AND ", where);
        var countSql = $@"SELECT COUNT(1)
            FROM mov_reclasi mr
            JOIN mov_almacen ma ON ma.mov_almacen_id = mr.mov_almacen_id
            JOIN almacen a ON a.almacen_id = ma.almacen_id
            LEFT JOIN proveedor p ON p.proveedor_id = ma.proveedor_id
            WHERE {whereSql}";

        var total = await conn.ExecuteScalarAsync<int>(countSql, p);
        model.Paginar(total, page, top);

        var sql = $@"{ReclasItemSelect}
            WHERE {whereSql}
            ORDER BY {columns[model.Sort]} {model.Dir}
            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

        p.Add("@offset", (model.Page - 1) * model.PageSize);
        p.Add("@pageSize", model.PageSize);

        model.Items = (await conn.QueryAsync<ReclasItemModel>(sql, p)).AsList();
        return model;
    }

    public async Task<ReclasItemDetalleModel?> GetReclasItemDetalleAsync(int empresaId, int contextoId, int id)
    {
        if (id <= 0) return null;

        using var conn = factory.CreateConnection();
        var sql = $@"{ReclasItemSelect}
            WHERE mr.mov_reclasi_id = @id AND mr.periodo_emp_sede_id = @contextoId";

        var header = await conn.QueryFirstOrDefaultAsync<ReclasItemDetalleModel>(sql, new { id, contextoId });
        if (header is null) return null;

        header.Productos = await GetReclasItemProductosAsync(empresaId, header.MovimientoAlmacenId);
        return header;
    }

    public async Task<List<LookupItemModel>> BuscarMovimientosParaReclasificarAsync(int empresaId, int contextoId, int almacenId, string? search = null)
    {
        if (almacenId <= 0 || contextoId <= 0) return new();

        using var conn = factory.CreateConnection();
        var where = new List<string>
        {
            "ma.periodo_emp_sede_id = @contextoId",
            "ma.almacen_id = @almacenId",
            "ma.estado_mov_alm_id != 3",
            "ma.estado = 'A'",
            "ta.trans_alma NOT IN ('S05', 'S06')"
        };
        var p = new DynamicParameters();
        p.Add("@contextoId", contextoId);
        p.Add("@almacenId", almacenId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(ma.mov_almacen LIKE @search OR p.razon_social LIKE @search)");
            p.Add("@search", $"%{search.Trim()}%");
        }

        var sql = $@"SELECT TOP 20
            ma.mov_almacen_id Id,
            RTRIM(ma.mov_almacen) Codigo,
            CONCAT(RTRIM(ma.mov_almacen), ' - ', CONVERT(VARCHAR(10), ma.fecha, 103), ' - ', ISNULL(RTRIM(p.razon_social), '')) Nombre
            FROM mov_almacen ma
            JOIN trans_alma ta ON ta.trans_alma_id = ma.trans_alma_id
            LEFT JOIN proveedor p ON p.proveedor_id = ma.proveedor_id
            WHERE {string.Join(" AND ", where)}
            ORDER BY ma.fecha DESC, ma.mov_almacen_id DESC";

        return (await conn.QueryAsync<LookupItemModel>(sql, p)).AsList();
    }

    public async Task<List<ReclasItemProductoModel>> GetReclasItemProductosAsync(int empresaId, int movimientoAlmacenId)
    {
        if (movimientoAlmacenId <= 0) return new();

        using var conn = factory.CreateConnection();
        var sql = @"SELECT map.mov_alm_prod_id MovimientoAlmacenProductoId,
            map.mov_almacen_id MovimientoAlmacenId,
            map.producto_id ProductoOrigenId,
            RTRIM(p_orig.producto) ProductoOrigenCodigo,
            RTRIM(p_orig.descripcion) ProductoOrigenNombre,
            RTRIM(ISNULL(um_orig.abrev, '')) UnidadMedidaOrigen,
            map.cantidad CantidadOrigen,
            map.valor_unitario ValorUnitario,
            map.produ_recla_id ProductoDestinoId,
            RTRIM(ISNULL(p_dest.producto, '')) ProductoDestinoCodigo,
            RTRIM(ISNULL(p_dest.descripcion, '')) ProductoDestinoNombre,
            RTRIM(ISNULL(um_dest.abrev, '')) UnidadMedidaDestino,
            map.cant_recla CantidadDestino
            FROM mov_alm_prod map
            JOIN producto p_orig ON p_orig.producto_id = map.producto_id
            LEFT JOIN unid_med um_orig ON um_orig.unid_med_id = p_orig.unid_med_id
            LEFT JOIN producto p_dest ON p_dest.producto_id = map.produ_recla_id
            LEFT JOIN unid_med um_dest ON um_dest.unid_med_id = p_dest.unid_med_id
            WHERE map.mov_almacen_id = @movimientoAlmacenId AND map.estado = 'A'
            ORDER BY p_orig.descripcion";

        return (await conn.QueryAsync<ReclasItemProductoModel>(sql, new { movimientoAlmacenId })).AsList();
    }

    public async Task<(bool success, string message, int id)> SaveReclasItemAsync(
        int empresaId,
        int contextoId,
        int userId,
        ReclasItemGuardarSolicitud model)
    {
        if (model.AlmacenId <= 0)
            return (false, "Seleccione un almacén.", 0);

        if (model.MovimientoAlmacenId <= 0)
            return (false, "Seleccione el movimiento de almacén a reclasificar.", 0);

        using var conn = factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            var periodoInfo = await conn.QueryFirstOrDefaultAsync<(int periodoId, int sedeId)>(@"
                SELECT pes.periodo_id, pes.emp_sede_id
                FROM periodo_emp_sede pes
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

            if (model.Id <= 0)
            {
                // Correlativo empresa_corre tabla_id = 21 (RECLASIFICACION ITEMS)
                var correRow = await conn.QueryFirstOrDefaultAsync<(int id, int correlativo)>(@"
                    SELECT empresa_corre_id, correlativo
                    FROM empresa_corre WITH (UPDLOCK, HOLDLOCK)
                    WHERE empresa_id = @empresaId AND tabla_id = 21",
                    new { empresaId }, tx);

                int nextNum = (correRow.correlativo <= 0 ? 0 : correRow.correlativo) + 1;
                string newCodigo = $"DS{nextNum:D5}";

                if (correRow.id == 0)
                {
                    await conn.ExecuteAsync(@"
                        INSERT INTO empresa_corre (empresa_id, tabla_id, correlativo, usuario_creacion_id, fecha_creacion)
                        VALUES (@empresaId, 21, @nextNum, @userId, GETDATE())",
                        new { empresaId, nextNum, userId }, tx);
                }
                else
                {
                    await conn.ExecuteAsync(@"
                        UPDATE empresa_corre
                        SET correlativo = @nextNum, usuario_modifica_id = @userId, fecha_modifica = GETDATE()
                        WHERE empresa_corre_id = @id",
                        new { nextNum, userId, id = correRow.id }, tx);
                }

                var spParams = new DynamicParameters();
                spParams.Add("@periodoEmpresaSedeId", contextoId);
                spParams.Add("@movimientoReclasificacion", newCodigo);
                spParams.Add("@fecha", model.Fecha.Date);
                spParams.Add("@mesContableId", mesContable.id);
                spParams.Add("@movimientoAlmacenId", model.MovimientoAlmacenId);
                spParams.Add("@observacion", (model.Observacion ?? string.Empty).Trim());
                spParams.Add("@estadoMovimientoReclasificacionId", 1); // 1 = MOVIMIENTO
                spParams.Add("@usuarioAccionId", userId);
                spParams.Add("@newID", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await conn.ExecuteAsync("sp_mov_reclasi_insertar", spParams, tx, commandType: CommandType.StoredProcedure);
                int newId = spParams.Get<int>("@newID");

                tx.Commit();
                return (true, $"Reclasificación {newCodigo} creada correctamente.", newId);
            }
            else
            {
                var existing = await conn.QueryFirstOrDefaultAsync<ReclasItemModel>(@"
                    SELECT mr.mov_reclasi_id Id,
                           mr.periodo_emp_sede_id PeriodoEmpresaSedeId,
                           RTRIM(mr.mov_reclasi) Codigo,
                           mr.mov_almacen_id MovimientoAlmacenId,
                           mr.estado_mov_rec_id EstadoMovimientoReclasificacionId,
                           mr.estado Estado,
                           COALESCE(mr.fecha_modifica, mr.fecha_creacion) FechaRevision
                    FROM mov_reclasi mr WITH (UPDLOCK, HOLDLOCK)
                    WHERE mr.mov_reclasi_id = @id",
                    new { id = model.Id }, tx);

                if (existing is null)
                    return (false, "El registro de reclasificación no existe.", 0);

                if (existing.PeriodoEmpresaSedeId != contextoId)
                    return (false, "El registro pertenece a otro contexto.", 0);

                if (existing.EsAnulado)
                    return (false, "No se puede modificar un registro anulado.", 0);

                if (existing.EsAprobado)
                    return (false, "No se puede modificar un registro ya aprobado.", 0);

                if (!string.IsNullOrEmpty(model.Revision) && existing.Revision != model.Revision)
                    return (false, "El registro fue modificado concurrentemente por otro usuario.", 0);

                var spParams = new DynamicParameters();
                spParams.Add("@movimientoReclasificacionId", model.Id);
                spParams.Add("@periodoEmpresaSedeId", contextoId);
                spParams.Add("@movimientoReclasificacion", existing.Codigo);
                spParams.Add("@fecha", model.Fecha.Date);
                spParams.Add("@mesContableId", mesContable.id);
                spParams.Add("@movimientoAlmacenId", existing.MovimientoAlmacenId);
                spParams.Add("@observacion", (model.Observacion ?? string.Empty).Trim());
                spParams.Add("@estadoMovimientoReclasificacionId", existing.EstadoMovimientoReclasificacionId);
                spParams.Add("@usuarioAccionId", userId);

                await conn.ExecuteAsync("sp_mov_reclasi_actualizar", spParams, tx, commandType: CommandType.StoredProcedure);

                tx.Commit();
                return (true, $"Reclasificación {existing.Codigo} actualizada correctamente.", model.Id);
            }
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, $"Error al guardar reclasificación: {ex.Message}", 0);
        }
    }

    public async Task<(bool success, string message, int id)> SaveReclasItemProductoAsync(
        int empresaId,
        int contextoId,
        int userId,
        ReclasItemProductoGuardarSolicitud model)
    {
        if (model.MovimientoAlmacenProductoId <= 0)
            return (false, "Identificador de item no válido.", 0);

        if (model.ProductoDestinoId <= 0)
            return (false, "Seleccione el producto de reclasificación.", 0);

        if (model.CantidadDestino <= 0)
            return (false, "La cantidad a reclasificar debe ser mayor a cero.", 0);

        using var conn = factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            var spParams = new DynamicParameters();
            spParams.Add("@movimientoAlmacenProductoId", model.MovimientoAlmacenProductoId);
            spParams.Add("@productoReclasificacionId", model.ProductoDestinoId);
            spParams.Add("@cantidadReclasificacion", model.CantidadDestino);
            spParams.Add("@usuarioAccionId", userId);

            await conn.ExecuteAsync("sp_mov_alm_prod_actualizarProductoReclasificacion", spParams, tx, commandType: CommandType.StoredProcedure);

            tx.Commit();
            return (true, "Producto de reclasificación actualizado correctamente.", model.MovimientoAlmacenProductoId);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, $"Error al actualizar producto: {ex.Message}", 0);
        }
    }

    public async Task<(bool success, string message, int id)> AnularReclasItemAsync(
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
            var row = await conn.QueryFirstOrDefaultAsync<(int id, int pesId, string codigo, int estadoId)>(@"
                SELECT mov_reclasi_id, periodo_emp_sede_id, RTRIM(mov_reclasi), estado_mov_rec_id
                FROM mov_reclasi WITH (UPDLOCK, HOLDLOCK)
                WHERE mov_reclasi_id = @id",
                new { id }, tx);

            if (row.id == 0) return (false, "No se encontró el registro.", 0);
            if (row.pesId != contextoId) return (false, "El registro pertenece a otro contexto.", 0);
            if (row.estadoId == 3) return (false, "El registro ya está anulado.", 0);
            if (row.estadoId == 2) return (false, "No se puede anular una reclasificación ya aprobada.", 0);

            var spParams = new DynamicParameters();
            spParams.Add("@movimientoReclasificacionId", id);
            spParams.Add("@usuarioAccionId", userId);
            await conn.ExecuteAsync("sp_mov_reclasi_eliminar", spParams, tx, commandType: CommandType.StoredProcedure);

            tx.Commit();
            return (true, $"Reclasificación {row.Item3} anulada exitosamente.", id);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, $"Error al anular: {ex.Message}", 0);
        }
    }

    public async Task<(bool success, string message, int id)> SolicitarAprobacionReclasItemAsync(
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
            var row = await conn.QueryFirstOrDefaultAsync<(int id, int pesId, string codigo, int movAlmId, int estadoId)>(@"
                SELECT mov_reclasi_id, periodo_emp_sede_id, RTRIM(mov_reclasi), mov_almacen_id, estado_mov_rec_id
                FROM mov_reclasi WITH (UPDLOCK, HOLDLOCK)
                WHERE mov_reclasi_id = @id",
                new { id }, tx);

            if (row.id == 0) return (false, "No se encontró el registro.", 0);
            if (row.pesId != contextoId) return (false, "El registro pertenece a otro contexto.", 0);
            if (row.estadoId == 3) return (false, "No se puede solicitar aprobación de un registro anulado.", 0);

            var itemsConRecla = await conn.ExecuteScalarAsync<int>(@"
                SELECT COUNT(1) FROM mov_alm_prod
                WHERE mov_almacen_id = @movAlmId AND estado = 'A' AND produ_recla_id IS NOT NULL AND cant_recla > 0",
                new { movAlmId = row.movAlmId }, tx);

            if (itemsConRecla == 0)
                return (false, "Debe configurar al menos un producto destino y cantidad de reclasificación antes de solicitar aprobación.", 0);

            var spParams = new DynamicParameters();
            spParams.Add("@movimientoReclasificacionId", id);
            spParams.Add("@usuarioAccionId", userId);
            spParams.Add("@estadomovReclaId", 5); // 5 = PENDIENTE DE APROBACION

            await conn.ExecuteAsync("sp_mov_reclasi_aprobdesa", spParams, tx, commandType: CommandType.StoredProcedure);

            tx.Commit();
            return (true, $"Reclasificación {row.Item3} enviada a aprobación correctamente.", id);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, $"Error al solicitar aprobación: {ex.Message}", 0);
        }
    }

    public async Task<ReclasItemAutoPageModel> GetReclasItemsAutoAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        string? codigo = null,
        int? almacenId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new ReclasItemAutoPageModel
        {
            Search = search,
            Codigo = codigo,
            AlmacenId = almacenId,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        };

        var columns = new Dictionary<string, string>
        {
            ["codigo"] = "mr.mov_reclasi",
            ["fecha"] = "mr.fecha",
            ["almacen"] = "a.nombre",
            ["movimiento"] = "ma.mov_almacen",
            ["cliente"] = "p.razon_social"
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

        var where = new List<string>
        {
            "mr.periodo_emp_sede_id = @contextoId",
            "mr.estado_mov_rec_id = 5", // 5 = PENDIENTE DE APROBACION
            "mr.estado = 'A'"
        };
        var p = new DynamicParameters();
        p.Add("@contextoId", contextoId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(mr.mov_reclasi LIKE @search OR ma.mov_almacen LIKE @search OR a.nombre LIKE @search OR p.razon_social LIKE @search OR mr.observacion LIKE @search)");
            p.Add("@search", $"%{search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(codigo))
        {
            where.Add("mr.mov_reclasi LIKE @codigo");
            p.Add("@codigo", $"%{codigo.Trim()}%");
        }
        if (almacenId.HasValue && almacenId.Value > 0)
        {
            where.Add("ma.almacen_id = @almacenId");
            p.Add("@almacenId", almacenId.Value);
        }
        if (fechaDesde.HasValue)
        {
            where.Add("mr.fecha >= @fechaDesde");
            p.Add("@fechaDesde", fechaDesde.Value.Date);
        }
        if (fechaHasta.HasValue)
        {
            where.Add("mr.fecha <= @fechaHasta");
            p.Add("@fechaHasta", fechaHasta.Value.Date.AddDays(1).AddTicks(-1));
        }

        var whereSql = string.Join(" AND ", where);
        var countSql = $@"SELECT COUNT(1)
            FROM mov_reclasi mr
            JOIN mov_almacen ma ON ma.mov_almacen_id = mr.mov_almacen_id
            JOIN almacen a ON a.almacen_id = ma.almacen_id
            LEFT JOIN proveedor p ON p.proveedor_id = ma.proveedor_id
            WHERE {whereSql}";

        var total = await conn.ExecuteScalarAsync<int>(countSql, p);
        model.Paginar(total, page, top);

        var sql = $@"{ReclasItemSelect}
            WHERE {whereSql}
            ORDER BY {columns[model.Sort]} {model.Dir}
            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

        p.Add("@offset", (model.Page - 1) * model.PageSize);
        p.Add("@pageSize", model.PageSize);

        model.Items = (await conn.QueryAsync<ReclasItemModel>(sql, p)).AsList();
        return model;
    }

    public async Task<(bool success, string message, int id)> AprobarReclasItemAsync(
        int empresaId,
        int contextoId,
        int userId,
        AprobarReclasItemSolicitud model)
    {
        if (model.Id <= 0) return (false, "Identificador inválido.", 0);

        using var conn = factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            var row = await conn.QueryFirstOrDefaultAsync<(int id, int pesId, string codigo, int movAlmId, int estadoId)>(@"
                SELECT mov_reclasi_id, periodo_emp_sede_id, RTRIM(mov_reclasi), mov_almacen_id, estado_mov_rec_id
                FROM mov_reclasi WITH (UPDLOCK, HOLDLOCK)
                WHERE mov_reclasi_id = @id",
                new { id = model.Id }, tx);

            if (row.id == 0) return (false, "No se encontró el registro.", 0);
            if (row.pesId != contextoId) return (false, "El registro pertenece a otro contexto.", 0);

            var spParams = new DynamicParameters();
            spParams.Add("@movimientoReclasificacionId", model.Id);
            spParams.Add("@usuarioAccionId", userId);
            spParams.Add("@estadomovReclaId", 2); // 2 = APROBADO

            await conn.ExecuteAsync("sp_mov_reclasi_aprobdesa", spParams, tx, commandType: CommandType.StoredProcedure);

            tx.Commit();
            return (true, $"Reclasificación {row.Item3} aprobada correctamente.", model.Id);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, $"Error al aprobar: {ex.Message}", 0);
        }
    }

    public async Task<(bool success, string message, int id)> DesaprobarReclasItemAsync(
        int empresaId,
        int contextoId,
        int userId,
        DesaprobarReclasItemSolicitud model)
    {
        if (model.Id <= 0) return (false, "Identificador inválido.", 0);

        using var conn = factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            var row = await conn.QueryFirstOrDefaultAsync<(int id, int pesId, string codigo, int estadoId)>(@"
                SELECT mov_reclasi_id, periodo_emp_sede_id, RTRIM(mov_reclasi), estado_mov_rec_id
                FROM mov_reclasi WITH (UPDLOCK, HOLDLOCK)
                WHERE mov_reclasi_id = @id",
                new { id = model.Id }, tx);

            if (row.id == 0) return (false, "No se encontró el registro.", 0);
            if (row.pesId != contextoId) return (false, "El registro pertenece a otro contexto.", 0);

            var spParams = new DynamicParameters();
            spParams.Add("@movimientoReclasificacionId", model.Id);
            spParams.Add("@usuarioAccionId", userId);
            spParams.Add("@estadomovReclaId", 4); // 4 = DESAPROBADO

            await conn.ExecuteAsync("sp_mov_reclasi_aprobdesa", spParams, tx, commandType: CommandType.StoredProcedure);

            tx.Commit();
            return (true, $"Reclasificación {row.Item3} desaprobada/rechazada.", model.Id);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, $"Error al desaprobar: {ex.Message}", 0);
        }
    }
}

