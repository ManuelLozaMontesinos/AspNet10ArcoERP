using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.MK.Controllers
{
    [Area("MK")]
    [Authorize]
    public class MerchandisingController : Controller
    {
        private readonly IMKService _mkService;
        private int CurrentEmpresaSedeId =>
            int.TryParse(User.FindFirst("SedeId")?.Value, out var id) ? id : 1;
        private int CurrentPeriodoAnio =>
            int.TryParse(User.FindFirst("PeriodoAnio")?.Value, out var year) ? year : DateTime.Today.Year;

        public MerchandisingController(IMKService mkService)
        {
            _mkService = mkService;
        }

        public async Task<IActionResult> Index(
            string? search = null,
            int? transaccionId = null,
            DateTime? desde = null,
            DateTime? hasta = null,
            int page = 1,
            int top = 25, string? sort = null, string? dir = null)
        {
            var pageSize = top is 25 or 50 or 100 or 500 ? top : 25;
            if (desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date)
                ModelState.AddModelError(string.Empty, "La fecha inicial no puede ser posterior a la fecha final.");

            var model = await _mkService.GetMerchandisingMovementsAsync(
                search, transaccionId, desde, hasta, CurrentPeriodoAnio, CurrentEmpresaSedeId, page, pageSize, sort, dir);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            if (id <= 0)
                return BadRequest(new { success = false, message = "El movimiento indicado no es válido." });

            var detail = await _mkService.GetMerchandisingMovementDetailAsync(
                id, CurrentPeriodoAnio, CurrentEmpresaSedeId);
            return detail is null
                ? NotFound(new { success = false, message = "No se encontró el movimiento del almacén de Marketing." })
                : Json(new { success = true, data = detail });
        }
    }
}
