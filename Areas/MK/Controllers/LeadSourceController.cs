using System.Security.Claims;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.MK.Controllers
{
    [Area("MK")]
    [Authorize]
    public class LeadSourceController : Controller
    {
        private readonly IMKService _mkService;

        public LeadSourceController(IMKService mkService)
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
            int top = 25, string? sort = null, string? dir = null)
        {
            var pageSize = top is 25 or 50 or 100 or 500 ? top : 25;
            var model = await _mkService.GetLeadSourcesAsync(search, estado, page, pageSize, sort, dir);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Obtener(int id)
        {
            var model = await _mkService.GetLeadSourceByIdAsync(id);
            return model == null
                ? NotFound(new { success = false, message = "Origen de prospecto no encontrado." })
                : Json(new { success = true, data = model });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar([FromForm] LeadSourceModel? model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "No se recibieron los datos del origen." });

            var result = await _mkService.SaveLeadSourceAsync(model, CurrentUserId);
            return result.success
                ? Json(new { success = true, message = result.message, id = result.id })
                : BadRequest(new { success = false, message = result.message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var result = await _mkService.ToggleLeadSourceAsync(id, CurrentUserId);
            return result.success
                ? Json(new { success = true, message = result.message, nuevoEstado = result.nuevoEstado })
                : BadRequest(new { success = false, message = result.message });
        }
    }
}
