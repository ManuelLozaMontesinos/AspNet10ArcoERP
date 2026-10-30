using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.IV.Controllers;

[Area("IV")]
public class ConsultaSaldoController(IInventarioService service, IRHService contextService, IAuthService authService, ILogger<ConsultaSaldoController> logger)
    : InventarioReporteController(service, contextService, authService, logger, "consultaSaldo/index", "saldos")
{
    [HttpGet]
    public async Task<IActionResult> Index(InventarioReporteFiltro filtro, int page = 1, int top = 25, string? sort = null, string? dir = null)
        => !ModelState.IsValid ? InvalidInput() : View(await ReportService.GetConsultaSaldosAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, filtro, page, top, sort, dir));

    [HttpGet]
    public async Task<IActionResult> Detalle(int productoId, int mes)
    {
        if (!ModelState.IsValid || productoId <= 0 || mes is < 1 or > 12) return InvalidInput();
        var items = await ReportService.GetSaldoDetalleAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, mes, productoId);
        return items is null ? NotFound(new { success = false, message = "No se encontró el producto en el ámbito habilitado." }) : Json(new { success = true, data = items });
    }

    [HttpGet]
    public async Task<IActionResult> Exportar(InventarioReporteFiltro filtro, string? sort = null, string? dir = null)
    {
        if (!ModelState.IsValid) return InvalidInput();
        var model = await ReportService.GetConsultaSaldosAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, filtro, sort: sort, dir: dir, exportar: true);
        return Download(model, model.Items, Columns(model), "saldos-" + model.Contexto.Anio);
    }

    [HttpGet]
    public async Task<IActionResult> Imprimir(InventarioReporteFiltro filtro, string? sort = null, string? dir = null)
    {
        if (!ModelState.IsValid) return InvalidInput();
        var model = await ReportService.GetConsultaSaldosAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, filtro, sort: sort, dir: dir, exportar: true);
        return PrintReport(model, model.Items, Columns(model));
    }

    private static List<(string Label, Func<SaldoConsultaRowModel, object?> Value)> Columns(SaldoConsultaPageModel model)
    {
        var columns = new List<(string Label, Func<SaldoConsultaRowModel, object?> Value)> {
            ("Código", r=>r.Codigo), ("Descripción", r=>r.Descripcion), ("Referencia", r=>r.Referencia), ("U.M.", r=>r.Unidad),
            ("Físico", r=>r.Fisico), ("Reserva", r=>r.Reserva), ("Disponible", r=>r.Disponible)
        };
        foreach (var warehouse in model.Almacenes) columns.Add((warehouse.Nombre, r => r.PorAlmacen.GetValueOrDefault(warehouse.Id)));
        return columns;
    }
}
