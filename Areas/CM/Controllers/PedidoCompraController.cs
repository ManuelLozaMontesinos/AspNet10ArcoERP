using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.CM.Controllers;

[Area("CM")]
public class PedidoCompraController(IComprasService service, IRHService context, IAuthService auth, ILogger<PedidoCompraController> logger)
    : ModuloContextController(context, auth, logger, "cm", "pedidoCompra/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        string? codigo = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? tipoProductoId = null,
        int? estadoPedidoCompraId = null,
        int? estadoAprobacionId = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
        => !ModelState.IsValid || search?.Length > 150
            ? InvalidInput()
            : View(await service.GetPedidosCompraAsync(EmpresaId, PeriodoEmpresaSedeId, search, codigo, fechaDesde, fechaHasta, tipoProductoId, estadoPedidoCompraId, estadoAprobacionId, page, top, sort, dir));

    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        if (!ModelState.IsValid || id < 0) return InvalidInput();
        var model = await service.GetPedidoCompraDetalleAsync(EmpresaId, PeriodoEmpresaSedeId, id);
        return model is null ? NotFound(new { success = false, message = "No se encontró el requerimiento de compra." }) : Json(new { success = true, data = model });
    }

    [HttpPost]
    public async Task<IActionResult> Guardar([FromForm] PedidoCompraGuardarSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.SavePedidoCompraAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model));
    }

    [HttpPost]
    public async Task<IActionResult> Anular(int id, string revision, int contextoId)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        if (contextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.AnularPedidoCompraAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, id, revision));
    }

    [HttpPost]
    public async Task<IActionResult> SolicitarAprobacion(int id, string revision, int contextoId)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        if (contextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.EnviarAprobacionPedidoCompraAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, id, revision));
    }

    [HttpGet]
    public async Task<IActionResult> BuscarProveedores(string? search)
    {
        if (!ModelState.IsValid) return InvalidInput();
        var data = await service.BuscarProveedoresCompraAsync(EmpresaId, search);
        return Json(new { success = true, data });
    }

    [HttpGet]
    public async Task<IActionResult> BuscarProductos(int? tipoProductoId, string? search)
    {
        if (!ModelState.IsValid) return InvalidInput();
        var data = await service.BuscarProductosCompraAsync(EmpresaId, tipoProductoId, search);
        return Json(new { success = true, data });
    }

    [HttpGet]
    public async Task<IActionResult> Productos(int pedidoCompraId)
    {
        if (!ModelState.IsValid || pedidoCompraId <= 0) return InvalidInput();
        var data = await service.GetPedidoCompraProductosAsync(EmpresaId, pedidoCompraId);
        return Json(new { success = true, data });
    }

    [HttpPost]
    public async Task<IActionResult> GuardarProducto([FromForm] PedidoCompraProductoGuardarSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.SavePedidoCompraProductoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model));
    }

    [HttpPost]
    public async Task<IActionResult> EliminarProducto(int id, int contextoId)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        if (contextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.EliminarPedidoCompraProductoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, id));
    }

    [HttpGet]
    public async Task<IActionResult> Exportar(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        try
        {
            var (titulo, contenido) = await service.ExportarPedidoCompraExcelAsync(EmpresaId, id);
            return File(contenido, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{titulo}.xlsx");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "No se encontró el requerimiento de compra." });
        }
    }
}

