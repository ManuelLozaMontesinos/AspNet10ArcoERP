using System.Data;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services;

public partial class InventarioService
{
    private const string TransaccionSelect = @"
        SELECT t.trans_alma_id Id, RTRIM(t.trans_alma) Codigo, t.nombre Nombre,
               t.tipo_mov_alma_id TipoMovimientoId, ISNULL(m.nombre,'') TipoMovimientoNombre,
               t.tipo_operacion_id TipoOperacionId, ISNULL(o.nombre,'') TipoOperacionNombre,
               CAST(CASE WHEN t.es_compra='S' THEN 1 ELSE 0 END AS bit) EsCompra, t.estado Estado
        FROM trans_alma t
        LEFT JOIN tipo_mov_alma m ON m.tipo_mov_alma_id=t.tipo_mov_alma_id
        LEFT JOIN tipo_operacion o ON o.tipo_operacion_id=t.tipo_operacion_id ";

    public async Task<TransaccionAlmacenPageModel> GetTransaccionesAsync(int empresaId, string? search = null, int? tipoMovimientoId = null, string? estado = null, int page = 1, int top = 25, string? sort = null, string? dir = null)
    {
        var columns = new Dictionary<string, string> {
            ["id"]="t.trans_alma_id", ["codigo"]="t.trans_alma", ["nombre"]="t.nombre",
            ["movimiento"]="m.nombre", ["compra"]="t.es_compra", ["operacion"]="o.nombre", ["estado"]="t.estado"
        };
        var model = new TransaccionAlmacenPageModel();
        model.Ordenar(sort, dir, columns.Keys, "nombre");
        const string filter = @"WHERE t.empresa_id=@EmpresaId AND (@Search IS NULL OR t.nombre LIKE @Search OR t.trans_alma LIKE @Search)
            AND (@TipoMovimientoId IS NULL OR t.tipo_mov_alma_id=@TipoMovimientoId) AND (@Estado IS NULL OR t.estado=@Estado)";
        var args = new DynamicParameters(new { EmpresaId = empresaId, Search = Search(search), TipoMovimientoId = tipoMovimientoId, Estado = State(estado) });
        using var conn = factory.CreateConnection();
        model.Paginar(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM trans_alma t " + filter, args), page, top);
        args.Add("Offset", (model.Page - 1) * model.PageSize); args.Add("Top", model.PageSize);
        model.Items = (await conn.QueryAsync<TransaccionAlmacenModel>(TransaccionSelect + filter +
            $" ORDER BY {columns[model.Sort]} {model.Dir}{(model.Sort == "id" ? "" : ", t.trans_alma_id ASC")} OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY", args)).AsList();
        model.VisibleCount = model.Items.Count;
        model.TiposMovimiento = (await conn.QueryAsync<LookupItemModel>("SELECT tipo_mov_alma_id Id, tipo_mov_alma Codigo, nombre Nombre FROM tipo_mov_alma WHERE estado='A' ORDER BY nombre")).AsList();
        model.TiposOperacion = (await conn.QueryAsync<LookupItemModel>("SELECT tipo_operacion_id Id, tipo_operacion Codigo, nombre Nombre FROM tipo_operacion WHERE estado='A' ORDER BY nombre")).AsList();
        return model;
    }

    public async Task<TransaccionAlmacenModel?> GetTransaccionAsync(int id, int empresaId)
    {
        using var conn = factory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<TransaccionAlmacenModel>(TransaccionSelect + "WHERE t.trans_alma_id=@Id AND t.empresa_id=@EmpresaId", new { Id = id, EmpresaId = empresaId });
    }

    public async Task<(bool success, string message, int id)> SaveTransaccionAsync(TransaccionAlmacenModel model, int empresaId, int userId)
    {
        model.Nombre = Text(model.Nombre);
        if (!Valid(model) || empresaId <= 0 || userId <= 0) return Failure("Revise el nombre y las referencias de la transacción.");
        using var conn = factory.CreateConnection();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);
        if (!await LockCompanyAsync(conn, tx, empresaId)) return Failure("La empresa no está habilitada.");
        var old = model.Id == 0 ? null : await conn.QuerySingleOrDefaultAsync<TransaccionAlmacenModel>(@"
            SELECT trans_alma_id Id, tipo_mov_alma_id TipoMovimientoId, tipo_operacion_id TipoOperacionId, estado Estado
            FROM trans_alma WITH (UPDLOCK,HOLDLOCK) WHERE trans_alma_id=@Id AND empresa_id=@EmpresaId", new { model.Id, EmpresaId = empresaId }, tx);
        if (model.Id > 0 && (old is null || old.Estado != "A")) return Failure("La transacción no existe o está anulada.");
        if (old is not null && old.TipoMovimientoId != model.TipoMovimientoId) return Failure("El tipo de movimiento no puede cambiar después del alta.");
        if (model.TipoMovimientoId.HasValue && old is null && await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM tipo_mov_alma WHERE tipo_mov_alma_id=@Id AND estado='A'", new { Id = model.TipoMovimientoId }, tx) != 1)
            return Failure("Seleccione un tipo de movimiento vigente.");
        if (model.TipoOperacionId.HasValue && (old is null || old.TipoOperacionId != model.TipoOperacionId) && await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM tipo_operacion WHERE tipo_operacion_id=@Id AND estado='A'", new { Id = model.TipoOperacionId }, tx) != 1)
            return Failure("Seleccione un tipo de operación vigente.");
        var values = new DynamicParameters(new { Nombre = model.Nombre, TipoMovimientoAlmacenId = model.TipoMovimientoId, EsCompra = model.EsCompra ? "S" : "N", TipoOperacionId = model.TipoOperacionId, UsuarioAccionId = userId });
        int id = model.Id;
        if (id == 0)
        {
            // Legacy table 0014 generates a company-scoped, three-character code.
            // The TablaGeneral implementation/configuration is not modified.
            int? origin = await conn.QuerySingleOrDefaultAsync<int?>("SELECT origen_registro_id FROM origen_registro WHERE origen_registro='02' AND estado='A'", transaction: tx);
            if (!origin.HasValue) return Failure("No está configurado el origen de registro de usuario.");
            string code;
            try { code = await NextTransactionCodeAsync(conn, tx, empresaId, userId); }
            catch (ValidationException ex) { return Failure(ex.Message); }
            values.Add("EmpresaId", empresaId); values.Add("TransaccionAlmacen", code);
            values.Add("OrigenRegistroId", origin.Value); values.Add("EsConfigurable", "S"); values.Add("TieneMantenimiento", "S");
            id = await InsertAsync(conn, tx, "sp_trans_alma_insertar", values);
        }
        else
        {
            values.Add("TransaccionAlmacenId", id);
            await conn.ExecuteAsync("sp_trans_alma_actualizar", values, tx, commandType: CommandType.StoredProcedure);
        }
        tx.Commit();
        return (true, "Transacción guardada correctamente.", id);
    }

    private static Task<string> NextTransactionCodeAsync(IDbConnection conn, IDbTransaction tx, int empresaId, int userId)
        => MaestroEmpresaSql.NextCodeAsync(conn, tx, empresaId, userId, "0014");
    public async Task<(bool success, string message, int id)> AnularTransaccionAsync(int id, int empresaId, int userId)
    {
        if (id <= 0 || empresaId <= 0 || userId <= 0) return Failure("Identificador no válido.");
        using var conn = factory.CreateConnection(); using var tx = conn.BeginTransaction(IsolationLevel.Serializable);
        if (await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM trans_alma WITH (UPDLOCK,HOLDLOCK) WHERE trans_alma_id=@Id AND empresa_id=@EmpresaId AND estado='A'", new { Id = id, EmpresaId = empresaId }, tx) != 1)
            return Failure("La transacción no existe o ya está anulada.");
        await conn.ExecuteAsync("sp_trans_alma_eliminar", new { TransaccionAlmacenId = id, UsuarioAccionId = userId }, tx, commandType: CommandType.StoredProcedure);
        tx.Commit(); return (true, "Transacción anulada correctamente.", id);
    }

}
