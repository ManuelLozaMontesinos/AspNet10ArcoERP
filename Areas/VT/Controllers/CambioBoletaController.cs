using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class CambioBoletaController(
    IVentasService service,
    IRHService context,
    IAuthService auth,
    ILogger<CambioBoletaController> logger)
    : ModuloContextController(context, auth, logger, "vt", "cambioBoleta/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        int? tipoComprobanteId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
        => !ModelState.IsValid || search?.Length > 150
            ? InvalidInput()
            : View(await service.GetBoletasParaCambioAsync(
                EmpresaId,
                PeriodoEmpresaSedeId,
                search,
                tipoComprobanteId,
                fechaDesde,
                fechaHasta,
                page,
                top,
                sort,
                dir));

    [HttpPost]
    public async Task<IActionResult> CambiarTipo(int comprobanteId, int nuevoTipoId)
    {
        if (!ModelState.IsValid || comprobanteId <= 0 || nuevoTipoId <= 0) return InvalidInput();
        var (success, message) = await service.CambiarTipoComprobanteAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, comprobanteId, nuevoTipoId);
        return Json(new { success, message });
    }
}

