using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.CM.Controllers;

[Area("CM")]
public class ValorizaCompraController(IComprasService service, IRHService context, IAuthService auth, ILogger<ValorizaCompraController> logger)
    : ModuloContextController(context, auth, logger, "cm", "ValorizaCompra/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        string? ordenCompraCodigo = null,
        string? serie = null,
        string? numero = null,
        int? tipoDocumentoComercialId = null,
        int? condicionPagoId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
        => !ModelState.IsValid || search?.Length > 150
            ? InvalidInput()
            : View(await service.GetValorizacionesCompraAsync(EmpresaId, PeriodoEmpresaSedeId, search, ordenCompraCodigo, serie, numero, tipoDocumentoComercialId, condicionPagoId, fechaDesde, fechaHasta, page, top, sort, dir));

    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        var model = await service.GetValorizacionCompraDetalleAsync(EmpresaId, PeriodoEmpresaSedeId, id);
        return model is null ? NotFound(new { success = false, message = "No se encontró el comprobante." }) : Json(new { success = true, data = model });
    }

    [HttpGet]
    public async Task<IActionResult> BuscarOrdenes(string? q = null)
    {
        if (!ModelState.IsValid) return InvalidInput();
        var data = await service.BuscarOrdenesCompraParaComprobanteAsync(EmpresaId, PeriodoEmpresaSedeId, q);
        return Json(new { success = true, data });
    }

    [HttpPost]
    public async Task<IActionResult> Guardar([FromForm] ValorizaCompraGuardarSolicitud model)
    {
        if (!ModelState.IsValid) return InvalidInput();
        if (model.ContextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.SaveValorizacionCompraAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, model));
    }

    [HttpPost]
    public async Task<IActionResult> Eliminar(int id, int contextoId)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        if (contextoId != PeriodoEmpresaSedeId) return Conflict(new { success = false, message = "El contexto de trabajo cambió. Actualice el listado." });
        return Result(await service.EliminarValorizacionCompraAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, id));
    }
}
