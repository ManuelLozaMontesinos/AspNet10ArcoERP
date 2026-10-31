using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class ProduccionService
{
    private const string OrdenProduccionSelect = @"SELECT op.orden_pro_id Id,
        RTRIM(op.orden_pro) Codigo,
        op.fecha Fecha,
        op.cotiza_id CotizacionId,
        RTRIM(c.cotiza) CotizacionCodigo,
        op.proveedor_id ProveedorId,
        RTRIM(p.razon_social) ClienteRazonSocial,
        COALESCE(NULLIF(p.numero_ruc, ''), p.num_doc_iden, '') ClienteDocumento,
        RTRIM(pvd.razon_social) VendedorNombre,
        op.almacen_id AlmacenId,
        RTRIM(a.nombre) AlmacenNombre,
        op.ruta_prod_id RutaProduccionId,
        RTRIM(rp.nombre) RutaProduccionNombre,
        op.fecha_ent FechaEntrega,
        op.estado_orden_p_id EstadoOrdenProduccionId,
        RTRIM(eop.nombre) EstadoOrdenProduccionNombre,
        op.estado_plano_mel_id EstadoPlanoMelId,
        RTRIM(mel.nombre) EstadoPlanoMelNombre,
        op.estado_plano_mdf_id EstadoPlanoMdfId,
        RTRIM(mdf.nombre) EstadoPlanoMdfNombre,
        op.estado_despacho_id EstadoDespachoId,
        RTRIM(ed.nombre) EstadoDespachoNombre,
        COALESCE(op.modificaciones, 0) Modificaciones,
        op.comentarios Comentarios,
        op.observa Observacion,
        op.incluye_insu IncluyeInsumo,
        op.estado Estado,
        COALESCE(op.fecha_modifica, op.fecha_creacion) FechaRevision,
        (SELECT COUNT(1) FROM orden_p_prod opp WHERE opp.orden_pro_id = op.orden_pro_id AND opp.estado = 'A') CantidadItems,
        (SELECT COUNT(1) FROM orden_p_s_p opsp WHERE opsp.orden_pro_id = op.orden_pro_id AND opsp.estado = 'A') CantidadSubprocesos
        FROM orden_pro op {LOCK}
        LEFT JOIN cotiza c ON c.cotiza_id = op.cotiza_id
        INNER JOIN proveedor p ON p.proveedor_id = op.proveedor_id
        INNER JOIN almacen a ON a.almacen_id = op.almacen_id
        INNER JOIN estado_orden_p eop ON eop.estado_orden_p_id = op.estado_orden_p_id
        LEFT JOIN estado_despacho ed ON ed.estado_despacho_id = op.estado_despacho_id
        LEFT JOIN personal per ON per.personal_id = op.personal_id
        LEFT JOIN vendedor v ON v.vendedor_id = c.vendedor_id
        LEFT JOIN proveedor pvd ON pvd.proveedor_id = v.proveedor_id
        LEFT JOIN estado_orden_p mel ON mel.estado_orden_p_id = op.estado_plano_mel_id
        LEFT JOIN estado_orden_p mdf ON mdf.estado_orden_p_id = op.estado_plano_mdf_id
        LEFT JOIN ruta_prod rp ON rp.ruta_prod_id = op.ruta_prod_id ";

    public async Task<OrdenProduccionPageModel> GetOrdenesProduccionAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        string? codigo = null,
        string? cotizacion = null,
        string? cliente = null,
        string? vendedor = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? estadoOrdenProduccionId = null,
        bool mostrarAnulados = false,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new OrdenProduccionPageModel
        {
            Search = search,
            Codigo = codigo,
            Cotizacion = cotizacion,
            Cliente = cliente,
            Vendedor = vendedor,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            EstadoOrdenProduccionId = estadoOrdenProduccionId,
            MostrarAnulados = mostrarAnulados
        };

        var columns = new Dictionary<string, string>
        {
            ["codigo"] = "op.orden_pro",
            ["fecha"] = "op.fecha",
            ["cotizacion"] = "c.cotiza",
            ["cliente"] = "p.razon_social",
            ["vendedor"] = "pvd.razon_social",
            ["fechaentrega"] = "op.fecha_ent",
            ["estado"] = "eop.nombre"
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
            model.Errores.Add("El contexto de trabajo no está habilitado.");
            return model;
        }

        model.Estados = (await conn.QueryAsync<LookupItemModel>(
            "SELECT estado_orden_p_id Id, RTRIM(nombre) Descripcion, RTRIM(estado_orden_p) Codigo FROM estado_orden_p WHERE estado = 'A' ORDER BY nombre")).AsList();

        var where = new List<string> { "op.periodo_emp_sede_id = @ContextoId" };
        var args = new DynamicParameters();
        args.Add("ContextoId", contextoId);

        if (!mostrarAnulados)
        {
            where.Add("op.estado = 'A'");
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(op.orden_pro LIKE @Search OR p.razon_social LIKE @Search OR c.cotiza LIKE @Search)");
            args.Add("Search", Search(search));
        }

        if (!string.IsNullOrWhiteSpace(codigo))
        {
            where.Add("op.orden_pro LIKE @Codigo");
            args.Add("Codigo", Search(codigo));
        }

        if (!string.IsNullOrWhiteSpace(cotizacion))
        {
            where.Add("c.cotiza LIKE @Cotizacion");
            args.Add("Cotizacion", Search(cotizacion));
        }

        if (!string.IsNullOrWhiteSpace(cliente))
        {
            where.Add("p.razon_social LIKE @Cliente");
            args.Add("Cliente", Search(cliente));
        }

        if (!string.IsNullOrWhiteSpace(vendedor))
        {
            where.Add("pvd.razon_social LIKE @Vendedor");
            args.Add("Vendedor", Search(vendedor));
        }

        if (fechaDesde.HasValue)
        {
            where.Add("op.fecha >= @FechaDesde");
            args.Add("FechaDesde", fechaDesde.Value.Date);
        }

        if (fechaHasta.HasValue)
        {
            where.Add("op.fecha <= @FechaHasta");
            args.Add("FechaHasta", fechaHasta.Value.Date.AddDays(1).AddTicks(-1));
        }

        if (estadoOrdenProduccionId.HasValue && estadoOrdenProduccionId.Value > 0)
        {
            where.Add("op.estado_orden_p_id = @EstadoId");
            args.Add("EstadoId", estadoOrdenProduccionId.Value);
        }

        string from = " FROM orden_pro op " +
                      "LEFT JOIN cotiza c ON c.cotiza_id = op.cotiza_id " +
                      "INNER JOIN proveedor p ON p.proveedor_id = op.proveedor_id " +
                      "INNER JOIN almacen a ON a.almacen_id = op.almacen_id " +
                      "INNER JOIN estado_orden_p eop ON eop.estado_orden_p_id = op.estado_orden_p_id " +
                      "LEFT JOIN estado_despacho ed ON ed.estado_despacho_id = op.estado_despacho_id " +
                      "LEFT JOIN personal per ON per.personal_id = op.personal_id " +
                      "LEFT JOIN vendedor v ON v.vendedor_id = c.vendedor_id " +
                      "LEFT JOIN proveedor pvd ON pvd.proveedor_id = v.proveedor_id " +
                      "LEFT JOIN estado_orden_p mel ON mel.estado_orden_p_id = op.estado_plano_mel_id " +
                      "LEFT JOIN estado_orden_p mdf ON mdf.estado_orden_p_id = op.estado_plano_mdf_id " +
                      "LEFT JOIN ruta_prod rp ON rp.ruta_prod_id = op.ruta_prod_id " +
                      "WHERE " + string.Join(" AND ", where);

        int total = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*)" + from, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        string sql = Locked(OrdenProduccionSelect, null) +
                     "WHERE " + string.Join(" AND ", where) +
                     $" ORDER BY {columns[model.Sort]} {model.Dir}, op.orden_pro DESC " +
                     "OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";

        var rows = (await conn.QueryAsync<OrdenProduccionItemModel>(sql, args)).AsList();
        model.Items = rows;
        model.VisibleCount = rows.Count;
        return model;
    }

    public async Task<OrdenProduccionDetalleModel?> GetOrdenProduccionDetalleAsync(int empresaId, int contextoId, int id)
    {
        if (empresaId <= 0 || contextoId <= 0) return null;

        using var conn = factory.CreateConnection();
        if (!await ContextAsync(conn, empresaId, contextoId)) return null;

        int sedeId = await conn.ExecuteScalarAsync<int>(
            "SELECT empresa_sede_id FROM periodo_emp_sede WHERE periodo_emp_sede_id = @ContextoId",
            new { ContextoId = contextoId });

        var detalle = new OrdenProduccionDetalleModel();

        // Combos
        detalle.Almacenes = (await conn.QueryAsync<LookupItemModel>(
            @"SELECT a.almacen_id Id, RTRIM(a.nombre) Descripcion, RTRIM(a.almacen) Codigo
              FROM almacen a
              WHERE a.empresa_sede_id = @SedeId AND a.estado = 'A'
              ORDER BY a.nombre", new { SedeId = sedeId })).AsList();

        detalle.Rutas = (await conn.QueryAsync<LookupItemModel>(
            @"SELECT rp.ruta_prod_id Id, RTRIM(rp.nombre) Descripcion, '' Codigo
              FROM ruta_prod rp
              WHERE rp.estado = 'A'
              ORDER BY rp.nombre")).AsList();

        detalle.Estados = (await conn.QueryAsync<LookupItemModel>(
            @"SELECT eop.estado_orden_p_id Id, RTRIM(eop.nombre) Descripcion, RTRIM(eop.estado_orden_p) Codigo
              FROM estado_orden_p eop
              WHERE eop.estado = 'A'
              ORDER BY eop.nombre")).AsList();

        detalle.EstadosPlanoMel = detalle.Estados;
        detalle.EstadosPlanoMdf = detalle.Estados;

        detalle.Maquinas = (await conn.QueryAsync<LookupItemModel>(
            @"SELECT m.maquina_id Id, RTRIM(m.nombre) Descripcion, RTRIM(m.codigo) Codigo
              FROM maquina m
              WHERE m.estado = 'A'
              ORDER BY m.nombre")).AsList();

        detalle.Personal = (await conn.QueryAsync<LookupItemModel>(
            @"SELECT pe.personal_id Id, RTRIM(pr.razon_social) Descripcion, RTRIM(pe.personal) Codigo
              FROM personal pe
              INNER JOIN proveedor pr ON pr.proveedor_id = pe.proveedor_id
              WHERE pe.empresa_sede_id = @SedeId AND pe.estado = 'A'
              ORDER BY pr.razon_social", new { SedeId = sedeId })).AsList();

        if (id > 0)
        {
            var op = await conn.QuerySingleOrDefaultAsync<OrdenProduccionItemModel>(
                Locked(OrdenProduccionSelect, null) + " WHERE op.orden_pro_id = @Id", new { Id = id });

            if (op == null) return null;
            detalle.Cabecera = op;

            // Productos
            detalle.Productos = (await conn.QueryAsync<OrdenProduccionProductoModel>(
                @"SELECT opp.orden_p_prod_id Id,
                         opp.orden_pro_id OrdenProduccionId,
                         opp.producto_id ProductoId,
                         RTRIM(p.producto) ProductoCodigo,
                         RTRIM(p.descripcion) ProductoDescripcion,
                         opp.descripcion Descripcion,
                         opp.cantidad Cantidad,
                         opp.cant_entrega CantidadEntregada,
                         opp.fecha_entre FechaEntrega,
                         RTRIM(opp.entrega_a) EntregadoA,
                         RTRIM(um.abrev) UnidadMedida,
                         RTRIM(ed.nombre) Insumo,
                         opp.estado Estado
                  FROM orden_p_prod opp
                  INNER JOIN producto p ON p.producto_id = opp.producto_id
                  INNER JOIN unidad_medida um ON um.unidad_medida_id = p.unidad_medida_id
                  LEFT JOIN estado_despacho ed ON ed.estado_despacho_id = opp.estado_despacho_id
                  WHERE opp.orden_pro_id = @Id
                  ORDER BY opp.orden_p_prod_id", new { Id = id })).AsList();

            // Subprocesos
            detalle.Subprocesos = (await conn.QueryAsync<OrdenProduccionSubprocesoModel>(
                @"SELECT opsp.orden_p_s_p_id Id,
                         opsp.orden_pro_id OrdenProduccionId,
                         opsp.sub_proce_p_id SubProcesoProduccionId,
                         RTRIM(pp.nombre) + ' / ' + RTRIM(spp.nombre) SubProcesoNombre,
                         opsp.orden Orden,
                         opsp.fecha_ini FechaInicio,
                         opsp.fecha_fin FechaFin,
                         opsp.personal_id PersonalId,
                         RTRIM(pr.razon_social) PersonalNombre,
                         opsp.maquina_id MaquinaId,
                         RTRIM(m.nombre) MaquinaNombre,
                         opsp.maquina_real_id MaquinaRealId,
                         RTRIM(mr.nombre) MaquinaRealNombre,
                         RTRIM(opsp.observa) Observacion,
                         opsp.estado_o_p_s_p_id EstadoSubProcesoId,
                         RTRIM(eopsp.nombre) EstadoSubProcesoNombre,
                         opsp.estado Estado
                  FROM orden_p_s_p opsp
                  INNER JOIN sub_proce_p spp ON spp.sub_proce_p_id = opsp.sub_proce_p_id
                  INNER JOIN proce_prod pp ON pp.proce_prod_id = spp.proce_prod_id
                  LEFT JOIN estado_o_p_s_p eopsp ON eopsp.estado_o_p_s_p_id = opsp.estado_o_p_s_p_id
                  LEFT JOIN personal per ON per.personal_id = opsp.personal_id
                  LEFT JOIN proveedor pr ON pr.proveedor_id = per.proveedor_id
                  LEFT JOIN maquina m ON m.maquina_id = opsp.maquina_id
                  LEFT JOIN maquina mr ON mr.maquina_id = opsp.maquina_real_id
                  WHERE opsp.orden_pro_id = @Id
                  ORDER BY opsp.orden", new { Id = id })).AsList();
        }
        else
        {
            detalle.Cabecera = new OrdenProduccionItemModel
            {
                Fecha = DateTime.Today,
                FechaEntrega = DateTime.Today.AddDays(2),
                EstadoOrdenProduccionId = 1,
                EstadoOrdenProduccionNombre = "PENDIENTE",
                AlmacenId = detalle.Almacenes.FirstOrDefault()?.Id ?? 0
            };
        }

        return detalle;
    }

    public async Task<(bool success, string message, int id)> SaveOrdenProduccionAsync(int empresaId, int contextoId, int userId, OrdenProduccionGuardarSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || userId <= 0)
            return (false, "Contexto de usuario o empresa inválido.", 0);

        if (model.ProveedorId <= 0)
            return (false, "Debe seleccionar un cliente/proveedor válido.", 0);

        using var conn = factory.CreateConnection();
        if (!await ContextAsync(conn, empresaId, contextoId))
            return (false, "El contexto de trabajo no está habilitado.", 0);

        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            if (model.Id > 0)
            {
                // Actualizar OP existente
                var existing = await conn.QuerySingleOrDefaultAsync<OrdenProduccionItemModel>(
                    Locked(OrdenProduccionSelect, tx) + " WHERE op.orden_pro_id = @Id", new { Id = model.Id }, tx);

                if (existing == null)
                {
                    tx.Rollback();
                    return (false, "La orden de producción no existe.", 0);
                }

                if (!string.IsNullOrEmpty(model.Revision) && existing.Revision != model.Revision)
                {
                    tx.Rollback();
                    return (false, "El registro fue modificado por otro usuario.", 0);
                }

                await conn.ExecuteAsync("sp_orden_pro_actualizar", new
                {
                    ordenProduccionId = model.Id,
                    fecha = model.Fecha,
                    proveedorId = model.ProveedorId,
                    observacion = Text(model.Observacion),
                    fechaEntrega = model.FechaEntrega ?? DateTime.Today.AddDays(2),
                    usuarioAccionId = userId
                }, tx, commandType: CommandType.StoredProcedure);

                tx.Commit();
                return (true, $"Orden de producción {existing.Codigo} actualizada correctamente.", model.Id);
            }
            else
            {
                // Correlativo Tabla 25 (ORDEN DE PRODUCCION)
                var counter = await conn.QuerySingleOrDefaultAsync<int?>(@"SELECT correlativo FROM periodo_emp_corre WITH (UPDLOCK,HOLDLOCK)
                    WHERE periodo_emp_sede_id = @ContextoId AND tabla_id = 25", new { ContextoId = contextoId }, tx);

                int nextVal = (counter ?? 0) + 1;
                // Year prefix (last 2 digits) + 5 digits sequential
                string yearPrefix = model.Fecha.ToString("yy");
                string code = $"{yearPrefix}{nextVal:D5}";

                if (counter is null)
                {
                    await InsertAsync(conn, tx, "sp_periodo_emp_corre_insertar", new
                    {
                        PeriodoEmpresaSedeId = contextoId,
                        TablaId = 25,
                        Correlativo = nextVal,
                        UsuarioAccionId = userId
                    });
                }
                else
                {
                    int correId = await conn.ExecuteScalarAsync<int>(
                        "SELECT periodo_emp_corre_id FROM periodo_emp_corre WHERE periodo_emp_sede_id = @ContextoId AND tabla_id = 25",
                        new { ContextoId = contextoId }, tx);

                    await conn.ExecuteAsync("sp_periodo_emp_corre_actualizar", new
                    {
                        PeriodoEmpresaCorrelativoId = correId,
                        Correlativo = nextVal,
                        UsuarioAccionId = userId
                    }, tx, commandType: CommandType.StoredProcedure);
                }

                int newId = await InsertAsync(conn, tx, "sp_orden_pro_insertar", new
                {
                    periodoEmpresaSedeId = contextoId,
                    ordenProduccion = code,
                    cotizacionId = model.CotizacionId,
                    almacenId = model.AlmacenId,
                    fecha = model.Fecha,
                    proveedorId = model.ProveedorId,
                    observacion = Text(model.Observacion),
                    rutaProduccionId = model.RutaProduccionId ?? 0,
                    fechaEntrega = model.FechaEntrega ?? DateTime.Today.AddDays(2),
                    estadoOrdenProduccionId = 1, // PENDIENTE
                    incluyeInsumo = "N",
                    estadoDespachoId = (int?)null,
                    observacionDespacho = "",
                    usuarioAccionId = userId
                });

                // Si seleccionó una ruta de producción, cargar automáticamente sus subprocesos
                if (model.RutaProduccionId.HasValue && model.RutaProduccionId.Value > 0)
                {
                    var subprocesosRuta = (await conn.QueryAsync<(int SubProcesoId, int Orden)>(
                        "SELECT sub_proce_p_id, orden FROM ruta_p_s_p_p WHERE ruta_prod_id = @RutaId AND estado = 'A' ORDER BY orden",
                        new { RutaId = model.RutaProduccionId.Value }, tx)).AsList();

                    foreach (var s in subprocesosRuta)
                    {
                        await InsertAsync(conn, tx, "sp_orden_p_s_p_insertar", new
                        {
                            ordenProduccionId = newId,
                            subProcesoProduccionId = s.SubProcesoId,
                            fechaInicio = (DateTime?)null,
                            fechaFin = (DateTime?)null,
                            orden = s.Orden,
                            personalId = (int?)null,
                            subProcesoProduccionAnteriorId = (int?)null,
                            subProcesoProduccionAnteriorConcluido = "N",
                            observacion = "",
                            estadoOrdenProduccionSubProcesoId = 1,
                            almacenId = model.AlmacenId,
                            usuarioAccionId = userId
                        });
                    }
                }

                tx.Commit();
                return (true, $"Orden de producción {code} generada exitosamente.", newId);
            }
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, "Error al guardar la orden de producción: " + ex.Message, 0);
        }
    }

    public async Task<(bool success, string message)> AnularOrdenProduccionAsync(int empresaId, int contextoId, int userId, int id)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0)
            return (false, "Parámetros inválidos para anular la orden de producción.");

        using var conn = factory.CreateConnection();
        if (!await ContextAsync(conn, empresaId, contextoId))
            return (false, "El contexto de trabajo no está habilitado.");

        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var existing = await conn.QuerySingleOrDefaultAsync<OrdenProduccionItemModel>(
                Locked(OrdenProduccionSelect, tx) + " WHERE op.orden_pro_id = @Id", new { Id = id }, tx);

            if (existing == null)
            {
                tx.Rollback();
                return (false, "La orden de producción no existe.");
            }

            await conn.ExecuteAsync("sp_orden_pro_eliminar", new
            {
                ordenProduccionId = id,
                usuarioAccionId = userId
            }, tx, commandType: CommandType.StoredProcedure);

            // Si estaba vinculada a una cotización, revertir enviado a producción
            if (existing.CotizacionId.HasValue && existing.CotizacionId.Value > 0)
            {
                await conn.ExecuteAsync(
                    "UPDATE cotiza SET enviado_produ = 'N', usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE cotiza_id = @CotizaId",
                    new { CotizaId = existing.CotizacionId.Value, UserId = userId }, tx);
            }

            tx.Commit();
            return (true, $"Orden de producción {existing.Codigo} anulada correctamente.");
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, "Error al anular la orden de producción: " + ex.Message);
        }
    }

    public async Task<(bool success, string message)> CambiarEstadoOrdenProduccionAsync(int empresaId, int contextoId, int userId, OrdenProduccionCambiarEstadoSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.OrdenProduccionId <= 0)
            return (false, "Parámetros inválidos.");

        using var conn = factory.CreateConnection();
        if (!await ContextAsync(conn, empresaId, contextoId))
            return (false, "El contexto de trabajo no está habilitado.");

        try
        {
            await conn.ExecuteAsync("sp_orden_pro_cambiarEstado", new
            {
                ordenProduccionId = model.OrdenProduccionId,
                estadoplanoMelId = model.EstadoPlanoMelId,
                estadoplanoMdfId = model.EstadoPlanoMdfId,
                comentarios = Text(model.Comentarios)
            }, commandType: CommandType.StoredProcedure);

            if (model.Modificaciones.HasValue)
            {
                await conn.ExecuteAsync(
                    "UPDATE orden_pro SET modificaciones = @Mod, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE orden_pro_id = @Id",
                    new { Mod = model.Modificaciones.Value, UserId = userId, Id = model.OrdenProduccionId });
            }

            return (true, "Estado de orden de producción actualizado correctamente.");
        }
        catch (Exception ex)
        {
            return (false, "Error al actualizar estado: " + ex.Message);
        }
    }

    public async Task<(bool success, string message, int id)> SaveOrdenProduccionProductoAsync(int empresaId, int contextoId, int userId, OrdenProduccionProductoGuardarSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.OrdenProduccionId <= 0 || model.ProductoId <= 0)
            return (false, "Seleccione una orden y producto válidos.", 0);

        if (model.Cantidad <= 0)
            return (false, "La cantidad debe ser mayor a 0.", 0);

        using var conn = factory.CreateConnection();
        if (!await ContextAsync(conn, empresaId, contextoId))
            return (false, "El contexto de trabajo no está habilitado.", 0);

        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            if (model.Id > 0)
            {
                await conn.ExecuteAsync("sp_orden_p_prod_actualizar", new
                {
                    ordenProduccionProductoId = model.Id,
                    descripcion = Text(model.Descripcion),
                    cantidad = model.Cantidad,
                    usuarioAccionId = userId
                }, tx, commandType: CommandType.StoredProcedure);

                tx.Commit();
                return (true, "Producto actualizado correctamente en la orden de producción.", model.Id);
            }
            else
            {
                int newId = await InsertAsync(conn, tx, "sp_orden_p_prod_insertar", new
                {
                    ordenProduccionId = model.OrdenProduccionId,
                    productoId = model.ProductoId,
                    descripcion = Text(model.Descripcion),
                    cantidad = model.Cantidad,
                    cantidadEntregada = model.CantidadEntregada,
                    entregadoA = Text(model.EntregadoA),
                    usuarioAccionId = userId,
                    registroReferencialId = (int?)null
                });

                tx.Commit();
                return (true, "Producto agregado exitosamente a la orden de producción.", newId);
            }
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, "Error al guardar producto: " + ex.Message, 0);
        }
    }

    public async Task<(bool success, string message)> AnularOrdenProduccionProductoAsync(int empresaId, int contextoId, int userId, int productoItemId)
    {
        if (empresaId <= 0 || contextoId <= 0 || productoItemId <= 0)
            return (false, "Identificador de producto inválido.");

        using var conn = factory.CreateConnection();
        if (!await ContextAsync(conn, empresaId, contextoId))
            return (false, "El contexto de trabajo no está habilitado.");

        try
        {
            await conn.ExecuteAsync("sp_orden_p_prod_eliminar", new
            {
                ordenProduccionProductoId = productoItemId,
                usuarioAccionId = userId
            }, commandType: CommandType.StoredProcedure);

            return (true, "Producto retirado de la orden de producción.");
        }
        catch (Exception ex)
        {
            return (false, "Error al retirar producto: " + ex.Message);
        }
    }

    public async Task<(bool success, string message, int id)> SaveOrdenProduccionSubprocesoAsync(int empresaId, int contextoId, int userId, OrdenProduccionSubprocesoGuardarSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.OrdenProduccionId <= 0 || model.SubProcesoProduccionId <= 0)
            return (false, "Seleccione una orden y subproceso válidos.", 0);

        using var conn = factory.CreateConnection();
        if (!await ContextAsync(conn, empresaId, contextoId))
            return (false, "El contexto de trabajo no está habilitado.", 0);

        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            if (model.Id > 0)
            {
                await conn.ExecuteAsync("sp_orden_p_s_p_actualizar", new
                {
                    ordenProduccionSubProcesoId = model.Id,
                    personalId = model.PersonalId,
                    observacion = Text(model.Observacion),
                    fechaIni = model.FechaInicio,
                    fechaFin = model.FechaFin,
                    maquinaRealId = model.MaquinaRealId,
                    usuarioAccionId = userId
                }, tx, commandType: CommandType.StoredProcedure);

                if (model.MaquinaId.HasValue && model.MaquinaId.Value > 0)
                {
                    await conn.ExecuteAsync(
                        "UPDATE orden_p_s_p SET maquina_id = @MaquinaId WHERE orden_p_s_p_id = @Id",
                        new { MaquinaId = model.MaquinaId.Value, Id = model.Id }, tx);
                }

                tx.Commit();
                return (true, "Subproceso de producción actualizado correctamente.", model.Id);
            }
            else
            {
                int maxOrden = await conn.ExecuteScalarAsync<int>(
                    "SELECT COALESCE(MAX(orden), 0) + 1 FROM orden_p_s_p WHERE orden_pro_id = @OpId",
                    new { OpId = model.OrdenProduccionId }, tx);

                int opAlmacenId = await conn.ExecuteScalarAsync<int>(
                    "SELECT COALESCE(almacen_id, 1) FROM orden_pro WHERE orden_pro_id = @OpId",
                    new { OpId = model.OrdenProduccionId }, tx);

                int newId = await InsertAsync(conn, tx, "sp_orden_p_s_p_insertar", new
                {
                    ordenProduccionId = model.OrdenProduccionId,
                    subProcesoProduccionId = model.SubProcesoProduccionId,
                    fechaInicio = model.FechaInicio,
                    fechaFin = model.FechaFin,
                    orden = maxOrden,
                    personalId = model.PersonalId,
                    subProcesoProduccionAnteriorId = (int?)null,
                    subProcesoProduccionAnteriorConcluido = "N",
                    observacion = Text(model.Observacion),
                    estadoOrdenProduccionSubProcesoId = 1,
                    almacenId = opAlmacenId,
                    usuarioAccionId = userId
                });

                if (model.MaquinaId.HasValue && model.MaquinaId.Value > 0)
                {
                    await conn.ExecuteAsync(
                        "UPDATE orden_p_s_p SET maquina_id = @MaquinaId WHERE orden_p_s_p_id = @Id",
                        new { MaquinaId = model.MaquinaId.Value, Id = newId }, tx);
                }

                tx.Commit();
                return (true, "Subproceso agregado exitosamente a la orden de producción.", newId);
            }
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, "Error al guardar subproceso: " + ex.Message, 0);
        }
    }

    public async Task<(bool success, string message)> AnularOrdenProduccionSubprocesoAsync(int empresaId, int contextoId, int userId, int subprocesoItemId)
    {
        if (empresaId <= 0 || contextoId <= 0 || subprocesoItemId <= 0)
            return (false, "Identificador de subproceso inválido.");

        using var conn = factory.CreateConnection();
        if (!await ContextAsync(conn, empresaId, contextoId))
            return (false, "El contexto de trabajo no está habilitado.");

        try
        {
            await conn.ExecuteAsync("sp_orden_p_s_p_eliminar", new
            {
                ordenProduccionSubProcesoId = subprocesoItemId,
                usuarioAccionId = userId
            }, commandType: CommandType.StoredProcedure);

            return (true, "Subproceso retirado de la orden de producción.");
        }
        catch (Exception ex)
        {
            return (false, "Error al retirar subproceso: " + ex.Message);
        }
    }
}
