using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.IV.Controllers;

[Area("IV")]
public class GuiaRemiController(
    IInventarioService service,
    IRHService context,
    IAuthService auth,
    ILogger<GuiaRemiController> logger)
    : InventarioContextController(context, auth, logger, "guiaremi/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(
        int? almacenId = null,
        string? codigo = null,
        string? search = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? estadoGuiaId = null,
        int? estadoAprobacionId = null,
        int? motivoTrasladoId = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        if (!ModelState.IsValid || search?.Length > 150 || codigo?.Length > 30)
            return InvalidInput();

        var model = await service.GetGuiasRemisionAsync(
            EmpresaId, PeriodoEmpresaSedeId, almacenId, codigo, search, fechaDesde, fechaHasta,
            estadoGuiaId, estadoAprobacionId, motivoTrasladoId, esInterno: true,
            page, top, sort, dir);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetDetalle(int id)
    {
        var detalle = await service.GetGuiaRemisionDetalleAsync(EmpresaId, PeriodoEmpresaSedeId, id, esInterno: true);
        if (detalle is null && id > 0)
            return NotFound(new { success = false, message = "Guía de remisión no encontrada." });

        return Json(new { success = true, data = detalle });
    }

    [HttpGet]
    public async Task<IActionResult> GetSeries(int almacenId)
    {
        if (almacenId <= 0) return InvalidInput();
        var series = await service.GetSeriesGuiaPorAlmacenAsync(almacenId);
        return Json(series);
    }

    [HttpGet]
    public async Task<IActionResult> GetConductoresYUnidades(int transportistaId)
    {
        if (transportistaId <= 0) return InvalidInput();
        var conductores = await service.GetConductoresPorTransportistaAsync(transportistaId);
        var unidades = await service.GetUnidadesPorTransportistaAsync(transportistaId);
        return Json(new { conductores, unidades });
    }

    [HttpGet]
    public async Task<IActionResult> BuscarMovimientos(int almacenId, string? search = null)
    {
        if (almacenId <= 0) return InvalidInput();
        var movimientos = await service.BuscarMovimientosParaGuiaAsync(EmpresaId, PeriodoEmpresaSedeId, almacenId, search);
        return Json(movimientos);
    }

    [HttpGet]
    public async Task<IActionResult> GetProductosMovimiento(int movimientoAlmacenId)
    {
        if (movimientoAlmacenId <= 0) return InvalidInput();
        var productos = await service.GetProductosDeMovimientoParaGuiaAsync(movimientoAlmacenId);
        return Json(new { success = true, data = productos });
    }

    [HttpPost]
    public async Task<IActionResult> Guardar([FromBody] GuiaRemiGuardarSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        var (success, message, id) = await service.SaveGuiaRemisionAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model);
        return success 
            ? Json(new { success = true, message, id }) 
            : BadRequest(new { success = false, message });
    }

    [HttpPost]
    public async Task<IActionResult> Anular([FromBody] GuiaRemiAnularSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        var (success, message, id) = await service.AnularGuiaRemisionAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model);
        return success 
            ? Json(new { success = true, message, id }) 
            : BadRequest(new { success = false, message });
    }

    [HttpPost]
    public async Task<IActionResult> SolicitarAprobacion(int id, string revision)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var (success, message, resultId) = await service.SolicitarAprobacionGuiaRemisionAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, id, revision);
        return success 
            ? Json(new { success = true, message, id = resultId }) 
            : BadRequest(new { success = false, message });
    }
}
