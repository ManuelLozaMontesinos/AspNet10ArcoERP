using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.RH.Controllers;

[Area("RH")]
public class PlanillaController(IRHService rhService, ILogger<PlanillaController> logger)
    : RHContextController(rhService, logger, "planilla/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search=null, string? estado=null, int page=1, int top=25, string? sort=null, string? dir=null)
    {
        if (!ModelState.IsValid || search?.Length > 150 || (!string.IsNullOrEmpty(estado) && estado is not ("A" or "I")))
            return BadRequest(new { success=false, message="Los filtros indicados no son válidos." });
        ViewBag.Search=search;
        ViewBag.Estado=estado;
        return View(await RHService.GetPlanillasPageAsync(CurrentEmpresaId,search,estado,page,top,sort,dir));
    }

    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        var item=await RHService.GetPlanillaAdminByIdAsync(id,CurrentEmpresaId);
        return item is null ? NotFound(new { success=false, message="Planilla no encontrada para esta empresa." })
            : Json(new { success=true, data=item });
    }

    [HttpGet]
    public async Task<IActionResult> Conceptos(int id)
    {
        var items=await RHService.GetPlanillaConceptosAsync(id,CurrentEmpresaId);
        return items is null ? NotFound(new { success=false, message="Planilla no encontrada para esta empresa." })
            : Json(new { success=true, data=items });
    }

    [HttpPost]
    public async Task<IActionResult> Guardar([FromForm] PlanillaModel? model)
    {
        if (model is null || !ModelState.IsValid)
            return BadRequest(new { success=false, message="Revise los datos de la planilla." });
        model.EmpresaId=CurrentEmpresaId;
        var result=await RHService.SavePlanillaAsync(model,CurrentUserId);
        return result.success ? Json(new { success=true, message=result.message, id=result.id })
            : BadRequest(new { success=false, message=result.message });
    }

    [HttpPost]
    public async Task<IActionResult> Anular(int id)
    {
        var result=await RHService.AnularPlanillaAsync(id,CurrentEmpresaId,CurrentUserId);
        return result.success ? Json(new { success=true, message=result.message })
            : BadRequest(new { success=false, message=result.message });
    }

    [HttpPost]
    public async Task<IActionResult> GuardarConcepto([FromForm] PlanillaConceptoModel? model)
    {
        if (model is null || !ModelState.IsValid)
            return BadRequest(new { success=false, message="Revise el concepto y su secuencia." });
        var result=await RHService.SavePlanillaConceptoAsync(model,CurrentEmpresaId,CurrentUserId);
        return result.success ? Json(new { success=true, message=result.message, id=result.id })
            : BadRequest(new { success=false, message=result.message });
    }

    [HttpPost]
    public async Task<IActionResult> AnularConcepto(int id, int planillaId)
    {
        var result=await RHService.AnularPlanillaConceptoAsync(id,planillaId,CurrentEmpresaId,CurrentUserId);
        return result.success ? Json(new { success=true, message=result.message })
            : BadRequest(new { success=false, message=result.message });
    }
}
