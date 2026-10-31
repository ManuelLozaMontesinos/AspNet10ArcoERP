using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.AS.Controllers;

[Area("AS")]
public class EmpresaController(
    IAdministracionService service,
    IRHService context,
    IAuthService auth,
    ILogger<EmpresaController> logger)
    : ModuloContextController(context, auth, logger, "as", "empresa/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        string? estado = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        if (!ModelState.IsValid || search?.Length > 150)
            return InvalidInput();

        var model = await service.GetEmpresasAsync(search, estado, page, top, sort, dir);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetDetalle(int id)
    {
        var detalle = await service.GetEmpresaDetalleAsync(id);
        if (detalle is null && id > 0)
            return NotFound(new { success = false, message = "Empresa no encontrada." });

        return Json(new { success = true, data = detalle });
    }

    [HttpGet]
    public async Task<IActionResult> GetProvincias(int departamentoId)
    {
        var provincias = await service.GetProvinciasPorDepartamentoAsync(departamentoId);
        return Json(provincias);
    }

    [HttpGet]
    public async Task<IActionResult> GetDistritos(int provinciaId)
    {
        var distritos = await service.GetDistritosPorProvinciaAsync(provinciaId);
        return Json(distritos);
    }

    [HttpPost]
    public async Task<IActionResult> Guardar([FromBody] EmpresaGuardarSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        var (success, message, id) = await service.SaveEmpresaAsync(UsuarioId, model);
        return success 
            ? Json(new { success = true, message, id }) 
            : BadRequest(new { success = false, message });
    }

    [HttpPost]
    public async Task<IActionResult> CambiarEstado(int id, string nuevoEstado)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var (success, message) = await service.CambiarEstadoEmpresaAsync(id, nuevoEstado);
        return success 
            ? Json(new { success = true, message }) 
            : BadRequest(new { success = false, message });
    }

    [HttpGet]
    public async Task<IActionResult> GetSedes(int empresaId)
    {
        if (empresaId <= 0) return InvalidInput();
        var sedes = await service.GetEmpresaSedesAsync(empresaId);
        return Json(new { success = true, data = sedes });
    }

    [HttpPost]
    public async Task<IActionResult> GuardarSede([FromBody] EmpresaSedeSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        var (success, message, id) = await service.SaveEmpresaSedeAsync(UsuarioId, model);
        return success 
            ? Json(new { success = true, message, id }) 
            : BadRequest(new { success = false, message });
    }

    [HttpPost]
    public async Task<IActionResult> CambiarEstadoSede(int id, string nuevoEstado)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var (success, message) = await service.CambiarEstadoEmpresaSedeAsync(id, nuevoEstado);
        return success 
            ? Json(new { success = true, message }) 
            : BadRequest(new { success = false, message });
    }

    [HttpGet]
    public async Task<IActionResult> GetPeriodoSedes(int empresaId, int periodoId)
    {
        if (empresaId <= 0) return InvalidInput();
        var asignaciones = await service.GetPeriodoSedesAsync(empresaId, periodoId);
        return Json(new { success = true, data = asignaciones });
    }

    [HttpPost]
    public async Task<IActionResult> AsignarPeriodoSede([FromBody] PeriodoSedeAsignarSolicitud model)
    {
        if (!ModelState.IsValid || model.PeriodoId <= 0 || model.EmpresaSedeId <= 0) return InvalidInput();
        var (success, message) = await service.AsignarSedePeriodoAsync(model.PeriodoId, model.EmpresaSedeId, UsuarioId);
        return success 
            ? Json(new { success = true, message }) 
            : BadRequest(new { success = false, message });
    }

    [HttpPost]
    public async Task<IActionResult> EliminarPeriodoSede(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var (success, message) = await service.EliminarSedePeriodoAsync(id);
        return success 
            ? Json(new { success = true, message }) 
            : BadRequest(new { success = false, message });
    }
}

