using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class ProduccionService
{
    private const string RutaSelect = @"SELECT rp.ruta_prod_id Id,
        RTRIM(rp.nombre) Nombre,
        rp.estado Estado,
        COALESCE(rp.fecha_modifica, rp.fecha_creacion) FechaRevision,
        (SELECT COUNT(1) FROM ruta_p_s_p_p rpsp WHERE rpsp.ruta_prod_id = rp.ruta_prod_id AND rpsp.estado = 'A') CantidadSubprocesos
        FROM ruta_prod rp {LOCK} ";

    public async Task<RutaProduccionPageModel> GetRutasProduccionAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        string? estado = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new RutaProduccionPageModel
        {
            Search = search,
            Estado = State(estado)
        };

        var columns = new Dictionary<string, string>
        {
            ["nombre"] = "rp.nombre",
            ["estado"] = "rp.estado",
            ["items"] = "CantidadSubprocesos",
            ["id"] = "rp.ruta_prod_id"
        };
        model.Ordenar(sort, dir, columns.Keys, "nombre");
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

        var where = new List<string>();
        var args = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("rp.nombre LIKE @Search");
            args.Add("Search", $"%{search.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(estado) && (estado == "A" || estado == "I"))
        {
            where.Add("rp.estado = @Estado");
            args.Add("Estado", estado);
        }

        string whereSql = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "";

        string countSql = "SELECT COUNT(1) FROM ruta_prod rp " + whereSql;
        int total = await conn.ExecuteScalarAsync<int>(countSql, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        string dataSql = RutaSelect.Replace("{LOCK}", "") + whereSql +
                         $" ORDER BY {columns[model.Sort]} {model.Dir}, rp.ruta_prod_id DESC " +
                         " OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";

        model.Items = (await conn.QueryAsync<RutaProduccionItemModel>(dataSql, args)).AsList();
        model.VisibleCount = model.Items.Count;
        return model;
    }

    public async Task<RutaProduccionDetalleModel?> GetRutaProduccionDetalleAsync(int empresaId, int id)
    {
        if (empresaId <= 0) return null;
        using var conn = factory.CreateConnection();

        var cabecera = await conn.QuerySingleOrDefaultAsync<RutaProduccionItemModel>(
            RutaSelect.Replace("{LOCK}", "") + " WHERE rp.ruta_prod_id = @Id", new { Id = id });

        if (cabecera is null) return null;

        var subprocesos = (await conn.QueryAsync<RutaSubProcesoItemModel>(@"
            SELECT rpsp.ruta_p_s_p_p_id Id,
                   rpsp.ruta_prod_id RutaProduccionId,
                   rpsp.sub_proce_p_id SubProcesoProduccionId,
                   RTRIM(sp.nombre) SubProcesoNombre,
                   sp.proce_prod_id ProcesoProduccionId,
                   RTRIM(pp.nombre) ProcesoNombre,
                   rpsp.orden Orden,
                   rpsp.estado Estado
            FROM ruta_p_s_p_p rpsp
            JOIN sub_proce_p sp ON sp.sub_proce_p_id = rpsp.sub_proce_p_id
            JOIN proce_prod pp ON pp.proce_prod_id = sp.proce_prod_id
            WHERE rpsp.ruta_prod_id = @Id AND rpsp.estado = 'A'
            ORDER BY rpsp.orden ASC, rpsp.ruta_p_s_p_p_id ASC",
            new { Id = id })).AsList();

        var disponibles = await GetSubprocesosDisponiblesAsync(empresaId, id);

        return new RutaProduccionDetalleModel
        {
            Cabecera = cabecera,
            Subprocesos = subprocesos,
            SubprocesosDisponibles = disponibles
        };
    }

    public async Task<List<LookupItemModel>> GetSubprocesosDisponiblesAsync(int empresaId, int rutaProduccionId)
    {
        if (empresaId <= 0) return new();
        using var conn = factory.CreateConnection();

        return (await conn.QueryAsync<LookupItemModel>(@"
            SELECT sp.sub_proce_p_id Id,
                   RTRIM(pp.nombre) Codigo,
                   CONCAT(RTRIM(pp.nombre), ' - ', RTRIM(sp.nombre)) Nombre
            FROM sub_proce_p sp
            JOIN proce_prod pp ON pp.proce_prod_id = sp.proce_prod_id
            WHERE sp.estado = 'A' AND pp.estado = 'A'
              AND NOT EXISTS (
                  SELECT 1 FROM ruta_p_s_p_p rpsp
                  WHERE rpsp.ruta_prod_id = @RutaProduccionId
                    AND rpsp.sub_proce_p_id = sp.sub_proce_p_id
                    AND rpsp.estado = 'A'
              )
            ORDER BY pp.nombre, sp.nombre",
            new { RutaProduccionId = rutaProduccionId })).AsList();
    }

    public async Task<(bool success, string message, int id)> SaveRutaProduccionAsync(
        int empresaId,
        int contextoId,
        int userId,
        RutaProduccionGuardarSolicitud model)
    {
        if (empresaId <= 0 || contextoId <= 0 || userId <= 0 || model is null || string.IsNullOrWhiteSpace(model.Nombre))
            return (false, "Complete el nombre de la ruta de producción.", 0);

        string nombreTrim = model.Nombre.Trim().ToUpperInvariant();
        if (nombreTrim.Length > 100)
            return (false, "El nombre de la ruta no puede superar los 100 caracteres.", 0);

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId) || !await ContextAsync(conn, empresaId, contextoId, tx))
            return (false, "El contexto de trabajo cambió o no está disponible.", 0);

        if (model.Id > 0)
        {
            var old = await conn.QuerySingleOrDefaultAsync<RutaProduccionItemModel>(
                RutaSelect.Replace("{LOCK}", "WITH (UPDLOCK, HOLDLOCK)") + " WHERE rp.ruta_prod_id = @Id",
                new { Id = model.Id }, tx);

            if (old is null || old.Estado != "A" || (!string.IsNullOrEmpty(model.Revision) && old.Revision != model.Revision))
                return (false, "La ruta de producción no se encuentra disponible o fue modificada por otro usuario.", model.Id);

            // Validar unicidad de nombre excluyendo la actual
            int duplicate = await conn.ExecuteScalarAsync<int>(@"
                SELECT COUNT(1) FROM ruta_prod
                WHERE nombre = @Nombre AND ruta_prod_id <> @Id AND estado = 'A'",
                new { Nombre = nombreTrim, Id = model.Id }, tx);

            if (duplicate > 0)
                return (false, $"Ya existe una ruta de producción con el nombre '{nombreTrim}'.", model.Id);

            await conn.ExecuteAsync("sp_ruta_prod_actualizar", new
            {
                rutaProduccionId = model.Id,
                nombre = nombreTrim,
                usuarioAccionId = userId
            }, tx, commandType: CommandType.StoredProcedure);

            tx.Commit();
            return (true, $"Ruta de producción '{nombreTrim}' actualizada correctamente.", model.Id);
        }
        else
        {
            // Validar duplicado
            int duplicate = await conn.ExecuteScalarAsync<int>(@"
                SELECT COUNT(1) FROM ruta_prod
                WHERE nombre = @Nombre AND estado = 'A'",
                new { Nombre = nombreTrim }, tx);

            if (duplicate > 0)
                return (false, $"Ya existe una ruta de producción con el nombre '{nombreTrim}'.", 0);

            var p = new DynamicParameters();
            p.Add("@nombre", nombreTrim);
            p.Add("@usuarioAccionId", userId);
            p.Add("@newID", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("sp_ruta_prod_insertar", p, tx, commandType: CommandType.StoredProcedure);
            int newId = p.Get<int>("@newID");

            tx.Commit();
            return (true, $"Ruta de producción '{nombreTrim}' registrada exitosamente.", newId);
        }
    }

    public async Task<(bool success, string message, int id)> AnularRutaProduccionAsync(
        int empresaId,
        int contextoId,
        int userId,
        int id)
    {
        if (empresaId <= 0 || contextoId <= 0 || id <= 0)
            return (false, "Identificador de ruta no válido.", 0);

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId))
            return (false, "La empresa no se encuentra habilitada.", 0);

        var old = await conn.QuerySingleOrDefaultAsync<RutaProduccionItemModel>(
            RutaSelect.Replace("{LOCK}", "WITH (UPDLOCK, HOLDLOCK)") + " WHERE rp.ruta_prod_id = @Id",
            new { Id = id }, tx);

        if (old is null || old.Estado != "A")
            return (false, "La ruta de producción no existe o ya está anulada.", id);

        // Validar si está en uso por alguna Orden de Producción activa
        int opCount = await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1) FROM orden_pro
            WHERE ruta_prod_id = @Id AND estado = 'A'",
            new { Id = id }, tx);

        if (opCount > 0)
            return (false, $"No se puede anular la ruta '{old.Nombre}' porque está asignada a {opCount} orden(es) de producción.", id);

        await conn.ExecuteAsync("sp_ruta_prod_eliminar", new
        {
            rutaProduccionId = id,
            usuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, $"Ruta de producción '{old.Nombre}' anulada correctamente.", id);
    }

    public async Task<(bool success, string message, int id)> AgregarSubprocesoRutaAsync(
        int empresaId,
        int contextoId,
        int userId,
        RutaSubProcesoGuardarSolicitud model)
    {
        if (empresaId <= 0 || model is null || model.RutaProduccionId <= 0 || model.SubProcesoProduccionId <= 0)
            return (false, "Parámetros de subproceso inválidos.", 0);

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId))
            return (false, "La empresa no se encuentra habilitada.", 0);

        // Validar que la ruta existe y está activa
        var ruta = await conn.QuerySingleOrDefaultAsync<RutaProduccionItemModel>(
            RutaSelect.Replace("{LOCK}", "WITH (UPDLOCK, HOLDLOCK)") + " WHERE rp.ruta_prod_id = @Id",
            new { Id = model.RutaProduccionId }, tx);

        if (ruta is null || ruta.Estado != "A")
            return (false, "La ruta de producción no se encuentra activa.", 0);

        // Validar si ya está asignado
        int exists = await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1) FROM ruta_p_s_p_p
            WHERE ruta_prod_id = @RutaId AND sub_proce_p_id = @SubId AND estado = 'A'",
            new { RutaId = model.RutaProduccionId, SubId = model.SubProcesoProduccionId }, tx);

        if (exists > 0)
            return (false, "El subproceso ya está agregado a esta ruta.", 0);

        // Determinar orden automático si no se envió o es <= 0
        int orden = model.Orden;
        if (orden <= 0)
        {
            orden = await conn.ExecuteScalarAsync<int>(@"
                SELECT COALESCE(MAX(orden), 0) + 1 FROM ruta_p_s_p_p
                WHERE ruta_prod_id = @RutaId AND estado = 'A'",
                new { RutaId = model.RutaProduccionId }, tx);
        }

        var p = new DynamicParameters();
        p.Add("@rutaProduccionId", model.RutaProduccionId);
        p.Add("@subProcesoProduccionId", model.SubProcesoProduccionId);
        p.Add("@orden", orden);
        p.Add("@usuarioAccionId", userId);
        p.Add("@newID", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await conn.ExecuteAsync("sp_ruta_p_s_p_p_insertar", p, tx, commandType: CommandType.StoredProcedure);
        int newId = p.Get<int>("@newID");

        tx.Commit();
        return (true, "Subproceso agregado exitosamente a la ruta.", newId);
    }

    public async Task<(bool success, string message, int id)> ActualizarOrdenSubprocesoRutaAsync(
        int empresaId,
        int contextoId,
        int userId,
        RutaSubProcesoOrdenSolicitud model)
    {
        if (empresaId <= 0 || model is null || model.RutaSubProcesoId <= 0 || model.Orden <= 0)
            return (false, "Parámetros de orden inválidos.", 0);

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId))
            return (false, "La empresa no se encuentra habilitada.", 0);

        await conn.ExecuteAsync("sp_ruta_p_s_p_p_actualizar", new
        {
            rutaSubProcesoProduccionId = model.RutaSubProcesoId,
            orden = model.Orden,
            usuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, "Orden de subproceso actualizado.", model.RutaSubProcesoId);
    }

    public async Task<(bool success, string message, int id)> QuitarSubprocesoRutaAsync(
        int empresaId,
        int contextoId,
        int userId,
        int id)
    {
        if (empresaId <= 0 || id <= 0)
            return (false, "Identificador inválido.", 0);

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId))
            return (false, "La empresa no se encuentra habilitada.", 0);

        await conn.ExecuteAsync("sp_ruta_p_s_p_p_eliminar", new
        {
            rutaSubProcesoProduccionId = id,
            usuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, "Subproceso removido de la ruta.", id);
    }
}

