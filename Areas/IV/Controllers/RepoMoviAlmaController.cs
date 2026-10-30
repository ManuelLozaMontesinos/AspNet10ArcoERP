using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.IV.Controllers;

[Area("IV")]
public class RepoMoviAlmaController(IInventarioService service, IRHService contextService, IAuthService authService, ILogger<RepoMoviAlmaController> logger)
    : InventarioReporteController(service, contextService, authService, logger, "repoMoviAlma/index", "movimientos")
{
    [HttpGet]
    public async Task<IActionResult> Index(InventarioReporteFiltro filtro, int page = 1, int top = 25, string? sort = null, string? dir = null)
        => !ModelState.IsValid ? InvalidInput() : View(await ReportService.GetReporteMovimientosAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, filtro, page, top, sort, dir));

    [HttpGet]
    public async Task<IActionResult> Exportar(InventarioReporteFiltro filtro, string? sort = null, string? dir = null)
    {
        if (!ModelState.IsValid) return InvalidInput(); filtro.Consultar = true;
        var model = await ReportService.GetReporteMovimientosAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, filtro, sort: sort, dir: dir, exportar: true);
        return Download(model, model.Items, Columns(), "movimientos-" + model.Contexto.Anio);
    }

    [HttpGet]
    public async Task<IActionResult> Imprimir(InventarioReporteFiltro filtro, string? sort = null, string? dir = null)
    {
        if (!ModelState.IsValid) return InvalidInput(); filtro.Consultar = true;
        var model = await ReportService.GetReporteMovimientosAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, filtro, sort: sort, dir: dir, exportar: true);
        return PrintReport(model, model.Items, Columns());
    }

    private static List<(string Label, Func<MovimientoReporteRowModel, object?> Value)> Columns() => new() {
        ("Fecha", r=>r.Fecha), ("Operación", r=>r.Operacion), ("Solicitante", r=>r.Solicitante), ("Tipo", r=>r.TipoMovimiento),
        ("Orden de compra", r=>r.OrdenCompra), ("Tercero", r=>r.Tercero), ("Guía", r=>r.Guia), ("Documento", r=>r.Documento),
        ("Observación", r=>r.Observacion), ("Código", r=>r.Codigo), ("Producto", r=>r.Producto), ("U.M.", r=>r.Unidad),
        ("Detalle", r=>r.Descripcion), ("Cantidad", r=>r.Cantidad), ("Valor unitario de ingreso", r=>r.ValorUnitario)
    };
}
