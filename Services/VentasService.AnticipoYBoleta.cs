using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class VentasService
{
    public async Task<AnticipoClientePageModel> GetAnticiposClienteAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        int? clienteId = null,
        int? monedaId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        bool conSaldo = false,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new AnticipoClientePageModel
        {
            Search = search,
            ClienteId = clienteId,
            MonedaId = monedaId,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            ConSaldo = conSaldo
        };

        var columns = new Dictionary<string, string>
        {
            ["fecha"] = "ac.fecha",
            ["operacion"] = "ac.numero_opera",
            ["cliente"] = "p.razon_social",
            ["monto"] = "ac.monto",
            ["saldo"] = "(ac.monto - ac.monto_aplica)"
        };
        model.Ordenar(sort, dir, columns.Keys, "fecha");
        model.Paginar(0, page, top);

        if (empresaId <= 0 || contextoId <= 0)
        {
            model.Errores.Add("Seleccione una empresa y sede válidas.");
            return model;
        }

        using var conn = factory.CreateConnection();
        var sedeInfo = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT empresa_sede_id FROM periodo_emp_sede WHERE periodo_emp_sede_id = @ContextoId",
            new { ContextoId = contextoId });
        if (sedeInfo is null)
        {
            model.Errores.Add("Contexto de trabajo no válido.");
            return model;
        }
        int sedeId = (int)sedeInfo.empresa_sede_id;

        model.Monedas = (await conn.QueryAsync<LookupItemModel>("SELECT moneda_id Id, simbolo Codigo, nombre Nombre FROM moneda WHERE estado='A'")).AsList();
        model.MediosPago = (await conn.QueryAsync<LookupItemModel>("SELECT medio_pago_id Id, abrev Codigo, nombre Nombre FROM medio_pago WHERE estado='A'")).AsList();
        model.Bancos = (await conn.QueryAsync<LookupItemModel>("SELECT banco_id Id, banco Codigo, nombre Nombre FROM banco WHERE estado='A'")).AsList();

        var where = new List<string>
        {
            "ac.empresa_sede_id = @SedeId",
            "ac.estado = 'A'"
        };
        var args = new DynamicParameters();
        args.Add("SedeId", sedeId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(p.razon_social LIKE @Search OR p.num_doc_iden LIKE @Search OR ac.numero_opera LIKE @Search OR ac.numero_ref LIKE @Search)");
            args.Add("Search", $"%{search.Trim()}%");
        }
        if (clienteId.HasValue && clienteId.Value > 0)
        {
            where.Add("ac.proveedor_id = @ClienteId");
            args.Add("ClienteId", clienteId.Value);
        }
        if (monedaId.HasValue && monedaId.Value > 0)
        {
            where.Add("ac.moneda_id = @MonedaId");
            args.Add("MonedaId", monedaId.Value);
        }
        if (fechaDesde.HasValue)
        {
            where.Add("ac.fecha >= @FechaDesde");
            args.Add("FechaDesde", fechaDesde.Value.Date);
        }
        if (fechaHasta.HasValue)
        {
            where.Add("ac.fecha < @FechaHasta");
            args.Add("FechaHasta", fechaHasta.Value.Date.AddDays(1));
        }
        if (conSaldo)
        {
            where.Add("(ac.monto - ac.monto_aplica) > 0");
        }

        var filter = string.Join(" AND ", where);
        var fromSql = @"FROM anticipo_cliente ac
            INNER JOIN proveedor p ON p.proveedor_id = ac.proveedor_id
            INNER JOIN moneda m ON m.moneda_id = ac.moneda_id
            INNER JOIN medio_pago mp ON mp.medio_pago_id = ac.medio_pago_id
            LEFT JOIN banco b ON b.banco_id = ac.banco_id
            INNER JOIN estado_compro ec ON ec.estado_compro_id = ac.estado_compro_id
            WHERE " + filter;

        var countSql = "SELECT COUNT(1) " + fromSql;
        var total = await conn.ExecuteScalarAsync<int>(countSql, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        var selectSql = $@"SELECT
            ac.anticipo_cliente_id AS Id,
            ac.fecha AS Fecha,
            ac.numero_opera AS NumeroOperacion,
            ac.proveedor_id AS ClienteId,
            p.razon_social AS RazonSocial,
            p.num_doc_iden AS NumeroDocumento,
            ac.moneda_id AS MonedaId,
            m.nombre AS Moneda,
            m.simbolo AS SimboloMoneda,
            ac.monto AS Monto,
            ac.monto_aplica AS MontoAplicado,
            mp.nombre AS MedioPago,
            ISNULL(b.nombre, '') AS Banco,
            ISNULL(ac.numero_ref, '') AS NumeroRef,
            ac.extornado AS Extornado,
            ec.nombre AS EstadoComprobante,
            ISNULL(ac.observa, '') AS Observacion,
            ac.estado AS Estado,
            COALESCE(ac.fecha_modifica, ac.fecha_creacion, ac.fecha) AS FechaRevision
            {fromSql}
            ORDER BY {columns[model.Sort]} {model.Dir}
            OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";

        var rows = (await conn.QueryAsync<AnticipoClienteItemModel>(selectSql, args)).AsList();
        model.Items = rows;
        model.VisibleCount = rows.Count;
        return model;
    }

    public async Task<AnticipoClienteItemModel?> GetAnticipoClienteAsync(int empresaId, int contextoId, int id)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0) return null;
        using var conn = factory.CreateConnection();

        var sql = @"SELECT
            ac.anticipo_cliente_id AS Id,
            ac.fecha AS Fecha,
            ac.numero_opera AS NumeroOperacion,
            ac.proveedor_id AS ClienteId,
            p.razon_social AS RazonSocial,
            p.num_doc_iden AS NumeroDocumento,
            ac.moneda_id AS MonedaId,
            m.nombre AS Moneda,
            m.simbolo AS SimboloMoneda,
            ac.monto AS Monto,
            ac.monto_aplica AS MontoAplicado,
            mp.nombre AS MedioPago,
            ISNULL(b.nombre, '') AS Banco,
            ISNULL(ac.numero_ref, '') AS NumeroRef,
            ac.extornado AS Extornado,
            ec.nombre AS EstadoComprobante,
            ISNULL(ac.observa, '') AS Observacion,
            ac.estado AS Estado,
            COALESCE(ac.fecha_modifica, ac.fecha_creacion, ac.fecha) AS FechaRevision
            FROM anticipo_cliente ac
            INNER JOIN proveedor p ON p.proveedor_id = ac.proveedor_id
            INNER JOIN moneda m ON m.moneda_id = ac.moneda_id
            INNER JOIN medio_pago mp ON mp.medio_pago_id = ac.medio_pago_id
            LEFT JOIN banco b ON b.banco_id = ac.banco_id
            INNER JOIN estado_compro ec ON ec.estado_compro_id = ac.estado_compro_id
            WHERE ac.anticipo_cliente_id = @Id";

        return await conn.QuerySingleOrDefaultAsync<AnticipoClienteItemModel>(sql, new { Id = id });
    }

    public async Task<(bool success, string message, int id)> SaveAnticipoClienteAsync(
        int empresaId, int contextoId, int userId, AnticipoClienteGuardarSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.ClienteId <= 0 || model.Monto <= 0)
            return (false, "Datos del anticipo no válidos.", 0);

        using var conn = factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var sedeInfo = await conn.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT empresa_sede_id FROM periodo_emp_sede WHERE periodo_emp_sede_id = @ContextoId",
                new { ContextoId = contextoId }, tx);
            if (sedeInfo is null)
            {
                tx.Rollback();
                return (false, "Contexto no válido.", 0);
            }
            int sedeId = (int)sedeInfo.empresa_sede_id;

            // Obtener vendedor predeterminado si no se envió
            int vendedorId = model.VendedorId ?? 0;
            if (vendedorId <= 0)
            {
                vendedorId = await conn.ExecuteScalarAsync<int>(
                    "SELECT TOP 1 vendedor_id FROM vendedor WHERE estado='A' ORDER BY vendedor_id", transaction: tx);
            }

            var nextNum = await conn.ExecuteScalarAsync<int>(
                "SELECT ISNULL(MAX(CAST(numero_opera AS INT)), 0) + 1 FROM anticipo_cliente WHERE empresa_sede_id = @SedeId",
                new { SedeId = sedeId }, tx);
            string numeroOperacion = nextNum.ToString("D10");

            var dp = new DynamicParameters();
            dp.Add("empresaSedeId", sedeId);
            dp.Add("proveedorId", model.ClienteId);
            dp.Add("fecha", DateTime.Today);
            dp.Add("tipoCargoAbonoId", 2); // 2 = ABONO
            dp.Add("numeroOperacion", numeroOperacion);
            dp.Add("secuencia", 1);
            dp.Add("numeroGrupo", "GRP0001");
            dp.Add("monedaId", model.MonedaId);
            dp.Add("vendedorId", vendedorId);
            dp.Add("medioPagoId", model.MedioPagoId);
            dp.Add("bancoId", model.BancoId);
            dp.Add("fechaReferencia", model.FechaRef ?? DateTime.Today);
            dp.Add("numeroReferencia", model.NumeroRef ?? "");
            dp.Add("monto", model.Monto);
            dp.Add("montoAplicado", 0m);
            dp.Add("tipoOperacionAnticipoId", 1); // 1 = Cobro anticipado
            dp.Add("tipoDocumentoComercialId", null);
            dp.Add("serie", "0001");
            dp.Add("comprobanteId", null);
            dp.Add("cotizacionId", null);
            dp.Add("anticipoClientePadreId", null);
            dp.Add("extornado", "N");
            dp.Add("observacion", model.Observacion ?? "Anticipo de cliente");
            dp.Add("estadoComprobanteId", 1); // 1 = REGISTRADO / EMITIDO
            dp.Add("registradoEnCotizacion", "N");
            dp.Add("usuarioAccionId", userId);
            dp.Add("newID", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("sp_anticipo_cliente_insertar", dp, tx, commandType: CommandType.StoredProcedure);
            int newId = dp.Get<int>("newID");

            tx.Commit();
            return (true, "Anticipo registrado exitosamente.", newId);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, "Error al guardar anticipo: " + ex.Message, 0);
        }
    }

    public async Task<(bool success, string message)> ExtornarAnticipoClienteAsync(int empresaId, int contextoId, int userId, int id)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0)
            return (false, "Identificador no válido.");

        using var conn = factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var ant = await conn.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT monto, monto_aplica, extornado FROM anticipo_cliente WHERE anticipo_cliente_id = @Id",
                new { Id = id }, tx);
            if (ant is null)
            {
                tx.Rollback();
                return (false, "No se encontró el anticipo.");
            }
            if ((string)ant.extornado == "S")
            {
                tx.Rollback();
                return (false, "El anticipo ya se encuentra extornado.");
            }
            if ((decimal)ant.monto_aplica > 0)
            {
                tx.Rollback();
                return (false, "No se puede extornar un anticipo que ya ha sido aplicado a comprobantes.");
            }

            await conn.ExecuteAsync(@"UPDATE anticipo_cliente SET
                extornado = 'S',
                estado_compro_id = 3, -- ANULADO
                usuario_modifica_id = @UserId,
                fecha_modifica = GETDATE()
                WHERE anticipo_cliente_id = @Id", new { Id = id, UserId = userId }, tx);

            tx.Commit();
            return (true, "Anticipo extornado correctamente.");
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, "Error al extornar anticipo: " + ex.Message);
        }
    }

    public async Task<ComprobantePageModel> GetBoletasParaCambioAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        int? tipoComprobanteId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        // Traer comprobantes filtrados estrictamente por tipo_doc_com correspondiente a Boleta de Venta ("03")
        var model = new ComprobantePageModel
        {
            Search = search,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        };

        var columns = new Dictionary<string, string>
        {
            ["fecha"] = "c.fecha",
            ["documento"] = "c.serie + '-' + c.numero",
            ["cliente"] = "p.razon_social",
            ["total"] = "c.monto_total"
        };
        model.Ordenar(sort, dir, columns.Keys, "fecha");
        model.Paginar(0, page, top);

        if (empresaId <= 0 || contextoId <= 0)
        {
            return model;
        }

        using var conn = factory.CreateConnection();
        var where = new List<string>
        {
            "c.periodo_emp_sede_id = @ContextoId",
            "tdc.tipo_doc_com = '03'", // Boleta de Venta
            "c.estado = 'A'"
        };
        var args = new DynamicParameters();
        args.Add("ContextoId", contextoId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(c.serie LIKE @Search OR c.numero LIKE @Search OR p.razon_social LIKE @Search OR p.num_doc_iden LIKE @Search)");
            args.Add("Search", $"%{search.Trim()}%");
        }
        if (tipoComprobanteId.HasValue && tipoComprobanteId.Value > 0)
        {
            where.Add("c.tipo_comprob_id = @TipoCompId");
            args.Add("TipoCompId", tipoComprobanteId.Value);
        }
        if (fechaDesde.HasValue)
        {
            where.Add("c.fecha >= @FechaDesde");
            args.Add("FechaDesde", fechaDesde.Value.Date);
        }
        if (fechaHasta.HasValue)
        {
            where.Add("c.fecha < @FechaHasta");
            args.Add("FechaHasta", fechaHasta.Value.Date.AddDays(1));
        }

        var filter = string.Join(" AND ", where);
        var fromSql = @"FROM comprobante c
            INNER JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id = c.tipo_doc_com_id
            INNER JOIN proveedor p ON p.proveedor_id = c.proveedor_id
            INNER JOIN tipo_comprob tc ON tc.tipo_comprob_id = c.tipo_comprob_id
            INNER JOIN estado_compro ec ON ec.estado_compro_id = c.estado_compro_id
            WHERE " + filter;

        var countSql = "SELECT COUNT(1) " + fromSql;
        var total = await conn.ExecuteScalarAsync<int>(countSql, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        var selectSql = $@"SELECT
            c.comprobante_id AS ComprobanteId,
            tdc.abrev AS TipoDocumentoComercialNombre,
            c.serie AS Serie,
            c.numero AS Numero,
            c.fecha AS Fecha,
            p.razon_social AS ClienteRazonSocial,
            p.num_doc_iden AS ClienteNumeroDocumento,
            c.monto_total AS MontoTotal,
            tc.nombre AS TipoComprobante,
            ec.nombre AS EstadoComprobanteNombre
            {fromSql}
            ORDER BY {columns[model.Sort]} {model.Dir}
            OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";

        var rows = (await conn.QueryAsync<ComprobanteFilaModel>(selectSql, args)).AsList();
        model.Items = rows;
        model.VisibleCount = rows.Count;
        return model;
    }

    public async Task<(bool success, string message)> CambiarTipoComprobanteAsync(
        int empresaId, int contextoId, int userId, int comprobanteId, int nuevoTipoId)
    {
        if (empresaId <= 0 || contextoId <= 0 || comprobanteId <= 0 || nuevoTipoId <= 0)
            return (false, "Parámetros no válidos.");

        using var conn = factory.CreateConnection();
        try
        {
            var comp = await conn.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT comprobante_id, tipo_comprob_id, estado_compro_id FROM comprobante WHERE comprobante_id = @Id AND periodo_emp_sede_id = @ContextoId",
                new { Id = comprobanteId, ContextoId = contextoId });
            if (comp is null) return (false, "Comprobante no encontrado.");

            await conn.ExecuteAsync(@"UPDATE comprobante SET
                tipo_comprob_id = @NuevoTipoId,
                usuario_modifica_id = @UserId,
                fecha_modifica = GETDATE()
                WHERE comprobante_id = @Id", new
            {
                NuevoTipoId = nuevoTipoId,
                UserId = userId,
                Id = comprobanteId
            });

            return (true, "Tipo de comprobante actualizado correctamente.");
        }
        catch (Exception ex)
        {
            return (false, "Error al actualizar tipo: " + ex.Message);
        }
    }
}
