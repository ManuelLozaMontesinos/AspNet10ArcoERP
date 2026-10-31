using System;
using System.Text;
using System.Threading.Tasks;
using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AspNet10ArcoERP.Areas.FE.Controllers;

[Area("FE")]
public class DocEmitidoController(
    IVentasService ventasService,
    IFacturacionElectronicaService feService,
    IRHService context,
    IAuthService auth,
    ILogger<DocEmitidoController> logger)
    : ModuloContextController(context, auth, logger, "fe", "docemitido/index")
{
    public async Task<IActionResult> Index(
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? tipoDocId = null,
        string? estadoDoc = null,
        string? search = null,
        int page = 1,
        int top = 25)
    {
        var model = await ventasService.GetDocumentosEmitidosAsync(
            EmpresaId, PeriodoEmpresaSedeId, fechaDesde, fechaHasta, tipoDocId, estadoDoc, search, page, top);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Enviar([FromBody] EnviarDocumentoRequest model)
    {
        var response = await feService.EnviarDocumentoAsync(model.ComprobanteId, model.TipoDocumento, UsuarioId);
        return Json(response);
    }

    [HttpPost]
    public async Task<IActionResult> GenerarXml([FromBody] EnviarDocumentoRequest model)
    {
        var response = await feService.GenerarEnviarDocumentoAsync(model.ComprobanteId, model.TipoDocumento, "N", UsuarioId);
        return Json(response);
    }

    [HttpPost]
    public async Task<IActionResult> ActualizarCdr([FromBody] EnviarDocumentoRequest model)
    {
        var response = await feService.ActualizarCdrAsync(model.ComprobanteId, model.TipoDocumento, UsuarioId);
        return Json(response);
    }

    [HttpGet]
    public async Task<IActionResult> DescargarXml(int id, string tipoDoc = "01")
    {
        var xml = await feService.ObtenerXmlAsync(id, tipoDoc);
        if (string.IsNullOrEmpty(xml)) return NotFound("El archivo XML UBL no existe aún.");
        return File(Encoding.UTF8.GetBytes(xml), "application/xml", $"CPE-{id}.xml");
    }

    [HttpGet]
    public async Task<IActionResult> DescargarCdr(int id, string tipoDoc = "01")
    {
        var cdr = await feService.ObtenerCdrAsync(id, tipoDoc);
        if (string.IsNullOrEmpty(cdr)) return NotFound("La constancia CDR de SUNAT no existe aún.");
        return File(Encoding.UTF8.GetBytes(cdr), "application/xml", $"CDR-{id}.xml");
    }
}
