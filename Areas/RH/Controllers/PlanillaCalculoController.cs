using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.RH.Controllers;

[Area("RH")]
[Authorize]
public class PlanillaCalculoController : RHContextController
{
    public PlanillaCalculoController(IRHService rhService, IAuthService auth, ILogger<PlanillaCalculoController> logger)
        : base(rhService, auth, logger, "planillaCalculo/index")
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? anio = null, int? mes = null, string? search = null, int page = 1, int top = 25, string? sort = null, string? dir = null)
    {
        ViewBag.Anio = anio;
        ViewBag.Mes = mes;
        ViewBag.Search = search;
        var model = await RHService.GetPlanillasProcesoPageAsync(
            CurrentEmpresaId, CurrentPeriodoEmpSedeId, anio, mes, search, page, top, sort, dir);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerDetalle(int id, int? personalId = null)
    {
        if (id <= 0) return BadRequest(new { success = false, message = "ID inválido." });
        var items = await RHService.GetPlanillaCalculoDetalleAsync(id, CurrentEmpresaId, CurrentPeriodoEmpSedeId, personalId);
        return Json(new { success = true, data = items });
    }
}

