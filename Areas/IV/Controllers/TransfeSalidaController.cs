using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.IV.Controllers;

[Area("IV")]
public class TransfeSalidaController(IInventarioService service, IRHService context, IAuthService auth, ILogger<TransfeSalidaController> logger)
    : InventarioContextController(context, auth, logger, "transfeSalida/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        string? codigo = null,
        int? almacenId = null,
        int? almacenDestinoId = null,
        int? estadoMovimientoAlmacenId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
        => !ModelState.IsValid || search?.Length > 150
            ? InvalidInput()
            : View(await service.GetTransferenciasSalidaAsync(EmpresaId, PeriodoEmpresaSedeId, search, codigo, almacenId, almacenDestinoId, estadoMovimientoAlmacenId, fechaDesde, fechaHasta, page, top, sort, dir));

    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var model = await service.GetTransferenciaSalidaDetalleAsync(EmpresaId, PeriodoEmpresaSedeId, id);
        return model is null ? NotFound(new { success = false, message = "No se encontró la transferencia." }) : Json(new { success = true, data = model });
    }

    [HttpPost]
    public async Task<IActionResult> Guardar([FromForm] TransfeSalidaGuardarSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.SaveTransferenciaSalidaAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model));
    }

    [HttpPost]
    public async Task<IActionResult> Anular(int id, string revision, int contextoId)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        if (contextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.AnularTransferenciaSalidaAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, id, revision));
    }

    [HttpGet]
    public async Task<IActionResult> Productos(int movimientoAlmacenId)
    {
        if (!ModelState.IsValid || movimientoAlmacenId <= 0) return InvalidInput();
        var data = await service.GetMovimientoAlmacenProductosAsync(EmpresaId, movimientoAlmacenId);
        return Json(new { success = true, data });
    }

    [HttpPost]
    public async Task<IActionResult> GuardarProducto([FromForm] MovAlmacenProductoGuardarSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.SaveMovimientoAlmacenProductoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model));
    }

    [HttpPost]
    public async Task<IActionResult> EliminarProducto(int id, int contextoId)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        if (contextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.EliminarMovimientoAlmacenProductoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, id));
    }

    [HttpGet]
    public async Task<IActionResult> BuscarProductos(string? q = null)
    {
        if (!ModelState.IsValid) return InvalidInput();
        var data = await service.BuscarProductosAlmacenAsync(EmpresaId, q);
        return Json(new { success = true, data });
    }

    [HttpGet]
    public async Task<IActionResult> Exportar(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        try
        {
            var (titulo, contenido) = await service.ExportarMovimientoAlmacenExcelAsync(EmpresaId, id);
            return File(contenido, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{titulo}.xlsx");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "No se encontró la transferencia." });
        }
    }
}

