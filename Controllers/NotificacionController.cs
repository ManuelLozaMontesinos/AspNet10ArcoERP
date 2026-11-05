using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Controllers
{
    [Authorize]
    public class NotificacionController : Controller
    {
        private readonly INotifyService _notifyService;

        public NotificacionController(INotifyService notifyService)
        {
            _notifyService = notifyService;
        }

        private int ObtenerUsuarioId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out int id) && id > 0)
                return id;

            var userSessionJson = HttpContext.Session.GetString("USER_SESSION");
            if (!string.IsNullOrEmpty(userSessionJson))
            {
                try
                {
                    var session = JsonSerializer.Deserialize<UserSessionDto>(userSessionJson);
                    if (session != null && session.UsuarioId > 0) return session.UsuarioId;
                }
                catch { }
            }
            return 1;
        }

        [HttpGet("/ListarNotificaciones")]
        [HttpGet("/Notificacion/ListarNotificaciones")]
        public async Task<IActionResult> ListarNotificaciones()
        {
            int usuarioId = ObtenerUsuarioId();
            if (usuarioId <= 0)
            {
                return Json(new { success = false, message = "Sesión no válida.", data = Array.Empty<object>(), total = 0 });
            }

            var notificaciones = await _notifyService.GetNotificacionesDeHoyAsync(usuarioId);
            return Json(new
            {
                success = true,
                data = notificaciones,
                total = notificaciones.Count
            });
        }

        [HttpPost("/EliminarNotificacion")]
        [HttpPost("/Notificacion/EliminarNotificacion")]
        public async Task<IActionResult> EliminarNotificacion([FromForm] int id, [FromQuery] int? idQuery)
        {
            int notificacionId = id > 0 ? id : (idQuery ?? 0);
            if (notificacionId <= 0)
            {
                return Json(new { success = false, message = "ID de notificación no proporcionado." });
            }

            int usuarioId = ObtenerUsuarioId();
            bool ok = await _notifyService.EliminarNotificacionAsync(notificacionId, usuarioId);
            return Json(new { success = ok });
        }

        [HttpPost("/VaciarNotificaciones")]
        [HttpPost("/Notificacion/VaciarNotificaciones")]
        public async Task<IActionResult> VaciarNotificaciones()
        {
            int usuarioId = ObtenerUsuarioId();
            int total = await _notifyService.VaciarNotificacionesDeHoyAsync(usuarioId);
            return Json(new { success = true, totalBorrados = total });
        }
    }
}

