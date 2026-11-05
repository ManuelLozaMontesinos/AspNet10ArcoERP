using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNet10ArcoERP.Models;

namespace AspNet10ArcoERP.Services
{
    public interface INotifyService
    {
        Task SendNotificationAsync(int usuarioId, string titulo, string descripcion);
        Task SendCalendarReminderToUsersAsync(IEnumerable<int> usuarioIds, string tituloEvento, DateTime fechaInicio, string? areaNombre = null);
        Task<List<NotificacionDto>> GetNotificacionesDeHoyAsync(int usuarioId);
        Task<bool> EliminarNotificacionAsync(int notificacionId, int usuarioId);
        Task<int> VaciarNotificacionesDeHoyAsync(int usuarioId);
    }
}

