using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.IV.Controllers;

[Area("IV")]
public class GuiaRemiAutoController(
    IInventarioService service,
    IRHService context,
    IAuthService auth,
    ILogger<GuiaRemiAutoController> logger)
    : InventarioContextController(context, auth, logger, "guiaremiauto/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(
        int? almacenId = null,
        string? codigo = null,
        string? search = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? motivoTrasladoId = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        if (!ModelState.IsValid || search?.Length > 150 || codigo?.Length > 30)
            return InvalidInput();

        var model = await service.GetGuiasRemisionAutoAsync(
            EmpresaId, PeriodoEmpresaSedeId, almacenId, codigo, search, fechaDesde, fechaHasta,
            motivoTrasladoId, page, top, sort, dir);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetDetalle(int id)
    {
        var detalle = await service.GetGuiaRemisionDetalleAsync(EmpresaId, PeriodoEmpresaSedeId, id);
        if (detalle is null && id > 0)
            return NotFound(new { success = false, message = "Guía no encontrada." });

        return Json(new { success = true, data = detalle });
    }

    [HttpPost]
    public async Task<IActionResult> Aprobar([FromBody] AprobarGuiaRemiSolicitud model)
    {
        if (!ModelState.IsValid || model.Id <= 0) return InvalidInput();
        var (success, message, id) = await service.AprobarGuiaRemisionAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model);
        return success 
            ? Json(new { success = true, message, id }) 
            : BadRequest(new { success = false, message });
    }

    [HttpPost]
    public async Task<IActionResult> Desaprobar([FromBody] DesaprobarGuiaRemiSolicitud model)
    {
        if (!ModelState.IsValid || model.Id <= 0) return InvalidInput();
        var (success, message, id) = await service.DesaprobarGuiaRemisionAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model);
        return success 
            ? Json(new { success = true, message, id }) 
            : BadRequest(new { success = false, message });
    }
}
