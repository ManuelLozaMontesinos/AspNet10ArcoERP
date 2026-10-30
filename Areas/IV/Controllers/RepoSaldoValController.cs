using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.IV.Controllers;

[Area("IV")]
public class RepoSaldoValController(IInventarioService service, IRHService contextService, IAuthService authService, ILogger<RepoSaldoValController> logger)
    : InventarioReporteController(service, contextService, authService, logger, "repoSaldoVal/index", "saldo-valorado")
{
    private static readonly HashSet<int> CostColumns = new() { 6, 11 };
    [HttpGet]
    public async Task<IActionResult> Index(InventarioReporteFiltro filtro, int page = 1, int top = 25, string? sort = null, string? dir = null)
        => !ModelState.IsValid ? InvalidInput() : View(await ReportService.GetSaldoValoradoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, filtro, page, top, sort, dir));

    [HttpGet]
    public async Task<IActionResult> Exportar(InventarioReporteFiltro filtro, string? sort = null, string? dir = null)
    {
        if (!ModelState.IsValid) return InvalidInput(); filtro.Consultar = true;
        var model = await ReportService.GetSaldoValoradoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, filtro, sort: sort, dir: dir, exportar: true);
        return Download(model, model.Items, Columns(), "saldo-valorizado-" + model.Contexto.Anio, CostColumns);
    }

    [HttpGet]
    public async Task<IActionResult> Imprimir(InventarioReporteFiltro filtro, string? sort = null, string? dir = null)
    {
        if (!ModelState.IsValid) return InvalidInput(); filtro.Consultar = true;
        var model = await ReportService.GetSaldoValoradoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, filtro, sort: sort, dir: dir, exportar: true);
        return PrintReport(model, model.Items, Columns(), CostColumns);
    }

    private static List<(string Label, Func<SaldoValoradoRowModel, object?> Value)> Columns() => new() {
        ("Sede",r=>r.Sede),("Código",r=>r.Codigo),("Producto",r=>r.Producto),("U.M.",r=>r.Unidad),("Anterior",r=>r.Anterior),
        ("Valor unitario anterior",r=>r.ValorUnitarioAnterior),("Ingresos",r=>r.Ingreso),("Salidas",r=>r.Salida),("Reserva",r=>r.Reserva),
        ("Saldo disponible",r=>r.Saldo),("Valor unitario",r=>r.ValorUnitario),("Importe del saldo",r=>r.Importe)
    };
}
