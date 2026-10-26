using System.Security.Claims;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.RH.Controllers
{
    [Area("RH")]
    [Authorize]
    public class PlanillaRegulaController : Controller
    {
        private readonly IRHService _rhService;

        public PlanillaRegulaController(IRHService rhService)
        {
            _rhService = rhService;
        }

        private int CurrentUserId
        {
            get
            {
                var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(value, out var id) ? id : 1;
            }
        }

        private int CurrentEmpresaId
        {
            get
            {
                var value = User.FindFirst("EmpresaId")?.Value;
                return int.TryParse(value, out var id) ? id : 1;
            }
        }

        public async Task<IActionResult> Index(
            int? anio = null,
            int? mes = null,
            string? search = null,
            int top = 25)
        {
            var pageSize = top is 25 or 50 or 100 or 500 ? top : 25;
            ViewBag.Anio = anio;
            ViewBag.Mes = mes;
            ViewBag.Search = search;
            ViewBag.Top = pageSize;

            var procesos = await _rhService.GetPlanillasProcesoListAsync(
                CurrentEmpresaId,
                anio,
                mes,
                search,
                pageSize);
            return View(procesos);
        }

        [HttpGet]
        public async Task<IActionResult> Obtener(int id, int page = 1, int top = 25)
        {
            var pageSize = top is 25 or 50 or 100 or 500 ? top : 25;
            var regularizaciones = await _rhService.GetPlanillaRegularizacionesAsync(
                id,
                CurrentEmpresaId,
                page,
                pageSize);
            if (regularizaciones == null)
                return NotFound(new { success = false, message = "Proceso de planilla no encontrado para la empresa activa." });

            var proceso = await _rhService.GetPlanillaProcesoByIdAsync(id);
            if (proceso == null)
                return NotFound(new { success = false, message = "Proceso de planilla no encontrado." });

            return Json(new { success = true, proceso, regularizaciones });
        }

        [HttpGet]
        public async Task<IActionResult> PersonalDisponible(int id, string? search = null, int top = 25)
        {
            var personal = await _rhService.GetPersonalDisponibleRegularizacionAsync(
                id,
                CurrentEmpresaId,
                search,
                top);
            if (personal == null)
                return NotFound(new { success = false, message = "Proceso de planilla no encontrado para la empresa activa." });

            return Json(new { success = true, data = personal });
        }

        [HttpGet]
        public async Task<IActionResult> ConceptosDisponibles(int id, string? search = null, int top = 25)
        {
            var conceptos = await _rhService.GetConceptosDisponiblesRegularizacionAsync(
                id,
                CurrentEmpresaId,
                search,
                top);
            if (conceptos == null)
                return NotFound(new { success = false, message = "Proceso de planilla no encontrado para la empresa activa." });

            return Json(new { success = true, data = conceptos });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar([FromBody] PlanillaRegularizacionGuardarModel? model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "No se recibieron los datos de la regularización." });

            var result = await _rhService.SavePlanillaRegularizacionAsync(
                model,
                CurrentEmpresaId,
                CurrentUserId);
            return result.success
                ? Json(new { success = true, message = result.message })
                : BadRequest(new { success = false, message = result.message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Anular(int id, int planProceId)
        {
            var result = await _rhService.AnularPlanillaRegularizacionAsync(
                id,
                planProceId,
                CurrentEmpresaId,
                CurrentUserId);
            return result.success
                ? Json(new { success = true, message = result.message })
                : BadRequest(new { success = false, message = result.message });
        }
    }
}
