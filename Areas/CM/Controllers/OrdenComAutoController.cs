using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.CM.Controllers;

[Area("CM")]
public class OrdenComAutoController(IComprasService service, IRHService context, IAuthService auth, ILogger<OrdenComAutoController> logger)
    : ModuloContextController(context, auth, logger, "cm", "ordenComAuto/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        string? codigo = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? tipoProductoId = null,
        int? estadoAprobacionId = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
        => !ModelState.IsValid || search?.Length > 150
            ? InvalidInput()
            : View(await service.GetOrdenesComAutoAsync(EmpresaId, PeriodoEmpresaSedeId, search, codigo, fechaDesde, fechaHasta, tipoProductoId, estadoAprobacionId, page, top, sort, dir));

    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var model = await service.GetOrdenCompraDetalleAsync(EmpresaId, PeriodoEmpresaSedeId, id);
        return model is null ? NotFound(new { success = false, message = "No se encontró la orden de compra." }) : Json(new { success = true, data = model });
    }

    [HttpPost]
    public async Task<IActionResult> Aprobar([FromForm] AprobarOrdenComAutoSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.AprobarOrdenComAutoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model));
    }

    [HttpPost]
    public async Task<IActionResult> Desaprobar([FromForm] DesaprobarOrdenComAutoSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.DesaprobarOrdenComAutoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model));
    }
}

