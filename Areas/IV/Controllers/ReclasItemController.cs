using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.IV.Controllers;

[Area("IV")]
public class ReclasItemController(IInventarioService service, IRHService context, IAuthService auth, ILogger<ReclasItemController> logger)
    : InventarioContextController(context, auth, logger, "reclasItem/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        string? codigo = null,
        int? almacenId = null,
        int? estadoId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
        => !ModelState.IsValid || search?.Length > 150
            ? InvalidInput()
            : View(await service.GetReclasItemsAsync(EmpresaId, PeriodoEmpresaSedeId, search, codigo, almacenId, estadoId, fechaDesde, fechaHasta, page, top, sort, dir));

    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var model = await service.GetReclasItemDetalleAsync(EmpresaId, PeriodoEmpresaSedeId, id);
        return model is null ? NotFound(new { success = false, message = "No se encontró el registro de reclasificación." }) : Json(new { success = true, data = model });
    }

    [HttpPost]
    public async Task<IActionResult> Guardar([FromForm] ReclasItemGuardarSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.SaveReclasItemAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model));
    }

    [HttpPost]
    public async Task<IActionResult> Anular(int id, string revision, int contextoId)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        if (contextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.AnularReclasItemAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, id, revision));
    }

    [HttpPost]
    public async Task<IActionResult> SolicitarAprobacion(int id, string revision, int contextoId)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        if (contextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.SolicitarAprobacionReclasItemAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, id, revision));
    }

    [HttpGet]
    public async Task<IActionResult> Productos(int movimientoAlmacenId)
    {
        if (!ModelState.IsValid || movimientoAlmacenId <= 0) return InvalidInput();
        var data = await service.GetReclasItemProductosAsync(EmpresaId, movimientoAlmacenId);
        return Json(new { success = true, data });
    }

    [HttpPost]
    public async Task<IActionResult> GuardarProducto([FromForm] ReclasItemProductoGuardarSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.SaveReclasItemProductoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model));
    }

    [HttpGet]
    public async Task<IActionResult> BuscarMovimientos(int almacenId, string? q = null)
    {
        if (!ModelState.IsValid || almacenId <= 0) return InvalidInput();
        var data = await service.BuscarMovimientosParaReclasificarAsync(EmpresaId, PeriodoEmpresaSedeId, almacenId, q);
        return Json(new { success = true, data });
    }

    [HttpGet]
    public async Task<IActionResult> BuscarProductos(string? q = null)
    {
        if (!ModelState.IsValid) return InvalidInput();
        var data = await service.BuscarProductosAlmacenAsync(EmpresaId, q);
        return Json(new { success = true, data });
    }
}

