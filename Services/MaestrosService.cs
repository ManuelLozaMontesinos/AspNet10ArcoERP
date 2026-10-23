using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services
{
    public class MaestrosService : IMaestrosService
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        private sealed class SkuFamilyData
        {
            public string GrupoCodigo { get; set; } = string.Empty;
            public string ClaseCodigo { get; set; } = string.Empty;
            public string FamiliaCodigo { get; set; } = string.Empty;
            public int Correlativo { get; set; }
            public int MaximoAsignado { get; set; }
        }

        public MaestrosService(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        private async Task<(bool success, string message, string codigo)> GenerarCodigoSkuAsync(
            IDbConnection conn,
            IDbTransaction transaction,
            int familiaId,
            int? tipoProductoId,
            bool requireComboGroup = false)
        {
            const string sql = @"
                SELECT RTRIM(g.grupo_producto) AS GrupoCodigo,
                       RTRIM(c.clase_producto) AS ClaseCodigo,
                       RTRIM(f.familia_producto) AS FamiliaCodigo,
                       ISNULL(f.correla, 0) AS Correlativo,
                       ISNULL(MAX(TRY_CONVERT(int, RIGHT(RTRIM(p.producto), 3))), 0) AS MaximoAsignado
                FROM familia_producto f WITH (UPDLOCK, HOLDLOCK)
                INNER JOIN clase_producto c ON c.clase_producto_id = f.clase_producto_id
                INNER JOIN grupo_producto g ON g.grupo_producto_id = c.grupo_producto_id
                LEFT JOIN producto p ON p.familia_producto_id = f.familia_producto_id
                    AND LEFT(RTRIM(p.producto), 9) = RTRIM(g.grupo_producto) + RTRIM(c.clase_producto) + RTRIM(f.familia_producto)
                WHERE f.familia_producto_id = @FamiliaId
                    AND f.estado = 'A' AND c.estado = 'A' AND g.estado = 'A'
                    AND (@TipoProductoId IS NULL OR g.tipo_producto_id = @TipoProductoId)
                    AND (@RequireComboGroup = 0 OR g.es_combo = 'S' OR g.tipo_producto_id = 2)
                GROUP BY g.grupo_producto, c.clase_producto, f.familia_producto, f.correla";
            var family = await conn.QueryFirstOrDefaultAsync<SkuFamilyData>(
                sql,
                new { FamiliaId = familiaId, TipoProductoId = tipoProductoId, RequireComboGroup = requireComboGroup },
                transaction);

            if (family == null)
                return (false, "La familia o su jerarquía no está activa o no corresponde al tipo seleccionado.", string.Empty);

            int next = Math.Max(family.Correlativo, family.MaximoAsignado) + 1;
            if (next > 999)
                return (false, "La familia agotó su correlativo SKU de tres dígitos.", string.Empty);

            string codigo = $"{family.GrupoCodigo}{family.ClaseCodigo}{family.FamiliaCodigo}{next:D3}";
            if (codigo.Length != 12)
                return (false, "La jerarquía de la familia no tiene códigos SKU válidos de tres caracteres.", string.Empty);

            await conn.ExecuteAsync(
                "UPDATE familia_producto SET correla = @Correlativo WHERE familia_producto_id = @FamiliaId",
                new { Correlativo = next, FamiliaId = familiaId },
                transaction);
            return (true, string.Empty, codigo);
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
        public async Task<List<CentroCostoModel>> GetCentrosCostoAsync(int empresaId = 1)
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
                WHERE empresa_id = @EmpresaId
                ORDER BY nombre ASC";

            var rows = await conn.QueryAsync<CentroCostoModel>(sql, new { EmpresaId = empresaId });
            return rows.ToList();
        }

        public async Task<CentroCostoModel?> GetCentroCostoByIdAsync(int id, int empresaId = 1)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT centro_costo_id AS Id, empresa_id AS EmpresaId, nombre AS Nombre, sigla AS Sigla, estado AS Estado FROM centro_costo WHERE centro_costo_id = @Id AND empresa_id = @EmpresaId";
            return await conn.QueryFirstOrDefaultAsync<CentroCostoModel>(sql, new { Id = id, EmpresaId = empresaId });
        }

        public async Task<(bool success, string message, int id)> SaveCentroCostoAsync(CentroCostoModel model, int userId, int empresaId = 1)
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
                int newId = await conn.ExecuteScalarAsync<int>(sql, new { EmpresaId = empresaId, Nombre = model.Nombre.Trim(), Sigla = (model.Sigla ?? "").Trim(), Estado = model.Estado ?? "A", UserId = userId });
                return (true, "Centro de costo registrado exitosamente.", newId);
            }
            else
            {
                const string sql = @"
                    UPDATE centro_costo 
                    SET nombre = @Nombre, sigla = @Sigla, estado = @Estado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                    WHERE centro_costo_id = @Id AND empresa_id = @EmpresaId";
                int updated = await conn.ExecuteAsync(sql, new { Id = model.Id, EmpresaId = empresaId, Nombre = model.Nombre.Trim(), Sigla = (model.Sigla ?? "").Trim(), Estado = model.Estado ?? "A", UserId = userId });
                return updated == 0 ? (false, "Centro de costo no encontrado para la empresa activa.", 0) : (true, "Centro de costo actualizado exitosamente.", model.Id);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoCentroCostoAsync(int id, int userId, int empresaId = 1)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var item = await conn.QueryFirstOrDefaultAsync<CentroCostoModel>("SELECT centro_costo_id AS Id, estado AS Estado FROM centro_costo WHERE centro_costo_id = @Id AND empresa_id = @EmpresaId", new { Id = id, EmpresaId = empresaId });
            if (item == null) return (false, "Registro no encontrado.", "I");

            string nuevoEstado = item.Estado == "A" ? "I" : "A";
            const string sql = "UPDATE centro_costo SET estado = @NuevoEstado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE centro_costo_id = @Id AND empresa_id = @EmpresaId";
            await conn.ExecuteAsync(sql, new { Id = id, EmpresaId = empresaId, NuevoEstado = nuevoEstado, UserId = userId });
            return (true, $"Centro de costo {(nuevoEstado == "A" ? "activado" : "desactivado")} exitosamente.", nuevoEstado);
        }

        // =========================================================================
        // 5. CONDICIÓN DE PAGO
        // =========================================================================
        public async Task<List<CondPagoModel>> GetCondicionesPagoAsync(int empresaId = 1)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    cp.cond_pago_id AS Id,
                    cp.cond_pago AS Codigo,
                    cp.nombre AS Nombre,
                    cp.tipo_c_pago_id AS TipoCondicionPagoId,
                    ISNULL(tcp.nombre, '') AS TipoCondicionPagoNombre,
                    cp.dia AS Dias,
                    cp.estado AS Estado
                FROM cond_pago cp
                INNER JOIN tipo_c_pago tcp ON tcp.tipo_c_pago_id = cp.tipo_c_pago_id
                WHERE cp.empresa_id = @EmpresaId
                ORDER BY cp.dia ASC";

            var rows = await conn.QueryAsync<CondPagoModel>(sql, new { EmpresaId = empresaId });
            return rows.ToList();
        }

        public async Task<CondPagoModel?> GetCondPagoByIdAsync(int id, int empresaId = 1)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT cp.cond_pago_id AS Id, cp.cond_pago AS Codigo, cp.nombre AS Nombre, cp.tipo_c_pago_id AS TipoCondicionPagoId, ISNULL(tcp.nombre, '') AS TipoCondicionPagoNombre, cp.dia AS Dias, cp.estado AS Estado FROM cond_pago cp INNER JOIN tipo_c_pago tcp ON tcp.tipo_c_pago_id = cp.tipo_c_pago_id WHERE cp.cond_pago_id = @Id AND cp.empresa_id = @EmpresaId";
            return await conn.QueryFirstOrDefaultAsync<CondPagoModel>(sql, new { Id = id, EmpresaId = empresaId });
        }

        public async Task<(bool success, string message, int id)> SaveCondPagoAsync(CondPagoModel model, int userId, int empresaId = 1)
        {
            if (string.IsNullOrWhiteSpace(model.Nombre))
                return (false, "El nombre de la condición de pago es obligatorio.", 0);
            if (model.TipoCondicionPagoId <= 0)
                return (false, "Debe seleccionar el tipo de condición de pago.", 0);

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
                    VALUES (@newId, @EmpresaId, @Codigo, @Nombre, @TipoCondicionPagoId, @Dias, @Estado, @UserId, GETDATE());
                    SELECT @newId;";
                int newId = await conn.ExecuteScalarAsync<int>(sql, new { EmpresaId = empresaId, Codigo = codigo, Nombre = model.Nombre.Trim(), TipoCondicionPagoId = model.TipoCondicionPagoId > 0 ? model.TipoCondicionPagoId : 1, Dias = model.Dias, Estado = model.Estado ?? "A", UserId = userId });
                return (true, "Condición de pago registrada exitosamente.", newId);
            }
            else
            {
                const string sql = @"
                    UPDATE cond_pago 
                    SET nombre = @Nombre, tipo_c_pago_id = @TipoCondicionPagoId, dia = @Dias, estado = @Estado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                    WHERE cond_pago_id = @Id AND empresa_id = @EmpresaId";
                int updated = await conn.ExecuteAsync(sql, new { Id = model.Id, EmpresaId = empresaId, Nombre = model.Nombre.Trim(), TipoCondicionPagoId = model.TipoCondicionPagoId, Dias = model.Dias, Estado = model.Estado ?? "A", UserId = userId });
                return updated == 0 ? (false, "Condición de pago no encontrada para la empresa activa.", 0) : (true, "Condición de pago actualizada exitosamente.", model.Id);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoCondPagoAsync(int id, int userId, int empresaId = 1)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var item = await conn.QueryFirstOrDefaultAsync<CondPagoModel>("SELECT cond_pago_id AS Id, estado AS Estado FROM cond_pago WHERE cond_pago_id = @Id AND empresa_id = @EmpresaId", new { Id = id, EmpresaId = empresaId });
            if (item == null) return (false, "Registro no encontrado.", "I");

            string nuevoEstado = item.Estado == "A" ? "I" : "A";
            const string sql = "UPDATE cond_pago SET estado = @NuevoEstado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE cond_pago_id = @Id AND empresa_id = @EmpresaId";
            await conn.ExecuteAsync(sql, new { Id = id, EmpresaId = empresaId, NuevoEstado = nuevoEstado, UserId = userId });
            return (true, $"Condición de pago {(nuevoEstado == "A" ? "activada" : "desactivada")} exitosamente.", nuevoEstado);
        }

        public async Task<List<LookupItemModel>> GetTiposCondicionPagoLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_c_pago_id AS Id, tipo_c_pago AS Codigo, nombre AS Nombre FROM tipo_c_pago WHERE estado = 'A' ORDER BY nombre";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        // =========================================================================
        // 6. TIPO DE CAMBIO (tipo_camb_dia)
        // =========================================================================
        public async Task<List<TipoCambioModel>> GetTiposCambioAsync(string? search = null, int? tipo = null, string? estado = null, int top = 25)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var p = new DynamicParameters();
            int safeTop = top is 25 or 50 or 100 or 500 ? top : 25;
            string topClause = $"TOP ({safeTop}) ";
            string sql = $@"
                SELECT {topClause}
                    tcd.tipo_camb_dia_id AS Id,
                    tcd.tipo_cambio_id AS TipoCambioId,
                    tc.nombre AS TipoCambioNombre,
                    tcd.fecha AS Fecha,
                    tcd.moneda_id AS MonedaId,
                    m.nombre AS MonedaNombre,
                    m.simbolo AS MonedaSimbolo,
                    tcd.monto_c AS MontoCompra,
                    tcd.monto_v AS MontoVenta,
                    tcd.estado AS Estado
                FROM tipo_camb_dia tcd
                INNER JOIN tipo_cambio tc ON tc.tipo_cambio_id = tcd.tipo_cambio_id
                INNER JOIN moneda m ON m.moneda_id = tcd.moneda_id
                WHERE 1=1 ";

            if (!string.IsNullOrWhiteSpace(search))
            {
                p.Add("Search", $"%{search.Trim()}%");
                sql += " AND (CONVERT(VARCHAR(10), tcd.fecha, 103) LIKE @Search OR CONVERT(VARCHAR(10), tcd.fecha, 120) LIKE @Search OR CAST(tcd.monto_c AS VARCHAR(20)) LIKE @Search OR CAST(tcd.monto_v AS VARCHAR(20)) LIKE @Search OR tc.nombre LIKE @Search) ";
            }

            if (tipo.HasValue && tipo.Value > 0)
            {
                p.Add("Tipo", tipo.Value);
                sql += " AND tcd.tipo_cambio_id = @Tipo ";
            }

            if (!string.IsNullOrWhiteSpace(estado))
            {
                p.Add("Estado", estado.Trim().ToUpper());
                sql += " AND tcd.estado = @Estado ";
            }

            sql += " ORDER BY tcd.fecha DESC, tcd.tipo_cambio_id ASC";

            var rows = await conn.QueryAsync<TipoCambioModel>(sql, p);
            return rows.ToList();
        }

        public async Task<TipoCambioModel?> GetTipoCambioByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    tcd.tipo_camb_dia_id AS Id,
                    tcd.tipo_cambio_id AS TipoCambioId,
                    tc.nombre AS TipoCambioNombre,
                    tcd.fecha AS Fecha,
                    tcd.moneda_id AS MonedaId,
                    m.nombre AS MonedaNombre,
                    m.simbolo AS MonedaSimbolo,
                    tcd.monto_c AS MontoCompra,
                    tcd.monto_v AS MontoVenta,
                    tcd.estado AS Estado
                FROM tipo_camb_dia tcd
                INNER JOIN tipo_cambio tc ON tc.tipo_cambio_id = tcd.tipo_cambio_id
                INNER JOIN moneda m ON m.moneda_id = tcd.moneda_id
                WHERE tcd.tipo_camb_dia_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<TipoCambioModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveTipoCambioAsync(TipoCambioModel model, int userId)
        {
            if (model.MontoCompra <= 0)
                return (false, "El tipo de cambio de compra debe ser mayor a cero.", 0);

            if (model.MontoVenta <= 0)
                return (false, "El tipo de cambio de venta debe ser mayor a cero.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            if (model.Id == 0)
            {
                // Verificar si ya existe registro para la misma fecha, tipo y moneda
                const string checkSql = "SELECT COUNT(1) FROM tipo_camb_dia WHERE CONVERT(DATE, fecha) = CONVERT(DATE, @Fecha) AND tipo_cambio_id = @TipoCambioId AND moneda_id = @MonedaId";
                int existe = await conn.ExecuteScalarAsync<int>(checkSql, new { Fecha = model.Fecha, TipoCambioId = model.TipoCambioId, MonedaId = model.MonedaId });
                if (existe > 0)
                {
                    return (false, $"Ya existe un tipo de cambio registrado para la fecha {model.Fecha:dd/MM/yyyy} con esa paridad y moneda.", 0);
                }

                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR tipo_camb_dia_seq;
                    INSERT INTO tipo_camb_dia (tipo_camb_dia_id, tipo_cambio_id, fecha, moneda_id, monto_c, monto_v, estado, usuario_creacion_id, fecha_creacion)
                    VALUES (@newId, @TipoCambioId, @Fecha, @MonedaId, @MontoCompra, @MontoVenta, @Estado, @UserId, GETDATE());
                    SELECT @newId;";
                int newId = await conn.ExecuteScalarAsync<int>(sql, new {
                    TipoCambioId = model.TipoCambioId > 0 ? model.TipoCambioId : 1,
                    Fecha = model.Fecha.Date,
                    MonedaId = model.MonedaId > 0 ? model.MonedaId : 2,
                    MontoCompra = model.MontoCompra,
                    MontoVenta = model.MontoVenta,
                    Estado = model.Estado ?? "A",
                    UserId = userId
                });
                return (true, "Tipo de cambio diario registrado exitosamente.", newId);
            }
            else
            {
                const string sql = @"
                    UPDATE tipo_camb_dia 
                    SET tipo_cambio_id = @TipoCambioId,
                        fecha = @Fecha,
                        moneda_id = @MonedaId,
                        monto_c = @MontoCompra,
                        monto_v = @MontoVenta,
                        estado = @Estado,
                        usuario_modifica_id = @UserId,
                        fecha_modifica = GETDATE()
                    WHERE tipo_camb_dia_id = @Id";
                await conn.ExecuteAsync(sql, new {
                    Id = model.Id,
                    TipoCambioId = model.TipoCambioId > 0 ? model.TipoCambioId : 1,
                    Fecha = model.Fecha.Date,
                    MonedaId = model.MonedaId > 0 ? model.MonedaId : 2,
                    MontoCompra = model.MontoCompra,
                    MontoVenta = model.MontoVenta,
                    Estado = model.Estado ?? "A",
                    UserId = userId
                });
                return (true, "Tipo de cambio diario actualizado exitosamente.", model.Id);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoTipoCambioAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var item = await conn.QueryFirstOrDefaultAsync<TipoCambioModel>("SELECT tipo_camb_dia_id AS Id, estado AS Estado FROM tipo_camb_dia WHERE tipo_camb_dia_id = @Id", new { Id = id });
            if (item == null) return (false, "Registro no encontrado.", "I");

            string nuevoEstado = item.Estado == "A" ? "I" : "A";
            const string sql = "UPDATE tipo_camb_dia SET estado = @NuevoEstado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE tipo_camb_dia_id = @Id";
            await conn.ExecuteAsync(sql, new { Id = id, NuevoEstado = nuevoEstado, UserId = userId });
            return (true, $"Tipo de cambio diario {(nuevoEstado == "A" ? "activado" : "desactivado")} exitosamente.", nuevoEstado);
        }

        public async Task<List<TipoCambioCatalogoItem>> GetTiposCambioCatalogoAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_cambio_id AS Id, tipo_cambio AS Codigo, nombre AS Nombre FROM tipo_cambio WHERE estado = 'A' ORDER BY tipo_cambio_id ASC";
            var rows = await conn.QueryAsync<TipoCambioCatalogoItem>(sql);
            return rows.ToList();
        }

        public async Task<List<MonedaCatalogoItem>> GetMonedasCatalogoAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT moneda_id AS Id, moneda AS Codigo, nombre AS Nombre, simbolo AS Simbolo FROM moneda WHERE estado = 'A' ORDER BY moneda_id ASC";
            var rows = await conn.QueryAsync<MonedaCatalogoItem>(sql);
            return rows.ToList();
        }

        public async Task<List<TipoCambioMaestroModel>> GetTiposCambioMaestrosAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_cambio_id AS Id, tipo_cambio AS Codigo, nombre AS Nombre, estado AS Estado FROM tipo_cambio ORDER BY nombre";
            var rows = await conn.QueryAsync<TipoCambioMaestroModel>(sql);
            return rows.ToList();
        }

        public async Task<TipoCambioMaestroModel?> GetTipoCambioMaestroByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_cambio_id AS Id, tipo_cambio AS Codigo, nombre AS Nombre, estado AS Estado FROM tipo_cambio WHERE tipo_cambio_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<TipoCambioMaestroModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveTipoCambioMaestroAsync(TipoCambioMaestroModel model, int userId)
        {
            if (string.IsNullOrWhiteSpace(model.Codigo) || string.IsNullOrWhiteSpace(model.Nombre))
                return (false, "Código y nombre del tipo de cambio son obligatorios.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            string codigo = SafeTruncate(model.Codigo.Trim().ToUpperInvariant(), 2);
            int duplicate = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM tipo_cambio WHERE tipo_cambio = @Codigo AND tipo_cambio_id <> @Id",
                new { Codigo = codigo, model.Id });
            if (duplicate > 0)
                return (false, "El código ya está asignado a otro tipo de cambio.", 0);
            if (model.Id == 0)
            {
                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR tipo_cambio_seq;
                    INSERT INTO tipo_cambio (tipo_cambio_id, tipo_cambio, nombre, origen_registro_id, estado, usuario_creacion_id, fecha_creacion)
                    VALUES (@newId, @Codigo, @Nombre, 2, @Estado, @UserId, GETDATE());
                    SELECT @newId;";
                int id = await conn.ExecuteScalarAsync<int>(sql, new { Codigo = codigo, Nombre = model.Nombre.Trim(), Estado = model.Estado == "I" ? "I" : "A", UserId = userId });
                return (true, "Tipo de cambio registrado.", id);
            }

            const string updateSql = @"
                UPDATE tipo_cambio
                SET nombre = @Nombre, estado = @Estado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                WHERE tipo_cambio_id = @Id";
            int updated = await conn.ExecuteAsync(updateSql, new { model.Id, Nombre = model.Nombre.Trim(), Estado = model.Estado == "I" ? "I" : "A", UserId = userId });
            return updated == 0 ? (false, "Tipo de cambio no encontrado.", 0) : (true, "Tipo de cambio actualizado.", model.Id);
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoTipoCambioMaestroAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                UPDATE tipo_cambio SET estado = CASE WHEN estado = 'A' THEN 'I' ELSE 'A' END,
                    usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                OUTPUT inserted.estado WHERE tipo_cambio_id = @Id";
            string? state = await conn.QuerySingleOrDefaultAsync<string>(sql, new { Id = id, UserId = userId });
            return state == null ? (false, "Tipo de cambio no encontrado.", "I") : (true, "Estado del tipo de cambio actualizado.", state);
        }

        // =========================================================================
        // 7. FAMILIA DE PRODUCTO
        // =========================================================================
        public async Task<List<GrupoClaseFamiliaModel>> GetFamiliasProductoAsync(int? claseId = null, int? grupoId = null)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
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
                WHERE (@ClaseId IS NULL OR f.clase_producto_id = @ClaseId)
                  AND (@GrupoId IS NULL OR c.grupo_producto_id = @GrupoId)
                ORDER BY g.nombre, c.nombre, f.nombre";

            var rows = await conn.QueryAsync<GrupoClaseFamiliaModel>(sql, new {
                ClaseId = (claseId > 0) ? claseId : null,
                GrupoId = (grupoId > 0) ? grupoId : null
            });
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
            if (model.FamiliaId == 0 && string.IsNullOrWhiteSpace(model.FamiliaCodigo))
                return (false, "El código de la familia es obligatorio.", 0);
            if (model.ClaseId <= 0)
                return (false, "Debe seleccionar una clase de producto.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            int activeClass = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM clase_producto WHERE clase_producto_id = @ClaseId AND estado = 'A'",
                new { model.ClaseId });
            if (activeClass == 0)
                return (false, "La clase seleccionada no existe o está inactiva.", 0);

            string familiaCodigo = SafeTruncate((model.FamiliaCodigo ?? string.Empty).Trim().ToUpperInvariant(), 3);
            int duplicate = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM familia_producto WHERE clase_producto_id = @ClaseId AND familia_producto = @Codigo AND familia_producto_id <> @Id",
                new { model.ClaseId, Codigo = familiaCodigo, Id = model.FamiliaId });
            if (duplicate > 0)
                return (false, "El código ya está asignado a otra familia de esta clase.", 0);

            if (model.FamiliaId == 0)
            {
                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR familia_producto_seq;
                    INSERT INTO familia_producto (familia_producto_id, clase_producto_id, familia_producto, nombre, correla, estado, fecha_alta, usuario_creacion_id, fecha_creacion)
                    VALUES (@newId, @ClaseId, @Codigo, @Nombre, @Correlativo, @Estado, GETDATE(), @UserId, GETDATE());
                    SELECT @newId;";
                int newId = await conn.ExecuteScalarAsync<int>(sql, new { ClaseId = model.ClaseId, Codigo = familiaCodigo, Nombre = model.FamiliaNombre.Trim(), Correlativo = Math.Max(0, (int)model.Correlativo), Estado = model.Estado ?? "A", UserId = userId });
                return (true, "Familia de producto registrada exitosamente.", newId);
            }
            else
            {
                const string sql = @"
                    UPDATE familia_producto 
                    SET nombre = @Nombre, clase_producto_id = @ClaseId, estado = @Estado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                    WHERE familia_producto_id = @Id";
                int updated = await conn.ExecuteAsync(sql, new { Id = model.FamiliaId, Nombre = model.FamiliaNombre.Trim(), ClaseId = model.ClaseId, Estado = model.Estado ?? "A", UserId = userId });
                return updated == 0 ? (false, "Familia de producto no encontrada.", 0) : (true, "Familia de producto actualizada exitosamente.", model.FamiliaId);
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

        public async Task<List<LookupItemModel>> GetClasesProductoLookupAsync(int? grupoId = null, string? tipo = null)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            string sql = @"
                SELECT c.clase_producto_id AS Id, c.clase_producto AS Codigo, c.nombre AS Nombre 
                FROM clase_producto c
                INNER JOIN grupo_producto g ON c.grupo_producto_id = g.grupo_producto_id
                LEFT JOIN tipo_producto tp ON g.tipo_producto_id = tp.tipo_producto_id
                WHERE c.estado = 'A' 
                  AND (@GrupoId IS NULL OR c.grupo_producto_id = @GrupoId)
                  AND (@Tipo IS NULL OR tp.tipo_producto = @Tipo)
                ORDER BY c.nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql, new { 
                GrupoId = (grupoId > 0) ? grupoId : null,
                Tipo = string.IsNullOrWhiteSpace(tipo) ? null : tipo.Trim().ToUpper()
            });
            return rows.ToList();
        }

        public async Task<List<GrupoProductoModel>> GetGruposProductoAsync(int? tipoProductoId = null, string? search = null, string? estado = null)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT g.grupo_producto_id AS Id, g.tipo_producto_id AS TipoProductoId,
                       tp.nombre AS TipoProductoNombre, g.grupo_producto AS Codigo, g.nombre AS Nombre,
                       g.correla AS Correlativo, g.usar_inc_fracc AS UsarIncrementoFraccion,
                       g.es_combo AS EsCombo, g.es_insumo AS EsInsumo, g.estado AS Estado,
                       g.fecha_alta AS FechaAlta, g.fecha_baja AS FechaBaja,
                       (SELECT COUNT(1) FROM clase_producto cp WHERE cp.grupo_producto_id = g.grupo_producto_id) AS ClasesCount,
                       (SELECT COUNT(1) FROM familia_producto fp INNER JOIN clase_producto cp ON fp.clase_producto_id = cp.clase_producto_id WHERE cp.grupo_producto_id = g.grupo_producto_id) AS FamiliasCount
                FROM grupo_producto g
                INNER JOIN tipo_producto tp ON tp.tipo_producto_id = g.tipo_producto_id
                WHERE (@TipoProductoId IS NULL OR g.tipo_producto_id = @TipoProductoId)
                  AND (@Estado IS NULL OR g.estado = @Estado)
                  AND (@Search IS NULL OR g.grupo_producto LIKE '%' + @Search + '%' OR g.nombre LIKE '%' + @Search + '%')
                ORDER BY tp.nombre, g.nombre";
            var rows = await conn.QueryAsync<GrupoProductoModel>(sql, new { 
                TipoProductoId = tipoProductoId > 0 ? tipoProductoId : null,
                Estado = string.IsNullOrWhiteSpace(estado) ? null : estado.Trim(),
                Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim()
            });
            return rows.ToList();
        }

        public async Task<GrupoProductoModel?> GetGrupoProductoByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT g.grupo_producto_id AS Id, g.tipo_producto_id AS TipoProductoId,
                       tp.nombre AS TipoProductoNombre, g.grupo_producto AS Codigo, g.nombre AS Nombre,
                       g.correla AS Correlativo, g.usar_inc_fracc AS UsarIncrementoFraccion,
                       g.es_combo AS EsCombo, g.es_insumo AS EsInsumo, g.estado AS Estado,
                       g.fecha_alta AS FechaAlta, g.fecha_baja AS FechaBaja,
                       (SELECT COUNT(1) FROM clase_producto cp WHERE cp.grupo_producto_id = g.grupo_producto_id) AS ClasesCount,
                       (SELECT COUNT(1) FROM familia_producto fp INNER JOIN clase_producto cp ON fp.clase_producto_id = cp.clase_producto_id WHERE cp.grupo_producto_id = g.grupo_producto_id) AS FamiliasCount
                FROM grupo_producto g
                INNER JOIN tipo_producto tp ON tp.tipo_producto_id = g.tipo_producto_id
                WHERE g.grupo_producto_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<GrupoProductoModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveGrupoProductoAsync(GrupoProductoModel model, int userId)
        {
            if (model.TipoProductoId <= 0 || string.IsNullOrWhiteSpace(model.Codigo) || string.IsNullOrWhiteSpace(model.Nombre))
                return (false, "Tipo, código y nombre de grupo son obligatorios.", 0);

            string codigo = SafeTruncate(model.Codigo.ToUpperInvariant(), 3);
            using var conn = _dbConnectionFactory.CreateConnection();
            int activeType = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM tipo_producto WHERE tipo_producto_id = @TipoProductoId AND estado = 'A'",
                new { model.TipoProductoId });
            if (activeType == 0)
                return (false, "El tipo de producto seleccionado no existe o está inactivo.", 0);
            int duplicate = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM grupo_producto WHERE tipo_producto_id = @TipoProductoId AND grupo_producto = @Codigo AND grupo_producto_id <> @Id",
                new { model.TipoProductoId, Codigo = codigo, model.Id });
            if (duplicate > 0)
                return (false, "El código ya está asignado a otro grupo de este tipo.", 0);
            if (model.Id == 0)
            {
                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR grupo_producto_seq;
                    INSERT INTO grupo_producto
                        (grupo_producto_id, tipo_producto_id, grupo_producto, nombre, correla, usar_inc_fracc, es_combo, es_insumo, estado, fecha_alta, usuario_creacion_id, fecha_creacion)
                    VALUES
                        (@newId, @TipoProductoId, @Codigo, @Nombre, @Correlativo, @UsarIncrementoFraccion, @EsCombo, @EsInsumo, @Estado, @FechaAlta, @UserId, GETDATE());
                    SELECT @newId;";
                int id = await conn.ExecuteScalarAsync<int>(sql, new
                {
                    model.TipoProductoId, Codigo = codigo, Nombre = model.Nombre.Trim(),
                    Correlativo = model.Correlativo > 0 ? model.Correlativo : (short)1,
                    UsarIncrementoFraccion = model.UsarIncrementoFraccion == "S" ? "S" : "N",
                    EsCombo = model.EsCombo == "S" ? "S" : "N",
                    EsInsumo = model.EsInsumo == "S" ? "S" : "N",
                    Estado = model.Estado == "I" ? "I" : "A",
                    FechaAlta = model.FechaAlta == default ? DateTime.Today : model.FechaAlta,
                    UserId = userId
                });
                return (true, "Grupo de producto registrado.", id);
            }

            const string updateSql = @"
                UPDATE grupo_producto
                SET nombre = @Nombre, usar_inc_fracc = @UsarIncrementoFraccion,
                    es_combo = @EsCombo, es_insumo = @EsInsumo, estado = @Estado,
                    fecha_baja = CASE WHEN @Estado = 'I' THEN ISNULL(fecha_baja, GETDATE()) ELSE NULL END,
                    usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                WHERE grupo_producto_id = @Id";
            int updated = await conn.ExecuteAsync(updateSql, new
            {
                model.Id, Nombre = model.Nombre.Trim(),
                UsarIncrementoFraccion = model.UsarIncrementoFraccion == "S" ? "S" : "N",
                EsCombo = model.EsCombo == "S" ? "S" : "N",
                EsInsumo = model.EsInsumo == "S" ? "S" : "N",
                Estado = model.Estado == "I" ? "I" : "A", UserId = userId
            });
            return updated == 0 ? (false, "Grupo no encontrado.", 0) : (true, "Grupo de producto actualizado.", model.Id);
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoGrupoProductoAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                UPDATE grupo_producto
                SET estado = CASE WHEN estado = 'A' THEN 'I' ELSE 'A' END,
                    fecha_baja = CASE WHEN estado = 'A' THEN ISNULL(fecha_baja, GETDATE()) ELSE NULL END,
                    usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                OUTPUT inserted.estado
                WHERE grupo_producto_id = @Id";
            string? state = await conn.QuerySingleOrDefaultAsync<string>(sql, new { Id = id, UserId = userId });
            return state == null ? (false, "Grupo no encontrado.", "I") : (true, "Estado del grupo actualizado.", state);
        }

        public async Task<List<ClaseProductoModel>> GetClasesProductoAsync(int? grupoId = null)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT c.clase_producto_id AS Id, c.grupo_producto_id AS GrupoId, g.nombre AS GrupoNombre,
                       c.clase_producto AS Codigo, c.nombre AS Nombre, c.correla AS Correlativo,
                       c.tipo_detrac_id AS TipoDetraccionId, ISNULL(td.nombre, '') AS TipoDetraccionNombre,
                       ISNULL(td.tasa, 0) AS TipoDetraccionTasa,
                       (SELECT COUNT(1) FROM familia_producto fp WHERE fp.clase_producto_id = c.clase_producto_id) AS FamiliasCount,
                       c.estado AS Estado, c.fecha_alta AS FechaAlta, c.fecha_baja AS FechaBaja
                FROM clase_producto c
                INNER JOIN grupo_producto g ON g.grupo_producto_id = c.grupo_producto_id
                LEFT JOIN tipo_detrac td ON td.tipo_detrac_id = c.tipo_detrac_id
                WHERE (@GrupoId IS NULL OR c.grupo_producto_id = @GrupoId)
                ORDER BY c.clase_producto, c.nombre";
            var rows = await conn.QueryAsync<ClaseProductoModel>(sql, new { GrupoId = grupoId > 0 ? grupoId : null });
            return rows.ToList();
        }

        public async Task<ClaseProductoModel?> GetClaseProductoByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT c.clase_producto_id AS Id, c.grupo_producto_id AS GrupoId, g.nombre AS GrupoNombre,
                       c.clase_producto AS Codigo, c.nombre AS Nombre, c.correla AS Correlativo,
                       c.tipo_detrac_id AS TipoDetraccionId, ISNULL(td.nombre, '') AS TipoDetraccionNombre,
                       ISNULL(td.tasa, 0) AS TipoDetraccionTasa,
                       (SELECT COUNT(1) FROM familia_producto fp WHERE fp.clase_producto_id = c.clase_producto_id) AS FamiliasCount,
                       c.estado AS Estado, c.fecha_alta AS FechaAlta, c.fecha_baja AS FechaBaja
                FROM clase_producto c
                INNER JOIN grupo_producto g ON g.grupo_producto_id = c.grupo_producto_id
                LEFT JOIN tipo_detrac td ON td.tipo_detrac_id = c.tipo_detrac_id
                WHERE c.clase_producto_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<ClaseProductoModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveClaseProductoAsync(ClaseProductoModel model, int userId)
        {
            if (model.GrupoId <= 0 || string.IsNullOrWhiteSpace(model.Codigo) || string.IsNullOrWhiteSpace(model.Nombre))
                return (false, "Grupo, código y nombre de clase son obligatorios.", 0);

            string codigo = SafeTruncate(model.Codigo.ToUpperInvariant(), 3);
            using var conn = _dbConnectionFactory.CreateConnection();
            int activeGroup = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM grupo_producto WHERE grupo_producto_id = @GrupoId AND estado = 'A'",
                new { model.GrupoId });
            if (activeGroup == 0)
                return (false, "El grupo seleccionado no existe o está inactivo.", 0);
            int duplicate = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM clase_producto WHERE grupo_producto_id = @GrupoId AND clase_producto = @Codigo AND clase_producto_id <> @Id",
                new { model.GrupoId, Codigo = codigo, model.Id });
            if (duplicate > 0)
                return (false, "El código ya está asignado a otra clase de este grupo.", 0);
            if (model.Id == 0)
            {
                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR clase_producto_seq;
                    INSERT INTO clase_producto
                        (clase_producto_id, grupo_producto_id, clase_producto, nombre, correla, tipo_detrac_id, estado, fecha_alta, usuario_creacion_id, fecha_creacion)
                    VALUES
                        (@newId, @GrupoId, @Codigo, @Nombre, @Correlativo, @TipoDetraccionId, @Estado, @FechaAlta, @UserId, GETDATE());
                    SELECT @newId;";
                int id = await conn.ExecuteScalarAsync<int>(sql, new
                {
                    model.GrupoId, Codigo = codigo, Nombre = model.Nombre.Trim(),
                    Correlativo = model.Correlativo > 0 ? model.Correlativo : (short)1,
                    model.TipoDetraccionId, Estado = model.Estado == "I" ? "I" : "A",
                    FechaAlta = model.FechaAlta == default ? DateTime.Today : model.FechaAlta, UserId = userId
                });
                return (true, "Clase de producto registrada.", id);
            }

            const string updateSql = @"
                UPDATE clase_producto
                SET nombre = @Nombre, tipo_detrac_id = @TipoDetraccionId, estado = @Estado,
                    fecha_baja = CASE WHEN @Estado = 'I' THEN ISNULL(fecha_baja, GETDATE()) ELSE NULL END,
                    usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                WHERE clase_producto_id = @Id";
            int updated = await conn.ExecuteAsync(updateSql, new
            {
                model.Id, Nombre = model.Nombre.Trim(), model.TipoDetraccionId,
                Estado = model.Estado == "I" ? "I" : "A", UserId = userId
            });
            return updated == 0 ? (false, "Clase no encontrada.", 0) : (true, "Clase de producto actualizada.", model.Id);
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoClaseProductoAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                UPDATE clase_producto
                SET estado = CASE WHEN estado = 'A' THEN 'I' ELSE 'A' END,
                    fecha_baja = CASE WHEN estado = 'A' THEN ISNULL(fecha_baja, GETDATE()) ELSE NULL END,
                    usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                OUTPUT inserted.estado
                WHERE clase_producto_id = @Id";
            string? state = await conn.QuerySingleOrDefaultAsync<string>(sql, new { Id = id, UserId = userId });
            return state == null ? (false, "Clase no encontrada.", "I") : (true, "Estado de la clase actualizado.", state);
        }

        public async Task<List<LookupItemModel>> GetTiposDetraccionLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_detrac_id AS Id, tipo_detrac AS Codigo, nombre AS Nombre FROM tipo_detrac WHERE estado = 'A' ORDER BY nombre";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        // =========================================================================
        // 8. PRODUCTO
        // =========================================================================
        // =========================================================================
        // 8. PRODUCTO Y SERVICIO
        // =========================================================================
        public async Task<List<ProductoModel>> GetProductosAsync(
            string? search = null, 
            string? tipo = null, 
            int? grupoId = null, 
            int? claseId = null,
            int? familiaId = null, 
            string? estado = null, 
            string? sort = null, 
            string? dir = null, 
            int top = 25)
        {
            using var conn = _dbConnectionFactory.CreateConnection();

            string orderBy = (sort?.ToLower()) switch
            {
                "codigo" => dir == "desc" ? "p.producto DESC" : "p.producto ASC",
                "descripcion" => dir == "desc" ? "p.descripcion DESC" : "p.descripcion ASC",
                "tipo" => dir == "desc" ? "tp.tipo_producto DESC" : "tp.tipo_producto ASC",
                "grupo" => dir == "desc" ? "g.nombre DESC" : "g.nombre ASC",
                "clase" => dir == "desc" ? "c.nombre DESC" : "c.nombre ASC",
                "familia" => dir == "desc" ? "f.nombre DESC" : "f.nombre ASC",
                "um" => dir == "desc" ? "u.unidad_medida DESC" : "u.unidad_medida ASC",
                "peso" => dir == "desc" ? "p.peso_neto DESC" : "p.peso_neto ASC",
                "estado" => dir == "desc" ? "p.estado DESC" : "p.estado ASC",
                _ => dir == "asc" ? "p.producto_id ASC" : "p.producto_id DESC"
            };

            int safeTop = top is 25 or 50 or 100 or 500 ? top : 25;
            string topClause = $"TOP ({safeTop})";

            string sql = $@"
                SELECT {topClause}
                    p.producto_id AS Id,
                    p.producto AS Codigo,
                    p.descripcion AS Descripcion,
                    ISNULL(p.cod_referencial, '') AS CodReferencial,
                    
                    ISNULL(tp.tipo_producto_id, 1) AS TipoProductoId,
                    ISNULL(tp.tipo_producto, 'P') AS TipoProductoCodigo,
                    ISNULL(tp.nombre, 'PRODUCTO') AS TipoProductoNombre,

                    ISNULL(g.grupo_producto_id, 0) AS GrupoId,
                    ISNULL(g.grupo_producto, '') AS GrupoCodigo,
                    ISNULL(g.nombre, '') AS GrupoNombre,
                    ISNULL(c.clase_producto_id, 0) AS ClaseId,
                    ISNULL(c.clase_producto, '') AS ClaseCodigo,
                    ISNULL(c.nombre, '') AS ClaseNombre,
                    p.familia_producto_id AS FamiliaId,
                    ISNULL(f.familia_producto, '') AS FamiliaCodigo,
                    ISNULL(f.nombre, 'Sin Familia') AS FamiliaNombre,

                    p.unidad_medida_id AS UnidadMedidaId,
                    ISNULL(u.unidad_medida, 'UND') AS UnidadMedida,
                    ISNULL(u.nombre, 'UNIDADES') AS UnidadMedidaNombre,
                    ISNULL(p.tipo_igv_id, 1) AS TipoIgvId,
                    ISNULL(ti.nombre, 'GRAVADO') AS TipoIgvNombre,

                    p.peso_neto AS PesoNeto,
                    p.peso_bruto AS PesoBruto,
                    p.valor_repo AS ValorReposicion,
                    p.volumen AS Volumen,
                    p.color_id AS ColorId,
                    ISNULL(col.nombre, '') AS ColorNombre,
                    p.master_pack AS MasterPack,

                    ISNULL(p.estado_producto_id, 1) AS EstadoProductoId,
                    ISNULL(ep.nombre, 'ACTIVO') AS EstadoProductoNombre,
                    ISNULL(p.estado_com_prod_id, 1) AS EstadoComProdId,
                    ISNULL(ecp.nombre, 'STOCK') AS EstadoComProdNombre,
                    p.estado AS Estado,

                    ISNULL(p.tiene_pres_vta, 'N') AS TienePresVta,
                    ISNULL(p.es_opera, 'N') AS EsOpera,
                    ISNULL(p.es_perecible, 'N') AS EsPerecible,
                    ISNULL(p.detalle_req, 'N') AS DetalleReq
                FROM producto p
                LEFT JOIN familia_producto f ON p.familia_producto_id = f.familia_producto_id
                LEFT JOIN clase_producto c ON f.clase_producto_id = c.clase_producto_id
                LEFT JOIN grupo_producto g ON c.grupo_producto_id = g.grupo_producto_id
                LEFT JOIN tipo_producto tp ON g.tipo_producto_id = tp.tipo_producto_id
                LEFT JOIN unidad_medida u ON p.unidad_medida_id = u.unidad_medida_id
                LEFT JOIN tipo_igv ti ON p.tipo_igv_id = ti.tipo_igv_id
                LEFT JOIN color col ON p.color_id = col.color_id
                LEFT JOIN estado_producto ep ON p.estado_producto_id = ep.estado_producto_id
                LEFT JOIN estado_com_prod ecp ON p.estado_com_prod_id = ecp.estado_com_prod_id
                WHERE (@Search IS NULL OR p.descripcion LIKE '%' + @Search + '%' OR p.producto LIKE '%' + @Search + '%' OR p.cod_referencial LIKE '%' + @Search + '%')
                  AND (@Tipo IS NULL OR tp.tipo_producto = @Tipo)
                  AND (@GrupoId IS NULL OR g.grupo_producto_id = @GrupoId)
                  AND (@ClaseId IS NULL OR c.clase_producto_id = @ClaseId)
                  AND (@FamiliaId IS NULL OR p.familia_producto_id = @FamiliaId)
                  AND (@Estado IS NULL OR p.estado = @Estado)
                ORDER BY {orderBy}";

            var rows = await conn.QueryAsync<ProductoModel>(sql, new { 
                Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                Tipo = string.IsNullOrWhiteSpace(tipo) ? null : tipo.Trim().ToUpper(),
                GrupoId = (grupoId > 0) ? grupoId : null,
                ClaseId = (claseId > 0) ? claseId : null,
                FamiliaId = (familiaId > 0) ? familiaId : null,
                Estado = string.IsNullOrWhiteSpace(estado) ? null : estado.Trim().ToUpper()
            });
            return rows.ToList();
        }

        public async Task<(int total, int productos, int servicios)> GetProductosCountsAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    COUNT(*) AS Total,
                    SUM(CASE WHEN ISNULL(tp.tipo_producto, 'P') = 'P' THEN 1 ELSE 0 END) AS Productos,
                    SUM(CASE WHEN ISNULL(tp.tipo_producto, 'P') = 'S' THEN 1 ELSE 0 END) AS Servicios
                FROM producto p
                LEFT JOIN familia_producto fp ON fp.familia_producto_id = p.familia_producto_id
                LEFT JOIN clase_producto cp ON cp.clase_producto_id = fp.clase_producto_id
                LEFT JOIN grupo_producto gp ON gp.grupo_producto_id = cp.grupo_producto_id
                LEFT JOIN tipo_producto tp ON tp.tipo_producto_id = gp.tipo_producto_id";
            
            var row = await conn.QueryFirstOrDefaultAsync(sql);
            if (row != null)
            {
                return ((int)row.Total, (int)row.Productos, (int)row.Servicios);
            }
            return (0, 0, 0);
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
                    
                    ISNULL(tp.tipo_producto_id, 1) AS TipoProductoId,
                    ISNULL(tp.tipo_producto, 'P') AS TipoProductoCodigo,
                    ISNULL(tp.nombre, 'PRODUCTO') AS TipoProductoNombre,

                    ISNULL(g.grupo_producto_id, 0) AS GrupoId,
                    ISNULL(g.grupo_producto, '') AS GrupoCodigo,
                    ISNULL(g.nombre, '') AS GrupoNombre,
                    ISNULL(c.clase_producto_id, 0) AS ClaseId,
                    ISNULL(c.clase_producto, '') AS ClaseCodigo,
                    ISNULL(c.nombre, '') AS ClaseNombre,
                    p.familia_producto_id AS FamiliaId,
                    ISNULL(f.familia_producto, '') AS FamiliaCodigo,
                    ISNULL(f.nombre, 'Sin Familia') AS FamiliaNombre,

                    p.unidad_medida_id AS UnidadMedidaId,
                    ISNULL(u.unidad_medida, 'UND') AS UnidadMedida,
                    ISNULL(u.nombre, 'UNIDADES') AS UnidadMedidaNombre,
                    ISNULL(p.tipo_igv_id, 1) AS TipoIgvId,
                    ISNULL(ti.nombre, 'GRAVADO') AS TipoIgvNombre,

                    p.peso_neto AS PesoNeto,
                    p.peso_bruto AS PesoBruto,
                    p.valor_repo AS ValorReposicion,
                    p.volumen AS Volumen,
                    p.color_id AS ColorId,
                    ISNULL(col.nombre, '') AS ColorNombre,
                    p.master_pack AS MasterPack,

                    ISNULL(p.estado_producto_id, 1) AS EstadoProductoId,
                    ISNULL(ep.nombre, 'ACTIVO') AS EstadoProductoNombre,
                    ISNULL(p.estado_com_prod_id, 1) AS EstadoComProdId,
                    ISNULL(ecp.nombre, 'STOCK') AS EstadoComProdNombre,
                    p.estado AS Estado,

                    ISNULL(p.tiene_pres_vta, 'N') AS TienePresVta,
                    ISNULL(p.es_opera, 'N') AS EsOpera,
                    ISNULL(p.es_perecible, 'N') AS EsPerecible,
                    ISNULL(p.detalle_req, 'N') AS DetalleReq
                FROM producto p
                LEFT JOIN familia_producto f ON p.familia_producto_id = f.familia_producto_id
                LEFT JOIN clase_producto c ON f.clase_producto_id = c.clase_producto_id
                LEFT JOIN grupo_producto g ON c.grupo_producto_id = g.grupo_producto_id
                LEFT JOIN tipo_producto tp ON g.tipo_producto_id = tp.tipo_producto_id
                LEFT JOIN unidad_medida u ON p.unidad_medida_id = u.unidad_medida_id
                LEFT JOIN tipo_igv ti ON p.tipo_igv_id = ti.tipo_igv_id
                LEFT JOIN color col ON p.color_id = col.color_id
                LEFT JOIN estado_producto ep ON p.estado_producto_id = ep.estado_producto_id
                LEFT JOIN estado_com_prod ecp ON p.estado_com_prod_id = ecp.estado_com_prod_id
                WHERE p.producto_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<ProductoModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveProductoAsync(ProductoModel model, int userId)
        {
            if (string.IsNullOrWhiteSpace(model.Descripcion))
                return (false, "La descripción del producto o servicio es obligatoria.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            if (conn.State != ConnectionState.Open)
                conn.Open();
            using var transaction = conn.BeginTransaction();

            // Si es servicio y no tiene familia seleccionada, buscar la familia de servicios (192)
            if (model.TipoProductoId == 2 && model.FamiliaId <= 0)
            {
                var famSvc = await conn.QueryFirstOrDefaultAsync<int?>(
                    "SELECT TOP 1 f.familia_producto_id FROM familia_producto f INNER JOIN clase_producto c ON f.clase_producto_id = c.clase_producto_id INNER JOIN grupo_producto g ON c.grupo_producto_id = g.grupo_producto_id WHERE g.tipo_producto_id = 2 AND f.estado = 'A' AND c.estado = 'A' AND g.estado = 'A' ORDER BY f.familia_producto_id",
                    transaction: transaction);
                model.FamiliaId = famSvc ?? 192;
            }

            if (model.FamiliaId <= 0)
                return (false, "Debe seleccionar una familia de producto.", 0);

            bool esServicio = (model.TipoProductoId == 2);

            if (model.Id == 0)
            {
                var sku = await GenerarCodigoSkuAsync(conn, transaction, model.FamiliaId, model.TipoProductoId);
                if (!sku.success)
                    return (false, sku.message, 0);
                string codigo = sku.codigo;

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
                        @TipoIgvId, @PesoNeto, @PesoBruto, @ValorReposicion, @Volumen, @TienePresVta, @EsOpera, @EsPerecible,
                        @DetalleReq, @MasterPack, @ColorId, @EstadoProductoId, @EstadoComProdId, @Estado, GETDATE(),
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
                    PesoNeto = esServicio ? 0 : (model.PesoNeto ?? 0),
                    PesoBruto = esServicio ? 0 : (model.PesoBruto ?? 0),
                    ValorReposicion = model.ValorReposicion ?? 0,
                    Volumen = esServicio ? 0 : (model.Volumen ?? 0),
                    TienePresVta = model.TienePresVta ?? "N",
                    EsOpera = esServicio ? (model.EsOpera ?? "S") : (model.EsOpera ?? "N"),
                    EsPerecible = esServicio ? "N" : (model.EsPerecible ?? "N"),
                    DetalleReq = model.DetalleReq ?? "N",
                    MasterPack = esServicio ? 1 : (model.MasterPack > 0 ? model.MasterPack : 1),
                    ColorId = esServicio ? null : model.ColorId,
                    EstadoProductoId = model.EstadoProductoId > 0 ? model.EstadoProductoId : 1,
                    EstadoComProdId = model.EstadoComProdId > 0 ? model.EstadoComProdId : 1,
                    Estado = model.Estado ?? "A",
                    UserId = userId
                }, transaction);
                transaction.Commit();
                return (true, $"{(esServicio ? "Servicio" : "Producto")} registrado exitosamente.", newId);
            }
            else
            {
                int? familiaActual = await conn.QueryFirstOrDefaultAsync<int?>(
                    "SELECT familia_producto_id FROM producto WHERE producto_id = @Id",
                    new { model.Id },
                    transaction);
                if (!familiaActual.HasValue)
                    return (false, "Producto no encontrado.", 0);
                if (familiaActual.Value != model.FamiliaId)
                    return (false, "No se puede modificar la jerarquía del producto porque forma parte de su SKU.", 0);

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
                        volumen = @Volumen,
                        tiene_pres_vta = @TienePresVta,
                        es_opera = @EsOpera,
                        es_perecible = @EsPerecible,
                        detalle_req = @DetalleReq,
                        color_id = @ColorId,
                        master_pack = @MasterPack,
                        estado_producto_id = @EstadoProductoId,
                        estado_com_prod_id = @EstadoComProdId,
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
                    PesoNeto = esServicio ? 0 : (model.PesoNeto ?? 0),
                    PesoBruto = esServicio ? 0 : (model.PesoBruto ?? 0),
                    ValorReposicion = model.ValorReposicion ?? 0,
                    Volumen = esServicio ? 0 : (model.Volumen ?? 0),
                    TienePresVta = model.TienePresVta ?? "N",
                    EsOpera = esServicio ? (model.EsOpera ?? "S") : (model.EsOpera ?? "N"),
                    EsPerecible = esServicio ? "N" : (model.EsPerecible ?? "N"),
                    DetalleReq = model.DetalleReq ?? "N",
                    ColorId = esServicio ? null : model.ColorId,
                    MasterPack = esServicio ? 1 : (model.MasterPack > 0 ? model.MasterPack : 1),
                    EstadoProductoId = model.EstadoProductoId > 0 ? model.EstadoProductoId : 1,
                    EstadoComProdId = model.EstadoComProdId > 0 ? model.EstadoComProdId : 1,
                    Estado = model.Estado ?? "A",
                    UserId = userId
                }, transaction);
                transaction.Commit();
                return (true, $"{(esServicio ? "Servicio" : "Producto")} actualizado exitosamente.", model.Id);
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
            return (true, $"Registro {(nuevoEstado == "A" ? "activado" : "desactivado")} exitosamente.", nuevoEstado);
        }

        public async Task<List<LookupItemModel>> GetTiposProductoLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_producto_id AS Id, tipo_producto AS Codigo, nombre AS Nombre FROM tipo_producto WHERE estado = 'A' ORDER BY tipo_producto_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetGruposProductoLookupAsync(int? tipoProductoId = null, string? tipo = null)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            string sql = @"
                SELECT g.grupo_producto_id AS Id, g.grupo_producto AS Codigo, g.nombre AS Nombre 
                FROM grupo_producto g
                LEFT JOIN tipo_producto tp ON g.tipo_producto_id = tp.tipo_producto_id
                WHERE g.estado = 'A' 
                  AND (@TipoProductoId IS NULL OR g.tipo_producto_id = @TipoProductoId)
                  AND (@Tipo IS NULL OR tp.tipo_producto = @Tipo)
                ORDER BY g.nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql, new { 
                TipoProductoId = (tipoProductoId > 0) ? tipoProductoId : null,
                Tipo = string.IsNullOrWhiteSpace(tipo) ? null : tipo.Trim().ToUpper()
            });
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetFamiliasProductoLookupAsync(int? grupoId = null, int? claseId = null, string? tipo = null)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            string sql = @"
                SELECT f.familia_producto_id AS Id, f.familia_producto AS Codigo, f.nombre AS Nombre 
                FROM familia_producto f
                INNER JOIN clase_producto c ON f.clase_producto_id = c.clase_producto_id
                INNER JOIN grupo_producto g ON c.grupo_producto_id = g.grupo_producto_id
                LEFT JOIN tipo_producto tp ON g.tipo_producto_id = tp.tipo_producto_id
                WHERE f.estado = 'A' 
                  AND (@GrupoId IS NULL OR c.grupo_producto_id = @GrupoId)
                  AND (@ClaseId IS NULL OR f.clase_producto_id = @ClaseId)
                  AND (@Tipo IS NULL OR tp.tipo_producto = @Tipo)
                ORDER BY f.nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql, new { 
                GrupoId = (grupoId > 0) ? grupoId : null,
                ClaseId = (claseId > 0) ? claseId : null,
                Tipo = string.IsNullOrWhiteSpace(tipo) ? null : tipo.Trim().ToUpper()
            });
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetUnidadesMedidaLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT unidad_medida_id AS Id, unidad_medida AS Codigo, nombre AS Nombre FROM unidad_medida WHERE estado = 'A' ORDER BY nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetColoresLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT color_id AS Id, '' AS Codigo, nombre AS Nombre FROM color WHERE estado = 'A' ORDER BY nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetTiposIgvLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_igv_id AS Id, tipo_igv AS Codigo, nombre AS Nombre FROM tipo_igv WHERE estado = 'A' ORDER BY tipo_igv_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetEstadosProductoLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT estado_producto_id AS Id, estado_producto AS Codigo, nombre AS Nombre FROM estado_producto ORDER BY estado_producto_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetEstadosComercialesProductoLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT estado_com_prod_id AS Id, estado_com_prod AS Codigo, nombre AS Nombre FROM estado_com_prod WHERE estado = 'A' ORDER BY estado_com_prod_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        // -------------------------------------------------------------------------
        // 8.1 Presentaciones para la Venta (produ_p_vta)
        // -------------------------------------------------------------------------
        public async Task<List<ProductoPresentacionVentaModel>> GetPresentacionesVentaAsync(int productoId)
        {
            if (productoId <= 0) return new List<ProductoPresentacionVentaModel>();
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    ppv.produ_p_vta_id AS Id,
                    ppv.producto_id AS ProductoId,
                    ppv.unidad_medida_id AS UnidadMedidaId,
                    um.nombre AS UnidadMedidaNombre,
                    ppv.cant_equiv AS CantidadEquivalente,
                    ppv.estado AS Estado
                FROM produ_p_vta ppv
                INNER JOIN unidad_medida um ON ppv.unidad_medida_id = um.unidad_medida_id
                WHERE ppv.producto_id = @ProductoId AND ppv.estado = 'A'
                ORDER BY um.nombre ASC";
            var rows = await conn.QueryAsync<ProductoPresentacionVentaModel>(sql, new { ProductoId = productoId });
            return rows.ToList();
        }

        public async Task<(bool success, string message, int id)> SavePresentacionVentaAsync(ProductoPresentacionVentaModel model, int userId)
        {
            if (model.ProductoId <= 0)
                return (false, "El producto es inválido.", 0);
            if (model.UnidadMedidaId <= 0)
                return (false, "Debe seleccionar una unidad de medida.", 0);
            if (model.CantidadEquivalente <= 0)
                return (false, "La cantidad equivalente debe ser mayor a cero.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            if (model.Id == 0)
            {
                var exists = await conn.QueryFirstOrDefaultAsync<int?>(
                    "SELECT TOP 1 produ_p_vta_id FROM produ_p_vta WHERE producto_id = @ProductoId AND unidad_medida_id = @UnidadMedidaId AND estado = 'A'",
                    new { ProductoId = model.ProductoId, UnidadMedidaId = model.UnidadMedidaId });
                if (exists.HasValue)
                    return (false, "Ya existe una presentación de venta registrada para esta unidad de medida.", 0);

                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR produ_p_vta_seq;
                    INSERT INTO produ_p_vta (produ_p_vta_id, producto_id, unidad_medida_id, cant_equiv, estado, usuario_creacion_id, fecha_creacion)
                    VALUES (@newId, @ProductoId, @UnidadMedidaId, @CantidadEquivalente, 'A', @UserId, GETDATE());
                    UPDATE producto SET tiene_pres_vta = 'S' WHERE producto_id = @ProductoId;
                    SELECT @newId;";
                int newId = await conn.ExecuteScalarAsync<int>(sql, new {
                    ProductoId = model.ProductoId,
                    UnidadMedidaId = model.UnidadMedidaId,
                    CantidadEquivalente = model.CantidadEquivalente,
                    UserId = userId
                });
                return (true, "Presentación de venta agregada exitosamente.", newId);
            }
            else
            {
                const string sql = @"
                    UPDATE produ_p_vta 
                    SET unidad_medida_id = @UnidadMedidaId, 
                        cant_equiv = @CantidadEquivalente, 
                        usuario_modifica_id = @UserId, 
                        fecha_modifica = GETDATE()
                    WHERE produ_p_vta_id = @Id;
                    UPDATE producto SET tiene_pres_vta = 'S' WHERE producto_id = @ProductoId;";
                await conn.ExecuteAsync(sql, new {
                    Id = model.Id,
                    ProductoId = model.ProductoId,
                    UnidadMedidaId = model.UnidadMedidaId,
                    CantidadEquivalente = model.CantidadEquivalente,
                    UserId = userId
                });
                return (true, "Presentación de venta actualizada exitosamente.", model.Id);
            }
        }

        public async Task<(bool success, string message)> DeletePresentacionVentaAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var prodId = await conn.QueryFirstOrDefaultAsync<int?>("SELECT producto_id FROM produ_p_vta WHERE produ_p_vta_id = @Id", new { Id = id });
            if (!prodId.HasValue)
                return (false, "Presentación no encontrada.");

            const string sql = @"
                UPDATE produ_p_vta SET estado = 'I', usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE produ_p_vta_id = @Id;
                UPDATE producto 
                SET tiene_pres_vta = (CASE WHEN EXISTS(SELECT 1 FROM produ_p_vta WHERE producto_id = @ProductoId AND estado = 'A') THEN 'S' ELSE 'N' END)
                WHERE producto_id = @ProductoId;";
            await conn.ExecuteAsync(sql, new { Id = id, ProductoId = prodId.Value, UserId = userId });
            return (true, "Presentación de venta eliminada exitosamente.");
        }

        // -------------------------------------------------------------------------
        // 8.2 Procesos de Producción (produ_s_p_p)
        // -------------------------------------------------------------------------
        public async Task<List<ProductoProcesoModel>> GetProductoProcesosAsync(int productoId)
        {
            if (productoId <= 0) return new List<ProductoProcesoModel>();
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    pspp.produ_s_p_p_id AS Id,
                    pspp.producto_id AS ProductoId,
                    pspp.sub_proce_p_id AS SubProcesoProduccionId,
                    spp.nombre AS SubProcesoNombre,
                    pp.proce_prod_id AS ProcesoProduccionId,
                    pp.nombre AS ProcesoNombre,
                    pspp.orden AS Orden,
                    pspp.estado AS Estado
                FROM produ_s_p_p pspp
                INNER JOIN sub_proce_p spp ON spp.sub_proce_p_id = pspp.sub_proce_p_id
                INNER JOIN proce_prod pp ON pp.proce_prod_id = spp.proce_prod_id
                WHERE pspp.producto_id = @ProductoId AND pspp.estado = 'A'
                ORDER BY pspp.orden ASC, spp.nombre ASC";
            var rows = await conn.QueryAsync<ProductoProcesoModel>(sql, new { ProductoId = productoId });
            return rows.ToList();
        }

        public async Task<(bool success, string message, int id)> SaveProductoProcesoAsync(ProductoProcesoModel model, int userId)
        {
            if (model.ProductoId <= 0)
                return (false, "El producto es inválido.", 0);
            if (model.SubProcesoProduccionId <= 0)
                return (false, "Debe seleccionar un sub-proceso de producción.", 0);
            if (model.Orden <= 0) model.Orden = 1;

            using var conn = _dbConnectionFactory.CreateConnection();
            if (model.Id == 0)
            {
                var exists = await conn.QueryFirstOrDefaultAsync<int?>(
                    "SELECT TOP 1 produ_s_p_p_id FROM produ_s_p_p WHERE producto_id = @ProductoId AND sub_proce_p_id = @SubProcesoId AND estado = 'A'",
                    new { ProductoId = model.ProductoId, SubProcesoId = model.SubProcesoProduccionId });
                if (exists.HasValue)
                    return (false, "Este sub-proceso ya está asignado al producto.", 0);

                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR produ_s_p_p_seq;
                    INSERT INTO produ_s_p_p (produ_s_p_p_id, producto_id, sub_proce_p_id, orden, estado, usuario_creacion_id, fecha_creacion)
                    VALUES (@newId, @ProductoId, @SubProcesoProduccionId, @Orden, 'A', @UserId, GETDATE());
                    SELECT @newId;";
                int newId = await conn.ExecuteScalarAsync<int>(sql, new {
                    ProductoId = model.ProductoId,
                    SubProcesoProduccionId = model.SubProcesoProduccionId,
                    Orden = model.Orden,
                    UserId = userId
                });
                return (true, "Proceso asignado exitosamente.", newId);
            }
            else
            {
                const string sql = @"
                    UPDATE produ_s_p_p 
                    SET orden = @Orden, usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                    WHERE produ_s_p_p_id = @Id;";
                await conn.ExecuteAsync(sql, new { Id = model.Id, Orden = model.Orden, UserId = userId });
                return (true, "Orden de proceso actualizado exitosamente.", model.Id);
            }
        }

        public async Task<(bool success, string message)> DeleteProductoProcesoAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "UPDATE produ_s_p_p SET estado = 'I', usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE produ_s_p_p_id = @Id";
            int rows = await conn.ExecuteAsync(sql, new { Id = id, UserId = userId });
            return (rows > 0, rows > 0 ? "Proceso removido exitosamente." : "Registro no encontrado.");
        }

        public async Task<List<SubProcesoLookupModel>> GetSubProcesosLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    spp.sub_proce_p_id AS SubProcesoId,
                    pp.proce_prod_id AS ProcesoId,
                    pp.nombre AS ProcesoNombre,
                    spp.nombre AS Nombre
                FROM sub_proce_p spp
                INNER JOIN proce_prod pp ON spp.proce_prod_id = pp.proce_prod_id
                WHERE spp.estado = 'A'
                ORDER BY pp.nombre ASC, spp.nombre ASC";
            var rows = await conn.QueryAsync<SubProcesoLookupModel>(sql);
            return rows.ToList();
        }

        // -------------------------------------------------------------------------
        // 8.3 Detalles Requeridos (produ_detalle)
        // -------------------------------------------------------------------------
        public async Task<List<ProductoDetalleModel>> GetProductoDetallesAsync(int productoId)
        {
            if (productoId <= 0) return new List<ProductoDetalleModel>();
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    pd.produ_detalle_id AS Id,
                    pd.producto_id AS ProductoId,
                    pd.nombre AS Nombre,
                    pd.estado AS Estado
                FROM produ_detalle pd
                WHERE pd.producto_id = @ProductoId AND pd.estado = 'A'
                ORDER BY pd.produ_detalle_id ASC";
            var rows = await conn.QueryAsync<ProductoDetalleModel>(sql, new { ProductoId = productoId });
            return rows.ToList();
        }

        public async Task<(bool success, string message, int id)> SaveProductoDetalleAsync(ProductoDetalleModel model, int userId)
        {
            if (model.ProductoId <= 0)
                return (false, "El producto es inválido.", 0);
            if (string.IsNullOrWhiteSpace(model.Nombre))
                return (false, "El nombre o especificación del detalle es obligatorio.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            if (model.Id == 0)
            {
                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR produ_detalle_seq;
                    INSERT INTO produ_detalle (produ_detalle_id, producto_id, nombre, estado, usuario_creacion_id, fecha_creacion)
                    VALUES (@newId, @ProductoId, @Nombre, 'A', @UserId, GETDATE());
                    SELECT @newId;";
                int newId = await conn.ExecuteScalarAsync<int>(sql, new {
                    ProductoId = model.ProductoId,
                    Nombre = model.Nombre.Trim(),
                    UserId = userId
                });
                return (true, "Detalle agregado exitosamente.", newId);
            }
            else
            {
                const string sql = @"
                    UPDATE produ_detalle 
                    SET nombre = @Nombre, usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                    WHERE produ_detalle_id = @Id;";
                await conn.ExecuteAsync(sql, new { Id = model.Id, Nombre = model.Nombre.Trim(), UserId = userId });
                return (true, "Detalle actualizado exitosamente.", model.Id);
            }
        }

        public async Task<(bool success, string message)> DeleteProductoDetalleAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "UPDATE produ_detalle SET estado = 'I', usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE produ_detalle_id = @Id";
            int rows = await conn.ExecuteAsync(sql, new { Id = id, UserId = userId });
            return (rows > 0, rows > 0 ? "Detalle eliminado exitosamente." : "Registro no encontrado.");
        }

        public async Task<(bool success, string message)> UpdateDetalleRequeridoAsync(int productoId, string detalleReq, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            string req = (detalleReq?.ToUpper() == "S") ? "S" : "N";
            const string sql = "UPDATE producto SET detalle_req = @Req, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE producto_id = @ProductoId";
            await conn.ExecuteAsync(sql, new { Req = req, ProductoId = productoId, UserId = userId });
            return (true, "Configuración de detalle actualizado.");
        }

        // -------------------------------------------------------------------------
        // 8.4 Color Rápido (color)
        // -------------------------------------------------------------------------
        public async Task<(bool success, string message, int id, string nombre)> QuickCreateColorAsync(string nombre, int userId)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return (false, "El nombre del color es obligatorio.", 0, "");

            using var conn = _dbConnectionFactory.CreateConnection();
            string nomTrim = nombre.Trim().ToUpper();
            var exist = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT color_id AS Id, nombre AS Nombre FROM color WHERE UPPER(nombre) = @Nom AND estado = 'A'", new { Nom = nomTrim });
            if (exist != null)
                return (true, "Color ya existe.", (int)exist.Id, (string)exist.Nombre);

            const string sql = @"
                DECLARE @newId INT = NEXT VALUE FOR color_seq;
                INSERT INTO color (color_id, nombre, estado, usuario_creacion_id, fecha_creacion)
                VALUES (@newId, @Nombre, 'A', @UserId, GETDATE());
                SELECT @newId;";
            int newId = await conn.ExecuteScalarAsync<int>(sql, new { Nombre = nomTrim, UserId = userId });
            return (true, "Color registrado exitosamente.", newId, nomTrim);
        }

        // =========================================================================
        // 9. PROVEEDOR
        // =========================================================================
        public async Task<List<ProveedorModel>> GetProveedoresAsync(
            string? search = null, 
            int? tipoProveedorId = null, 
            string? estado = null, 
            int? tipoNaturalezaId = null, 
            string? sort = null, 
            string? dir = null, 
            int top = 25,
            int empresaId = 1)
        {
            using var conn = _dbConnectionFactory.CreateConnection();

            int limit = top is 25 or 50 or 100 or 500 ? top : 25;

            string orderColumn = (sort?.ToLower()) switch
            {
                "codigo" => "p.proveedor",
                "rol" or "tipoproveedor" or "tipo" => "p.tipo_prove_id",
                "razon_social" or "razonsocial" or "nombre" => "p.razon_social",
                "documento" or "ruc" => "ISNULL(NULLIF(p.numero_ruc, ''), ISNULL(p.num_doc_iden, ''))",
                "ubicacion" or "distrito" => "dis.nombre",
                "contacto" => "p.contacto",
                "estado" => "p.estado",
                "id" => "p.proveedor_id",
                _ => "p.proveedor_id"
            };

            string orderDirection = (dir?.ToUpper()) == "ASC" ? "ASC" : "DESC";

            string sql = $@"
                SELECT TOP ({limit})
                    p.proveedor_id AS Id,
                    p.empresa_id AS EmpresaId,
                    p.proveedor AS Codigo,
                    p.tipo_nat_juri_id AS TipoNaturalezaJuridicaId,
                    ISNULL(nat.nombre, '') AS TipoNaturalezaJuridicaNombre,
                    ISNULL(nat.tipo_nat_juri, '') AS TipoNaturalezaJuridicaCodigo,
                    ISNULL(p.razon_social, '') AS RazonSocial,
                    ISNULL(p.abrev, '') AS Abreviatura,
                    ISNULL(p.nombres, '') AS Nombres,
                    ISNULL(p.apellido_pat, '') AS ApellidoPat,
                    ISNULL(p.apellido_mat, '') AS ApellidoMat,
                    p.tipo_doc_iden_id AS TipoDocumentoIdentidadId,
                    ISNULL(doc.abrev, '') AS TipoDocumentoIdentidadCodigo,
                    ISNULL(doc.nombre, '') AS TipoDocumentoIdentidadNombre,
                    ISNULL(doc.formato_valida, '') AS FormatoValidacion,
                    ISNULL(NULLIF(p.numero_ruc, ''), ISNULL(p.num_doc_iden, '')) AS Documento,
                    ISNULL(p.num_doc_iden, '') AS NumeroDocumentoIdentidad,
                    ISNULL(p.numero_ruc, '') AS NumeroRuc,
                    ISNULL(p.numero_cci, '') AS NumeroCCI,
                    ISNULL(p.contacto, '') AS Contacto,
                    p.tipo_via_id AS TipoViaId,
                    ISNULL(via.sigla, '') AS TipoViaSigla,
                    ISNULL(via.nombre, '') AS TipoViaNombre,
                    ISNULL(p.nombre_via, '') AS NombreVia,
                    ISNULL(p.numero_via, '') AS NumeroVia,
                    ISNULL(p.dpto, '') AS Dpto,
                    ISNULL(p.interior, '') AS Interior,
                    ISNULL(p.manzana, '') AS Manzana,
                    ISNULL(p.lote, '') AS Lote,
                    ISNULL(p.kilometro, '') AS Kilometro,
                    ISNULL(p.block, '') AS Block,
                    ISNULL(p.etapa, '') AS Etapa,
                    p.tipo_zona_id AS TipoZonaId,
                    ISNULL(zona.sigla, '') AS TipoZonaSigla,
                    ISNULL(zona.nombre, '') AS TipoZonaNombre,
                    ISNULL(p.nombre_zona, '') AS NombreZona,
                    ISNULL(p.referencia, '') AS Referencia,
                    p.distrito_id AS DistritoId,
                    ISNULL(dis.nombre, '') AS DistritoNombre,
                    ISNULL(pro.provincia_id, 0) AS ProvinciaId,
                    ISNULL(pro.nombre, '') AS ProvinciaNombre,
                    ISNULL(dep.departamento_id, 0) AS DepartamentoId,
                    ISNULL(dep.nombre, '') AS DepartamentoNombre,
                    ISNULL(p.telefono, '') AS Telefono,
                    ISNULL(p.email, '') AS Email,
                    ISNULL(p.web, '') AS Web,
                    p.tipo_prove_id AS TipoProveedorId,
                    ISNULL(tp.nombre, '') AS TipoProveedorNombre,
                    p.cond_pago_id AS CondicionPagoId,
                    ISNULL(cp.nombre, '') AS CondicionPagoNombre,
                    p.estado AS Estado
                FROM proveedor p
                LEFT JOIN tipo_nat_juri nat ON p.tipo_nat_juri_id = nat.tipo_nat_juri_id
                LEFT JOIN tipo_doc_iden doc ON p.tipo_doc_iden_id = doc.tipo_doc_iden_id
                LEFT JOIN tipo_via via ON p.tipo_via_id = via.tipo_via_id
                LEFT JOIN tipo_zona zona ON p.tipo_zona_id = zona.tipo_zona_id
                LEFT JOIN distrito dis ON p.distrito_id = dis.distrito_id
                LEFT JOIN provincia pro ON dis.provincia_id = pro.provincia_id
                LEFT JOIN departamento dep ON pro.departamento_id = dep.departamento_id
                LEFT JOIN tipo_prove tp ON p.tipo_prove_id = tp.tipo_prove_id
                LEFT JOIN cond_pago cp ON p.cond_pago_id = cp.cond_pago_id
                WHERE p.empresa_id = @EmpresaId
                  AND (@Search IS NULL
                       OR p.razon_social LIKE '%' + @Search + '%' 
                       OR p.nombres LIKE '%' + @Search + '%' 
                       OR p.apellido_pat LIKE '%' + @Search + '%'
                       OR p.proveedor LIKE '%' + @Search + '%' 
                       OR p.numero_ruc LIKE '%' + @Search + '%'
                       OR p.num_doc_iden LIKE '%' + @Search + '%')
                  AND (@TipoProveedorId IS NULL OR p.tipo_prove_id = @TipoProveedorId OR p.tipo_prove_id = 3)
                  AND (@Estado IS NULL OR p.estado = @Estado)
                  AND (@TipoNaturalezaId IS NULL OR p.tipo_nat_juri_id = @TipoNaturalezaId)
                ORDER BY {orderColumn} {orderDirection}";

            var rows = await conn.QueryAsync<ProveedorModel>(sql, new { 
                Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                EmpresaId = empresaId,
                TipoProveedorId = tipoProveedorId > 0 ? tipoProveedorId : null,
                Estado = string.IsNullOrWhiteSpace(estado) ? null : estado.Trim(),
                TipoNaturalezaId = tipoNaturalezaId > 0 ? tipoNaturalezaId : null
            });
            return rows.ToList();
        }

        public async Task<ProveedorModel?> GetProveedorByIdAsync(int id, int empresaId = 1)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    p.proveedor_id AS Id,
                    p.empresa_id AS EmpresaId,
                    p.proveedor AS Codigo,
                    p.tipo_nat_juri_id AS TipoNaturalezaJuridicaId,
                    ISNULL(nat.nombre, '') AS TipoNaturalezaJuridicaNombre,
                    ISNULL(nat.tipo_nat_juri, '') AS TipoNaturalezaJuridicaCodigo,
                    ISNULL(p.razon_social, '') AS RazonSocial,
                    ISNULL(p.abrev, '') AS Abreviatura,
                    ISNULL(p.nombres, '') AS Nombres,
                    ISNULL(p.apellido_pat, '') AS ApellidoPat,
                    ISNULL(p.apellido_mat, '') AS ApellidoMat,
                    p.tipo_doc_iden_id AS TipoDocumentoIdentidadId,
                    ISNULL(doc.abrev, '') AS TipoDocumentoIdentidadCodigo,
                    ISNULL(doc.nombre, '') AS TipoDocumentoIdentidadNombre,
                    ISNULL(doc.formato_valida, '') AS FormatoValidacion,
                    ISNULL(NULLIF(p.numero_ruc, ''), ISNULL(p.num_doc_iden, '')) AS Documento,
                    ISNULL(p.num_doc_iden, '') AS NumeroDocumentoIdentidad,
                    ISNULL(p.numero_ruc, '') AS NumeroRuc,
                    ISNULL(p.numero_cci, '') AS NumeroCCI,
                    ISNULL(p.contacto, '') AS Contacto,
                    p.tipo_via_id AS TipoViaId,
                    ISNULL(via.sigla, '') AS TipoViaSigla,
                    ISNULL(via.nombre, '') AS TipoViaNombre,
                    ISNULL(p.nombre_via, '') AS NombreVia,
                    ISNULL(p.numero_via, '') AS NumeroVia,
                    ISNULL(p.dpto, '') AS Dpto,
                    ISNULL(p.interior, '') AS Interior,
                    ISNULL(p.manzana, '') AS Manzana,
                    ISNULL(p.lote, '') AS Lote,
                    ISNULL(p.kilometro, '') AS Kilometro,
                    ISNULL(p.block, '') AS Block,
                    ISNULL(p.etapa, '') AS Etapa,
                    p.tipo_zona_id AS TipoZonaId,
                    ISNULL(zona.sigla, '') AS TipoZonaSigla,
                    ISNULL(zona.nombre, '') AS TipoZonaNombre,
                    ISNULL(p.nombre_zona, '') AS NombreZona,
                    ISNULL(p.referencia, '') AS Referencia,
                    p.distrito_id AS DistritoId,
                    ISNULL(dis.nombre, '') AS DistritoNombre,
                    ISNULL(pro.provincia_id, 0) AS ProvinciaId,
                    ISNULL(pro.nombre, '') AS ProvinciaNombre,
                    ISNULL(dep.departamento_id, 0) AS DepartamentoId,
                    ISNULL(dep.nombre, '') AS DepartamentoNombre,
                    ISNULL(p.telefono, '') AS Telefono,
                    ISNULL(p.email, '') AS Email,
                    ISNULL(p.web, '') AS Web,
                    p.tipo_prove_id AS TipoProveedorId,
                    ISNULL(tp.nombre, '') AS TipoProveedorNombre,
                    p.cond_pago_id AS CondicionPagoId,
                    ISNULL(cp.nombre, '') AS CondicionPagoNombre,
                    p.estado AS Estado
                FROM proveedor p
                LEFT JOIN tipo_nat_juri nat ON p.tipo_nat_juri_id = nat.tipo_nat_juri_id
                LEFT JOIN tipo_doc_iden doc ON p.tipo_doc_iden_id = doc.tipo_doc_iden_id
                LEFT JOIN tipo_via via ON p.tipo_via_id = via.tipo_via_id
                LEFT JOIN tipo_zona zona ON p.tipo_zona_id = zona.tipo_zona_id
                LEFT JOIN distrito dis ON p.distrito_id = dis.distrito_id
                LEFT JOIN provincia pro ON dis.provincia_id = pro.provincia_id
                LEFT JOIN departamento dep ON pro.departamento_id = dep.departamento_id
                LEFT JOIN tipo_prove tp ON p.tipo_prove_id = tp.tipo_prove_id
                LEFT JOIN cond_pago cp ON p.cond_pago_id = cp.cond_pago_id
                WHERE p.proveedor_id = @Id AND p.empresa_id = @EmpresaId";
            return await conn.QueryFirstOrDefaultAsync<ProveedorModel>(sql, new { Id = id, EmpresaId = empresaId });
        }

        public async Task<ProveedorModel?> BuscarProveedorPorDocumentoAsync(string doc, int empresaId = 1)
        {
            if (string.IsNullOrWhiteSpace(doc)) return null;
            doc = doc.Trim();

            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT TOP 1
                    p.proveedor_id AS Id,
                    p.empresa_id AS EmpresaId,
                    p.proveedor AS Codigo,
                    p.tipo_nat_juri_id AS TipoNaturalezaJuridicaId,
                    ISNULL(nat.nombre, '') AS TipoNaturalezaJuridicaNombre,
                    ISNULL(p.razon_social, '') AS RazonSocial,
                    ISNULL(p.abrev, '') AS Abreviatura,
                    ISNULL(p.nombres, '') AS Nombres,
                    ISNULL(p.apellido_pat, '') AS ApellidoPat,
                    ISNULL(p.apellido_mat, '') AS ApellidoMat,
                    p.tipo_doc_iden_id AS TipoDocumentoIdentidadId,
                    ISNULL(p.num_doc_iden, '') AS NumeroDocumentoIdentidad,
                    ISNULL(p.numero_ruc, '') AS NumeroRuc,
                    ISNULL(p.numero_cci, '') AS NumeroCCI,
                    ISNULL(p.contacto, '') AS Contacto,
                    p.tipo_via_id AS TipoViaId,
                    ISNULL(p.nombre_via, '') AS NombreVia,
                    ISNULL(p.numero_via, '') AS NumeroVia,
                    ISNULL(p.interior, '') AS Interior,
                    ISNULL(p.manzana, '') AS Manzana,
                    ISNULL(p.lote, '') AS Lote,
                    p.tipo_zona_id AS TipoZonaId,
                    ISNULL(p.nombre_zona, '') AS NombreZona,
                    ISNULL(p.referencia, '') AS Referencia,
                    p.distrito_id AS DistritoId,
                    ISNULL(dis.nombre, '') AS DistritoNombre,
                    ISNULL(pro.provincia_id, 0) AS ProvinciaId,
                    ISNULL(pro.nombre, '') AS ProvinciaNombre,
                    ISNULL(dep.departamento_id, 0) AS DepartamentoId,
                    ISNULL(dep.nombre, '') AS DepartamentoNombre,
                    ISNULL(p.telefono, '') AS Telefono,
                    ISNULL(p.email, '') AS Email,
                    ISNULL(p.web, '') AS Web,
                    p.tipo_prove_id AS TipoProveedorId,
                    p.cond_pago_id AS CondicionPagoId,
                    p.estado AS Estado
                FROM proveedor p
                LEFT JOIN tipo_nat_juri nat ON p.tipo_nat_juri_id = nat.tipo_nat_juri_id
                LEFT JOIN distrito dis ON p.distrito_id = dis.distrito_id
                LEFT JOIN provincia pro ON dis.provincia_id = pro.provincia_id
                LEFT JOIN departamento dep ON pro.departamento_id = dep.departamento_id
                WHERE p.empresa_id = @EmpresaId AND (p.numero_ruc = @Doc OR p.num_doc_iden = @Doc)
                ORDER BY p.proveedor_id DESC";

            return await conn.QueryFirstOrDefaultAsync<ProveedorModel>(sql, new { Doc = doc, EmpresaId = empresaId });
        }

        public async Task<(bool success, string message, int id, string codigo)> SaveProveedorAsync(ProveedorModel model, int userId, int empresaId = 1)
        {
            // Lógica de Naturaleza Jurídica (Natural vs. Jurídica)
            bool esNatural = model.TipoNaturalezaJuridicaId == 1;
            string razonSocialFinal;

            if (esNatural)
            {
                if (string.IsNullOrWhiteSpace(model.Nombres) || string.IsNullOrWhiteSpace(model.ApellidoPat))
                    return (false, "Para personas naturales es obligatorio ingresar Nombres y Apellido Paterno.", 0, "");

                razonSocialFinal = $"{model.ApellidoPat.Trim()} {(string.IsNullOrWhiteSpace(model.ApellidoMat) ? "" : model.ApellidoMat.Trim() + " ")}{model.Nombres.Trim()}".Trim();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(model.RazonSocial))
                    return (false, "Para personas jurídicas la Razón Social es obligatoria.", 0, "");

                razonSocialFinal = model.RazonSocial.Trim();
                if (string.IsNullOrWhiteSpace(model.NumeroDocumentoIdentidad) && !string.IsNullOrWhiteSpace(model.NumeroRuc))
                    model.NumeroDocumentoIdentidad = model.NumeroRuc.Trim();
            }

            using var conn = _dbConnectionFactory.CreateConnection();
            if (conn.State != System.Data.ConnectionState.Open) conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                if (model.Id == 0)
                {
                    // Generación del Código Oficial Correlativo desde la tabla 'tabla' (tabla='0003' proveedor)
                    string codigoFinal = model.Codigo?.Trim().ToUpper() ?? "";
                    if (string.IsNullOrWhiteSpace(codigoFinal))
                    {
                        const string sqlCorrelativo = @"
                            UPDATE tabla 
                            SET correlativo = correlativo + 1 
                            WHERE tabla = '0003';

                            SELECT RIGHT('0000000' + CAST(correlativo AS VARCHAR(10)), 7) 
                            FROM tabla 
                            WHERE tabla = '0003';";

                        var correlativoGenerado = await conn.ExecuteScalarAsync<string>(sqlCorrelativo, transaction: tx);
                        codigoFinal = !string.IsNullOrWhiteSpace(correlativoGenerado) ? correlativoGenerado : DateTime.Now.ToString("mmssfff");
                    }

                    codigoFinal = SafeTruncate(codigoFinal, 7);

                    const string sqlInsert = @"
                        DECLARE @newId INT = NEXT VALUE FOR proveedor_seq;
                        INSERT INTO proveedor (
                            proveedor_id, empresa_id, proveedor, tipo_nat_juri_id, razon_social, abrev, 
                            apellido_pat, apellido_mat, nombres, tipo_doc_iden_id, num_doc_iden, numero_ruc, 
                            numero_cci, contacto, tipo_via_id, nombre_via, numero_via, dpto, interior, 
                            manzana, lote, kilometro, block, etapa, tipo_zona_id, nombre_zona, referencia, 
                            telefono, distrito_id, email, web, tipo_prove_id, cond_pago_id, origen_registro_id, 
                            es_local, es_prospecto, num_doc_iden2, cargo, cod_externo, estado, 
                            usuario_creacion_id, fecha_creacion
                        )
                        VALUES (
                            @newId, @EmpresaId, @Codigo, @TipoNatJuriId, @RazonSocial, @Abrev,
                            @ApellidoPat, @ApellidoMat, @Nombres, @TipoDocId, @Doc, @Ruc, 
                            @NumeroCCI, @Contacto, @TipoViaId, @NombreVia, @NumeroVia, @Dpto, @Interior, 
                            @Manzana, @Lote, @Kilometro, @Block, @Etapa, @TipoZonaId, @NombreZona, @Referencia, 
                            @Telefono, @DistritoId, @Email, @Web, @TipoProveedorId, @CondPagoId, 2, 
                            @EsLocal, @EsProspecto, '', '', '', @Estado, 
                            @UserId, GETDATE()
                        );
                        SELECT @newId;";

                    int newId = await conn.ExecuteScalarAsync<int>(sqlInsert, new {
                        EmpresaId = empresaId,
                        Codigo = codigoFinal,
                        TipoNatJuriId = model.TipoNaturalezaJuridicaId > 0 ? model.TipoNaturalezaJuridicaId : (esNatural ? 1 : 2),
                        RazonSocial = SafeTruncate(razonSocialFinal, 150),
                        Abrev = SafeTruncate(model.Abreviatura, 50),
                        ApellidoPat = esNatural ? SafeTruncate(model.ApellidoPat, 50) : "",
                        ApellidoMat = esNatural ? SafeTruncate(model.ApellidoMat, 50) : "",
                        Nombres = esNatural ? SafeTruncate(model.Nombres, 50) : "",
                        TipoDocId = model.TipoDocumentoIdentidadId > 0 ? model.TipoDocumentoIdentidadId : (esNatural ? 1 : 3),
                        Doc = SafeTruncate(model.NumeroDocumentoIdentidad, 35),
                        Ruc = SafeTruncate(model.NumeroRuc, 35),
                        NumeroCCI = SafeTruncate(model.NumeroCCI, 20),
                        Contacto = SafeTruncate(model.Contacto, 150),
                        TipoViaId = model.TipoViaId > 0 ? model.TipoViaId : 12,
                        NombreVia = SafeTruncate(model.NombreVia, 50),
                        NumeroVia = SafeTruncate(model.NumeroVia, 10),
                        Dpto = SafeTruncate(model.Dpto, 10),
                        Interior = SafeTruncate(model.Interior, 10),
                        Manzana = SafeTruncate(model.Manzana, 10),
                        Lote = SafeTruncate(model.Lote, 10),
                        Kilometro = SafeTruncate(model.Kilometro, 10),
                        Block = SafeTruncate(model.Block, 10),
                        Etapa = SafeTruncate(model.Etapa, 10),
                        TipoZonaId = model.TipoZonaId > 0 ? model.TipoZonaId : 12,
                        NombreZona = SafeTruncate(model.NombreZona, 25),
                        Referencia = SafeTruncate(model.Referencia, 50),
                        Telefono = SafeTruncate(model.Telefono, 15),
                        DistritoId = model.DistritoId > 0 ? model.DistritoId : 1281,
                        Email = SafeTruncate(model.Email, 60),
                        Web = SafeTruncate(model.Web, 60),
                        TipoProveedorId = model.TipoProveedorId > 0 ? model.TipoProveedorId : 1,
                        CondPagoId = model.CondicionPagoId > 0 ? model.CondicionPagoId : 1,
                        EsLocal = string.IsNullOrWhiteSpace(model.EsLocal) ? "S" : SafeTruncate(model.EsLocal, 1),
                        EsProspecto = string.IsNullOrWhiteSpace(model.EsProspecto) ? "N" : SafeTruncate(model.EsProspecto, 1),
                        Estado = string.IsNullOrWhiteSpace(model.Estado) ? "A" : SafeTruncate(model.Estado, 1),
                        UserId = userId
                    }, transaction: tx);

                    tx.Commit();
                    return (true, $"Registro '{razonSocialFinal}' creado con código {codigoFinal}.", newId, codigoFinal);
                }
                else
                {
                    const string sqlUpdate = @"
                        UPDATE proveedor 
                        SET tipo_nat_juri_id = @TipoNatJuriId,
                            razon_social = @RazonSocial,
                            abrev = @Abrev,
                            apellido_pat = @ApellidoPat,
                            apellido_mat = @ApellidoMat,
                            nombres = @Nombres,
                            tipo_doc_iden_id = @TipoDocId,
                            num_doc_iden = @Doc,
                            numero_ruc = @Ruc,
                            numero_cci = @NumeroCCI,
                            contacto = @Contacto,
                            tipo_via_id = @TipoViaId,
                            nombre_via = @NombreVia,
                            numero_via = @NumeroVia,
                            dpto = @Dpto,
                            interior = @Interior,
                            manzana = @Manzana,
                            lote = @Lote,
                            kilometro = @Kilometro,
                            block = @Block,
                            etapa = @Etapa,
                            tipo_zona_id = @TipoZonaId,
                            nombre_zona = @NombreZona,
                            referencia = @Referencia,
                            telefono = @Telefono,
                            distrito_id = @DistritoId,
                            email = @Email,
                            web = @Web,
                            tipo_prove_id = @TipoProveedorId,
                            cond_pago_id = @CondPagoId,
                            es_local = @EsLocal,
                            es_prospecto = @EsProspecto,
                            estado = @Estado,
                            usuario_modifica_id = @UserId,
                            fecha_modifica = GETDATE()
                        WHERE proveedor_id = @Id AND empresa_id = @EmpresaId";

                    await conn.ExecuteAsync(sqlUpdate, new {
                        Id = model.Id,
                        EmpresaId = empresaId,
                        TipoNatJuriId = model.TipoNaturalezaJuridicaId > 0 ? model.TipoNaturalezaJuridicaId : (esNatural ? 1 : 2),
                        RazonSocial = SafeTruncate(razonSocialFinal, 150),
                        Abrev = SafeTruncate(model.Abreviatura, 50),
                        ApellidoPat = esNatural ? SafeTruncate(model.ApellidoPat, 50) : "",
                        ApellidoMat = esNatural ? SafeTruncate(model.ApellidoMat, 50) : "",
                        Nombres = esNatural ? SafeTruncate(model.Nombres, 50) : "",
                        TipoDocId = model.TipoDocumentoIdentidadId > 0 ? model.TipoDocumentoIdentidadId : (esNatural ? 1 : 3),
                        Doc = SafeTruncate(model.NumeroDocumentoIdentidad, 35),
                        Ruc = SafeTruncate(model.NumeroRuc, 35),
                        NumeroCCI = SafeTruncate(model.NumeroCCI, 20),
                        Contacto = SafeTruncate(model.Contacto, 150),
                        TipoViaId = model.TipoViaId > 0 ? model.TipoViaId : 12,
                        NombreVia = SafeTruncate(model.NombreVia, 50),
                        NumeroVia = SafeTruncate(model.NumeroVia, 10),
                        Dpto = SafeTruncate(model.Dpto, 10),
                        Interior = SafeTruncate(model.Interior, 10),
                        Manzana = SafeTruncate(model.Manzana, 10),
                        Lote = SafeTruncate(model.Lote, 10),
                        Kilometro = SafeTruncate(model.Kilometro, 10),
                        Block = SafeTruncate(model.Block, 10),
                        Etapa = SafeTruncate(model.Etapa, 10),
                        TipoZonaId = model.TipoZonaId > 0 ? model.TipoZonaId : 12,
                        NombreZona = SafeTruncate(model.NombreZona, 25),
                        Referencia = SafeTruncate(model.Referencia, 50),
                        Telefono = SafeTruncate(model.Telefono, 15),
                        DistritoId = model.DistritoId > 0 ? model.DistritoId : 1281,
                        Email = SafeTruncate(model.Email, 60),
                        Web = SafeTruncate(model.Web, 60),
                        TipoProveedorId = model.TipoProveedorId > 0 ? model.TipoProveedorId : 1,
                        CondPagoId = model.CondicionPagoId > 0 ? model.CondicionPagoId : 1,
                        EsLocal = string.IsNullOrWhiteSpace(model.EsLocal) ? "S" : SafeTruncate(model.EsLocal, 1),
                        EsProspecto = string.IsNullOrWhiteSpace(model.EsProspecto) ? "N" : SafeTruncate(model.EsProspecto, 1),
                        Estado = string.IsNullOrWhiteSpace(model.Estado) ? "A" : SafeTruncate(model.Estado, 1),
                        UserId = userId
                    }, transaction: tx);

                    tx.Commit();
                    return (true, $"Registro '{razonSocialFinal}' actualizado exitosamente.", model.Id, model.Codigo);
                }
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                return (false, "Error al guardar: " + ex.Message, 0, "");
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoProveedorAsync(int id, int userId, int empresaId = 1)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var item = await conn.QueryFirstOrDefaultAsync<ProveedorModel>("SELECT proveedor_id AS Id, estado AS Estado FROM proveedor WHERE proveedor_id = @Id AND empresa_id = @EmpresaId", new { Id = id, EmpresaId = empresaId });
            if (item == null) return (false, "Registro no encontrado.", "I");

            string nuevoEstado = item.Estado == "A" ? "I" : "A";
            const string sql = "UPDATE proveedor SET estado = @NuevoEstado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE proveedor_id = @Id AND empresa_id = @EmpresaId";
            await conn.ExecuteAsync(sql, new { Id = id, EmpresaId = empresaId, NuevoEstado = nuevoEstado, UserId = userId });
            return (true, $"Registro {(nuevoEstado == "A" ? "activado" : "desactivado")} exitosamente.", nuevoEstado);
        }

        public async Task<List<LookupItemModel>> GetTiposNaturalezaJuridicaLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_nat_juri_id AS Id, tipo_nat_juri AS Codigo, nombre AS Nombre FROM tipo_nat_juri WHERE estado = 'A' ORDER BY tipo_nat_juri_id";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<TipoDocIdentidadLookupModel>> GetTiposDocumentoIdentidadLookupAsync(int? tipoNaturalezaJuridicaId = null)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            string sql;
            if (tipoNaturalezaJuridicaId == 2) // Jurídica -> Solo RUC
            {
                sql = "SELECT tipo_doc_iden_id AS Id, tipo_doc_iden AS Codigo, nombre AS Nombre, abrev AS Abreviatura, formato_valida AS FormatoValidacion FROM tipo_doc_iden WHERE estado = 'A' AND tipo_doc_iden = '06' ORDER BY nombre";
            }
            else if (tipoNaturalezaJuridicaId == 1) // Natural -> DNI, CE, Pasaporte, etc.
            {
                sql = "SELECT tipo_doc_iden_id AS Id, tipo_doc_iden AS Codigo, nombre AS Nombre, abrev AS Abreviatura, formato_valida AS FormatoValidacion FROM tipo_doc_iden WHERE estado = 'A' AND tipo_doc_iden != '06' ORDER BY nombre";
            }
            else
            {
                sql = "SELECT tipo_doc_iden_id AS Id, tipo_doc_iden AS Codigo, nombre AS Nombre, abrev AS Abreviatura, formato_valida AS FormatoValidacion FROM tipo_doc_iden WHERE estado = 'A' ORDER BY nombre";
            }

            var rows = await conn.QueryAsync<TipoDocIdentidadLookupModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetTiposViaLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_via_id AS Id, sigla AS Codigo, nombre AS Nombre FROM tipo_via WHERE estado = 'A' ORDER BY nombre";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetTiposZonaLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_zona_id AS Id, sigla AS Codigo, nombre AS Nombre FROM tipo_zona WHERE estado = 'A' ORDER BY nombre";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetTiposProveedorLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_prove_id AS Id, tipo_prove AS Codigo, nombre AS Nombre FROM tipo_prove ORDER BY tipo_prove_id";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetCondicionesPagoLookupAsync(int empresaId = 1)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT cond_pago_id AS Id, CAST(cond_pago_id AS VARCHAR) AS Codigo, nombre AS Nombre FROM cond_pago WHERE estado = 'A' AND empresa_id = @EmpresaId ORDER BY cond_pago_id";
            var rows = await conn.QueryAsync<LookupItemModel>(sql, new { EmpresaId = empresaId });
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetDepartamentosLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT departamento_id AS Id, departamento AS Codigo, nombre AS Nombre FROM departamento WHERE estado = 'A' ORDER BY nombre";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<UbigeoLookupModel>> GetProvinciasLookupAsync(int departamentoId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT provincia_id AS Id, departamento_id AS PadreId, provincia AS Codigo, nombre AS Nombre FROM provincia WHERE departamento_id = @DepartamentoId AND estado = 'A' ORDER BY nombre";
            var rows = await conn.QueryAsync<UbigeoLookupModel>(sql, new { DepartamentoId = departamentoId });
            return rows.ToList();
        }

        public async Task<List<UbigeoLookupModel>> GetDistritosLookupAsync(int provinciaId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT distrito_id AS Id, provincia_id AS PadreId, distrito AS Codigo, nombre AS Nombre FROM distrito WHERE provincia_id = @ProvinciaId AND estado = 'A' ORDER BY nombre";
            var rows = await conn.QueryAsync<UbigeoLookupModel>(sql, new { ProvinciaId = provinciaId });
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

        // -------------------------------------------------------------------------
        // 10.1 Gestión Integral de Combos y Kits (Bandeja, Cabecera y Artículos)
        // -------------------------------------------------------------------------
        public async Task<List<LookupItemModel>> GetGruposCombosLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT grupo_producto_id AS Id, grupo_producto AS Codigo, nombre AS Nombre 
                FROM grupo_producto 
                WHERE estado = 'A' AND (es_combo = 'S' OR tipo_producto_id = 2)
                ORDER BY tipo_producto_id, nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            var list = rows.ToList();
            if (list.Count == 0)
            {
                return await GetGruposProductoLookupAsync(1);
            }
            return list;
        }

        public async Task<List<LookupItemModel>> GetClasesCombosLookupAsync(int? grupoId = null)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT c.clase_producto_id AS Id, c.clase_producto AS Codigo, c.nombre AS Nombre 
                FROM clase_producto c
                INNER JOIN grupo_producto g ON g.grupo_producto_id = c.grupo_producto_id
                WHERE c.estado = 'A' 
                  AND (g.es_combo = 'S' OR g.tipo_producto_id = 2)
                  AND (@GrupoId IS NULL OR c.grupo_producto_id = @GrupoId)
                ORDER BY c.nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql, new { GrupoId = (grupoId > 0) ? grupoId : null });
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetFamiliasCombosLookupAsync(int? grupoId = null, int? claseId = null)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            string sql = @"
                SELECT f.familia_producto_id AS Id, f.familia_producto AS Codigo, f.nombre AS Nombre 
                FROM familia_producto f
                INNER JOIN clase_producto c ON f.clase_producto_id = c.clase_producto_id
                INNER JOIN grupo_producto g ON c.grupo_producto_id = g.grupo_producto_id
                WHERE f.estado = 'A'
                  AND (g.es_combo = 'S' OR g.tipo_producto_id = 2)
                  AND (@GrupoId IS NULL OR g.grupo_producto_id = @GrupoId)
                  AND (@ClaseId IS NULL OR f.clase_producto_id = @ClaseId)
                ORDER BY f.nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql, new { 
                GrupoId = (grupoId > 0) ? grupoId : null,
                ClaseId = (claseId > 0) ? claseId : null 
            });
            return rows.ToList();
        }

        public async Task<List<ProductoComboModel>> GetCombosBandejaAsync(
            string? search = null,
            int? grupoId = null,
            int? claseId = null,
            int? familiaId = null,
            string? estado = null,
            string? sort = null,
            string? dir = null,
            int top = 25)
        {
            using var conn = _dbConnectionFactory.CreateConnection();

            int limit = top switch
            {
                25 => 25,
                50 => 50,
                100 => 100,
                500 => 500,
                _ => (top > 0 ? top : 25)
            };

            string orderColumn = (sort?.ToLower()) switch
            {
                "codigo" => "p.producto",
                "descripcion" => "p.descripcion",
                "familia" => "fp.nombre",
                "um" => "um.abrev",
                "items" => "ItemsCount",
                "estado" => "p.estado",
                "id" => "p.producto_id",
                _ => "p.producto_id"
            };

            string orderDirection = (dir?.ToUpper()) == "ASC" ? "ASC" : "DESC";

            string sql = $@"
                SELECT TOP ({limit})
                    p.producto_id AS Id,
                    p.producto AS Codigo,
                    p.descripcion AS Descripcion,
                    p.unidad_medida_id AS UnidadMedidaId,
                    ISNULL(um.abrev, um.nombre) AS UnidadMedida,
                    gp.tipo_producto_id AS TipoProductoId,
                    gp.grupo_producto_id AS GrupoId,
                    gp.nombre AS GrupoNombre,
                    cprd.clase_producto_id AS ClaseId,
                    cprd.nombre AS ClaseNombre,
                    fp.familia_producto_id AS FamiliaId,
                    fp.nombre AS FamiliaNombre,
                    p.estado_producto_id AS EstadoProductoId,
                    p.estado_com_prod_id AS EstadoComProdId,
                    p.estado AS Estado,
                    (SELECT COUNT(*) FROM combo_producto cp WHERE cp.producto_id = p.producto_id AND cp.estado = 'A') AS ItemsCount
                FROM producto p
                INNER JOIN familia_producto fp ON fp.familia_producto_id = p.familia_producto_id
                INNER JOIN clase_producto cprd ON cprd.clase_producto_id = fp.clase_producto_id
                INNER JOIN grupo_producto gp ON gp.grupo_producto_id = cprd.grupo_producto_id
                LEFT JOIN unidad_medida um ON um.unidad_medida_id = p.unidad_medida_id
                WHERE (gp.es_combo = 'S' OR gp.tipo_producto_id = 2 OR p.producto_id IN (SELECT DISTINCT producto_id FROM combo_producto))
                  AND (@Search IS NULL OR p.producto LIKE '%' + @Search + '%' OR p.descripcion LIKE '%' + @Search + '%')
                  AND (@GrupoId IS NULL OR gp.grupo_producto_id = @GrupoId)
                  AND (@ClaseId IS NULL OR cprd.clase_producto_id = @ClaseId)
                  AND (@FamiliaId IS NULL OR fp.familia_producto_id = @FamiliaId)
                  AND (@Estado IS NULL OR p.estado = @Estado)
                ORDER BY {orderColumn} {orderDirection}";

            var rows = await conn.QueryAsync<ProductoComboModel>(sql, new {
                Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                GrupoId = (grupoId > 0) ? grupoId : null,
                ClaseId = (claseId > 0) ? claseId : null,
                FamiliaId = (familiaId > 0) ? familiaId : null,
                Estado = string.IsNullOrWhiteSpace(estado) ? null : estado.Trim()
            });

            return rows.ToList();
        }

        public async Task<ProductoComboModel?> GetComboCabeceraByIdAsync(int id)
        {
            if (id <= 0) return null;
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    p.producto_id AS Id,
                    p.producto AS Codigo,
                    p.descripcion AS Descripcion,
                    p.unidad_medida_id AS UnidadMedidaId,
                    ISNULL(um.abrev, um.nombre) AS UnidadMedida,
                    gp.tipo_producto_id AS TipoProductoId,
                    gp.grupo_producto_id AS GrupoId,
                    gp.nombre AS GrupoNombre,
                    cprd.clase_producto_id AS ClaseId,
                    cprd.nombre AS ClaseNombre,
                    fp.familia_producto_id AS FamiliaId,
                    fp.nombre AS FamiliaNombre,
                    p.estado_producto_id AS EstadoProductoId,
                    p.estado_com_prod_id AS EstadoComProdId,
                    p.estado AS Estado,
                    (SELECT COUNT(*) FROM combo_producto cp WHERE cp.producto_id = p.producto_id AND cp.estado = 'A') AS ItemsCount
                FROM producto p
                INNER JOIN familia_producto fp ON fp.familia_producto_id = p.familia_producto_id
                INNER JOIN clase_producto cprd ON cprd.clase_producto_id = fp.clase_producto_id
                INNER JOIN grupo_producto gp ON gp.grupo_producto_id = cprd.grupo_producto_id
                LEFT JOIN unidad_medida um ON um.unidad_medida_id = p.unidad_medida_id
                WHERE p.producto_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<ProductoComboModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id, string codigo)> SaveComboCabeceraAsync(ProductoComboModel model, int userId)
        {
            if (string.IsNullOrWhiteSpace(model.Descripcion))
                return (false, "La descripción del combo o kit es obligatoria.", 0, "");
            if (model.Id == 0 && model.FamiliaId <= 0)
                return (false, "Debe seleccionar la familia del combo.", 0, "");

            using var conn = _dbConnectionFactory.CreateConnection();
            if (conn.State != ConnectionState.Open)
                conn.Open();
            using var transaction = conn.BeginTransaction();
            if (model.Id == 0)
            {
                var sku = await GenerarCodigoSkuAsync(conn, transaction, model.FamiliaId, null, requireComboGroup: false);
                if (!sku.success)
                    return (false, sku.message, 0, string.Empty);
                string codigo = sku.codigo;

                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR producto_seq;
                    INSERT INTO producto (
                        producto_id, producto, descripcion, cod_referencial, familia_producto_id, unidad_medida_id,
                        tipo_igv_id, peso_neto, peso_bruto, valor_repo, volumen, tiene_pres_vta, es_opera, es_perecible,
                        detalle_req, master_pack, estado_producto_id, estado_com_prod_id, estado, fecha_alta,
                        usuario_creacion_id, fecha_creacion
                    )
                    VALUES (
                        @newId, @Codigo, @Descripcion, '', @FamiliaId, @UnidadMedidaId,
                        1, 0, 0, 0, 0, 'N', 'N', 'N',
                        'N', 1, @EstadoProductoId, @EstadoComProdId, @Estado, GETDATE(),
                        @UserId, GETDATE()
                    );
                    SELECT @newId;";

                int newId = await conn.ExecuteScalarAsync<int>(sql, new {
                    Codigo = codigo,
                    Descripcion = model.Descripcion.Trim(),
                    FamiliaId = model.FamiliaId,
                    UnidadMedidaId = model.UnidadMedidaId > 0 ? model.UnidadMedidaId : 1,
                    EstadoProductoId = model.EstadoProductoId > 0 ? model.EstadoProductoId : 1,
                    EstadoComProdId = model.EstadoComProdId > 0 ? model.EstadoComProdId : 1,
                    Estado = model.Estado ?? "A",
                    UserId = userId
                }, transaction);

                transaction.Commit();
                return (true, "Combo registrado exitosamente.", newId, codigo);
            }
            else
            {
                var existingId = await conn.QueryFirstOrDefaultAsync<int?>(
                    @"SELECT p.producto_id FROM producto p WHERE p.producto_id = @Id",
                    new { model.Id },
                    transaction);
                if (!existingId.HasValue)
                    return (false, "Combo no encontrado.", 0, string.Empty);

                const string sql = @"
                    UPDATE producto 
                    SET descripcion = @Descripcion,
                        unidad_medida_id = @UnidadMedidaId,
                        estado_producto_id = @EstadoProductoId,
                        estado_com_prod_id = @EstadoComProdId,
                        estado = @Estado,
                        usuario_modifica_id = @UserId,
                        fecha_modifica = GETDATE()
                    WHERE producto_id = @Id";

                await conn.ExecuteAsync(sql, new {
                    Id = model.Id,
                    Descripcion = model.Descripcion.Trim(),
                    UnidadMedidaId = model.UnidadMedidaId > 0 ? model.UnidadMedidaId : 1,
                    EstadoProductoId = model.EstadoProductoId > 0 ? model.EstadoProductoId : 1,
                    EstadoComProdId = model.EstadoComProdId > 0 ? model.EstadoComProdId : 1,
                    Estado = model.Estado ?? "A",
                    UserId = userId
                }, transaction);

                transaction.Commit();
                return (true, "Combo actualizado exitosamente.", model.Id, model.Codigo ?? "");
            }
        }

        public async Task<List<ComboDetalleItemModel>> GetComboDetallesAsync(int productoId)
        {
            if (productoId <= 0) return new List<ComboDetalleItemModel>();
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    cp.combo_producto_id AS Id,
                    cp.producto_id AS ProductoId,
                    cp.prod_inclu_id AS ProdIncluId,
                    pi.producto AS Codigo,
                    pi.descripcion AS Descripcion,
                    ISNULL(um.abrev, um.nombre) AS UnidadMedida,
                    cp.cantidad AS Cantidad,
                    cp.porc_precio AS PorcentajePrecio,
                    cp.num_dec_redo AS NumDecRedo,
                    cp.estado AS Estado
                FROM combo_producto cp
                INNER JOIN producto pi ON pi.producto_id = cp.prod_inclu_id
                LEFT JOIN unidad_medida um ON um.unidad_medida_id = pi.unidad_medida_id
                WHERE cp.producto_id = @ProductoId AND cp.estado = 'A'
                ORDER BY pi.descripcion ASC";
            var rows = await conn.QueryAsync<ComboDetalleItemModel>(sql, new { ProductoId = productoId });
            return rows.ToList();
        }

        public async Task<(bool success, string message, int id)> SaveComboDetalleAsync(ComboDetalleItemModel model, int userId)
        {
            if (model.ProductoId <= 0)
                return (false, "El combo principal es inválido.", 0);
            if (model.ProdIncluId <= 0)
                return (false, "Debe seleccionar el artículo a incluir.", 0);
            if (model.ProductoId == model.ProdIncluId)
                return (false, "Un combo no puede incluirse a sí mismo.", 0);
            if (model.Cantidad <= 0)
                return (false, "La cantidad debe ser mayor a cero.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            if (model.Id == 0)
            {
                var exists = await conn.QueryFirstOrDefaultAsync<int?>(
                    "SELECT TOP 1 combo_producto_id FROM combo_producto WHERE producto_id = @ProductoId AND prod_inclu_id = @ProdIncluId AND estado = 'A'",
                    new { ProductoId = model.ProductoId, ProdIncluId = model.ProdIncluId });
                if (exists.HasValue)
                    return (false, "Este artículo ya forma parte del combo.", 0);

                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR combo_producto_seq;
                    INSERT INTO combo_producto (
                        combo_producto_id, producto_id, prod_inclu_id, cantidad, porc_precio, num_dec_redo,
                        estado, usuario_creacion_id, fecha_creacion
                    )
                    VALUES (
                        @newId, @ProductoId, @ProdIncluId, @Cantidad, @PorcentajePrecio, @NumDecRedo,
                        'A', @UserId, GETDATE()
                    );
                    SELECT @newId;";

                int newId = await conn.ExecuteScalarAsync<int>(sql, new {
                    ProductoId = model.ProductoId,
                    ProdIncluId = model.ProdIncluId,
                    Cantidad = model.Cantidad,
                    PorcentajePrecio = model.PorcentajePrecio > 0 ? model.PorcentajePrecio : 100,
                    NumDecRedo = model.NumDecRedo >= 0 ? model.NumDecRedo : 1,
                    UserId = userId
                });

                return (true, "Artículo agregado al combo exitosamente.", newId);
            }
            else
            {
                const string sql = @"
                    UPDATE combo_producto
                    SET cantidad = @Cantidad,
                        porc_precio = @PorcentajePrecio,
                        num_dec_redo = @NumDecRedo,
                        usuario_modifica_id = @UserId,
                        fecha_modifica = GETDATE()
                    WHERE combo_producto_id = @Id";

                await conn.ExecuteAsync(sql, new {
                    Id = model.Id,
                    Cantidad = model.Cantidad,
                    PorcentajePrecio = model.PorcentajePrecio > 0 ? model.PorcentajePrecio : 100,
                    NumDecRedo = model.NumDecRedo >= 0 ? model.NumDecRedo : 1,
                    UserId = userId
                });

                return (true, "Artículo en combo actualizado exitosamente.", model.Id);
            }
        }

        public async Task<(bool success, string message)> DeleteComboDetalleAsync(int comboProductoId, int userId)
        {
            if (comboProductoId <= 0) return (false, "Identificador inválido.");
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "DELETE FROM combo_producto WHERE combo_producto_id = @Id";
            int rows = await conn.ExecuteAsync(sql, new { Id = comboProductoId });
            return (rows > 0, rows > 0 ? "Artículo retirado del combo exitosamente." : "Registro no encontrado.");
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoComboAsync(int productoId, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var item = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT producto_id, estado FROM producto WHERE producto_id = @Id", new { Id = productoId });
            if (item == null) return (false, "Combo no encontrado.", "I");

            string estadoActual = (string)item.estado;
            string nuevoEstado = estadoActual == "A" ? "I" : "A";
            const string sql = "UPDATE producto SET estado = @NuevoEstado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE producto_id = @Id";
            await conn.ExecuteAsync(sql, new { Id = productoId, NuevoEstado = nuevoEstado, UserId = userId });
            return (true, $"Combo {(nuevoEstado == "A" ? "activado" : "desactivado")} exitosamente.", nuevoEstado);
        }

        public async Task<List<LookupItemModel>> BuscarProductosParaComboAsync(string search)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT TOP 25 
                    p.producto_id AS Id,
                    p.producto AS Codigo,
                    CONCAT(p.producto, ' - ', p.descripcion, ' (', ISNULL(um.abrev, 'UND'), ')') AS Nombre
                FROM producto p
                LEFT JOIN unidad_medida um ON um.unidad_medida_id = p.unidad_medida_id
                WHERE p.estado = 'A'
                  AND (@Search IS NULL OR p.producto LIKE '%' + @Search + '%' OR p.descripcion LIKE '%' + @Search + '%')
                ORDER BY p.descripcion ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql, new { Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim() });
            return rows.ToList();
        }

        public async Task<List<TransportistaModel>> GetTransportistasAsync(string? search = null, string? estado = null, int top = 25, int empresaId = 1)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            int limit = top is 25 or 50 or 100 or 500 ? top : 25;
            const string sql = @"
                SELECT TOP (@Limit) transpo_id AS Id, empresa_id AS EmpresaId, numero_ruc AS NumeroRuc,
                       razon_social AS RazonSocial, num_ins_vehic AS NumeroInscripcionVehicular, estado AS Estado
                FROM transpo
                WHERE empresa_id = @EmpresaId
                  AND (@Search IS NULL OR razon_social LIKE '%' + @Search + '%' OR numero_ruc LIKE '%' + @Search + '%' OR num_ins_vehic LIKE '%' + @Search + '%')
                  AND (@Estado IS NULL OR estado = @Estado)
                ORDER BY razon_social";
            var rows = await conn.QueryAsync<TransportistaModel>(sql, new
            {
                Limit = limit, EmpresaId = empresaId,
                Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                Estado = string.IsNullOrWhiteSpace(estado) ? null : estado.Trim().ToUpperInvariant()
            });
            return rows.ToList();
        }

        public async Task<TransportistaModel?> GetTransportistaByIdAsync(int id, int empresaId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT transpo_id AS Id, empresa_id AS EmpresaId, numero_ruc AS NumeroRuc,
                       razon_social AS RazonSocial, num_ins_vehic AS NumeroInscripcionVehicular, estado AS Estado
                FROM transpo WHERE transpo_id = @Id AND empresa_id = @EmpresaId";
            return await conn.QueryFirstOrDefaultAsync<TransportistaModel>(sql, new { Id = id, EmpresaId = empresaId });
        }

        public async Task<(bool success, string message, int id)> SaveTransportistaAsync(TransportistaModel model, int userId, int empresaId)
        {
            if (string.IsNullOrWhiteSpace(model.RazonSocial) || string.IsNullOrWhiteSpace(model.NumeroInscripcionVehicular))
                return (false, "Razón social y número de inscripción vehicular son obligatorios.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            if (model.Id == 0)
            {
                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR transpo_seq;
                    INSERT INTO transpo (transpo_id, empresa_id, numero_ruc, razon_social, num_ins_vehic, estado, usuario_creacion_id, fecha_creacion)
                    VALUES (@newId, @EmpresaId, @NumeroRuc, @RazonSocial, @Inscripcion, @Estado, @UserId, GETDATE());
                    SELECT @newId;";
                int id = await conn.ExecuteScalarAsync<int>(sql, new
                {
                    EmpresaId = empresaId, NumeroRuc = SafeTruncate(model.NumeroRuc, 35),
                    RazonSocial = SafeTruncate(model.RazonSocial, 150),
                    Inscripcion = SafeTruncate(model.NumeroInscripcionVehicular, 25),
                    Estado = model.Estado == "I" ? "I" : "A", UserId = userId
                });
                return (true, "Transportista registrado.", id);
            }

            const string updateSql = @"
                UPDATE transpo
                SET numero_ruc = @NumeroRuc, razon_social = @RazonSocial, num_ins_vehic = @Inscripcion,
                    estado = @Estado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                WHERE transpo_id = @Id AND empresa_id = @EmpresaId";
            int updated = await conn.ExecuteAsync(updateSql, new
            {
                model.Id, EmpresaId = empresaId,
                NumeroRuc = SafeTruncate(model.NumeroRuc, 35),
                RazonSocial = SafeTruncate(model.RazonSocial, 150),
                Inscripcion = SafeTruncate(model.NumeroInscripcionVehicular, 25),
                Estado = model.Estado == "I" ? "I" : "A", UserId = userId
            });
            return updated == 0 ? (false, "Transportista no encontrado para la empresa activa.", 0) : (true, "Transportista actualizado.", model.Id);
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoTransportistaAsync(int id, int userId, int empresaId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                UPDATE transpo SET estado = CASE WHEN estado = 'A' THEN 'I' ELSE 'A' END,
                    usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                OUTPUT inserted.estado WHERE transpo_id = @Id AND empresa_id = @EmpresaId";
            string? state = await conn.QuerySingleOrDefaultAsync<string>(sql, new { Id = id, UserId = userId, EmpresaId = empresaId });
            return state == null ? (false, "Transportista no encontrado.", "I") : (true, "Estado del transportista actualizado.", state);
        }

        public async Task<List<TransportistaUnidadModel>> GetTransportistaUnidadesAsync(int transportistaId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT transp_unid_id AS Id, transpo_id AS TransportistaId, marca AS Marca, modelo AS Modelo,
                       placa AS Placa, placa_2 AS Placa2, estado AS Estado
                FROM transp_unid WHERE transpo_id = @TransportistaId ORDER BY marca, modelo, placa";
            var rows = await conn.QueryAsync<TransportistaUnidadModel>(sql, new { TransportistaId = transportistaId });
            return rows.ToList();
        }

        public async Task<(bool success, string message, int id)> SaveTransportistaUnidadAsync(TransportistaUnidadModel model, int userId)
        {
            if (model.TransportistaId <= 0 || string.IsNullOrWhiteSpace(model.Marca) || string.IsNullOrWhiteSpace(model.Placa))
                return (false, "Transportista, marca y placa son obligatorios.", 0);
            using var conn = _dbConnectionFactory.CreateConnection();
            if (model.Id == 0)
            {
                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR transp_unid_seq;
                    INSERT INTO transp_unid (transp_unid_id, transpo_id, marca, modelo, placa, placa_2, estado, usuario_creacion_id, fecha_creacion)
                    VALUES (@newId, @TransportistaId, @Marca, @Modelo, @Placa, @Placa2, 'A', @UserId, GETDATE());
                    SELECT @newId;";
                int id = await conn.ExecuteScalarAsync<int>(sql, new
                {
                    model.TransportistaId, Marca = SafeTruncate(model.Marca, 20),
                    Modelo = SafeTruncate(model.Modelo, 20), Placa = SafeTruncate(model.Placa.ToUpperInvariant(), 6),
                    Placa2 = SafeTruncate(model.Placa2.ToUpperInvariant(), 6), UserId = userId
                });
                return (true, "Unidad registrada.", id);
            }

            const string updateSql = @"
                UPDATE transp_unid SET marca = @Marca, modelo = @Modelo, placa = @Placa, placa_2 = @Placa2,
                    usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                WHERE transp_unid_id = @Id AND transpo_id = @TransportistaId AND estado = 'A'";
            int updated = await conn.ExecuteAsync(updateSql, new
            {
                model.Id, model.TransportistaId, Marca = SafeTruncate(model.Marca, 20),
                Modelo = SafeTruncate(model.Modelo, 20), Placa = SafeTruncate(model.Placa.ToUpperInvariant(), 6),
                Placa2 = SafeTruncate(model.Placa2.ToUpperInvariant(), 6), UserId = userId
            });
            return updated == 0 ? (false, "Unidad no encontrada.", 0) : (true, "Unidad actualizada.", model.Id);
        }

        public async Task<(bool success, string message)> DeleteTransportistaUnidadAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            int updated = await conn.ExecuteAsync(
                "UPDATE transp_unid SET estado = 'I', usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE transp_unid_id = @Id AND estado = 'A'",
                new { Id = id, UserId = userId });
            return updated == 0 ? (false, "Unidad no encontrada o ya inactiva.") : (true, "Unidad desactivada.");
        }

        public async Task<List<TransportistaConductorModel>> GetTransportistaConductoresAsync(int transportistaId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT c.transp_condu_id AS Id, c.transpo_id AS TransportistaId,
                       c.apellido_pat AS ApellidoPaterno, c.apellido_mat AS ApellidoMaterno, c.nombres AS Nombres,
                       c.tipo_doc_iden_id AS TipoDocumentoIdentidadId, d.nombre AS TipoDocumentoNombre,
                       c.num_doc_iden AS NumeroDocumentoIdentidad, c.num_licencia AS NumeroLicencia,
                       c.telefono AS Telefono, c.estado AS Estado
                FROM transp_condu c
                INNER JOIN tipo_doc_iden d ON d.tipo_doc_iden_id = c.tipo_doc_iden_id
                WHERE c.transpo_id = @TransportistaId ORDER BY c.apellido_pat, c.apellido_mat, c.nombres";
            var rows = await conn.QueryAsync<TransportistaConductorModel>(sql, new { TransportistaId = transportistaId });
            return rows.ToList();
        }

        public async Task<(bool success, string message, int id)> SaveTransportistaConductorAsync(TransportistaConductorModel model, int userId)
        {
            if (model.TransportistaId <= 0 || model.TipoDocumentoIdentidadId <= 0
                || string.IsNullOrWhiteSpace(model.ApellidoPaterno) || string.IsNullOrWhiteSpace(model.Nombres)
                || string.IsNullOrWhiteSpace(model.NumeroDocumentoIdentidad) || string.IsNullOrWhiteSpace(model.NumeroLicencia))
                return (false, "Complete conductor, documento de identidad y licencia.", 0);
            using var conn = _dbConnectionFactory.CreateConnection();
            if (model.Id == 0)
            {
                const string sql = @"
                    DECLARE @newId INT = NEXT VALUE FOR transp_condu_seq;
                    INSERT INTO transp_condu (transp_condu_id, transpo_id, apellido_pat, apellido_mat, nombres, tipo_doc_iden_id, num_doc_iden, num_licencia, telefono, estado, usuario_creacion_id, fecha_creacion)
                    VALUES (@newId, @TransportistaId, @ApellidoPaterno, @ApellidoMaterno, @Nombres, @TipoDocumentoIdentidadId, @Documento, @Licencia, @Telefono, 'A', @UserId, GETDATE());
                    SELECT @newId;";
                int id = await conn.ExecuteScalarAsync<int>(sql, new
                {
                    model.TransportistaId, ApellidoPaterno = SafeTruncate(model.ApellidoPaterno, 50),
                    ApellidoMaterno = SafeTruncate(model.ApellidoMaterno, 50), Nombres = SafeTruncate(model.Nombres, 50),
                    model.TipoDocumentoIdentidadId, Documento = SafeTruncate(model.NumeroDocumentoIdentidad, 35),
                    Licencia = SafeTruncate(model.NumeroLicencia, 20), Telefono = SafeTruncate(model.Telefono, 15), UserId = userId
                });
                return (true, "Conductor registrado.", id);
            }

            const string updateSql = @"
                UPDATE transp_condu
                SET apellido_pat = @ApellidoPaterno, apellido_mat = @ApellidoMaterno, nombres = @Nombres,
                    tipo_doc_iden_id = @TipoDocumentoIdentidadId, num_doc_iden = @Documento,
                    num_licencia = @Licencia, telefono = @Telefono,
                    usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                WHERE transp_condu_id = @Id AND transpo_id = @TransportistaId AND estado = 'A'";
            int updated = await conn.ExecuteAsync(updateSql, new
            {
                model.Id, model.TransportistaId, ApellidoPaterno = SafeTruncate(model.ApellidoPaterno, 50),
                ApellidoMaterno = SafeTruncate(model.ApellidoMaterno, 50), Nombres = SafeTruncate(model.Nombres, 50),
                model.TipoDocumentoIdentidadId, Documento = SafeTruncate(model.NumeroDocumentoIdentidad, 35),
                Licencia = SafeTruncate(model.NumeroLicencia, 20), Telefono = SafeTruncate(model.Telefono, 15), UserId = userId
            });
            return updated == 0 ? (false, "Conductor no encontrado.", 0) : (true, "Conductor actualizado.", model.Id);
        }

        public async Task<(bool success, string message)> DeleteTransportistaConductorAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            int updated = await conn.ExecuteAsync(
                "UPDATE transp_condu SET estado = 'I', usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE transp_condu_id = @Id AND estado = 'A'",
                new { Id = id, UserId = userId });
            return updated == 0 ? (false, "Conductor no encontrado o ya inactivo.") : (true, "Conductor desactivado.");
        }

        public async Task<List<TipoDocIdentidadLookupModel>> GetDocumentosTransportistaLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_doc_iden_id AS Id, tipo_doc_iden AS Codigo, nombre AS Nombre, abrev AS Abreviatura, formato_valida AS FormatoValidacion FROM tipo_doc_iden WHERE estado = 'A' ORDER BY nombre";
            var rows = await conn.QueryAsync<TipoDocIdentidadLookupModel>(sql);
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

        private static string SafeTruncate(string? val, int maxLen)
        {
            if (string.IsNullOrWhiteSpace(val)) return "";
            var trimmed = val.Trim();
            return trimmed.Length <= maxLen ? trimmed : trimmed.Substring(0, maxLen);
        }
    }
}
