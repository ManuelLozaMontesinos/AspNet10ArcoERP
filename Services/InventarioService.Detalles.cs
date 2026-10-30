using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services;

public partial class InventarioService
{
    public async Task<AlmacenDetalleModel?> GetAlmacenDetalleAsync(int almacenId, int empresaId)
    {
        using var conn = factory.CreateConnection();
        if (!await OwnsAlmacenAsync(conn, almacenId, empresaId)) return null;
        var args = new { AlmacenId = almacenId, EmpresaId = empresaId };
        var model = new AlmacenDetalleModel();
        model.Transacciones = (await conn.QueryAsync<AlmacenTransaccionModel>(@"
            SELECT at.alm_transa_id Id, at.almacen_id AlmacenId, at.trans_alma_id TransaccionId,
                   RTRIM(t.trans_alma) Codigo, t.nombre Nombre, ISNULL(m.nombre,'') TipoMovimientoNombre, at.estado Estado
            FROM alm_transa at JOIN trans_alma t ON t.trans_alma_id=at.trans_alma_id AND t.empresa_id=@EmpresaId
            LEFT JOIN tipo_mov_alma m ON m.tipo_mov_alma_id=t.tipo_mov_alma_id
            WHERE at.almacen_id=@AlmacenId AND at.estado='A' ORDER BY t.nombre, at.alm_transa_id", args)).AsList();
        model.TransaccionesDisponibles = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT t.trans_alma_id Id, RTRIM(t.trans_alma) Codigo, t.nombre Nombre FROM trans_alma t
            WHERE t.empresa_id=@EmpresaId AND t.estado='A' ORDER BY t.nombre", args)).AsList();
        model.Series = (await conn.QueryAsync<AlmacenSerieModel>(@"
            SELECT a.almacen_serie_id Id, d.doc_com_serie_id DocumentoSerieId, t.nombre DocumentoNombre,
                   d.serie Serie, d.correlativo Correlativo, a.estado Estado
            FROM almacen_serie a JOIN doc_com_serie d ON d.doc_com_serie_id=a.doc_com_serie_id
            JOIN empresa_sede s ON s.empresa_sede_id=d.empresa_sede_id AND s.empresa_id=@EmpresaId
            JOIN tipo_doc_com t ON t.tipo_doc_com_id=d.tipo_doc_com_id
            WHERE a.almacen_id=@AlmacenId AND a.estado='A' ORDER BY t.nombre,d.serie,a.almacen_serie_id", args)).AsList();
        model.SeriesDisponibles = (await conn.QueryAsync<LookupItemModel>(@"
            SELECT d.doc_com_serie_id Id, CONCAT(t.nombre,' · ',d.serie,' · ',s.nombre) Nombre
            FROM doc_com_serie d JOIN empresa_sede s ON s.empresa_sede_id=d.empresa_sede_id
            JOIN tipo_doc_com t ON t.tipo_doc_com_id=d.tipo_doc_com_id
            WHERE s.empresa_id=@EmpresaId AND s.estado='A' AND d.estado='A' AND t.estado='A'
              AND NOT EXISTS(SELECT 1 FROM almacen_serie a WHERE a.almacen_id=@AlmacenId AND a.doc_com_serie_id=d.doc_com_serie_id AND a.estado='A')
            ORDER BY t.nombre,d.serie,d.doc_com_serie_id", args)).AsList();
        return model;
    }

    public async Task<List<AlmacenConfiguracionModel>?> GetConfiguracionAsync(int almacenId, int almacenTransaccionId, int empresaId)
    {
        using var conn = factory.CreateConnection();
        if (!await OwnsAlmacenAsync(conn, almacenId, empresaId) || await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM alm_transa a JOIN trans_alma t ON t.trans_alma_id=a.trans_alma_id
            WHERE a.alm_transa_id=@Id AND a.almacen_id=@AlmacenId AND a.estado='A' AND t.empresa_id=@EmpresaId",
            new { Id = almacenTransaccionId, AlmacenId = almacenId, EmpresaId = empresaId }) != 1) return null;
        return (await conn.QueryAsync<AlmacenConfiguracionModel>(@"
            SELECT ISNULL(c.alm_trans_config_id,0) Id, @Id AlmacenTransaccionId, a.atrib_config_id AtributoId, a.nombre Nombre,
                   CAST(CASE WHEN c.visible='S' THEN 1 ELSE 0 END AS bit) Visible,
                   CAST(CASE WHEN c.requerido='S' THEN 1 ELSE 0 END AS bit) Requerido
            FROM atrib_config a LEFT JOIN alm_trans_config c ON c.atrib_config_id=a.atrib_config_id AND c.alm_transa_id=@Id AND c.estado='A'
            WHERE a.estado='A' ORDER BY a.orden,a.atrib_config_id", new { Id = almacenTransaccionId })).AsList();
    }

    public async Task<(bool success, string message, int id)> SaveAlmacenTransaccionAsync(AlmacenTransaccionModel model, int empresaId, int userId)
    {
        if (!Valid(model) || empresaId <= 0 || userId <= 0) return Failure("Seleccione una transacción válida.");
        using var conn = factory.CreateConnection(); using var tx = conn.BeginTransaction(IsolationLevel.Serializable);
        if (await LockAlmacenAsync(conn, tx, model.AlmacenId, empresaId) is null) return Failure("El almacén no está disponible.");
        if (await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM trans_alma WITH (HOLDLOCK) WHERE trans_alma_id=@Id AND empresa_id=@EmpresaId AND estado='A'", new { Id = model.TransaccionId, EmpresaId = empresaId }, tx) != 1)
            return Failure("Seleccione una transacción activa de la empresa actual.");
        if (model.Id > 0 && await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM alm_transa WHERE alm_transa_id=@Id AND almacen_id=@AlmacenId AND estado='A'", model, tx) != 1)
            return Failure("La relación indicada no pertenece al almacén.");
        if (await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM alm_transa WHERE almacen_id=@AlmacenId AND trans_alma_id=@TransaccionId AND estado='A' AND alm_transa_id<>@Id", model, tx) > 0)
            return Failure("La transacción ya está vinculada a este almacén.");
        int id = model.Id;
        var args = new DynamicParameters(new { model.AlmacenId, TransaccionAlmacenId = model.TransaccionId, UsuarioAccionId = userId });
        if (id == 0) id = await InsertAsync(conn, tx, "sp_alm_transa_insertar", args);
        else { args.Add("AlmacenTransaccionId", id); await conn.ExecuteAsync("sp_alm_transa_actualizar", args, tx, commandType: CommandType.StoredProcedure); }
        tx.Commit(); return (true, "Transacción vinculada correctamente.", id);
    }

    public async Task<(bool success, string message, int id)> AnularAlmacenTransaccionAsync(int almacenId, int id, int empresaId, int userId)
    {
        if (almacenId <= 0 || id <= 0 || empresaId <= 0 || userId <= 0) return Failure("Identificador no válido.");
        using var conn = factory.CreateConnection(); using var tx = conn.BeginTransaction(IsolationLevel.Serializable);
        if (await LockAlmacenAsync(conn, tx, almacenId, empresaId) is null || await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM alm_transa WHERE alm_transa_id=@Id AND almacen_id=@AlmacenId AND estado='A'", new { Id = id, AlmacenId = almacenId }, tx) != 1)
            return Failure("La relación no existe o ya está anulada.");
        await conn.ExecuteAsync("sp_alm_transa_eliminar", new { AlmacenTransaccionId = id, UsuarioAccionId = userId }, tx, commandType: CommandType.StoredProcedure);
        tx.Commit(); return (true, "Relación anulada correctamente.", id);
    }

    public async Task<(bool success, string message, int id)> SaveConfiguracionAsync(int almacenId, AlmacenConfiguracionModel model, int empresaId, int userId)
    {
        if (almacenId <= 0 || !Valid(model) || empresaId <= 0 || userId <= 0) return Failure("Seleccione un atributo y una transacción válidos.");
        using var conn = factory.CreateConnection(); using var tx = conn.BeginTransaction(IsolationLevel.Serializable);
        if (await LockAlmacenAsync(conn, tx, almacenId, empresaId) is null || await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM alm_transa a JOIN trans_alma t WITH (HOLDLOCK) ON t.trans_alma_id=a.trans_alma_id
            WHERE a.alm_transa_id=@Id AND a.almacen_id=@AlmacenId AND a.estado='A' AND t.empresa_id=@EmpresaId AND t.estado='A'",
            new { Id = model.AlmacenTransaccionId, AlmacenId = almacenId, EmpresaId = empresaId }, tx) != 1) return Failure("La transacción vinculada no está disponible.");
        if (await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM atrib_config WHERE atrib_config_id=@Id AND estado='A'", new { Id = model.AtributoId }, tx) != 1)
            return Failure("El atributo no está habilitado.");
        // Resolve identity from the scoped parent and attribute, never from a client-supplied row ID.
        int? id = await conn.QuerySingleOrDefaultAsync<int?>(@"
            SELECT alm_trans_config_id FROM alm_trans_config WITH (UPDLOCK,HOLDLOCK)
            WHERE alm_transa_id=@AlmacenTransaccionId AND atrib_config_id=@AtributoId AND estado='A'", model, tx);
        var args = new DynamicParameters(new { Visible = model.Visible ? "S" : "N", Requerido = model.Requerido ? "S" : "N", UsuarioAccionId = userId });
        if (id.HasValue) { args.Add("AlmacenTransaccionConfiguracionId", id.Value); await conn.ExecuteAsync("sp_alm_trans_config_actualizar", args, tx, commandType: CommandType.StoredProcedure); }
        else { args.Add("AlmacenTransaccionId", model.AlmacenTransaccionId); args.Add("AtributoConfiguracionId", model.AtributoId); id = await InsertAsync(conn, tx, "sp_alm_trans_config_insertar", args); }
        tx.Commit(); return (true, "Configuración guardada correctamente.", id.Value);
    }

    public async Task<(bool success, string message, int id)> AgregarSerieAsync(int almacenId, int documentoSerieId, int empresaId, int userId)
    {
        if (almacenId <= 0 || documentoSerieId <= 0 || empresaId <= 0 || userId <= 0) return Failure("Seleccione una serie válida.");
        using var conn = factory.CreateConnection(); using var tx = conn.BeginTransaction(IsolationLevel.Serializable);
        if (await LockAlmacenAsync(conn, tx, almacenId, empresaId) is null) return Failure("El almacén no está disponible.");
        if (await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM doc_com_serie d JOIN empresa_sede s ON s.empresa_sede_id=d.empresa_sede_id
            JOIN tipo_doc_com t ON t.tipo_doc_com_id=d.tipo_doc_com_id
            WHERE d.doc_com_serie_id=@Id AND s.empresa_id=@EmpresaId AND s.estado='A' AND d.estado='A' AND t.estado='A'",
            new { Id = documentoSerieId, EmpresaId = empresaId }, tx) != 1) return Failure("Seleccione una serie vigente de la empresa actual.");
        if (await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM almacen_serie WHERE almacen_id=@AlmacenId AND doc_com_serie_id=@Id AND estado='A'", new { AlmacenId = almacenId, Id = documentoSerieId }, tx) > 0)
            return Failure("La serie ya está vinculada al almacén.");
        int id = await InsertAsync(conn, tx, "sp_almacen_serie_insertar", new { AlmacenId = almacenId, DocumentoComercialSerieId = documentoSerieId, UsuarioAccionId = userId });
        tx.Commit(); return (true, "Serie vinculada correctamente.", id);
    }

    public async Task<(bool success, string message, int id)> AnularSerieAsync(int almacenId, int id, int empresaId, int userId)
    {
        if (almacenId <= 0 || id <= 0 || empresaId <= 0 || userId <= 0) return Failure("Identificador no válido.");
        using var conn = factory.CreateConnection(); using var tx = conn.BeginTransaction(IsolationLevel.Serializable);
        if (await LockAlmacenAsync(conn, tx, almacenId, empresaId) is null || await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM almacen_serie WHERE almacen_serie_id=@Id AND almacen_id=@AlmacenId AND estado='A'", new { Id = id, AlmacenId = almacenId }, tx) != 1)
            return Failure("La serie no existe o ya está anulada.");
        await conn.ExecuteAsync("sp_almacen_serie_eliminar", new { AlmacenSerieId = id, UsuarioAccionId = userId }, tx, commandType: CommandType.StoredProcedure);
        tx.Commit(); return (true, "Serie desvinculada correctamente.", id);
    }
}
