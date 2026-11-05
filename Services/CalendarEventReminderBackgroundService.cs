using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AspNet10ArcoERP.Services
{
    public class CalendarEventReminderBackgroundService : BackgroundService
    {
        private const int ReminderMinutes = 15;
        private const int ClaimLeaseMinutes = 5;
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILogger<CalendarEventReminderBackgroundService> _logger;

        public CalendarEventReminderBackgroundService(
            IServiceScopeFactory scopeFactory,
            IDbConnectionFactory dbConnectionFactory,
            ILogger<CalendarEventReminderBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _dbConnectionFactory = dbConnectionFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var events = ClaimDueEvents();
                    if (events.Count > 0)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var notifyService = scope.ServiceProvider.GetRequiredService<INotifyService>();

                        foreach (var calendarEvent in events)
                        {
                            try
                            {
                                var (memberIds, areaNombre) = ObtenerMiembrosArea(calendarEvent.AreaId);
                                if (memberIds.Count == 0 && calendarEvent.UserId > 0)
                                {
                                    memberIds.Add(calendarEvent.UserId);
                                }

                                await notifyService.SendCalendarReminderToUsersAsync(
                                    memberIds,
                                    calendarEvent.Title,
                                    calendarEvent.Start,
                                    areaNombre);

                                MarkReminderSent(calendarEvent.Id);
                            }
                            catch (Exception ex)
                            {
                                ReleaseReminder(calendarEvent.Id);
                                _logger.LogError(ex, "Error al enviar recordatorio del evento {EventId}", calendarEvent.Id);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consultando recordatorios pendientes del calendario");
                }

                try
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private List<DueCalendarEvent> ClaimDueEvents()
        {
            var events = new List<DueCalendarEvent>();

            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var rows = conn.Query<DueCalendarEvent>(@"
                    ;WITH eventos_pendientes AS
                    (
                        SELECT TOP (25) evento_id
                        FROM dbo.calendario_evento WITH (UPDLOCK, READPAST, ROWLOCK)
                        WHERE estado = 'A'
                          AND todo_el_dia = 0
                          AND fecha_inicio > GETDATE()
                          AND fecha_inicio <= DATEADD(MINUTE, @MinutosAviso, GETDATE())
                          AND
                          (
                             recordatorio_estado = 'P'
                             OR (recordatorio_estado = 'R' AND recordatorio_reclamado_hasta <= GETDATE())
                          )
                        ORDER BY fecha_inicio
                    )
                    UPDATE evento
                    SET recordatorio_estado = 'R',
                        recordatorio_reclamado_hasta = DATEADD(MINUTE, @MinutosReclamo, GETDATE())
                    OUTPUT inserted.evento_id AS Id, 
                           inserted.usuario_id AS UserId, 
                           ISNULL(inserted.area_id, 0) AS AreaId, 
                           inserted.titulo AS Title, 
                           inserted.fecha_inicio AS Start
                    FROM dbo.calendario_evento AS evento
                    INNER JOIN eventos_pendientes AS pendiente ON pendiente.evento_id = evento.evento_id;",
                    new { MinutosAviso = ReminderMinutes, MinutosReclamo = ClaimLeaseMinutes });

                return rows.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reclamar eventos por notificar");
                return events;
            }
        }

        private void MarkReminderSent(int eventId)
        {
            UpdateReminderState(eventId, "E", true);
        }

        private void ReleaseReminder(int eventId)
        {
            UpdateReminderState(eventId, "P", false);
        }

        private void UpdateReminderState(int eventId, string state, bool sent)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                conn.Execute(@"
                    UPDATE dbo.calendario_evento
                    SET recordatorio_estado = @Estado,
                        recordatorio_reclamado_hasta = NULL,
                        recordatorio_enviado_en = CASE WHEN @Sent = 1 THEN GETDATE() ELSE NULL END
                    WHERE evento_id = @Id AND recordatorio_estado = 'R'",
                    new { Estado = state, Sent = sent ? 1 : 0, Id = eventId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando estado de recordatorio para evento {EventId}", eventId);
            }
        }

        private (List<int> memberIds, string? areaNombre) ObtenerMiembrosArea(int areaId)
        {
            var members = new List<int>();
            string? areaNombre = null;

            if (areaId <= 0) return (members, areaNombre);

            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var rows = conn.Query<dynamic>(@"
                    SELECT cua.usuario_id, a.nombre AS area_nombre
                    FROM dbo.calendario_usuario_area cua
                    INNER JOIN dbo.calendario_area a ON cua.area_id = a.area_id
                    INNER JOIN dbo.usuario u ON cua.usuario_id = u.usuario_id
                    WHERE cua.area_id = @AreaId AND a.estado = 'A' AND u.estado = 'A'",
                    new { AreaId = areaId });

                foreach (var r in rows)
                {
                    members.Add((int)r.usuario_id);
                    if (string.IsNullOrEmpty(areaNombre) && r.area_nombre != null)
                    {
                        areaNombre = ((string)r.area_nombre).Trim();
                    }
                }

                if (string.IsNullOrEmpty(areaNombre))
                {
                    areaNombre = conn.ExecuteScalar<string>(
                        "SELECT nombre FROM dbo.calendario_area WHERE area_id = @AreaId",
                        new { AreaId = areaId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar miembros del área {AreaId}", areaId);
            }

            return (members, areaNombre);
        }

        private sealed class DueCalendarEvent
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public int AreaId { get; set; }
            public string Title { get; set; } = string.Empty;
            public DateTime Start { get; set; }
        }
    }
}

