using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.PD.Controllers;

[Area("PD")]
public class OrdenProduController(
    IProduccionService service,
    IRHService context,
    IAuthService auth,
    ILogger<OrdenProduController> logger)
    : ModuloContextController(context, auth, logger, "pd", "ordenProdu/index")
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
        int? estadoOrdenProduccionId = null,
        bool mostrarAnulados = false,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
        => !ModelState.IsValid || search?.Length > 150
            ? InvalidInput()
            : View(await service.GetOrdenesProduccionAsync(
                EmpresaId,
                PeriodoEmpresaSedeId,
                search,
                codigo,
                cotizacion,
                cliente,
                vendedor,
                fechaDesde,
                fechaHasta,
                estadoOrdenProduccionId,
                mostrarAnulados,
                page,
                top,
                sort,
                dir));

    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        if (!ModelState.IsValid || id < 0) return InvalidInput();
        var data = await service.GetOrdenProduccionDetalleAsync(EmpresaId, PeriodoEmpresaSedeId, id);
        return data is null
            ? NotFound(new { success = false, message = "No se encontró la orden de producción." })
            : Json(new { success = true, data });
    }

    [HttpPost]
    public async Task<IActionResult> Guardar([FromBody] OrdenProduccionGuardarSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId)
            return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });

        return Result(await service.SaveOrdenProduccionAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model));
    }

    [HttpPost]
    public async Task<IActionResult> Anular(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var (success, message) = await service.AnularOrdenProduccionAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, id);
        return success ? Json(new { success = true, message }) : BadRequest(new { success = false, message });
    }

    [HttpPost]
    public async Task<IActionResult> CambiarEstado([FromBody] OrdenProduccionCambiarEstadoSolicitud model)
    {
        if (!ModelState.IsValid || model.OrdenProduccionId <= 0) return InvalidInput();
        var (success, message) = await service.CambiarEstadoOrdenProduccionAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model);
        return success ? Json(new { success = true, message }) : BadRequest(new { success = false, message });
    }

    [HttpPost]
    public async Task<IActionResult> GuardarProducto([FromBody] OrdenProduccionProductoGuardarSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        return Result(await service.SaveOrdenProduccionProductoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model));
    }

    [HttpPost]
    public async Task<IActionResult> AnularProducto(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var (success, message) = await service.AnularOrdenProduccionProductoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, id);
        return success ? Json(new { success = true, message }) : BadRequest(new { success = false, message });
    }

    [HttpPost]
    public async Task<IActionResult> GuardarSubproceso([FromBody] OrdenProduccionSubprocesoGuardarSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        return Result(await service.SaveOrdenProduccionSubprocesoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model));
    }

    [HttpPost]
    public async Task<IActionResult> AnularSubproceso(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var (success, message) = await service.AnularOrdenProduccionSubprocesoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, id);
        return success ? Json(new { success = true, message }) : BadRequest(new { success = false, message });
    }
}
