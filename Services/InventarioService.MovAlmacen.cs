using System.Data;
using System.Globalization;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class InventarioService
{
    private const string MovAlmacenSelect = @"SELECT ma.mov_almacen_id Id,
        ma.periodo_emp_sede_id PeriodoEmpresaSedeId,
        RTRIM(ma.mov_almacen) Codigo,
        ma.fecha Fecha,
        ma.almacen_id AlmacenId,
        RTRIM(a.nombre) AlmacenNombre,
        ma.tipo_mov_alma_id TipoMovimientoAlmacenId,
        RTRIM(tma.tipo_mov_alma) TipoMovimientoAlmacenCodigo,
        RTRIM(tma.nombre) TipoMovimientoAlmacenNombre,
        ma.trans_alma_id TransaccionAlmacenId,
        RTRIM(ta.trans_alma) TransaccionAlmacenCodigo,
        RTRIM(ta.nombre) TransaccionAlmacenNombre,
        ma.almacen_destino_id AlmacenDestinoId,
        RTRIM(ad.nombre) AlmacenDestinoNombre,
        ma.proveedor_id ProveedorId,
        RTRIM(p.razon_social) ProveedorNombre,
        COALESCE(NULLIF(p.numero_ruc,''), p.num_doc_iden, '') ProveedorDocumento,
        ma.orden_cs_id OrdenCompraId,
        RTRIM(ma.orden_cs) NumeroOrdenCompra,
        RTRIM(ma.nro_guia_rem) NumeroGuiaRemision,
        ma.tipo_doc_com_id TipoDocumentoComercialId,
        RTRIM(tdc.abrev) TipoDocumentoComercialAbrev,
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
        JOIN tipo_mov_alma tma ON tma.tipo_mov_alma_id = ma.tipo_mov_alma_id
        JOIN trans_alma ta ON ta.trans_alma_id = ma.trans_alma_id
        JOIN estado_mov_alm ema ON ema.estado_mov_alm_id = ma.estado_mov_alm_id
        JOIN usuario u ON u.usuario_id = ma.usuario_creacion_id
        LEFT JOIN almacen ad ON ad.almacen_id = ma.almacen_destino_id
        LEFT JOIN proveedor p ON p.proveedor_id = ma.proveedor_id
        LEFT JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id = ma.tipo_doc_com_id ";

    public async Task<MovAlmacenPageModel> GetMovimientosAlmacenAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        string? codigo = null,
        int? almacenId = null,
        int? tipoMovimientoAlmacenId = null,
        int? transaccionAlmacenId = null,
        int? estadoMovimientoAlmacenId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new MovAlmacenPageModel
        {
            Search = search,
            Codigo = codigo,
            AlmacenId = almacenId,
            TipoMovimientoAlmacenId = tipoMovimientoAlmacenId,
            TransaccionAlmacenId = transaccionAlmacenId,
            EstadoMovimientoAlmacenId = estadoMovimientoAlmacenId,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        };

        var columns = new Dictionary<string, string>
        {
            ["codigo"] = "ma.mov_almacen",
            ["fecha"] = "ma.fecha",
            ["almacen"] = "a.nombre",
            ["tipo"] = "tma.nombre",
            ["transaccion"] = "ta.nombre",
            ["proveedor"] = "p.razon_social",
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

        model.TiposMovimiento = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT tipo_mov_alma_id Id, RTRIM(tipo_mov_alma) Codigo, RTRIM(nombre) Nombre
            FROM tipo_mov_alma WHERE estado = 'A' ORDER BY nombre")).AsList();

        model.Transacciones = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT trans_alma_id Id, RTRIM(trans_alma) Codigo, RTRIM(nombre) Nombre
            FROM trans_alma WHERE empresa_id = @EmpresaId AND estado = 'A' ORDER BY nombre",
            new { EmpresaId = empresaId })).AsList();

        model.EstadosMovimiento = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT estado_mov_alm_id Id, RTRIM(estado_mov_alm) Codigo, RTRIM(nombre) Nombre
            FROM estado_mov_alm WHERE estado = 'A' ORDER BY estado_mov_alm_id")).AsList();

        var where = new List<string> { "ma.periodo_emp_sede_id = @ContextoId" };
        var args = new DynamicParameters(new
        {
            EmpresaId = empresaId,
            ContextoId = contextoId,
            Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%",
            Codigo = string.IsNullOrWhiteSpace(codigo) ? null : $"%{codigo.Trim()}%",
            AlmacenId = almacenId,
            TipoMovimientoAlmacenId = tipoMovimientoAlmacenId,
            TransaccionAlmacenId = transaccionAlmacenId,
            EstadoMovimientoAlmacenId = estadoMovimientoAlmacenId,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        });

        if (!string.IsNullOrWhiteSpace(codigo)) where.Add("ma.mov_almacen LIKE @Codigo");
        if (almacenId.HasValue) where.Add("ma.almacen_id = @AlmacenId");
        if (tipoMovimientoAlmacenId.HasValue) where.Add("ma.tipo_mov_alma_id = @TipoMovimientoAlmacenId");
        if (transaccionAlmacenId.HasValue) where.Add("ma.trans_alma_id = @TransaccionAlmacenId");
        if (estadoMovimientoAlmacenId.HasValue) where.Add("ma.estado_mov_alm_id = @EstadoMovimientoAlmacenId");
        if (fechaDesde.HasValue) where.Add("ma.fecha >= @FechaDesde");
        if (fechaHasta.HasValue) where.Add("ma.fecha <= @FechaHasta");

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add(@"(ma.mov_almacen LIKE @Search
                OR a.nombre LIKE @Search
                OR ta.nombre LIKE @Search
                OR ma.orden_cs LIKE @Search
                OR ma.nro_guia_rem LIKE @Search
                OR ma.serie_doc LIKE @Search
                OR ma.numero_doc LIKE @Search
                OR p.razon_social LIKE @Search
                OR p.num_doc_iden LIKE @Search
                OR p.numero_ruc LIKE @Search
                OR ma.observacion LIKE @Search)");
        }

        string whereSql = " WHERE " + string.Join(" AND ", where);
        string countSql = "SELECT COUNT(1) FROM mov_almacen ma JOIN almacen a ON a.almacen_id = ma.almacen_id JOIN tipo_mov_alma tma ON tma.tipo_mov_alma_id = ma.tipo_mov_alma_id JOIN trans_alma ta ON ta.trans_alma_id = ma.trans_alma_id JOIN estado_mov_alm ema ON ema.estado_mov_alm_id = ma.estado_mov_alm_id JOIN usuario u ON u.usuario_id = ma.usuario_creacion_id LEFT JOIN almacen ad ON ad.almacen_id = ma.almacen_destino_id LEFT JOIN proveedor p ON p.proveedor_id = ma.proveedor_id LEFT JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id = ma.tipo_doc_com_id " + whereSql;

        int total = await conn.ExecuteScalarAsync<int>(countSql, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        string orderSql = $" ORDER BY {columns[model.Sort]} {model.Dir}, ma.mov_almacen_id DESC ";
        string dataSql = MovAlmacenSelect + whereSql + orderSql + " OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";

        model.Items = (await conn.QueryAsync<MovAlmacenItemModel>(dataSql, args)).AsList();
        model.VisibleCount = model.Items.Count;
        return model;
    }

    public async Task<MovAlmacenDetalleModel?> GetMovimientoAlmacenDetalleAsync(int empresaId, int contextoId, int id)
    {
        if (empresaId <= 0 || contextoId <= 0) return null;
        using var conn = factory.CreateConnection();

        var detalle = new MovAlmacenDetalleModel();

        detalle.Almacenes = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT a.almacen_id Id, RTRIM(a.abrev) Codigo, RTRIM(a.nombre) Nombre
            FROM almacen a
            JOIN empresa_sede es ON es.empresa_sede_id = a.empresa_sede_id
            WHERE es.empresa_id = @EmpresaId AND a.estado = 'A'
            ORDER BY a.nombre",
            new { EmpresaId = empresaId })).AsList();

        detalle.TiposMovimiento = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT tipo_mov_alma_id Id, RTRIM(tipo_mov_alma) Codigo, RTRIM(nombre) Nombre
            FROM tipo_mov_alma WHERE estado = 'A' ORDER BY nombre")).AsList();

        detalle.TiposDocumentoComercial = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT tipo_doc_com_id Id, RTRIM(abrev) Codigo, RTRIM(nombre) Nombre
            FROM tipo_doc_com WHERE es_interno = 'N' AND estado = 'A' ORDER BY nombre")).AsList();

        if (id <= 0)
        {
            detalle.Cabecera = new MovAlmacenItemModel
            {
                Fecha = DateTime.Today,
                PeriodoEmpresaSedeId = contextoId
            };
            return detalle;
        }

        var cab = await conn.QuerySingleOrDefaultAsync<MovAlmacenItemModel>(
            MovAlmacenSelect + " WHERE ma.mov_almacen_id = @Id AND ma.periodo_emp_sede_id = @ContextoId",
            new { Id = id, ContextoId = contextoId });

        if (cab is null) return null;
        detalle.Cabecera = cab;
        detalle.Productos = await GetMovimientoAlmacenProductosAsync(empresaId, id);

        detalle.Transacciones = await GetTransaccionesPorAlmacenTipoAsync(empresaId, cab.AlmacenId, cab.TipoMovimientoAlmacenId);
        return detalle;
    }

    public async Task<List<LookupItemModel>> GetTransaccionesPorAlmacenTipoAsync(int empresaId, int almacenId, int tipoMovimientoId)
    {
        if (empresaId <= 0) return new();
        using var conn = factory.CreateConnection();

        return (await conn.QueryAsync<LookupItemModel>(@"
            SELECT DISTINCT ta.trans_alma_id Id, RTRIM(ta.trans_alma) Codigo, RTRIM(ta.nombre) Nombre
            FROM alm_transa ata
            JOIN trans_alma ta ON ta.trans_alma_id = ata.trans_alma_id
            WHERE ata.almacen_id = @AlmacenId
              AND ta.tipo_mov_alma_id = @TipoMovimientoId
              AND ata.estado = 'A'
              AND ta.estado = 'A'
            ORDER BY ta.nombre",
            new { AlmacenId = almacenId, TipoMovimientoId = tipoMovimientoId })).AsList();
    }

    public async Task<List<LookupItemModel>> BuscarProductosAlmacenAsync(int empresaId, string? search)
    {
        if (empresaId <= 0) return new();
        using var conn = factory.CreateConnection();
        return (await conn.QueryAsync<LookupItemModel>(@"
            SELECT TOP 30 prd.producto_id Id,
                RTRIM(prd.producto) Codigo,
                RTRIM(prd.descripcion) Nombre,
                RTRIM(ISNULL(um.abrev, 'UND')) Extra
            FROM producto prd
            JOIN unidad_medida um ON um.unidad_medida_id = prd.unidad_medida_id
            JOIN familia_producto fp ON fp.familia_producto_id = prd.familia_producto_id
            JOIN clase_producto cp ON cp.clase_producto_id = fp.clase_producto_id
            JOIN grupo_producto gp ON gp.grupo_producto_id = cp.grupo_producto_id
            JOIN linea_producto lp ON lp.linea_producto_id = gp.linea_producto_id
            WHERE lp.empresa_id = @EmpresaId AND prd.estado = 'A'
              AND (@Search IS NULL OR prd.producto LIKE @Search OR prd.descripcion LIKE @Search)
            ORDER BY prd.descripcion",
            new { EmpresaId = empresaId, Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%" })).AsList();
    }

    public async Task<List<LookupItemModel>> BuscarProveedoresAlmacenAsync(int empresaId, string? search)
    {
        if (empresaId <= 0) return new();
        using var conn = factory.CreateConnection();
        return (await conn.QueryAsync<LookupItemModel>(@"
            SELECT TOP 30 proveedor_id Id,
                RTRIM(COALESCE(NULLIF(numero_ruc,''), num_doc_iden, '')) Codigo,
                RTRIM(razon_social) Nombre
            FROM proveedor
            WHERE empresa_id = @EmpresaId AND estado = 'A'
              AND (@Search IS NULL OR razon_social LIKE @Search OR num_doc_iden LIKE @Search OR numero_ruc LIKE @Search)
            ORDER BY razon_social",
            new { EmpresaId = empresaId, Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%" })).AsList();
    }

    public async Task<(bool success, string message, int id)> SaveMovimientoAlmacenAsync(int empresaId, int contextoId, int userId, MovAlmacenGuardarSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.AlmacenId <= 0 || model.TipoMovimientoAlmacenId <= 0 || model.TransaccionAlmacenId <= 0)
            return (false, "Complete los datos obligatorios del movimiento (Almacén, Tipo y Transacción).", 0);

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

        int movId;
        string codigoMov;

        if (model.Id > 0)
        {
            var old = await conn.QuerySingleOrDefaultAsync<MovAlmacenItemModel>(
                MovAlmacenSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE ma.mov_almacen_id = @Id AND ma.periodo_emp_sede_id = @ContextoId",
                new { Id = model.Id, ContextoId = contextoId }, tx);

            if (old is null || old.Estado != "A" || (!string.IsNullOrEmpty(model.Revision) && old.Revision != model.Revision))
                return (false, "El movimiento no está disponible o fue modificado por otro usuario.", model.Id);

            if (old.EsAnulado)
                return (false, "No se puede modificar un movimiento que ha sido anulado.", model.Id);

            await conn.ExecuteAsync("sp_mov_almacen_actualizar", new
            {
                movimientoAlmacenId = model.Id,
                proveedorId = model.ProveedorId,
                almacenDestinoId = model.AlmacenDestinoId,
                fecha = model.Fecha,
                mesContableId = mesContableId,
                ordenCompraId = model.OrdenCompraId,
                numeroOrdenCompra = model.NumeroOrdenCompra ?? "",
                numeroGuiaRemision = model.NumeroGuiaRemision ?? "",
                comprobanteId = (int?)null,
                tipoDocumentoComercialId = model.TipoDocumentoComercialId,
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
            // Correlativo seguro por periodo_emp_sede_id, almacen_id y tipo_mov_alma_id
            var correlaRow = await conn.QuerySingleOrDefaultAsync<(int Id, int Correla)>(@"
                SELECT mov_alm_corre_id Id, correla Correla
                FROM mov_alm_corre WITH (UPDLOCK, HOLDLOCK)
                WHERE periodo_emp_sede_id = @ContextoId
                  AND almacen_id = @AlmacenId
                  AND tipo_mov_alma_id = @TipoMovimientoId",
                new { ContextoId = contextoId, AlmacenId = model.AlmacenId, TipoMovimientoId = model.TipoMovimientoAlmacenId }, tx);

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
                    VALUES (@ContextoId, @AlmacenId, @TipoMovimientoId, @Next, @UserId, GETDATE())",
                    new { ContextoId = contextoId, AlmacenId = model.AlmacenId, TipoMovimientoId = model.TipoMovimientoAlmacenId, Next = nextNum, UserId = userId }, tx);
            }

            string tipoCodigo = (await conn.ExecuteScalarAsync<string>(
                "SELECT RTRIM(tipo_mov_alma) FROM tipo_mov_alma WHERE tipo_mov_alma_id = @Id",
                new { Id = model.TipoMovimientoAlmacenId }, tx)) ?? "I";

            codigoMov = $"{tipoCodigo}{nextNum:D9}";

            movId = await InsertAsync(conn, tx, "sp_mov_almacen_insertar", new
            {
                periodoEmpresaSedeId = contextoId,
                almacenId = model.AlmacenId,
                transaccionAlmacenId = model.TransaccionAlmacenId,
                tipoMovimientoAlmacenId = model.TipoMovimientoAlmacenId,
                movimientoAlmacen = codigoMov,
                proveedorId = model.ProveedorId,
                almacenDestinoId = model.AlmacenDestinoId,
                movimientoAlmacenReferencialId = (int?)null,
                fecha = model.Fecha,
                mesContableId = mesContableId,
                ordenCompraId = model.OrdenCompraId,
                numeroOrdenCompra = model.NumeroOrdenCompra ?? "",
                numeroGuiaRemision = model.NumeroGuiaRemision ?? "",
                comprobanteId = (int?)null,
                tipoDocumentoComercialId = model.TipoDocumentoComercialId,
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
                estadoMovimientoAlmacenId = 1, // 1 = MOVIMIENTO
                usuarioAccionId = userId
            });
        }

        tx.Commit();
        return (true, $"Movimiento {codigoMov} guardado correctamente.", movId);
    }

    public async Task<(bool success, string message, int id)> AnularMovimientoAlmacenAsync(int empresaId, int contextoId, int userId, int id, string revision)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0) return (false, "Identificador no válido.", 0);
        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return (false, "La empresa no se encuentra activa.", 0);

        var old = await conn.QuerySingleOrDefaultAsync<MovAlmacenItemModel>(
            MovAlmacenSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE ma.mov_almacen_id = @Id AND ma.periodo_emp_sede_id = @ContextoId",
            new { Id = id, ContextoId = contextoId }, tx);

        if (old is null || old.Estado != "A" || (!string.IsNullOrEmpty(revision) && old.Revision != revision))
            return (false, "El movimiento no está disponible o ya fue modificado.", id);

        if (old.EsAnulado)
            return (false, "El movimiento ya se encuentra anulado.", id);

        await conn.ExecuteAsync("sp_mov_almacen_eliminar", new
        {
            movimientoAlmacenId = id,
            estadoMovimientoAlmacenId = 3, // 3 = ANULADO ('9')
            usuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, $"Movimiento {old.Codigo} anulado correctamente.", id);
    }

    public async Task<List<MovAlmacenProductoItemModel>> GetMovimientoAlmacenProductosAsync(int empresaId, int movimientoAlmacenId)
    {
        if (empresaId <= 0 || movimientoAlmacenId <= 0) return new();
        using var conn = factory.CreateConnection();

        return (await conn.QueryAsync<MovAlmacenProductoItemModel>(@"
            SELECT map.mov_alm_prod_id Id,
                map.mov_almacen_id MovimientoAlmacenId,
                map.producto_id ProductoId,
                RTRIM(prd.producto) ProductoCodigo,
                RTRIM(map.descripcion) Descripcion,
                RTRIM(ISNULL(um.abrev, 'UND')) UnidadMedida,
                map.cant_docu CantidadDocumento,
                map.cantidad Cantidad,
                map.valor_unitario ValorUnitario,
                map.peso_neto PesoNeto,
                map.peso_bruto PesoBruto,
                map.estado Estado,
                COALESCE(map.fecha_modifica, map.fecha_creacion) FechaRevision
            FROM mov_alm_prod map
            JOIN producto prd ON prd.producto_id = map.producto_id
            LEFT JOIN unidad_medida um ON um.unidad_medida_id = prd.unidad_medida_id
            WHERE map.mov_almacen_id = @MovimientoAlmacenId AND map.estado = 'A'
            ORDER BY map.mov_alm_prod_id ASC",
            new { MovimientoAlmacenId = movimientoAlmacenId })).AsList();
    }

    public async Task<(bool success, string message, int id)> SaveMovimientoAlmacenProductoAsync(int empresaId, int contextoId, int userId, MovAlmacenProductoGuardarSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.MovimientoAlmacenId <= 0 || model.ProductoId <= 0 || model.Cantidad <= 0m)
            return (false, "Especifique un producto válido y una cantidad superior a cero.", 0);

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return (false, "La empresa no está activa.", 0);

        var mov = await conn.QuerySingleOrDefaultAsync<MovAlmacenItemModel>(
            MovAlmacenSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE ma.mov_almacen_id = @Id AND ma.periodo_emp_sede_id = @ContextoId",
            new { Id = model.MovimientoAlmacenId, ContextoId = contextoId }, tx);

        if (mov is null || mov.Estado != "A")
            return (false, "El movimiento de almacén no se encuentra disponible.", 0);

        if (mov.EsAnulado)
            return (false, "No se pueden registrar productos en un movimiento anulado.", 0);

        string prdDesc = (await conn.ExecuteScalarAsync<string>(
            "SELECT RTRIM(descripcion) FROM producto WHERE producto_id = @Id",
            new { Id = model.ProductoId }, tx)) ?? "Producto";

        int prodId;
        if (model.Id > 0)
        {
            var old = await conn.QuerySingleOrDefaultAsync<MovAlmacenProductoItemModel>(@"
                SELECT map.mov_alm_prod_id Id, map.mov_almacen_id MovimientoAlmacenId, map.producto_id ProductoId,
                       map.cantidad Cantidad, map.valor_unitario ValorUnitario, map.estado Estado,
                       COALESCE(map.fecha_modifica, map.fecha_creacion) FechaRevision
                FROM mov_alm_prod map WITH (UPDLOCK,HOLDLOCK)
                WHERE map.mov_alm_prod_id = @Id AND map.estado = 'A'",
                new { Id = model.Id }, tx);

            if (old is null || old.Estado != "A" || (!string.IsNullOrEmpty(model.Revision) && old.Revision != model.Revision))
                return (false, "El producto no está disponible o fue modificado por otro usuario.", model.Id);

            await conn.ExecuteAsync("sp_mov_alm_prod_actualizar", new
            {
                movimientoAlmacenProductoId = model.Id,
                descripcion = string.IsNullOrWhiteSpace(model.Descripcion) ? prdDesc : model.Descripcion.Trim(),
                cantidadDocumento = model.CantidadDocumento > 0 ? model.CantidadDocumento : model.Cantidad,
                pesoNetoDocumento = model.PesoNeto,
                pesoBrutoDocumento = model.PesoBruto,
                cantidad = model.Cantidad,
                pesoNeto = model.PesoNeto,
                pesoBruto = model.PesoBruto,
                valorUnitario = model.ValorUnitario,
                registroReferencialId = (int?)null,
                usuarioAccionId = userId
            }, tx, commandType: CommandType.StoredProcedure);

            prodId = model.Id;
        }
        else
        {
            prodId = await InsertAsync(conn, tx, "sp_mov_alm_prod_insertar", new
            {
                movimientoAlmacenId = model.MovimientoAlmacenId,
                productoId = model.ProductoId,
                descripcion = string.IsNullOrWhiteSpace(model.Descripcion) ? prdDesc : model.Descripcion.Trim(),
                cantidadDocumento = model.CantidadDocumento > 0 ? model.CantidadDocumento : model.Cantidad,
                pesoNetoDocumento = model.PesoNeto,
                pesoBrutoDocumento = model.PesoBruto,
                cantidad = model.Cantidad,
                pesoNeto = model.PesoNeto,
                pesoBruto = model.PesoBruto,
                valorUnitario = model.ValorUnitario,
                productoReclasificacionId = (int?)null,
                cantidadReclasificacion = 0m,
                registroReferencialId = (int?)null,
                usuarioAccionId = userId
            });
        }

        tx.Commit();
        return (true, "Producto guardado en el movimiento.", prodId);
    }

    public async Task<(bool success, string message, int id)> EliminarMovimientoAlmacenProductoAsync(int empresaId, int contextoId, int userId, int id)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0) return (false, "Identificador no válido.", 0);
        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return (false, "La empresa no está activa.", 0);

        var item = await conn.QuerySingleOrDefaultAsync<MovAlmacenProductoItemModel>(@"
            SELECT map.mov_alm_prod_id Id, map.mov_almacen_id MovimientoAlmacenId
            FROM mov_alm_prod map WITH (UPDLOCK,HOLDLOCK)
            WHERE map.mov_alm_prod_id = @Id AND map.estado = 'A'",
            new { Id = id }, tx);

        if (item is null) return (false, "El producto no existe o ya fue eliminado.", id);

        var mov = await conn.QuerySingleOrDefaultAsync<MovAlmacenItemModel>(
            MovAlmacenSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE ma.mov_almacen_id = @Id AND ma.periodo_emp_sede_id = @ContextoId",
            new { Id = item.MovimientoAlmacenId, ContextoId = contextoId }, tx);

        if (mov is null || mov.Estado != "A")
            return (false, "El movimiento asociado no se encuentra disponible.", id);

        if (mov.EsAnulado)
            return (false, "No se pueden eliminar productos en un movimiento anulado.", id);

        await conn.ExecuteAsync("sp_mov_alm_prod_eliminar", new
        {
            movimientoAlmacenProductoId = id,
            usuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, "Producto eliminado del movimiento.", id);
    }

    public async Task<(string Titulo, byte[] Contenido)> ExportarMovimientoAlmacenExcelAsync(int empresaId, int movimientoAlmacenId)
    {
        if (empresaId <= 0 || movimientoAlmacenId <= 0) throw new ArgumentException("Parámetros no válidos.");
        using var conn = factory.CreateConnection();

        var mov = await conn.QuerySingleOrDefaultAsync<MovAlmacenItemModel>(MovAlmacenSelect + @"
            JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id = ma.periodo_emp_sede_id
            JOIN empresa_sede es ON es.empresa_sede_id = pes.empresa_sede_id
            WHERE ma.mov_almacen_id = @Id AND es.empresa_id = @EmpresaId",
            new { Id = movimientoAlmacenId, EmpresaId = empresaId })
            ?? throw new KeyNotFoundException("No se encontró el movimiento de almacén.");

        var items = await GetMovimientoAlmacenProductosAsync(empresaId, movimientoAlmacenId);

        var columns = new List<(string Label, Func<MovAlmacenProductoItemModel, object?> Value)>
        {
            ("Código", r => r.ProductoCodigo),
            ("Descripción", r => r.Descripcion),
            ("Unidad", r => r.UnidadMedida),
            ("Cant. Doc.", r => r.CantidadDocumento),
            ("Cantidad", r => r.Cantidad),
            ("Valor Unitario", r => r.ValorUnitario),
            ("Total", r => r.Total),
            ("Peso Neto", r => r.PesoNeto),
            ("Peso Bruto", r => r.PesoBruto)
        };

        string resumen = $"Operación: {mov.Codigo} | Almacén: {mov.AlmacenNombre} | Tipo: {mov.TipoMovimientoAlmacenNombre} | Transacción: {mov.TransaccionAlmacenNombre} | Fecha: {mov.FechaTexto} | Total: S/ {mov.MontoTotal:N2}";
        byte[] excelBytes = InformeExcel.Crear("MOVIMIENTO ALMACEN - " + mov.Codigo, "ARCO ERP", resumen, items, columns);
        return ("Movimiento_Almacen_" + mov.Codigo, excelBytes);
    }
}

