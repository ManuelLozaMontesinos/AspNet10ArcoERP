using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.RH.Controllers;

[Area("RH")]
public class RegistroContratoController(
    IRHService service,
    IAuthService auth,
    ILogger<RegistroContratoController> logger)
    : RHContextController(service, auth, logger, "registroContrato/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search = null, int? tipoContratoId = null, int page = 1, int top = 25)
        => View(await RHService.GetContratosPersonalAsync(CurrentEmpresaId, search, tipoContratoId, page, top));

    [HttpPost]
    public async Task<IActionResult> Actualizar([FromBody] ActualizarContratoSolicitud model)
    {
        if (!ModelState.IsValid) return BadRequest(new { success = false, message = "Datos inválidos." });
        var (success, message) = await RHService.ActualizarContratoPersonalAsync(CurrentEmpresaId, CurrentUserId, model);
        return Json(new { success, message });
    }
}
