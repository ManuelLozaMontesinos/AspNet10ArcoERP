using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class PedidoController(IVentasService service, IRHService context, IAuthService auth, ILogger<PedidoController> logger)
    : ModuloContextController(context, auth, logger, "vt", "pedido/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        string? codigo = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? vendedorId = null,
        int? estadoCotizacionId = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
        => !ModelState.IsValid || search?.Length > 150
            ? InvalidInput()
            : View(await service.GetPedidosAsync(EmpresaId, PeriodoEmpresaSedeId, search, codigo, fechaDesde, fechaHasta, vendedorId, estadoCotizacionId, page, top, sort, dir));

    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var model = await service.GetPedidoDetalleAsync(EmpresaId, PeriodoEmpresaSedeId, id);
        return model is null ? NotFound(new { success = false, message = "No se encontró el pedido seleccionado." }) : Json(new { success = true, data = model });
    }

    [HttpPost]
    public async Task<IActionResult> Guardar([FromForm] PedidoGuardarSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.SavePedidoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model));
    }

    [HttpPost]
    public async Task<IActionResult> Anular(int id, string revision, int contextoId)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        if (contextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.AnularPedidoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, id, revision));
    }

    [HttpPost]
    public async Task<IActionResult> CambiarACotizacion(int id, string revision, int contextoId)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        if (contextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.CambiarACotizacionAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, id, revision));
    }

    [HttpPost]
    public async Task<IActionResult> CambiarEstado([FromForm] CambiarEstadoPedidoSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.CambiarEstadoPedidoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model));
    }

    [HttpGet]
    public async Task<IActionResult> Productos(int pedidoId)
    {
        if (!ModelState.IsValid || pedidoId <= 0) return InvalidInput();
        var data = await service.GetCotizacionProductosAsync(EmpresaId, pedidoId);
        return Json(new { success = true, data });
    }

    [HttpPost]
    public async Task<IActionResult> GuardarProducto([FromForm] CotizacionProductoGuardarSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.SaveCotizacionProductoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model));
    }

    [HttpPost]
    public async Task<IActionResult> EliminarProducto(int id, int contextoId)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        if (contextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.EliminarCotizacionProductoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, id));
    }

    [HttpGet]
    public async Task<IActionResult> Exportar(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        try
        {
            var (titulo, contenido) = await service.ExportarPedidoExcelAsync(EmpresaId, id);
            return File(contenido, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{titulo}.xlsx");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "No se encontró el pedido." });
        }
    }
}

