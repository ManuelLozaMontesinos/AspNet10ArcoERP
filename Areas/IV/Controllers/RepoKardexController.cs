using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.IV.Controllers;

[Area("IV")]
public class RepoKardexController(IInventarioService service, IRHService contextService, IAuthService authService, ILogger<RepoKardexController> logger)
    : InventarioReporteController(service, contextService, authService, logger, "repoKardex/index", "kardex")
{
    [HttpGet]
    public async Task<IActionResult> Index(InventarioReporteFiltro filtro, int page = 1, int top = 25, string? sort = null, string? dir = null)
        => !ModelState.IsValid ? InvalidInput() : View(await ReportService.GetKardexUnidadesAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, filtro, page, top, sort, dir));

    [HttpGet]
    public async Task<IActionResult> Exportar(InventarioReporteFiltro filtro, string? sort = null, string? dir = null)
    {
        if (!ModelState.IsValid) return InvalidInput(); filtro.Consultar = true;
        var model = await ReportService.GetKardexUnidadesAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, filtro, sort: sort, dir: dir, exportar: true);
        return Download(model, model.Items, Columns(), "kardex-unidades-" + model.Contexto.Anio);
    }

    [HttpGet]
    public async Task<IActionResult> Imprimir(InventarioReporteFiltro filtro, string? sort = null, string? dir = null)
    {
        if (!ModelState.IsValid) return InvalidInput(); filtro.Consultar = true;
        var model = await ReportService.GetKardexUnidadesAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, filtro, sort: sort, dir: dir, exportar: true);
        return PrintReport(model, model.Items, Columns());
    }

    private static List<(string Label, Func<KardexUnidadRowModel, object?> Value)> Columns() => new() {
        ("Código", r=>r.Codigo), ("Producto", r=>r.Producto), ("U.M.", r=>r.Unidad), ("Fecha", r=>r.Fecha), ("Operación", r=>r.Operacion),
        ("Tipo", r=>r.TipoMovimiento), ("Transacción", r=>r.Transaccion), ("Guía", r=>r.Guia), ("Documento", r=>r.Documento),
        ("Detalle", r=>r.Descripcion), ("Ingreso", r=>r.Ingreso), ("Salida", r=>r.Salida), ("Saldo acumulado", r=>r.Saldo)
    };
}
