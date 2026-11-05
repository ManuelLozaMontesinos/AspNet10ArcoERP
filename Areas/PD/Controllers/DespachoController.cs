using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.PD.Controllers;

[Area("PD")]
public class DespachoController(
    IProduccionService service,
    IRHService context,
    IAuthService auth,
    ILogger<DespachoController> logger)
    : ModuloContextController(context, auth, logger, "pd", "despacho/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        string? codigo = null,
        string? cotizacion = null,
        string? cliente = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? estadoDespachoId = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
        => !ModelState.IsValid || search?.Length > 150
            ? InvalidInput()
            : View(await service.GetDespachosAsync(
                EmpresaId,
                PeriodoEmpresaSedeId,
                search,
                codigo,
                cotizacion,
                cliente,
                fechaDesde,
                fechaHasta,
                estadoDespachoId,
                page,
                top,
                sort,
                dir));

    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var data = await service.GetDespachoDetalleAsync(EmpresaId, PeriodoEmpresaSedeId, id);
        return data is null
            ? NotFound(new { success = false, message = "No se encontró el comprobante para despacho." })
            : Json(new { success = true, data });
    }

    [HttpPost]
    public async Task<IActionResult> EntregarItem([FromBody] DespachoEntregaSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        var (success, message) = await service.EntregarItemDespachoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model);
        return Json(new { success, message });
    }

    [HttpPost]
    public async Task<IActionResult> Cerrar([FromBody] DespachoCerrarSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        var (success, message) = await service.CerrarDespachoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model);
        return Json(new { success, message });
    }
}

