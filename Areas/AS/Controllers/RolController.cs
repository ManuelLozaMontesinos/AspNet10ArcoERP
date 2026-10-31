using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.AS.Controllers;

[Area("AS")]
public class RolController(
    IAdministracionService service,
    IRHService context,
    IAuthService auth,
    ILogger<RolController> logger)
    : ModuloContextController(context, auth, logger, "as", "rol/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        string? estado = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
        => !ModelState.IsValid || search?.Length > 150
            ? InvalidInput()
            : View(await service.GetRolesAsync(EmpresaId, search, estado, page, top, sort, dir));

    [HttpPost]
    public async Task<IActionResult> Guardar([FromBody] RolGuardarSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        return Result(await service.SaveRolAsync(EmpresaId, UsuarioId, model));
    }

    [HttpPost]
    public async Task<IActionResult> Anular(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var (success, message) = await service.AnularRolAsync(id, UsuarioId);
        return success ? Json(new { success = true, message }) : BadRequest(new { success = false, message });
    }
}

