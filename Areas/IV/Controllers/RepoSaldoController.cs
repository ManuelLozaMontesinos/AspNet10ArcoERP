using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.IV.Controllers;

[Area("IV")]
public class RepoSaldoController(IInventarioService service, IRHService contextService, IAuthService authService, ILogger<RepoSaldoController> logger)
    : InventarioReporteController(service, contextService, authService, logger, "repoSaldo/index", "saldo-unidades")
{
    [HttpGet]
    public async Task<IActionResult> Index(InventarioReporteFiltro filtro, int page = 1, int top = 25, string? sort = null, string? dir = null)
        => !ModelState.IsValid ? InvalidInput() : View(await ReportService.GetSaldoUnidadesAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, filtro, page, top, sort, dir));

    [HttpGet]
    public async Task<IActionResult> Detalle(InventarioReporteFiltro filtro, int productoId)
    {
        if (!ModelState.IsValid || productoId <= 0) return InvalidInput();
        var rows = await ReportService.GetSaldoUnidadesDetalleAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, filtro, productoId);
        return rows is null ? NotFound(new { success = false, message = "No se encontró el producto en el ámbito de la consulta." }) : Json(new { success = true, data = rows });
    }

    [HttpGet]
    public async Task<IActionResult> Exportar(InventarioReporteFiltro filtro, string? sort = null, string? dir = null)
    {
        if (!ModelState.IsValid) return InvalidInput(); filtro.Consultar = true;
        var model = await ReportService.GetSaldoUnidadesAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, filtro, sort: sort, dir: dir, exportar: true);
        return Download(model, model.Items, Columns(model), "saldo-unidades-" + model.Contexto.Anio);
    }

    [HttpGet]
    public async Task<IActionResult> Imprimir(InventarioReporteFiltro filtro, string? sort = null, string? dir = null)
    {
        if (!ModelState.IsValid) return InvalidInput(); filtro.Consultar = true;
        var model = await ReportService.GetSaldoUnidadesAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, filtro, sort: sort, dir: dir, exportar: true);
        return PrintReport(model, model.Items, Columns(model));
    }

    private static List<(string Label, Func<SaldoUnidadesRowModel, object?> Value)> Columns(SaldoUnidadesPageModel model)
    {
        var columns = new List<(string Label, Func<SaldoUnidadesRowModel, object?> Value)> { ("Código",r=>r.Codigo),("Producto",r=>r.Producto),("U.M.",r=>r.Unidad) };
        if (model.Filtro.Variante == "almacenes") foreach (var warehouse in model.ColumnasAlmacen) columns.Add((warehouse.Nombre,r=>r.PorAlmacen.GetValueOrDefault(warehouse.Id)));
        else { columns.Add(("Anterior",r=>r.Anterior)); columns.Add(("Ingresos",r=>r.Ingreso)); columns.Add(("Salidas",r=>r.Salida)); }
        columns.Add(("Saldo",r=>r.Saldo)); return columns;
    }
}
