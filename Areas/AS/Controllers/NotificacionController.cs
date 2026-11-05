using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.AS.Controllers;

[Area("AS")]
public class NotificacionController(
    INotifyService notifyService,
    IRHService context,
    IAuthService auth,
    ILogger<NotificacionController> logger)
    : ModuloContextController(context, auth, logger, "as", "notificacion/index")
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var notificaciones = await notifyService.GetNotificacionesDeHoyAsync(UsuarioId);
        return View(notificaciones);
    }

    [HttpPost]
    public async Task<IActionResult> Eliminar(int id)
    {
        if (id <= 0) return InvalidInput();
        var ok = await notifyService.EliminarNotificacionAsync(id, UsuarioId);
        return Json(new { success = ok });
    }

    [HttpPost]
    public async Task<IActionResult> Vaciar()
    {
        var total = await notifyService.VaciarNotificacionesDeHoyAsync(UsuarioId);
        return Json(new { success = true, totalBorrados = total });
    }
}
