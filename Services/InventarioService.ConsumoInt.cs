using System.Data;
using System.Globalization;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class InventarioService
{
    private const string ConsumoIntSelect = @"SELECT ma.mov_almacen_id Id,
        ma.periodo_emp_sede_id PeriodoEmpresaSedeId,
        RTRIM(ma.mov_almacen) Codigo,
        ma.fecha Fecha,
        ma.almacen_id AlmacenId,
        RTRIM(a.nombre) AlmacenNombre,
        ISNULL(ma.centro_costo_id, 0) CentroCostoId,
        RTRIM(ISNULL(cc.nombre, '')) CentroCostoNombre,
        RTRIM(ISNULL(cc.sigla, '')) CentroCostoSigla,
        ISNULL(ma.personal_id, 0) PersonalId,
        RTRIM(ISNULL(pp.razon_social, '')) PersonalNombre,
        COALESCE(NULLIF(pp.numero_ruc,''), pp.num_doc_iden, '') PersonalDocumento,
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
        JOIN estado_mov_alm ema ON ema.estado_mov_alm_id = ma.estado_mov_alm_id
        JOIN usuario u ON u.usuario_id = ma.usuario_creacion_id
        LEFT JOIN centro_costo cc ON cc.centro_costo_id = ma.centro_costo_id
        LEFT JOIN personal per ON per.personal_id = ma.personal_id
        LEFT JOIN proveedor pp ON pp.proveedor_id = per.proveedor_id
        LEFT JOIN estado_aproba ea ON ea.estado_aproba_id = ma.estado_aproba_id
        LEFT JOIN personal per_au ON per_au.personal_id = ma.perso_auto_id
        LEFT JOIN proveedor pau ON pau.proveedor_id = per_au.proveedor_id ";

    public async Task<ConsumoIntPageModel> GetConsumosInternosAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        string? codigo = null,
        int? almacenId = null,
        int? centroCostoId = null,
        int? personalId = null,
        int? estadoAprobacionId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new ConsumoIntPageModel
        {
            Search = search,
            Codigo = codigo,
            AlmacenId = almacenId,
            CentroCostoId = centroCostoId,
            PersonalId = personalId,
            EstadoAprobacionId = estadoAprobacionId,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        };

        var columns = new Dictionary<string, string>
        {
            ["codigo"] = "ma.mov_almacen",
            ["fecha"] = "ma.fecha",
            ["almacen"] = "a.nombre",
            ["centroCosto"] = "cc.nombre",
            ["personal"] = "pp.razon_social",
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
            WHERE es.empresa_id = @EmpresaId AND a.estado = 'A'
            ORDER BY a.nombre",
            new { EmpresaId = empresaId })).AsList();

        model.CentrosCosto = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT centro_costo_id Id, RTRIM(sigla) Codigo, RTRIM(nombre) Nombre
            FROM centro_costo WHERE empresa_id = @EmpresaId AND estado = 'A' ORDER BY nombre",
            new { EmpresaId = empresaId })).AsList();

        model.Personal = await BuscarPersonalAlmacenAsync(empresaId, null);

        model.EstadosAprobacion = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT estado_aproba_id Id, RTRIM(estado_aproba) Codigo, RTRIM(nombre) Nombre
            FROM estado_aproba WHERE estado = 'A' ORDER BY estado_aproba_id")).AsList();

        var where = new List<string> {
            "ma.periodo_emp_sede_id = @ContextoId",
            "ma.tipo_mov_alma_id = 2", // Salida
            "ta.trans_alma = 'S09'"    // Salida por consumo interno
        };

        var args = new DynamicParameters(new
        {
            EmpresaId = empresaId,
            ContextoId = contextoId,
            Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%",
            Codigo = string.IsNullOrWhiteSpace(codigo) ? null : $"%{codigo.Trim()}%",
            AlmacenId = almacenId,
            CentroCostoId = centroCostoId,
            PersonalId = personalId,
            EstadoAprobacionId = estadoAprobacionId,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        });

        if (!string.IsNullOrWhiteSpace(codigo)) where.Add("ma.mov_almacen LIKE @Codigo");
        if (almacenId.HasValue) where.Add("ma.almacen_id = @AlmacenId");
        if (centroCostoId.HasValue) where.Add("ma.centro_costo_id = @CentroCostoId");
        if (personalId.HasValue) where.Add("ma.personal_id = @PersonalId");
        if (estadoAprobacionId.HasValue) where.Add("ma.estado_aproba_id = @EstadoAprobacionId");
        if (fechaDesde.HasValue) where.Add("ma.fecha >= @FechaDesde");
        if (fechaHasta.HasValue) where.Add("ma.fecha <= @FechaHasta");

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add(@"(ma.mov_almacen LIKE @Search
                OR a.nombre LIKE @Search
                OR cc.nombre LIKE @Search
                OR pp.razon_social LIKE @Search
                OR pp.num_doc_iden LIKE @Search
                OR ma.observacion LIKE @Search)");
        }

        string whereSql = " WHERE " + string.Join(" AND ", where);
        string countSql = "SELECT COUNT(1) FROM mov_almacen ma JOIN almacen a ON a.almacen_id = ma.almacen_id JOIN trans_alma ta ON ta.trans_alma_id = ma.trans_alma_id JOIN estado_mov_alm ema ON ema.estado_mov_alm_id = ma.estado_mov_alm_id LEFT JOIN centro_costo cc ON cc.centro_costo_id = ma.centro_costo_id LEFT JOIN personal per ON per.personal_id = ma.personal_id LEFT JOIN proveedor pp ON pp.proveedor_id = per.proveedor_id LEFT JOIN estado_aproba ea ON ea.estado_aproba_id = ma.estado_aproba_id " + whereSql;

        int total = await conn.ExecuteScalarAsync<int>(countSql, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        string orderSql = $" ORDER BY {columns[model.Sort]} {model.Dir}, ma.mov_almacen_id DESC ";
        string dataSql = ConsumoIntSelect + whereSql + orderSql + " OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";

        model.Items = (await conn.QueryAsync<ConsumoIntItemModel>(dataSql, args)).AsList();
        model.VisibleCount = model.Items.Count;
        return model;
    }

    public async Task<MovAlmacenDetalleModel?> GetConsumoInternoDetalleAsync(int empresaId, int contextoId, int id)
    {
        return await GetMovimientoAlmacenDetalleAsync(empresaId, contextoId, id);
    }

    public async Task<List<LookupItemModel>> BuscarPersonalAlmacenAsync(int empresaId, string? search)
    {
        if (empresaId <= 0) return new();
        using var conn = factory.CreateConnection();
        return (await conn.QueryAsync<LookupItemModel>(@"
            SELECT TOP 40 per.personal_id Id,
                RTRIM(p.num_doc_iden) Codigo,
                RTRIM(p.razon_social) Nombre
            FROM personal per
            JOIN proveedor p ON p.proveedor_id = per.proveedor_id
            WHERE per.estado = 'A'
              AND (@Search IS NULL OR p.razon_social LIKE @Search OR p.num_doc_iden LIKE @Search)
            ORDER BY p.razon_social",
            new { Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%" })).AsList();
    }

    public async Task<(bool success, string message, int id)> SaveConsumoInternoAsync(int empresaId, int contextoId, int userId, ConsumoIntGuardarSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.AlmacenId <= 0 || model.CentroCostoId <= 0 || model.PersonalId <= 0)
            return (false, "Seleccione el almacén, el centro de costo y el personal solicitante.", 0);

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId))
            return (false, "La empresa no se encuentra activa.", 0);

        string mesStr = model.Fecha.ToString("MM");
        int mesContableId = await conn.ExecuteScalarAsync<int>(
            "SELECT ISNULL(MAX(mes_contable_id), 1) FROM mes_contable WHERE mes_contable = @Mes",
            new { Mes = mesStr }, tx);
        if (mesContableId == 0) mesContableId = 1;

        int transAlmaId = await conn.ExecuteScalarAsync<int>(
            "SELECT ISNULL(MAX(trans_alma_id), 9) FROM trans_alma WHERE trans_alma = 'S09' AND estado = 'A'",
            null, tx);
        if (transAlmaId == 0) transAlmaId = 9;

        int movId;
        string codigoMov;

        if (model.Id > 0)
        {
            var old = await conn.QuerySingleOrDefaultAsync<ConsumoIntItemModel>(
                ConsumoIntSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE ma.mov_almacen_id = @Id AND ma.periodo_emp_sede_id = @ContextoId",
                new { Id = model.Id, ContextoId = contextoId }, tx);

            if (old is null || old.Estado != "A" || (!string.IsNullOrEmpty(model.Revision) && old.Revision != model.Revision))
                return (false, "El consumo interno no está disponible o fue modificado por otro usuario.", model.Id);

            if (old.EsAprobado || old.EsAnulado)
                return (false, "No se puede modificar un consumo interno aprobado o anulado.", model.Id);

            await conn.ExecuteAsync("sp_mov_almacen_actualizar", new
            {
                movimientoAlmacenId = model.Id,
                proveedorId = (int?)null,
                almacenDestinoId = (int?)null,
                fecha = model.Fecha,
                mesContableId = mesContableId,
                ordenCompraId = (int?)null,
                numeroOrdenCompra = "",
                numeroGuiaRemision = "",
                comprobanteId = (int?)null,
                tipoDocumentoComercialId = (int?)null,
                serieDocumento = "",
                numeroDocumento = "",
                tieneGuiaRemision = "N",
                personalId = model.PersonalId,
                centroCostoId = model.CentroCostoId,
                personalAutorizaId = old.PersonalAutorizaId,
                afectaSaldo = "N",
                observacion = model.Observacion ?? "",
                estadoMovimientoAlmacenId = old.EstadoMovimientoAlmacenId,
                usuarioAccionId = userId
            }, tx, commandType: CommandType.StoredProcedure);

            await conn.ExecuteAsync("sp_mov_almacen_actualizarMesContable", new
            {
                movimientoAlmacenId = model.Id,
                mesContableId = mesContableId,
                usuarioAccionId = userId
            }, tx, commandType: CommandType.StoredProcedure);

            movId = model.Id;
            codigoMov = old.Codigo;
        }
        else
        {
            // Correlativo seguro para Salida (tipo_mov_alma_id = 2)
            var correlaRow = await conn.QuerySingleOrDefaultAsync<(int Id, int Correla)>(@"
                SELECT mov_alm_corre_id Id, correla Correla
                FROM mov_alm_corre WITH (UPDLOCK, HOLDLOCK)
                WHERE periodo_emp_sede_id = @ContextoId
                  AND almacen_id = @AlmacenId
                  AND tipo_mov_alma_id = 2",
                new { ContextoId = contextoId, AlmacenId = model.AlmacenId }, tx);

            int nextNum = (correlaRow.Id > 0 ? correlaRow.Correla : 0) + 1;
            if (correlaRow.Id > 0)
            {
                await conn.ExecuteAsync(@"
                    UPDATE mov_alm_corre
                    SET correla = @Next, usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                    WHERE mov_alm_corre_id = @Id",
                    new { Next = nextNum, UserId = userId, Id = correlaRow.Id }, tx);
            }
            else
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO mov_alm_corre (periodo_emp_sede_id, almacen_id, tipo_mov_alma_id, correla, usuario_creacion_id, fecha_creacion)
                    VALUES (@ContextoId, @AlmacenId, 2, @Next, @UserId, GETDATE())",
                    new { ContextoId = contextoId, AlmacenId = model.AlmacenId, Next = nextNum, UserId = userId }, tx);
            }

            codigoMov = $"S{nextNum:D9}";

            movId = await InsertAsync(conn, tx, "sp_mov_almacen_insertar", new
            {
                periodoEmpresaSedeId = contextoId,
                almacenId = model.AlmacenId,
                transaccionAlmacenId = transAlmaId,
                tipoMovimientoAlmacenId = 2, // Salida
                movimientoAlmacen = codigoMov,
                proveedorId = (int?)null,
                almacenDestinoId = (int?)null,
                movimientoAlmacenReferencialId = (int?)null,
                fecha = model.Fecha,
                mesContableId = mesContableId,
                ordenCompraId = (int?)null,
                numeroOrdenCompra = "",
                numeroGuiaRemision = "",
                comprobanteId = (int?)null,
                tipoDocumentoComercialId = (int?)null,
                serieDocumento = "",
                numeroDocumento = "",
                tieneGuiaRemision = "N",
                guiaRemisionEnModificacion = "N",
                personalId = model.PersonalId,
                centroCostoId = model.CentroCostoId,
                personalAutorizaId = (int?)null,
                movimientoReclasificacionId = (int?)null,
                ordenReclasificacion = 0,
                afectaSaldo = "N",
                observacion = model.Observacion ?? "",
                estadoMovimientoAlmacenId = 1, // 1 = MOVIMIENTO
                usuarioAccionId = userId
            });
        }

        tx.Commit();
        return (true, $"Consumo interno {codigoMov} guardado correctamente.", movId);
    }

    public async Task<(bool success, string message, int id)> AnularConsumoInternoAsync(int empresaId, int contextoId, int userId, int id, string revision)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0) return (false, "Identificador no válido.", 0);
        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return (false, "La empresa no se encuentra activa.", 0);

        var old = await conn.QuerySingleOrDefaultAsync<ConsumoIntItemModel>(
            ConsumoIntSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE ma.mov_almacen_id = @Id AND ma.periodo_emp_sede_id = @ContextoId",
            new { Id = id, ContextoId = contextoId }, tx);

        if (old is null || old.Estado != "A" || (!string.IsNullOrEmpty(revision) && old.Revision != revision))
            return (false, "El consumo interno no está disponible o ya fue modificado.", id);

        if (old.EsAprobado)
            return (false, "No se puede anular un consumo interno que ya fue aprobado.", id);

        await conn.ExecuteAsync("sp_mov_almacen_eliminar", new
        {
            movimientoAlmacenId = id,
            estadoMovimientoAlmacenId = 3, // 3 = ANULADO ('9')
            usuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, $"Consumo interno {old.Codigo} anulado correctamente.", id);
    }

    public async Task<(bool success, string message, int id)> SolicitarAprobacionConsumoInternoAsync(int empresaId, int contextoId, int userId, int id, string revision)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0) return (false, "Identificador no válido.", 0);
        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return (false, "La empresa no está activa.", 0);

        var old = await conn.QuerySingleOrDefaultAsync<ConsumoIntItemModel>(
            ConsumoIntSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE ma.mov_almacen_id = @Id AND ma.periodo_emp_sede_id = @ContextoId",
            new { Id = id, ContextoId = contextoId }, tx);

        if (old is null || old.Estado != "A" || (!string.IsNullOrEmpty(revision) && old.Revision != revision))
            return (false, "El consumo interno no está disponible.", id);

        if (old.CantidadItems == 0)
            return (false, "Debe registrar al menos un producto para solicitar la aprobación.", id);

        await conn.ExecuteAsync("sp_mov_almacen_actualizarEstadoAprobacion", new
        {
            movimientoAlmacenId = id,
            estadoAprobacionId = 1, // 1 = PENDIENTE ('01')
            personalAutorizaId = (int?)null,
            esEditable = "N",
            fechaAutorizacion = (DateTime?)null,
            usuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, $"Aprobación solicitada para el consumo interno {old.Codigo}.", id);
    }

    public async Task<ConsumoIntAutoPageModel> GetConsumosInternosAutoAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        string? codigo = null,
        int? almacenId = null,
        int? centroCostoId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new ConsumoIntAutoPageModel
        {
            Search = search,
            Codigo = codigo,
            AlmacenId = almacenId,
            CentroCostoId = centroCostoId,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        };

        var columns = new Dictionary<string, string>
        {
            ["codigo"] = "ma.mov_almacen",
            ["fecha"] = "ma.fecha",
            ["almacen"] = "a.nombre",
            ["centroCosto"] = "cc.nombre",
            ["personal"] = "pp.razon_social",
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
            WHERE es.empresa_id = @EmpresaId AND a.estado = 'A'
            ORDER BY a.nombre",
            new { EmpresaId = empresaId })).AsList();

        model.CentrosCosto = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT centro_costo_id Id, RTRIM(sigla) Codigo, RTRIM(nombre) Nombre
            FROM centro_costo WHERE empresa_id = @EmpresaId AND estado = 'A' ORDER BY nombre",
            new { EmpresaId = empresaId })).AsList();

        var where = new List<string> {
            "ma.periodo_emp_sede_id = @ContextoId",
            "ma.tipo_mov_alma_id = 2",
            "ta.trans_alma = 'S09'",
            "ma.estado_aproba_id = 1", // Pendiente
            "ma.estado = 'A'"
        };

        var args = new DynamicParameters(new
        {
            EmpresaId = empresaId,
            ContextoId = contextoId,
            Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%",
            Codigo = string.IsNullOrWhiteSpace(codigo) ? null : $"%{codigo.Trim()}%",
            AlmacenId = almacenId,
            CentroCostoId = centroCostoId,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        });

        if (!string.IsNullOrWhiteSpace(codigo)) where.Add("ma.mov_almacen LIKE @Codigo");
        if (almacenId.HasValue) where.Add("ma.almacen_id = @AlmacenId");
        if (centroCostoId.HasValue) where.Add("ma.centro_costo_id = @CentroCostoId");
        if (fechaDesde.HasValue) where.Add("ma.fecha >= @FechaDesde");
        if (fechaHasta.HasValue) where.Add("ma.fecha <= @FechaHasta");

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add(@"(ma.mov_almacen LIKE @Search
                OR a.nombre LIKE @Search
                OR cc.nombre LIKE @Search
                OR pp.razon_social LIKE @Search
                OR pp.num_doc_iden LIKE @Search
                OR ma.observacion LIKE @Search)");
        }

        string whereSql = " WHERE " + string.Join(" AND ", where);
        string countSql = "SELECT COUNT(1) FROM mov_almacen ma JOIN almacen a ON a.almacen_id = ma.almacen_id JOIN trans_alma ta ON ta.trans_alma_id = ma.trans_alma_id JOIN estado_mov_alm ema ON ema.estado_mov_alm_id = ma.estado_mov_alm_id LEFT JOIN centro_costo cc ON cc.centro_costo_id = ma.centro_costo_id LEFT JOIN personal per ON per.personal_id = ma.personal_id LEFT JOIN proveedor pp ON pp.proveedor_id = per.proveedor_id LEFT JOIN estado_aproba ea ON ea.estado_aproba_id = ma.estado_aproba_id " + whereSql;

        int total = await conn.ExecuteScalarAsync<int>(countSql, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        string orderSql = $" ORDER BY {columns[model.Sort]} {model.Dir}, ma.mov_almacen_id DESC ";
        string dataSql = ConsumoIntSelect + whereSql + orderSql + " OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";

        model.Items = (await conn.QueryAsync<ConsumoIntItemModel>(dataSql, args)).AsList();
        model.VisibleCount = model.Items.Count;
        return model;
    }

    public async Task<(bool success, string message, int id)> AprobarConsumoInternoAsync(int empresaId, int contextoId, int userId, AprobarConsumoIntSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.Id <= 0) return (false, "Identificador no válido.", 0);
        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return (false, "La empresa no está activa.", 0);

        var old = await conn.QuerySingleOrDefaultAsync<ConsumoIntItemModel>(
            ConsumoIntSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE ma.mov_almacen_id = @Id AND ma.periodo_emp_sede_id = @ContextoId",
            new { Id = model.Id, ContextoId = contextoId }, tx);

        if (old is null || old.Estado != "A" || (!string.IsNullOrEmpty(model.Revision) && old.Revision != model.Revision))
            return (false, "El consumo interno no está disponible o ya fue procesado.", id: model.Id);

        if (!old.EsPendienteAprobacion)
            return (false, "El consumo interno no está en estado pendiente de aprobación.", id: model.Id);

        // Buscar personal asociado al usuario autorizador si existe
        int? personalAutorizaId = await conn.ExecuteScalarAsync<int?>(@"
            SELECT per.personal_id FROM personal per
            JOIN proveedor p ON p.proveedor_id = per.proveedor_id
            WHERE p.usuario_id = @UserId AND per.estado = 'A'",
            new { UserId = userId }, tx);

        await conn.ExecuteAsync("sp_mov_almacen_actualizarEstadoAprobacion", new
        {
            movimientoAlmacenId = model.Id,
            estadoAprobacionId = 2, // 2 = APROBADO ('02')
            personalAutorizaId = personalAutorizaId,
            esEditable = "N",
            fechaAutorizacion = DateTime.Now,
            usuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, $"Consumo interno {old.Codigo} aprobado exitosamente.", model.Id);
    }

    public async Task<(bool success, string message, int id)> DesaprobarConsumoInternoAsync(int empresaId, int contextoId, int userId, DesaprobarConsumoIntSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.Id <= 0) return (false, "Identificador no válido.", 0);
        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return (false, "La empresa no está activa.", 0);

        var old = await conn.QuerySingleOrDefaultAsync<ConsumoIntItemModel>(
            ConsumoIntSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE ma.mov_almacen_id = @Id AND ma.periodo_emp_sede_id = @ContextoId",
            new { Id = model.Id, ContextoId = contextoId }, tx);

        if (old is null || old.Estado != "A" || (!string.IsNullOrEmpty(model.Revision) && old.Revision != model.Revision))
            return (false, "El consumo interno no está disponible o ya fue procesado.", id: model.Id);

        if (!old.EsPendienteAprobacion)
            return (false, "El consumo interno no está en estado pendiente de aprobación.", id: model.Id);

        int? personalAutorizaId = await conn.ExecuteScalarAsync<int?>(@"
            SELECT per.personal_id FROM personal per
            JOIN proveedor p ON p.proveedor_id = per.proveedor_id
            WHERE p.usuario_id = @UserId AND per.estado = 'A'",
            new { UserId = userId }, tx);

        await conn.ExecuteAsync("sp_mov_almacen_actualizarEstadoAprobacion", new
        {
            movimientoAlmacenId = model.Id,
            estadoAprobacionId = 3, // 3 = DESAPROBADO ('03')
            personalAutorizaId = personalAutorizaId,
            esEditable = "S",
            fechaAutorizacion = DateTime.Now,
            usuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, $"Consumo interno {old.Codigo} desaprobado.", model.Id);
    }
}

