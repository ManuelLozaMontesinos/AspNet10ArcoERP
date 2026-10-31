using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services
{
    public partial class InventarioService
    {
        public async Task<GuiaRemiPageModel> GetGuiasRemisionAsync(
            int empresaId,
            int contextoId,
            int? almacenId = null,
            string? codigo = null,
            string? search = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            int? estadoGuiaId = null,
            int? estadoAprobacionId = null,
            int? motivoTrasladoId = null,
            bool esInterno = true,
            int page = 1,
            int top = 25,
            string? sort = null,
            string? dir = null)
        {
            var columns = new Dictionary<string, string>
            {
                ["id"] = "gr.guia_remision_id",
                ["serienumero"] = "gr.serie",
                ["fecha"] = "gr.fecha",
                ["fechatraslado"] = "gr.fec_ini_tra",
                ["almacen"] = "a.nombre",
                ["movimiento"] = "ma.mov_almacen",
                ["destinatario"] = "p2.razon_social",
                ["motivo"] = "mt.nombre",
                ["transportista"] = "t.razon_social",
                ["estadoguia"] = "egr.nombre",
                ["estadoaproba"] = "ea.nombre",
                ["estado"] = "gr.estado"
            };

            var model = new GuiaRemiPageModel
            {
                AlmacenId = almacenId,
                Codigo = codigo,
                Search = search,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                EstadoGuiaId = estadoGuiaId,
                EstadoAprobacionId = estadoAprobacionId,
                MotivoTrasladoId = motivoTrasladoId,
                EsInterno = esInterno
            };

            model.Ordenar(sort, dir, columns.Keys, "fecha", "desc");
            model.Paginar(0, page, top);

            using var conn = factory.CreateConnection();

            // Lookups para filtros
            model.Almacenes = (await conn.QueryAsync<LookupItemModel>(
                @"SELECT a.almacen_id AS Id, RTRIM(a.nombre) AS Descripcion 
                  FROM dbo.almacen a 
                  JOIN dbo.periodo_emp_sede pes ON pes.empresa_sede_id = a.empresa_sede_id 
                  WHERE pes.periodo_emp_sede_id = @ContextoId AND a.estado = 'A' 
                  ORDER BY a.nombre",
                new { ContextoId = contextoId })).AsList();

            model.MotivosTraslado = (await conn.QueryAsync<LookupItemModel>(
                "SELECT motivo_trasla_id AS Id, RTRIM(motivo_trasla) AS Codigo, RTRIM(nombre) AS Descripcion FROM dbo.motivo_trasla WHERE estado = 'A' ORDER BY motivo_trasla_id")).AsList();

            model.EstadosGuia = (await conn.QueryAsync<LookupItemModel>(
                "SELECT estado_guia_r_id AS Id, RTRIM(estado_guia_r) AS Codigo, RTRIM(nombre) AS Descripcion FROM dbo.estado_guia_r WHERE estado = 'A' ORDER BY estado_guia_r_id")).AsList();

            model.EstadosAprobacion = (await conn.QueryAsync<LookupItemModel>(
                "SELECT estado_aproba_id AS Id, RTRIM(estado_aproba) AS Codigo, RTRIM(nombre) AS Descripcion FROM dbo.estado_aproba WHERE estado = 'A' ORDER BY estado_aproba_id")).AsList();

            var where = new List<string>
            {
                "gr.periodo_emp_sede_id = @ContextoId",
                "gr.es_venta = 'N'",
                "gr.es_interno = @EsInternoChar"
            };

            var args = new DynamicParameters();
            args.Add("ContextoId", contextoId);
            args.Add("EsInternoChar", esInterno ? "S" : "N");

            if (almacenId.HasValue && almacenId.Value > 0)
            {
                where.Add("ma.almacen_id = @AlmacenId");
                args.Add("AlmacenId", almacenId.Value);
            }

            if (!string.IsNullOrWhiteSpace(codigo))
            {
                where.Add("(CONCAT(RTRIM(gr.serie), '-', RTRIM(gr.numero)) LIKE @Codigo OR gr.numero LIKE @Codigo)");
                args.Add("Codigo", $"%{codigo.Trim()}%");
            }

            if (fechaDesde.HasValue)
            {
                where.Add("gr.fecha >= @FechaDesde");
                args.Add("FechaDesde", fechaDesde.Value.Date);
            }

            if (fechaHasta.HasValue)
            {
                where.Add("gr.fecha <= @FechaHasta");
                args.Add("FechaHasta", fechaHasta.Value.Date.AddDays(1).AddTicks(-1));
            }

            if (estadoGuiaId.HasValue && estadoGuiaId.Value > 0)
            {
                where.Add("gr.estado_guia_r_id = @EstadoGuiaId");
                args.Add("EstadoGuiaId", estadoGuiaId.Value);
            }

            if (estadoAprobacionId.HasValue && estadoAprobacionId.Value > 0)
            {
                where.Add("gr.estado_aproba_id = @EstadoAprobacionId");
                args.Add("EstadoAprobacionId", estadoAprobacionId.Value);
            }

            if (motivoTrasladoId.HasValue && motivoTrasladoId.Value > 0)
            {
                where.Add("gr.motivo_trasla_id = @MotivoTrasladoId");
                args.Add("MotivoTrasladoId", motivoTrasladoId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                where.Add(@"(CONCAT(RTRIM(gr.serie), '-', RTRIM(gr.numero)) LIKE @Search 
                             OR p2.razon_social LIKE @Search 
                             OR p2.numero_ruc LIKE @Search 
                             OR p2.num_doc_iden LIKE @Search 
                             OR ISNULL(t.razon_social, '') LIKE @Search 
                             OR ISNULL(gr.observa, '') LIKE @Search)");
                args.Add("Search", $"%{search.Trim()}%");
            }

            string whereSql = " WHERE " + string.Join(" AND ", where);

            string countSql = $@"
                SELECT COUNT(1) 
                FROM dbo.guia_remision gr
                LEFT JOIN dbo.mov_almacen ma ON ma.mov_almacen_id = gr.mov_almacen_id
                LEFT JOIN dbo.almacen a ON a.almacen_id = ma.almacen_id
                JOIN dbo.proveedor p2 ON p2.proveedor_id = gr.proveedor2_id
                JOIN dbo.motivo_trasla mt ON mt.motivo_trasla_id = gr.motivo_trasla_id
                LEFT JOIN dbo.transpo t ON t.transpo_id = gr.transportista_id
                JOIN dbo.estado_guia_r egr ON egr.estado_guia_r_id = gr.estado_guia_r_id
                LEFT JOIN dbo.estado_aproba ea ON ea.estado_aproba_id = gr.estado_aproba_id
                {whereSql}";

            int total = await conn.ExecuteScalarAsync<int>(countSql, args);
            model.Paginar(total, page, top);

            string dataSql = $@"
                SELECT 
                    gr.guia_remision_id AS Id,
                    CONCAT(RTRIM(gr.serie), ' - ', RTRIM(gr.numero)) AS SerieNumero,
                    RTRIM(gr.serie) AS Serie,
                    RTRIM(gr.numero) AS Numero,
                    gr.fecha AS Fecha,
                    gr.fec_ini_tra AS FechaInicioTraslado,
                    ma.almacen_id AS AlmacenId,
                    RTRIM(ISNULL(a.nombre, '')) AS AlmacenNombre,
                    gr.mov_almacen_id AS MovimientoAlmacenId,
                    RTRIM(ISNULL(ma.mov_almacen, '')) AS MovimientoAlmacenCodigo,
                    gr.proveedor2_id AS ProveedorDestinoId,
                    RTRIM(p2.razon_social) AS Destinatario,
                    RTRIM(COALESCE(NULLIF(p2.numero_ruc,''), p2.num_doc_iden, '')) AS DestinatarioDocumento,
                    gr.motivo_trasla_id AS MotivoTrasladoId,
                    RTRIM(mt.nombre) AS MotivoTraslado,
                    RTRIM(ISNULL(t.razon_social, '')) AS Transportista,
                    RTRIM(ISNULL(gr.punto_partida, '')) AS PuntoPartida,
                    RTRIM(ISNULL(gr.punto_llegada, '')) AS PuntoLlegada,
                    ISNULL(gr.peso_b_total, 0) AS PesoBrutoTotal,
                    RTRIM(ISNULL(u.usuario_logon, '')) AS Usuario,
                    gr.estado_guia_r_id AS EstadoGuiaRemisionId,
                    RTRIM(egr.estado_guia_r) AS EstadoGuiaRemisionCodigo,
                    RTRIM(egr.nombre) AS EstadoGuiaRemisionNombre,
                    gr.estado_aproba_id AS EstadoAprobacionId,
                    RTRIM(ISNULL(ea.estado_aproba, '')) AS EstadoAprobacionCodigo,
                    RTRIM(ISNULL(ea.nombre, '')) AS EstadoAprobacionNombre,
                    RTRIM(ISNULL(es.estado_sunat, '')) AS EstadoSunatCodigo,
                    RTRIM(ISNULL(es.nombre, '')) AS EstadoSunatNombre,
                    gr.impreso AS Impreso,
                    gr.estado AS Estado
                FROM dbo.guia_remision gr
                LEFT JOIN dbo.mov_almacen ma ON ma.mov_almacen_id = gr.mov_almacen_id
                LEFT JOIN dbo.almacen a ON a.almacen_id = ma.almacen_id
                JOIN dbo.proveedor p2 ON p2.proveedor_id = gr.proveedor2_id
                JOIN dbo.motivo_trasla mt ON mt.motivo_trasla_id = gr.motivo_trasla_id
                LEFT JOIN dbo.transpo t ON t.transpo_id = gr.transportista_id
                JOIN dbo.estado_guia_r egr ON egr.estado_guia_r_id = gr.estado_guia_r_id
                LEFT JOIN dbo.estado_aproba ea ON ea.estado_aproba_id = gr.estado_aproba_id
                LEFT JOIN dbo.estado_sunat es ON es.estado_sunat_id = gr.estado_sunat_id
                LEFT JOIN dbo.usuario u ON u.usuario_id = gr.usuario_creacion_id
                {whereSql}
                ORDER BY {columns[model.Sort]} {model.Dir}
                OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";

            args.Add("Offset", (model.Page - 1) * model.PageSize);
            args.Add("Top", model.PageSize);

            model.Items = (await conn.QueryAsync<GuiaRemiItemModel>(dataSql, args)).AsList();
            model.VisibleCount = model.Items.Count;
            return model;
        }

        public async Task<GuiaRemiDetalleModel?> GetGuiaRemisionDetalleAsync(int empresaId, int contextoId, int id, bool esInterno = true)
        {
            using var conn = factory.CreateConnection();

            GuiaRemiDetalleModel model;

            if (id > 0)
            {
                const string sql = @"
                    SELECT 
                        gr.guia_remision_id AS Id,
                        ma.almacen_id AS AlmacenId,
                        gr.tipo_doc_com_id AS TipoDocumentoComercialId,
                        RTRIM(gr.serie) AS Serie,
                        RTRIM(gr.numero) AS Numero,
                        gr.fecha AS Fecha,
                        gr.fec_ini_tra AS FechaInicioTraslado,
                        gr.mov_almacen_id AS MovimientoAlmacenId,
                        RTRIM(ISNULL(ma.mov_almacen, '')) AS MovimientoAlmacenCodigo,
                        gr.motivo_trasla_id AS MotivoTrasladoId,
                        gr.proveedor2_id AS ProveedorDestinoId,
                        RTRIM(p2.razon_social) AS ProveedorDestinoNombre,
                        RTRIM(COALESCE(NULLIF(p2.numero_ruc,''), p2.num_doc_iden, '')) AS ProveedorDestinoDocumento,
                        gr.transportista_id AS TransportistaId,
                        gr.transp_condu_id AS TransportistaConductorId,
                        gr.transp_unid_id AS TransportistaUnidadId,
                        gr.distrito_id AS DistritoPartidaId,
                        RTRIM(ISNULL(gr.punto_partida, '')) AS PuntoPartida,
                        RTRIM(ISNULL(gr.cod_sunat_par, '')) AS CodigoSunatPartida,
                        gr.distrito_llegada_id AS DistritoLlegadaId,
                        RTRIM(ISNULL(gr.punto_llegada, '')) AS PuntoLlegada,
                        RTRIM(ISNULL(gr.cod_sunat_lle, '')) AS CodigoSunatLlegada,
                        ISNULL(gr.costo_flete, 0) AS CostoFlete,
                        ISNULL(gr.peso_b_total, 0) AS PesoBrutoTotal,
                        RTRIM(ISNULL(gr.observa, '')) AS Observacion,
                        gr.es_interno AS EsInterno,
                        gr.es_venta AS EsVenta,
                        gr.estado_guia_r_id AS EstadoGuiaRemisionId,
                        gr.estado_aproba_id AS EstadoAprobacionId,
                        gr.estado AS Estado
                    FROM dbo.guia_remision gr
                    LEFT JOIN dbo.mov_almacen ma ON ma.mov_almacen_id = gr.mov_almacen_id
                    JOIN dbo.proveedor p2 ON p2.proveedor_id = gr.proveedor2_id
                    WHERE gr.guia_remision_id = @Id AND gr.periodo_emp_sede_id = @ContextoId";

                var item = await conn.QuerySingleOrDefaultAsync<GuiaRemiDetalleModel>(sql, new { Id = id, ContextoId = contextoId });
                if (item is null) return null;
                model = item;

                // Cargar ubigeo partida
                if (model.DistritoPartidaId > 0)
                {
                    var ubigeoPartida = await conn.QuerySingleOrDefaultAsync<(int DistritoId, int ProvinciaId, int DepartamentoId)?>(
                        @"SELECT dis.distrito_id, pro.provincia_id, dep.departamento_id 
                          FROM dbo.distrito dis 
                          JOIN dbo.provincia pro ON pro.provincia_id = dis.provincia_id 
                          JOIN dbo.departamento dep ON dep.departamento_id = pro.departamento_id 
                          WHERE dis.distrito_id = @DistritoId",
                        new { DistritoId = model.DistritoPartidaId });
                    if (ubigeoPartida.HasValue)
                    {
                        model.DepartamentoPartidaId = ubigeoPartida.Value.DepartamentoId;
                        model.ProvinciaPartidaId = ubigeoPartida.Value.ProvinciaId;
                    }
                }

                // Cargar ubigeo llegada
                if (model.DistritoLlegadaId > 0)
                {
                    var ubigeoLlegada = await conn.QuerySingleOrDefaultAsync<(int DistritoId, int ProvinciaId, int DepartamentoId)?>(
                        @"SELECT dis.distrito_id, pro.provincia_id, dep.departamento_id 
                          FROM dbo.distrito dis 
                          JOIN dbo.provincia pro ON pro.provincia_id = dis.provincia_id 
                          JOIN dbo.departamento dep ON dep.departamento_id = pro.departamento_id 
                          WHERE dis.distrito_id = @DistritoId",
                        new { DistritoId = model.DistritoLlegadaId });
                    if (ubigeoLlegada.HasValue)
                    {
                        model.DepartamentoLlegadaId = ubigeoLlegada.Value.DepartamentoId;
                        model.ProvinciaLlegadaId = ubigeoLlegada.Value.ProvinciaId;
                    }
                }

                // Cargar productos
                const string prodSql = @"
                    SELECT 
                        grp.guia_r_prod_id AS Id,
                        grp.guia_remision_id AS GuiaRemisionId,
                        grp.mov_alm_prod_id AS MovimientoAlmacenProductoId,
                        p.producto_id AS ProductoId,
                        RTRIM(ISNULL(grp.cod_produ, '')) AS ProductoCodigo,
                        RTRIM(grp.descripcion) AS Descripcion,
                        RTRIM(ISNULL(um.abrev, 'UND')) AS UnidadMedida,
                        grp.cantidad AS Cantidad,
                        ISNULL(grp.peso_neto, 0) AS PesoNeto,
                        ISNULL(grp.peso_bruto, 0) AS PesoBruto,
                        grp.estado AS Estado
                    FROM dbo.guia_r_prod grp
                    LEFT JOIN dbo.mov_alm_prod map ON map.mov_alm_prod_id = grp.mov_alm_prod_id
                    LEFT JOIN dbo.producto p ON p.producto_id = map.producto_id
                    LEFT JOIN dbo.unidad_medida um ON um.unidad_medida_id = p.unidad_medida_id
                    WHERE grp.guia_remision_id = @Id AND grp.estado = 'A'
                    ORDER BY grp.guia_r_prod_id ASC";

                model.Productos = (await conn.QueryAsync<GuiaRemiProductoModel>(prodSql, new { Id = id })).AsList();
            }
            else
            {
                model = new GuiaRemiDetalleModel
                {
                    TipoDocumentoComercialId = 9,
                    Fecha = DateTime.Today,
                    FechaInicioTraslado = DateTime.Today,
                    MotivoTrasladoId = 6,
                    EsInterno = esInterno ? "S" : "N",
                    EsVenta = "N",
                    EstadoGuiaRemisionId = 1,
                    Estado = "A"
                };
            }

            // Lookups
            model.Almacenes = (await conn.QueryAsync<LookupItemModel>(
                @"SELECT a.almacen_id AS Id, RTRIM(a.nombre) AS Descripcion 
                  FROM dbo.almacen a 
                  JOIN dbo.periodo_emp_sede pes ON pes.empresa_sede_id = a.empresa_sede_id 
                  WHERE pes.periodo_emp_sede_id = @ContextoId AND a.estado = 'A' 
                  ORDER BY a.nombre",
                new { ContextoId = contextoId })).AsList();

            if (model.AlmacenId == 0 && model.Almacenes.Count > 0)
            {
                model.AlmacenId = model.Almacenes[0].Id;
            }

            if (model.AlmacenId > 0)
            {
                model.Series = await GetSeriesGuiaPorAlmacenAsync(model.AlmacenId);
                if (string.IsNullOrEmpty(model.Serie) && model.Series.Count > 0)
                {
                    model.Serie = model.Series[0].Codigo;
                }
            }

            model.MotivosTraslado = (await conn.QueryAsync<LookupItemModel>(
                "SELECT motivo_trasla_id AS Id, RTRIM(motivo_trasla) AS Codigo, RTRIM(nombre) AS Descripcion FROM dbo.motivo_trasla WHERE estado = 'A' ORDER BY motivo_trasla_id")).AsList();

            model.Transportistas = (await conn.QueryAsync<LookupItemModel>(
                "SELECT transpo_id AS Id, RTRIM(numero_ruc) AS Codigo, RTRIM(razon_social) AS Descripcion FROM dbo.transpo WHERE empresa_id = @EmpresaId AND estado = 'A' ORDER BY razon_social",
                new { EmpresaId = empresaId })).AsList();

            if (model.TransportistaId.HasValue && model.TransportistaId.Value > 0)
            {
                model.Conductores = await GetConductoresPorTransportistaAsync(model.TransportistaId.Value);
                model.Unidades = await GetUnidadesPorTransportistaAsync(model.TransportistaId.Value);
            }

            model.Departamentos = (await conn.QueryAsync<LookupItemModel>(
                "SELECT departamento_id AS Id, RTRIM(departamento) AS Codigo, RTRIM(nombre) AS Descripcion FROM dbo.departamento WHERE estado = 'A' ORDER BY nombre")).AsList();

            // Ubigeo Partida
            if (model.DepartamentoPartidaId > 0)
            {
                model.ProvinciasPartida = (await conn.QueryAsync<LookupItemModel>(
                    "SELECT provincia_id AS Id, RTRIM(provincia) AS Codigo, RTRIM(nombre) AS Descripcion FROM dbo.provincia WHERE departamento_id = @DepId AND estado = 'A' ORDER BY nombre",
                    new { DepId = model.DepartamentoPartidaId })).AsList();
            }
            if (model.ProvinciaPartidaId > 0)
            {
                model.DistritosPartida = (await conn.QueryAsync<LookupItemModel>(
                    "SELECT distrito_id AS Id, RTRIM(distrito) AS Codigo, RTRIM(nombre) AS Descripcion FROM dbo.distrito WHERE provincia_id = @ProvId AND estado = 'A' ORDER BY nombre",
                    new { ProvId = model.ProvinciaPartidaId })).AsList();
            }

            // Ubigeo Llegada
            if (model.DepartamentoLlegadaId > 0)
            {
                model.ProvinciasLlegada = (await conn.QueryAsync<LookupItemModel>(
                    "SELECT provincia_id AS Id, RTRIM(provincia) AS Codigo, RTRIM(nombre) AS Descripcion FROM dbo.provincia WHERE departamento_id = @DepId AND estado = 'A' ORDER BY nombre",
                    new { DepId = model.DepartamentoLlegadaId })).AsList();
            }
            if (model.ProvinciaLlegadaId > 0)
            {
                model.DistritosLlegada = (await conn.QueryAsync<LookupItemModel>(
                    "SELECT distrito_id AS Id, RTRIM(distrito) AS Codigo, RTRIM(nombre) AS Descripcion FROM dbo.distrito WHERE provincia_id = @ProvId AND estado = 'A' ORDER BY nombre",
                    new { ProvId = model.ProvinciaLlegadaId })).AsList();
            }

            return model;
        }

        public async Task<List<LookupItemModel>> GetSeriesGuiaPorAlmacenAsync(int almacenId)
        {
            using var conn = factory.CreateConnection();
            const string sql = @"
                SELECT dcs.doc_com_serie_id AS Id, RTRIM(dcs.serie) AS Codigo, RTRIM(dcs.serie) AS Descripcion 
                FROM dbo.almacen_serie als 
                JOIN dbo.doc_com_serie dcs ON dcs.doc_com_serie_id = als.doc_com_serie_id 
                WHERE als.almacen_id = @AlmacenId AND dcs.tipo_doc_com_id = 9 AND als.estado = 'A' AND dcs.estado = 'A'
                ORDER BY dcs.serie ASC";

            return (await conn.QueryAsync<LookupItemModel>(sql, new { AlmacenId = almacenId })).AsList();
        }

        public async Task<List<LookupItemModel>> GetConductoresPorTransportistaAsync(int transportistaId)
        {
            using var conn = factory.CreateConnection();
            const string sql = @"
                SELECT transp_condu_id AS Id, RTRIM(num_doc_iden) AS Codigo, RTRIM(CONCAT(nombres, ' ', apellido_pat, ' ', apellido_mat)) AS Descripcion 
                FROM dbo.transp_condu 
                WHERE transpo_id = @TranspoId AND estado = 'A' 
                ORDER BY apellido_pat, nombres";

            return (await conn.QueryAsync<LookupItemModel>(sql, new { TranspoId = transportistaId })).AsList();
        }

        public async Task<List<LookupItemModel>> GetUnidadesPorTransportistaAsync(int transportistaId)
        {
            using var conn = factory.CreateConnection();
            const string sql = @"
                SELECT transp_unid_id AS Id, RTRIM(placa) AS Codigo, RTRIM(CONCAT(placa, ' - ', marca, ' ', ISNULL(modelo, ''))) AS Descripcion 
                FROM dbo.transp_unid 
                WHERE transpo_id = @TranspoId AND estado = 'A' 
                ORDER BY placa";

            return (await conn.QueryAsync<LookupItemModel>(sql, new { TranspoId = transportistaId })).AsList();
        }

        public async Task<List<LookupItemModel>> BuscarMovimientosParaGuiaAsync(int empresaId, int contextoId, int almacenId, string? search = null)
        {
            using var conn = factory.CreateConnection();
            const string sql = @"
                SELECT TOP 30 
                    ma.mov_almacen_id AS Id,
                    RTRIM(ma.mov_almacen) AS Codigo,
                    CONCAT(RTRIM(ma.mov_almacen), ' | ', CONVERT(varchar(10), ma.fecha, 103), ' | ', RTRIM(p.razon_social)) AS Descripcion
                FROM dbo.mov_almacen ma
                JOIN dbo.proveedor p ON p.proveedor_id = ma.proveedor_id
                WHERE ma.periodo_emp_sede_id = @ContextoId 
                  AND ma.almacen_id = @AlmacenId 
                  AND ma.estado = 'A'
                  AND (@Search IS NULL OR ma.mov_almacen LIKE @Search OR p.razon_social LIKE @Search)
                ORDER BY ma.fecha DESC, ma.mov_almacen DESC";

            return (await conn.QueryAsync<LookupItemModel>(sql, new
            {
                ContextoId = contextoId,
                AlmacenId = almacenId,
                Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%"
            })).AsList();
        }

        public async Task<List<GuiaRemiProductoModel>> GetProductosDeMovimientoParaGuiaAsync(int movimientoAlmacenId)
        {
            using var conn = factory.CreateConnection();
            const string sql = @"
                SELECT 
                    0 AS Id,
                    0 AS GuiaRemisionId,
                    map.mov_alm_prod_id AS MovimientoAlmacenProductoId,
                    p.producto_id AS ProductoId,
                    RTRIM(p.producto) AS ProductoCodigo,
                    RTRIM(p.descripcion) AS Descripcion,
                    RTRIM(ISNULL(um.abrev, 'UND')) AS UnidadMedida,
                    map.cantidad AS Cantidad,
                    ISNULL(p.peso_neto, 0) * map.cantidad AS PesoNeto,
                    ISNULL(p.peso_bruto, 0) * map.cantidad AS PesoBruto,
                    'A' AS Estado
                FROM dbo.mov_alm_prod map
                JOIN dbo.producto p ON p.producto_id = map.producto_id
                LEFT JOIN dbo.unidad_medida um ON um.unidad_medida_id = p.unidad_medida_id
                WHERE map.mov_almacen_id = @MovimientoAlmacenId AND map.estado = 'A'
                ORDER BY map.mov_alm_prod_id ASC";

            return (await conn.QueryAsync<GuiaRemiProductoModel>(sql, new { MovimientoAlmacenId = movimientoAlmacenId })).AsList();
        }

        public async Task<(bool success, string message, int id)> SaveGuiaRemisionAsync(int empresaId, int contextoId, int userId, GuiaRemiGuardarSolicitud model)
        {
            if (string.IsNullOrWhiteSpace(model.Serie))
                return (false, "Debe seleccionar una serie válida para la guía de remisión.", model.Id);

            if (model.ProveedorDestinoId <= 0)
                return (false, "Debe especificar el destinatario de la guía.", model.Id);

            if (string.IsNullOrWhiteSpace(model.PuntoPartida) || model.DistritoPartidaId <= 0)
                return (false, "Complete los datos y ubigeo del punto de partida.", model.Id);

            if (string.IsNullOrWhiteSpace(model.PuntoLlegada) || model.DistritoLlegadaId <= 0)
                return (false, "Complete los datos y ubigeo del punto de llegada.", model.Id);

            if (model.Productos is null || model.Productos.Count == 0)
                return (false, "La guía de remisión debe incluir al menos un producto a trasladar.", model.Id);

            using var conn = factory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                int guiaRemisionId = model.Id;
                string serie = model.Serie.Trim().ToUpperInvariant();
                string numero = "";

                // Obtener datos del remitente (empresa / sede)
                int remitenteId = await conn.ExecuteScalarAsync<int>(
                    @"SELECT TOP 1 p.proveedor_id 
                      FROM dbo.periodo_emp_sede pes 
                      JOIN dbo.empresa_sede es ON es.empresa_sede_id = pes.empresa_sede_id 
                      JOIN dbo.empresa e ON e.empresa_id = es.empresa_id 
                      JOIN dbo.proveedor p ON p.num_doc_iden = e.ruc OR p.numero_ruc = e.ruc 
                      WHERE pes.periodo_emp_sede_id = @ContextoId",
                    new { ContextoId = contextoId }, tx);

                if (remitenteId <= 0)
                {
                    remitenteId = model.ProveedorDestinoId;
                }

                if (model.Id == 0)
                {
                    // Obtener correlativo de serie
                    int sedeId = await conn.ExecuteScalarAsync<int>(
                        "SELECT empresa_sede_id FROM dbo.periodo_emp_sede WHERE periodo_emp_sede_id = @ContextoId",
                        new { ContextoId = contextoId }, tx);

                    int correlativoActual = await conn.ExecuteScalarAsync<int>(
                        @"SELECT ISNULL(correlativo, 0) 
                          FROM dbo.doc_com_serie 
                          WHERE empresa_sede_id = @SedeId AND serie = @Serie AND tipo_doc_com_id = 9",
                        new { SedeId = sedeId, Serie = serie }, tx);

                    int nuevoCorrelativo = correlativoActual + 1;
                    numero = nuevoCorrelativo.ToString("D8");

                    // Actualizar correlativo de la serie
                    await conn.ExecuteAsync(
                        @"UPDATE dbo.doc_com_serie 
                          SET correlativo = @Correlativo, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() 
                          WHERE empresa_sede_id = @SedeId AND serie = @Serie AND tipo_doc_com_id = 9",
                        new { Correlativo = nuevoCorrelativo, SedeId = sedeId, Serie = serie, UserId = userId }, tx);

                    var p = new DynamicParameters();
                    p.Add("periodoEmpresaSedeId", contextoId);
                    p.Add("tipoDocumentoComercialId", 9); // Guia de Remision Remitente
                    p.Add("serie", serie);
                    p.Add("numero", numero);
                    p.Add("movimientoAlmacenId", model.MovimientoAlmacenId > 0 ? model.MovimientoAlmacenId : null);
                    p.Add("fecha", model.Fecha.Date);
                    p.Add("fechaInicioTraslado", model.FechaInicioTraslado.Date);
                    p.Add("costoFlete", model.CostoFlete);
                    p.Add("EsInterno", model.EsInterno == "N" ? "N" : "S");
                    p.Add("proveedorId", remitenteId);
                    p.Add("distritoId", model.DistritoPartidaId);
                    p.Add("puntoPartida", model.PuntoPartida.Trim().ToUpperInvariant());
                    p.Add("codigoSunatPartida", model.CodigoSunatPartida?.Trim() ?? "0000");
                    p.Add("tipoDocumentoComercialReferenciaId", null);
                    p.Add("serieDocumentoReferencia", null);
                    p.Add("numeroDocumentoReferencia", null);
                    p.Add("proveedorDestinoId", model.ProveedorDestinoId);
                    p.Add("distritoLlegadaId", model.DistritoLlegadaId);
                    p.Add("puntoLlegada", model.PuntoLlegada.Trim().ToUpperInvariant());
                    p.Add("codigoSunatLlegada", model.CodigoSunatLlegada?.Trim() ?? "0000");
                    p.Add("transportistaUnidadId", model.TransportistaUnidadId > 0 ? model.TransportistaUnidadId : null);
                    p.Add("transportistaConductorId", model.TransportistaConductorId > 0 ? model.TransportistaConductorId : null);
                    p.Add("motivoTrasladoId", model.MotivoTrasladoId);
                    p.Add("esVenta", "N");
                    p.Add("impreso", "N");
                    p.Add("pesoBrutoTotal", model.PesoBrutoTotal);
                    p.Add("esModificacion", "N");
                    p.Add("estadoAprobacionId", 1); // 1 = Pendiente
                    p.Add("estadoGuiaRemisionId", 1); // 1 = En Edicion
                    p.Add("observacion", model.Observacion?.Trim());
                    p.Add("observacionAnulacion", null);
                    p.Add("usuarioAccionId", userId);
                    p.Add("newID", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    await conn.ExecuteAsync("dbo.sp_guia_remision_insertar", p, tx, commandType: CommandType.StoredProcedure);
                    guiaRemisionId = p.Get<int>("newID");

                    // Actualizar transportista_id en la guía si fue provisto
                    if (model.TransportistaId.HasValue && model.TransportistaId.Value > 0)
                    {
                        await conn.ExecuteAsync(
                            "UPDATE dbo.guia_remision SET transportista_id = @TranspoId WHERE guia_remision_id = @Id",
                            new { TranspoId = model.TransportistaId.Value, Id = guiaRemisionId }, tx);
                    }
                }
                else
                {
                    // Actualizar cabecera existente
                    var p = new DynamicParameters();
                    p.Add("guiaRemisionId", model.Id);
                    p.Add("fechaInicioTraslado", model.FechaInicioTraslado.Date);
                    p.Add("costoFlete", model.CostoFlete);
                    p.Add("EsInterno", model.EsInterno == "N" ? "N" : "S");
                    p.Add("proveedorId", remitenteId);
                    p.Add("distritoId", model.DistritoPartidaId);
                    p.Add("puntoPartida", model.PuntoPartida.Trim().ToUpperInvariant());
                    p.Add("codigoSunatPartida", model.CodigoSunatPartida?.Trim() ?? "0000");
                    p.Add("tipoDocumentoComercialReferenciaId", null);
                    p.Add("serieDocumentoReferencia", null);
                    p.Add("numeroDocumentoReferencia", null);
                    p.Add("proveedorDestinoId", model.ProveedorDestinoId);
                    p.Add("distritoLlegadaId", model.DistritoLlegadaId);
                    p.Add("puntoLlegada", model.PuntoLlegada.Trim().ToUpperInvariant());
                    p.Add("codigoSunatLlegada", model.CodigoSunatLlegada?.Trim() ?? "0000");
                    p.Add("transportistaUnidadId", model.TransportistaUnidadId > 0 ? model.TransportistaUnidadId : null);
                    p.Add("transportistaConductorId", model.TransportistaConductorId > 0 ? model.TransportistaConductorId : null);
                    p.Add("motivoTrasladoId", model.MotivoTrasladoId);
                    p.Add("pesoBrutoTotal", model.PesoBrutoTotal);
                    p.Add("observacion", model.Observacion?.Trim());
                    p.Add("usuarioAccionId", userId);

                    await conn.ExecuteAsync("dbo.sp_guia_remision_actualizar", p, tx, commandType: CommandType.StoredProcedure);

                    if (model.TransportistaId.HasValue && model.TransportistaId.Value > 0)
                    {
                        await conn.ExecuteAsync(
                            "UPDATE dbo.guia_remision SET transportista_id = @TranspoId WHERE guia_remision_id = @Id",
                            new { TranspoId = model.TransportistaId.Value, Id = model.Id }, tx);
                    }

                    // Eliminar items previos para reinsertar actualizados
                    await conn.ExecuteAsync(
                        "dbo.sp_guia_r_prod_eliminarPorGuiaRemisionId",
                        new { guiaRemisionId = model.Id, usuarioAccionId = userId },
                        tx,
                        commandType: CommandType.StoredProcedure);
                }

                // Insertar productos
                foreach (var prod in model.Productos)
                {
                    var pProd = new DynamicParameters();
                    pProd.Add("guiaRemisionId", guiaRemisionId);
                    pProd.Add("movimientoAlmacenProductoId", prod.MovimientoAlmacenProductoId > 0 ? prod.MovimientoAlmacenProductoId : null);
                    pProd.Add("codigoProducto", string.IsNullOrWhiteSpace(prod.ProductoCodigo) ? "GENERICO" : prod.ProductoCodigo.Trim());
                    pProd.Add("descripcion", prod.Descripcion.Trim().ToUpperInvariant());
                    pProd.Add("cantidad", prod.Cantidad);
                    pProd.Add("pesoNeto", prod.PesoNeto);
                    pProd.Add("pesoBruto", prod.PesoBruto);
                    pProd.Add("usuarioAccionId", userId);
                    pProd.Add("newID", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    await conn.ExecuteAsync("dbo.sp_guia_r_prod_insertar", pProd, tx, commandType: CommandType.StoredProcedure);
                }

                tx.Commit();
                return (true, $"Guía de remisión {(model.Id == 0 ? $"{serie}-{numero}" : "actualizada")} correctamente.", guiaRemisionId);
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return (false, "Error al procesar la guía: " + ex.Message, 0);
            }
        }

        public async Task<(bool success, string message, int id)> AnularGuiaRemisionAsync(int empresaId, int contextoId, int userId, GuiaRemiAnularSolicitud model)
        {
            if (model.Id <= 0) return (false, "Identificador de guía no válido.", 0);
            if (string.IsNullOrWhiteSpace(model.MotivoAnulacion))
                return (false, "Debe ingresar el motivo de la anulación.", model.Id);

            using var conn = factory.CreateConnection();

            var p = new DynamicParameters();
            p.Add("guiaRemisionId", model.Id);
            p.Add("estadoGuiaRemisionId", 4); // 4 = Anulado
            p.Add("observacionAnulacion", model.MotivoAnulacion.Trim());
            p.Add("usuarioAccionId", userId);

            await conn.ExecuteAsync("dbo.sp_guia_remision_eliminar", p, commandType: CommandType.StoredProcedure);
            return (true, "Guía de remisión anulada correctamente.", model.Id);
        }

        public async Task<(bool success, string message, int id)> SolicitarAprobacionGuiaRemisionAsync(int empresaId, int contextoId, int userId, int id, string revision)
        {
            if (id <= 0) return (false, "Identificador inválido.", 0);

            using var conn = factory.CreateConnection();

            int rows = await conn.ExecuteAsync(
                @"UPDATE dbo.guia_remision 
                  SET estado_aproba_id = 1, fecha_modifica = GETDATE(), usuario_modifica_id = @UserId 
                  WHERE guia_remision_id = @Id AND periodo_emp_sede_id = @ContextoId AND estado = 'A'",
                new { Id = id, ContextoId = contextoId, UserId = userId });

            return rows > 0
                ? (true, "Guía de remisión enviada a aprobación.", id)
                : (false, "No se pudo actualizar el estado de la guía.", id);
        }

        public async Task<GuiaRemiAutoPageModel> GetGuiasRemisionAutoAsync(
            int empresaId,
            int contextoId,
            int? almacenId = null,
            string? codigo = null,
            string? search = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            int? motivoTrasladoId = null,
            int page = 1,
            int top = 25,
            string? sort = null,
            string? dir = null)
        {
            var columns = new Dictionary<string, string>
            {
                ["id"] = "gr.guia_remision_id",
                ["serienumero"] = "gr.serie",
                ["fecha"] = "gr.fecha",
                ["fechatraslado"] = "gr.fec_ini_tra",
                ["almacen"] = "a.nombre",
                ["destinatario"] = "p2.razon_social",
                ["motivo"] = "mt.nombre",
                ["transportista"] = "t.razon_social",
                ["estado"] = "gr.estado"
            };

            var model = new GuiaRemiAutoPageModel
            {
                AlmacenId = almacenId,
                Codigo = codigo,
                Search = search,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                MotivoTrasladoId = motivoTrasladoId
            };

            model.Ordenar(sort, dir, columns.Keys, "fecha", "desc");
            model.Paginar(0, page, top);

            using var conn = factory.CreateConnection();

            model.Almacenes = (await conn.QueryAsync<LookupItemModel>(
                @"SELECT a.almacen_id AS Id, RTRIM(a.nombre) AS Descripcion 
                  FROM dbo.almacen a 
                  JOIN dbo.periodo_emp_sede pes ON pes.empresa_sede_id = a.empresa_sede_id 
                  WHERE pes.periodo_emp_sede_id = @ContextoId AND a.estado = 'A' 
                  ORDER BY a.nombre",
                new { ContextoId = contextoId })).AsList();

            model.MotivosTraslado = (await conn.QueryAsync<LookupItemModel>(
                "SELECT motivo_trasla_id AS Id, RTRIM(motivo_trasla) AS Codigo, RTRIM(nombre) AS Descripcion FROM dbo.motivo_trasla WHERE estado = 'A' ORDER BY motivo_trasla_id")).AsList();

            var where = new List<string>
            {
                "gr.periodo_emp_sede_id = @ContextoId",
                "gr.estado_aproba_id = 1", // Solo pendientes de aprobación
                "gr.estado = 'A'"
            };

            var args = new DynamicParameters();
            args.Add("ContextoId", contextoId);

            if (almacenId.HasValue && almacenId.Value > 0)
            {
                where.Add("ma.almacen_id = @AlmacenId");
                args.Add("AlmacenId", almacenId.Value);
            }

            if (!string.IsNullOrWhiteSpace(codigo))
            {
                where.Add("(CONCAT(RTRIM(gr.serie), '-', RTRIM(gr.numero)) LIKE @Codigo OR gr.numero LIKE @Codigo)");
                args.Add("Codigo", $"%{codigo.Trim()}%");
            }

            if (fechaDesde.HasValue)
            {
                where.Add("gr.fecha >= @FechaDesde");
                args.Add("FechaDesde", fechaDesde.Value.Date);
            }

            if (fechaHasta.HasValue)
            {
                where.Add("gr.fecha <= @FechaHasta");
                args.Add("FechaHasta", fechaHasta.Value.Date.AddDays(1).AddTicks(-1));
            }

            if (motivoTrasladoId.HasValue && motivoTrasladoId.Value > 0)
            {
                where.Add("gr.motivo_trasla_id = @MotivoTrasladoId");
                args.Add("MotivoTrasladoId", motivoTrasladoId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                where.Add(@"(CONCAT(RTRIM(gr.serie), '-', RTRIM(gr.numero)) LIKE @Search 
                             OR p2.razon_social LIKE @Search 
                             OR p2.numero_ruc LIKE @Search 
                             OR p2.num_doc_iden LIKE @Search 
                             OR ISNULL(t.razon_social, '') LIKE @Search 
                             OR ISNULL(gr.observa, '') LIKE @Search)");
                args.Add("Search", $"%{search.Trim()}%");
            }

            string whereSql = " WHERE " + string.Join(" AND ", where);

            string countSql = $@"
                SELECT COUNT(1) 
                FROM dbo.guia_remision gr
                LEFT JOIN dbo.mov_almacen ma ON ma.mov_almacen_id = gr.mov_almacen_id
                LEFT JOIN dbo.almacen a ON a.almacen_id = ma.almacen_id
                JOIN dbo.proveedor p2 ON p2.proveedor_id = gr.proveedor2_id
                JOIN dbo.motivo_trasla mt ON mt.motivo_trasla_id = gr.motivo_trasla_id
                LEFT JOIN dbo.transpo t ON t.transpo_id = gr.transportista_id
                {whereSql}";

            int total = await conn.ExecuteScalarAsync<int>(countSql, args);
            model.Paginar(total, page, top);

            string dataSql = $@"
                SELECT 
                    gr.guia_remision_id AS Id,
                    CONCAT(RTRIM(gr.serie), ' - ', RTRIM(gr.numero)) AS SerieNumero,
                    RTRIM(gr.serie) AS Serie,
                    RTRIM(gr.numero) AS Numero,
                    gr.fecha AS Fecha,
                    gr.fec_ini_tra AS FechaInicioTraslado,
                    ma.almacen_id AS AlmacenId,
                    RTRIM(ISNULL(a.nombre, '')) AS AlmacenNombre,
                    gr.mov_almacen_id AS MovimientoAlmacenId,
                    RTRIM(ISNULL(ma.mov_almacen, '')) AS MovimientoAlmacenCodigo,
                    gr.proveedor2_id AS ProveedorDestinoId,
                    RTRIM(p2.razon_social) AS Destinatario,
                    RTRIM(COALESCE(NULLIF(p2.numero_ruc,''), p2.num_doc_iden, '')) AS DestinatarioDocumento,
                    gr.motivo_trasla_id AS MotivoTrasladoId,
                    RTRIM(mt.nombre) AS MotivoTraslado,
                    RTRIM(ISNULL(t.razon_social, '')) AS Transportista,
                    RTRIM(ISNULL(gr.punto_partida, '')) AS PuntoPartida,
                    RTRIM(ISNULL(gr.punto_llegada, '')) AS PuntoLlegada,
                    ISNULL(gr.peso_b_total, 0) AS PesoBrutoTotal,
                    gr.estado_guia_r_id AS EstadoGuiaRemisionId,
                    '1' AS EstadoGuiaRemisionCodigo,
                    'PENDIENTE' AS EstadoGuiaRemisionNombre,
                    gr.estado_aproba_id AS EstadoAprobacionId,
                    '1' AS EstadoAprobacionCodigo,
                    'PENDIENTE' AS EstadoAprobacionNombre,
                    gr.estado AS Estado
                FROM dbo.guia_remision gr
                LEFT JOIN dbo.mov_almacen ma ON ma.mov_almacen_id = gr.mov_almacen_id
                LEFT JOIN dbo.almacen a ON a.almacen_id = ma.almacen_id
                JOIN dbo.proveedor p2 ON p2.proveedor_id = gr.proveedor2_id
                JOIN dbo.motivo_trasla mt ON mt.motivo_trasla_id = gr.motivo_trasla_id
                LEFT JOIN dbo.transpo t ON t.transpo_id = gr.transportista_id
                {whereSql}
                ORDER BY {columns[model.Sort]} {model.Dir}
                OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";

            args.Add("Offset", (model.Page - 1) * model.PageSize);
            args.Add("Top", model.PageSize);

            model.Items = (await conn.QueryAsync<GuiaRemiItemModel>(dataSql, args)).AsList();
            model.VisibleCount = model.Items.Count;
            return model;
        }

        public async Task<(bool success, string message, int id)> AprobarGuiaRemisionAsync(int empresaId, int contextoId, int userId, AprobarGuiaRemiSolicitud model)
        {
            if (model.Id <= 0) return (false, "Identificador de guía no válido.", 0);

            using var conn = factory.CreateConnection();

            int rows = await conn.ExecuteAsync(
                @"UPDATE dbo.guia_remision 
                  SET estado_aproba_id = 2, estado_guia_r_id = 2, 
                      observa = COALESCE(@Observacion, observa), 
                      fecha_modifica = GETDATE(), usuario_modifica_id = @UserId 
                  WHERE guia_remision_id = @Id AND periodo_emp_sede_id = @ContextoId AND estado = 'A'",
                new { Id = model.Id, ContextoId = contextoId, Observacion = model.Observacion?.Trim(), UserId = userId });

            return rows > 0
                ? (true, "Guía de remisión aprobada y emitida exitosamente.", model.Id)
                : (false, "No se pudo aprobar la guía de remisión.", model.Id);
        }

        public async Task<(bool success, string message, int id)> DesaprobarGuiaRemisionAsync(int empresaId, int contextoId, int userId, DesaprobarGuiaRemiSolicitud model)
        {
            if (model.Id <= 0) return (false, "Identificador de guía no válido.", 0);
            if (string.IsNullOrWhiteSpace(model.Motivo))
                return (false, "Debe ingresar el motivo de desaprobación.", model.Id);

            using var conn = factory.CreateConnection();

            int rows = await conn.ExecuteAsync(
                @"UPDATE dbo.guia_remision 
                  SET estado_aproba_id = 3, estado_guia_r_id = 1, obs_anula = @Motivo, 
                      fecha_modifica = GETDATE(), usuario_modifica_id = @UserId 
                  WHERE guia_remision_id = @Id AND periodo_emp_sede_id = @ContextoId AND estado = 'A'",
                new { Id = model.Id, ContextoId = contextoId, Motivo = model.Motivo.Trim(), UserId = userId });

            return rows > 0
                ? (true, "Guía de remisión rechazada.", model.Id)
                : (false, "No se pudo actualizar el estado de la guía.", model.Id);
        }
    }
}
