using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.PD.Controllers;

[Area("PD")]
public class RequeInsumoController(
    IProduccionService service,
    IRHService context,
    IAuthService auth,
    ILogger<RequeInsumoController> logger)
    : ModuloContextController(context, auth, logger, "pd", "requeInsumo/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        string? codigo = null,
        string? cotizacion = null,
        string? cliente = null,
        string? vendedor = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? estadoDespachoId = null,
        bool mostrarAnulados = false,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
        => !ModelState.IsValid || search?.Length > 150
            ? InvalidInput()
            : View(await service.GetRequerimientosInsumoAsync(
                EmpresaId,
                PeriodoEmpresaSedeId,
                search,
                codigo,
                cotizacion,
                cliente,
                vendedor,
                fechaDesde,
                fechaHasta,
                estadoDespachoId,
                mostrarAnulados,
                page,
                top,
                sort,
                dir));

    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var data = await service.GetOrdenProduccionDetalleAsync(EmpresaId, PeriodoEmpresaSedeId, id);
        return data is null
            ? NotFound(new { success = false, message = "No se encontró el requerimiento de insumos." })
            : Json(new { success = true, data });
    }

    [HttpPost]
    public async Task<IActionResult> CerrarInsumo([FromBody] RequeInsumoCerrarSolicitud model)
    {
        if (!ModelState.IsValid || model.OrdenProduccionId <= 0) return InvalidInput();
        var (success, message) = await service.CerrarInsumoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model);
        return success ? Json(new { success = true, message }) : BadRequest(new { success = false, message });
    }
}

