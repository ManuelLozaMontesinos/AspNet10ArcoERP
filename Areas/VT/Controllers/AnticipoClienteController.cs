using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class AnticipoClienteController(
    IVentasService service,
    IRHService context,
    IAuthService auth,
    ILogger<AnticipoClienteController> logger)
    : ModuloContextController(context, auth, logger, "vt", "anticipoCliente/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        int? clienteId = null,
        int? monedaId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        bool conSaldo = false,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
        => !ModelState.IsValid || search?.Length > 150
            ? InvalidInput()
            : View(await service.GetAnticiposClienteAsync(
                EmpresaId,
                PeriodoEmpresaSedeId,
                search,
                clienteId,
                monedaId,
                fechaDesde,
                fechaHasta,
                conSaldo,
                page,
                top,
                sort,
                dir));

    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var item = await service.GetAnticipoClienteAsync(EmpresaId, PeriodoEmpresaSedeId, id);
        return item is null ? NotFound(new { success = false, message = "Anticipo no encontrado." }) : Json(new { success = true, data = item });
    }

    [HttpPost]
    public async Task<IActionResult> Guardar([FromBody] AnticipoClienteGuardarSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        var (success, message, id) = await service.SaveAnticipoClienteAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model);
        return Json(new { success, message, id });
    }

    [HttpPost]
    public async Task<IActionResult> Extornar(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var (success, message) = await service.ExtornarAnticipoClienteAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, id);
        return Json(new { success, message });
    }
}

