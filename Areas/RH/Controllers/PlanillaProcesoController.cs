using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.RH.Controllers
{
    [Area("RH")]
    [Authorize]
    public class PlanillaProcesoController : Controller
    {
        private readonly IRHService _rhService;

        public PlanillaProcesoController(IRHService rhService)
        {
            _rhService = rhService;
        }

        private int CurrentUserId
        {
            get
            {
                var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(idStr, out int id) ? id : 1;
            }
        }

        private int CurrentEmpresaId
        {
            get
            {
                var empStr = User.FindFirst("EmpresaId")?.Value;
                return int.TryParse(empStr, out int id) ? id : 1;
            }
        }

        public async Task<IActionResult> Index(int? anio = null, int? mes = null, string? search = null, int top = 50)
        {
            ViewBag.Anio = anio;
            ViewBag.Mes = mes;
            ViewBag.Search = search;
            ViewBag.Top = top;

            ViewBag.Planillas = await _rhService.GetPlanillasAdminListAsync(CurrentEmpresaId, estado: "A", top: 100);

            var list = await _rhService.GetPlanillasProcesoListAsync(CurrentEmpresaId, anio, mes, search, top);
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Obtener(int id)
        {
            var item = await _rhService.GetPlanillaProcesoByIdAsync(id);
            if (item == null)
                return Json(new { success = false, message = "Proceso de planilla no encontrado." });

            return Json(new { success = true, data = item });
        }

        [HttpGet]
        public async Task<IActionResult> DetalleCalculo(int id, int? personalId = null)
        {
            var detalles = await _rhService.GetPlanillaCalculoDetalleAsync(id, personalId);
            return Json(new { success = true, data = detalles });
        }

        [HttpPost]
        public async Task<IActionResult> Guardar([FromForm] PlanillaProcesoCrearModel model)
        {
            if (model.PlanillaId <= 0)
                return Json(new { success = false, message = "Debe seleccionar un tipo de planilla." });

            if (model.FechaDesde > model.FechaHasta)
                return Json(new { success = false, message = "La fecha desde no puede ser mayor que la fecha hasta." });

            var result = await _rhService.SavePlanillaProcesoAsync(model, CurrentUserId);
            return Json(new { success = result.success, message = result.message, id = result.id });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleEstado(int id)
        {
            var result = await _rhService.ToggleEstadoPlanillaProcesoAsync(id, CurrentUserId);
            return Json(new { success = result.success, message = result.message, nuevoEstado = result.nuevoEstado });
        }

        [HttpPost]
        public async Task<IActionResult> Procesar(int id)
        {
            var result = await _rhService.ProcesarCalculoPlanillaAsync(id, CurrentUserId);
            return Json(new { success = result.success, message = result.message });
        }
    }
}
