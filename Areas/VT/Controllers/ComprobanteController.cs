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
public class ComprobanteController(
    IVentasService ventasService,
    IFacturacionElectronicaService feService,
    IRHService context,
    IAuthService auth,
    ILogger<ComprobanteController> logger)
    : ModuloContextController(context, auth, logger, "vt", "comprobante/index")
{
    public async Task<IActionResult> Index(
        string? search = null,
        int? tipoDocId = null,
        int? estadoComproId = null,
        int? estadoSunatId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = await ventasService.GetComprobantesAsync(
            EmpresaId, PeriodoEmpresaSedeId, search, tipoDocId, estadoComproId, estadoSunatId, fechaDesde, fechaHasta, page, top, sort, dir);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var detalle = await ventasService.GetComprobanteDetalleAsync(EmpresaId, id);
        if (detalle == null) return NotFound(new { message = "Comprobante no encontrado." });
        return Json(new { success = true, data = detalle });
    }

    [HttpGet]
    public async Task<IActionResult> Series(int tipoDocId)
    {
        var series = await ventasService.GetSeriesComprobanteAsync(PeriodoEmpresaSedeId, tipoDocId);
        return Json(new { success = true, data = series });
    }

    [HttpGet]
    public async Task<IActionResult> SiguienteNumero(int tipoDocId, string serie)
    {
        var sig = await ventasService.GetSiguienteNumeroComprobanteAsync(PeriodoEmpresaSedeId, tipoDocId, serie);
        return Json(new { success = true, numero = sig });
    }

    [HttpPost]
    public async Task<IActionResult> Guardar([FromBody] ComprobanteGuardarSolicitud model)
    {
        model.PeriodoEmpresaSedeId = PeriodoEmpresaSedeId;
        var (success, message, id) = await ventasService.SaveComprobanteAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model);
        return Json(new { success, message, id });
    }

    [HttpPost]
    public async Task<IActionResult> Emitir([FromBody] ComprobanteEmisionSolicitud model)
    {
        var (success, message, feResponse) = await ventasService.EmitirComprobanteAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model);
        return Json(new { success, message, feResponse });
    }

    [HttpPost]
    public async Task<IActionResult> Anular([FromBody] ComprobanteAnularSolicitud model)
    {
        var (success, message, id) = await ventasService.AnularComprobanteAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model.ComprobanteId, model.Motivo);
        return Json(new { success, message, id });
    }

    [HttpGet]
    public async Task<IActionResult> DescargarXml(int id, string tipoDoc = "01")
    {
        var xml = await feService.ObtenerXmlAsync(id, tipoDoc);
        if (string.IsNullOrEmpty(xml)) return NotFound("El archivo XML UBL no existe aún para este comprobante.");
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

public class ComprobanteAnularSolicitud
{
    public int ComprobanteId { get; set; }
    public string Motivo { get; set; } = "Anulado por el usuario";
}
