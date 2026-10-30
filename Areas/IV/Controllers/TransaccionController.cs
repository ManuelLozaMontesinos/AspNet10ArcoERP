using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.IV.Controllers;

[Area("IV")]
public class TransaccionController(IInventarioService service, IRHService contextService, IAuthService authService, ILogger<TransaccionController> logger)
    : InventarioContextController(contextService, authService, logger, "transaccion/index")
{
    public async Task<IActionResult> Index(string? search = null, int? tipoMovimientoId = null, string? estado = null, int page = 1, int top = 25, string? sort = null, string? dir = null)
    {
        if (!ModelState.IsValid || tipoMovimientoId is <= 0) return InvalidInput();
        ViewBag.Search = search; ViewBag.TipoMovimientoId = tipoMovimientoId; ViewBag.Estado = estado;
        return View(await service.GetTransaccionesAsync(EmpresaId, search, tipoMovimientoId, estado, page, top, sort, dir));
    }

    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var item = await service.GetTransaccionAsync(id, EmpresaId);
        return item is null ? NotFound(new { success = false, message = "No se encontró la transacción." }) : Json(new { success = true, data = item });
    }

    [HttpPost]
    public async Task<IActionResult> Guardar(TransaccionAlmacenModel model)
        => ModelState.IsValid ? Result(await service.SaveTransaccionAsync(model, EmpresaId, UsuarioId)) : InvalidInput();

    [HttpPost]
    public async Task<IActionResult> Anular(int id)
        => ModelState.IsValid ? Result(await service.AnularTransaccionAsync(id, EmpresaId, UsuarioId)) : InvalidInput();
}
