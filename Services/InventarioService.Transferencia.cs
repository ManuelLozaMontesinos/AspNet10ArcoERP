using System.Data;
using System.Globalization;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class InventarioService
{
    private const string TransfeSalidaSelect = @"SELECT ma.mov_almacen_id Id,
        ma.periodo_emp_sede_id PeriodoEmpresaSedeId,
        RTRIM(ma.mov_almacen) Codigo,
        ma.fecha Fecha,
        ma.almacen_id AlmacenId,
        RTRIM(a.nombre) AlmacenNombre,
        ISNULL(ma.almacen_destino_id, 0) AlmacenDestinoId,
        RTRIM(ISNULL(ad.nombre, '')) AlmacenDestinoNombre,
        RTRIM(ma.nro_guia_rem) NumeroGuiaRemision,
        RTRIM(ma.serie_doc) SerieDocumento,
        RTRIM(ma.numero_doc) NumeroDocumento,
        RTRIM(ma.observacion) Observacion,
        ma.estado_mov_alm_id EstadoMovimientoAlmacenId,
        RTRIM(ema.estado_mov_alm) EstadoMovimientoAlmacenCodigo,
        RTRIM(ema.nombre) EstadoMovimientoAlmacenNombre,
        RTRIM(u.usuario_logon) UsuarioLogon,
        (SELECT COUNT(1) FROM mov_alm_prod map WHERE map.mov_almacen_id = ma.mov_almacen_id AND map.estado = 'A') CantidadItems,
        ISNULL((SELECT SUM(map.cantidad * map.valor_unitario) FROM mov_alm_prod map WHERE map.mov_almacen_id = ma.mov_almacen_id AND map.estado = 'A'), 0) MontoTotal,
        ma.estado Estado,
        COALESCE(ma.fecha_modifica, ma.fecha_creacion) FechaRevision
        FROM mov_almacen ma
        JOIN almacen a ON a.almacen_id = ma.almacen_id
        LEFT JOIN almacen ad ON ad.almacen_id = ma.almacen_destino_id
        JOIN trans_alma ta ON ta.trans_alma_id = ma.trans_alma_id
        JOIN estado_mov_alm ema ON ema.estado_mov_alm_id = ma.estado_mov_alm_id
        JOIN usuario u ON u.usuario_id = ma.usuario_creacion_id ";

    public async Task<TransfeSalidaPageModel> GetTransferenciasSalidaAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        string? codigo = null,
        int? almacenId = null,
        int? almacenDestinoId = null,
        int? estadoMovimientoAlmacenId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new TransfeSalidaPageModel
        {
            Search = search,
            Codigo = codigo,
            AlmacenId = almacenId,
            AlmacenDestinoId = almacenDestinoId,
            EstadoMovimientoAlmacenId = estadoMovimientoAlmacenId,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        };

        var columns = new Dictionary<string, string>
        {
            ["codigo"] = "ma.mov_almacen",
            ["fecha"] = "ma.fecha",
            ["almacen"] = "a.nombre",
            ["destino"] = "ad.nombre",
            ["total"] = "MontoTotal",
            ["estado"] = "ema.nombre"
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

        model.AlmacenesDestino = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT a.almacen_id Id, RTRIM(a.abrev) Codigo, RTRIM(a.nombre) Nombre
            FROM almacen a
            JOIN empresa_sede es ON es.empresa_sede_id = a.empresa_sede_id
            WHERE es.empresa_id = @EmpresaId AND a.estado = 'A'
            ORDER BY a.nombre",
            new { EmpresaId = empresaId })).AsList();

        model.EstadosMovimiento = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT estado_mov_alm_id Id, RTRIM(estado_mov_alm) Codigo, RTRIM(nombre) Nombre
            FROM estado_mov_alm WHERE estado = 'A' ORDER BY estado_mov_alm_id")).AsList();

        var where = new List<string> {
            "ma.periodo_emp_sede_id = @ContextoId",
            "ma.tipo_mov_alma_id = 2", // Salida
            "ta.trans_alma = 'S11'"    // Transferencia entre almacenes
        };

        var args = new DynamicParameters(new
        {
            EmpresaId = empresaId,
            ContextoId = contextoId,
            Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%",
            Codigo = string.IsNullOrWhiteSpace(codigo) ? null : $"%{codigo.Trim()}%",
            AlmacenId = almacenId,
            AlmacenDestinoId = almacenDestinoId,
            EstadoMovimientoAlmacenId = estadoMovimientoAlmacenId,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        });

        if (!string.IsNullOrWhiteSpace(codigo)) where.Add("ma.mov_almacen LIKE @Codigo");
        if (almacenId.HasValue) where.Add("ma.almacen_id = @AlmacenId");
        if (almacenDestinoId.HasValue) where.Add("ma.almacen_destino_id = @AlmacenDestinoId");
        if (estadoMovimientoAlmacenId.HasValue) where.Add("ma.estado_mov_alm_id = @EstadoMovimientoAlmacenId");
        if (fechaDesde.HasValue) where.Add("ma.fecha >= @FechaDesde");
        if (fechaHasta.HasValue) where.Add("ma.fecha <= @FechaHasta");

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add(@"(ma.mov_almacen LIKE @Search
                OR a.nombre LIKE @Search
                OR ad.nombre LIKE @Search
                OR ma.nro_guia_rem LIKE @Search
                OR ma.serie_doc LIKE @Search
                OR ma.numero_doc LIKE @Search
                OR ma.observacion LIKE @Search)");
        }

        string whereSql = " WHERE " + string.Join(" AND ", where);
        string countSql = "SELECT COUNT(1) FROM mov_almacen ma JOIN almacen a ON a.almacen_id = ma.almacen_id LEFT JOIN almacen ad ON ad.almacen_id = ma.almacen_destino_id JOIN trans_alma ta ON ta.trans_alma_id = ma.trans_alma_id JOIN estado_mov_alm ema ON ema.estado_mov_alm_id = ma.estado_mov_alm_id " + whereSql;

        int total = await conn.ExecuteScalarAsync<int>(countSql, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        string orderSql = $" ORDER BY {columns[model.Sort]} {model.Dir}, ma.mov_almacen_id DESC ";
        string dataSql = TransfeSalidaSelect + whereSql + orderSql + " OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";

        model.Items = (await conn.QueryAsync<TransfeSalidaItemModel>(dataSql, args)).AsList();
        model.VisibleCount = model.Items.Count;
        return model;
    }

    public async Task<MovAlmacenDetalleModel?> GetTransferenciaSalidaDetalleAsync(int empresaId, int contextoId, int id)
    {
        return await GetMovimientoAlmacenDetalleAsync(empresaId, contextoId, id);
    }

    public async Task<(bool success, string message, int id)> SaveTransferenciaSalidaAsync(int empresaId, int contextoId, int userId, TransfeSalidaGuardarSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.AlmacenId <= 0 || model.AlmacenDestinoId <= 0)
            return (false, "Seleccione el almacén de origen y el almacén de destino.", 0);

        if (model.AlmacenId == model.AlmacenDestinoId)
            return (false, "El almacén de destino debe ser diferente al almacén de origen.", 0);

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
            "SELECT ISNULL(MAX(trans_alma_id), 11) FROM trans_alma WHERE trans_alma = 'S11' AND estado = 'A'",
            null, tx);
        if (transAlmaId == 0) transAlmaId = 11;

        int movId;
        string codigoMov;

        if (model.Id > 0)
        {
            var old = await conn.QuerySingleOrDefaultAsync<TransfeSalidaItemModel>(
                TransfeSalidaSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE ma.mov_almacen_id = @Id AND ma.periodo_emp_sede_id = @ContextoId",
                new { Id = model.Id, ContextoId = contextoId }, tx);

            if (old is null || old.Estado != "A" || (!string.IsNullOrEmpty(model.Revision) && old.Revision != model.Revision))
                return (false, "La transferencia no está disponible o fue modificada por otro usuario.", model.Id);

            if (!old.EsTransito)
                return (false, "No se puede modificar una transferencia que ya fue recepcionada o anulada.", model.Id);

            await conn.ExecuteAsync("sp_mov_almacen_actualizar", new
            {
                movimientoAlmacenId = model.Id,
                proveedorId = (int?)null,
                almacenDestinoId = model.AlmacenDestinoId,
                fecha = model.Fecha,
                mesContableId = mesContableId,
                ordenCompraId = (int?)null,
                numeroOrdenCompra = "",
                numeroGuiaRemision = model.NumeroGuiaRemision ?? "",
                comprobanteId = (int?)null,
                tipoDocumentoComercialId = (int?)null,
                serieDocumento = model.SerieDocumento ?? "",
                numeroDocumento = model.NumeroDocumento ?? "",
                tieneGuiaRemision = string.IsNullOrWhiteSpace(model.NumeroGuiaRemision) ? "N" : "S",
                personalId = (int?)null,
                centroCostoId = (int?)null,
                personalAutorizaId = (int?)null,
                afectaSaldo = "S",
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
                almacenDestinoId = model.AlmacenDestinoId,
                movimientoAlmacenReferencialId = (int?)null,
                fecha = model.Fecha,
                mesContableId = mesContableId,
                ordenCompraId = (int?)null,
                numeroOrdenCompra = "",
                numeroGuiaRemision = model.NumeroGuiaRemision ?? "",
                comprobanteId = (int?)null,
                tipoDocumentoComercialId = (int?)null,
                serieDocumento = model.SerieDocumento ?? "",
                numeroDocumento = model.NumeroDocumento ?? "",
                tieneGuiaRemision = string.IsNullOrWhiteSpace(model.NumeroGuiaRemision) ? "N" : "S",
                guiaRemisionEnModificacion = "N",
                personalId = (int?)null,
                centroCostoId = (int?)null,
                personalAutorizaId = (int?)null,
                movimientoReclasificacionId = (int?)null,
                ordenReclasificacion = 0,
                afectaSaldo = "S",
                observacion = model.Observacion ?? "",
                estadoMovimientoAlmacenId = 2, // 2 = TRANSITO
                usuarioAccionId = userId
            });
        }

        tx.Commit();
        return (true, $"Transferencia de salida {codigoMov} guardada correctamente.", movId);
    }

    public async Task<(bool success, string message, int id)> AnularTransferenciaSalidaAsync(int empresaId, int contextoId, int userId, int id, string revision)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0) return (false, "Identificador no válido.", 0);
        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return (false, "La empresa no se encuentra activa.", 0);

        var old = await conn.QuerySingleOrDefaultAsync<TransfeSalidaItemModel>(
            TransfeSalidaSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE ma.mov_almacen_id = @Id AND ma.periodo_emp_sede_id = @ContextoId",
            new { Id = id, ContextoId = contextoId }, tx);

        if (old is null || old.Estado != "A" || (!string.IsNullOrEmpty(revision) && old.Revision != revision))
            return (false, "La transferencia no está disponible o ya fue modificada.", id);

        if (!old.EsTransito)
            return (false, "Sólo se pueden anular transferencias en estado 'EN TRÁNSITO'.", id);

        await conn.ExecuteAsync("sp_mov_almacen_eliminar", new
        {
            movimientoAlmacenId = id,
            estadoMovimientoAlmacenId = 3, // 3 = ANULADO ('9')
            usuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, $"Transferencia {old.Codigo} anulada correctamente.", id);
    }

    public async Task<TransfeIngresoPageModel> GetTransferenciasIngresoAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        string? codigo = null,
        int? almacenOrigenId = null,
        int? estadoMovimientoAlmacenId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new TransfeIngresoPageModel
        {
            Search = search,
            Codigo = codigo,
            AlmacenOrigenId = almacenOrigenId,
            EstadoMovimientoAlmacenId = estadoMovimientoAlmacenId,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        };

        var columns = new Dictionary<string, string>
        {
            ["codigo"] = "ma.mov_almacen",
            ["fecha"] = "ma.fecha",
            ["origen"] = "a.nombre",
            ["destino"] = "ad.nombre",
            ["total"] = "MontoTotal",
            ["estado"] = "ema.nombre"
        };
        model.Ordenar(sort, dir, columns.Keys, "fecha", "desc");
        model.Paginar(0, page, top);

        if (empresaId <= 0 || contextoId <= 0)
        {
            model.Errores.Add("Seleccione una empresa y sede válidas.");
            return model;
        }

        using var conn = factory.CreateConnection();

        model.AlmacenesOrigen = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT a.almacen_id Id, RTRIM(a.abrev) Codigo, RTRIM(a.nombre) Nombre
            FROM almacen a
            JOIN empresa_sede es ON es.empresa_sede_id = a.empresa_sede_id
            WHERE es.empresa_id = @EmpresaId AND a.estado = 'A'
            ORDER BY a.nombre",
            new { EmpresaId = empresaId })).AsList();

        model.EstadosMovimiento = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT estado_mov_alm_id Id, RTRIM(estado_mov_alm) Codigo, RTRIM(nombre) Nombre
            FROM estado_mov_alm WHERE estado = 'A' ORDER BY estado_mov_alm_id")).AsList();

        // En transferencias de ingreso, buscamos las transferencias cuyo almacén destino pertenezca a la empresa
        var where = new List<string> {
            "ma.tipo_mov_alma_id = 2", // Salida original
            "ta.trans_alma = 'S11'",    // Transferencia entre almacenes
            "ad.almacen_id IS NOT NULL",
            "ad.almacen_id IN (SELECT a2.almacen_id FROM almacen a2 JOIN empresa_sede es2 ON es2.empresa_sede_id = a2.empresa_sede_id WHERE es2.empresa_id = @EmpresaId)"
        };

        var args = new DynamicParameters(new
        {
            EmpresaId = empresaId,
            ContextoId = contextoId,
            Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%",
            Codigo = string.IsNullOrWhiteSpace(codigo) ? null : $"%{codigo.Trim()}%",
            AlmacenOrigenId = almacenOrigenId,
            EstadoMovimientoAlmacenId = estadoMovimientoAlmacenId,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        });

        if (!string.IsNullOrWhiteSpace(codigo)) where.Add("ma.mov_almacen LIKE @Codigo");
        if (almacenOrigenId.HasValue) where.Add("ma.almacen_id = @AlmacenOrigenId");
        if (estadoMovimientoAlmacenId.HasValue) where.Add("ma.estado_mov_alm_id = @EstadoMovimientoAlmacenId");
        if (fechaDesde.HasValue) where.Add("ma.fecha >= @FechaDesde");
        if (fechaHasta.HasValue) where.Add("ma.fecha <= @FechaHasta");

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add(@"(ma.mov_almacen LIKE @Search
                OR a.nombre LIKE @Search
                OR ad.nombre LIKE @Search
                OR ma.nro_guia_rem LIKE @Search
                OR ma.observacion LIKE @Search)");
        }

        string whereSql = " WHERE " + string.Join(" AND ", where);
        string countSql = "SELECT COUNT(1) FROM mov_almacen ma JOIN almacen a ON a.almacen_id = ma.almacen_id LEFT JOIN almacen ad ON ad.almacen_id = ma.almacen_destino_id JOIN trans_alma ta ON ta.trans_alma_id = ma.trans_alma_id JOIN estado_mov_alm ema ON ema.estado_mov_alm_id = ma.estado_mov_alm_id " + whereSql;

        int total = await conn.ExecuteScalarAsync<int>(countSql, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        string orderSql = $" ORDER BY {columns[model.Sort]} {model.Dir}, ma.mov_almacen_id DESC ";
        string selectSql = @"SELECT ma.mov_almacen_id Id,
            ma.periodo_emp_sede_id PeriodoEmpresaSedeId,
            RTRIM(ma.mov_almacen) Codigo,
            ma.fecha Fecha,
            ma.almacen_id AlmacenOrigenId,
            RTRIM(a.nombre) AlmacenOrigenNombre,
            ISNULL(ma.almacen_destino_id, 0) AlmacenDestinoId,
            RTRIM(ISNULL(ad.nombre, '')) AlmacenDestinoNombre,
            RTRIM(ma.nro_guia_rem) NumeroGuiaRemision,
            RTRIM(ma.serie_doc) SerieDocumento,
            RTRIM(ma.numero_doc) NumeroDocumento,
            RTRIM(ma.observacion) Observacion,
            ma.estado_mov_alm_id EstadoMovimientoAlmacenId,
            RTRIM(ema.estado_mov_alm) EstadoMovimientoAlmacenCodigo,
            RTRIM(ema.nombre) EstadoMovimientoAlmacenNombre,
            (SELECT COUNT(1) FROM mov_alm_prod map WHERE map.mov_almacen_id = ma.mov_almacen_id AND map.estado = 'A') CantidadItems,
            ISNULL((SELECT SUM(map.cantidad * map.valor_unitario) FROM mov_alm_prod map WHERE map.mov_almacen_id = ma.mov_almacen_id AND map.estado = 'A'), 0) MontoTotal,
            ma.estado Estado,
            COALESCE(ma.fecha_modifica, ma.fecha_creacion) FechaRevision
            FROM mov_almacen ma
            JOIN almacen a ON a.almacen_id = ma.almacen_id
            LEFT JOIN almacen ad ON ad.almacen_id = ma.almacen_destino_id
            JOIN trans_alma ta ON ta.trans_alma_id = ma.trans_alma_id
            JOIN estado_mov_alm ema ON ema.estado_mov_alm_id = ma.estado_mov_alm_id ";

        string dataSql = selectSql + whereSql + orderSql + " OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";

        model.Items = (await conn.QueryAsync<TransfeIngresoItemModel>(dataSql, args)).AsList();
        model.VisibleCount = model.Items.Count;
        return model;
    }

    public async Task<(bool success, string message, int id)> RecepcionarTransferenciaAsync(int empresaId, int contextoId, int userId, RecepcionarTransferenciaSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.Id <= 0)
            return (false, "Identificador de transferencia no válido.", 0);

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId))
            return (false, "La empresa no se encuentra activa.", 0);

        var salida = await conn.QuerySingleOrDefaultAsync<TransfeSalidaItemModel>(
            TransfeSalidaSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE ma.mov_almacen_id = @Id",
            new { Id = model.Id }, tx);

        if (salida is null || salida.Estado != "A" || (!string.IsNullOrEmpty(model.Revision) && salida.Revision != model.Revision))
            return (false, "La transferencia de salida no está disponible o fue modificada.", 0);

        if (!salida.EsTransito)
            return (false, "La transferencia no se encuentra en estado 'EN TRÁNSITO' para poder ser recepcionada.", 0);

        var items = await conn.QueryAsync<MovAlmacenProductoItemModel>(@"
            SELECT map.mov_alm_prod_id Id, map.producto_id ProductoId, map.descripcion Descripcion,
                   map.cant_docu CantidadDocumento, map.cantidad Cantidad, map.valor_unitario ValorUnitario,
                   map.peso_neto PesoNeto, map.peso_bruto PesoBruto
            FROM mov_alm_prod map WITH (UPDLOCK,HOLDLOCK)
            WHERE map.mov_almacen_id = @Id AND map.estado = 'A'",
            new { Id = model.Id }, tx);

        var itemList = items.AsList();
        if (itemList.Count == 0)
            return (false, "La transferencia de salida no tiene productos registrados.", 0);

        // Correlativo seguro para INGRESO (tipo_mov_alma_id = 1) en el almacén de destino
        var correlaRow = await conn.QuerySingleOrDefaultAsync<(int Id, int Correla)>(@"
            SELECT mov_alm_corre_id Id, correla Correla
            FROM mov_alm_corre WITH (UPDLOCK, HOLDLOCK)
            WHERE periodo_emp_sede_id = @ContextoId
              AND almacen_id = @AlmacenId
              AND tipo_mov_alma_id = 1",
            new { ContextoId = contextoId, AlmacenId = salida.AlmacenDestinoId }, tx);

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
                VALUES (@ContextoId, @AlmacenId, 1, @Next, @UserId, GETDATE())",
                new { ContextoId = contextoId, AlmacenId = salida.AlmacenDestinoId, Next = nextNum, UserId = userId }, tx);
        }

        string codigoIngreso = $"I{nextNum:D9}";

        string mesStr = model.FechaRecepcion.ToString("MM");
        int mesContableId = await conn.ExecuteScalarAsync<int>(
            "SELECT ISNULL(MAX(mes_contable_id), 1) FROM mes_contable WHERE mes_contable = @Mes",
            new { Mes = mesStr }, tx);
        if (mesContableId == 0) mesContableId = 1;

        int transAlmaId = await conn.ExecuteScalarAsync<int>(
            "SELECT ISNULL(MAX(trans_alma_id), 11) FROM trans_alma WHERE trans_alma = 'S11' AND estado = 'A'",
            null, tx);
        if (transAlmaId == 0) transAlmaId = 11;

        // Crear movimiento de ingreso en el almacén de destino
        int newIngresoId = await InsertAsync(conn, tx, "sp_mov_almacen_insertar", new
        {
            periodoEmpresaSedeId = contextoId,
            almacenId = salida.AlmacenDestinoId,
            transaccionAlmacenId = transAlmaId,
            tipoMovimientoAlmacenId = 1, // Ingreso
            movimientoAlmacen = codigoIngreso,
            proveedorId = (int?)null,
            almacenDestinoId = (int?)null,
            movimientoAlmacenReferencialId = salida.Id,
            fecha = model.FechaRecepcion,
            mesContableId = mesContableId,
            ordenCompraId = (int?)null,
            numeroOrdenCompra = "",
            numeroGuiaRemision = salida.NumeroGuiaRemision,
            comprobanteId = (int?)null,
            tipoDocumentoComercialId = (int?)null,
            serieDocumento = salida.SerieDocumento,
            numeroDocumento = salida.NumeroDocumento,
            tieneGuiaRemision = string.IsNullOrWhiteSpace(salida.NumeroGuiaRemision) ? "N" : "S",
            guiaRemisionEnModificacion = "N",
            personalId = (int?)null,
            centroCostoId = (int?)null,
            personalAutorizaId = (int?)null,
            movimientoReclasificacionId = (int?)null,
            ordenReclasificacion = 0,
            afectaSaldo = "S",
            observacion = string.IsNullOrWhiteSpace(model.Observacion) ? $"Recepción de transferencia {salida.Codigo}" : model.Observacion.Trim(),
            estadoMovimientoAlmacenId = 1, // 1 = MOVIMIENTO
            usuarioAccionId = userId
        });

        // Copiar items al movimiento de ingreso
        foreach (var item in itemList)
        {
            await InsertAsync(conn, tx, "sp_mov_alm_prod_insertar", new
            {
                movimientoAlmacenId = newIngresoId,
                productoId = item.ProductoId,
                descripcion = item.Descripcion,
                cantidadDocumento = item.CantidadDocumento,
                pesoNetoDocumento = item.PesoNeto,
                pesoBrutoDocumento = item.PesoBruto,
                cantidad = item.Cantidad,
                pesoNeto = item.PesoNeto,
                pesoBruto = item.PesoBruto,
                valorUnitario = item.ValorUnitario,
                productoReclasificacionId = (int?)null,
                cantidadReclasificacion = 0m,
                registroReferencialId = item.Id,
                usuarioAccionId = userId
            });
        }

        // Transicionar la salida de TRANSITO (2) a MOVIMIENTO (1 = Cerrada/Entregada)
        await conn.ExecuteAsync(@"
            UPDATE mov_almacen
            SET estado_mov_alm_id = 1,
                usuario_modifica_id = @UserId,
                fecha_modifica = GETDATE()
            WHERE mov_almacen_id = @Id",
            new { UserId = userId, Id = salida.Id }, tx);

        tx.Commit();
        return (true, $"Transferencia recepcionada con éxito mediante Ingreso N° {codigoIngreso}.", newIngresoId);
    }
}

