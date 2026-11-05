using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class GuiaremvtaController(
    IInventarioService service,
    IRHService context,
    IAuthService auth,
    ILogger<GuiaremvtaController> logger)
    : ModuloContextController(context, auth, logger, "vt", "guiaremvta/index")
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
            estadoGuiaId, estadoAprobacionId, motivoTrasladoId ?? 1, esInterno: false,
            page, top, sort, dir);

        return View(model);
    }
}
