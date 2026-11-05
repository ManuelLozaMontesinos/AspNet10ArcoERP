using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.AS.Controllers;

[Area("AS")]
public class IntegraInventaController(
    IAdministracionService service,
    IRHService context,
    IAuthService auth,
    ILogger<IntegraInventaController> logger)
    : ModuloContextController(context, auth, logger, "as", "integraInventa/index")
{
    [HttpGet]
    public async Task<IActionResult> Index()
        => View(await service.GetProcesosIntegracionAsync(3));

    [HttpPost]
    public async Task<IActionResult> Ejecutar(int procesoId)
    {
        if (procesoId <= 0) return InvalidInput();
        var (success, message) = await service.EjecutarProcesoIntegracionAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, procesoId);
        return Json(new { success, message });
    }
}
