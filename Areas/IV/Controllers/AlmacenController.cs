using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.IV.Controllers;

[Area("IV")]
public class AlmacenController(IInventarioService service, IMaestrosService maestros, IRHService contextService, IAuthService authService, ILogger<AlmacenController> logger)
    : InventarioContextController(contextService, authService, logger, "almacen/index")
{
    public async Task<IActionResult> Index(string? search = null, int? sedeId = null, string? estado = null, int page = 1, int top = 25, string? sort = null, string? dir = null)
    {
        if (!ModelState.IsValid || sedeId is <= 0) return InvalidInput();
        ViewBag.Search = search; ViewBag.SedeId = sedeId; ViewBag.Estado = estado;
        return View(await service.GetAlmacenesAsync(EmpresaId, search, sedeId, estado, page, top, sort, dir));
    }

    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var item = await service.GetAlmacenAsync(id, EmpresaId);
        return item is null ? NotFound(new { success = false, message = "No se encontró el almacén." }) : Json(new { success = true, data = item });
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var detail = await service.GetAlmacenDetalleAsync(id, EmpresaId);
        return detail is null ? NotFound(new { success = false, message = "No se encontró el almacén." }) : Json(new { success = true, data = detail });
    }

    [HttpGet]
    public async Task<IActionResult> Configuracion(int almacenId, int id)
    {
        if (!ModelState.IsValid || almacenId <= 0 || id <= 0) return InvalidInput();
        var items = await service.GetConfiguracionAsync(almacenId, id, EmpresaId);
        return items is null ? NotFound(new { success = false, message = "No se encontró la transacción vinculada." }) : Json(new { success = true, data = items });
    }

    [HttpGet]
    public async Task<IActionResult> Provincias(int departamentoId)
        => !ModelState.IsValid || departamentoId <= 0 ? InvalidInput() : Json(new { success = true, data = await maestros.GetProvinciasLookupAsync(departamentoId) });

    [HttpGet]
    public async Task<IActionResult> Distritos(int provinciaId)
        => !ModelState.IsValid || provinciaId <= 0 ? InvalidInput() : Json(new { success = true, data = await maestros.GetDistritosLookupAsync(provinciaId) });

    [HttpPost]
    public async Task<IActionResult> Guardar(AlmacenModel model)
        => ModelState.IsValid ? Result(await service.SaveAlmacenAsync(model, EmpresaId, UsuarioId)) : InvalidInput();

    [HttpPost]
    public async Task<IActionResult> Anular(int id)
        => ModelState.IsValid ? Result(await service.AnularAlmacenAsync(id, EmpresaId, UsuarioId)) : InvalidInput();

    [HttpPost]
    public async Task<IActionResult> GuardarTransaccion(AlmacenTransaccionModel model)
        => ModelState.IsValid ? Result(await service.SaveAlmacenTransaccionAsync(model, EmpresaId, UsuarioId)) : InvalidInput();

    [HttpPost]
    public async Task<IActionResult> AnularTransaccion(int almacenId, int id)
        => ModelState.IsValid ? Result(await service.AnularAlmacenTransaccionAsync(almacenId, id, EmpresaId, UsuarioId)) : InvalidInput();

    [HttpPost]
    public async Task<IActionResult> GuardarConfiguracion(int almacenId, AlmacenConfiguracionModel model)
        => ModelState.IsValid ? Result(await service.SaveConfiguracionAsync(almacenId, model, EmpresaId, UsuarioId)) : InvalidInput();

    [HttpPost]
    public async Task<IActionResult> AgregarSerie(int almacenId, int documentoSerieId)
        => ModelState.IsValid ? Result(await service.AgregarSerieAsync(almacenId, documentoSerieId, EmpresaId, UsuarioId)) : InvalidInput();

    [HttpPost]
    public async Task<IActionResult> AnularSerie(int almacenId, int id)
        => ModelState.IsValid ? Result(await service.AnularSerieAsync(almacenId, id, EmpresaId, UsuarioId)) : InvalidInput();
}
