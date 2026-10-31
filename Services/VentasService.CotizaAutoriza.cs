using System.Data;
using System.Security.Cryptography;
using System.Text;
using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services;

public partial class VentasService
{
    private const string CotizaAutorizaSelect = @"SELECT
        ca.cotiza_autoriza_id Id,
        ca.cotiza_autoriza_id CotizacionAutorizacionId,
        ca.cotiza_id CotizacionId,
        RTRIM(es.nombre) EmpresaSedeNombre,
        RTRIM(ta.tipo_autoriza) TipoAutorizacionCodigo,
        RTRIM(ta.nombre) TipoAutorizacionNombre,
        RTRIM(c.cotiza) Codigo,
        c.fecha Fecha,
        RTRIM(p.razon_social) ClienteNombre,
        COALESCE(NULLIF(p.numero_ruc, ''), p.num_doc_iden, '') ClienteDocumento,
        RTRIM(p2.razon_social) VendedorNombre,
        RTRIM(m.simbolo) MonedaSimbolo,
        c.monto_total MontoTotal,
        ea.estado_aproba_id EstadoAprobacionId,
        RTRIM(ea.estado_aproba) EstadoAprobacionCodigo,
        RTRIM(ea.nombre) EstadoAprobacionNombre,
        ca.estado Estado,
        COALESCE(ca.fecha_modifica, ca.fecha_creacion) FechaRevision
        FROM cotiza_autoriza ca
        INNER JOIN tipo_autoriza ta ON ta.tipo_autoriza_id = ca.tipo_autoriza_id
        INNER JOIN cotiza c ON c.cotiza_id = ca.cotiza_id
        INNER JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id = c.periodo_emp_sede_id
        INNER JOIN empresa_sede es ON es.empresa_sede_id = pes.empresa_sede_id
        INNER JOIN proveedor p ON p.proveedor_id = c.proveedor_id
        INNER JOIN vendedor v ON v.vendedor_id = c.vendedor_id
        INNER JOIN proveedor p2 ON p2.proveedor_id = v.proveedor_id
        INNER JOIN moneda m ON m.moneda_id = c.moneda_id
        INNER JOIN estado_aproba ea ON ea.estado_aproba_id = ca.estado_aproba_id ";

    private static string CalculateAuthRevision(int id, DateTime fecha, string estado, int authId, int cotId, int eaId, decimal total)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{id}|{fecha.Ticks}|{estado}|{authId}|{cotId}|{eaId}|{total}")));

    public async Task<CotizaAutorizaPageModel> GetCotizacionesAutorizarAsync(
        int empresaId,
        int? periodoEmpresaSedeId,
        string? search = null,
        string? codigo = null,
        string? cliente = null,
        string? vendedor = null,
        int? estadoAprobacionId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new CotizaAutorizaPageModel
        {
            Search = search,
            Codigo = codigo,
            Cliente = cliente,
            Vendedor = vendedor,
            EstadoAprobacionId = estadoAprobacionId,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        };

        var columns = new Dictionary<string, string>
        {
            ["codigo"] = "c.cotiza",
            ["fecha"] = "c.fecha",
            ["cliente"] = "p.razon_social",
            ["vendedor"] = "p2.razon_social",
            ["tipo"] = "ta.nombre",
            ["total"] = "c.monto_total",
            ["estado"] = "ea.nombre",
            ["id"] = "ca.cotiza_autoriza_id"
        };
        model.Ordenar(sort, dir, columns.Keys, "fecha", "desc");
        model.Paginar(0, page, top);
        if (empresaId <= 0) return model;

        using var conn = factory.CreateConnection();

        // Estados de aprobación activos
        var estados = await conn.QueryAsync<LookupItemModel>(
            @"SELECT estado_aproba_id Id, RTRIM(nombre) Nombre, RTRIM(estado_aproba) Codigo
              FROM estado_aproba WHERE estado = 'A' ORDER BY estado_aproba_id");
        model.EstadosAprobacion = estados.ToList();

        var where = @"WHERE es.empresa_id = @EmpresaId AND ca.estado = 'A' ";
        var p = new DynamicParameters();
        p.Add("@EmpresaId", empresaId);

        if (periodoEmpresaSedeId.HasValue && periodoEmpresaSedeId.Value > 0)
        {
            where += "AND pes.periodo_emp_sede_id = @PeriodoSedeId ";
            p.Add("@PeriodoSedeId", periodoEmpresaSedeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            where += @"AND (c.cotiza LIKE @Search OR p.razon_social LIKE @Search OR p.num_doc_iden LIKE @Search OR p.numero_ruc LIKE @Search OR p2.razon_social LIKE @Search) ";
            p.Add("@Search", $"%{search.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(codigo))
        {
            where += "AND c.cotiza LIKE @Codigo ";
            p.Add("@Codigo", $"%{codigo.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(cliente))
        {
            where += "AND (p.razon_social LIKE @Cliente OR p.num_doc_iden LIKE @Cliente OR p.numero_ruc LIKE @Cliente) ";
            p.Add("@Cliente", $"%{cliente.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(vendedor))
        {
            where += "AND p2.razon_social LIKE @Vendedor ";
            p.Add("@Vendedor", $"%{vendedor.Trim()}%");
        }

        if (estadoAprobacionId.HasValue && estadoAprobacionId.Value > 0)
        {
            where += "AND ca.estado_aproba_id = @EstadoAprobacionId ";
            p.Add("@EstadoAprobacionId", estadoAprobacionId.Value);
        }

        if (fechaDesde.HasValue)
        {
            where += "AND c.fecha >= @FechaDesde ";
            p.Add("@FechaDesde", fechaDesde.Value.Date);
        }

        if (fechaHasta.HasValue)
        {
            where += "AND c.fecha < @FechaHasta ";
            p.Add("@FechaHasta", fechaHasta.Value.Date.AddDays(1));
        }

        var total = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM cotiza_autoriza ca INNER JOIN tipo_autoriza ta ON ta.tipo_autoriza_id = ca.tipo_autoriza_id INNER JOIN cotiza c ON c.cotiza_id = ca.cotiza_id INNER JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id = c.periodo_emp_sede_id INNER JOIN empresa_sede es ON es.empresa_sede_id = pes.empresa_sede_id INNER JOIN proveedor p ON p.proveedor_id = c.proveedor_id INNER JOIN vendedor v ON v.vendedor_id = c.vendedor_id INNER JOIN proveedor p2 ON p2.proveedor_id = v.proveedor_id INNER JOIN estado_aproba ea ON ea.estado_aproba_id = ca.estado_aproba_id {where}", p);
        model.Paginar(total, page, top);
        if (total == 0) return model;

        var sql = $"{CotizaAutorizaSelect} {where} ORDER BY {columns[model.Sort]} {model.Dir}, ca.cotiza_autoriza_id DESC OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";
        p.Add("@Offset", (model.Page - 1) * model.PageSize);
        p.Add("@Top", model.PageSize);

        var rows = await conn.QueryAsync<CotizaAutorizaItemModel>(sql, p);
        model.Items = rows.ToList();
        model.VisibleCount = model.Items.Count;

        return model;
    }

    public async Task<CotizaAutorizaDetalleModel?> GetCotizacionAutorizacionDetalleAsync(int empresaId, int cotizacionAutorizacionId)
    {
        if (empresaId <= 0 || cotizacionAutorizacionId <= 0) return null;
        using var conn = factory.CreateConnection();

        var sqlHeader = @"SELECT
            ca.cotiza_autoriza_id Id,
            ca.cotiza_autoriza_id CotizacionAutorizacionId,
            ca.cotiza_id CotizacionId,
            ca.estado_aproba_id EstadoAprobacionId,
            RTRIM(ea.estado_aproba) EstadoAprobacionCodigo,
            RTRIM(ea.nombre) EstadoAprobacionNombre,
            RTRIM(ta.tipo_autoriza) TipoAutorizacionCodigo,
            RTRIM(ta.nombre) TipoAutorizacionNombre,
            RTRIM(c.cotiza) Codigo,
            c.fecha Fecha,
            c.proveedor_id ClienteId,
            RTRIM(p.razon_social) ClienteNombre,
            COALESCE(NULLIF(p.numero_ruc, ''), p.num_doc_iden, '') ClienteDocumento,
            c.vendedor_id VendedorId,
            RTRIM(p2.razon_social) VendedorNombre,
            c.moneda_id MonedaId,
            RTRIM(m.nombre) MonedaNombre,
            RTRIM(m.simbolo) MonedaSimbolo,
            c.tipo_c_pago_id TipoCondicionPagoId,
            RTRIM(tcp.nombre) TipoCondicionPagoNombre,
            c.lista_precio_id ListaPrecioId,
            RTRIM(lp.nombre) ListaPrecioNombre,
            c.observa Observacion,
            c.monto_tc TipoCambio,
            it.tasa ImpuestoTasa,
            c.monto_bruto MontoBruto,
            c.monto_dcto MontoDctoItem,
            c.monto_neto MontoNeto,
            c.monto_igv MontoIgv,
            c.monto_total MontoTotal,
            ca.estado Estado,
            COALESCE(ca.fecha_modifica, ca.fecha_creacion) FechaRevision
            FROM cotiza_autoriza ca
            INNER JOIN tipo_autoriza ta ON ta.tipo_autoriza_id = ca.tipo_autoriza_id
            INNER JOIN estado_aproba ea ON ea.estado_aproba_id = ca.estado_aproba_id
            INNER JOIN cotiza c ON c.cotiza_id = ca.cotiza_id
            INNER JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id = c.periodo_emp_sede_id
            INNER JOIN empresa_sede es ON es.empresa_sede_id = pes.empresa_sede_id
            INNER JOIN proveedor p ON p.proveedor_id = c.proveedor_id
            INNER JOIN vendedor v ON v.vendedor_id = c.vendedor_id
            INNER JOIN proveedor p2 ON p2.proveedor_id = v.proveedor_id
            INNER JOIN moneda m ON m.moneda_id = c.moneda_id
            LEFT JOIN lista_precio lp ON lp.lista_precio_id = c.lista_precio_id
            LEFT JOIN impuesto_tasa it ON it.impuesto_tasa_id = c.impuesto_tasa_id
            LEFT JOIN tipo_c_pago tcp ON tcp.tipo_c_pago_id = c.tipo_c_pago_id
            WHERE ca.cotiza_autoriza_id = @Id AND es.empresa_id = @EmpresaId AND ca.estado = 'A'";

        var h = await conn.QueryFirstOrDefaultAsync(sqlHeader, new { Id = cotizacionAutorizacionId, EmpresaId = empresaId });
        if (h is null) return null;

        var detalle = new CotizaAutorizaDetalleModel
        {
            Id = (int)h.Id,
            CotizacionAutorizacionId = (int)h.CotizacionAutorizacionId,
            CotizacionId = (int)h.CotizacionId,
            Revision = CalculateAuthRevision((int)h.Id, (DateTime)h.FechaRevision, (string)h.Estado, (int)h.CotizacionAutorizacionId, (int)h.CotizacionId, (int)h.EstadoAprobacionId, (decimal)h.MontoTotal),
            TipoAutorizacionCodigo = (string?)h.TipoAutorizacionCodigo,
            TipoAutorizacionNombre = (string?)h.TipoAutorizacionNombre,
            EstadoAprobacionId = (int)h.EstadoAprobacionId,
            EstadoAprobacionCodigo = (string?)h.EstadoAprobacionCodigo,
            EstadoAprobacionNombre = (string?)h.EstadoAprobacionNombre,
            Codigo = (string?)h.Codigo,
            Fecha = (DateTime?)h.Fecha,
            FechaTexto = ((DateTime?)h.Fecha)?.ToString("dd/MM/yyyy"),
            ClienteId = (int)h.ClienteId,
            ClienteNombre = (string?)h.ClienteNombre,
            ClienteDocumento = (string?)h.ClienteDocumento,
            VendedorId = (int)h.VendedorId,
            VendedorNombre = (string?)h.VendedorNombre,
            MonedaId = (int)h.MonedaId,
            MonedaNombre = (string?)h.MonedaNombre,
            MonedaSimbolo = (string?)h.MonedaSimbolo,
            TipoCondicionPagoId = (int)(h.TipoCondicionPagoId ?? 0),
            TipoCondicionPagoNombre = (string?)h.TipoCondicionPagoNombre,
            ListaPrecioId = (int)(h.ListaPrecioId ?? 0),
            ListaPrecioNombre = (string?)h.ListaPrecioNombre,
            Observacion = (string?)h.Observacion,
            TipoCambio = (decimal)(h.TipoCambio ?? 1.0m),
            ImpuestoTasa = (decimal)(h.ImpuestoTasa ?? 18.0m),
            MontoBruto = (decimal)h.MontoBruto,
            MontoDctoItem = (decimal)h.MontoDctoItem,
            MontoNeto = (decimal)h.MontoNeto,
            MontoIgv = (decimal)h.MontoIgv,
            MontoTotal = (decimal)h.MontoTotal,
            Estado = (string?)h.Estado
        };

        // Line items
        var sqlItems = @"SELECT
            cp.cotiza_prod_id Id,
            cp.cotiza_prod_id CotizacionProductoId,
            cp.cotiza_id CotizacionId,
            cp.producto_id ProductoId,
            RTRIM(p.producto) ProductoCodigo,
            RTRIM(p.descripcion) ProductoNombre,
            RTRIM(um.abrev) UnidadMedidaCodigo,
            cp.descripcion Descripcion,
            cp.cantidad Cantidad,
            cp.valor_u_lista ValorUnitarioLista,
            cp.precio_u_lista PrecioUnitarioLista,
            cp.valor_unitario ValorUnitario,
            cp.precio_unitario PrecioUnitario,
            COALESCE(cp.precio_auto, cp.precio_unitario) PrecioAutorizado,
            cp.porc_dcto PorcentajeDescuento,
            cp.monto_neto MontoNeto,
            cp.monto_igv MontoIgv,
            cp.monto_total MontoTotal,
            RTRIM(ti.tipo_igv) TipoIgv,
            cp.soli_auto_dcto SolicitaAutorizacionDescuento,
            cp.inc_dcto_lp IncluyeDescuentoListaPrecio,
            cp.sin_saldo SinSaldo,
            cp.estado Estado
            FROM cotiza_prod cp
            INNER JOIN producto p ON p.producto_id = cp.producto_id
            INNER JOIN unidad_medida um ON um.unidad_medida_id = p.unidad_medida_id
            LEFT JOIN tipo_afecta_igv tai ON tai.tipo_afecta_igv_id = cp.tipo_afecta_igv_id
            LEFT JOIN tipo_igv ti ON ti.tipo_igv_id = tai.tipo_igv_id
            WHERE cp.cotiza_id = @CotizacionId AND cp.estado = 'A'
            ORDER BY cp.cotiza_prod_id";

        var items = await conn.QueryAsync<CotizaAutorizaProductoItemModel>(sqlItems, new { CotizacionId = detalle.CotizacionId });
        detalle.Productos = items.ToList();

        return detalle;
    }

    public async Task<(bool success, string message, int id)> AprobarCotizacionAutorizacionAsync(
        int empresaId,
        int periodoEmpresaSedeId,
        int userId,
        AprobarCotizacionAutorizacionSolicitud model)
    {
        if (empresaId <= 0 || model.CotizacionAutorizacionId <= 0 || model.CotizacionId <= 0)
            return (false, "Parámetros de autorización inválidos.", 0);

        using var conn = factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            // Validar existencia y pertenencia
            var auth = await conn.QueryFirstOrDefaultAsync(
                @"SELECT ca.cotiza_autoriza_id Id, ca.estado_aproba_id EstadoAprobacionId, RTRIM(ta.tipo_autoriza) TipoAutorizacion,
                         ca.estado Estado, c.monto_total MontoTotal,
                         COALESCE(ca.fecha_modifica, ca.fecha_creacion) FechaRevision
                  FROM cotiza_autoriza ca WITH (UPDLOCK, HOLDLOCK)
                  INNER JOIN tipo_autoriza ta ON ta.tipo_autoriza_id = ca.tipo_autoriza_id
                  INNER JOIN cotiza c ON c.cotiza_id = ca.cotiza_id
                  INNER JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id = c.periodo_emp_sede_id
                  INNER JOIN empresa_sede es ON es.empresa_sede_id = pes.empresa_sede_id
                  WHERE ca.cotiza_autoriza_id = @Id AND ca.cotiza_id = @CotizacionId AND es.empresa_id = @EmpresaId AND ca.estado = 'A'",
                new { Id = model.CotizacionAutorizacionId, CotizacionId = model.CotizacionId, EmpresaId = empresaId },
                tx);

            if (auth is null)
            {
                tx.Rollback();
                return (false, "No se encontró la solicitud de autorización.", 0);
            }

            if ((int)auth.EstadoAprobacionId != 1) // 1 = Pendiente de Aprobación
            {
                tx.Rollback();
                return (false, "La solicitud ya ha sido atendida previamente.", 0);
            }

            var currentRev = CalculateAuthRevision((int)auth.Id, (DateTime)auth.FechaRevision, (string)auth.Estado, model.CotizacionAutorizacionId, model.CotizacionId, (int)auth.EstadoAprobacionId, (decimal)auth.MontoTotal);
            if (!string.IsNullOrEmpty(model.Revision) && currentRev != model.Revision)
            {
                tx.Rollback();
                return (false, "El registro fue modificado por otro usuario mientras lo revisaba. Actualice el listado.", 0);
            }

            // Tasa de impuesto de la cotización
            var tasa = await conn.ExecuteScalarAsync<decimal>(
                @"SELECT COALESCE(it.tasa, 18.0) FROM cotiza c
                  LEFT JOIN impuesto_tasa it ON it.impuesto_tasa_id = c.impuesto_tasa_id
                  WHERE c.cotiza_id = @CotizacionId",
                new { CotizacionId = model.CotizacionId },
                tx);

            var divisorIgv = 1.0m + (tasa / 100.0m);

            // Si es descuento en precio unitario (01), actualizar productos aprobados
            if ((string)auth.TipoAutorizacion == "01" && model.Items != null && model.Items.Count > 0)
            {
                foreach (var item in model.Items)
                {
                    if (item.CotizacionProductoId <= 0 || item.PrecioAutorizado <= 0) continue;

                    var prod = await conn.QueryFirstOrDefaultAsync(
                        @"SELECT cotiza_prod_id Id, cantidad Cantidad, tipo_afecta_igv_id TipoAfectacionId
                          FROM cotiza_prod WITH (UPDLOCK, HOLDLOCK)
                          WHERE cotiza_prod_id = @Id AND cotiza_id = @CotizacionId AND estado = 'A'",
                        new { Id = item.CotizacionProductoId, CotizacionId = model.CotizacionId },
                        tx);

                    if (prod is null) continue;

                    decimal cant = (decimal)prod.Cantidad;
                    decimal precioAuto = item.PrecioAutorizado;
                    decimal valUnit = Math.Round(precioAuto / divisorIgv, 6);
                    decimal montoNeto = Math.Round(cant * valUnit, 2);
                    decimal montoIgv = Math.Round(montoNeto * (tasa / 100.0m), 2);
                    decimal montoTotal = Math.Round(cant * precioAuto, 2);

                    await conn.ExecuteAsync(
                        @"UPDATE cotiza_prod
                          SET precio_unitario = @PrecioAuto,
                              valor_unitario = @ValUnit,
                              cant_bloqueada = 'S',
                              soli_auto_dcto = 'N',
                              precio_auto = @PrecioAuto,
                              monto_bruto = @MontoNeto,
                              monto_neto = @MontoNeto,
                              monto_igv = @MontoIgv,
                              monto_total = @MontoTotal,
                              usuario_modifica_id = @UserId,
                              fecha_modifica = GETDATE()
                          WHERE cotiza_prod_id = @Id",
                        new
                        {
                            Id = item.CotizacionProductoId,
                            PrecioAuto = precioAuto,
                            ValUnit = valUnit,
                            MontoNeto = montoNeto,
                            MontoIgv = montoIgv,
                            MontoTotal = montoTotal,
                            UserId = userId
                        },
                        tx);
                }
            }

            // Actualizar estado de cotiza_autoriza a 2 = APROBADA
            await conn.ExecuteAsync(
                @"EXEC sp_cotiza_autoriza_actualizar
                    @cotizacionAutorizacionId = @AuthId,
                    @usuarioId = @UserId,
                    @fechaAutorizacion = @FechaAuto,
                    @estadoAprobacionId = 2,
                    @estado = 'A',
                    @usuarioAccionId = @UserId",
                new
                {
                    AuthId = model.CotizacionAutorizacionId,
                    UserId = userId,
                    FechaAuto = DateTime.Now
                },
                tx);

            // Recalcular montos de la cotización
            await conn.ExecuteAsync(
                "EXEC sp_cotiza_actualizarMonto @cotizacionId = @CotizacionId, @usuarioAccionId = @UserId",
                new { CotizacionId = model.CotizacionId, UserId = userId },
                tx);

            tx.Commit();
            return (true, "Solicitud de autorización aprobada exitosamente.", model.CotizacionAutorizacionId);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, $"Error al aprobar la cotización: {ex.Message}", 0);
        }
    }

    public async Task<(bool success, string message, int id)> DesaprobarCotizacionAutorizacionAsync(
        int empresaId,
        int periodoEmpresaSedeId,
        int userId,
        DesaprobarCotizacionAutorizacionSolicitud model)
    {
        if (empresaId <= 0 || model.CotizacionAutorizacionId <= 0 || model.CotizacionId <= 0)
            return (false, "Parámetros de desaprobación inválidos.", 0);

        using var conn = factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            var auth = await conn.QueryFirstOrDefaultAsync(
                @"SELECT ca.cotiza_autoriza_id Id, ca.estado_aproba_id EstadoAprobacionId, RTRIM(ta.tipo_autoriza) TipoAutorizacion,
                         ca.estado Estado, c.monto_total MontoTotal,
                         COALESCE(ca.fecha_modifica, ca.fecha_creacion) FechaRevision
                  FROM cotiza_autoriza ca WITH (UPDLOCK, HOLDLOCK)
                  INNER JOIN tipo_autoriza ta ON ta.tipo_autoriza_id = ca.tipo_autoriza_id
                  INNER JOIN cotiza c ON c.cotiza_id = ca.cotiza_id
                  INNER JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id = c.periodo_emp_sede_id
                  INNER JOIN empresa_sede es ON es.empresa_sede_id = pes.empresa_sede_id
                  WHERE ca.cotiza_autoriza_id = @Id AND ca.cotiza_id = @CotizacionId AND es.empresa_id = @EmpresaId AND ca.estado = 'A'",
                new { Id = model.CotizacionAutorizacionId, CotizacionId = model.CotizacionId, EmpresaId = empresaId },
                tx);

            if (auth is null)
            {
                tx.Rollback();
                return (false, "No se encontró la solicitud de autorización.", 0);
            }

            if ((int)auth.EstadoAprobacionId != 1)
            {
                tx.Rollback();
                return (false, "La solicitud ya ha sido atendida previamente.", 0);
            }

            var currentRev = CalculateAuthRevision((int)auth.Id, (DateTime)auth.FechaRevision, (string)auth.Estado, model.CotizacionAutorizacionId, model.CotizacionId, (int)auth.EstadoAprobacionId, (decimal)auth.MontoTotal);
            if (!string.IsNullOrEmpty(model.Revision) && currentRev != model.Revision)
            {
                tx.Rollback();
                return (false, "El registro fue modificado por otro usuario mientras lo revisaba. Actualice el listado.", 0);
            }

            // Quitar marca de solicitud de descuento en los items de la cotización
            await conn.ExecuteAsync(
                @"UPDATE cotiza_prod
                  SET soli_auto_dcto = 'N',
                      usuario_modifica_id = @UserId,
                      fecha_modifica = GETDATE()
                  WHERE cotiza_id = @CotizacionId AND soli_auto_dcto = 'S' AND estado = 'A'",
                new { CotizacionId = model.CotizacionId, UserId = userId },
                tx);

            // Actualizar estado de cotiza_autoriza a 3 = DESAPROBADA
            await conn.ExecuteAsync(
                @"EXEC sp_cotiza_autoriza_actualizar
                    @cotizacionAutorizacionId = @AuthId,
                    @usuarioId = @UserId,
                    @fechaAutorizacion = @FechaAuto,
                    @estadoAprobacionId = 3,
                    @estado = 'A',
                    @usuarioAccionId = @UserId",
                new
                {
                    AuthId = model.CotizacionAutorizacionId,
                    UserId = userId,
                    FechaAuto = DateTime.Now
                },
                tx);

            // Recalcular montos de la cotización
            await conn.ExecuteAsync(
                "EXEC sp_cotiza_actualizarMonto @cotizacionId = @CotizacionId, @usuarioAccionId = @UserId",
                new { CotizacionId = model.CotizacionId, UserId = userId },
                tx);

            tx.Commit();
            return (true, "Solicitud de autorización desaprobada.", model.CotizacionAutorizacionId);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, $"Error al desaprobar la cotización: {ex.Message}", 0);
        }
    }
}
