using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.PD.Controllers;

[Area("PD")]
public class RutaProduccionController(
    IProduccionService service,
    IRHService context,
    IAuthService auth,
    ILogger<RutaProduccionController> logger)
    : ModuloContextController(context, auth, logger, "pd", "rutaProduccion/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        string? estado = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
        => !ModelState.IsValid || search?.Length > 150
            ? InvalidInput()
            : View(await service.GetRutasProduccionAsync(EmpresaId, PeriodoEmpresaSedeId, search, estado, page, top, sort, dir));

    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var data = await service.GetRutaProduccionDetalleAsync(EmpresaId, id);
        return data is null
            ? NotFound(new { success = false, message = "No se encontró la ruta de producción." })
            : Json(new { success = true, data });
    }

    [HttpGet]
    public async Task<IActionResult> SubprocesosDisponibles(int rutaId)
    {
        if (!ModelState.IsValid || rutaId <= 0) return InvalidInput();
        var list = await service.GetSubprocesosDisponiblesAsync(EmpresaId, rutaId);
        return Json(new { success = true, data = list });
    }

    [HttpPost]
    public async Task<IActionResult> Guardar([FromBody] RutaProduccionGuardarSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId)
            return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });

        return Result(await service.SaveRutaProduccionAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model));
    }

    [HttpPost]
    public async Task<IActionResult> Anular(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        return Result(await service.AnularRutaProduccionAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, id));
    }

    [HttpPost]
    public async Task<IActionResult> AgregarSubproceso([FromBody] RutaSubProcesoGuardarSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId)
            return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });

        return Result(await service.AgregarSubprocesoRutaAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model));
    }

    [HttpPost]
    public async Task<IActionResult> ActualizarOrden([FromBody] RutaSubProcesoOrdenSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId)
            return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });

        return Result(await service.ActualizarOrdenSubprocesoRutaAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model));
    }

    [HttpPost]
    public async Task<IActionResult> QuitarSubproceso(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        return Result(await service.QuitarSubprocesoRutaAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, id));
    }
}

