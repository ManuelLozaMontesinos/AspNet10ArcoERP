using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services
{
    public class CalendarioService : ICalendarioService
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public CalendarioService(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<List<CalendarioAreaModel>> GetAreasUsuarioAsync(int usuarioId, bool esAdmin)
        {
            var areas = new List<CalendarioAreaModel>();
            if (usuarioId <= 0) return areas;

            using var conn = _dbConnectionFactory.CreateConnection();

            try
            {
                if (esAdmin)
                {
                    var rows = await conn.QueryAsync<CalendarioAreaModel>(@"
                        SELECT area_id AS AreaId, nombre AS Nombre, ISNULL(descripcion, '') AS Descripcion,
                               ISNULL(color, '#0d6efd') AS Color, orden AS Orden, estado AS Estado
                        FROM dbo.calendario_area
                        WHERE estado = 'A'
                        ORDER BY orden ASC, nombre ASC");
                    return rows.ToList();
                }

                // Autoprovisión según roles para usuarios estándar
                await conn.ExecuteAsync(@"
                    INSERT INTO dbo.calendario_usuario_area (usuario_id, area_id, es_principal, fecha_creacion)
                    SELECT DISTINCT @usuarioId, m.area_id, 0, SYSDATETIME()
                    FROM dbo.rol_usuario ru
                    JOIN dbo.rol r ON ru.rol_id = r.rol_id
                    CROSS APPLY (
                        SELECT area_id FROM dbo.calendario_area
                        WHERE estado = 'A' AND (
                            (area_id = 2 AND (r.nombre LIKE '%Venta%' OR r.nombre LIKE '%Comercial%' OR r.rol IN ('00003','00004','00010','00017')))
                            OR (area_id = 3 AND (r.nombre LIKE '%Market%' OR r.rol IN ('00015')))
                            OR (area_id = 4 AND (r.nombre LIKE '%Producci%' OR r.rol IN ('00023')))
                            OR (area_id = 5 AND (r.nombre LIKE '%Almac%' OR r.nombre LIKE '%Log%stica%' OR r.nombre LIKE '%Despacho%' OR r.nombre LIKE '%Operacion%' OR r.rol IN ('00007','00011','00013','00014','00016','00018','00019','00021','00022')))
                            OR (area_id = 6 AND (r.nombre LIKE '%Contab%' OR r.nombre LIKE '%Tesor%' OR r.nombre LIKE '%Caja%' OR r.rol IN ('00005','00006','00008','00012')))
                            OR (area_id = 7 AND (r.nombre LIKE '%Capital Humano%' OR r.nombre LIKE '%RRHH%' OR r.rol IN ('00009')))
                            OR (area_id = 8 AND (r.rol = '00020' OR r.nombre = 'TI' OR r.nombre LIKE '%TI %' OR r.nombre LIKE '% TI' OR r.nombre LIKE '%Sistemas%'))
                            OR (area_id = 1 AND (r.rol = 'S0001' OR r.nombre LIKE '%Admin%'))
                        )
                    ) m
                    WHERE ru.usuario_id = @usuarioId
                      AND NOT EXISTS (
                          SELECT 1 FROM dbo.calendario_usuario_area cua 
                          WHERE cua.usuario_id = @usuarioId AND cua.area_id = m.area_id
                      );

                    IF EXISTS (SELECT 1 FROM dbo.calendario_usuario_area WHERE usuario_id = @usuarioId)
                       AND NOT EXISTS (SELECT 1 FROM dbo.calendario_usuario_area WHERE usuario_id = @usuarioId AND es_principal = 1)
                    BEGIN
                        UPDATE TOP (1) dbo.calendario_usuario_area 
                        SET es_principal = 1 
                        WHERE usuario_id = @usuarioId;
                    END",
                    new { usuarioId });

                var userAreas = await conn.QueryAsync<CalendarioAreaModel>(@"
                    SELECT a.area_id AS AreaId, a.nombre AS Nombre, ISNULL(a.descripcion, '') AS Descripcion,
                           ISNULL(a.color, '#0d6efd') AS Color, a.orden AS Orden, a.estado AS Estado
                    FROM dbo.calendario_area a
                    INNER JOIN dbo.calendario_usuario_area cua ON a.area_id = cua.area_id
                    WHERE cua.usuario_id = @usuarioId AND a.estado = 'A'
                    ORDER BY cua.es_principal DESC, a.orden ASC, a.nombre ASC",
                    new { usuarioId });

                var result = userAreas.ToList();

                // Si no tiene áreas asignadas tras el intento, asignarle el área de Ventas (2) o la primera disponible
                if (result.Count == 0)
                {
                    await conn.ExecuteAsync(@"
                        IF NOT EXISTS (SELECT 1 FROM dbo.calendario_usuario_area WHERE usuario_id = @uid)
                        BEGIN
                            DECLARE @aid INT = (SELECT TOP 1 area_id FROM dbo.calendario_area WHERE estado = 'A' ORDER BY area_id);
                            IF @aid IS NOT NULL
                            BEGIN
                                INSERT INTO dbo.calendario_usuario_area (usuario_id, area_id, es_principal, fecha_creacion)
                                VALUES (@uid, @aid, 1, SYSDATETIME());
                            END
                        END",
                        new { uid = usuarioId });

                    var fallback = await conn.QueryAsync<CalendarioAreaModel>(@"
                        SELECT a.area_id AS AreaId, a.nombre AS Nombre, ISNULL(a.descripcion, '') AS Descripcion,
                               ISNULL(a.color, '#0d6efd') AS Color, a.orden AS Orden, a.estado AS Estado
                        FROM dbo.calendario_area a
                        INNER JOIN dbo.calendario_usuario_area cua ON a.area_id = cua.area_id
                        WHERE cua.usuario_id = @usuarioId AND a.estado = 'A'
                        ORDER BY a.orden ASC",
                        new { usuarioId });
                    result = fallback.ToList();
                }

                return result;
            }
            catch
            {
                return new List<CalendarioAreaModel>();
            }
        }

        public async Task<List<object>> GetEventosAreaAsync(int areaId, int usuarioId, bool esAdmin)
        {
            var eventos = new List<object>();
            if (usuarioId <= 0) return eventos;

            var areasPermitidas = await GetAreasUsuarioAsync(usuarioId, esAdmin);
            int idAreaFinal;
            if (esAdmin)
            {
                idAreaFinal = areaId > 0 ? areaId : (areasPermitidas.FirstOrDefault()?.AreaId ?? 1);
            }
            else
            {
                if (areasPermitidas.Count == 0) return eventos;
                idAreaFinal = (areaId > 0 && areasPermitidas.Any(a => a.AreaId == areaId))
                    ? areaId
                    : areasPermitidas[0].AreaId;
            }

            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var rows = await conn.QueryAsync<dynamic>(@"
                    SELECT e.evento_id, e.area_id, e.usuario_id, e.usuario_logon, e.titulo, e.contexto,
                           e.fecha_inicio, e.fecha_fin, e.todo_el_dia, e.color, e.fecha_creacion,
                           ISNULL(a.nombre, 'General') AS area_nombre
                    FROM dbo.calendario_evento e
                    LEFT JOIN dbo.calendario_area a ON e.area_id = a.area_id
                    WHERE e.estado = 'A' AND e.area_id = @areaId
                    ORDER BY e.fecha_inicio ASC",
                    new { areaId = idAreaFinal });

                foreach (var r in rows)
                {
                    int id = Convert.ToInt32(r.evento_id);
                    int propietarioId = Convert.ToInt32(r.usuario_id);
                    string titulo = (string)r.titulo ?? "";
                    string contexto = (string)r.contexto ?? "";
                    DateTime fInicio = (DateTime)r.fecha_inicio;
                    DateTime? fFin = r.fecha_fin != null ? (DateTime?)r.fecha_fin : null;
                    bool todoElDia = Convert.ToBoolean(r.todo_el_dia);
                    string color = (string)r.color ?? "#0d6efd";
                    string usuario = (string)r.usuario_logon ?? "";
                    DateTime fCreacion = (DateTime)r.fecha_creacion;
                    string areaNombre = (string)r.area_nombre ?? "General";
                    bool puedeEditar = esAdmin || areasPermitidas.Any(a => a.AreaId == idAreaFinal);

                    eventos.Add(new
                    {
                        id = id.ToString(),
                        title = titulo,
                        start = todoElDia ? fInicio.ToString("yyyy-MM-dd") : fInicio.ToString("yyyy-MM-ddTHH:mm:ss"),
                        end = fFin.HasValue ? (todoElDia ? fFin.Value.ToString("yyyy-MM-dd") : fFin.Value.ToString("yyyy-MM-ddTHH:mm:ss")) : null,
                        allDay = todoElDia,
                        editable = puedeEditar,
                        backgroundColor = color,
                        borderColor = color,
                        textColor = (color == "#ffc107") ? "#212529" : "#ffffff",
                        extendedProps = new
                        {
                            contexto = contexto,
                            usuario = usuario,
                            usuarioId = propietarioId,
                            areaId = idAreaFinal,
                            areaNombre = areaNombre,
                            puedeEditar = puedeEditar,
                            fechaCreacion = fCreacion.ToString("dd/MM/yyyy HH:mm")
                        }
                    });
                }
            }
            catch { }

            return eventos;
        }

        public async Task<(bool Success, int EventoId, string Message)> GuardarEventoAsync(EventoInputModel model, int usuarioId, string usuarioLogon, bool esAdmin)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Titulo) || string.IsNullOrWhiteSpace(model.FechaInicio))
            {
                return (false, 0, "El título y la fecha son obligatorios.");
            }

            if (model.Titulo.Trim().Length > 150)
            {
                return (false, 0, "El título no puede superar los 150 caracteres.");
            }

            string color = string.IsNullOrWhiteSpace(model.Color) ? "#0d6efd" : model.Color.Trim();

            var areasPermitidas = await GetAreasUsuarioAsync(usuarioId, esAdmin);
            int areaIdFinal;
            if (esAdmin)
            {
                areaIdFinal = (model.AreaId.HasValue && model.AreaId.Value > 0) ? model.AreaId.Value : 1;
            }
            else
            {
                if (areasPermitidas.Count == 0)
                {
                    return (false, 0, "No tienes permiso para registrar eventos en este momento.");
                }
                areaIdFinal = (model.AreaId.HasValue && model.AreaId.Value > 0 && areasPermitidas.Any(a => a.AreaId == model.AreaId.Value))
                    ? model.AreaId.Value
                    : areasPermitidas[0].AreaId;
            }

            DateTime fInicio;
            string fInicioStr = (model.FechaInicio ?? "").Trim();
            if (!model.TodoElDia && fInicioStr.Length == 10 && fInicioStr.IndexOf('T') < 0)
            {
                fInicioStr += "T09:00:00";
            }

            if (!DateTime.TryParse(fInicioStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out fInicio) &&
                !DateTime.TryParse(fInicioStr, CultureInfo.CurrentCulture, DateTimeStyles.None, out fInicio))
            {
                return (false, 0, "Selecciona una fecha y hora de inicio válidas.");
            }

            DateTime? fFin = null;
            if (!string.IsNullOrWhiteSpace(model.FechaFin))
            {
                string fFinStr = model.FechaFin.Trim();
                if (!model.TodoElDia && fFinStr.Length == 10 && fFinStr.IndexOf('T') < 0)
                {
                    fFinStr += "T10:00:00";
                }

                if (!DateTime.TryParse(fFinStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedFin) &&
                    !DateTime.TryParse(fFinStr, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsedFin))
                {
                    return (false, 0, "Formato de fecha y hora de fin inválido.");
                }

                fFin = parsedFin;
                if (fFin <= fInicio)
                {
                    return (false, 0, "La fecha y hora de fin debe ser posterior al inicio.");
                }
            }

            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                int eventoIdGuardado = 0;

                if (model.Id.HasValue && model.Id.Value > 0)
                {
                    int existingAreaId = await conn.ExecuteScalarAsync<int>(
                        "SELECT area_id FROM dbo.calendario_evento WHERE evento_id = @id AND estado = 'A'",
                        new { id = model.Id.Value });

                    if (!esAdmin && !areasPermitidas.Any(a => a.AreaId == existingAreaId))
                    {
                        return (false, 0, "No tienes permiso para modificar eventos de esta área.");
                    }

                    int updated = await conn.ExecuteAsync(@"
                        UPDATE dbo.calendario_evento
                        SET titulo = @Titulo,
                            contexto = @Contexto,
                            fecha_inicio = @FInicio,
                            fecha_fin = @FFin,
                            todo_el_dia = @TodoElDia,
                            color = @Color,
                            area_id = COALESCE(@AreaId, area_id),
                            recordatorio_estado = CASE WHEN fecha_inicio <> @FInicio OR todo_el_dia <> @TodoElDia OR area_id <> @AreaId THEN 'P' ELSE recordatorio_estado END,
                            recordatorio_reclamado_hasta = CASE WHEN fecha_inicio <> @FInicio OR todo_el_dia <> @TodoElDia OR area_id <> @AreaId THEN NULL ELSE recordatorio_reclamado_hasta END,
                            recordatorio_enviado_en = CASE WHEN fecha_inicio <> @FInicio OR todo_el_dia <> @TodoElDia OR area_id <> @AreaId THEN NULL ELSE recordatorio_enviado_en END,
                            fecha_modifica = GETDATE()
                        WHERE evento_id = @Id AND estado = 'A'",
                        new
                        {
                            Id = model.Id.Value,
                            Titulo = model.Titulo.Trim(),
                            Contexto = string.IsNullOrWhiteSpace(model.Contexto) ? null : model.Contexto.Trim(),
                            FInicio = fInicio,
                            FFin = fFin,
                            TodoElDia = model.TodoElDia ? 1 : 0,
                            Color = color,
                            AreaId = areaIdFinal
                        });

                    if (updated == 0)
                    {
                        return (false, 0, "No se pudo actualizar el evento.");
                    }
                    eventoIdGuardado = model.Id.Value;
                }
                else
                {
                    eventoIdGuardado = await conn.ExecuteScalarAsync<int>(@"
                        INSERT INTO dbo.calendario_evento (area_id, usuario_id, usuario_logon, titulo, contexto, fecha_inicio, fecha_fin, todo_el_dia, color, estado, recordatorio_estado, fecha_creacion)
                        OUTPUT INSERTED.evento_id
                        VALUES (@AreaId, @UsuarioId, @UsuarioLogon, @Titulo, @Contexto, @FInicio, @FFin, @TodoElDia, @Color, 'A', 'P', GETDATE())",
                        new
                        {
                            AreaId = areaIdFinal,
                            UsuarioId = usuarioId,
                            UsuarioLogon = usuarioLogon,
                            Titulo = model.Titulo.Trim(),
                            Contexto = string.IsNullOrWhiteSpace(model.Contexto) ? null : model.Contexto.Trim(),
                            FInicio = fInicio,
                            FFin = fFin,
                            TodoElDia = model.TodoElDia ? 1 : 0,
                            Color = color
                        });
                }

                return (true, eventoIdGuardado, "Evento guardado exitosamente.");
            }
            catch (Exception ex)
            {
                return (false, 0, "Error al guardar evento: " + ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> MoverOActualizarEventoAsync(MoverEventoInputModel model, int usuarioId, bool esAdmin)
        {
            if (model.Id <= 0 || string.IsNullOrWhiteSpace(model.FechaInicio))
            {
                return (false, "Datos del evento incompletos.");
            }

            DateTime fInicio;
            if (!DateTime.TryParse(model.FechaInicio, CultureInfo.InvariantCulture, DateTimeStyles.None, out fInicio) &&
                !DateTime.TryParse(model.FechaInicio, CultureInfo.CurrentCulture, DateTimeStyles.None, out fInicio))
            {
                return (false, "Fecha de inicio inválida.");
            }

            DateTime? fFin = null;
            if (!string.IsNullOrWhiteSpace(model.FechaFin))
            {
                if (DateTime.TryParse(model.FechaFin, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime pFin) ||
                    DateTime.TryParse(model.FechaFin, CultureInfo.CurrentCulture, DateTimeStyles.None, out pFin))
                {
                    fFin = pFin;
                }
            }

            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                int rows = await conn.ExecuteAsync(@"
                    UPDATE dbo.calendario_evento
                    SET fecha_inicio = @FInicio,
                        fecha_fin = @FFin,
                        todo_el_dia = @TodoElDia,
                        recordatorio_estado = 'P',
                        recordatorio_reclamado_hasta = NULL,
                        recordatorio_enviado_en = NULL,
                        fecha_modifica = GETDATE()
                    WHERE evento_id = @Id AND estado = 'A'",
                    new { Id = model.Id, FInicio = fInicio, FFin = fFin, TodoElDia = model.TodoElDia ? 1 : 0 });

                return rows > 0 ? (true, "Evento reprogramado exitosamente.") : (false, "No se encontró el evento.");
            }
            catch (Exception ex)
            {
                return (false, "Error al mover evento: " + ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> EliminarEventoAsync(int id, int usuarioId, bool esAdmin)
        {
            if (id <= 0) return (false, "ID de evento no válido.");

            try
            {
                var areasPermitidas = await GetAreasUsuarioAsync(usuarioId, esAdmin);
                using var conn = _dbConnectionFactory.CreateConnection();

                int existingAreaId = await conn.ExecuteScalarAsync<int>(
                    "SELECT area_id FROM dbo.calendario_evento WHERE evento_id = @id AND estado = 'A'",
                    new { id });

                if (!esAdmin && !areasPermitidas.Any(a => a.AreaId == existingAreaId))
                {
                    return (false, "No tienes permiso para eliminar eventos de esta área.");
                }

                int rows = await conn.ExecuteAsync(@"
                    UPDATE dbo.calendario_evento
                    SET estado = 'I',
                        fecha_modifica = GETDATE()
                    WHERE evento_id = @id AND estado = 'A'",
                    new { id });

                return rows > 0 ? (true, "Evento eliminado exitosamente.") : (false, "El evento ya no existe.");
            }
            catch (Exception ex)
            {
                return (false, "Error al eliminar evento: " + ex.Message);
            }
        }

        public async Task<List<CalendarioEtiquetaModel>> GetEtiquetasAreaAsync(int areaId, int usuarioId, bool esAdmin)
        {
            var list = new List<CalendarioEtiquetaModel>();
            if (usuarioId <= 0) return list;

            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var rows = await conn.QueryAsync<CalendarioEtiquetaModel>(@"
                    SELECT slot_index AS SlotIndex, ISNULL(titulo, '') AS Titulo, ISNULL(color, '#0d6efd') AS Color
                    FROM dbo.calendario_etiqueta
                    WHERE area_id = @areaId
                    ORDER BY slot_index ASC",
                    new { areaId });

                return rows.ToList();
            }
            catch
            {
                return list;
            }
        }

        public async Task<(bool Success, string Message)> GuardarEtiquetaAreaAsync(EtiquetaInputModel model, int usuarioId, bool esAdmin)
        {
            if (model == null || model.SlotIndex < 0 || model.SlotIndex > 10)
            {
                return (false, "Datos inválidos para la etiqueta.");
            }

            int areaIdFinal = model.AreaId ?? 1;
            string color = string.IsNullOrWhiteSpace(model.Color) ? "#0d6efd" : model.Color.Trim();
            string titulo = string.IsNullOrWhiteSpace(model.Titulo) ? "" : model.Titulo.Trim();

            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                await conn.ExecuteAsync(@"
                    MERGE dbo.calendario_etiqueta AS target
                    USING (SELECT @AreaId AS area_id, @SlotIndex AS slot_index) AS source
                    ON (target.area_id = source.area_id AND target.slot_index = source.slot_index)
                    WHEN MATCHED THEN
                        UPDATE SET titulo = @Titulo, color = @Color, usuario_id = @UsuarioId, fecha_modifica = GETDATE()
                    WHEN NOT MATCHED THEN
                        INSERT (usuario_id, area_id, slot_index, titulo, color, fecha_modifica)
                        VALUES (@UsuarioId, @AreaId, @SlotIndex, @Titulo, @Color, GETDATE());",
                    new
                    {
                        UsuarioId = usuarioId,
                        AreaId = areaIdFinal,
                        SlotIndex = model.SlotIndex,
                        Titulo = titulo,
                        Color = color
                    });

                return (true, "Etiqueta arrastrable guardada exitosamente.");
            }
            catch (Exception ex)
            {
                return (false, "Error al guardar etiqueta: " + ex.Message);
            }
        }

        public async Task<List<CalendarioAreaModel>> AdminGetAreasAsync()
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var rows = await conn.QueryAsync<CalendarioAreaModel>(@"
                    SELECT a.area_id AS AreaId, a.nombre AS Nombre, ISNULL(a.descripcion, '') AS Descripcion,
                           ISNULL(a.color, '#0d6efd') AS Color, a.orden AS Orden, a.estado AS Estado,
                           CONVERT(VARCHAR(10), a.fecha_creacion, 103) AS FechaCreacion,
                           COUNT(DISTINCT cua.usuario_id) AS TotalUsuarios,
                           COUNT(DISTINCT e.evento_id) AS TotalEventos
                    FROM dbo.calendario_area a
                    LEFT JOIN dbo.calendario_usuario_area cua ON a.area_id = cua.area_id
                    LEFT JOIN dbo.calendario_evento e ON a.area_id = e.area_id AND e.estado = 'A'
                    GROUP BY a.area_id, a.nombre, a.descripcion, a.color, a.orden, a.estado, a.fecha_creacion
                    ORDER BY a.orden ASC, a.nombre ASC");

                return rows.ToList();
            }
            catch
            {
                return new List<CalendarioAreaModel>();
            }
        }

        public async Task<(bool Success, string Message, int AreaId)> AdminGuardarAreaAsync(AreaAdminInputModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Nombre))
            {
                return (false, "El nombre del área es obligatorio.", 0);
            }

            string nombre = model.Nombre.Trim();
            string descripcion = model.Descripcion?.Trim() ?? string.Empty;
            string color = !string.IsNullOrWhiteSpace(model.Color) ? model.Color : "#0d6efd";
            int orden = model.Orden > 0 ? model.Orden : 1;
            string estado = string.Equals(model.Estado, "I", StringComparison.OrdinalIgnoreCase) ? "I" : "A";

            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                if (model.AreaId <= 0)
                {
                    int newId = await conn.ExecuteScalarAsync<int>(@"
                        IF @orden = 1 AND EXISTS (SELECT 1 FROM dbo.calendario_area)
                        BEGIN
                           SELECT @orden = ISNULL(MAX(orden), 0) + 1 FROM dbo.calendario_area;
                        END

                        INSERT INTO dbo.calendario_area (nombre, descripcion, color, orden, estado, fecha_creacion)
                        OUTPUT INSERTED.area_id
                        VALUES (@nombre, @descripcion, @color, @orden, 'A', SYSDATETIME());",
                        new { nombre, descripcion, color, orden });

                    return (true, "Área creada exitosamente.", newId);
                }
                else
                {
                    await conn.ExecuteAsync(@"
                        UPDATE dbo.calendario_area
                        SET nombre = @nombre,
                            descripcion = @descripcion,
                            color = @color,
                            orden = @orden,
                            estado = @estado
                        WHERE area_id = @areaId",
                        new { areaId = model.AreaId, nombre, descripcion, color, orden, estado });

                    return (true, "Área actualizada exitosamente.", model.AreaId);
                }
            }
            catch (Exception ex)
            {
                return (false, "Error al guardar área: " + ex.Message, 0);
            }
        }

        public async Task<(bool Success, string Message)> AdminCambiarEstadoAreaAsync(int areaId, string estado)
        {
            if (areaId <= 0) return (false, "Identificador de área inválido.");
            string nuevoEstado = string.Equals(estado, "A", StringComparison.OrdinalIgnoreCase) ? "A" : "I";

            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                await conn.ExecuteAsync("UPDATE dbo.calendario_area SET estado = @estado WHERE area_id = @areaId",
                    new { estado = nuevoEstado, areaId });

                return (true, nuevoEstado == "A" ? "Área activada correctamente." : "Área desactivada correctamente.");
            }
            catch (Exception ex)
            {
                return (false, "Error al cambiar estado del área: " + ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> AdminEliminarAreaAsync(int areaId)
        {
            if (areaId <= 0) return (false, "Identificador de área inválido.");

            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                int eventosCount = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM dbo.calendario_evento WHERE area_id = @areaId AND estado = 'A'",
                    new { areaId });

                if (eventosCount > 0)
                {
                    return (false, $"No se puede eliminar el área porque contiene {eventosCount} evento(s) activo(s). Puedes desactivarla para ocultarla.");
                }

                await conn.ExecuteAsync(@"
                    DELETE FROM dbo.calendario_evento WHERE area_id = @areaId;
                    DELETE FROM dbo.calendario_etiqueta WHERE area_id = @areaId;
                    DELETE FROM dbo.calendario_usuario_area WHERE area_id = @areaId;
                    DELETE FROM dbo.calendario_area WHERE area_id = @areaId;",
                    new { areaId });

                return (true, "Área eliminada exitosamente.");
            }
            catch (Exception ex)
            {
                return (false, "Error al eliminar área: " + ex.Message);
            }
        }

        public async Task<(string AreaNombre, string AreaColor, List<UsuarioAreaModel> Asignados, List<UsuarioAreaModel> Disponibles)> AdminGetUsuariosAreaAsync(int areaId)
        {
            var asignados = new List<UsuarioAreaModel>();
            var disponibles = new List<UsuarioAreaModel>();
            string areaNombre = "";
            string areaColor = "#0d6efd";

            if (areaId <= 0) return (areaNombre, areaColor, asignados, disponibles);

            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var areaInfo = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT nombre, ISNULL(color, '#0d6efd') AS color FROM dbo.calendario_area WHERE area_id = @areaId",
                    new { areaId });

                if (areaInfo != null)
                {
                    areaNombre = (string)areaInfo.nombre ?? "";
                    areaColor = (string)areaInfo.color ?? "#0d6efd";
                }

                var rowsAsignados = await conn.QueryAsync<dynamic>(@"
                    SELECT u.usuario_id, u.usuario_logon, u.nombre_completo, cua.es_principal,
                           CONVERT(VARCHAR(10), cua.fecha_creacion, 103) AS fecha_creacion,
                           STRING_AGG(r.nombre, ', ') AS roles
                    FROM dbo.calendario_usuario_area cua
                    JOIN dbo.usuario u ON cua.usuario_id = u.usuario_id
                    LEFT JOIN dbo.rol_usuario ru ON u.usuario_id = ru.usuario_id
                    LEFT JOIN dbo.rol r ON ru.rol_id = r.rol_id
                    WHERE cua.area_id = @areaId
                    GROUP BY u.usuario_id, u.usuario_logon, u.nombre_completo, cua.es_principal, cua.fecha_creacion
                    ORDER BY cua.es_principal DESC, u.nombre_completo ASC",
                    new { areaId });

                foreach (var r in rowsAsignados)
                {
                    asignados.Add(new UsuarioAreaModel
                    {
                        UsuarioId = Convert.ToInt32(r.usuario_id),
                        UsuarioLogon = (string)r.usuario_logon ?? "",
                        NombreCompleto = (string)r.nombre_completo ?? "",
                        EsPrincipal = Convert.ToBoolean(r.es_principal),
                        Roles = (string)r.roles ?? "Sin rol",
                        FechaCreacion = (string)r.fecha_creacion ?? ""
                    });
                }

                var rowsDisponibles = await conn.QueryAsync<dynamic>(@"
                    SELECT u.usuario_id, u.usuario_logon, u.nombre_completo,
                           STRING_AGG(r.nombre, ', ') AS roles
                    FROM dbo.usuario u
                    LEFT JOIN dbo.rol_usuario ru ON u.usuario_id = ru.usuario_id
                    LEFT JOIN dbo.rol r ON ru.rol_id = r.rol_id
                    WHERE u.estado = 'A'
                      AND u.usuario_id NOT IN (SELECT usuario_id FROM dbo.calendario_usuario_area WHERE area_id = @areaId)
                    GROUP BY u.usuario_id, u.usuario_logon, u.nombre_completo
                    ORDER BY u.nombre_completo ASC",
                    new { areaId });

                foreach (var r in rowsDisponibles)
                {
                    disponibles.Add(new UsuarioAreaModel
                    {
                        UsuarioId = Convert.ToInt32(r.usuario_id),
                        UsuarioLogon = (string)r.usuario_logon ?? "",
                        NombreCompleto = (string)r.nombre_completo ?? "",
                        Roles = (string)r.roles ?? "Sin rol"
                    });
                }
            }
            catch { }

            return (areaNombre, areaColor, asignados, disponibles);
        }

        public async Task<(bool Success, string Message)> AdminAsignarUsuarioAreaAsync(int usuarioId, int areaId, bool esPrincipal)
        {
            if (usuarioId <= 0 || areaId <= 0) return (false, "Usuario y área son requeridos.");

            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                await conn.ExecuteAsync(@"
                    IF NOT EXISTS (SELECT 1 FROM dbo.calendario_usuario_area WHERE usuario_id = @uid AND area_id = @aid)
                    BEGIN
                        IF @esPrincipal = 1
                        BEGIN
                            UPDATE dbo.calendario_usuario_area SET es_principal = 0 WHERE usuario_id = @uid;
                        END
                        ELSE IF NOT EXISTS (SELECT 1 FROM dbo.calendario_usuario_area WHERE usuario_id = @uid AND es_principal = 1)
                        BEGIN
                            SET @esPrincipal = 1;
                        END

                        INSERT INTO dbo.calendario_usuario_area (usuario_id, area_id, es_principal, fecha_creacion)
                        VALUES (@uid, @aid, @esPrincipal, SYSDATETIME());
                    END
                    ELSE
                    BEGIN
                        IF @esPrincipal = 1
                        BEGIN
                            UPDATE dbo.calendario_usuario_area SET es_principal = 0 WHERE usuario_id = @uid;
                            UPDATE dbo.calendario_usuario_area SET es_principal = 1 WHERE usuario_id = @uid AND area_id = @aid;
                        END
                    END",
                    new { uid = usuarioId, aid = areaId, esPrincipal = esPrincipal ? 1 : 0 });

                return (true, "Usuario asignado exitosamente al área.");
            }
            catch (Exception ex)
            {
                return (false, "Error al asignar usuario al área: " + ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> AdminRemoverUsuarioAreaAsync(int usuarioId, int areaId)
        {
            if (usuarioId <= 0 || areaId <= 0) return (false, "Usuario y área son requeridos.");

            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                await conn.ExecuteAsync(@"
                    DELETE FROM dbo.calendario_usuario_area 
                    WHERE usuario_id = @uid AND area_id = @aid;

                    IF NOT EXISTS (SELECT 1 FROM dbo.calendario_usuario_area WHERE usuario_id = @uid AND es_principal = 1)
                    BEGIN
                        UPDATE TOP (1) dbo.calendario_usuario_area SET es_principal = 1 WHERE usuario_id = @uid;
                    END",
                    new { uid = usuarioId, aid = areaId });

                return (true, "Usuario removido del área correctamente.");
            }
            catch (Exception ex)
            {
                return (false, "Error al remover usuario del área: " + ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> AdminToggleUsuarioPrincipalAsync(int usuarioId, int areaId)
        {
            if (usuarioId <= 0 || areaId <= 0) return (false, "Usuario y área son requeridos.");

            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                await conn.ExecuteAsync(@"
                    UPDATE dbo.calendario_usuario_area SET es_principal = 0 WHERE usuario_id = @uid;
                    UPDATE dbo.calendario_usuario_area SET es_principal = 1 WHERE usuario_id = @uid AND area_id = @aid;",
                    new { uid = usuarioId, aid = areaId });

                return (true, "Área establecida como principal para el usuario.");
            }
            catch (Exception ex)
            {
                return (false, "Error al actualizar área principal: " + ex.Message);
            }
        }
    }
}

