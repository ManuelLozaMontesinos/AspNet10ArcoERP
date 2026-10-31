using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class ListaPrecioController(IVentasService service, IRHService context, IAuthService auth, ILogger<ListaPrecioController> logger)
    : ModuloContextController(context, auth, logger, "vt", "listaPrecio/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search = null, int? sedeId = null, int? monedaId = null, string? estado = null, int page = 1, int top = 25, string? sort = null, string? dir = null)
        => !ModelState.IsValid || search?.Length > 100
            ? InvalidInput()
            : View(await service.GetListasPrecioAsync(EmpresaId, PeriodoEmpresaSedeId, search, sedeId, monedaId, estado, page, top, sort, dir));

    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var model = await service.GetListaPrecioDetalleAsync(EmpresaId, id);
        return model is null ? NotFound(new { success = false, message = "No se encontró la lista de precios seleccionada." }) : Json(new { success = true, data = model });
    }

    [HttpGet]
    public async Task<IActionResult> Lookups(string tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo)) return InvalidInput();
        var data = await service.GetListaPrecioLookupsAsync(EmpresaId, tipo);
        return Json(new { success = true, data });
    }

    [HttpPost]
    public async Task<IActionResult> Guardar([FromForm] ListaPrecioSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.SaveListaPrecioAsync(EmpresaId, UsuarioId, model));
    }

    [HttpPost]
    public async Task<IActionResult> Anular(int id, string revision, int contextoId)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        if (contextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.AnularListaPrecioAsync(EmpresaId, UsuarioId, id, revision));
    }

    [HttpGet]
    public async Task<IActionResult> Productos(int listaPrecioId, string? search = null, string? estado = null, int page = 1, int top = 25, string? sort = null, string? dir = null)
    {
        if (!ModelState.IsValid || listaPrecioId <= 0 || search?.Length > 150) return InvalidInput();
        var model = await service.GetListaPrecioProductosAsync(EmpresaId, listaPrecioId, search, estado, page, top, sort, dir);
        return Json(new { success = true, data = model });
    }

    [HttpPost]
    public async Task<IActionResult> GuardarProducto([FromForm] ListaPrecioProductoSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.SaveListaPrecioProductoAsync(EmpresaId, UsuarioId, model));
    }

    [HttpGet]
    public async Task<IActionResult> Grupos(int listaPrecioId)
    {
        if (!ModelState.IsValid || listaPrecioId <= 0) return InvalidInput();
        var data = await service.GetListaPrecioGruposAsync(EmpresaId, listaPrecioId);
        return Json(new { success = true, data });
    }

    [HttpGet]
    public async Task<IActionResult> GruposDisponibles(int listaPrecioId)
    {
        if (!ModelState.IsValid || listaPrecioId <= 0) return InvalidInput();
        var data = await service.GetGruposClienteDisponiblesAsync(EmpresaId, listaPrecioId);
        return Json(new { success = true, data });
    }

    [HttpPost]
    public async Task<IActionResult> GuardarGrupo([FromForm] ListaPrecioGrupoSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.SaveListaPrecioGrupoAsync(EmpresaId, UsuarioId, model));
    }

    [HttpPost]
    public async Task<IActionResult> QuitarGrupo(int id, int contextoId)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        if (contextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.QuitarListaPrecioGrupoAsync(EmpresaId, UsuarioId, id));
    }

    [HttpGet]
    public async Task<IActionResult> GrupoProductos(int listaPProveId, string? search = null, int page = 1, int top = 25)
    {
        if (!ModelState.IsValid || listaPProveId <= 0 || search?.Length > 150) return InvalidInput();
        var model = await service.GetListaPrecioGrupoProductosAsync(EmpresaId, listaPProveId, search, page, top);
        return Json(new { success = true, data = model });
    }

    [HttpPost]
    public async Task<IActionResult> GuardarGrupoProducto([FromForm] ListaPrecioGrupoProductoSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.SaveListaPrecioGrupoProductoAsync(EmpresaId, UsuarioId, model));
    }

    [HttpPost]
    public async Task<IActionResult> QuitarGrupoProducto(int id, int contextoId)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        if (contextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.QuitarListaPrecioGrupoProductoAsync(EmpresaId, UsuarioId, id));
    }

    [HttpGet]
    public async Task<IActionResult> Exportar(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        try
        {
            var (titulo, contenido) = await service.ExportarListaPrecioExcelAsync(EmpresaId, id);
            return File(contenido, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{titulo}.xlsx");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "No se encontró la lista de precios." });
        }
    }
}

