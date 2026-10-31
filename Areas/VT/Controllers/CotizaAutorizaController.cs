using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class CotizaAutorizaController(IVentasService service, IRHService context, IAuthService auth, ILogger<CotizaAutorizaController> logger)
    : ModuloContextController(context, auth, logger, "vt", "cotizaAutoriza/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        string? codigo = null,
        string? cliente = null,
        string? vendedor = null,
        int? estadoAprobacionId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
        => !ModelState.IsValid || search?.Length > 150
            ? InvalidInput()
            : View(await service.GetCotizacionesAutorizarAsync(
                EmpresaId,
                PeriodoEmpresaSedeId,
                search,
                codigo,
                cliente,
                vendedor,
                estadoAprobacionId,
                fechaDesde,
                fechaHasta,
                page,
                top,
                sort,
                dir));

    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var model = await service.GetCotizacionAutorizacionDetalleAsync(EmpresaId, id);
        return model is null
            ? NotFound(new { success = false, message = "No se encontró la solicitud de autorización." })
            : Json(new { success = true, data = model });
    }

    [HttpPost]
    public async Task<IActionResult> Aprobar([FromBody] AprobarCotizacionAutorizacionSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId)
            return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });

        return Result(await service.AprobarCotizacionAutorizacionAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model));
    }

    [HttpPost]
    public async Task<IActionResult> Desaprobar([FromBody] DesaprobarCotizacionAutorizacionSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId)
            return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });

        return Result(await service.DesaprobarCotizacionAutorizacionAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model));
    }
}

