using System.Security.Claims;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.AS.Controllers
{
    [Area("AS")]
    [Authorize]
    public class PeriodoController : Controller
    {
        private readonly IAdministracionService _administracionService;

        public PeriodoController(IAdministracionService administracionService)
        {
            _administracionService = administracionService;
        }

        private int CurrentUserId =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 1;

        public async Task<IActionResult> Index(int? anio = null, int page = 1, int top = 25, string? sort = null, string? dir = null)
        {
            if (!ModelState.IsValid || anio is < 1900 or > 9999)
                return BadRequest(new { success=false, message="Ingrese un año válido para la búsqueda." });
            var pageSize = top is 25 or 50 or 100 or 500 ? top : 25;
            var model = await _administracionService.GetPeriodosAsync(anio, page, pageSize, sort, dir);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Obtener(int id)
        {
            if (id <= 0)
                return BadRequest(new { success = false, message = "El identificador del período no es válido." });

            var item = await _administracionService.GetPeriodoAsync(id);
            return item is null
                ? NotFound(new { success = false, message = "No se encontró el período indicado." })
                : Json(new { success = true, data = item });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar([FromForm] PeriodoSistemaModel? model)
        {
            if (model is null)
                return BadRequest(new { success = false, message = "No se recibieron los datos del período." });

            var result = await _administracionService.SavePeriodoAsync(model, CurrentUserId);
            return result.success
                ? Json(new { success = true, message = result.message, id = result.id })
                : BadRequest(new { success = false, message = result.message });
        }
    }
}
