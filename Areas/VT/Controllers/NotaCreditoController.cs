using System;
using System.Text;
using System.Threading.Tasks;
using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class NotaCreditoController(
    IVentasService ventasService,
    IFacturacionElectronicaService feService,
    IRHService context,
    IAuthService auth,
    ILogger<NotaCreditoController> logger)
    : ModuloContextController(context, auth, logger, "vt", "notacredito/index")
{
    public async Task<IActionResult> Index(
        string? search = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = await ventasService.GetNotasCreditoAsync(
            EmpresaId, PeriodoEmpresaSedeId, search, fechaDesde, fechaHasta, page, top, sort, dir);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Guardar([FromBody] NotaCreditoGuardarSolicitud model)
    {
        model.PeriodoEmpresaSedeId = PeriodoEmpresaSedeId;
        var (success, message, id) = await ventasService.SaveNotaCreditoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model);
        return Json(new { success, message, id });
    }

    [HttpGet]
    public async Task<IActionResult> DescargarXml(int id)
    {
        var xml = await feService.ObtenerXmlAsync(id, "07");
        if (string.IsNullOrEmpty(xml)) return NotFound("El archivo XML no existe aún para esta Nota de Crédito.");
        return File(Encoding.UTF8.GetBytes(xml), "application/xml", $"NC-{id}.xml");
    }

    [HttpGet]
    public async Task<IActionResult> DescargarCdr(int id)
    {
        var cdr = await feService.ObtenerCdrAsync(id, "07");
        if (string.IsNullOrEmpty(cdr)) return NotFound("La constancia CDR de SUNAT no existe aún.");
        return File(Encoding.UTF8.GetBytes(cdr), "application/xml", $"CDR-NC-{id}.xml");
    }
}
