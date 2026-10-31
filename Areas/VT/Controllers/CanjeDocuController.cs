using System.Threading.Tasks;
using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class CanjeDocuController(
    IVentasService ventasService,
    IRHService context,
    IAuthService auth,
    ILogger<CanjeDocuController> logger)
    : ModuloContextController(context, auth, logger, "vt", "canjedocu/index")
{
    public async Task<IActionResult> Index(string? search = null, int page = 1, int top = 25)
    {
        var model = await ventasService.GetCanjeDocuAsync(EmpresaId, PeriodoEmpresaSedeId, search, page, top);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Procesar([FromBody] CanjeDocuModel model)
    {
        var (success, message, id) = await ventasService.ProcesarCanjeDocuAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model);
        return Json(new { success, message, id });
    }
}
