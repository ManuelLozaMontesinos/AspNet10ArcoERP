using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class VentasService
{
    private const string CotizacionSelect = @"SELECT c.cotiza_id Id, c.periodo_emp_sede_id PeriodoEmpresaSedeId, RTRIM(c.cotiza) Codigo,
        c.fecha Fecha, c.proveedor_id ClienteId, RTRIM(p.razon_social) ClienteNombre,
        COALESCE(NULLIF(p.numero_ruc,''), p.num_doc_iden, '') ClienteDocumento,
        c.vendedor_id VendedorId, RTRIM(p2.razon_social) VendedorNombre,
        c.moneda_id MonedaId, RTRIM(m.simbolo) MonedaSimbolo, RTRIM(m.nombre) MonedaNombre, c.monto_tc MontoTipoCambio,
        c.lista_precio_id ListaPrecioId, RTRIM(lp.nombre) ListaPrecioNombre,
        c.tipo_c_pago_id TipoCondicionPagoId, RTRIM(tcp.nombre) CondicionPagoNombre,
        c.observa Observacion, c.monto_bruto MontoBruto, c.porc_dcto PorcentajeDescuento,
        c.monto_dcto MontoDescuento, c.monto_neto MontoNeto, c.monto_igv MontoIgv, c.monto_total MontoTotal,
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

    public async Task<CotizacionPageModel> GetCotizacionesAsync(int empresaId, int contextoId, string? search = null, string? codigo = null, DateTime? fechaDesde = null, DateTime? fechaHasta = null, int? vendedorId = null, int? estadoCotizacionId = null, int page = 1, int top = 25, string? sort = null, string? dir = null)
    {
        var model = new CotizacionPageModel
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
            ["estado"] = "ec.nombre",
            ["id"] = "c.cotiza_id"
        };
        model.Ordenar(sort, dir, columns.Keys, "fecha", "desc");
        model.Paginar(0, page, top);
        if (empresaId <= 0 || contextoId <= 0) return model;

        using var conn = factory.CreateConnection();

        // Lookups for filters
        model.Vendedores = (await conn.QueryAsync<LookupItemModel>(@"SELECT v.vendedor_id Id, RTRIM(p.razon_social) Nombre
            FROM vendedor v JOIN proveedor p ON p.proveedor_id = v.proveedor_id
            WHERE p.empresa_id = @EmpresaId AND v.estado = 'A' ORDER BY p.razon_social", new { EmpresaId = empresaId })).AsList();

        model.EstadosCotizacion = (await conn.QueryAsync<LookupItemModel>("SELECT estado_cotiza_id Id, RTRIM(nombre) Nombre, RTRIM(estado_cotiza) Codigo FROM estado_cotiza ORDER BY estado_cotiza_id")).AsList();
        model.Monedas = (await conn.QueryAsync<LookupItemModel>("SELECT moneda_id Id, RTRIM(nombre) Nombre, RTRIM(simbolo) Codigo FROM moneda WHERE estado = 'A' ORDER BY moneda_id")).AsList();
        model.CondicionesPago = (await conn.QueryAsync<LookupItemModel>("SELECT tipo_c_pago_id Id, RTRIM(nombre) Nombre FROM tipo_c_pago ORDER BY tipo_c_pago_id")).AsList();
        model.ListasPrecio = (await conn.QueryAsync<LookupItemModel>(@"SELECT lp.lista_precio_id Id, RTRIM(lp.nombre) Nombre
            FROM lista_precio lp JOIN empresa_sede es ON es.empresa_sede_id = lp.empresa_sede_id
            WHERE es.empresa_id = @EmpresaId AND lp.estado = 'A' ORDER BY lp.nombre", new { EmpresaId = empresaId })).AsList();

        const string where = @"WHERE c.periodo_emp_sede_id = @ContextoId AND c.es_pedido = 'N' AND c.estado = 'A'
            AND (@Codigo IS NULL OR c.cotiza LIKE @Codigo)
            AND (@Search IS NULL OR p.razon_social LIKE @Search OR p.proveedor LIKE @Search OR p.numero_ruc LIKE @Search OR p.num_doc_iden LIKE @Search)
            AND (@VendedorId IS NULL OR c.vendedor_id = @VendedorId)
            AND (@EstadoCotizacionId IS NULL OR c.estado_cotiza_id = @EstadoCotizacionId)
            AND (@FechaDesde IS NULL OR c.fecha >= @FechaDesde)
            AND (@FechaHasta IS NULL OR c.fecha < DATEADD(day, 1, @FechaHasta))";

        var args = new DynamicParameters(new
        {
            EmpresaId = empresaId,
            ContextoId = contextoId,
            Codigo = Search(codigo),
            Search = Search(search),
            VendedorId = vendedorId > 0 ? vendedorId : null,
            EstadoCotizacionId = estadoCotizacionId > 0 ? estadoCotizacionId : null,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        });

        int total = await conn.ExecuteScalarAsync<int>(@"SELECT COUNT(*) FROM cotiza c
            JOIN proveedor p ON p.proveedor_id = c.proveedor_id " + where, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        string query = CotizacionSelect + where + $" ORDER BY {columns[model.Sort]} {model.Dir}, c.cotiza_id DESC OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";
        model.Items = (await conn.QueryAsync<CotizacionItemModel>(query, args)).AsList();
        model.VisibleCount = model.Items.Count;
        return model;
    }

    public async Task<CotizacionDetalleModel?> GetCotizacionDetalleAsync(int empresaId, int contextoId, int id)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0) return null;
        using var conn = factory.CreateConnection();

        var cabecera = await conn.QuerySingleOrDefaultAsync<CotizacionItemModel>(CotizacionSelect + @"JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id = c.periodo_emp_sede_id
            JOIN empresa_sede es ON es.empresa_sede_id = pes.empresa_sede_id
            WHERE c.cotiza_id = @Id AND es.empresa_id = @EmpresaId", new { Id = id, EmpresaId = empresaId });

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

        return new CotizacionDetalleModel
        {
            Cabecera = cabecera,
            Productos = productos,
            Vendedores = vendedores,
            Monedas = monedas,
            ListasPrecio = listas,
            CondicionesPago = condiciones
        };
    }

    public async Task<List<LookupItemModel>> BuscarClientesCotizacionAsync(int empresaId, string? search)
    {
        if (empresaId <= 0) return new();
        using var conn = factory.CreateConnection();
        const string sql = @"SELECT TOP 25 p.proveedor_id Id,
            RTRIM(p.razon_social) Nombre,
            COALESCE(NULLIF(p.numero_ruc,''), p.num_doc_iden, p.proveedor, '') Codigo
            FROM proveedor p
            WHERE p.empresa_id = @EmpresaId AND p.estado = 'A'
              AND (@Search IS NULL OR p.razon_social LIKE @Search OR p.proveedor LIKE @Search OR p.numero_ruc LIKE @Search OR p.num_doc_iden LIKE @Search)
            ORDER BY p.razon_social ASC";
        return (await conn.QueryAsync<LookupItemModel>(sql, new { EmpresaId = empresaId, Search = Search(search) })).AsList();
    }

    public async Task<List<ProductoPrecioLookupModel>> BuscarProductosCotizacionAsync(int listaPrecioId, string? search)
    {
        if (listaPrecioId <= 0) return new();
        using var conn = factory.CreateConnection();
        const string sql = @"SELECT TOP 25 p.producto_id ProductoId, RTRIM(p.producto) Codigo, RTRIM(p.descripcion) Nombre,
            RTRIM(um.abrev) UnidadMedida, lpp.valor_unitario ValorUnitario, lpp.precio_unitario PrecioUnitario,
            lpp.permite_dcto PermiteDescuento, p.tipo_afecta_igv_id TipoAfectacionIgvId
            FROM lista_p_prod lpp
            JOIN producto p ON p.producto_id = lpp.producto_id
            JOIN unidad_medida um ON um.unidad_medida_id = p.unidad_medida_id
            WHERE lpp.lista_precio_id = @ListaPrecioId AND lpp.estado = 'A'
              AND (@Search IS NULL OR p.descripcion LIKE @Search OR p.producto LIKE @Search)
            ORDER BY p.descripcion ASC";
        return (await conn.QueryAsync<ProductoPrecioLookupModel>(sql, new { ListaPrecioId = listaPrecioId, Search = Search(search) })).AsList();
    }

    public async Task<(bool success, string message, int id)> SaveCotizacionAsync(int empresaId, int contextoId, int userId, CotizacionGuardarSolicitud model)
    {
        if (model is null || model.ClienteId <= 0 || model.VendedorId <= 0 || model.ListaPrecioId <= 0 || model.MonedaId <= 0)
            return Failure("Complete todos los campos obligatorios (cliente, vendedor, moneda, lista de precios y condición de pago).");

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return Failure("La empresa no está disponible.");

        // Impuesto tasa de la lista de precios
        int impuestoTasaId = await conn.ExecuteScalarAsync<int>("SELECT impuesto_tasa_id FROM lista_precio WHERE lista_precio_id = @Id", new { Id = model.ListaPrecioId }, tx);
        if (impuestoTasaId <= 0) return Failure("La lista de precios seleccionada no tiene una tasa de impuesto configurada.");

        // Tipo de cambio
        decimal tc = await conn.ExecuteScalarAsync<decimal?>(@"SELECT TOP 1 tcd.venta
            FROM tipo_cambio_dia tcd JOIN tipo_cambio tc ON tc.tipo_cambio_id = tcd.tipo_cambio_id
            JOIN empresa_sede es ON es.empresa_sede_id = tc.empresa_sede_id
            JOIN periodo_emp_sede pes ON pes.empresa_sede_id = es.empresa_sede_id
            WHERE pes.periodo_emp_sede_id = @ContextoId AND tcd.fecha <= @Fecha ORDER BY tcd.fecha DESC",
            new { ContextoId = contextoId, Fecha = model.Fecha }, tx) ?? 1.0000m;

        if (model.Id > 0)
        {
            var old = await conn.QuerySingleOrDefaultAsync<CotizacionItemModel>(CotizacionSelect + "WITH (UPDLOCK,HOLDLOCK) WHERE c.cotiza_id = @Id AND c.periodo_emp_sede_id = @ContextoId", new { Id = model.Id, ContextoId = contextoId }, tx);
            if (old is null || old.Estado != "A" || old.EstadoCotizacionId != 1 || (!string.IsNullOrEmpty(model.Revision) && old.Revision != model.Revision))
                return Failure("La cotización cambió o ya no se encuentra en estado editable. Actualice el listado.");

            await conn.ExecuteAsync("sp_cotiza_actualizar", new
            {
                CotizacionId = model.Id,
                Fecha = model.Fecha,
                ProveedorId = model.ClienteId,
                VendedorId = model.VendedorId,
                TipoCondicionPagoId = model.TipoCondicionPagoId,
                Observacion = model.Observacion?.Trim() ?? "",
                ImpuestoTasaId = impuestoTasaId,
                MontoBruto = old.MontoBruto,
                MontoNeto = old.MontoNeto,
                MontoIgv = old.MontoIgv,
                MontoIsc = 0m,
                MontoTotal = old.MontoTotal,
                AlmacenId = (int?)null,
                MesContableId = (int?)null,
                EsPedido = "N",
                UsuarioAccionId = userId
            }, tx, commandType: CommandType.StoredProcedure);

            tx.Commit();
            return (true, "Cabecera de cotización actualizada.", model.Id);
        }
        else
        {
            // Correlativo 0015 (COTIZACION) por periodo_emp_sede
            var counter = await conn.QuerySingleOrDefaultAsync<int?>(@"SELECT correlativo FROM periodo_emp_corre WITH (UPDLOCK,HOLDLOCK)
                WHERE periodo_emp_sede_id = @ContextoId AND tabla_id = 15", new { ContextoId = contextoId }, tx);

            int nextVal = (counter ?? 0) + 1;
            string code = nextVal.ToString("0000000");

            if (counter is null)
            {
                await InsertAsync(conn, tx, "sp_periodo_emp_corre_insertar", new
                {
                    PeriodoEmpresaSedeId = contextoId,
                    TablaId = 15,
                    Correlativo = nextVal,
                    UsuarioAccionId = userId
                });
            }
            else
            {
                int correId = await conn.ExecuteScalarAsync<int>("SELECT periodo_emp_corre_id FROM periodo_emp_corre WHERE periodo_emp_sede_id = @ContextoId AND tabla_id = 15", new { ContextoId = contextoId }, tx);
                await conn.ExecuteAsync("sp_periodo_emp_corre_actualizar", new
                {
                    PeriodoEmpresaCorrelativoId = correId,
                    Correlativo = nextVal,
                    UsuarioAccionId = userId
                }, tx, commandType: CommandType.StoredProcedure);
            }

            int newId = await InsertAsync(conn, tx, "sp_cotiza_insertar", new
            {
                PeriodoEmpresaSedeId = contextoId,
                Cotizacion = code,
                Fecha = model.Fecha,
                ProveedorId = model.ClienteId,
                VendedorId = model.VendedorId,
                MonedaId = model.MonedaId,
                MontoTipoCambio = tc,
                ListaPrecioId = model.ListaPrecioId,
                TipoCondicionPagoId = model.TipoCondicionPagoId,
                Observacion = model.Observacion?.Trim() ?? "",
                ImpuestoTasaId = impuestoTasaId,
                MontoBruto = 0m,
                PorcentajeDescuento = 0m,
                MontoDescuento = 0m,
                MontoDescuentoItem = 0m,
                MontoNeto = 0m,
                MontoIgv = 0m,
                MontoIsc = 0m,
                MontoAnticipo = 0m,
                MontoTotal = 0m,
                AlmacenId = (int?)null,
                MesContableId = (int?)null,
                EsPedido = "N",
                EnviadoProduccion = "N",
                EstadoCotizacionId = 1, // 01 COTIZACION
                UsuarioAccionId = userId
            });

            tx.Commit();
            return (true, $"Cotización N° {code} registrada exitosamente.", newId);
        }
    }

    public async Task<(bool success, string message, int id)> AnularCotizacionAsync(int empresaId, int contextoId, int userId, int id, string revision)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0) return Failure("Identificador no válido.");
        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return Failure("La empresa no está habilitada.");

        var old = await conn.QuerySingleOrDefaultAsync<CotizacionItemModel>(CotizacionSelect + "WITH (UPDLOCK,HOLDLOCK) WHERE c.cotiza_id = @Id AND c.periodo_emp_sede_id = @ContextoId", new { Id = id, ContextoId = contextoId }, tx);
        if (old is null || old.Estado != "A" || (!string.IsNullOrEmpty(revision) && old.Revision != revision))
            return Failure("La cotización no se encuentra disponible o fue modificada.");

        if (old.EstadoCotizacionId is 2 or 3 or 4)
            return Failure("No se puede anular una cotización que ya fue convertida a Pedido o Facturada.");

        await conn.ExecuteAsync("sp_cotiza_eliminar", new
        {
            CotizacionId = id,
            EstadoCotizacionId = 5, // 99 ANULADO
            UsuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, "Cotización anulada correctamente.", id);
    }

    public async Task<List<CotizacionProductoItemModel>> GetCotizacionProductosAsync(int empresaId, int cotizacionId)
    {
        if (empresaId <= 0 || cotizacionId <= 0) return new();
        using var conn = factory.CreateConnection();
        const string sql = @"SELECT cp.cotiza_prod_id Id, cp.cotiza_id CotizacionId, cp.producto_id ProductoId,
            RTRIM(p.producto) Codigo, RTRIM(cp.descripcion) Descripcion, RTRIM(fp.nombre) Familia,
            RTRIM(um.abrev) UnidadMedida, cp.cantidad Cantidad, cp.valor_unitario ValorUnitario,
            cp.precio_unitario PrecioUnitario, cp.monto_bruto MontoBruto, cp.porc_dcto PorcentajeDescuento,
            cp.monto_dcto1 MontoDescuento1, cp.monto_neto MontoNeto, cp.monto_igv MontoIgv, cp.monto_total MontoTotal,
            cp.permite_dcto PermiteDescuento, cp.sin_saldo SinSaldo, cp.soli_auto_dcto SolicitaAutorizacion,
            cp.estado Estado, COALESCE(cp.fecha_modifica, cp.fecha_creacion) FechaRevision
            FROM cotiza_prod cp
            JOIN producto p ON p.producto_id = cp.producto_id
            JOIN familia_producto fp ON fp.familia_producto_id = p.familia_producto_id
            JOIN unidad_medida um ON um.unidad_medida_id = p.unidad_medida_id
            WHERE cp.cotiza_id = @CotizacionId AND cp.estado = 'A'
            ORDER BY cp.cotiza_prod_id ASC";
        return (await conn.QueryAsync<CotizacionProductoItemModel>(sql, new { CotizacionId = cotizacionId })).AsList();
    }

    public async Task<(bool success, string message, int id)> SaveCotizacionProductoAsync(int empresaId, int contextoId, int userId, CotizacionProductoGuardarSolicitud model)
    {
        if (model is null || model.CotizacionId <= 0 || model.ProductoId <= 0 || model.Cantidad <= 0 || model.PrecioUnitario < 0)
            return Failure("Revise los datos del producto (cantidad y precio mayores a cero).");

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return Failure("La empresa no está disponible.");

        var cot = await conn.QuerySingleOrDefaultAsync<CotizacionItemModel>(CotizacionSelect + "WITH (UPDLOCK,HOLDLOCK) WHERE c.cotiza_id = @Id AND c.periodo_emp_sede_id = @ContextoId", new { Id = model.CotizacionId, ContextoId = contextoId }, tx);
        if (cot is null || cot.Estado != "A" || cot.EstadoCotizacionId != 1)
            return Failure("La cotización no se encuentra activa o ya no admite modificaciones de ítems.");

        // Tax rate from cotizacion's impuesto_tasa
        decimal tasa = await conn.ExecuteScalarAsync<decimal>("SELECT tasa FROM impuesto_tasa WHERE impuesto_tasa_id = (SELECT impuesto_tasa_id FROM cotiza WHERE cotiza_id = @Id)", new { Id = model.CotizacionId }, tx);

        // Product details
        var prodInfo = await conn.QuerySingleOrDefaultAsync<(int UnidadMedidaId, int TipoAfectacionIgvId, string Descripcion)>(
            "SELECT unidad_medida_id, tipo_afecta_igv_id, descripcion FROM producto WHERE producto_id = @Id", new { Id = model.ProductoId }, tx);

        string descripcion = string.IsNullOrWhiteSpace(model.Descripcion) ? prodInfo.Descripcion : model.Descripcion.Trim();

        // Calculations
        decimal valorUnitario = model.ValorUnitario > 0 ? model.ValorUnitario : decimal.Round(model.PrecioUnitario / (1 + tasa / 100), 4);
        decimal precioUnitario = model.PrecioUnitario > 0 ? model.PrecioUnitario : decimal.Round(valorUnitario * (1 + tasa / 100), 4);

        decimal montoBruto = decimal.Round(model.Cantidad * valorUnitario, 4);
        decimal montoDcto = decimal.Round(montoBruto * (model.PorcentajeDescuento / 100), 4);
        decimal montoNeto = montoBruto - montoDcto;
        decimal montoIgv = decimal.Round(montoNeto * (tasa / 100), 4);
        decimal montoTotal = montoNeto + montoIgv;

        if (model.Id > 0)
        {
            await conn.ExecuteAsync("sp_cotiza_prod_actualizar", new
            {
                CotizacionProductoId = model.Id,
                Descripcion = descripcion,
                Cantidad = model.Cantidad,
                UnidadMedidaId = prodInfo.UnidadMedidaId,
                CantidadPresentacion = model.Cantidad,
                CantidadEquivalente = model.Cantidad,
                CantidadComprobante = 0m,
                ValorUnitario = valorUnitario,
                PrecioUnitario = precioUnitario,
                SinSaldo = "N",
                MontoBruto = montoBruto,
                TipoDescuentoId = 1,
                DescuentoValor = montoDcto,
                PorcentajeDescuento = model.PorcentajeDescuento,
                MontoDescuento1 = montoDcto,
                MontoDescuento2 = 0m,
                MontoNeto = montoNeto,
                MontoIgv = montoIgv,
                MontoIsc = 0m,
                MontoTotal = montoTotal,
                PesoNeto = 0m,
                TipoAfectacionIgvId = prodInfo.TipoAfectacionIgvId,
                TipoIscId = (int?)null,
                UsuarioAccionId = userId
            }, tx, commandType: CommandType.StoredProcedure);
        }
        else
        {
            await InsertAsync(conn, tx, "sp_cotiza_prod_insertar", new
            {
                CotizacionId = model.CotizacionId,
                ProductoId = model.ProductoId,
                Descripcion = descripcion,
                Cantidad = model.Cantidad,
                UnidadMedidaId = prodInfo.UnidadMedidaId,
                CantidadPresentacion = model.Cantidad,
                CantidadEquivalente = model.Cantidad,
                CantidadComprobante = 0m,
                ValorUnitarioLista = valorUnitario,
                PrecioUnitarioLista = precioUnitario,
                ValorUnitario = valorUnitario,
                PrecioUnitario = precioUnitario,
                CantidadBloqueada = "N",
                IncluyeDescuentoListaPrecio = "N",
                PermiteDescuento = model.PermiteDescuento,
                SinSaldo = "N",
                SolicitaAutorizacionDescuento = "N",
                PrecioAutorizado = precioUnitario,
                MontoBruto = montoBruto,
                TipoDescuentoId = 1,
                DescuentoValor = montoDcto,
                PorcentajeDescuento = model.PorcentajeDescuento,
                MontoDescuento1 = montoDcto,
                MontoDescuento2 = 0m,
                MontoNeto = montoNeto,
                MontoIgv = montoIgv,
                MontoIsc = 0m,
                MontoTotal = montoTotal,
                PesoNeto = 0m,
                TipoAfectacionIgvId = prodInfo.TipoAfectacionIgvId,
                TipoIscId = (int?)null,
                RegistroComboId = (int?)null,
                UsuarioAccionId = userId
            });
        }

        // Recalculate Quote Totals
        await conn.ExecuteAsync("sp_cotiza_actualizarMonto", new
        {
            CotizacionId = model.CotizacionId,
            UsuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, "Ítem guardado y totales actualizados.", model.CotizacionId);
    }

    public async Task<(bool success, string message, int id)> EliminarCotizacionProductoAsync(int empresaId, int contextoId, int userId, int id)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0) return Failure("Identificador no válido.");
        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return Failure("La empresa no está disponible.");

        int cotizacionId = await conn.ExecuteScalarAsync<int>(@"SELECT cp.cotiza_id FROM cotiza_prod cp
            JOIN cotiza c ON c.cotiza_id = cp.cotiza_id
            WHERE cp.cotiza_prod_id = @Id AND c.periodo_emp_sede_id = @ContextoId AND cp.estado = 'A'",
            new { Id = id, ContextoId = contextoId }, tx);

        if (cotizacionId <= 0) return Failure("El ítem no existe o la cotización no pertenece al contexto actual.");

        await conn.ExecuteAsync("sp_cotiza_prod_eliminar", new
        {
            CotizacionProductoId = id,
            UsuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        // Recalculate totals
        await conn.ExecuteAsync("sp_cotiza_actualizarMonto", new
        {
            CotizacionId = cotizacionId,
            UsuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, "Ítem retirado de la cotización.", cotizacionId);
    }

    public async Task<(bool success, string message, int id)> GenerarPedidoDesdeCotizacionAsync(int empresaId, int contextoId, int userId, GenerarPedidoSolicitud model)
    {
        if (model is null || model.CotizacionId <= 0) return Failure("Cotización no válida.");
        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return Failure("La empresa no está disponible.");

        var cot = await conn.QuerySingleOrDefaultAsync<CotizacionItemModel>(CotizacionSelect + "WITH (UPDLOCK,HOLDLOCK) WHERE c.cotiza_id = @Id AND c.periodo_emp_sede_id = @ContextoId", new { Id = model.CotizacionId, ContextoId = contextoId }, tx);
        if (cot is null || cot.Estado != "A" || cot.EstadoCotizacionId != 1)
            return Failure("Solo se pueden convertir a pedido cotizaciones activas en estado COTIZACION.");

        if (cot.CantidadItems <= 0)
            return Failure("La cotización no tiene productos agregados. Incorpore ítems antes de convertir a pedido.");

        // Update to PEDIDO
        await conn.ExecuteAsync(@"UPDATE cotiza
            SET es_pedido = 'S', estado_cotiza_id = 2, fecha_ent = @FechaEnt,
                usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
            WHERE cotiza_id = @Id",
            new { Id = model.CotizacionId, FechaEnt = model.FechaEntrega ?? DateTime.Today, UserId = userId }, tx);

        tx.Commit();
        return (true, $"Cotización {cot.Codigo} convertida a Pedido de Venta exitosamente.", model.CotizacionId);
    }

    public async Task<(string Titulo, byte[] Contenido)> ExportarCotizacionExcelAsync(int empresaId, int cotizacionId)
    {
        if (empresaId <= 0 || cotizacionId <= 0) throw new ArgumentException("Parámetros no válidos.");
        using var conn = factory.CreateConnection();

        var cot = await conn.QuerySingleOrDefaultAsync<CotizacionItemModel>(CotizacionSelect + @"JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id = c.periodo_emp_sede_id
            JOIN empresa_sede es ON es.empresa_sede_id = pes.empresa_sede_id
            WHERE c.cotiza_id = @Id AND es.empresa_id = @EmpresaId", new { Id = cotizacionId, EmpresaId = empresaId })
            ?? throw new KeyNotFoundException("No se encontró la cotización solicitada.");

        var productos = await GetCotizacionProductosAsync(empresaId, cotizacionId);

        var columns = new List<(string Label, Func<CotizacionProductoItemModel, object?> Value)>
        {
            ("Código", r => r.Codigo),
            ("Descripción", r => r.Descripcion),
            ("Unidad", r => r.UnidadMedida),
            ("Cantidad", r => r.Cantidad),
            ("Valor Unitario", r => r.ValorUnitario),
            ("Precio Unitario", r => r.PrecioUnitario),
            ("Dcto %", r => r.PorcentajeDescuento),
            ("Monto Neto", r => r.MontoNeto),
            ("IGV", r => r.MontoIgv),
            ("Total", r => r.MontoTotal)
        };

        string resumen = $"Cotización N°: {cot.Codigo} | Cliente: {cot.ClienteNombre} ({cot.ClienteDocumento}) | Fecha: {cot.FechaTexto} | Moneda: {cot.MonedaNombre} ({cot.MonedaSimbolo}) | Total: {cot.MonedaSimbolo} {cot.MontoTotal:N2}";
        byte[] excelBytes = InformeExcel.Crear("COTIZACIÓN DE VENTA - " + cot.Codigo, "ARCO ERP", resumen, productos, columns);
        return ("Cotizacion_" + cot.Codigo, excelBytes);
    }
}

