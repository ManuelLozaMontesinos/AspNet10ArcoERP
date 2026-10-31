using System.Threading.Tasks;
using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class ComproAutorizaController(
    IVentasService ventasService,
    IRHService context,
    IAuthService auth,
    ILogger<ComproAutorizaController> logger)
    : ModuloContextController(context, auth, logger, "vt", "comproautoriza/index")
{
    public async Task<IActionResult> Index(
        string? search = null,
        int page = 1,
        int top = 25)
    {
        var model = await ventasService.GetComprobantesAsync(
            EmpresaId, PeriodoEmpresaSedeId, search, null, 1, 1, null, null, page, top);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Autorizar([FromBody] ComprobanteEmisionSolicitud model)
    {
        var (success, message, feResponse) = await ventasService.EmitirComprobanteAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model);
        return Json(new { success, message, feResponse });
    }
}
