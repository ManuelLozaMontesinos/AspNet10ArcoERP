using System;
using System.Threading.Tasks;
using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AspNet10ArcoERP.Areas.FE.Controllers;

[Area("FE")]
public class ResumenDiarioController(
    IVentasService ventasService,
    IFacturacionElectronicaService feService,
    IRHService context,
    IAuthService auth,
    ILogger<ResumenDiarioController> logger)
    : ModuloContextController(context, auth, logger, "fe", "resumendiario/index")
{
    public async Task<IActionResult> Index(DateTime? fecha = null, int page = 1, int top = 25)
    {
        DateTime fechaConsulta = fecha ?? DateTime.Today;
        var model = await ventasService.GetResumenDiarioAsync(EmpresaId, fechaConsulta, page, top);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> GenerarEnviar([FromBody] ResumenDiarioSolicitud model)
    {
        var response = await feService.GenerarResumenDiarioAsync(EmpresaId, model.Fecha, UsuarioId);
        return Json(response);
    }
}

public class ResumenDiarioSolicitud
{
    public DateTime Fecha { get; set; } = DateTime.Today;
}
