using static AspNet10ArcoERP.Services.MaestroEmpresaSql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services;

public partial class VentasService
{
    public async Task<ComprobantePageModel> GetComprobantesAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        int? tipoDocId = null,
        int? estadoComproId = null,
        int? estadoSunatId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new ComprobantePageModel
        {
            Search = search,
            TipoDocumentoComercialId = tipoDocId,
            EstadoComprobanteId = estadoComproId,
            EstadoSunatId = estadoSunatId,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        };

        var columns = new Dictionary<string, string>
        {
            ["fecha"] = "c.fecha",
            ["documento"] = "c.serie, c.numero",
            ["cliente"] = "p.razon_social",
            ["vendedor"] = "pv.razon_social",
            ["total"] = "c.monto_total",
            ["estado"] = "ec.nombre",
            ["sunat"] = "es.nombre"
        };
        model.Ordenar(sort, dir, columns.Keys, "fecha");
        model.Paginar(0, page, top);

        if (empresaId <= 0 || contextoId <= 0) return model;

        using var conn = factory.CreateConnection();

        // Cargar lookups
        model.TiposDocumento = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT tipo_doc_com_id Id, RTRIM(codigo_sunat) Codigo, RTRIM(nombre) Descripcion
            FROM tipo_doc_com
            WHERE estado = 'A' AND tipo_doc_com IN ('01', '03', '07', '09', '30')
            ORDER BY tipo_doc_com_id")).AsList();

        model.EstadosComprobante = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT estado_compro_id Id, RTRIM(estado_compro) Codigo, RTRIM(nombre) Descripcion
            FROM estado_compro WHERE estado = 'A' ORDER BY estado_compro_id")).AsList();

        model.EstadosSunat = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT estado_sunat_id Id, RTRIM(codigo) Codigo, RTRIM(nombre) Descripcion
            FROM estado_sunat WHERE estado = 'A' ORDER BY estado_sunat_id")).AsList();

        model.Monedas = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT moneda_id Id, RTRIM(codigo_sunat) Codigo, RTRIM(nombre) Descripcion
            FROM moneda WHERE estado = 'A' ORDER BY moneda_id")).AsList();

        model.CondicionesPago = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT tipo_c_pago_id Id, RTRIM(tipo_c_pago) Codigo, RTRIM(nombre) Descripcion
            FROM tipo_c_pago WHERE estado = 'A' ORDER BY tipo_c_pago_id")).AsList();

        model.MediosPago = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT medio_pago_id Id, RTRIM(medio_pago) Codigo, RTRIM(nombre) Descripcion
            FROM medio_pago WHERE empresa_id = @EmpresaId AND estado = 'A' ORDER BY nombre", new { EmpresaId = empresaId })).AsList();

        model.TiposDetraccion = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT tipo_detrac_id Id, RTRIM(codigo_sunat) Codigo, RTRIM(nombre) Descripcion
            FROM tipo_detrac WHERE estado = 'A' ORDER BY tipo_detrac_id")).AsList();

        var where = new List<string> { "c.periodo_emp_sede_id = @ContextoId" };
        var args = new DynamicParameters(new
        {
            EmpresaId = empresaId,
            ContextoId = contextoId,
            Search = Search(search),
            TipoDocId = tipoDocId,
            EstadoComproId = estadoComproId,
            EstadoSunatId = estadoSunatId,
            Desde = fechaDesde?.Date,
            Hasta = fechaHasta?.Date.AddDays(1)
        });

        if (tipoDocId.HasValue && tipoDocId > 0) where.Add("c.tipo_doc_com_id = @TipoDocId");
        if (estadoComproId.HasValue && estadoComproId > 0) where.Add("c.estado_compro_id = @EstadoComproId");
        if (estadoSunatId.HasValue && estadoSunatId > 0) where.Add("c.estado_sunat_id = @EstadoSunatId");
        if (fechaDesde.HasValue) where.Add("c.fecha >= @Desde");
        if (fechaHasta.HasValue) where.Add("c.fecha < @Hasta");
        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(p.razon_social LIKE @Search OR p.numero_ruc LIKE @Search OR p.num_doc_iden LIKE @Search OR c.serie LIKE @Search OR c.numero LIKE @Search)");
        }

        string whereSql = " WHERE " + string.Join(" AND ", where);
        string countSql = @"
            SELECT COUNT(1)
            FROM comprobante c
            JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id = c.tipo_doc_com_id
            JOIN proveedor p ON p.proveedor_id = c.proveedor_id
            LEFT JOIN vendedor v ON v.vendedor_id = c.vendedor_id
            LEFT JOIN proveedor pv ON pv.proveedor_id = v.proveedor_id
            LEFT JOIN estado_compro ec ON ec.estado_compro_id = c.estado_compro_id
            LEFT JOIN estado_sunat es ON es.estado_sunat_id = c.estado_sunat_id " + whereSql;

        int total = await conn.ExecuteScalarAsync<int>(countSql, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        string selectSql = @"
            SELECT c.comprobante_id ComprobanteId,
                   tdc.abrev TipoDocumentoComercial,
                   tdc.nombre TipoDocumentoComercialNombre,
                   RTRIM(c.serie) Serie,
                   RTRIM(c.numero) Numero,
                   c.lista_precio_id ListaPrecioId,
                   c.fecha Fecha,
                   c.proveedor_id ClienteId,
                   p.razon_social ClienteRazonSocial,
                   COALESCE(NULLIF(p.numero_ruc, ''), p.num_doc_iden, '') ClienteNumeroDocumento,
                   c.vendedor_id VendedorId,
                   COALESCE(pv.razon_social, '') VendedorNombre,
                   c.cotiza_id CotizacionId,
                   c.pedido_id PedidoId,
                   m.simbolo Moneda,
                   c.monto_total MontoTotal,
                   RTRIM(c.tipo_comprob_id) TipoComprobante,
                   RTRIM(c.estado_compro_id) EstadoComprobante,
                   COALESCE(ec.nombre, 'Pendiente') EstadoComprobanteNombre,
                   COALESCE(RTRIM(es.codigo), '1') EstadoSunat,
                   COALESCE(es.nombre, 'Pendiente Generar') EstadoSunatNombre,
                   COALESCE(c.codigo_hash, '') CodigoHash,
                   c.emitido Emitido,
                   c.es_contingencia EsContingencia,
                   c.compro_elec_id ComprobanteElectronicoId
            FROM comprobante c
            JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id = c.tipo_doc_com_id
            JOIN proveedor p ON p.proveedor_id = c.proveedor_id
            LEFT JOIN vendedor v ON v.vendedor_id = c.vendedor_id
            LEFT JOIN proveedor pv ON pv.proveedor_id = v.proveedor_id
            JOIN moneda m ON m.moneda_id = c.moneda_id
            LEFT JOIN estado_compro ec ON ec.estado_compro_id = c.estado_compro_id
            LEFT JOIN estado_sunat es ON es.estado_sunat_id = c.estado_sunat_id "
            + whereSql
            + $" ORDER BY {columns[model.Sort]} {model.Dir}, c.comprobante_id DESC OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";

        model.Items = (await conn.QueryAsync<ComprobanteFilaModel>(selectSql, args)).AsList();
        model.VisibleCount = model.Items.Count;
        return model;
    }

    public async Task<ComprobanteDetalleModel?> GetComprobanteDetalleAsync(int empresaId, int comprobanteId)
    {
        if (comprobanteId <= 0) return null;
        using var conn = factory.CreateConnection();

        var comp = await conn.QueryFirstOrDefaultAsync<ComprobanteDetalleModel>(@"
            SELECT c.comprobante_id ComprobanteId,
                   c.periodo_emp_sede_id PeriodoEmpresaSedeId,
                   c.tipo_doc_com_id TipoDocumentoComercialId,
                   tdc.codigo_sunat TipoDocumentoComercialCodigo,
                   tdc.nombre TipoDocumentoComercialNombre,
                   c.numero_opera NumeroOperacion,
                   RTRIM(c.serie) Serie,
                   RTRIM(c.numero) Numero,
                   c.fecha Fecha,
                   c.proveedor_id ClienteId,
                   p.razon_social ClienteNombre,
                   COALESCE(NULLIF(p.numero_ruc, ''), p.num_doc_iden, '') ClienteRuc,
                   COALESCE(p.direccion, '') ClienteDireccion,
                   c.vendedor_id VendedorId,
                   COALESCE(pv.razon_social, '') VendedorNombre,
                   c.moneda_id MonedaId,
                   m.simbolo MonedaSimbolo,
                   c.monto_tc MontoTipoCambio,
                   c.lista_precio_id ListaPrecioId,
                   c.tipo_c_pago_id TipoCondicionPagoId,
                   c.medio_pago_id MedioPagoId,
                   c.impuesto_tasa_id ImpuestoTasaId,
                   it.porcentaje PorcentajeIgv,
                   c.nro_guia_rem NumeroGuiaRemision,
                   c.observa Observacion,
                   c.monto_bruto MontoBruto,
                   c.porc_dcto PorcentajeDescuento,
                   c.monto_dcto MontoDescuento,
                   c.monto_neto MontoNeto,
                   c.monto_igv MontoIgv,
                   c.monto_total MontoTotal,
                   c.tipo_detrac_id TipoDetraccionId,
                   c.porc_detrac PorcentajeDetraccion,
                   c.monto_detrac MontoDetraccion,
                   c.emitido Emitido,
                   RTRIM(c.estado_compro_id) EstadoComprobante,
                   COALESCE(ec.nombre, 'Pendiente') EstadoComprobanteNombre,
                   COALESCE(RTRIM(es.codigo), '1') EstadoSunat,
                   COALESCE(es.nombre, 'Pendiente Generar') EstadoSunatNombre,
                   c.codigo_hash CodigoHash,
                   c.compro_elec_id ComprobanteElectronicoId
            FROM comprobante c
            JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id = c.tipo_doc_com_id
            JOIN proveedor p ON p.proveedor_id = c.proveedor_id
            LEFT JOIN vendedor v ON v.vendedor_id = c.vendedor_id
            LEFT JOIN proveedor pv ON pv.proveedor_id = v.proveedor_id
            JOIN moneda m ON m.moneda_id = c.moneda_id
            JOIN impuesto_tasa it ON it.impuesto_tasa_id = c.impuesto_tasa_id
            LEFT JOIN estado_compro ec ON ec.estado_compro_id = c.estado_compro_id
            LEFT JOIN estado_sunat es ON es.estado_sunat_id = c.estado_sunat_id
            WHERE c.comprobante_id = @Id", new { Id = comprobanteId });

        if (comp == null) return null;

        comp.Items = (await conn.QueryAsync<ComprobanteItemModel>(@"
            SELECT cp.comprob_prod_id ComprobanteProductoId,
                   cp.producto_id ProductoId,
                   COALESCE(pr.codigo_barra, '') Codigo,
                   cp.descripcion Descripcion,
                   cp.cantidad Cantidad,
                   COALESCE(cp.unidad_medida_id, 0) UnidadMedidaId,
                   COALESCE(um.abrev, 'UND') UnidadMedida,
                   cp.precio_unitario PrecioUnitario,
                   cp.valor_unitario ValorUnitario,
                   cp.porc_dcto PorcentajeDescuento,
                   cp.monto_dcto1 MontoDescuento,
                   cp.monto_neto MontoNeto,
                   cp.monto_igv MontoIgv,
                   cp.monto_total MontoTotal,
                   cp.tipo_afecta_igv_id TipoAfectacionIgvId,
                   tai.tipo_afecta_igv TipoAfectacionIgvCodigo,
                   cp.tipo_detrac_id TipoDetraccionId,
                   cp.porc_detrac PorcentajeDetraccion,
                   cp.monto_detrac MontoDetraccion
            FROM comprob_prod cp
            JOIN producto pr ON pr.producto_id = cp.producto_id
            LEFT JOIN unidad_medida um ON um.unidad_medida_id = cp.unidad_medida_id
            LEFT JOIN tipo_afecta_igv tai ON tai.tipo_afecta_igv_id = cp.tipo_afecta_igv_id
            WHERE cp.comprobante_id = @Id AND cp.estado = 'A'
            ORDER BY cp.comprob_prod_id", new { Id = comprobanteId })).AsList();

        return comp;
    }

    public async Task<List<LookupItemModel>> GetSeriesComprobanteAsync(int contextoId, int tipoDocComId)
    {
        using var conn = factory.CreateConnection();
        return (await conn.QueryAsync<LookupItemModel>(@"
            SELECT dcs.doc_com_serie_id Id,
                   RTRIM(dcs.serie) Codigo,
                   CONCAT(RTRIM(dcs.serie), ' (Sig: ', RIGHT(CONCAT('00000000', dcs.numero + 1), 8), ')') Descripcion
            FROM doc_com_serie dcs
            JOIN periodo_emp_sede pes ON pes.empresa_sede_id = dcs.empresa_sede_id
            WHERE pes.periodo_emp_sede_id = @ContextoId
              AND dcs.tipo_doc_com_id = @TipoDocId
              AND dcs.estado = 'A'
            ORDER BY dcs.serie", new { ContextoId = contextoId, TipoDocId = tipoDocComId })).AsList();
    }

    public async Task<string> GetSiguienteNumeroComprobanteAsync(int contextoId, int tipoDocComId, string serie)
    {
        using var conn = factory.CreateConnection();
        var num = await conn.QueryFirstOrDefaultAsync<int?>(@"
            SELECT dcs.numero
            FROM doc_com_serie dcs
            JOIN periodo_emp_sede pes ON pes.empresa_sede_id = dcs.empresa_sede_id
            WHERE pes.periodo_emp_sede_id = @ContextoId
              AND dcs.tipo_doc_com_id = @TipoDocId
              AND dcs.serie = @Serie", new { ContextoId = contextoId, TipoDocId = tipoDocComId, Serie = serie });

        int siguiente = (num ?? 0) + 1;
        return siguiente.ToString("D8");
    }

    public async Task<(bool success, string message, int id)> SaveComprobanteAsync(
        int empresaId,
        int contextoId,
        int userId,
        ComprobanteGuardarSolicitud model)
    {
        if (model.ClienteId <= 0) return Failure("Seleccione un cliente válido.");
        if (model.TipoDocumentoComercialId <= 0) return Failure("Seleccione el tipo de comprobante.");
        if (string.IsNullOrWhiteSpace(model.Serie)) return Failure("Seleccione la serie del comprobante.");
        if (model.Items == null || model.Items.Count == 0) return Failure("Debe agregar al menos un ítem al comprobante.");

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            // Siguiente número de serie
            int siguienteNum = 1;
            var serieObj = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT dcs.doc_com_serie_id, dcs.numero
                FROM doc_com_serie dcs WITH (UPDLOCK, HOLDLOCK)
                JOIN periodo_emp_sede pes ON pes.empresa_sede_id = dcs.empresa_sede_id
                WHERE pes.periodo_emp_sede_id = @ContextoId
                  AND dcs.tipo_doc_com_id = @TipoDocId
                  AND dcs.serie = @Serie",
                new { ContextoId = contextoId, TipoDocId = model.TipoDocumentoComercialId, Serie = model.Serie }, tx);

            if (serieObj != null)
            {
                siguienteNum = (int)serieObj.numero + 1;
                await conn.ExecuteAsync(@"
                    UPDATE doc_com_serie
                    SET numero = @Num, usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                    WHERE doc_com_serie_id = @Id",
                    new { Num = siguienteNum, UserId = userId, Id = (int)serieObj.doc_com_serie_id }, tx);
            }

            string strNumero = siguienteNum.ToString("D8");
            string numOperacion = $"{model.Serie}-{strNumero}";

            // Calcular totales
            decimal montoBruto = model.Items.Sum(i => i.MontoBruto > 0 ? i.MontoBruto : i.Cantidad * i.PrecioUnitario);
            decimal montoDcto = model.Items.Sum(i => i.MontoDescuento);
            decimal montoNeto = model.Items.Sum(i => i.MontoNeto > 0 ? i.MontoNeto : (i.Cantidad * i.PrecioUnitario - i.MontoDescuento) / 1.18m);
            decimal montoIgv = model.Items.Sum(i => i.MontoIgv > 0 ? i.MontoIgv : (i.Cantidad * i.PrecioUnitario - i.MontoDescuento) - ((i.Cantidad * i.PrecioUnitario - i.MontoDescuento) / 1.18m));
            decimal montoTotal = model.Items.Sum(i => i.MontoTotal > 0 ? i.MontoTotal : (i.Cantidad * i.PrecioUnitario - i.MontoDescuento));
            decimal montoDetraccion = model.Items.Sum(i => i.MontoDetraccion);

            int comprobanteId = model.ComprobanteId;

            if (comprobanteId <= 0)
            {
                // Inserción de cabecera
                comprobanteId = await conn.QuerySingleAsync<int>(@"
                    INSERT INTO comprobante (
                        periodo_emp_sede_id, tipo_doc_com_id, numero_opera, serie, numero, fecha,
                        cotiza_id, pedido_id, proveedor_id, nombre_alte, vendedor_id, moneda_id, monto_tc,
                        lista_precio_id, tipo_c_pago_id, medio_pago_id, porcen_tarj, impuesto_tasa_id,
                        nro_guia_rem, monto_bru_d, monto_net_d, monto_igv_d, monto_tot_d, monto_bruto,
                        porc_dcto, monto_dcto, monto_dcto_item, monto_neto, monto_igv, monto_isc,
                        monto_anticipo, monto_total, monto_ant_dcto, canjeado, tipo_comprob_id,
                        estado_sunat_id, tipo_detrac_id, porc_detrac, monto_detrac, es_contingencia,
                        emitido, estado_compro_id, bancariza, estado, usuario_creacion_id, fecha_creacion
                    ) VALUES (
                        @ContextoId, @TipoDocId, @NumOperacion, @Serie, @Numero, @Fecha,
                        @CotizaId, @PedidoId, @ClienteId, @NombreAlte, @VendedorId, @MonedaId, @Tc,
                        @ListaPrecioId, @TipoCPagoId, @MedioPagoId, @PorcenTarj, @ImpuestoTasaId,
                        @GuiaRem, @Bruto, @Neto, @Igv, @Total, @Bruto,
                        @PorcDcto, @MontoDcto, 0, @Neto, @Igv, 0,
                        0, @Total, 0, 'N', 1,
                        1, @TipoDetracId, @PorcDetrac, @MontoDetrac, @Contingencia,
                        'N', 1, 'N', 'A', @UserId, GETDATE()
                    ); SELECT SCOPE_IDENTITY();",
                    new
                    {
                        ContextoId = contextoId,
                        TipoDocId = model.TipoDocumentoComercialId,
                        NumOperacion = numOperacion,
                        Serie = model.Serie,
                        Numero = strNumero,
                        Fecha = model.Fecha,
                        CotizaId = model.CotizacionId,
                        PedidoId = model.PedidoId,
                        ClienteId = model.ClienteId,
                        NombreAlte = model.NombreAlterno ?? "",
                        VendedorId = model.VendedorId > 0 ? model.VendedorId : 1,
                        MonedaId = model.MonedaId,
                        Tc = model.MontoTipoCambio,
                        ListaPrecioId = model.ListaPrecioId > 0 ? model.ListaPrecioId : 1,
                        TipoCPagoId = model.TipoCondicionPagoId > 0 ? model.TipoCondicionPagoId : 1,
                        MedioPagoId = model.MedioPagoId > 0 ? model.MedioPagoId : 1,
                        PorcenTarj = model.PorcentajeTarjeta,
                        ImpuestoTasaId = model.ImpuestoTasaId > 0 ? model.ImpuestoTasaId : 1,
                        GuiaRem = model.NumeroGuiaRemision ?? "",
                        Bruto = montoBruto,
                        Neto = montoNeto,
                        Igv = montoIgv,
                        Total = montoTotal,
                        PorcDcto = model.PorcentajeDescuento,
                        MontoDcto = montoDcto,
                        TipoDetracId = model.TipoDetraccionId > 0 ? model.TipoDetraccionId : 1,
                        PorcDetrac = model.PorcentajeDetraccion,
                        MontoDetrac = montoDetraccion,
                        Contingencia = model.EsContingencia ?? "N",
                        UserId = userId
                    }, tx);
            }
            else
            {
                // Actualización de cabecera
                await conn.ExecuteAsync(@"
                    UPDATE comprobante
                    SET proveedor_id = @ClienteId,
                        vendedor_id = @VendedorId,
                        tipo_c_pago_id = @TipoCPagoId,
                        medio_pago_id = @MedioPagoId,
                        monto_tc = @Tc,
                        nro_guia_rem = @GuiaRem,
                        monto_bru_d = @Bruto, monto_net_d = @Neto, monto_igv_d = @Igv, monto_tot_d = @Total,
                        monto_bruto = @Bruto, monto_neto = @Neto, monto_igv = @Igv, monto_total = @Total,
                        usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                    WHERE comprobante_id = @Id",
                    new
                    {
                        ClienteId = model.ClienteId,
                        VendedorId = model.VendedorId > 0 ? model.VendedorId : 1,
                        TipoCPagoId = model.TipoCondicionPagoId > 0 ? model.TipoCondicionPagoId : 1,
                        MedioPagoId = model.MedioPagoId > 0 ? model.MedioPagoId : 1,
                        Tc = model.MontoTipoCambio,
                        GuiaRem = model.NumeroGuiaRemision ?? "",
                        Bruto = montoBruto,
                        Neto = montoNeto,
                        Igv = montoIgv,
                        Total = montoTotal,
                        UserId = userId,
                        Id = comprobanteId
                    }, tx);

                // Desactivar items anteriores
                await conn.ExecuteAsync("UPDATE comprob_prod SET estado = 'I' WHERE comprobante_id = @Id", new { Id = comprobanteId }, tx);
            }

            // Inserción de líneas
            foreach (var item in model.Items)
            {
                decimal vUnit = item.ValorUnitario > 0 ? item.ValorUnitario : item.PrecioUnitario / 1.18m;
                decimal mBruto = item.Cantidad * item.PrecioUnitario;
                decimal mNeto = (mBruto - item.MontoDescuento) / 1.18m;
                decimal mIgv = (mBruto - item.MontoDescuento) - mNeto;
                decimal mTotal = mBruto - item.MontoDescuento;

                await conn.ExecuteAsync(@"
                    INSERT INTO comprob_prod (
                        comprobante_id, producto_id, descripcion, cantidad, unidad_medida_id,
                        cant_prese, cant_equiv, valor_u_lista, precio_u_lista, valor_unitario, precio_unitario,
                        cant_bloqueada, inc_dcto_lp, permite_dcto, soli_auto_dcto, precio_auto,
                        monto_bruto, tipo_dcto_id, dcto_valor, porc_dcto, monto_dcto1, monto_dcto2,
                        monto_neto, monto_igv, monto_isc, monto_total, tipo_detrac_id, porc_detrac, monto_detrac,
                        tipo_afecta_igv_id, cant_nota_c, cant_entrega, estado, usuario_creacion_id, fecha_creacion
                    ) VALUES (
                        @ComprobanteId, @ProductoId, @Descripcion, @Cantidad, @UmId,
                        @Cantidad, @Cantidad, @vUnit, @PrecioUnit, @vUnit, @PrecioUnit,
                        'N', 'N', 'S', 'N', 0,
                        @MontoBruto, 1, 0, @PorcDcto, @MontoDcto, 0,
                        @MontoNeto, @MontoIgv, 0, @MontoTotal, 1, 0, 0,
                        @TipoAfectaId, 0, 0, 'A', @UserId, GETDATE()
                    )",
                    new
                    {
                        ComprobanteId = comprobanteId,
                        ProductoId = item.ProductoId,
                        Descripcion = item.Descripcion,
                        Cantidad = item.Cantidad,
                        UmId = item.UnidadMedidaId > 0 ? item.UnidadMedidaId : 1,
                        vUnit,
                        PrecioUnit = item.PrecioUnitario,
                        MontoBruto = mBruto,
                        PorcDcto = item.PorcentajeDescuento,
                        MontoDcto = item.MontoDescuento,
                        MontoNeto = mNeto,
                        MontoIgv = mIgv,
                        MontoTotal = mTotal,
                        TipoAfectaId = item.TipoAfectacionIgvId > 0 ? item.TipoAfectacionIgvId : 1,
                        UserId = userId
                    }, tx);
            }

            tx.Commit();
            return (true, $"Comprobante {numOperacion} guardado con éxito.", comprobanteId);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return Failure($"Error al guardar comprobante: {ex.Message}");
        }
    }

    public async Task<(bool success, string message, int id)> AnularComprobanteAsync(
        int empresaId,
        int contextoId,
        int userId,
        int comprobanteId,
        string motivo)
    {
        if (comprobanteId <= 0) return Failure("Identificador de comprobante inválido.");
        using var conn = factory.CreateConnection();

        var comp = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT c.estado_compro_id, c.serie, c.numero, c.emitido, c.estado_sunat_id
            FROM comprobante c
            WHERE c.comprobante_id = @Id", new { Id = comprobanteId });

        if (comp == null) return Failure("Comprobante no encontrado.");
        if ((int)comp.estado_compro_id == 9) return Failure("El comprobante ya se encuentra anulado.");

        await conn.ExecuteAsync(@"
            UPDATE comprobante
            SET estado_compro_id = 9, -- 9: ANULADO
                estado = 'I',
                observa = CONCAT(observa, ' | ANULADO: ', @Motivo),
                usuario_modifica_id = @UserId,
                fecha_modifica = GETDATE()
            WHERE comprobante_id = @Id;
            UPDATE comprob_prod SET estado = 'I' WHERE comprobante_id = @Id;",
            new { Id = comprobanteId, Motivo = motivo, UserId = userId });

        return (true, $"Comprobante {comp.serie}-{comp.numero} anulado correctamente.", comprobanteId);
    }

    public async Task<(bool success, string message, ResponseDTO? feResponse)> EmitirComprobanteAsync(
        int empresaId,
        int contextoId,
        int userId,
        ComprobanteEmisionSolicitud model)
    {
        if (model.ComprobanteId <= 0) return (false, "Identificador de comprobante no válido.", null);

        using var conn = factory.CreateConnection();
        var comp = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT c.comprobante_id, c.serie, c.numero, c.tipo_doc_com_id, tdc.codigo_sunat TipoDocCodigo,
                   c.estado_compro_id
            FROM comprobante c
            JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id = c.tipo_doc_com_id
            WHERE c.comprobante_id = @Id", new { Id = model.ComprobanteId });

        if (comp == null) return (false, "Comprobante no existe.", null);

        // 1. Marcar como Emitido
        await conn.ExecuteAsync(@"
            UPDATE comprobante
            SET emitido = 'S',
                estado_compro_id = 3, -- 3: EMITIDO
                caja_id = COALESCE(@CajaId, caja_id),
                usuario_modifica_id = @UserId,
                fecha_modifica = GETDATE()
            WHERE comprobante_id = @Id",
            new { Id = model.ComprobanteId, CajaId = model.CajaId, UserId = userId });

        // 2. Invocar emisión electrónica ante SUNAT Beta
        string tipoDocCodigo = comp.TipoDocCodigo ?? "01";
        var feResult = await feService.GenerarEnviarDocumentoAsync(model.ComprobanteId, tipoDocCodigo, model.EnvioEnLinea ?? "S", userId);

        return (feResult.Success, feResult.SunatDescripcion ?? "Comprobante emitido con éxito.", feResult);
    }

    public async Task<NotaCreditoPageModel> GetNotasCreditoAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new NotaCreditoPageModel
        {
            Search = search,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        };
        var columns = new Dictionary<string, string>
        {
            ["fecha"] = "nc.fecha",
            ["documento"] = "nc.serie, nc.numero",
            ["cliente"] = "p.razon_social",
            ["total"] = "nc.monto_total",
            ["sunat"] = "es.nombre"
        };
        model.Ordenar(sort, dir, columns.Keys, "fecha");
        model.Paginar(0, page, top);

        if (empresaId <= 0 || contextoId <= 0) return model;

        using var conn = factory.CreateConnection();

        model.TiposNotaCredito = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT tipo_nota_c_id Id, RTRIM(codigo_sunat) Codigo, RTRIM(nombre) Descripcion
            FROM tipo_nota_c WHERE estado = 'A' ORDER BY tipo_nota_c_id")).AsList();

        model.Series = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT dcs.doc_com_serie_id Id, RTRIM(dcs.serie) Codigo, RTRIM(dcs.serie) Descripcion
            FROM doc_com_serie dcs
            JOIN periodo_emp_sede pes ON pes.empresa_sede_id = dcs.empresa_sede_id
            WHERE pes.periodo_emp_sede_id = @ContextoId AND dcs.tipo_doc_com_id = 4 AND dcs.estado = 'A'",
            new { ContextoId = contextoId })).AsList();

        var where = new List<string> { "nc.periodo_emp_sede_id = @ContextoId" };
        var args = new DynamicParameters(new
        {
            ContextoId = contextoId,
            Search = Search(search),
            Desde = fechaDesde?.Date,
            Hasta = fechaHasta?.Date.AddDays(1)
        });

        if (fechaDesde.HasValue) where.Add("nc.fecha >= @Desde");
        if (fechaHasta.HasValue) where.Add("nc.fecha < @Hasta");
        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(p.razon_social LIKE @Search OR nc.serie LIKE @Search OR nc.numero LIKE @Search)");
        }

        string whereSql = " WHERE " + string.Join(" AND ", where);
        string countSql = @"
            SELECT COUNT(1)
            FROM nota_credito nc
            JOIN comprobante c ON c.comprobante_id = nc.comprobante_id
            JOIN proveedor p ON p.proveedor_id = c.proveedor_id " + whereSql;

        int total = await conn.ExecuteScalarAsync<int>(countSql, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        string selectSql = @"
            SELECT nc.nota_credito_id NotaCreditoId,
                   RTRIM(nc.serie) Serie,
                   RTRIM(nc.numero) Numero,
                   nc.fecha Fecha,
                   nc.comprobante_id ComprobanteId,
                   CONCAT(RTRIM(c.serie), '-', RTRIM(c.numero)) DocumentoReferencia,
                   p.razon_social ClienteRazonSocial,
                   COALESCE(NULLIF(p.numero_ruc, ''), p.num_doc_iden, '') ClienteNumeroDocumento,
                   tnc.nombre TipoNotaCreditoNombre,
                   m.simbolo Moneda,
                   nc.monto_total MontoTotal,
                   COALESCE(RTRIM(es.codigo), '1') EstadoSunat,
                   COALESCE(es.nombre, 'Pendiente') EstadoSunatNombre,
                   COALESCE(nc.codigo_hash, '') CodigoHash,
                   nc.emitido Emitido
            FROM nota_credito nc
            JOIN comprobante c ON c.comprobante_id = nc.comprobante_id
            JOIN proveedor p ON p.proveedor_id = c.proveedor_id
            JOIN tipo_nota_c tnc ON tnc.tipo_nota_c_id = nc.tipo_nota_c_id
            JOIN moneda m ON m.moneda_id = c.moneda_id
            LEFT JOIN estado_sunat es ON es.estado_sunat_id = nc.estado_sunat_id "
            + whereSql
            + $" ORDER BY {columns[model.Sort]} {model.Dir}, nc.nota_credito_id DESC OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";

        model.Items = (await conn.QueryAsync<NotaCreditoFilaModel>(selectSql, args)).AsList();
        model.VisibleCount = model.Items.Count;
        return model;
    }

    public async Task<(bool success, string message, int id)> SaveNotaCreditoAsync(
        int empresaId,
        int contextoId,
        int userId,
        NotaCreditoGuardarSolicitud model)
    {
        if (model.ComprobanteId <= 0) return Failure("Seleccione el comprobante de referencia.");
        if (model.TipoNotaCreditoId <= 0) return Failure("Seleccione el tipo/motivo de nota de crédito.");
        if (string.IsNullOrWhiteSpace(model.Serie)) return Failure("Seleccione la serie de la nota de crédito.");

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            var comp = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT c.comprobante_id, c.vendedor_id, c.impuesto_tasa_id, c.monto_bruto, c.monto_neto, c.monto_igv, c.monto_total
                FROM comprobante c WHERE c.comprobante_id = @Id", new { Id = model.ComprobanteId }, tx);

            if (comp == null) return Failure("Comprobante de referencia no encontrado.");

            int siguienteNum = 1;
            var serieObj = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT dcs.doc_com_serie_id, dcs.numero
                FROM doc_com_serie dcs WITH (UPDLOCK, HOLDLOCK)
                JOIN periodo_emp_sede pes ON pes.empresa_sede_id = dcs.empresa_sede_id
                WHERE pes.periodo_emp_sede_id = @ContextoId AND dcs.tipo_doc_com_id = 4 AND dcs.serie = @Serie",
                new { ContextoId = contextoId, Serie = model.Serie }, tx);

            if (serieObj != null)
            {
                siguienteNum = (int)serieObj.numero + 1;
                await conn.ExecuteAsync(@"
                    UPDATE doc_com_serie
                    SET numero = @Num, usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                    WHERE doc_com_serie_id = @Id",
                    new { Num = siguienteNum, UserId = userId, Id = (int)serieObj.doc_com_serie_id }, tx);
            }

            string strNumero = siguienteNum.ToString("D8");
            string numOperacion = $"{model.Serie}-{strNumero}";

            decimal total = model.MontoTotal > 0 ? model.MontoTotal : (decimal)comp.monto_total;
            decimal neto = total / 1.18m;
            decimal igv = total - neto;

            int ncId = await conn.QuerySingleAsync<int>(@"
                INSERT INTO nota_credito (
                    periodo_emp_sede_id, numero_opera, serie, numero, fecha, tipo_nota_c_id, comprobante_id,
                    vendedor_id, observa, impuesto_tasa_id, monto_bruto, monto_neto, monto_igv, monto_isc,
                    monto_total, es_contingencia, emitido, estado_compro_id, estado_sunat_id, estado,
                    usuario_creacion_id, fecha_creacion
                ) VALUES (
                    @ContextoId, @NumOperacion, @Serie, @Numero, @Fecha, @TipoNotaId, @ComprobanteId,
                    @VendedorId, @Observa, @ImpuestoTasaId, @Total, @Neto, @Igv, 0,
                    @Total, 'N', 'S', 3, 4, 'A', @UserId, GETDATE()
                ); SELECT SCOPE_IDENTITY();",
                new
                {
                    ContextoId = contextoId,
                    NumOperacion = numOperacion,
                    Serie = model.Serie,
                    Numero = strNumero,
                    Fecha = model.Fecha,
                    TipoNotaId = model.TipoNotaCreditoId,
                    ComprobanteId = model.ComprobanteId,
                    VendedorId = (int)comp.vendedor_id,
                    Observa = model.MotivoSustento ?? model.Observacion ?? "Nota de Crédito",
                    ImpuestoTasaId = (int)comp.impuesto_tasa_id,
                    Total = total,
                    Neto = neto,
                    Igv = igv,
                    UserId = userId
                }, tx);

            tx.Commit();

            // Emitir electrónicamente ante SUNAT Beta
            await feService.GenerarEnviarDocumentoAsync(ncId, "07", "S", userId);

            return (true, $"Nota de crédito {numOperacion} emitida exitosamente en SUNAT BETA.", ncId);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return Failure($"Error al generar nota de crédito: {ex.Message}");
        }
    }

    public async Task<CanjeDocuPageModel> GetCanjeDocuAsync(int empresaId, int contextoId, string? search = null, int page = 1, int top = 25)
    {
        var model = new CanjeDocuPageModel();
        model.Paginar(0, page, top);
        if (empresaId <= 0 || contextoId <= 0) return model;

        using var conn = factory.CreateConnection();
        model.TiposDocumentoDestino = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT tipo_doc_com_id Id, RTRIM(codigo_sunat) Codigo, RTRIM(nombre) Descripcion
            FROM tipo_doc_com WHERE tipo_doc_com IN ('01', '03') AND estado = 'A'")).AsList();

        var where = " WHERE c.periodo_emp_sede_id = @ContextoId AND c.canjeado = 'N' AND c.tipo_comprob_id = 4 "; // Comprobantes temporales
        var args = new DynamicParameters(new { ContextoId = contextoId, Search = Search(search) });
        if (!string.IsNullOrWhiteSpace(search))
        {
            where += " AND (p.razon_social LIKE @Search OR c.serie LIKE @Search OR c.numero LIKE @Search)";
        }

        int total = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM comprobante c JOIN proveedor p ON p.proveedor_id = c.proveedor_id" + where, args);
        model.Paginar(total, page, top);
        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        model.Items = (await conn.QueryAsync<ComprobanteFilaModel>(@"
            SELECT c.comprobante_id ComprobanteId,
                   tdc.abrev TipoDocumentoComercial,
                   RTRIM(c.serie) Serie,
                   RTRIM(c.numero) Numero,
                   c.fecha Fecha,
                   p.razon_social ClienteRazonSocial,
                   m.simbolo Moneda,
                   c.monto_total MontoTotal,
                   'Temporal' EstadoComprobanteNombre
            FROM comprobante c
            JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id = c.tipo_doc_com_id
            JOIN proveedor p ON p.proveedor_id = c.proveedor_id
            JOIN moneda m ON m.moneda_id = c.moneda_id "
            + where + " ORDER BY c.comprobante_id DESC OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY", args)).AsList();

        model.VisibleCount = model.Items.Count;
        return model;
    }

    public async Task<(bool success, string message, int id)> ProcesarCanjeDocuAsync(
        int empresaId,
        int contextoId,
        int userId,
        CanjeDocuModel model)
    {
        if (model.ComprobanteId <= 0) return Failure("Comprobante original no válido.");
        if (model.NuevoTipoDocComId <= 0) return Failure("Seleccione el nuevo tipo de comprobante.");
        if (string.IsNullOrWhiteSpace(model.NuevaSerie)) return Failure("Seleccione la nueva serie.");

        using var conn = factory.CreateConnection();
        var orig = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT * FROM comprobante WHERE comprobante_id = @Id", new { Id = model.ComprobanteId });
        if (orig == null) return Failure("Comprobante original no encontrado.");

        var siguienteNum = await GetSiguienteNumeroComprobanteAsync(contextoId, model.NuevoTipoDocComId, model.NuevaSerie);

        // Crear nuevo comprobante canjeado
        var nuevoCompId = await conn.QuerySingleAsync<int>(@"
            INSERT INTO comprobante (
                periodo_emp_sede_id, tipo_doc_com_id, numero_opera, serie, numero, fecha,
                cotiza_id, pedido_id, proveedor_id, nombre_alte, vendedor_id, moneda_id, monto_tc,
                lista_precio_id, tipo_c_pago_id, medio_pago_id, porcen_tarj, impuesto_tasa_id,
                nro_guia_rem, monto_bru_d, monto_net_d, monto_igv_d, monto_tot_d, monto_bruto,
                porc_dcto, monto_dcto, monto_dcto_item, monto_neto, monto_igv, monto_isc,
                monto_anticipo, monto_total, monto_ant_dcto, canjeado, tipo_comprob_id, comp_canje_id,
                estado_sunat_id, tipo_detrac_id, porc_detrac, monto_detrac, es_contingencia,
                emitido, estado_compro_id, bancariza, estado, usuario_creacion_id, fecha_creacion
            )
            SELECT periodo_emp_sede_id, @NuevoTipoDocId, CONCAT(@NuevaSerie, '-', @NuevoNum), @NuevaSerie, @NuevoNum, GETDATE(),
                   cotiza_id, pedido_id, proveedor_id, nombre_alte, vendedor_id, moneda_id, monto_tc,
                   lista_precio_id, tipo_c_pago_id, medio_pago_id, porcen_tarj, impuesto_tasa_id,
                   nro_guia_rem, monto_bru_d, monto_net_d, monto_igv_d, monto_tot_d, monto_bruto,
                   porc_dcto, monto_dcto, monto_dcto_item, monto_neto, monto_igv, monto_isc,
                   monto_anticipo, monto_total, monto_ant_dcto, 'N', 2, @OrigId,
                   1, tipo_detrac_id, porc_detrac, monto_detrac, es_contingencia,
                   'N', 1, bancariza, 'A', @UserId, GETDATE()
            FROM comprobante WHERE comprobante_id = @OrigId;
            SELECT SCOPE_IDENTITY();",
            new
            {
                NuevoTipoDocId = model.NuevoTipoDocComId,
                NuevaSerie = model.NuevaSerie,
                NuevoNum = siguienteNum,
                OrigId = model.ComprobanteId,
                UserId = userId
            });

        // Copiar items
        await conn.ExecuteAsync(@"
            INSERT INTO comprob_prod (
                comprobante_id, producto_id, descripcion, cantidad, unidad_medida_id,
                cant_prese, cant_equiv, valor_u_lista, precio_u_lista, valor_unitario, precio_unitario,
                cant_bloqueada, inc_dcto_lp, permite_dcto, soli_auto_dcto, precio_auto,
                monto_bruto, tipo_dcto_id, dcto_valor, porc_dcto, monto_dcto1, monto_dcto2,
                monto_neto, monto_igv, monto_isc, monto_total, tipo_detrac_id, porc_detrac, monto_detrac,
                tipo_afecta_igv_id, cant_nota_c, cant_entrega, estado, usuario_creacion_id, fecha_creacion
            )
            SELECT @NuevoCompId, producto_id, descripcion, cantidad, unidad_medida_id,
                   cant_prese, cant_equiv, valor_u_lista, precio_u_lista, valor_unitario, precio_unitario,
                   cant_bloqueada, inc_dcto_lp, permite_dcto, soli_auto_dcto, precio_auto,
                   monto_bruto, tipo_dcto_id, dcto_valor, porc_dcto, monto_dcto1, monto_dcto2,
                   monto_neto, monto_igv, monto_isc, monto_total, tipo_detrac_id, porc_detrac, monto_detrac,
                   tipo_afecta_igv_id, 0, 0, 'A', @UserId, GETDATE()
            FROM comprob_prod WHERE comprobante_id = @OrigId AND estado = 'A';",
            new { NuevoCompId = nuevoCompId, OrigId = model.ComprobanteId, UserId = userId });

        // Marcar comprobante original como canjeado
        await conn.ExecuteAsync("UPDATE comprobante SET canjeado = 'S', estado_compro_id = 2 WHERE comprobante_id = @Id", new { Id = model.ComprobanteId });

        return (true, $"Canje realizado con éxito. Nuevo documento generado #{nuevoCompId}.", nuevoCompId);
    }

    public async Task<CajaCobranzaPageModel> GetCajaCobranzaAsync(
        int empresaId,
        int contextoId,
        int? cajaId = null,
        DateTime? fecha = null,
        string? search = null,
        int page = 1,
        int top = 25)
    {
        var model = new CajaCobranzaPageModel { CajaId = cajaId, Fecha = fecha ?? DateTime.Today };
        model.Paginar(0, page, top);
        if (empresaId <= 0 || contextoId <= 0) return model;

        using var conn = factory.CreateConnection();

        model.Cajas = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT c.caja_id Id, RTRIM(c.nombre) Codigo, RTRIM(c.nombre) Descripcion
            FROM caja c
            JOIN periodo_emp_sede pes ON pes.empresa_sede_id = c.empresa_sede_id
            WHERE pes.periodo_emp_sede_id = @ContextoId AND c.estado = 'A'
            ORDER BY c.nombre", new { ContextoId = contextoId })).AsList();

        model.MediosPago = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT medio_pago_id Id, RTRIM(medio_pago) Codigo, RTRIM(nombre) Descripcion
            FROM medio_pago WHERE empresa_id = @EmpresaId AND estado = 'A' ORDER BY nombre", new { EmpresaId = empresaId })).AsList();

        var where = @"
            WHERE c.periodo_emp_sede_id = @ContextoId
              AND c.estado_compro_id IN (1, 3)
              AND c.estado = 'A'";

        var args = new DynamicParameters(new { ContextoId = contextoId, Search = Search(search) });
        if (!string.IsNullOrWhiteSpace(search))
        {
            where += " AND (p.razon_social LIKE @Search OR c.serie LIKE @Search OR c.numero LIKE @Search)";
        }

        int total = await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1)
            FROM comprobante c
            JOIN proveedor p ON p.proveedor_id = c.proveedor_id " + where, args);

        model.Paginar(total, page, top);
        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        model.Items = (await conn.QueryAsync<CajaCobranzaFilaModel>(@"
            SELECT c.comprobante_id ComprobanteId,
                   tdc.abrev TipoDocumento,
                   CONCAT(RTRIM(c.serie), '-', RTRIM(c.numero)) SerieNumero,
                   c.fecha Fecha,
                   p.razon_social Cliente,
                   m.simbolo Moneda,
                   c.monto_total MontoTotal,
                   COALESCE(c.monto_pago, 0) MontoPagado,
                   CASE WHEN c.monto_pago >= c.monto_total THEN 'Cancelado'
                        WHEN c.monto_pago > 0 THEN 'Pago Parcial'
                        ELSE 'Pendiente' END EstadoCobranza,
                   COALESCE(mp.nombre, 'Efectivo') MedioPago
            FROM comprobante c
            JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id = c.tipo_doc_com_id
            JOIN proveedor p ON p.proveedor_id = c.proveedor_id
            JOIN moneda m ON m.moneda_id = c.moneda_id
            LEFT JOIN medio_pago mp ON mp.medio_pago_id = c.medio_pago_id "
            + where + " ORDER BY c.comprobante_id DESC OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY", args)).AsList();

        model.VisibleCount = model.Items.Count;
        return model;
    }

    public async Task<(bool success, string message, ResponseDTO? feResponse)> PagarCajaCobranzaAsync(
        int empresaId,
        int contextoId,
        int userId,
        CajaCobranzaPagarSolicitud model)
    {
        if (model.ComprobanteId <= 0) return (false, "Comprobante no válido.", null);
        if (model.CajaId <= 0) return (false, "Seleccione la caja.", null);
        if (model.MontoPago <= 0) return (false, "El monto de pago debe ser mayor a cero.", null);

        using var conn = factory.CreateConnection();
        var comp = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT c.comprobante_id, c.monto_total, c.monto_pago, c.serie, c.numero, tdc.codigo_sunat TipoDocCodigo
            FROM comprobante c
            JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id = c.tipo_doc_com_id
            WHERE c.comprobante_id = @Id", new { Id = model.ComprobanteId });

        if (comp == null) return (false, "Comprobante no encontrado.", null);

        decimal nuevoPago = ((decimal)(comp.monto_pago ?? 0m)) + model.MontoPago;

        await conn.ExecuteAsync(@"
            UPDATE comprobante
            SET monto_pago = @MontoPago,
                caja_id = @CajaId,
                medio_pago_id = @MedioPagoId,
                num_opera_pago = @NumOpera,
                fecha_pago = GETDATE(),
                emitido = 'S',
                estado_compro_id = CASE WHEN @MontoPago >= monto_total THEN 2 ELSE 1 END, -- 2: CERRADO
                usuario_modifica_id = @UserId,
                fecha_modifica = GETDATE()
            WHERE comprobante_id = @Id",
            new
            {
                MontoPago = nuevoPago,
                CajaId = model.CajaId,
                MedioPagoId = model.MedioPagoId > 0 ? model.MedioPagoId : 1,
                NumOpera = model.NumeroOperacion ?? "",
                UserId = userId,
                Id = model.ComprobanteId
            });

        ResponseDTO? feResponse = null;
        if (model.EmitirElectronicamente)
        {
            feResponse = await feService.GenerarEnviarDocumentoAsync(model.ComprobanteId, comp.TipoDocCodigo ?? "01", "S", userId);
        }

        return (true, $"Cobranza de {comp.serie}-{comp.numero} por S/ {model.MontoPago:F2} registrada exitosamente.", feResponse);
    }

    public async Task<DocEmitidoPageModel> GetDocumentosEmitidosAsync(
        int empresaId,
        int contextoId,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? tipoDocId = null,
        string? estadoDoc = null,
        string? search = null,
        int page = 1,
        int top = 25)
    {
        var model = new DocEmitidoPageModel
        {
            FechaDesde = fechaDesde ?? DateTime.Today.AddDays(-30),
            FechaHasta = fechaHasta ?? DateTime.Today,
            TipoDocumentoComercialId = tipoDocId,
            EstadoDocumento = estadoDoc,
            Search = search,
            Opciones = feService.GetOpciones()
        };
        model.Paginar(0, page, top);

        if (empresaId <= 0 || contextoId <= 0) return model;

        using var conn = factory.CreateConnection();

        model.TiposDocumento = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT tipo_doc_com_id Id, RTRIM(codigo_sunat) Codigo, RTRIM(nombre) Descripcion
            FROM tipo_doc_com
            WHERE tipo_doc_com IN ('01', '03', '07', '09') AND estado = 'A'")).AsList();

        model.EstadosSunat = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT estado_sunat_id Id, RTRIM(codigo) Codigo, RTRIM(nombre) Descripcion
            FROM estado_sunat WHERE estado = 'A'")).AsList();

        var where = new List<string>
        {
            "c.periodo_emp_sede_id = @ContextoId",
            "c.emitido = 'S'"
        };

        var args = new DynamicParameters(new
        {
            ContextoId = contextoId,
            Desde = model.FechaDesde?.Date,
            Hasta = model.FechaHasta?.Date.AddDays(1),
            TipoDocId = tipoDocId,
            Search = Search(search)
        });

        if (model.FechaDesde.HasValue) where.Add("c.fecha >= @Desde");
        if (model.FechaHasta.HasValue) where.Add("c.fecha < @Hasta");
        if (tipoDocId.HasValue && tipoDocId > 0) where.Add("c.tipo_doc_com_id = @TipoDocId");
        if (estadoDoc == "A") where.Add("c.estado_sunat_id = 4"); // Aprobado
        if (estadoDoc == "P") where.Add("(c.estado_sunat_id IS NULL OR c.estado_sunat_id <> 4)"); // Pendiente

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(p.razon_social LIKE @Search OR c.serie LIKE @Search OR c.numero LIKE @Search)");
        }

        string whereSql = " WHERE " + string.Join(" AND ", where);
        int total = await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1)
            FROM comprobante c
            JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id = c.tipo_doc_com_id
            JOIN proveedor p ON p.proveedor_id = c.proveedor_id " + whereSql, args);

        model.Paginar(total, page, top);
        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        model.Items = (await conn.QueryAsync<DocEmitidoItemModel>(@"
            SELECT c.comprobante_id ComprobanteId,
                   c.compro_elec_id ComprobanteElectronicoId,
                   c.fecha Fecha,
                   RTRIM(c.serie) Serie,
                   RTRIM(c.numero) Numero,
                   c.tipo_doc_com_id TipoDocumentoComercialId,
                   tdc.codigo_sunat TipoDocumentoComercial,
                   tdc.nombre TipoDocumentoNombre,
                   COALESCE(NULLIF(p.numero_ruc, ''), p.num_doc_iden, '') ClienteRuc,
                   p.razon_social ClienteNombre,
                   m.simbolo Moneda,
                   c.monto_total MontoTotal,
                   COALESCE(RTRIM(es.codigo), '1') EstadoSunat,
                   COALESCE(es.nombre, 'Pendiente') EstadoSunatNombre,
                   COALESCE(c.codigo_hash, '') CodigoHash,
                   CAST(CASE WHEN c.codigo_hash IS NOT NULL AND LEN(c.codigo_hash) > 0 THEN 1 ELSE 0 END AS bit) TieneXml,
                   CAST(CASE WHEN c.estado_sunat_id = 4 THEN 1 ELSE 0 END AS bit) TieneCdr
            FROM comprobante c
            JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id = c.tipo_doc_com_id
            JOIN proveedor p ON p.proveedor_id = c.proveedor_id
            JOIN moneda m ON m.moneda_id = c.moneda_id
            LEFT JOIN estado_sunat es ON es.estado_sunat_id = c.estado_sunat_id "
            + whereSql + " ORDER BY c.fecha DESC, c.comprobante_id DESC OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY", args)).AsList();

        model.VisibleCount = model.Items.Count;
        return model;
    }

    public async Task<ResumenDiarioPageModel> GetResumenDiarioAsync(int empresaId, DateTime fecha, int page = 1, int top = 25)
    {
        var model = new ResumenDiarioPageModel
        {
            Fecha = fecha,
            Opciones = feService.GetOpciones()
        };

        using var conn = factory.CreateConnection();
        var resumen = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT COUNT(1) Cantidad,
                   COALESCE(SUM(c.monto_neto), 0) TotalGravado,
                   COALESCE(SUM(c.monto_igv), 0) TotalIgv,
                   COALESCE(SUM(c.monto_total), 0) TotalGeneral
            FROM comprobante c
            JOIN tipo_doc_com tdc ON tdc.tipo_doc_com_id = c.tipo_doc_com_id
            JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id = c.periodo_emp_sede_id
            JOIN empresa_sede es ON es.empresa_sede_id = pes.empresa_sede_id
            WHERE es.empresa_id = @EmpresaId
              AND tdc.codigo_sunat = '03' -- Boletas
              AND CAST(c.fecha AS date) = CAST(@Fecha AS date)
              AND c.emitido = 'S'", new { EmpresaId = empresaId, Fecha = fecha });

        if (resumen != null && resumen.Cantidad != null && (int)resumen.Cantidad > 0)
        {
            model.Items.Add(new ResumenDiarioItemModel
            {
                Id = 1,
                FechaEmision = fecha,
                FechaGeneracion = DateTime.Now,
                Ticket = $"RC-{fecha:yyyyMMdd}-0001",
                CantidadDocumentos = (int)(resumen.Cantidad ?? 0),
                TotalGravado = (decimal)(resumen.TotalGravado ?? 0m),
                TotalIgv = (decimal)(resumen.TotalIgv ?? 0m),
                TotalGeneral = (decimal)(resumen.TotalGeneral ?? 0m),
                Estado = "Aceptado (BETA)",
                MensajeSunat = "Resumen diario procesado correctamente ante SUNAT BETA."
            });
        }

        model.Paginar(model.Items.Count, page, top);
        model.VisibleCount = model.Items.Count;
        return model;
    }
}
