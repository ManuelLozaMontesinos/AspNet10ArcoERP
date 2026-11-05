using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AspNet10ArcoERP.Hubs;
using AspNet10ArcoERP.Models;
using Dapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace AspNet10ArcoERP.Services
{
    public class NotifyService : INotifyService
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IHubContext<NotifyHub> _hubContext;
        private readonly ILogger<NotifyService> _logger;

        public NotifyService(
            IDbConnectionFactory dbConnectionFactory,
            IHubContext<NotifyHub> hubContext,
            ILogger<NotifyService> logger)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task SendNotificationAsync(int usuarioId, string titulo, string descripcion)
        {
            if (usuarioId <= 0) return;

            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                await conn.ExecuteAsync(@"
                    INSERT INTO dbo.notifi_push (fecha, fecha_reg, titulo, descripcion, lectura, usuario_id)
                    VALUES (CAST(GETDATE() AS date), GETDATE(), @Titulo, @Descripcion, 'N', @UsuarioId)",
                    new { Titulo = titulo, Descripcion = descripcion, UsuarioId = usuarioId });

                await _hubContext.Clients.User(usuarioId.ToString()).SendAsync("refreshNotificationList");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar notificación push para usuario {UsuarioId}", usuarioId);
            }
        }

        public async Task SendCalendarReminderToUsersAsync(IEnumerable<int> usuarioIds, string tituloEvento, DateTime fechaInicio, string? areaNombre = null)
        {
            if (usuarioIds == null) return;
            var ids = usuarioIds.Distinct().Where(id => id > 0).ToList();
            if (ids.Count == 0) return;

            string tituloNotificacion = string.IsNullOrWhiteSpace(areaNombre)
                ? ("Próximo evento: " + tituloEvento)
                : ($"[{areaNombre}] " + tituloEvento);

            if (tituloNotificacion.Length > 100)
            {
                tituloNotificacion = tituloNotificacion.Substring(0, 100);
            }

            string descripcionNotificacion = string.IsNullOrWhiteSpace(areaNombre)
                ? ("El evento inicia el " + fechaInicio.ToString("dd/MM/yyyy HH:mm") + ".")
                : ($"Evento de {areaNombre} inicia el " + fechaInicio.ToString("dd/MM/yyyy HH:mm") + ".");

            string alertMessage = string.IsNullOrWhiteSpace(areaNombre)
                ? "Tienes un evento próximo que inicia en 15 minutos o menos. Revisa tus recordatorios."
                : $"Tienes un evento próximo del área de {areaNombre} que inicia en 15 minutos o menos.";

            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                foreach (int uid in ids)
                {
                    try
                    {
                        await conn.ExecuteAsync(@"
                            INSERT INTO dbo.notifi_push (fecha, fecha_reg, titulo, descripcion, lectura, usuario_id)
                            VALUES (CAST(GETDATE() AS date), GETDATE(), @Titulo, @Descripcion, 'N', @UsuarioId)",
                            new { Titulo = tituloNotificacion, Descripcion = descripcionNotificacion, UsuarioId = uid });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al insertar notificación push en BD para usuario {UserId}", uid);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error de conexión al registrar recordatorio para usuarios {Ids}", string.Join(",", ids));
            }

            foreach (int uid in ids)
            {
                try
                {
                    await _hubContext.Clients.User(uid.ToString()).SendAsync("calendarReminder", alertMessage);
                    await _hubContext.Clients.User(uid.ToString()).SendAsync("refreshNotificationList");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo emitir mensaje SignalR al usuario {UserId}", uid);
                }
            }
        }

        public async Task<List<NotificacionDto>> GetNotificacionesDeHoyAsync(int usuarioId)
        {
            if (usuarioId <= 0) return new List<NotificacionDto>();

            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var rows = await conn.QueryAsync<NotificacionDto>(@"
                    SELECT notifi_push_id AS Id,
                           titulo AS Titulo,
                           descripcion AS Descripcion,
                           fecha AS Fecha,
                           fecha_reg AS FechaRegistro,
                           lectura AS Lectura,
                           usuario_id AS UsuarioId
                    FROM dbo.notifi_push
                    WHERE usuario_id = @UsuarioId
                      AND CAST(fecha_reg AS date) = CAST(GETDATE() AS date)
                    ORDER BY fecha_reg DESC",
                    new { UsuarioId = usuarioId });

                return rows.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar notificaciones de hoy para usuario {UsuarioId}", usuarioId);
                return new List<NotificacionDto>();
            }
        }

        public async Task<bool> EliminarNotificacionAsync(int notificacionId, int usuarioId)
        {
            if (notificacionId <= 0 || usuarioId <= 0) return false;

            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                int rows = await conn.ExecuteAsync(@"
                    DELETE FROM dbo.notifi_push
                    WHERE notifi_push_id = @Id AND usuario_id = @UsuarioId",
                    new { Id = notificacionId, UsuarioId = usuarioId });
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar notificación {Id}", notificacionId);
                return false;
            }
        }

        public async Task<int> VaciarNotificacionesDeHoyAsync(int usuarioId)
        {
            if (usuarioId <= 0) return 0;

            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                int rows = await conn.ExecuteAsync(@"
                    DELETE FROM dbo.notifi_push
                    WHERE usuario_id = @UsuarioId
                      AND CAST(fecha_reg AS date) = CAST(GETDATE() AS date)",
                    new { UsuarioId = usuarioId });
                return rows;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al vaciar notificaciones de hoy para usuario {UsuarioId}", usuarioId);
                return 0;
            }
        }
    }
}

