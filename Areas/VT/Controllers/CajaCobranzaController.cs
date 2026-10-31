using System;
using System.Threading.Tasks;
using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class CajaCobranzaController(
    IVentasService ventasService,
    IRHService context,
    IAuthService auth,
    ILogger<CajaCobranzaController> logger)
    : ModuloContextController(context, auth, logger, "vt", "cajacobranza/index")
{
    public async Task<IActionResult> Index(int? cajaId = null, DateTime? fecha = null, string? search = null, int page = 1, int top = 25)
    {
        var model = await ventasService.GetCajaCobranzaAsync(EmpresaId, PeriodoEmpresaSedeId, cajaId, fecha, search, page, top);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Pagar([FromBody] CajaCobranzaPagarSolicitud model)
    {
        var (success, message, feResponse) = await ventasService.PagarCajaCobranzaAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model);
        return Json(new { success, message, feResponse });
    }
}
