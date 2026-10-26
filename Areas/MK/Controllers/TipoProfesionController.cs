using System.Security.Claims;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.MK.Controllers
{
    [Area("MK")]
    [Authorize]
    public class TipoProfesionController : Controller
    {
        private readonly IMKService _mkService;

        public TipoProfesionController(IMKService mkService)
        {
            _mkService = mkService;
        }

        private int CurrentUserId
        {
            get
            {
                var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(value, out var id) ? id : 1;
            }
        }

        public async Task<IActionResult> Index(
            string? search = null,
            string? estado = null,
            int page = 1,
            int top = 25)
        {
            var pageSize = top is 25 or 50 or 100 or 500 ? top : 25;
            var model = await _mkService.GetTipoProfesionesAsync(search, estado, page, pageSize);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Obtener(int id)
        {
            var model = await _mkService.GetTipoProfesionByIdAsync(id);
            return model == null || model.Estado != "A"
                ? NotFound(new { success = false, message = "Tipo de profesión u oficio no encontrado o inactivo." })
                : Json(new { success = true, data = model });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar([FromForm] TipoProfesionModel? model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "No se recibieron los datos." });

            var result = await _mkService.SaveTipoProfesionAsync(model, CurrentUserId);
            return result.success
                ? Json(new { success = true, message = result.message, id = result.id })
                : BadRequest(new { success = false, message = result.message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Inactivar(int id)
        {
            var result = await _mkService.InactivarTipoProfesionAsync(id, CurrentUserId);
            return result.success
                ? Json(new { success = true, message = result.message })
                : BadRequest(new { success = false, message = result.message });
        }
    }
}
