using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class ProduccionService
{
    public async Task<DespachoPageModel> GetDespachosAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        string? codigo = null,
        string? cotizacion = null,
        string? cliente = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? estadoDespachoId = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new DespachoPageModel
        {
            Search = search,
            Codigo = codigo,
            Cotizacion = cotizacion,
            Cliente = cliente,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            EstadoDespachoId = estadoDespachoId
        };

        var columns = new Dictionary<string, string>
        {
            ["documento"] = "c.serie + '-' + c.numero",
            ["fecha"] = "c.fecha",
            ["cotizacion"] = "ct.cotiza",
            ["cliente"] = "p.razon_social",
            ["despacho"] = "ed.nombre"
        };
        model.Ordenar(sort, dir, columns.Keys, "fecha");
        model.Paginar(0, page, top);

        if (empresaId <= 0 || contextoId <= 0)
        {
            model.Errores.Add("Seleccione una empresa y sede válidas.");
            return model;
        }

        using var conn = factory.CreateConnection();
        if (!await ContextAsync(conn, empresaId, contextoId))
        {
            model.Errores.Add("No se encontró el período activo para la empresa.");
            return model;
        }

        model.EstadosDespacho = (await conn.QueryAsync<LookupItemModel>(
            "SELECT estado_despacho_id Id, estado_despacho Codigo, nombre Nombre FROM estado_despacho ORDER BY estado_despacho_id"
        )).AsList();

        var where = new List<string>
        {
            "c.periodo_emp_sede_id = @ContextoId",
            "c.estado = 'A'",
            "c.tipo_comprob_id = 1", // Normal
            "ec.estado_compro = 'EMI'" // Emitido
        };
        var args = new DynamicParameters();
        args.Add("ContextoId", contextoId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(c.serie LIKE @Search OR c.numero LIKE @Search OR p.razon_social LIKE @Search OR p.num_doc_iden LIKE @Search OR ct.cotiza LIKE @Search)");
            args.Add("Search", $"%{search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(codigo))
        {
            where.Add("(c.serie + '-' + c.numero LIKE @Codigo OR c.numero LIKE @Codigo)");
            args.Add("Codigo", $"%{codigo.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(cotizacion))
        {
            where.Add("ct.cotiza LIKE @Cotizacion");
            args.Add("Cotizacion", $"%{cotizacion.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(cliente))
        {
            where.Add("(p.razon_social LIKE @Cliente OR p.num_doc_iden LIKE @Cliente)");
            args.Add("Cliente", $"%{cliente.Trim()}%");
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
        if (estadoDespachoId.HasValue && estadoDespachoId.Value > 0)
        {
            where.Add("ISNULL(c.estado_despacho_id, 1) = @EstadoDespachoId");
            args.Add("EstadoDespachoId", estadoDespachoId.Value);
        }

        var filter = string.Join(" AND ", where);
        var fromSql = @"FROM comprobante c
            INNER JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id = c.tipo_doc_com_id
            LEFT JOIN cotiza ct ON ct.cotiza_id = c.cotiza_id
            INNER JOIN proveedor p ON p.proveedor_id = c.proveedor_id
            INNER JOIN tipo_comprob tc ON tc.tipo_comprob_id = c.tipo_comprob_id
            INNER JOIN estado_compro ec ON ec.estado_compro_id = c.estado_compro_id
            LEFT JOIN estado_despacho ed ON ed.estado_despacho_id = c.estado_despacho_id
            LEFT JOIN personal per ON per.personal_id = c.personal_id
            LEFT JOIN proveedor pper ON pper.proveedor_id = per.proveedor_id
            WHERE " + filter;

        var countSql = "SELECT COUNT(1) " + fromSql;
        var total = await conn.ExecuteScalarAsync<int>(countSql, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        var selectSql = $@"SELECT
            c.comprobante_id AS ComprobanteId,
            tdc.abrev AS TipoDocumento,
            c.serie + '-' + c.numero AS SerieNumero,
            c.fecha AS Fecha,
            p.razon_social AS RazonSocial,
            p.num_doc_iden AS NumeroDocumento,
            ISNULL(ct.cotiza, '') AS CotizacionPedido,
            ISNULL(ed.estado_despacho_id, 1) AS EstadoDespachoId,
            ISNULL(ed.estado_despacho, '1') AS EstadoDespachoCodigo,
            ISNULL(ed.nombre, 'PENDIENTE') AS EstadoDespachoNombre,
            ISNULL(pper.razon_social, '') AS PersonalDespacho,
            c.fecha_desp AS FechaDespacho,
            ISNULL(c.observa_desp, '') AS ObservacionDespacho,
            ISNULL(c.entrega_desp, '') AS EntregaDespacho,
            c.estado AS Estado,
            COALESCE(c.fecha_modifica, c.fecha_creacion, c.fecha) AS FechaRevision
            {fromSql}
            ORDER BY {columns[model.Sort]} {model.Dir}
            OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";

        var rows = (await conn.QueryAsync<DespachoItemModel>(selectSql, args)).AsList();
        model.Items = rows;
        model.VisibleCount = rows.Count;
        return model;
    }

    public async Task<DespachoDetalleModel?> GetDespachoDetalleAsync(int empresaId, int contextoId, int id)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0) return null;
        using var conn = factory.CreateConnection();

        var cabSql = @"SELECT
            c.comprobante_id AS ComprobanteId,
            tdc.abrev AS TipoDocumento,
            c.serie + '-' + c.numero AS SerieNumero,
            c.fecha AS Fecha,
            p.razon_social AS RazonSocial,
            p.num_doc_iden AS NumeroDocumento,
            ISNULL(ct.cotiza, '') AS CotizacionPedido,
            ISNULL(ed.estado_despacho_id, 1) AS EstadoDespachoId,
            ISNULL(ed.estado_despacho, '1') AS EstadoDespachoCodigo,
            ISNULL(ed.nombre, 'PENDIENTE') AS EstadoDespachoNombre,
            ISNULL(pper.razon_social, '') AS PersonalDespacho,
            c.fecha_desp AS FechaDespacho,
            ISNULL(c.observa_desp, '') AS ObservacionDespacho,
            ISNULL(c.entrega_desp, '') AS EntregaDespacho,
            c.estado AS Estado,
            COALESCE(c.fecha_modifica, c.fecha_creacion, c.fecha) AS FechaRevision
            FROM comprobante c
            INNER JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id = c.tipo_doc_com_id
            LEFT JOIN cotiza ct ON ct.cotiza_id = c.cotiza_id
            INNER JOIN proveedor p ON p.proveedor_id = c.proveedor_id
            LEFT JOIN estado_despacho ed ON ed.estado_despacho_id = c.estado_despacho_id
            LEFT JOIN personal per ON per.personal_id = c.personal_id
            LEFT JOIN proveedor pper ON pper.proveedor_id = per.proveedor_id
            WHERE c.comprobante_id = @Id AND c.periodo_emp_sede_id = @ContextoId AND c.estado = 'A'";

        var cab = await conn.QuerySingleOrDefaultAsync<DespachoItemModel>(cabSql, new { Id = id, ContextoId = contextoId });
        if (cab is null) return null;

        var itemsSql = @"SELECT
            cp.comprob_prod_id AS ComprobanteProductoId,
            cp.producto_id AS ProductoId,
            p.codigo AS Codigo,
            p.descripcion AS Descripcion,
            um.abrev AS UnidadMedida,
            cp.cantidad AS Cantidad,
            ISNULL(cp.cant_entrega, 0) AS CantidadEntregada,
            cp.fecha_desp AS FechaDespacho,
            ISNULL(cp.observa_desp, '') AS ObservacionDespacho,
            ISNULL(cp.entrega_desp, '') AS EntregaDespacho,
            COALESCE(cp.fecha_modifica, cp.fecha_creacion) AS FechaRevision
            FROM comprob_prod cp
            INNER JOIN producto p ON p.producto_id = cp.producto_id
            INNER JOIN unidad_medida um ON um.unidad_medida_id = p.unidad_medida_id
            WHERE cp.comprobante_id = @Id AND cp.estado = 'A'
            ORDER BY cp.comprob_prod_id";

        var items = (await conn.QueryAsync<DespachoDetalleItemModel>(itemsSql, new { Id = id })).AsList();

        return new DespachoDetalleModel
        {
            Cabecera = cab,
            Items = items
        };
    }

    public async Task<(bool success, string message)> EntregarItemDespachoAsync(int empresaId, int contextoId, int userId, DespachoEntregaSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.ComprobanteId <= 0 || model.ComprobanteProductoId <= 0 || model.Cantidad <= 0)
            return (false, "Datos de entrega inválidos.");

        using var conn = factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var item = await conn.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT comprob_prod_id, cantidad, ISNULL(cant_entrega, 0) cant_entrega FROM comprob_prod WHERE comprob_prod_id = @Id AND comprobante_id = @CompId AND estado = 'A'",
                new { Id = model.ComprobanteProductoId, CompId = model.ComprobanteId }, tx);

            if (item is null)
            {
                tx.Rollback();
                return (false, "El producto no pertenece al comprobante indicado.");
            }

            decimal totalCant = (decimal)item.cantidad;
            decimal yaEntregado = (decimal)item.cant_entrega;
            decimal nuevaEntrega = yaEntregado + model.Cantidad;

            if (nuevaEntrega > totalCant)
            {
                tx.Rollback();
                return (false, $"La cantidad a entregar excede el saldo pendiente (Pendiente: {totalCant - yaEntregado:N2}).");
            }

            // Registrar en comprob_p_ent
            await conn.ExecuteAsync(@"INSERT INTO comprob_p_ent (
                comprob_prod_id, cantidad, fecha, entrega_desp, observa, personal_id, estado, usuario_creacion_id, fecha_creacion
            ) VALUES (
                @ComprobProdId, @Cantidad, GETDATE(), 'PARCIAL', @Observa, @PersonalId, 'A', @UserId, GETDATE()
            )", new
            {
                ComprobProdId = model.ComprobanteProductoId,
                Cantidad = model.Cantidad,
                Observa = model.Observacion ?? "",
                PersonalId = model.PersonalId,
                UserId = userId
            }, tx);

            // Actualizar comprob_prod
            await conn.ExecuteAsync(@"UPDATE comprob_prod SET
                cant_entrega = @NuevaEntrega,
                fecha_desp = GETDATE(),
                observa_desp = @Observa,
                usuario_modifica_id = @UserId,
                fecha_modifica = GETDATE()
                WHERE comprob_prod_id = @Id", new
            {
                NuevaEntrega = nuevaEntrega,
                Observa = model.Observacion ?? "",
                UserId = userId,
                Id = model.ComprobanteProductoId
            }, tx);

            // Evaluar estado general del comprobante
            var counts = await conn.QuerySingleOrDefaultAsync<dynamic>(@"SELECT
                COUNT(1) TotalItems,
                SUM(CASE WHEN ISNULL(cant_entrega, 0) >= cantidad THEN 1 ELSE 0 END) ItemsCompletos,
                SUM(CASE WHEN ISNULL(cant_entrega, 0) > 0 THEN 1 ELSE 0 END) ItemsConEntrega
                FROM comprob_prod
                WHERE comprobante_id = @CompId AND estado = 'A'", new { CompId = model.ComprobanteId }, tx);

            int estadoDesp = 1; // PENDIENTE
            if (counts != null)
            {
                if (counts.ItemsCompletos == counts.TotalItems) estadoDesp = 3; // ATENDIDO
                else if (counts.ItemsConEntrega > 0) estadoDesp = 2; // ATENDIDO PARCIAL
            }

            await conn.ExecuteAsync(@"UPDATE comprobante SET
                estado_despacho_id = @EstadoDesp,
                fecha_desp = GETDATE(),
                usuario_modifica_id = @UserId,
                fecha_modifica = GETDATE()
                WHERE comprobante_id = @CompId", new
            {
                EstadoDesp = estadoDesp,
                UserId = userId,
                CompId = model.ComprobanteId
            }, tx);

            tx.Commit();
            return (true, "Entrega registrada satisfactoriamente.");
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, "Error al registrar la entrega: " + ex.Message);
        }
    }

    public async Task<(bool success, string message)> CerrarDespachoAsync(int empresaId, int contextoId, int userId, DespachoCerrarSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.ComprobanteId <= 0)
            return (false, "Identificador de despacho no válido.");

        using var conn = factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            // Marcar todos los productos como entregados al total
            await conn.ExecuteAsync(@"UPDATE comprob_prod SET
                cant_entrega = cantidad,
                fecha_desp = GETDATE(),
                observa_desp = @Observacion,
                usuario_modifica_id = @UserId,
                fecha_modifica = GETDATE()
                WHERE comprobante_id = @CompId AND estado = 'A'", new
            {
                Observacion = model.Observacion ?? "Despacho cerrado",
                UserId = userId,
                CompId = model.ComprobanteId
            }, tx);

            // Actualizar comprobante a ATENDIDO (3)
            await conn.ExecuteAsync(@"UPDATE comprobante SET
                estado_despacho_id = 3,
                fecha_desp = GETDATE(),
                observa_desp = @Observacion,
                usuario_modifica_id = @UserId,
                fecha_modifica = GETDATE()
                WHERE comprobante_id = @CompId", new
            {
                Observacion = model.Observacion ?? "Despacho cerrado",
                UserId = userId,
                CompId = model.ComprobanteId
            }, tx);

            tx.Commit();
            return (true, "Despacho cerrado y atendido completamente.");
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, "Error al cerrar el despacho: " + ex.Message);
        }
    }
}

