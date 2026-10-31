using System.Data;
using AspNet10ArcoERP.Models;
using ClosedXML.Excel;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class VentasService
{
    private const string PedidoSelect = @"SELECT c.cotiza_id Id, c.periodo_emp_sede_id PeriodoEmpresaSedeId, RTRIM(c.cotiza) Codigo,
        c.fecha Fecha, c.fecha_ent FechaEntrega, c.proveedor_id ClienteId, RTRIM(p.razon_social) ClienteNombre,
        COALESCE(NULLIF(p.numero_ruc,''), p.num_doc_iden, '') ClienteDocumento,
        c.vendedor_id VendedorId, RTRIM(p2.razon_social) VendedorNombre,
        c.moneda_id MonedaId, RTRIM(m.simbolo) MonedaSimbolo, RTRIM(m.nombre) MonedaNombre, c.monto_tc MontoTipoCambio,
        c.lista_precio_id ListaPrecioId, RTRIM(lp.nombre) ListaPrecioNombre,
        c.tipo_c_pago_id TipoCondicionPagoId, RTRIM(tcp.nombre) CondicionPagoNombre,
        c.observa Observacion, c.monto_bruto MontoBruto, c.porc_dcto PorcentajeDescuento,
        c.monto_dcto MontoDescuento, c.monto_neto MontoNeto, c.monto_igv MontoIgv, c.monto_total MontoTotal,
        COALESCE(c.monto_anticipo, 0) MontoAnticipo,
        c.estado_cotiza_id EstadoCotizacionId, RTRIM(ec.estado_cotiza) EstadoCotizacionCodigo, RTRIM(ec.nombre) EstadoCotizacionNombre,
        c.es_pedido EsPedido, c.envia_prod EnviadoProduccion, c.estado Estado,
        COALESCE(c.fecha_modifica, c.fecha_creacion) FechaRevision,
        (SELECT COUNT(*) FROM cotiza_prod cp WHERE cp.cotiza_id = c.cotiza_id AND cp.estado = 'A') CantidadItems
        FROM cotiza c
        JOIN proveedor p ON p.proveedor_id = c.proveedor_id
        JOIN vendedor v ON v.vendedor_id = c.vendedor_id
        JOIN proveedor p2 ON p2.proveedor_id = v.proveedor_id
        JOIN moneda m ON m.moneda_id = c.moneda_id
        JOIN estado_cotiza ec ON ec.estado_cotiza_id = c.estado_cotiza_id
        LEFT JOIN lista_precio lp ON lp.lista_precio_id = c.lista_precio_id
        LEFT JOIN tipo_c_pago tcp ON tcp.tipo_c_pago_id = c.tipo_c_pago_id ";

    public async Task<PedidoPageModel> GetPedidosAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        string? codigo = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? vendedorId = null,
        int? estadoCotizacionId = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new PedidoPageModel
        {
            Search = search,
            Codigo = codigo,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            VendedorId = vendedorId,
            EstadoCotizacionId = estadoCotizacionId
        };

        var columns = new Dictionary<string, string>
        {
            ["codigo"] = "c.cotiza",
            ["fecha"] = "c.fecha",
            ["cliente"] = "p.razon_social",
            ["vendedor"] = "p2.razon_social",
            ["total"] = "c.monto_total",
            ["anticipo"] = "c.monto_anticipo",
            ["estado"] = "ec.nombre",
            ["id"] = "c.cotiza_id"
        };
        model.Ordenar(sort, dir, columns.Keys, "fecha", "desc");
        model.Paginar(0, page, top);
        if (empresaId <= 0 || contextoId <= 0) return model;

        using var conn = factory.CreateConnection();

        // Dropdown lookups
        var vendedores = (await conn.QueryAsync<LookupItemModel>(@"SELECT v.vendedor_id Id, RTRIM(p.razon_social) Nombre
            FROM vendedor v JOIN proveedor p ON p.proveedor_id = v.proveedor_id
            WHERE p.empresa_id = @EmpresaId AND v.estado = 'A' ORDER BY p.razon_social", new { EmpresaId = empresaId })).AsList();

        var estados = (await conn.QueryAsync<LookupItemModel>(@"SELECT estado_cotiza_id Id, RTRIM(nombre) Nombre, RTRIM(estado_cotiza) Codigo
            FROM estado_cotiza WHERE estado = 'A' AND estado_cotiza IN ('02','03','04','05') ORDER BY estado_cotiza_id")).AsList();

        var monedas = (await conn.QueryAsync<LookupItemModel>("SELECT moneda_id Id, RTRIM(nombre) Nombre, RTRIM(simbolo) Codigo FROM moneda WHERE estado = 'A' ORDER BY moneda_id")).AsList();
        var condiciones = (await conn.QueryAsync<LookupItemModel>("SELECT tipo_c_pago_id Id, RTRIM(nombre) Nombre FROM tipo_c_pago ORDER BY tipo_c_pago_id")).AsList();
        var listas = (await conn.QueryAsync<LookupItemModel>(@"SELECT lp.lista_precio_id Id, RTRIM(lp.nombre) Nombre
            FROM lista_precio lp JOIN empresa_sede es ON es.empresa_sede_id = lp.empresa_sede_id
            WHERE es.empresa_id = @EmpresaId AND lp.estado = 'A' ORDER BY lp.nombre", new { EmpresaId = empresaId })).AsList();

        model.Vendedores = vendedores;
        model.EstadosCotizacion = estados;
        model.Monedas = monedas;
        model.CondicionesPago = condiciones;
        model.ListasPrecio = listas;

        var where = @"WHERE c.periodo_emp_sede_id = @ContextoId AND c.es_pedido = 'S' AND c.estado = 'A' ";
        if (!string.IsNullOrWhiteSpace(search))
            where += "AND (c.cotiza LIKE @Search OR p.razon_social LIKE @Search OR p.numero_ruc LIKE @Search OR p.num_doc_iden LIKE @Search) ";
        if (!string.IsNullOrWhiteSpace(codigo))
            where += "AND c.cotiza LIKE @Codigo ";
        if (fechaDesde.HasValue)
            where += "AND c.fecha >= @FechaDesde ";
        if (fechaHasta.HasValue)
            where += "AND c.fecha < @FechaHasta ";
        if (vendedorId.HasValue && vendedorId.Value > 0)
            where += "AND c.vendedor_id = @VendedorId ";
        if (estadoCotizacionId.HasValue && estadoCotizacionId.Value > 0)
            where += "AND c.estado_cotiza_id = @EstadoCotizacionId ";

        var args = new DynamicParameters(new
        {
            ContextoId = contextoId,
            EmpresaId = empresaId,
            Search = Search(search),
            Codigo = Search(codigo),
            VendedorId = vendedorId > 0 ? vendedorId : null,
            EstadoCotizacionId = estadoCotizacionId > 0 ? estadoCotizacionId : null,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta?.AddDays(1)
        });

        int total = await conn.ExecuteScalarAsync<int>(@"SELECT COUNT(*) FROM cotiza c
            JOIN proveedor p ON p.proveedor_id = c.proveedor_id " + where, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        string query = PedidoSelect + where + $" ORDER BY {columns[model.Sort]} {model.Dir}, c.cotiza_id DESC OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";
        model.Items = (await conn.QueryAsync<PedidoItemModel>(query, args)).AsList();
        model.VisibleCount = model.Items.Count;
        return model;
    }

    public async Task<PedidoDetalleModel?> GetPedidoDetalleAsync(int empresaId, int contextoId, int id)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0) return null;
        using var conn = factory.CreateConnection();

        var cabecera = await conn.QuerySingleOrDefaultAsync<PedidoItemModel>(PedidoSelect + @"JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id = c.periodo_emp_sede_id
            JOIN empresa_sede es ON es.empresa_sede_id = pes.empresa_sede_id
            WHERE c.cotiza_id = @Id AND es.empresa_id = @EmpresaId AND c.es_pedido = 'S'", new { Id = id, EmpresaId = empresaId });

        if (cabecera is null) return null;

        var productos = await GetCotizacionProductosAsync(empresaId, id);

        var vendedores = (await conn.QueryAsync<LookupItemModel>(@"SELECT v.vendedor_id Id, RTRIM(p.razon_social) Nombre
            FROM vendedor v JOIN proveedor p ON p.proveedor_id = v.proveedor_id
            WHERE p.empresa_id = @EmpresaId AND v.estado = 'A' ORDER BY p.razon_social", new { EmpresaId = empresaId })).AsList();

        var monedas = (await conn.QueryAsync<LookupItemModel>("SELECT moneda_id Id, RTRIM(nombre) Nombre, RTRIM(simbolo) Codigo FROM moneda WHERE estado = 'A' ORDER BY moneda_id")).AsList();
        var condiciones = (await conn.QueryAsync<LookupItemModel>("SELECT tipo_c_pago_id Id, RTRIM(nombre) Nombre FROM tipo_c_pago ORDER BY tipo_c_pago_id")).AsList();
        var listas = (await conn.QueryAsync<LookupItemModel>(@"SELECT lp.lista_precio_id Id, RTRIM(lp.nombre) Nombre
            FROM lista_precio lp JOIN empresa_sede es ON es.empresa_sede_id = lp.empresa_sede_id
            WHERE es.empresa_id = @EmpresaId AND lp.estado = 'A' ORDER BY lp.nombre", new { EmpresaId = empresaId })).AsList();

        return new PedidoDetalleModel
        {
            Cabecera = cabecera,
            Productos = productos,
            Vendedores = vendedores,
            Monedas = monedas,
            ListasPrecio = listas,
            CondicionesPago = condiciones
        };
    }

    public async Task<(bool success, string message, int id)> SavePedidoAsync(
        int empresaId,
        int contextoId,
        int userId,
        PedidoGuardarSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0) return (false, "Contexto de trabajo no válido.", 0);
        using var conn = factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            if (model.Id == 0)
            {
                // Numeración automática desde periodo_emp_corre para tabla '0015'
                var correRow = await conn.QuerySingleOrDefaultAsync(
                    @"SELECT periodo_emp_corre_id Id, correlativo Correlativo
                      FROM periodo_emp_corre WITH (UPDLOCK, HOLDLOCK)
                      WHERE periodo_emp_sede_id = @ContextoId AND tabla_id = 15",
                    new { ContextoId = contextoId }, tx);

                int nextNum = 1;
                if (correRow is not null)
                {
                    nextNum = ((int)correRow.Correlativo) + 1;
                    await conn.ExecuteAsync(
                        @"UPDATE periodo_emp_corre
                          SET correlativo = @Correlativo, usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                          WHERE periodo_emp_corre_id = @Id",
                        new { Id = (int)correRow.Id, Correlativo = nextNum, UserId = userId }, tx);
                }
                else
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO periodo_emp_corre (periodo_emp_sede_id, tabla_id, correlativo, usuario_creacion_id, fecha_creacion)
                          VALUES (@ContextoId, 15, @Correlativo, @UserId, GETDATE())",
                        new { ContextoId = contextoId, Correlativo = nextNum, UserId = userId }, tx);
                }

                string codigo = nextNum.ToString("D7");

                var prms = new DynamicParameters();
                prms.Add("PeriodoEmpresaSedeId", contextoId);
                prms.Add("Cotizacion", codigo);
                prms.Add("Fecha", model.Fecha);
                prms.Add("FechaEntrega", model.FechaEntrega);
                prms.Add("ProveedorId", model.ClienteId);
                prms.Add("VendedorId", model.VendedorId);
                prms.Add("MonedaId", model.MonedaId);
                prms.Add("MontoTipoCambio", 1.0m);
                prms.Add("ListaPrecioId", model.ListaPrecioId);
                prms.Add("TipoCondicionPagoId", model.TipoCondicionPagoId);
                prms.Add("Observacion", model.Observacion ?? string.Empty);
                prms.Add("MontoBruto", 0m);
                prms.Add("PorcentajeDescuento", 0m);
                prms.Add("MontoDescuento", 0m);
                prms.Add("MontoNeto", 0m);
                prms.Add("MontoIgv", 0m);
                prms.Add("MontoIsc", 0m);
                prms.Add("MontoTotal", 0m);
                prms.Add("EstadoCotizacionId", 2); // 2 = PEDIDO
                prms.Add("EsPedido", "S");
                prms.Add("Estado", "A");
                prms.Add("UsuarioAccionId", userId);

                int newId = await conn.ExecuteScalarAsync<int>("sp_cotiza_insertar", prms, tx, commandType: CommandType.StoredProcedure);
                tx.Commit();
                return (true, $"Pedido {codigo} creado con éxito.", newId);
            }
            else
            {
                var existing = await conn.QuerySingleOrDefaultAsync<PedidoItemModel>(
                    PedidoSelect + " WHERE c.cotiza_id = @Id AND c.periodo_emp_sede_id = @ContextoId AND c.es_pedido = 'S'",
                    new { Id = model.Id, ContextoId = contextoId }, tx);

                if (existing is null)
                {
                    tx.Rollback();
                    return (false, "No se encontró el pedido o fue eliminado.", 0);
                }

                if (!string.IsNullOrEmpty(model.Revision) && existing.Revision != model.Revision)
                {
                    tx.Rollback();
                    return (false, "El pedido fue modificado por otro usuario mientras lo editaba. Vuelva a cargarlo.", 0);
                }

                if (existing.EstadoCotizacionCodigo != "02")
                {
                    tx.Rollback();
                    return (false, "Solo los pedidos en estado 'Pedido' pueden ser modificados.", 0);
                }

                var prms = new DynamicParameters();
                prms.Add("CotizacionId", model.Id);
                prms.Add("PeriodoEmpresaSedeId", contextoId);
                prms.Add("Cotizacion", existing.Codigo);
                prms.Add("Fecha", model.Fecha);
                prms.Add("FechaEntrega", model.FechaEntrega);
                prms.Add("ProveedorId", model.ClienteId);
                prms.Add("VendedorId", model.VendedorId);
                prms.Add("MonedaId", model.MonedaId);
                prms.Add("MontoTipoCambio", existing.MontoTipoCambio);
                prms.Add("ListaPrecioId", model.ListaPrecioId);
                prms.Add("TipoCondicionPagoId", model.TipoCondicionPagoId);
                prms.Add("Observacion", model.Observacion ?? string.Empty);
                prms.Add("MontoBruto", existing.MontoBruto);
                prms.Add("PorcentajeDescuento", existing.PorcentajeDescuento);
                prms.Add("MontoDescuento", existing.MontoDescuento);
                prms.Add("MontoNeto", existing.MontoNeto);
                prms.Add("MontoIgv", existing.MontoIgv);
                prms.Add("MontoIsc", 0m);
                prms.Add("MontoTotal", existing.MontoTotal);
                prms.Add("EstadoCotizacionId", existing.EstadoCotizacionId);
                prms.Add("EsPedido", "S");
                prms.Add("Estado", "A");
                prms.Add("UsuarioAccionId", userId);

                await conn.ExecuteAsync("sp_cotiza_actualizar", prms, tx, commandType: CommandType.StoredProcedure);
                tx.Commit();
                return (true, "Cabecera de pedido actualizada con éxito.", model.Id);
            }
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, $"Error al guardar el pedido: {ex.Message}", 0);
        }
    }

    public async Task<(bool success, string message, int id)> AnularPedidoAsync(
        int empresaId,
        int contextoId,
        int userId,
        int id,
        string revision)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0) return (false, "Parámetros inválidos.", 0);
        using var conn = factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            var existing = await conn.QuerySingleOrDefaultAsync<PedidoItemModel>(
                PedidoSelect + " WHERE c.cotiza_id = @Id AND c.periodo_emp_sede_id = @ContextoId AND c.es_pedido = 'S'",
                new { Id = id, ContextoId = contextoId }, tx);

            if (existing is null)
            {
                tx.Rollback();
                return (false, "No se encontró el pedido a anular.", 0);
            }

            if (!string.IsNullOrEmpty(revision) && existing.Revision != revision)
            {
                tx.Rollback();
                return (false, "El pedido fue modificado por otro usuario. Actualice el listado.", 0);
            }

            if (existing.EnviadoProduccion == "S")
            {
                tx.Rollback();
                return (false, "No se puede anular un pedido que ya ha sido enviado a producción.", 0);
            }

            // Cambiar estado a 4 = ANULADO
            await conn.ExecuteAsync(
                @"UPDATE cotiza
                  SET estado_cotiza_id = 4, usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                  WHERE cotiza_id = @Id",
                new { Id = id, UserId = userId }, tx);

            tx.Commit();
            return (true, $"Pedido {existing.Codigo} anulado correctamente.", id);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, $"Error al anular el pedido: {ex.Message}", 0);
        }
    }

    public async Task<(bool success, string message, int id)> CambiarACotizacionAsync(
        int empresaId,
        int contextoId,
        int userId,
        int id,
        string revision)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0) return (false, "Parámetros inválidos.", 0);
        using var conn = factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            var existing = await conn.QuerySingleOrDefaultAsync<PedidoItemModel>(
                PedidoSelect + " WHERE c.cotiza_id = @Id AND c.periodo_emp_sede_id = @ContextoId AND c.es_pedido = 'S'",
                new { Id = id, ContextoId = contextoId }, tx);

            if (existing is null)
            {
                tx.Rollback();
                return (false, "No se encontró el pedido.", 0);
            }

            if (!string.IsNullOrEmpty(revision) && existing.Revision != revision)
            {
                tx.Rollback();
                return (false, "El pedido fue modificado. Actualice el listado.", 0);
            }

            if (existing.EstadoCotizacionCodigo != "02")
            {
                tx.Rollback();
                return (false, "Solo los pedidos en estado 'Pedido' pueden revertirse a cotización.", 0);
            }

            if (existing.EnviadoProduccion == "S")
            {
                tx.Rollback();
                return (false, "El pedido ya fue enviado a producción y no puede regresar a cotización.", 0);
            }

            if (existing.MontoAnticipo > 0)
            {
                tx.Rollback();
                return (false, "El pedido cuenta con anticipos registrados y no puede revertirse a cotización.", 0);
            }

            // Actualizar a es_pedido = 'N' y estado_cotiza_id = 1
            await conn.ExecuteAsync(
                @"UPDATE cotiza
                  SET es_pedido = 'N', estado_cotiza_id = 1, fecha_ent = NULL,
                      usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                  WHERE cotiza_id = @Id",
                new { Id = id, UserId = userId }, tx);

            tx.Commit();
            return (true, $"El pedido {existing.Codigo} fue convertido exitosamente en Cotización.", id);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, $"Error al revertir pedido: {ex.Message}", 0);
        }
    }

    public async Task<(bool success, string message, int id)> CambiarEstadoPedidoAsync(
        int empresaId,
        int contextoId,
        int userId,
        CambiarEstadoPedidoSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.PedidoId <= 0) return (false, "Parámetros inválidos.", 0);
        using var conn = factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            var existing = await conn.QuerySingleOrDefaultAsync<PedidoItemModel>(
                PedidoSelect + " WHERE c.cotiza_id = @Id AND c.periodo_emp_sede_id = @ContextoId AND c.es_pedido = 'S'",
                new { Id = model.PedidoId, ContextoId = contextoId }, tx);

            if (existing is null)
            {
                tx.Rollback();
                return (false, "No se encontró el pedido.", 0);
            }

            if (!string.IsNullOrEmpty(model.Revision) && existing.Revision != model.Revision)
            {
                tx.Rollback();
                return (false, "El registro fue modificado por otro usuario. Actualice la vista.", 0);
            }

            await conn.ExecuteAsync(
                @"UPDATE cotiza
                  SET estado_cotiza_id = @NuevoEstadoId, usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                  WHERE cotiza_id = @Id",
                new { Id = model.PedidoId, NuevoEstadoId = model.NuevoEstadoId, UserId = userId }, tx);

            tx.Commit();
            return (true, $"Estado del pedido {existing.Codigo} actualizado con éxito.", model.PedidoId);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, $"Error al cambiar estado del pedido: {ex.Message}", 0);
        }
    }

    public async Task<(string Titulo, byte[] Contenido)> ExportarPedidoExcelAsync(int empresaId, int pedidoId)
    {
        using var conn = factory.CreateConnection();
        var p = await conn.QuerySingleOrDefaultAsync<PedidoItemModel>(
            PedidoSelect + @"JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id = c.periodo_emp_sede_id
                JOIN empresa_sede es ON es.empresa_sede_id = pes.empresa_sede_id
                WHERE c.cotiza_id = @Id AND es.empresa_id = @EmpresaId AND c.es_pedido = 'S'",
            new { Id = pedidoId, EmpresaId = empresaId });

        if (p is null) throw new KeyNotFoundException("Pedido no encontrado");

        var items = await GetCotizacionProductosAsync(empresaId, pedidoId);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Pedido");

        ws.Cell("A1").Value = "PEDIDO DE VENTA";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 14;

        ws.Cell("A2").Value = $"N° Pedido: {p.Codigo}";
        ws.Cell("B2").Value = $"Fecha: {p.FechaTexto}";
        ws.Cell("C2").Value = $"F. Entrega: {p.FechaEntregaTexto ?? "-"}";

        ws.Cell("A3").Value = $"Cliente: {p.ClienteNombre} ({p.ClienteDocumento})";
        ws.Cell("C3").Value = $"Vendedor: {p.VendedorNombre}";

        ws.Cell("A4").Value = $"Moneda: {p.MonedaNombre} ({p.MonedaSimbolo})";
        ws.Cell("B4").Value = $"Condición: {p.CondicionPagoNombre}";
        ws.Cell("C4").Value = $"Lista: {p.ListaPrecioNombre}";

        int row = 6;
        ws.Cell(row, 1).Value = "Código";
        ws.Cell(row, 2).Value = "Descripción";
        ws.Cell(row, 3).Value = "U.M.";
        ws.Cell(row, 4).Value = "Cantidad";
        ws.Cell(row, 5).Value = "P. Unitario";
        ws.Cell(row, 6).Value = "Dcto %";
        ws.Cell(row, 7).Value = "Total";
        ws.Range(row, 1, row, 7).Style.Font.Bold = true;
        ws.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#EFEFEF");

        foreach (var item in items)
        {
            row++;
            ws.Cell(row, 1).Value = item.Codigo;
            ws.Cell(row, 2).Value = item.Descripcion;
            ws.Cell(row, 3).Value = item.UnidadMedida;
            ws.Cell(row, 4).Value = item.Cantidad;
            ws.Cell(row, 5).Value = item.PrecioUnitario;
            ws.Cell(row, 6).Value = item.PorcentajeDescuento;
            ws.Cell(row, 7).Value = item.MontoNeto;

            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.0000";
            ws.Cell(row, 6).Style.NumberFormat.Format = "0.00%";
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
        }

        row += 2;
        ws.Cell(row, 6).Value = "Subtotal Bruto:";
        ws.Cell(row, 7).Value = p.MontoBruto;
        ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";

        row++;
        ws.Cell(row, 6).Value = "Descuento:";
        ws.Cell(row, 7).Value = p.MontoDescuento;
        ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";

        row++;
        ws.Cell(row, 6).Value = "IGV:";
        ws.Cell(row, 7).Value = p.MontoIgv;
        ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";

        row++;
        ws.Cell(row, 6).Value = "Total:";
        ws.Cell(row, 6).Style.Font.Bold = true;
        ws.Cell(row, 7).Value = p.MontoTotal;
        ws.Cell(row, 7).Style.Font.Bold = true;
        ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";

        row++;
        ws.Cell(row, 6).Value = "Anticipo:";
        ws.Cell(row, 7).Value = p.MontoAnticipo;
        ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";

        row++;
        ws.Cell(row, 6).Value = "Saldo a Cobrar:";
        ws.Cell(row, 6).Style.Font.Bold = true;
        ws.Cell(row, 7).Value = p.MontoSaldo;
        ws.Cell(row, 7).Style.Font.Bold = true;
        ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ($"Pedido_{p.Codigo}", ms.ToArray());
    }
}
