using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.AS.Controllers;

[Area("AS")]
public class IntegraCapitalController(
    IAdministracionService service,
    IRHService context,
    IAuthService auth,
    ILogger<IntegraCapitalController> logger)
    : ModuloContextController(context, auth, logger, "as", "integraCapital/index")
{
    [HttpGet]
    public async Task<IActionResult> Index()
        => View(await service.GetProcesosIntegracionAsync(1));

    [HttpPost]
    public async Task<IActionResult> Ejecutar(int procesoId)
    {
        if (procesoId <= 0) return InvalidInput();
        var (success, message) = await service.EjecutarProcesoIntegracionAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, procesoId);
        return Json(new { success, message });
    }
}
