using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class ProduccionService
{
    public async Task<RequeInsumoPageModel> GetRequerimientosInsumoAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        string? codigo = null,
        string? cotizacion = null,
        string? cliente = null,
        string? vendedor = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? estadoDespachoId = null,
        bool mostrarAnulados = false,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new RequeInsumoPageModel
        {
            Search = search,
            Codigo = codigo,
            Cotizacion = cotizacion,
            Cliente = cliente,
            Vendedor = vendedor,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            EstadoDespachoId = estadoDespachoId,
            MostrarAnulados = mostrarAnulados
        };

        var columns = new Dictionary<string, string>
        {
            ["codigo"] = "op.orden_pro",
            ["fecha"] = "op.fecha",
            ["cotizacion"] = "c.cotiza",
            ["cliente"] = "p.razon_social",
            ["vendedor"] = "pvd.razon_social",
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
            model.Errores.Add("El contexto de trabajo no está habilitado.");
            return model;
        }

        model.EstadosDespacho = (await conn.QueryAsync<LookupItemModel>(
            @"SELECT estado_despacho_id Id, RTRIM(nombre) Descripcion, RTRIM(estado_despacho) Codigo
              FROM estado_despacho
              WHERE estado = 'A' AND estado_despacho <> '02'
              ORDER BY nombre")).AsList();

        var where = new List<string> { "op.periodo_emp_sede_id = @ContextoId", "op.incluye_insu = 'S'" };
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

        if (estadoDespachoId.HasValue && estadoDespachoId.Value > 0)
        {
            where.Add("op.estado_despacho_id = @EstadoDespachoId");
            args.Add("EstadoDespachoId", estadoDespachoId.Value);
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

    public async Task<(bool success, string message)> CerrarInsumoAsync(int empresaId, int contextoId, int userId, RequeInsumoCerrarSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.OrdenProduccionId <= 0)
            return (false, "Identificador de orden inválido.");

        using var conn = factory.CreateConnection();
        if (!await ContextAsync(conn, empresaId, contextoId))
            return (false, "El contexto de trabajo no está habilitado.");

        try
        {
            await conn.ExecuteAsync("sp_orden_pro_cerrarInsumo", new
            {
                ordenProduccionId = model.OrdenProduccionId,
                estadoDespachoId = model.EstadoDespachoId,
                observacion = Text(model.Observacion),
                usuarioAccionId = userId
            }, commandType: CommandType.StoredProcedure);

            return (true, "Requerimiento de insumos atendido y cerrado exitosamente.");
        }
        catch (Exception ex)
        {
            return (false, "Error al cerrar requerimiento de insumos: " + ex.Message);
        }
    }

    public async Task<List<PersonalMaquinaItemModel>> GetPersonalMaquinasAsync(int empresaId, DateTime? fecha = null)
    {
        if (empresaId <= 0) return new();

        using var conn = factory.CreateConnection();
        var dateParam = (fecha ?? DateTime.Today).Date;

        return (await conn.QueryAsync<PersonalMaquinaItemModel>(
            @"SELECT pm.personal_maquina_id Id,
                     pm.maquina_id MaquinaId,
                     RTRIM(m.nombre) MaquinaNombre,
                     pm.personal_id PersonalId,
                     RTRIM(pr.razon_social) PersonalNombre,
                     pm.asistente_id AsistenteId,
                     RTRIM(pra.razon_social) AsistenteNombre,
                     pm.fecha Fecha
              FROM personal_maquina pm
              INNER JOIN maquina m ON m.maquina_id = pm.maquina_id
              INNER JOIN personal pe ON pe.personal_id = pm.personal_id
              INNER JOIN proveedor pr ON pr.proveedor_id = pe.proveedor_id
              LEFT JOIN personal pea ON pea.personal_id = pm.asistente_id
              LEFT JOIN proveedor pra ON pra.proveedor_id = pea.proveedor_id
              WHERE (@Fecha IS NULL OR pm.fecha = @Fecha)
              ORDER BY m.nombre, pm.fecha DESC", new { Fecha = dateParam })).AsList();
    }

    public async Task<(bool success, string message)> AsignarCortadorAsync(int empresaId, int contextoId, int userId, AsignarCortadorSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || model.OrdenProduccionId <= 0 || model.MaquinaId <= 0)
            return (false, "Parámetros de asignación incompletos.");

        using var conn = factory.CreateConnection();
        if (!await ContextAsync(conn, empresaId, contextoId))
            return (false, "El contexto de trabajo no está habilitado.");

        try
        {
            int rows = await conn.ExecuteAsync(
                @"UPDATE orden_p_s_p
                  SET personal_id = @PersonalId,
                      maquina_id = @MaquinaId,
                      maquina_real_id = @MaquinaId,
                      observa = COALESCE(NULLIF(@Observacion, ''), observa),
                      usuario_modifica_id = @UserId,
                      fecha_modifica = GETDATE()
                  WHERE orden_pro_id = @OpId AND sub_proce_p_id = @SubProcesoId",
                new
                {
                    PersonalId = model.PersonalId,
                    MaquinaId = model.MaquinaId,
                    Observacion = Text(model.Observacion),
                    UserId = userId,
                    OpId = model.OrdenProduccionId,
                    SubProcesoId = model.SubProcesoProduccionId
                });

            if (rows == 0)
            {
                // Si aún no tenía el subproceso asignado en orden_p_s_p, lo insertamos
                await conn.ExecuteAsync(
                    @"INSERT INTO orden_p_s_p (orden_pro_id, sub_proce_p_id, orden, personal_id, maquina_id, maquina_real_id, observa, estado_o_p_s_p_id, almacen_id, estado, usuario_creacion_id, fecha_creacion)
                      VALUES (@OpId, @SubProcesoId, 1, @PersonalId, @MaquinaId, @MaquinaId, @Observacion, 1, 1, 'A', @UserId, GETDATE())",
                    new
                    {
                        OpId = model.OrdenProduccionId,
                        SubProcesoId = model.SubProcesoProduccionId,
                        PersonalId = model.PersonalId,
                        MaquinaId = model.MaquinaId,
                        Observacion = Text(model.Observacion),
                        UserId = userId
                    });
            }

            return (true, "Asignación de cortador y máquina guardada correctamente.");
        }
        catch (Exception ex)
        {
            return (false, "Error al asignar cortador: " + ex.Message);
        }
    }
}

