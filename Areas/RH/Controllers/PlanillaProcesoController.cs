using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.RH.Controllers
{
    [Area("RH")]
    [Authorize]
    [AutoValidateAntiforgeryToken]
    public class PlanillaProcesoController : RHContextController
    {


        public PlanillaProcesoController(IRHService rhService, ILogger<PlanillaProcesoController> logger)
            : base(rhService, logger, "planillaProceso/index")
        {

        }

        [HttpGet]
        public async Task<IActionResult> Index(int? anio = null, int? mes = null, string? search = null, int page = 1, int top = 25, string? sort = null, string? dir = null)
        {
            if (!ModelState.IsValid || search?.Length > 150)
                return BadRequest(new { success = false, message = "Los filtros de búsqueda no son válidos." });
            ViewBag.Anio = anio;
            ViewBag.Mes = mes;
            ViewBag.Search = search;
            return View(await RHService.GetPlanillasProcesoPageAsync(
                CurrentEmpresaId, CurrentPeriodoEmpSedeId, anio, mes, search, page, top, sort, dir));
        }

        [HttpGet]
        public async Task<IActionResult> Obtener(int id)
        {
            if (id <= 0)
                return BadRequest(new { success = false, message = "El identificador del proceso no es válido." });
            var item = await RHService.GetPlanillaProcesoByIdAsync(id, CurrentEmpresaId, CurrentPeriodoEmpSedeId);
            return item is null
                ? NotFound(new { success = false, message = "Proceso no encontrado para el período y la sede activos." })
                : Json(new { success = true, data = item });
        }

        [HttpGet]
        public async Task<IActionResult> DetalleCalculo(int id, int? personalId = null)
        {
            if (id <= 0 || personalId <= 0)
                return BadRequest(new { success = false, message = "El proceso o el trabajador indicado no es válido." });
            var items = await RHService.GetPlanillaCalculoDetalleAsync(id, CurrentEmpresaId, CurrentPeriodoEmpSedeId, personalId);
            return items is null
                ? NotFound(new { success = false, message = "Proceso no encontrado para el período y la sede activos." })
                : Json(new { success = true, data = items });
        }

        [HttpPost]
        public async Task<IActionResult> Guardar([FromForm] PlanillaProcesoCrearModel? model)
        {
            if (model is null || !ModelState.IsValid)
                return BadRequest(new { success = false, message = "Revise los campos obligatorios, las fechas y el tipo de cambio." });
            var result = await RHService.SavePlanillaProcesoAsync(model, CurrentUserId, CurrentEmpresaId, CurrentPeriodoEmpSedeId);
            return result.success
                ? Json(new { success = true, message = result.message, id = result.id })
                : BadRequest(new { success = false, message = result.message });
        }

        [HttpPost]
        public async Task<IActionResult> Anular(int id)
        {
            var result = await RHService.InactivarPlanillaProcesoAsync(id, CurrentUserId, CurrentEmpresaId, CurrentPeriodoEmpSedeId);
            return result.success
                ? Json(new { success = true, message = result.message })
                : BadRequest(new { success = false, message = result.message });
        }

        [HttpPost]
        public async Task<IActionResult> Procesar(int id)
        {
            if (id <= 0 || await RHService.GetPlanillaProcesoByIdAsync(id, CurrentEmpresaId, CurrentPeriodoEmpSedeId) is null)
                return NotFound(new { success = false, message = "Proceso no encontrado para el período y la sede activos." });
            var result = await RHService.ProcesarCalculoPlanillaAsync(id, CurrentUserId);
            return StatusCode(StatusCodes.Status501NotImplemented, new { success = false, message = result.message });
        }
    }
}
