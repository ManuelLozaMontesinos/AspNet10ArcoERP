using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class ComprasService
{
    private const string ValorizaCompraSelect = @"SELECT occ.orden_c_comp_id Id,
        occ.orden_co_id OrdenCompraId,
        RTRIM(oc.orden_co) OrdenCompraCodigo,
        occ.tipo_doc_com_id TipoDocumentoComercialId,
        RTRIM(tdc.tipo_doc_com) TipoDocumentoComercialCodigo,
        RTRIM(tdc.nombre) TipoDocumentoComercialNombre,
        RTRIM(tdc.abrev) TipoDocumentoComercialAbrev,
        RTRIM(occ.serie) Serie,
        RTRIM(occ.numero) Numero,
        occ.fecha Fecha,
        occ.fecha_vcto FechaVencimiento,
        oc.proveedor_id ProveedorId,
        RTRIM(p.razon_social) ProveedorNombre,
        COALESCE(NULLIF(p.numero_ruc,''), p.num_doc_iden, '') ProveedorDocumento,
        RTRIM(m.simbolo) MonedaSimbolo,
        occ.monto_bruto MontoBruto,
        occ.monto_impuesto MontoImpuesto,
        occ.monto_total MontoTotal,
        occ.cond_pago_id CondicionPagoId,
        RTRIM(ISNULL(cp.nombre, '')) CondicionPagoNombre,
        occ.reg_refe_id RegistroReferencialId,
        occ.estado Estado,
        COALESCE(occ.fecha_modifica, occ.fecha_creacion) FechaRevision
        FROM orden_c_comp occ
        JOIN orden_co oc ON oc.orden_co_id = occ.orden_co_id
        JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id = occ.tipo_doc_com_id
        JOIN proveedor p ON p.proveedor_id = oc.proveedor_id
        JOIN moneda m ON m.moneda_id = oc.moneda_id
        LEFT JOIN cond_pago cp ON cp.cond_pago_id = occ.cond_pago_id ";

    public async Task<ValorizaCompraPageModel> GetValorizacionesCompraAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        string? ordenCompraCodigo = null,
        string? serie = null,
        string? numero = null,
        int? tipoDocumentoComercialId = null,
        int? condicionPagoId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new ValorizaCompraPageModel
        {
            Search = search,
            OrdenCompraCodigo = ordenCompraCodigo,
            Serie = serie,
            Numero = numero,
            TipoDocumentoComercialId = tipoDocumentoComercialId,
            CondicionPagoId = condicionPagoId,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        };

        var columns = new Dictionary<string, string>
        {
            ["codigo"] = "oc.orden_co",
            ["documento"] = "occ.serie + '-' + occ.numero",
            ["fecha"] = "occ.fecha",
            ["proveedor"] = "p.razon_social",
            ["total"] = "occ.monto_total"
        };
        model.Ordenar(sort, dir, columns.Keys, "fecha");
        model.Paginar(0, page, top);

        if (empresaId <= 0 || contextoId <= 0)
        {
            model.Errores.Add("Seleccione una empresa y sede válidas.");
            return model;
        }

        using var conn = factory.CreateConnection();

        model.TiposDocumentoComercial = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT tipo_doc_com_id Id, RTRIM(abrev) Codigo, RTRIM(nombre) Nombre
            FROM tipo_doc_com WHERE es_interno = 'N' AND estado = 'A' ORDER BY nombre")).AsList();

        model.CondicionesPago = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT cond_pago_id Id, RTRIM(cond_pago) Codigo, RTRIM(nombre) Nombre
            FROM cond_pago WHERE empresa_id = @EmpresaId AND estado = 'A' ORDER BY nombre",
            new { EmpresaId = empresaId })).AsList();

        var where = new List<string> { "oc.periodo_emp_sede_id = @ContextoId", "occ.estado = 'A'" };
        var args = new DynamicParameters(new
        {
            EmpresaId = empresaId,
            ContextoId = contextoId,
            Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%",
            OrdenCompraCodigo = string.IsNullOrWhiteSpace(ordenCompraCodigo) ? null : $"%{ordenCompraCodigo.Trim()}%",
            Serie = string.IsNullOrWhiteSpace(serie) ? null : serie.Trim(),
            Numero = string.IsNullOrWhiteSpace(numero) ? null : numero.Trim(),
            TipoDocumentoComercialId = tipoDocumentoComercialId,
            CondicionPagoId = condicionPagoId,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        });

        if (!string.IsNullOrWhiteSpace(ordenCompraCodigo)) where.Add("oc.orden_co LIKE @OrdenCompraCodigo");
        if (!string.IsNullOrWhiteSpace(serie)) where.Add("occ.serie = @Serie");
        if (!string.IsNullOrWhiteSpace(numero)) where.Add("occ.numero = @Numero");
        if (tipoDocumentoComercialId.HasValue) where.Add("occ.tipo_doc_com_id = @TipoDocumentoComercialId");
        if (condicionPagoId.HasValue) where.Add("occ.cond_pago_id = @CondicionPagoId");
        if (fechaDesde.HasValue) where.Add("occ.fecha >= @FechaDesde");
        if (fechaHasta.HasValue) where.Add("occ.fecha <= @FechaHasta");

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add(@"(oc.orden_co LIKE @Search
                OR occ.serie LIKE @Search
                OR occ.numero LIKE @Search
                OR p.razon_social LIKE @Search
                OR p.num_doc_iden LIKE @Search
                OR p.numero_ruc LIKE @Search)");
        }

        string whereSql = " WHERE " + string.Join(" AND ", where);
        string countSql = "SELECT COUNT(1) FROM orden_c_comp occ JOIN orden_co oc ON oc.orden_co_id = occ.orden_co_id JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id = occ.tipo_doc_com_id JOIN proveedor p ON p.proveedor_id = oc.proveedor_id JOIN moneda m ON m.moneda_id = oc.moneda_id " + whereSql;

        int total = await conn.ExecuteScalarAsync<int>(countSql, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        string orderSql = $" ORDER BY {columns[model.Sort]} {model.Dir}, occ.orden_c_comp_id DESC ";
        string dataSql = ValorizaCompraSelect + whereSql + orderSql + " OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";

        model.Items = (await conn.QueryAsync<ValorizaCompraItemModel>(dataSql, args)).AsList();
        model.VisibleCount = model.Items.Count;
        return model;
    }

    public async Task<List<LookupItemModel>> BuscarOrdenesCompraParaComprobanteAsync(int empresaId, int contextoId, string? search)
    {
        if (empresaId <= 0 || contextoId <= 0) return new();
        using var conn = factory.CreateConnection();
        return (await conn.QueryAsync<LookupItemModel>(@"
            SELECT TOP 30 oc.orden_co_id Id,
                RTRIM(oc.orden_co) Codigo,
                RTRIM(p.razon_social) Descripcion,
                RTRIM(m.simbolo) + ' ' + CONVERT(varchar(30), oc.monto_total) + '|' + CONVERT(varchar(10), ISNULL(oc.cond_pago_id, 0)) Extra
            FROM orden_co oc
            JOIN proveedor p ON p.proveedor_id = oc.proveedor_id
            JOIN moneda m ON m.moneda_id = oc.moneda_id
            WHERE oc.periodo_emp_sede_id = @ContextoId
              AND oc.estado = 'A'
              AND (@Search IS NULL OR oc.orden_co LIKE @Search OR p.razon_social LIKE @Search OR p.num_doc_iden LIKE @Search OR p.numero_ruc LIKE @Search)
            ORDER BY oc.orden_co DESC",
            new { ContextoId = contextoId, Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%" })).AsList();
    }

    public async Task<ValorizaCompraItemModel?> GetValorizacionCompraDetalleAsync(int empresaId, int contextoId, int id)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0) return null;
        using var conn = factory.CreateConnection();

        return await conn.QuerySingleOrDefaultAsync<ValorizaCompraItemModel>(
            ValorizaCompraSelect + " WHERE occ.orden_c_comp_id = @Id AND oc.periodo_emp_sede_id = @ContextoId",
            new { Id = id, ContextoId = contextoId });
    }

    public async Task<(bool success, string message, int id)> SaveValorizacionCompraAsync(int empresaId, int contextoId, int userId, ValorizaCompraGuardarSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.OrdenCompraId <= 0 || model.TipoDocumentoComercialId <= 0 || string.IsNullOrWhiteSpace(model.Serie) || string.IsNullOrWhiteSpace(model.Numero) || model.MontoTotal <= 0m)
            return (false, "Complete los datos obligatorios del comprobante de compra.", 0);

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId))
            return (false, "La empresa no se encuentra activa.", 0);

        var oc = await conn.QuerySingleOrDefaultAsync<OrdenCompraItemModel>(
            OrdenCompraSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE oc.orden_co_id = @Id AND oc.periodo_emp_sede_id = @ContextoId",
            new { Id = model.OrdenCompraId, ContextoId = contextoId }, tx);

        if (oc is null || oc.Estado != "A")
            return (false, "La orden de compra asociada no está disponible.", 0);

        decimal tasaFactor = 1m + (oc.ImpuestoTasa / 100m);
        decimal montoTotal = model.MontoTotal;
        decimal montoBruto = Math.Round(montoTotal / tasaFactor, 4);
        decimal montoImpuesto = Math.Round(montoTotal - montoBruto, 4);

        int compId;
        if (model.Id > 0)
        {
            var old = await conn.QuerySingleOrDefaultAsync<ValorizaCompraItemModel>(
                ValorizaCompraSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE occ.orden_c_comp_id = @Id",
                new { Id = model.Id }, tx);

            if (old is null || old.Estado != "A" || (!string.IsNullOrEmpty(model.Revision) && old.Revision != model.Revision))
                return (false, "El comprobante no está disponible o fue modificado por otro usuario.", model.Id);

            await conn.ExecuteAsync("sp_orden_c_comp_actualizar", new
            {
                OrdenCompraComprobanteId = model.Id,
                TipoDocumentoComercialId = model.TipoDocumentoComercialId,
                Serie = model.Serie.Trim().ToUpperInvariant(),
                Numero = model.Numero.Trim().PadLeft(8, '0'),
                Fecha = model.Fecha,
                FechaVencimiento = model.FechaVencimiento,
                MontoBruto = montoBruto,
                MontoImpuesto = montoImpuesto,
                MontoTotal = montoTotal,
                CondicionPagoId = model.CondicionPagoId,
                UsuarioAccionId = userId
            }, tx, commandType: CommandType.StoredProcedure);

            compId = model.Id;
        }
        else
        {
            compId = await InsertAsync(conn, tx, "sp_orden_c_comp_insertar", new
            {
                OrdenCompraId = model.OrdenCompraId,
                TipoDocumentoComercialId = model.TipoDocumentoComercialId,
                Serie = model.Serie.Trim().ToUpperInvariant(),
                Numero = model.Numero.Trim().PadLeft(8, '0'),
                Fecha = model.Fecha,
                FechaVencimiento = model.FechaVencimiento,
                MontoBruto = montoBruto,
                MontoImpuesto = montoImpuesto,
                MontoTotal = montoTotal,
                CondicionPagoId = model.CondicionPagoId,
                RegistroReferencialId = (int?)null,
                UsuarioAccionId = userId
            });
        }

        tx.Commit();
        return (true, $"Comprobante {model.Serie}-{model.Numero} registrado exitosamente.", compId);
    }

    public async Task<(bool success, string message, int id)> EliminarValorizacionCompraAsync(int empresaId, int contextoId, int userId, int id)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0) return (false, "Identificador no válido.", 0);
        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return (false, "La empresa no se encuentra activa.", 0);

        var old = await conn.QuerySingleOrDefaultAsync<ValorizaCompraItemModel>(
            ValorizaCompraSelect + " WITH (UPDLOCK,HOLDLOCK) WHERE occ.orden_c_comp_id = @Id AND oc.periodo_emp_sede_id = @ContextoId",
            new { Id = id, ContextoId = contextoId }, tx);

        if (old is null || old.Estado != "A")
            return (false, "El comprobante no está disponible o ya fue eliminado.", id);

        await conn.ExecuteAsync("sp_orden_c_comp_eliminar", new
        {
            OrdenCompraComprobanteId = id,
            UsuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, $"Comprobante {old.DocumentoCompleto} eliminado correctamente.", id);
    }
}