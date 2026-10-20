using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services
{
    public class MaestrosService : IMaestrosService
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public MaestrosService(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        // =========================================================================
        // 1. COLOR
        // =========================================================================
        public async Task<List<ColorModel>> GetColoresAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    color_id AS Id,
                    nombre AS Nombre,
                    estado AS Estado
                FROM color
                ORDER BY nombre ASC";

            var rows = await conn.QueryAsync<ColorModel>(sql);
            return rows.ToList();
        }

        public async Task<ColorModel?> GetColorByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT color_id AS Id, nombre AS Nombre, estado AS Estado FROM color WHERE color_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<ColorModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveColorAsync(ColorModel model, int userId)
        {
            if (string.IsNullOrWhiteSpace(model.Nombre))
                return (false, "El nombre del color es obligatorio.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            if (model.Id == 0)
            {
                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR color_seq;
                    INSERT INTO color (color_id, nombre, estado, usuario_creacion_id, fecha_creacion)
                    VALUES (@newId, @Nombre, @Estado, @UserId, GETDATE());
                    SELECT @newId;";
                int newId = await conn.ExecuteScalarAsync<int>(sql, new { Nombre = model.Nombre.Trim(), Estado = model.Estado ?? "A", UserId = userId });
                return (true, "Color registrado exitosamente.", newId);
            }
            else
            {
                const string sql = @"
                    UPDATE color 
                    SET nombre = @Nombre, estado = @Estado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                    WHERE color_id = @Id";
                await conn.ExecuteAsync(sql, new { Id = model.Id, Nombre = model.Nombre.Trim(), Estado = model.Estado ?? "A", UserId = userId });
                return (true, "Color actualizado exitosamente.", model.Id);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoColorAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var item = await conn.QueryFirstOrDefaultAsync<ColorModel>("SELECT color_id AS Id, estado AS Estado FROM color WHERE color_id = @Id", new { Id = id });
            if (item == null) return (false, "Registro no encontrado.", "I");

            string nuevoEstado = item.Estado == "A" ? "I" : "A";
            const string sql = "UPDATE color SET estado = @NuevoEstado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE color_id = @Id";
            await conn.ExecuteAsync(sql, new { Id = id, NuevoEstado = nuevoEstado, UserId = userId });
            return (true, $"Color {(nuevoEstado == "A" ? "activado" : "desactivado")} exitosamente.", nuevoEstado);
        }

        // =========================================================================
        // 2. UIT
        // =========================================================================
        public async Task<List<UitModel>> GetUitsAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    uit_id AS Id,
                    monto AS Monto,
                    fecha_alta AS FechaAlta,
                    fecha_baja AS FechaBaja,
                    estado AS Estado
                FROM uit
                ORDER BY fecha_alta DESC";

            var rows = await conn.QueryAsync<UitModel>(sql);
            return rows.ToList();
        }

        public async Task<UitModel?> GetUitByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT uit_id AS Id, monto AS Monto, fecha_alta AS FechaAlta, fecha_baja AS FechaBaja, estado AS Estado FROM uit WHERE uit_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<UitModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveUitAsync(UitModel model, int userId)
        {
            if (model.Monto <= 0)
                return (false, "El monto de la UIT debe ser mayor a cero.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            if (model.Id == 0)
            {
                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR uit_seq;
                    INSERT INTO uit (uit_id, monto, fecha_alta, fecha_baja, estado, usuario_creacion_id, fecha_creacion)
                    VALUES (@newId, @Monto, @FechaAlta, @FechaBaja, @Estado, @UserId, GETDATE());
                    SELECT @newId;";
                int newId = await conn.ExecuteScalarAsync<int>(sql, new { Monto = model.Monto, FechaAlta = model.FechaAlta, FechaBaja = model.FechaBaja, Estado = model.Estado ?? "A", UserId = userId });
                return (true, "UIT registrada exitosamente.", newId);
            }
            else
            {
                const string sql = @"
                    UPDATE uit 
                    SET monto = @Monto, fecha_alta = @FechaAlta, fecha_baja = @FechaBaja, estado = @Estado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                    WHERE uit_id = @Id";
                await conn.ExecuteAsync(sql, new { Id = model.Id, Monto = model.Monto, FechaAlta = model.FechaAlta, FechaBaja = model.FechaBaja, Estado = model.Estado ?? "A", UserId = userId });
                return (true, "UIT actualizada exitosamente.", model.Id);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoUitAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var item = await conn.QueryFirstOrDefaultAsync<UitModel>("SELECT uit_id AS Id, estado AS Estado FROM uit WHERE uit_id = @Id", new { Id = id });
            if (item == null) return (false, "Registro no encontrado.", "I");

            string nuevoEstado = item.Estado == "A" ? "I" : "A";
            const string sql = "UPDATE uit SET estado = @NuevoEstado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE uit_id = @Id";
            await conn.ExecuteAsync(sql, new { Id = id, NuevoEstado = nuevoEstado, UserId = userId });
            return (true, $"UIT {(nuevoEstado == "A" ? "activada" : "desactivada")} exitosamente.", nuevoEstado);
        }

        // =========================================================================
        // 3. REMUNERACIÓN MÍNIMA VITAL
        // =========================================================================
        public async Task<List<RemMinVitalModel>> GetRemMinVitalAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    rem_min_vit_id AS Id,
                    monto AS Monto,
                    fecha_alta AS FechaAlta,
                    fecha_baja AS FechaBaja,
                    estado AS Estado
                FROM rem_min_vit
                ORDER BY fecha_alta DESC";

            var rows = await conn.QueryAsync<RemMinVitalModel>(sql);
            return rows.ToList();
        }

        public async Task<RemMinVitalModel?> GetRemMinVitalByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT rem_min_vit_id AS Id, monto AS Monto, fecha_alta AS FechaAlta, fecha_baja AS FechaBaja, estado AS Estado FROM rem_min_vit WHERE rem_min_vit_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<RemMinVitalModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveRemMinVitalAsync(RemMinVitalModel model, int userId)
        {
            if (model.Monto <= 0)
                return (false, "El monto de la remuneración mínima vital debe ser mayor a cero.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            if (model.Id == 0)
            {
                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR rem_min_vit_seq;
                    INSERT INTO rem_min_vit (rem_min_vit_id, monto, fecha_alta, fecha_baja, estado, usuario_creacion_id, fecha_creacion)
                    VALUES (@newId, @Monto, @FechaAlta, @FechaBaja, @Estado, @UserId, GETDATE());
                    SELECT @newId;";
                int newId = await conn.ExecuteScalarAsync<int>(sql, new { Monto = model.Monto, FechaAlta = model.FechaAlta, FechaBaja = model.FechaBaja, Estado = model.Estado ?? "A", UserId = userId });
                return (true, "Remuneración mínima vital registrada exitosamente.", newId);
            }
            else
            {
                const string sql = @"
                    UPDATE rem_min_vit 
                    SET monto = @Monto, fecha_alta = @FechaAlta, fecha_baja = @FechaBaja, estado = @Estado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                    WHERE rem_min_vit_id = @Id";
                await conn.ExecuteAsync(sql, new { Id = model.Id, Monto = model.Monto, FechaAlta = model.FechaAlta, FechaBaja = model.FechaBaja, Estado = model.Estado ?? "A", UserId = userId });
                return (true, "Remuneración mínima vital actualizada exitosamente.", model.Id);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoRemMinVitalAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var item = await conn.QueryFirstOrDefaultAsync<RemMinVitalModel>("SELECT rem_min_vit_id AS Id, estado AS Estado FROM rem_min_vit WHERE rem_min_vit_id = @Id", new { Id = id });
            if (item == null) return (false, "Registro no encontrado.", "I");

            string nuevoEstado = item.Estado == "A" ? "I" : "A";
            const string sql = "UPDATE rem_min_vit SET estado = @NuevoEstado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE rem_min_vit_id = @Id";
            await conn.ExecuteAsync(sql, new { Id = id, NuevoEstado = nuevoEstado, UserId = userId });
            return (true, $"Remuneración mínima vital {(nuevoEstado == "A" ? "activada" : "desactivada")} exitosamente.", nuevoEstado);
        }

        // =========================================================================
        // 4. CENTRO DE COSTO
        // =========================================================================
        public async Task<List<CentroCostoModel>> GetCentrosCostoAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    centro_costo_id AS Id,
                    empresa_id AS EmpresaId,
                    nombre AS Nombre,
                    sigla AS Sigla,
                    estado AS Estado
                FROM centro_costo
                ORDER BY nombre ASC";

            var rows = await conn.QueryAsync<CentroCostoModel>(sql);
            return rows.ToList();
        }

        public async Task<CentroCostoModel?> GetCentroCostoByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT centro_costo_id AS Id, empresa_id AS EmpresaId, nombre AS Nombre, sigla AS Sigla, estado AS Estado FROM centro_costo WHERE centro_costo_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<CentroCostoModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveCentroCostoAsync(CentroCostoModel model, int userId)
        {
            if (string.IsNullOrWhiteSpace(model.Nombre))
                return (false, "El nombre del centro de costo es obligatorio.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            if (model.Id == 0)
            {
                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR centro_costo_seq;
                    INSERT INTO centro_costo (centro_costo_id, empresa_id, nombre, sigla, estado, usuario_creacion_id, fecha_creacion)
                    VALUES (@newId, @EmpresaId, @Nombre, @Sigla, @Estado, @UserId, GETDATE());
                    SELECT @newId;";
                int newId = await conn.ExecuteScalarAsync<int>(sql, new { EmpresaId = model.EmpresaId > 0 ? model.EmpresaId : 1, Nombre = model.Nombre.Trim(), Sigla = (model.Sigla ?? "").Trim(), Estado = model.Estado ?? "A", UserId = userId });
                return (true, "Centro de costo registrado exitosamente.", newId);
            }
            else
            {
                const string sql = @"
                    UPDATE centro_costo 
                    SET nombre = @Nombre, sigla = @Sigla, estado = @Estado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                    WHERE centro_costo_id = @Id";
                await conn.ExecuteAsync(sql, new { Id = model.Id, Nombre = model.Nombre.Trim(), Sigla = (model.Sigla ?? "").Trim(), Estado = model.Estado ?? "A", UserId = userId });
                return (true, "Centro de costo actualizado exitosamente.", model.Id);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoCentroCostoAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var item = await conn.QueryFirstOrDefaultAsync<CentroCostoModel>("SELECT centro_costo_id AS Id, estado AS Estado FROM centro_costo WHERE centro_costo_id = @Id", new { Id = id });
            if (item == null) return (false, "Registro no encontrado.", "I");

            string nuevoEstado = item.Estado == "A" ? "I" : "A";
            const string sql = "UPDATE centro_costo SET estado = @NuevoEstado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE centro_costo_id = @Id";
            await conn.ExecuteAsync(sql, new { Id = id, NuevoEstado = nuevoEstado, UserId = userId });
            return (true, $"Centro de costo {(nuevoEstado == "A" ? "activado" : "desactivado")} exitosamente.", nuevoEstado);
        }

        // =========================================================================
        // 5. CONDICIÓN DE PAGO
        // =========================================================================
        public async Task<List<CondPagoModel>> GetCondicionesPagoAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    cond_pago_id AS Id,
                    cond_pago AS Codigo,
                    nombre AS Nombre,
                    tipo_c_pago_id AS TipoCondicionPagoId,
                    dia AS Dias,
                    estado AS Estado
                FROM cond_pago
                ORDER BY dia ASC";

            var rows = await conn.QueryAsync<CondPagoModel>(sql);
            return rows.ToList();
        }

        public async Task<CondPagoModel?> GetCondPagoByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT cond_pago_id AS Id, cond_pago AS Codigo, nombre AS Nombre, tipo_c_pago_id AS TipoCondicionPagoId, dia AS Dias, estado AS Estado FROM cond_pago WHERE cond_pago_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<CondPagoModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveCondPagoAsync(CondPagoModel model, int userId)
        {
            if (string.IsNullOrWhiteSpace(model.Nombre))
                return (false, "El nombre de la condición de pago es obligatorio.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            if (model.Id == 0)
            {
                // Generar código de 3 letras si viene vacío
                string codigo = string.IsNullOrWhiteSpace(model.Codigo) 
                    ? (model.Dias == 0 ? "CON" : $"C{model.Dias:D2}") 
                    : model.Codigo.Trim().ToUpper();
                if (codigo.Length > 3) codigo = codigo.Substring(0, 3);

                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR cond_pago_seq;
                    INSERT INTO cond_pago (cond_pago_id, empresa_id, cond_pago, nombre, tipo_c_pago_id, dia, estado, usuario_creacion_id, fecha_creacion)
                    VALUES (@newId, 1, @Codigo, @Nombre, @TipoCondicionPagoId, @Dias, @Estado, @UserId, GETDATE());
                    SELECT @newId;";
                int newId = await conn.ExecuteScalarAsync<int>(sql, new { Codigo = codigo, Nombre = model.Nombre.Trim(), TipoCondicionPagoId = model.TipoCondicionPagoId > 0 ? model.TipoCondicionPagoId : 1, Dias = model.Dias, Estado = model.Estado ?? "A", UserId = userId });
                return (true, "Condición de pago registrada exitosamente.", newId);
            }
            else
            {
                const string sql = @"
                    UPDATE cond_pago 
                    SET nombre = @Nombre, dia = @Dias, estado = @Estado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                    WHERE cond_pago_id = @Id";
                await conn.ExecuteAsync(sql, new { Id = model.Id, Nombre = model.Nombre.Trim(), Dias = model.Dias, Estado = model.Estado ?? "A", UserId = userId });
                return (true, "Condición de pago actualizada exitosamente.", model.Id);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoCondPagoAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var item = await conn.QueryFirstOrDefaultAsync<CondPagoModel>("SELECT cond_pago_id AS Id, estado AS Estado FROM cond_pago WHERE cond_pago_id = @Id", new { Id = id });
            if (item == null) return (false, "Registro no encontrado.", "I");

            string nuevoEstado = item.Estado == "A" ? "I" : "A";
            const string sql = "UPDATE cond_pago SET estado = @NuevoEstado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE cond_pago_id = @Id";
            await conn.ExecuteAsync(sql, new { Id = id, NuevoEstado = nuevoEstado, UserId = userId });
            return (true, $"Condición de pago {(nuevoEstado == "A" ? "activada" : "desactivada")} exitosamente.", nuevoEstado);
        }

        // =========================================================================
        // 6. TIPO DE CAMBIO
        // =========================================================================
        public async Task<List<TipoCambioModel>> GetTiposCambioAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    tipo_cambio_id AS Id,
                    tipo_cambio AS Codigo,
                    nombre AS Nombre,
                    origen_registro_id AS OrigenRegistroId,
                    estado AS Estado
                FROM tipo_cambio
                ORDER BY nombre ASC";

            var rows = await conn.QueryAsync<TipoCambioModel>(sql);
            return rows.ToList();
        }

        public async Task<TipoCambioModel?> GetTipoCambioByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_cambio_id AS Id, tipo_cambio AS Codigo, nombre AS Nombre, origen_registro_id AS OrigenRegistroId, estado AS Estado FROM tipo_cambio WHERE tipo_cambio_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<TipoCambioModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveTipoCambioAsync(TipoCambioModel model, int userId)
        {
            if (string.IsNullOrWhiteSpace(model.Nombre))
                return (false, "El nombre del tipo de cambio es obligatorio.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            if (model.Id == 0)
            {
                string codigo = string.IsNullOrWhiteSpace(model.Codigo) ? "TC" : model.Codigo.Trim().ToUpper();
                if (codigo.Length > 2) codigo = codigo.Substring(0, 2);

                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR tipo_cambio_seq;
                    INSERT INTO tipo_cambio (tipo_cambio_id, tipo_cambio, nombre, origen_registro_id, estado, usuario_creacion_id, fecha_creacion)
                    VALUES (@newId, @Codigo, @Nombre, @OrigenRegistroId, @Estado, @UserId, GETDATE());
                    SELECT @newId;";
                int newId = await conn.ExecuteScalarAsync<int>(sql, new { Codigo = codigo, Nombre = model.Nombre.Trim(), OrigenRegistroId = model.OrigenRegistroId > 0 ? model.OrigenRegistroId : 1, Estado = model.Estado ?? "A", UserId = userId });
                return (true, "Tipo de cambio registrado exitosamente.", newId);
            }
            else
            {
                const string sql = @"
                    UPDATE tipo_cambio 
                    SET nombre = @Nombre, estado = @Estado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                    WHERE tipo_cambio_id = @Id";
                await conn.ExecuteAsync(sql, new { Id = model.Id, Nombre = model.Nombre.Trim(), Estado = model.Estado ?? "A", UserId = userId });
                return (true, "Tipo de cambio actualizado exitosamente.", model.Id);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoTipoCambioAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var item = await conn.QueryFirstOrDefaultAsync<TipoCambioModel>("SELECT tipo_cambio_id AS Id, estado AS Estado FROM tipo_cambio WHERE tipo_cambio_id = @Id", new { Id = id });
            if (item == null) return (false, "Registro no encontrado.", "I");

            string nuevoEstado = item.Estado == "A" ? "I" : "A";
            const string sql = "UPDATE tipo_cambio SET estado = @NuevoEstado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE tipo_cambio_id = @Id";
            await conn.ExecuteAsync(sql, new { Id = id, NuevoEstado = nuevoEstado, UserId = userId });
            return (true, $"Tipo de cambio {(nuevoEstado == "A" ? "activado" : "desactivado")} exitosamente.", nuevoEstado);
        }

        // =========================================================================
        // 7. FAMILIA DE PRODUCTO
        // =========================================================================
        public async Task<List<GrupoClaseFamiliaModel>> GetFamiliasProductoAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT TOP 300
                    g.grupo_producto_id AS GrupoId,
                    g.grupo_producto AS GrupoCodigo,
                    g.nombre AS GrupoNombre,
                    c.clase_producto_id AS ClaseId,
                    c.clase_producto AS ClaseCodigo,
                    c.nombre AS ClaseNombre,
                    f.familia_producto_id AS FamiliaId,
                    f.familia_producto AS FamiliaCodigo,
                    f.nombre AS FamiliaNombre,
                    f.correla AS Correlativo,
                    f.estado AS Estado
                FROM familia_producto f
                INNER JOIN clase_producto c ON f.clase_producto_id = c.clase_producto_id
                INNER JOIN grupo_producto g ON c.grupo_producto_id = g.grupo_producto_id
                ORDER BY g.nombre, c.nombre, f.nombre";

            var rows = await conn.QueryAsync<GrupoClaseFamiliaModel>(sql);
            return rows.ToList();
        }

        public async Task<GrupoClaseFamiliaModel?> GetFamiliaProductoByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    c.clase_producto_id AS ClaseId,
                    c.nombre AS ClaseNombre,
                    f.familia_producto_id AS FamiliaId,
                    f.familia_producto AS FamiliaCodigo,
                    f.nombre AS FamiliaNombre,
                    f.correla AS Correlativo,
                    f.estado AS Estado
                FROM familia_producto f
                INNER JOIN clase_producto c ON f.clase_producto_id = c.clase_producto_id
                WHERE f.familia_producto_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<GrupoClaseFamiliaModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveFamiliaProductoAsync(GrupoClaseFamiliaModel model, int userId)
        {
            if (string.IsNullOrWhiteSpace(model.FamiliaNombre))
                return (false, "El nombre de la familia es obligatorio.", 0);
            if (model.ClaseId <= 0)
                return (false, "Debe seleccionar una clase de producto.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            if (model.FamiliaId == 0)
            {
                string codigo = string.IsNullOrWhiteSpace(model.FamiliaCodigo) ? "FAM" : model.FamiliaCodigo.Trim().ToUpper();
                if (codigo.Length > 3) codigo = codigo.Substring(0, 3);

                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR familia_producto_seq;
                    INSERT INTO familia_producto (familia_producto_id, clase_producto_id, familia_producto, nombre, correla, estado, fecha_alta, usuario_creacion_id, fecha_creacion)
                    VALUES (@newId, @ClaseId, @Codigo, @Nombre, @Correlativo, @Estado, GETDATE(), @UserId, GETDATE());
                    SELECT @newId;";
                int newId = await conn.ExecuteScalarAsync<int>(sql, new { ClaseId = model.ClaseId, Codigo = codigo, Nombre = model.FamiliaNombre.Trim(), Correlativo = model.Correlativo > 0 ? model.Correlativo : 1, Estado = model.Estado ?? "A", UserId = userId });
                return (true, "Familia de producto registrada exitosamente.", newId);
            }
            else
            {
                const string sql = @"
                    UPDATE familia_producto 
                    SET nombre = @Nombre, clase_producto_id = @ClaseId, estado = @Estado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                    WHERE familia_producto_id = @Id";
                await conn.ExecuteAsync(sql, new { Id = model.FamiliaId, Nombre = model.FamiliaNombre.Trim(), ClaseId = model.ClaseId, Estado = model.Estado ?? "A", UserId = userId });
                return (true, "Familia de producto actualizada exitosamente.", model.FamiliaId);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoFamiliaProductoAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var item = await conn.QueryFirstOrDefaultAsync<GrupoClaseFamiliaModel>("SELECT familia_producto_id AS FamiliaId, estado AS Estado FROM familia_producto WHERE familia_producto_id = @Id", new { Id = id });
            if (item == null) return (false, "Registro no encontrado.", "I");

            string nuevoEstado = item.Estado == "A" ? "I" : "A";
            const string sql = "UPDATE familia_producto SET estado = @NuevoEstado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE familia_producto_id = @Id";
            await conn.ExecuteAsync(sql, new { Id = id, NuevoEstado = nuevoEstado, UserId = userId });
            return (true, $"Familia {(nuevoEstado == "A" ? "activada" : "desactivada")} exitosamente.", nuevoEstado);
        }

        public async Task<List<LookupItemModel>> GetClasesProductoLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT clase_producto_id AS Id, clase_producto AS Codigo, nombre AS Nombre FROM clase_producto WHERE estado = 'A' ORDER BY nombre";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        // =========================================================================
        // 8. PRODUCTO
        // =========================================================================
        public async Task<List<ProductoModel>> GetProductosAsync(string? search = null, int top = 100)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            string sql = $@"
                SELECT TOP ({top})
                    p.producto_id AS Id,
                    p.producto AS Codigo,
                    p.descripcion AS Descripcion,
                    ISNULL(p.cod_referencial, '') AS CodReferencial,
                    p.familia_producto_id AS FamiliaId,
                    ISNULL(f.nombre, 'Sin Familia') AS FamiliaNombre,
                    p.unidad_medida_id AS UnidadMedidaId,
                    ISNULL(u.unidad_medida, 'UND') AS UnidadMedida,
                    p.peso_neto AS PesoNeto,
                    p.peso_bruto AS PesoBruto,
                    p.valor_repo AS ValorReposicion,
                    p.color_id AS ColorId,
                    p.master_pack AS MasterPack,
                    p.estado AS Estado
                FROM producto p
                LEFT JOIN familia_producto f ON p.familia_producto_id = f.familia_producto_id
                LEFT JOIN unidad_medida u ON p.unidad_medida_id = u.unidad_medida_id
                WHERE (@Search IS NULL OR p.descripcion LIKE '%' + @Search + '%' OR p.producto LIKE '%' + @Search + '%' OR p.cod_referencial LIKE '%' + @Search + '%')
                ORDER BY p.producto_id DESC";

            var rows = await conn.QueryAsync<ProductoModel>(sql, new { Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim() });
            return rows.ToList();
        }

        public async Task<ProductoModel?> GetProductoByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    p.producto_id AS Id,
                    p.producto AS Codigo,
                    p.descripcion AS Descripcion,
                    ISNULL(p.cod_referencial, '') AS CodReferencial,
                    p.familia_producto_id AS FamiliaId,
                    ISNULL(f.nombre, 'Sin Familia') AS FamiliaNombre,
                    p.unidad_medida_id AS UnidadMedidaId,
                    ISNULL(u.unidad_medida, 'UND') AS UnidadMedida,
                    p.tipo_igv_id AS TipoIgvId,
                    p.peso_neto AS PesoNeto,
                    p.peso_bruto AS PesoBruto,
                    p.valor_repo AS ValorReposicion,
                    p.volumen AS Volumen,
                    p.color_id AS ColorId,
                    p.master_pack AS MasterPack,
                    p.estado AS Estado
                FROM producto p
                LEFT JOIN familia_producto f ON p.familia_producto_id = f.familia_producto_id
                LEFT JOIN unidad_medida u ON p.unidad_medida_id = u.unidad_medida_id
                WHERE p.producto_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<ProductoModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveProductoAsync(ProductoModel model, int userId)
        {
            if (string.IsNullOrWhiteSpace(model.Descripcion))
                return (false, "La descripción del producto es obligatoria.", 0);
            if (model.FamiliaId <= 0)
                return (false, "Debe seleccionar una familia de producto.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            if (model.Id == 0)
            {
                string codigo = string.IsNullOrWhiteSpace(model.Codigo) 
                    ? "PRD" + DateTime.Now.ToString("yyMMddHHmm") 
                    : model.Codigo.Trim().ToUpper();
                if (codigo.Length > 12) codigo = codigo.Substring(0, 12);

                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR producto_seq;
                    INSERT INTO producto (
                        producto_id, producto, descripcion, cod_referencial, familia_producto_id, unidad_medida_id,
                        tipo_igv_id, peso_neto, peso_bruto, valor_repo, volumen, tiene_pres_vta, es_opera, es_perecible,
                        detalle_req, master_pack, color_id, estado_producto_id, estado_com_prod_id, estado, fecha_alta,
                        usuario_creacion_id, fecha_creacion
                    )
                    VALUES (
                        @newId, @Codigo, @Descripcion, @CodReferencial, @FamiliaId, @UnidadMedidaId,
                        @TipoIgvId, @PesoNeto, @PesoBruto, @ValorReposicion, @Volumen, 'N', 'N', 'N',
                        'N', @MasterPack, @ColorId, 1, 1, @Estado, GETDATE(),
                        @UserId, GETDATE()
                    );
                    SELECT @newId;";
                int newId = await conn.ExecuteScalarAsync<int>(sql, new {
                    Codigo = codigo,
                    Descripcion = model.Descripcion.Trim(),
                    CodReferencial = (model.CodReferencial ?? "").Trim(),
                    FamiliaId = model.FamiliaId,
                    UnidadMedidaId = model.UnidadMedidaId > 0 ? model.UnidadMedidaId : 1,
                    TipoIgvId = model.TipoIgvId > 0 ? model.TipoIgvId : 1,
                    PesoNeto = model.PesoNeto ?? 0,
                    PesoBruto = model.PesoBruto ?? 0,
                    ValorReposicion = model.ValorReposicion ?? 0,
                    Volumen = model.Volumen ?? 0,
                    MasterPack = model.MasterPack > 0 ? model.MasterPack : 1,
                    ColorId = model.ColorId,
                    Estado = model.Estado ?? "A",
                    UserId = userId
                });
                return (true, "Producto registrado exitosamente.", newId);
            }
            else
            {
                const string sql = @"
                    UPDATE producto 
                    SET descripcion = @Descripcion,
                        cod_referencial = @CodReferencial,
                        familia_producto_id = @FamiliaId,
                        unidad_medida_id = @UnidadMedidaId,
                        tipo_igv_id = @TipoIgvId,
                        peso_neto = @PesoNeto,
                        peso_bruto = @PesoBruto,
                        valor_repo = @ValorReposicion,
                        color_id = @ColorId,
                        master_pack = @MasterPack,
                        estado = @Estado,
                        usuario_modifica_id = @UserId,
                        fecha_modifica = GETDATE()
                    WHERE producto_id = @Id";
                await conn.ExecuteAsync(sql, new {
                    Id = model.Id,
                    Descripcion = model.Descripcion.Trim(),
                    CodReferencial = (model.CodReferencial ?? "").Trim(),
                    FamiliaId = model.FamiliaId,
                    UnidadMedidaId = model.UnidadMedidaId > 0 ? model.UnidadMedidaId : 1,
                    TipoIgvId = model.TipoIgvId > 0 ? model.TipoIgvId : 1,
                    PesoNeto = model.PesoNeto ?? 0,
                    PesoBruto = model.PesoBruto ?? 0,
                    ValorReposicion = model.ValorReposicion ?? 0,
                    ColorId = model.ColorId,
                    MasterPack = model.MasterPack > 0 ? model.MasterPack : 1,
                    Estado = model.Estado ?? "A",
                    UserId = userId
                });
                return (true, "Producto actualizado exitosamente.", model.Id);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoProductoAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var item = await conn.QueryFirstOrDefaultAsync<ProductoModel>("SELECT producto_id AS Id, estado AS Estado FROM producto WHERE producto_id = @Id", new { Id = id });
            if (item == null) return (false, "Registro no encontrado.", "I");

            string nuevoEstado = item.Estado == "A" ? "I" : "A";
            const string sql = "UPDATE producto SET estado = @NuevoEstado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE producto_id = @Id";
            await conn.ExecuteAsync(sql, new { Id = id, NuevoEstado = nuevoEstado, UserId = userId });
            return (true, $"Producto {(nuevoEstado == "A" ? "activado" : "desactivado")} exitosamente.", nuevoEstado);
        }

        public async Task<List<LookupItemModel>> GetFamiliasProductoLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT familia_producto_id AS Id, familia_producto AS Codigo, nombre AS Nombre FROM familia_producto WHERE estado = 'A' ORDER BY nombre";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetUnidadesMedidaLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT unidad_medida_id AS Id, unidad_medida AS Codigo, nombre AS Nombre FROM unidad_medida WHERE estado = 'A' ORDER BY nombre";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetColoresLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT color_id AS Id, '' AS Codigo, nombre AS Nombre FROM color WHERE estado = 'A' ORDER BY nombre";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        // =========================================================================
        // 9. PROVEEDOR
        // =========================================================================
        public async Task<List<ProveedorModel>> GetProveedoresAsync(string? search = null, int top = 100)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            string sql = $@"
                SELECT TOP ({top})
                    proveedor_id AS Id,
                    proveedor AS Codigo,
                    ISNULL(razon_social, '') AS RazonSocial,
                    ISNULL(nombres, '') AS Nombres,
                    ISNULL(apellido_pat, '') AS ApellidoPat,
                    ISNULL(apellido_mat, '') AS ApellidoMat,
                    ISNULL(NULLIF(numero_ruc, ''), ISNULL(num_doc_iden, '')) AS Documento,
                    ISNULL(numero_ruc, '') AS NumeroRuc,
                    ISNULL(telefono, '') AS Telefono,
                    ISNULL(email, '') AS Email,
                    ISNULL(contacto, '') AS Contacto,
                    ISNULL(nombre_via, '') AS Direccion,
                    tipo_doc_iden_id AS TipoDocumentoIdentidadId,
                    estado AS Estado
                FROM proveedor
                WHERE (@Search IS NULL OR razon_social LIKE '%' + @Search + '%' OR nombres LIKE '%' + @Search + '%' OR proveedor LIKE '%' + @Search + '%' OR numero_ruc LIKE '%' + @Search + '%')
                ORDER BY proveedor_id DESC";

            var rows = await conn.QueryAsync<ProveedorModel>(sql, new { Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim() });
            return rows.ToList();
        }

        public async Task<ProveedorModel?> GetProveedorByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    proveedor_id AS Id,
                    proveedor AS Codigo,
                    ISNULL(razon_social, '') AS RazonSocial,
                    ISNULL(nombres, '') AS Nombres,
                    ISNULL(apellido_pat, '') AS ApellidoPat,
                    ISNULL(apellido_mat, '') AS ApellidoMat,
                    ISNULL(num_doc_iden, '') AS Documento,
                    ISNULL(numero_ruc, '') AS NumeroRuc,
                    ISNULL(telefono, '') AS Telefono,
                    ISNULL(email, '') AS Email,
                    ISNULL(contacto, '') AS Contacto,
                    ISNULL(nombre_via, '') AS Direccion,
                    tipo_doc_iden_id AS TipoDocumentoIdentidadId,
                    estado AS Estado
                FROM proveedor
                WHERE proveedor_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<ProveedorModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveProveedorAsync(ProveedorModel model, int userId)
        {
            string nombrePrincipal = !string.IsNullOrWhiteSpace(model.RazonSocial) ? model.RazonSocial.Trim() : $"{model.Nombres} {model.ApellidoPat}".Trim();
            if (string.IsNullOrWhiteSpace(nombrePrincipal))
                return (false, "La razón social o nombres del proveedor es obligatorio.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            if (model.Id == 0)
            {
                string ruc = (model.NumeroRuc ?? model.Documento ?? "").Trim();
                string doc = (model.Documento ?? model.NumeroRuc ?? "").Trim();
                string codigo = string.IsNullOrWhiteSpace(model.Codigo) 
                    ? (ruc.Length >= 7 ? ruc.Substring(ruc.Length - 7) : "PR" + DateTime.Now.ToString("mmss"))
                    : model.Codigo.Trim().ToUpper();
                if (codigo.Length > 7) codigo = codigo.Substring(0, 7);

                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR proveedor_seq;
                    INSERT INTO proveedor (
                        proveedor_id, empresa_id, proveedor, tipo_nat_juri_id, razon_social, abrev, apellido_pat, apellido_mat, nombres,
                        tipo_doc_iden_id, num_doc_iden, numero_ruc, numero_cci, contacto, tipo_via_id, nombre_via, numero_via,
                        dpto, interior, manzana, lote, kilometro, block, etapa, tipo_zona_id, nombre_zona, referencia, telefono,
                        distrito_id, email, web, tipo_prove_id, condicion_pago_id, origen_registro_id, es_local, es_prospecto,
                        num_doc_iden2, cargo, cod_externo, estado, usuario_creacion_id, fecha_creacion
                    )
                    VALUES (
                        @newId, 1, @Codigo, 1, @RazonSocial, '', @ApellidoPat, @ApellidoMat, @Nombres,
                        @TipoDocId, @Doc, @Ruc, '', @Contacto, 1, @Direccion, '',
                        '', '', '', '', '', '', '', 1, '', '', @Telefono,
                        1, @Email, '', 1, 1, 1, 'S', 'N',
                        '', '', '', @Estado, @UserId, GETDATE()
                    );
                    SELECT @newId;";
                int newId = await conn.ExecuteScalarAsync<int>(sql, new {
                    Codigo = codigo,
                    RazonSocial = (model.RazonSocial ?? "").Trim(),
                    ApellidoPat = (model.ApellidoPat ?? "").Trim(),
                    ApellidoMat = (model.ApellidoMat ?? "").Trim(),
                    Nombres = (model.Nombres ?? "").Trim(),
                    TipoDocId = model.TipoDocumentoIdentidadId > 0 ? model.TipoDocumentoIdentidadId : 6,
                    Doc = doc,
                    Ruc = ruc,
                    Contacto = (model.Contacto ?? "").Trim(),
                    Direccion = (model.Direccion ?? "").Trim(),
                    Telefono = (model.Telefono ?? "").Trim(),
                    Email = (model.Email ?? "").Trim(),
                    Estado = model.Estado ?? "A",
                    UserId = userId
                });
                return (true, "Proveedor registrado exitosamente.", newId);
            }
            else
            {
                const string sql = @"
                    UPDATE proveedor 
                    SET razon_social = @RazonSocial,
                        apellido_pat = @ApellidoPat,
                        apellido_mat = @ApellidoMat,
                        nombres = @Nombres,
                        numero_ruc = @Ruc,
                        num_doc_iden = @Doc,
                        telefono = @Telefono,
                        email = @Email,
                        contacto = @Contacto,
                        nombre_via = @Direccion,
                        estado = @Estado,
                        usuario_modifica_id = @UserId,
                        fecha_modifica = GETDATE()
                    WHERE proveedor_id = @Id";
                await conn.ExecuteAsync(sql, new {
                    Id = model.Id,
                    RazonSocial = (model.RazonSocial ?? "").Trim(),
                    ApellidoPat = (model.ApellidoPat ?? "").Trim(),
                    ApellidoMat = (model.ApellidoMat ?? "").Trim(),
                    Nombres = (model.Nombres ?? "").Trim(),
                    Ruc = (model.NumeroRuc ?? model.Documento ?? "").Trim(),
                    Doc = (model.Documento ?? model.NumeroRuc ?? "").Trim(),
                    Telefono = (model.Telefono ?? "").Trim(),
                    Email = (model.Email ?? "").Trim(),
                    Contacto = (model.Contacto ?? "").Trim(),
                    Direccion = (model.Direccion ?? "").Trim(),
                    Estado = model.Estado ?? "A",
                    UserId = userId
                });
                return (true, "Proveedor actualizado exitosamente.", model.Id);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoProveedorAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var item = await conn.QueryFirstOrDefaultAsync<ProveedorModel>("SELECT proveedor_id AS Id, estado AS Estado FROM proveedor WHERE proveedor_id = @Id", new { Id = id });
            if (item == null) return (false, "Registro no encontrado.", "I");

            string nuevoEstado = item.Estado == "A" ? "I" : "A";
            const string sql = "UPDATE proveedor SET estado = @NuevoEstado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE proveedor_id = @Id";
            await conn.ExecuteAsync(sql, new { Id = id, NuevoEstado = nuevoEstado, UserId = userId });
            return (true, $"Proveedor {(nuevoEstado == "A" ? "activado" : "desactivado")} exitosamente.", nuevoEstado);
        }

        public async Task<List<LookupItemModel>> GetTiposDocumentoIdentidadLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_doc_iden_id AS Id, tipo_doc_iden AS Codigo, nombre AS Nombre FROM tipo_doc_iden WHERE estado = 'A' ORDER BY nombre";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        // =========================================================================
        // 10. COMBO PRODUCTO
        // =========================================================================
        public async Task<List<ComboProductoModel>> GetCombosProductoAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT TOP 150
                    cp.combo_producto_id AS Id,
                    cp.producto_id AS ProductoId,
                    ISNULL(p1.descripcion, 'Producto ' + CAST(cp.producto_id AS VARCHAR)) AS ProductoPrincipal,
                    cp.prod_inclu_id AS ProdIncluId,
                    ISNULL(p2.descripcion, 'Incluido ' + CAST(cp.prod_inclu_id AS VARCHAR)) AS ProductoIncluido,
                    cp.cantidad AS Cantidad,
                    cp.porc_precio AS PorcentajePrecio,
                    cp.num_dec_redo AS NumDecRedo,
                    cp.estado AS Estado
                FROM combo_producto cp
                LEFT JOIN producto p1 ON cp.producto_id = p1.producto_id
                LEFT JOIN producto p2 ON cp.prod_inclu_id = p2.producto_id
                ORDER BY cp.combo_producto_id DESC";

            var rows = await conn.QueryAsync<ComboProductoModel>(sql);
            return rows.ToList();
        }

        public async Task<ComboProductoModel?> GetComboProductoByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    cp.combo_producto_id AS Id,
                    cp.producto_id AS ProductoId,
                    ISNULL(p1.descripcion, 'Producto ' + CAST(cp.producto_id AS VARCHAR)) AS ProductoPrincipal,
                    cp.prod_inclu_id AS ProdIncluId,
                    ISNULL(p2.descripcion, 'Incluido ' + CAST(cp.prod_inclu_id AS VARCHAR)) AS ProductoIncluido,
                    cp.cantidad AS Cantidad,
                    cp.porc_precio AS PorcentajePrecio,
                    cp.num_dec_redo AS NumDecRedo,
                    cp.estado AS Estado
                FROM combo_producto cp
                LEFT JOIN producto p1 ON cp.producto_id = p1.producto_id
                LEFT JOIN producto p2 ON cp.prod_inclu_id = p2.producto_id
                WHERE cp.combo_producto_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<ComboProductoModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveComboProductoAsync(ComboProductoModel model, int userId)
        {
            if (model.ProductoId <= 0 || model.ProdIncluId <= 0)
                return (false, "Debe seleccionar el producto principal y el producto incluido.", 0);
            if (model.Cantidad <= 0)
                return (false, "La cantidad del producto incluido debe ser mayor a cero.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            if (model.Id == 0)
            {
                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR combo_producto_seq;
                    INSERT INTO combo_producto (combo_producto_id, producto_id, prod_inclu_id, cantidad, porc_precio, num_dec_redo, estado, usuario_creacion_id, fecha_creacion)
                    VALUES (@newId, @ProductoId, @ProdIncluId, @Cantidad, @PorcentajePrecio, @NumDecRedo, @Estado, @UserId, GETDATE());
                    SELECT @newId;";
                int newId = await conn.ExecuteScalarAsync<int>(sql, new {
                    ProductoId = model.ProductoId,
                    ProdIncluId = model.ProdIncluId,
                    Cantidad = model.Cantidad,
                    PorcentajePrecio = model.PorcentajePrecio > 0 ? model.PorcentajePrecio : 100,
                    NumDecRedo = model.NumDecRedo >= 0 ? model.NumDecRedo : 2,
                    Estado = model.Estado ?? "A",
                    UserId = userId
                });
                return (true, "Detalle de combo registrado exitosamente.", newId);
            }
            else
            {
                const string sql = @"
                    UPDATE combo_producto 
                    SET cantidad = @Cantidad, porc_precio = @PorcentajePrecio, num_dec_redo = @NumDecRedo, estado = @Estado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                    WHERE combo_producto_id = @Id";
                await conn.ExecuteAsync(sql, new {
                    Id = model.Id,
                    Cantidad = model.Cantidad,
                    PorcentajePrecio = model.PorcentajePrecio,
                    NumDecRedo = model.NumDecRedo,
                    Estado = model.Estado ?? "A",
                    UserId = userId
                });
                return (true, "Detalle de combo actualizado exitosamente.", model.Id);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoComboProductoAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var item = await conn.QueryFirstOrDefaultAsync<ComboProductoModel>("SELECT combo_producto_id AS Id, estado AS Estado FROM combo_producto WHERE combo_producto_id = @Id", new { Id = id });
            if (item == null) return (false, "Registro no encontrado.", "I");

            string nuevoEstado = item.Estado == "A" ? "I" : "A";
            const string sql = "UPDATE combo_producto SET estado = @NuevoEstado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE combo_producto_id = @Id";
            await conn.ExecuteAsync(sql, new { Id = id, NuevoEstado = nuevoEstado, UserId = userId });
            return (true, $"Combo {(nuevoEstado == "A" ? "activado" : "desactivado")} exitosamente.", nuevoEstado);
        }

        public async Task<List<LookupItemModel>> GetProductosLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT TOP 500 producto_id AS Id, producto AS Codigo, descripcion AS Nombre FROM producto WHERE estado = 'A' ORDER BY descripcion";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        // =========================================================================
        // 11. TABLA GENERAL
        // =========================================================================
        public async Task<List<TablaGeneralModel>> GetTablasGeneralesAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    tabla_id AS Id,
                    tabla AS Tabla,
                    nombre AS Nombre,
                    ISNULL(prefijo, '') AS Prefijo,
                    correlativo AS Correlativo,
                    estado AS Estado
                FROM tabla
                ORDER BY nombre ASC";

            var rows = await conn.QueryAsync<TablaGeneralModel>(sql);
            return rows.ToList();
        }

        public async Task<TablaGeneralModel?> GetTablaGeneralByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tabla_id AS Id, tabla AS Tabla, nombre AS Nombre, ISNULL(prefijo, '') AS Prefijo, correlativo AS Correlativo, estado AS Estado FROM tabla WHERE tabla_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<TablaGeneralModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveTablaGeneralAsync(TablaGeneralModel model, int userId)
        {
            if (string.IsNullOrWhiteSpace(model.Nombre))
                return (false, "El nombre de la tabla es obligatorio.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            if (model.Id == 0)
            {
                string codigo = string.IsNullOrWhiteSpace(model.Tabla) ? "TAB" : model.Tabla.Trim().ToUpper();
                if (codigo.Length > 4) codigo = codigo.Substring(0, 4);

                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR tabla_seq;
                    INSERT INTO tabla (tabla_id, tabla, nombre, corre_por_emp, corre_por_emp_sede, correlativo, prefijo, formato_corre, estado, usuario_creacion_id, fecha_creacion)
                    VALUES (@newId, @Codigo, @Nombre, 'N', 'N', @Correlativo, @Prefijo, '000000', @Estado, @UserId, GETDATE());
                    SELECT @newId;";
                int newId = await conn.ExecuteScalarAsync<int>(sql, new {
                    Codigo = codigo,
                    Nombre = model.Nombre.Trim(),
                    Correlativo = model.Correlativo > 0 ? model.Correlativo : 1,
                    Prefijo = (model.Prefijo ?? "").Trim(),
                    Estado = model.Estado ?? "A",
                    UserId = userId
                });
                return (true, "Tabla general registrada exitosamente.", newId);
            }
            else
            {
                const string sql = @"
                    UPDATE tabla 
                    SET nombre = @Nombre, prefijo = @Prefijo, correlativo = @Correlativo, estado = @Estado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                    WHERE tabla_id = @Id";
                await conn.ExecuteAsync(sql, new {
                    Id = model.Id,
                    Nombre = model.Nombre.Trim(),
                    Prefijo = (model.Prefijo ?? "").Trim(),
                    Correlativo = model.Correlativo,
                    Estado = model.Estado ?? "A",
                    UserId = userId
                });
                return (true, "Tabla general actualizada exitosamente.", model.Id);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoTablaGeneralAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var item = await conn.QueryFirstOrDefaultAsync<TablaGeneralModel>("SELECT tabla_id AS Id, estado AS Estado FROM tabla WHERE tabla_id = @Id", new { Id = id });
            if (item == null) return (false, "Registro no encontrado.", "I");

            string nuevoEstado = item.Estado == "A" ? "I" : "A";
            const string sql = "UPDATE tabla SET estado = @NuevoEstado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE tabla_id = @Id";
            await conn.ExecuteAsync(sql, new { Id = id, NuevoEstado = nuevoEstado, UserId = userId });
            return (true, $"Tabla {(nuevoEstado == "A" ? "activada" : "desactivada")} exitosamente.", nuevoEstado);
        }
    }
}
