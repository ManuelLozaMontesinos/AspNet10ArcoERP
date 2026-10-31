using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.AS.Controllers;

[Area("AS")]
public class UsuarioController(
    IAdministracionService service,
    IRHService context,
    IAuthService auth,
    ILogger<UsuarioController> logger)
    : ModuloContextController(context, auth, logger, "as", "usuario/index")
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
            : View(await service.GetUsuariosAsync(EmpresaId, search, estado, page, top, sort, dir));

    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        if (!ModelState.IsValid || id < 0) return InvalidInput();
        var data = await service.GetUsuarioDetalleAsync(EmpresaId, id);
        return data is null
            ? NotFound(new { success = false, message = "No se encontró el usuario." })
            : Json(new { success = true, data });
    }

    [HttpPost]
    public async Task<IActionResult> Guardar([FromBody] UsuarioGuardarSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        return Result(await service.SaveUsuarioAsync(EmpresaId, UsuarioId, model));
    }

    [HttpPost]
    public async Task<IActionResult> CambiarEstado(int id, string estado)
    {
        if (!ModelState.IsValid || id <= 0 || (estado != "A" && estado != "I")) return InvalidInput();
        var (success, message) = await service.CambiarEstadoUsuarioAsync(id, estado);
        return success ? Json(new { success = true, message }) : BadRequest(new { success = false, message });
    }

    [HttpPost]
    public async Task<IActionResult> CambiarContrasena([FromBody] UsuarioCambiarContrasenaSolicitud model)
    {
        if (!ModelState.IsValid || model.UsuarioId <= 0) return InvalidInput();
        var (success, message) = await service.CambiarContrasenaUsuarioAsync(model);
        return success ? Json(new { success = true, message }) : BadRequest(new { success = false, message });
    }
}

