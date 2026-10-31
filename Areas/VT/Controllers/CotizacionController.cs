using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class CotizacionController(IVentasService service, IRHService context, IAuthService auth, ILogger<CotizacionController> logger)
    : ModuloContextController(context, auth, logger, "vt", "cotizacion/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search = null, string? codigo = null, DateTime? fechaDesde = null, DateTime? fechaHasta = null, int? vendedorId = null, int? estadoCotizacionId = null, int page = 1, int top = 25, string? sort = null, string? dir = null)
        => !ModelState.IsValid || search?.Length > 150
            ? InvalidInput()
            : View(await service.GetCotizacionesAsync(EmpresaId, PeriodoEmpresaSedeId, search, codigo, fechaDesde, fechaHasta, vendedorId, estadoCotizacionId, page, top, sort, dir));

    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var model = await service.GetCotizacionDetalleAsync(EmpresaId, PeriodoEmpresaSedeId, id);
        return model is null ? NotFound(new { success = false, message = "No se encontró la cotización seleccionada." }) : Json(new { success = true, data = model });
    }

    [HttpGet]
    public async Task<IActionResult> BuscarClientes(string? search = null)
    {
        if (search?.Length > 150) return InvalidInput();
        var data = await service.BuscarClientesCotizacionAsync(EmpresaId, search);
        return Json(new { success = true, data });
    }

    [HttpGet]
    public async Task<IActionResult> BuscarProductos(int listaPrecioId, string? search = null)
    {
        if (listaPrecioId <= 0 || search?.Length > 150) return InvalidInput();
        var data = await service.BuscarProductosCotizacionAsync(listaPrecioId, search);
        return Json(new { success = true, data });
    }

    [HttpPost]
    public async Task<IActionResult> Guardar([FromForm] CotizacionGuardarSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.SaveCotizacionAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model));
    }

    [HttpPost]
    public async Task<IActionResult> Anular(int id, string revision, int contextoId)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        if (contextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.AnularCotizacionAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, id, revision));
    }

    [HttpGet]
    public async Task<IActionResult> Productos(int cotizacionId)
    {
        if (!ModelState.IsValid || cotizacionId <= 0) return InvalidInput();
        var data = await service.GetCotizacionProductosAsync(EmpresaId, cotizacionId);
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

    [HttpPost]
    public async Task<IActionResult> GenerarPedido([FromForm] GenerarPedidoSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.GenerarPedidoDesdeCotizacionAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model));
    }

    [HttpGet]
    public async Task<IActionResult> Exportar(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        try
        {
            var (titulo, contenido) = await service.ExportarCotizacionExcelAsync(EmpresaId, id);
            return File(contenido, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{titulo}.xlsx");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "No se encontró la cotización." });
        }
    }
}

