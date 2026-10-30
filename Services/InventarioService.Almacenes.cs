using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services;

public partial class InventarioService
{
    private const string AlmacenSelect = @"
        SELECT a.almacen_id Id, a.empresa_sede_id EmpresaSedeId, s.nombre SedeNombre, a.nombre Nombre,
               a.abrev Abreviatura, CAST(CASE WHEN a.es_venta='S' THEN 1 ELSE 0 END AS bit) EsVenta,
               a.tipo_via_id TipoViaId, a.nombre_via NombreVia, a.numero_via NumeroVia, a.dpto DepartamentoInterior,
               a.interior Interior, a.manzana Manzana, a.lote Lote, a.kilometro Kilometro, a.block Block, a.etapa Etapa,
               a.tipo_zona_id TipoZonaId, a.nombre_zona NombreZona, a.distrito_id DistritoId,
               p.provincia_id ProvinciaId, p.departamento_id DepartamentoId,
               RTRIM(a.codigo_sunat) CodigoSunat, a.servidor_imp ServidorImpresion, a.estado Estado,
               CONCAT(tv.abrev,' ',a.nombre_via,' ',a.numero_via,' ',tz.abrev,' ',a.nombre_zona,' - ',d.nombre) Direccion
        FROM almacen a
        JOIN empresa_sede s ON s.empresa_sede_id=a.empresa_sede_id
        JOIN distrito d ON d.distrito_id=a.distrito_id
        JOIN provincia p ON p.provincia_id=d.provincia_id
        JOIN tipo_via tv ON tv.tipo_via_id=a.tipo_via_id
        JOIN tipo_zona tz ON tz.tipo_zona_id=a.tipo_zona_id ";

    public async Task<AlmacenPageModel> GetAlmacenesAsync(int empresaId, string? search = null, int? sedeId = null, string? estado = null, int page = 1, int top = 25, string? sort = null, string? dir = null)
    {
        var columns = new Dictionary<string, string> {
            ["id"]="a.almacen_id", ["sede"]="s.nombre", ["nombre"]="a.nombre", ["abreviatura"]="a.abrev",
            ["direccion"]="Direccion", ["venta"]="a.es_venta", ["estado"]="a.estado"
        };
        var model = new AlmacenPageModel(); model.Ordenar(sort, dir, columns.Keys, "nombre");
        const string filter = @"WHERE s.empresa_id=@EmpresaId AND (@SedeId IS NULL OR a.empresa_sede_id=@SedeId)
            AND (@Search IS NULL OR a.nombre LIKE @Search OR a.abrev LIKE @Search OR a.nombre_via LIKE @Search)
            AND (@Estado IS NULL OR a.estado=@Estado)";
        using var conn = factory.CreateConnection();
        var args = new DynamicParameters(new { EmpresaId = empresaId, SedeId = sedeId, Search = Search(search), Estado = State(estado) });
        model.Paginar(await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM almacen a JOIN empresa_sede s ON s.empresa_sede_id=a.empresa_sede_id " + filter, args), page, top);
        args.Add("Offset", (model.Page - 1) * model.PageSize); args.Add("Top", model.PageSize);
        model.Items = (await conn.QueryAsync<AlmacenModel>(AlmacenSelect + filter +
            $" ORDER BY {columns[model.Sort]} {model.Dir}{(model.Sort == "id" ? "" : ", a.almacen_id ASC")} OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY", args)).AsList();
        model.VisibleCount = model.Items.Count;
        model.Sedes = (await conn.QueryAsync<LookupItemModel>("SELECT empresa_sede_id Id, nombre Nombre FROM empresa_sede WHERE empresa_id=@EmpresaId AND estado='A' ORDER BY nombre", new { EmpresaId = empresaId })).AsList();
        model.TiposVia = await maestros.GetTiposViaLookupAsync();
        model.TiposZona = await maestros.GetTiposZonaLookupAsync();
        model.Departamentos = await maestros.GetDepartamentosLookupAsync();
        return model;
    }

    public async Task<AlmacenModel?> GetAlmacenAsync(int id, int empresaId)
    {
        using var conn = factory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<AlmacenModel>(AlmacenSelect + "WHERE a.almacen_id=@Id AND s.empresa_id=@EmpresaId", new { Id = id, EmpresaId = empresaId });
    }

    public async Task<(bool success, string message, int id)> SaveAlmacenAsync(AlmacenModel model, int empresaId, int userId)
    {
        model.Nombre = Text(model.Nombre); model.Abreviatura = Text(model.Abreviatura);
        if (!Valid(model) || empresaId <= 0 || userId <= 0) return Failure("Revise los datos obligatorios y las longitudes del almacén.");
        using var conn = factory.CreateConnection(); using var tx = conn.BeginTransaction(IsolationLevel.Serializable);
        var old = model.Id == 0 ? null : await LockAlmacenAsync(conn, tx, model.Id, empresaId);
        if (model.Id > 0 && old is null) return Failure("El almacén no existe, está anulado o su sede está inactiva.");
        if (old is not null && old.EmpresaSedeId != model.EmpresaSedeId) return Failure("La sede no puede cambiar después del alta.");
        if (await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM empresa_sede WHERE empresa_sede_id=@Id AND empresa_id=@EmpresaId AND estado='A'", new { Id = model.EmpresaSedeId, EmpresaId = empresaId }, tx) != 1)
            return Failure("Seleccione una sede vigente de la empresa actual.");
        if (await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM tipo_via v CROSS JOIN tipo_zona z CROSS JOIN distrito d
            JOIN provincia p ON p.provincia_id=d.provincia_id JOIN departamento dep ON dep.departamento_id=p.departamento_id
            WHERE v.tipo_via_id=@TipoViaId AND z.tipo_zona_id=@TipoZonaId AND d.distrito_id=@DistritoId
              AND v.estado='A' AND z.estado='A' AND d.estado='A' AND p.estado='A' AND dep.estado='A'", model, tx) != 1)
            return Failure("Seleccione tipos de vía, zona y un distrito vigentes.");
        var values = new DynamicParameters(new {
            model.Nombre, model.Abreviatura, EsVenta = model.EsVenta ? "S" : "N", model.TipoViaId,
            NombreVia = Text(model.NombreVia), NumeroVia = Text(model.NumeroVia), Departamento = Text(model.DepartamentoInterior),
            Interior = Text(model.Interior), Manzana = Text(model.Manzana), Lote = Text(model.Lote), Kilometro = Text(model.Kilometro),
            Block = Text(model.Block), Etapa = Text(model.Etapa), model.TipoZonaId, NombreZona = Text(model.NombreZona),
            model.DistritoId, CodigoSunat = Text(model.CodigoSunat), ServidorImpresion = Text(model.ServidorImpresion), UsuarioAccionId = userId
        });
        int id = model.Id;
        if (id == 0)
        {
            int? origin = await conn.QuerySingleOrDefaultAsync<int?>("SELECT origen_registro_id FROM origen_registro WHERE origen_registro='02' AND estado='A'", transaction: tx);
            if (!origin.HasValue) return Failure("No está configurado el origen de registro de usuario.");
            values.Add("EmpresaSedeId", model.EmpresaSedeId); values.Add("OrigenRegistroId", origin.Value);
            id = await InsertAsync(conn, tx, "sp_almacen_insertar", values);
        }
        else
        {
            values.Add("AlmacenId", id);
            await conn.ExecuteAsync("sp_almacen_actualizar", values, tx, commandType: CommandType.StoredProcedure);
        }
        tx.Commit(); return (true, "Almacén guardado correctamente.", id);
    }

    public async Task<(bool success, string message, int id)> AnularAlmacenAsync(int id, int empresaId, int userId)
    {
        if (id <= 0 || empresaId <= 0 || userId <= 0) return Failure("Identificador no válido.");
        using var conn = factory.CreateConnection(); using var tx = conn.BeginTransaction(IsolationLevel.Serializable);
        if (await LockAlmacenAsync(conn, tx, id, empresaId) is null) return Failure("El almacén no existe o ya está anulado.");
        await conn.ExecuteAsync("sp_almacen_eliminar", new { AlmacenId = id, UsuarioAccionId = userId }, tx, commandType: CommandType.StoredProcedure);
        tx.Commit(); return (true, "Almacén anulado correctamente.", id);
    }
}
