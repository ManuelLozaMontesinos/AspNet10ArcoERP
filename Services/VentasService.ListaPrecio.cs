using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class VentasService
{
    private const string ListaPrecioSelect = @"SELECT lp.lista_precio_id Id, lp.empresa_sede_id EmpresaSedeId, RTRIM(es.nombre) SedeNombre,
        RTRIM(lp.nombre) Nombre, lp.impuesto_tasa_id ImpuestoTasaId, it.tasa Tasa, RTRIM(ti.nombre) ImpuestoNombre,
        lp.moneda_id MonedaId, RTRIM(m.nombre) MonedaNombre, RTRIM(m.simbolo) Simbolo, lp.estado Estado,
        COALESCE(lp.fecha_modifica, lp.fecha_creacion) FechaRevision,
        (SELECT COUNT(*) FROM lista_p_prod lpp WHERE lpp.lista_precio_id = lp.lista_precio_id AND lpp.estado = 'A') CantidadProductos,
        (SELECT COUNT(*) FROM lista_p_prove lpv WHERE lpv.lista_precio_id = lp.lista_precio_id AND lpv.estado = 'A') CantidadGrupos
        FROM lista_precio lp
        JOIN empresa_sede es ON es.empresa_sede_id = lp.empresa_sede_id
        JOIN impuesto_tasa it ON it.impuesto_tasa_id = lp.impuesto_tasa_id
        JOIN tipo_impuesto ti ON ti.tipo_impuesto_id = it.tipo_impuesto_id
        JOIN moneda m ON m.moneda_id = lp.moneda_id ";

    public async Task<ListaPrecioPageModel> GetListasPrecioAsync(int empresaId, int contextoId, string? search = null, int? sedeId = null, int? monedaId = null, string? estado = null, int page = 1, int top = 25, string? sort = null, string? dir = null)
    {
        var model = new ListaPrecioPageModel { Search = search, SedeId = sedeId, MonedaId = monedaId, Estado = State(estado) };
        var columns = new Dictionary<string, string>
        {
            ["nombre"] = "lp.nombre",
            ["sede"] = "es.nombre",
            ["moneda"] = "m.nombre",
            ["tasa"] = "it.tasa",
            ["estado"] = "lp.estado",
            ["id"] = "lp.lista_precio_id"
        };
        model.Ordenar(sort, dir, columns.Keys, "nombre");
        model.Paginar(0, page, top);
        if (empresaId <= 0) return model;

        using var conn = factory.CreateConnection();
        model.Sedes = (await conn.QueryAsync<LookupItemModel>("SELECT empresa_sede_id Id, RTRIM(nombre) Nombre FROM empresa_sede WHERE empresa_id = @EmpresaId AND estado = 'A' ORDER BY nombre", new { EmpresaId = empresaId })).AsList();
        model.Monedas = (await conn.QueryAsync<LookupItemModel>("SELECT moneda_id Id, RTRIM(nombre) Nombre, RTRIM(simbolo) Codigo FROM moneda WHERE estado = 'A' ORDER BY moneda_id")).AsList();
        model.Tasas = (await conn.QueryAsync<LookupItemModel>("SELECT it.impuesto_tasa_id Id, CONCAT(RTRIM(ti.nombre), ' (', CAST(it.tasa AS decimal(5,2)), '%)') Nombre, CAST(it.tasa AS varchar(20)) Codigo FROM impuesto_tasa it JOIN tipo_impuesto ti ON ti.tipo_impuesto_id = it.tipo_impuesto_id WHERE it.estado = 'A' ORDER BY it.impuesto_tasa_id")).AsList();

        const string where = @"WHERE es.empresa_id = @EmpresaId
            AND (@SedeId IS NULL OR lp.empresa_sede_id = @SedeId)
            AND (@MonedaId IS NULL OR lp.moneda_id = @MonedaId)
            AND (@Search IS NULL OR lp.nombre LIKE @Search)
            AND (@Estado IS NULL OR lp.estado = @Estado)";

        var args = new DynamicParameters(new
        {
            EmpresaId = empresaId,
            SedeId = sedeId > 0 ? sedeId : null,
            MonedaId = monedaId > 0 ? monedaId : null,
            Search = Search(search),
            Estado = State(estado)
        });

        int total = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM lista_precio lp JOIN empresa_sede es ON es.empresa_sede_id = lp.empresa_sede_id " + where, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        string query = ListaPrecioSelect + where + $" ORDER BY {columns[model.Sort]} {model.Dir}, lp.lista_precio_id OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";
        model.Items = (await conn.QueryAsync<ListaPrecioItemModel>(query, args)).AsList();
        model.VisibleCount = model.Items.Count;
        return model;
    }

    public async Task<ListaPrecioDetalleModel?> GetListaPrecioDetalleAsync(int empresaId, int id)
    {
        if (empresaId <= 0 || id <= 0) return null;
        using var conn = factory.CreateConnection();
        var item = await conn.QuerySingleOrDefaultAsync<ListaPrecioItemModel>(ListaPrecioSelect + "WHERE es.empresa_id = @EmpresaId AND lp.lista_precio_id = @Id", new { EmpresaId = empresaId, Id = id });
        if (item is null) return null;

        var sedes = (await conn.QueryAsync<LookupItemModel>("SELECT empresa_sede_id Id, RTRIM(nombre) Nombre FROM empresa_sede WHERE empresa_id = @EmpresaId AND estado = 'A' ORDER BY nombre", new { EmpresaId = empresaId })).AsList();
        var monedas = (await conn.QueryAsync<LookupItemModel>("SELECT moneda_id Id, RTRIM(nombre) Nombre, RTRIM(simbolo) Codigo FROM moneda WHERE estado = 'A' ORDER BY moneda_id")).AsList();
        var tasas = (await conn.QueryAsync<LookupItemModel>("SELECT it.impuesto_tasa_id Id, CONCAT(RTRIM(ti.nombre), ' (', CAST(it.tasa AS decimal(5,2)), '%)') Nombre, CAST(it.tasa AS varchar(20)) Codigo FROM impuesto_tasa it JOIN tipo_impuesto ti ON ti.tipo_impuesto_id = it.tipo_impuesto_id WHERE it.estado = 'A' ORDER BY it.impuesto_tasa_id")).AsList();

        return new ListaPrecioDetalleModel
        {
            Cabecera = item,
            Sedes = sedes,
            Monedas = monedas,
            Tasas = tasas
        };
    }

    public async Task<List<LookupItemModel>> GetListaPrecioLookupsAsync(int empresaId, string tipo)
    {
        if (empresaId <= 0) return new();
        using var conn = factory.CreateConnection();
        return tipo.ToLowerInvariant() switch
        {
            "sedes" => (await conn.QueryAsync<LookupItemModel>("SELECT empresa_sede_id Id, RTRIM(nombre) Nombre FROM empresa_sede WHERE empresa_id = @EmpresaId AND estado = 'A' ORDER BY nombre", new { EmpresaId = empresaId })).AsList(),
            "monedas" => (await conn.QueryAsync<LookupItemModel>("SELECT moneda_id Id, RTRIM(nombre) Nombre, RTRIM(simbolo) Codigo FROM moneda WHERE estado = 'A' ORDER BY moneda_id")).AsList(),
            "tasas" => (await conn.QueryAsync<LookupItemModel>("SELECT it.impuesto_tasa_id Id, CONCAT(RTRIM(ti.nombre), ' (', CAST(it.tasa AS decimal(5,2)), '%)') Nombre, CAST(it.tasa AS varchar(20)) Codigo FROM impuesto_tasa it JOIN tipo_impuesto ti ON ti.tipo_impuesto_id = it.tipo_impuesto_id WHERE it.estado = 'A' ORDER BY it.impuesto_tasa_id")).AsList(),
            _ => new()
        };
    }

    public async Task<(bool success, string message, int id)> SaveListaPrecioAsync(int empresaId, int userId, ListaPrecioSolicitud model)
    {
        if (model is null || string.IsNullOrWhiteSpace(model.Nombre) || model.EmpresaSedeId <= 0 || model.ImpuestoTasaId <= 0 || model.MonedaId <= 0)
            return Failure("Revise los datos de la lista de precios (nombre, sede, impuesto y moneda).");

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId))
            return Failure("La empresa no está disponible o no se encuentra habilitada.");

        // Sede validation
        int sedeCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM empresa_sede WHERE empresa_sede_id = @SedeId AND empresa_id = @EmpresaId AND estado = 'A'", new { SedeId = model.EmpresaSedeId, EmpresaId = empresaId }, tx);
        if (sedeCount == 0) return Failure("La sede seleccionada no pertenece a la empresa actual o está inactiva.");

        if (model.Id > 0)
        {
            var old = await conn.QuerySingleOrDefaultAsync<ListaPrecioItemModel>(ListaPrecioSelect + "WITH (UPDLOCK,HOLDLOCK) WHERE es.empresa_id = @EmpresaId AND lp.lista_precio_id = @Id", new { EmpresaId = empresaId, Id = model.Id }, tx);
            if (old is null || old.Estado != "A" || (!string.IsNullOrEmpty(model.Revision) && old.Revision != model.Revision))
                return Failure("La lista de precios fue modificada o ya no se encuentra activa. Actualice el listado.");

            await conn.ExecuteAsync("sp_lista_precio_actualizar", new
            {
                ListaPrecioId = model.Id,
                Nombre = model.Nombre.Trim(),
                UsuarioAccionId = userId
            }, tx, commandType: CommandType.StoredProcedure);

            tx.Commit();
            return (true, "Lista de precios actualizada correctamente.", model.Id);
        }
        else
        {
            int newId = await InsertAsync(conn, tx, "sp_lista_precio_insertar", new
            {
                EmpresaSedeId = model.EmpresaSedeId,
                Nombre = model.Nombre.Trim(),
                ImpuestoTasaId = model.ImpuestoTasaId,
                MonedaId = model.MonedaId,
                UsuarioAccionId = userId
            });

            tx.Commit();
            return (true, "Lista de precios registrada correctamente.", newId);
        }
    }

    public async Task<(bool success, string message, int id)> AnularListaPrecioAsync(int empresaId, int userId, int id, string revision)
    {
        if (empresaId <= 0 || userId <= 0 || id <= 0) return Failure("Identificador no válido.");
        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return Failure("La empresa no está habilitada.");

        var old = await conn.QuerySingleOrDefaultAsync<ListaPrecioItemModel>(ListaPrecioSelect + "WITH (UPDLOCK,HOLDLOCK) WHERE es.empresa_id = @EmpresaId AND lp.lista_precio_id = @Id", new { EmpresaId = empresaId, Id = id }, tx);
        if (old is null || old.Estado != "A" || (!string.IsNullOrEmpty(revision) && old.Revision != revision))
            return Failure("La lista de precios no se encuentra activa o fue modificada por otro usuario.");

        await conn.ExecuteAsync("sp_lista_precio_eliminar", new
        {
            ListaPrecioId = id,
            UsuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, "Lista de precios anulada correctamente.", id);
    }

    public async Task<ListaPrecioProductoPageModel> GetListaPrecioProductosAsync(int empresaId, int listaPrecioId, string? search = null, string? estado = null, int page = 1, int top = 25, string? sort = null, string? dir = null)
    {
        var model = new ListaPrecioProductoPageModel { ListaPrecioId = listaPrecioId, Search = search, Estado = State(estado) };
        var columns = new Dictionary<string, string>
        {
            ["codigo"] = "p.producto",
            ["descripcion"] = "p.descripcion",
            ["familia"] = "fp.nombre",
            ["clase"] = "cp.nombre",
            ["grupo"] = "gp.nombre",
            ["valor"] = "lpp.valor_unitario",
            ["precio"] = "lpp.precio_unitario",
            ["descuento"] = "lpp.permite_dcto",
            ["estado"] = "lpp.estado",
            ["id"] = "lpp.lista_p_prod_id"
        };
        model.Ordenar(sort, dir, columns.Keys, "descripcion");
        model.Paginar(0, page, top);
        if (empresaId <= 0 || listaPrecioId <= 0) return model;

        using var conn = factory.CreateConnection();

        const string where = @"WHERE lpp.lista_precio_id = @ListaPrecioId
            AND es.empresa_id = @EmpresaId
            AND (@Search IS NULL OR p.descripcion LIKE @Search OR p.producto LIKE @Search)
            AND (@Estado IS NULL OR lpp.estado = @Estado)";

        var args = new DynamicParameters(new
        {
            EmpresaId = empresaId,
            ListaPrecioId = listaPrecioId,
            Search = Search(search),
            Estado = State(estado)
        });

        const string countSql = @"SELECT COUNT(*) FROM lista_p_prod lpp
            JOIN lista_precio lp ON lp.lista_precio_id = lpp.lista_precio_id
            JOIN empresa_sede es ON es.empresa_sede_id = lp.empresa_sede_id
            JOIN producto p ON p.producto_id = lpp.producto_id " + where;

        int total = await conn.ExecuteScalarAsync<int>(countSql, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        const string selectSql = @"SELECT lpp.lista_p_prod_id Id, lpp.lista_precio_id ListaPrecioId, lpp.producto_id ProductoId,
            RTRIM(p.producto) Codigo, p.descripcion Descripcion, fp.nombre Familia, cp.nombre Clase, gp.nombre Grupo,
            lpp.valor_unitario ValorUnitario, lpp.precio_unitario PrecioUnitario, lpp.permite_dcto PermiteDescuento,
            lpp.estado Estado, it.tasa Tasa,
            COALESCE(lpp.fecha_modifica, lpp.fecha_creacion) FechaRevision
            FROM lista_p_prod lpp
            JOIN lista_precio lp ON lp.lista_precio_id = lpp.lista_precio_id
            JOIN empresa_sede es ON es.empresa_sede_id = lp.empresa_sede_id
            JOIN impuesto_tasa it ON it.impuesto_tasa_id = lp.impuesto_tasa_id
            JOIN producto p ON p.producto_id = lpp.producto_id
            JOIN familia_producto fp ON fp.familia_producto_id = p.familia_producto_id
            JOIN clase_producto cp ON cp.clase_producto_id = fp.clase_producto_id
            JOIN grupo_producto gp ON gp.grupo_producto_id = cp.grupo_producto_id " + where;

        string query = selectSql + $" ORDER BY {columns[model.Sort]} {model.Dir}, lpp.lista_p_prod_id OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";
        model.Items = (await conn.QueryAsync<ListaPrecioProductoItemModel>(query, args)).AsList();
        model.VisibleCount = model.Items.Count;
        return model;
    }

    public async Task<(bool success, string message, int id)> SaveListaPrecioProductoAsync(int empresaId, int userId, ListaPrecioProductoSolicitud model)
    {
        if (model is null || model.ListaPrecioId <= 0 || model.Id <= 0) return Failure("Datos del producto no válidos.");

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return Failure("La empresa no está disponible.");

        const string checkSql = @"SELECT lpp.lista_p_prod_id Id, lpp.estado Estado, COALESCE(lpp.fecha_modifica, lpp.fecha_creacion) FechaRevision,
            lpp.lista_precio_id ListaPrecioId, lpp.producto_id ProductoId, lpp.valor_unitario ValorUnitario, lpp.precio_unitario PrecioUnitario,
            lpp.permite_dcto PermiteDescuento
            FROM lista_p_prod lpp WITH (UPDLOCK,HOLDLOCK)
            JOIN lista_precio lp ON lp.lista_precio_id = lpp.lista_precio_id
            JOIN empresa_sede es ON es.empresa_sede_id = lp.empresa_sede_id
            WHERE lpp.lista_p_prod_id = @Id AND lpp.lista_precio_id = @ListaPrecioId AND es.empresa_id = @EmpresaId";

        var old = await conn.QuerySingleOrDefaultAsync<ListaPrecioProductoItemModel>(checkSql, new { Id = model.Id, ListaPrecioId = model.ListaPrecioId, EmpresaId = empresaId }, tx);
        if (old is null || (!string.IsNullOrEmpty(model.Revision) && old.Revision != model.Revision))
            return Failure("El producto en la lista de precios cambió o ya no se encuentra disponible. Actualice la tabla.");

        await conn.ExecuteAsync("sp_lista_p_prod_actualizar", new
        {
            ListaPrecioProductoId = model.Id,
            ValorUnitario = model.ValorUnitario,
            PrecioUnitario = model.PrecioUnitario,
            PermiteDescuento = model.PermiteDescuento,
            Estado = model.Estado,
            UsuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, "Precio de producto actualizado correctamente.", model.Id);
    }

    public async Task<List<ListaPrecioGrupoItemModel>> GetListaPrecioGruposAsync(int empresaId, int listaPrecioId)
    {
        if (empresaId <= 0 || listaPrecioId <= 0) return new();
        using var conn = factory.CreateConnection();
        const string sql = @"SELECT lpv.lista_p_prove_id Id, lpv.lista_precio_id ListaPrecioId, lpv.grupo_prove_id GrupoProveId,
            RTRIM(gp.nombre) GrupoNombre, lpv.estado Estado, COALESCE(lpv.fecha_modifica, lpv.fecha_creacion) FechaRevision,
            (SELECT COUNT(*) FROM lista_p_p_prod lppp WHERE lppp.lista_p_prove_id = lpv.lista_p_prove_id AND lppp.estado = 'A') CantidadDescuentos
            FROM lista_p_prove lpv
            JOIN lista_precio lp ON lp.lista_precio_id = lpv.lista_precio_id
            JOIN empresa_sede es ON es.empresa_sede_id = lp.empresa_sede_id
            JOIN grupo_prove gp ON gp.grupo_prove_id = lpv.grupo_prove_id
            WHERE lpv.lista_precio_id = @ListaPrecioId AND es.empresa_id = @EmpresaId
            ORDER BY gp.nombre ASC";
        return (await conn.QueryAsync<ListaPrecioGrupoItemModel>(sql, new { ListaPrecioId = listaPrecioId, EmpresaId = empresaId })).AsList();
    }

    public async Task<List<LookupItemModel>> GetGruposClienteDisponiblesAsync(int empresaId, int listaPrecioId)
    {
        if (empresaId <= 0 || listaPrecioId <= 0) return new();
        using var conn = factory.CreateConnection();
        const string sql = @"SELECT gp.grupo_prove_id Id, RTRIM(gp.nombre) Nombre
            FROM grupo_prove gp
            WHERE gp.empresa_id = @EmpresaId AND gp.estado = 'A'
              AND NOT EXISTS (SELECT 1 FROM lista_p_prove lpv WHERE lpv.lista_precio_id = @ListaPrecioId AND lpv.grupo_prove_id = gp.grupo_prove_id AND lpv.estado = 'A')
            ORDER BY gp.nombre ASC";
        return (await conn.QueryAsync<LookupItemModel>(sql, new { EmpresaId = empresaId, ListaPrecioId = listaPrecioId })).AsList();
    }

    public async Task<(bool success, string message, int id)> SaveListaPrecioGrupoAsync(int empresaId, int userId, ListaPrecioGrupoSolicitud model)
    {
        if (model is null || model.ListaPrecioId <= 0 || model.GrupoProveId <= 0) return Failure("Seleccione un grupo de clientes válido.");

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return Failure("La empresa no está disponible.");

        int exists = await conn.ExecuteScalarAsync<int>(@"SELECT COUNT(*) FROM lista_p_prove lpv
            JOIN lista_precio lp ON lp.lista_precio_id = lpv.lista_precio_id
            JOIN empresa_sede es ON es.empresa_sede_id = lp.empresa_sede_id
            WHERE lpv.lista_precio_id = @ListaPrecioId AND lpv.grupo_prove_id = @GrupoId AND es.empresa_id = @EmpresaId AND lpv.estado = 'A'",
            new { ListaPrecioId = model.ListaPrecioId, GrupoId = model.GrupoProveId, EmpresaId = empresaId }, tx);

        if (exists > 0) return Failure("El grupo de clientes ya se encuentra asignado a esta lista de precios.");

        int newId = await InsertAsync(conn, tx, "sp_lista_p_prove_insertar", new
        {
            ListaPrecioId = model.ListaPrecioId,
            GrupoProveedorId = model.GrupoProveId,
            UsuarioAccionId = userId
        });

        tx.Commit();
        return (true, "Grupo de clientes asignado correctamente a la lista.", newId);
    }

    public async Task<(bool success, string message, int id)> QuitarListaPrecioGrupoAsync(int empresaId, int userId, int id)
    {
        if (empresaId <= 0 || userId <= 0 || id <= 0) return Failure("Identificador no válido.");
        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return Failure("La empresa no está disponible.");

        int count = await conn.ExecuteScalarAsync<int>(@"SELECT COUNT(*) FROM lista_p_prove lpv
            JOIN lista_precio lp ON lp.lista_precio_id = lpv.lista_precio_id
            JOIN empresa_sede es ON es.empresa_sede_id = lp.empresa_sede_id
            WHERE lpv.lista_p_prove_id = @Id AND es.empresa_id = @EmpresaId AND lpv.estado = 'A'",
            new { Id = id, EmpresaId = empresaId }, tx);

        if (count == 0) return Failure("El vínculo no existe o ya fue retirado.");

        await conn.ExecuteAsync("sp_lista_p_prove_eliminar", new
        {
            ListaPrecioProveedorId = id,
            UsuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, "Grupo de clientes retirado de la lista.", id);
    }

    public async Task<ListaPrecioGrupoProductoPageModel> GetListaPrecioGrupoProductosAsync(int empresaId, int listaPProveId, string? search = null, int page = 1, int top = 25)
    {
        var model = new ListaPrecioGrupoProductoPageModel { ListaPProveId = listaPProveId, Search = search };
        model.Paginar(0, page, top);
        if (empresaId <= 0 || listaPProveId <= 0) return model;

        using var conn = factory.CreateConnection();

        const string where = @"WHERE lppp.lista_p_prove_id = @ListaPProveId
            AND es.empresa_id = @EmpresaId
            AND lppp.estado = 'A'
            AND (@Search IS NULL OR p.descripcion LIKE @Search OR p.producto LIKE @Search)";

        var args = new DynamicParameters(new
        {
            EmpresaId = empresaId,
            ListaPProveId = listaPProveId,
            Search = Search(search)
        });

        const string countSql = @"SELECT COUNT(*) FROM lista_p_p_prod lppp
            JOIN lista_p_prove lpv ON lpv.lista_p_prove_id = lppp.lista_p_prove_id
            JOIN lista_precio lp ON lp.lista_precio_id = lpv.lista_precio_id
            JOIN empresa_sede es ON es.empresa_sede_id = lp.empresa_sede_id
            JOIN lista_p_prod lpp ON lpp.lista_p_prod_id = lppp.lista_p_prod_id
            JOIN producto p ON p.producto_id = lpp.producto_id " + where;

        int total = await conn.ExecuteScalarAsync<int>(countSql, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        const string selectSql = @"SELECT lppp.lista_p_p_prod_id Id, lppp.lista_p_prove_id ListaPProveId, lppp.lista_p_prod_id ListaPProdId,
            lppp.porc_precio_base PorcentajePrecioBase, lppp.estado Estado,
            p.producto_id ProductoId, RTRIM(p.producto) Codigo, p.descripcion Descripcion,
            lpp.precio_unitario PrecioUnitario,
            COALESCE(lppp.fecha_modifica, lppp.fecha_creacion) FechaRevision
            FROM lista_p_p_prod lppp
            JOIN lista_p_prove lpv ON lpv.lista_p_prove_id = lppp.lista_p_prove_id
            JOIN lista_precio lp ON lp.lista_precio_id = lpv.lista_precio_id
            JOIN empresa_sede es ON es.empresa_sede_id = lp.empresa_sede_id
            JOIN lista_p_prod lpp ON lpp.lista_p_prod_id = lppp.lista_p_prod_id
            JOIN producto p ON p.producto_id = lpp.producto_id " + where;

        string query = selectSql + " ORDER BY p.descripcion ASC OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";
        model.Items = (await conn.QueryAsync<ListaPrecioGrupoProductoItemModel>(query, args)).AsList();
        model.VisibleCount = model.Items.Count;
        return model;
    }

    public async Task<(bool success, string message, int id)> SaveListaPrecioGrupoProductoAsync(int empresaId, int userId, ListaPrecioGrupoProductoSolicitud model)
    {
        if (model is null || model.ListaPProveId <= 0 || model.ListaPProdId <= 0) return Failure("Datos de porcentaje no válidos.");

        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return Failure("La empresa no está disponible.");

        if (model.Id > 0)
        {
            await conn.ExecuteAsync("sp_lista_p_p_prod_actualizar", new
            {
                ListaPrecioProveedorProductoId = model.Id,
                PorcentajePrecioBase = model.PorcentajePrecioBase,
                UsuarioAccionId = userId
            }, tx, commandType: CommandType.StoredProcedure);

            tx.Commit();
            return (true, "Porcentaje de precio base actualizado.", model.Id);
        }
        else
        {
            int newId = await InsertAsync(conn, tx, "sp_lista_p_p_prod_insertar", new
            {
                ListaPrecioProveedorId = model.ListaPProveId,
                ListaPrecioProductoId = model.ListaPProdId,
                PorcentajePrecioBase = model.PorcentajePrecioBase,
                UsuarioAccionId = userId
            });

            tx.Commit();
            return (true, "Porcentaje de precio base configurado.", newId);
        }
    }

    public async Task<(bool success, string message, int id)> QuitarListaPrecioGrupoProductoAsync(int empresaId, int userId, int id)
    {
        if (empresaId <= 0 || userId <= 0 || id <= 0) return Failure("Identificador no válido.");
        using var conn = factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        if (!await LockCompanyAsync(conn, tx, empresaId)) return Failure("La empresa no está disponible.");

        await conn.ExecuteAsync("sp_lista_p_p_prod_eliminar", new
        {
            ListaPrecioProveedorProductoId = id,
            UsuarioAccionId = userId
        }, tx, commandType: CommandType.StoredProcedure);

        tx.Commit();
        return (true, "Configuración de precio por grupo retirada.", id);
    }

    public async Task<(string Titulo, byte[] Contenido)> ExportarListaPrecioExcelAsync(int empresaId, int listaPrecioId)
    {
        if (empresaId <= 0 || listaPrecioId <= 0) throw new ArgumentException("Parámetros no válidos.");
        using var conn = factory.CreateConnection();
        var lp = await conn.QuerySingleOrDefaultAsync<ListaPrecioItemModel>(ListaPrecioSelect + "WHERE es.empresa_id = @EmpresaId AND lp.lista_precio_id = @Id", new { EmpresaId = empresaId, Id = listaPrecioId })
            ?? throw new KeyNotFoundException("No se encontró la lista de precios.");

        const string prodSql = @"SELECT RTRIM(p.producto) Codigo, p.descripcion Descripcion, fp.nombre Familia, cp.nombre Clase, gp.nombre Grupo,
            lpp.valor_unitario ValorUnitario, lpp.precio_unitario PrecioUnitario, lpp.permite_dcto PermiteDescuento,
            CASE WHEN lpp.estado = 'A' THEN 'Activo' ELSE 'Inactivo' END Estado
            FROM lista_p_prod lpp
            JOIN producto p ON p.producto_id = lpp.producto_id
            JOIN familia_producto fp ON fp.familia_producto_id = p.familia_producto_id
            JOIN clase_producto cp ON cp.clase_producto_id = fp.clase_producto_id
            JOIN grupo_producto gp ON gp.grupo_producto_id = cp.grupo_producto_id
            WHERE lpp.lista_precio_id = @ListaPrecioId
            ORDER BY p.descripcion ASC";

        var productos = (await conn.QueryAsync(prodSql, new { ListaPrecioId = listaPrecioId })).AsList();

        var columns = new List<(string Label, Func<dynamic, object?> Value)>
        {
            ("Código", r => (string)r.Codigo),
            ("Descripción", r => (string)r.Descripcion),
            ("Familia", r => (string)r.Familia),
            ("Clase", r => (string)r.Clase),
            ("Grupo", r => (string)r.Grupo),
            ("Valor Unitario", r => (decimal)r.ValorUnitario),
            ("Precio Unitario", r => (decimal)r.PrecioUnitario),
            ("Permite Dcto", r => (string)r.PermiteDescuento),
            ("Estado", r => (string)r.Estado)
        };

        string resumen = $"Sede: {lp.SedeNombre} | Moneda: {lp.MonedaNombre} ({lp.Simbolo}) | Afectación: {lp.ImpuestoNombre} ({lp.Tasa:N2}%) | Total ítems: {productos.Count}";
        byte[] excelBytes = InformeExcel.Crear("LISTA DE PRECIOS: " + lp.Nombre, "ARCO ERP", resumen, productos, columns);
        return ("ListaPrecios_" + lp.Nombre.Replace(" ", "_"), excelBytes);
    }
}

