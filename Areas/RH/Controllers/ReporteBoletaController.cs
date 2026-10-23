using System.Linq;
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
    public class ReporteBoletaController : Controller
    {
        private readonly IRHService _rhService;

        public ReporteBoletaController(IRHService rhService)
        {
            _rhService = rhService;
        }

        private int CurrentEmpresaId
        {
            get
            {
                var empStr = User.FindFirst("EmpresaId")?.Value;
                return int.TryParse(empStr, out int id) ? id : 1;
            }
        }

        public async Task<IActionResult> Index(int? planProceId = null, int? personalId = null)
        {
            var planillas = await _rhService.GetPlanillasProcesadasLookupAsync(CurrentEmpresaId);
            ViewBag.Planillas = planillas;

            if (!planProceId.HasValue && planillas.Any())
            {
                planProceId = planillas.First().Id;
            }

            ViewBag.PlanProceId = planProceId;

            var colaboradores = planProceId.HasValue 
                ? await _rhService.GetColaboradoresPorPlanillaProcesadaAsync(planProceId.Value)
                : new System.Collections.Generic.List<LookupItemModel>();
            ViewBag.Colaboradores = colaboradores;

            if (!personalId.HasValue && colaboradores.Any())
            {
                personalId = colaboradores.First().Id;
            }

            ViewBag.PersonalId = personalId;

            BoletaPagoModel? boleta = null;
            if (planProceId.HasValue && personalId.HasValue)
            {
                boleta = await _rhService.GetBoletaPagoAsync(planProceId.Value, personalId.Value);
            }

            return View(boleta);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerColaboradores(int planProceId)
        {
            var list = await _rhService.GetColaboradoresPorPlanillaProcesadaAsync(planProceId);
            return Json(new { success = true, data = list });
        }

        [HttpGet]
        public async Task<IActionResult> BoletaDetalle(int planProceId, int personalId)
        {
            var boleta = await _rhService.GetBoletaPagoAsync(planProceId, personalId);
            if (boleta == null)
                return Json(new { success = false, message = "Boleta no encontrada." });

            return Json(new { success = true, data = boleta });
        }

        [HttpGet]
        public async Task<IActionResult> Imprimir(int planProceId, int? personalId = null)
        {
            var boletas = new System.Collections.Generic.List<BoletaPagoModel>();

            if (personalId.HasValue)
            {
                var b = await _rhService.GetBoletaPagoAsync(planProceId, personalId.Value);
                if (b != null) boletas.Add(b);
            }
            else
            {
                boletas = await _rhService.GetBoletasPorPlanillaAsync(planProceId);
            }

            return View(boletas);
        }
    }
}
