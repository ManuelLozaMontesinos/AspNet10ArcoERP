using System.Globalization;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;

using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.IV.Controllers;

public abstract class InventarioReporteController(IInventarioService service, IRHService contextService, IAuthService authService, ILogger logger, string menuUrl, string reportType)
    : InventarioContextController(contextService, authService, logger, menuUrl)
{
    protected IInventarioService ReportService { get; } = service;
    [HttpGet]
    public async Task<IActionResult> Selector(string tipo, string? search = null, int page = 1, string? sort = null, string? dir = null)
    {
        if (!ModelState.IsValid || search?.Length > 150 || tipo is not ("producto" or "proveedor")) return InvalidInput();
        if (tipo == "proveedor" && reportType != "movimientos") return NotFound();
        return Json(new { success = true, data = await ReportService.GetReporteLookupAsync(EmpresaId, tipo, search, page, sort, dir) });
    }

    [HttpGet]
    public async Task<IActionResult> Movimiento(int id)
    {
        if (!ModelState.IsValid || id <= 0) return InvalidInput();
        if (reportType is not ("kardex" or "movimientos")) return NotFound();
        var detail = await ReportService.GetReporteMovimientoDetalleAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, id, reportType == "kardex");
        return detail is null ? NotFound(new { success = false, message = "No se encontró el movimiento en el ámbito habilitado." }) : Json(new { success = true, data = detail });
    }

    protected IActionResult Download<T>(InventarioReporteModel model, IEnumerable<T> rows, List<(string Label, Func<T, object?> Value)> columns, string filename, ISet<int>? precisionOcho = null)
    {
        if (model.Errores.Count > 0) return BadRequest(new { success = false, message = string.Join(" ", model.Errores) });
        return File(InformeExcel.Crear(model.Titulo,model.Contexto.Empresa,model.Resumen,rows,columns,precisionOcho),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",filename+".xlsx");
    }

    protected IActionResult PrintReport<T>(InventarioReporteModel model, IEnumerable<T> rows, List<(string Label, Func<T, object?> Value)> columns, ISet<int>? precisionOcho = null)
    {
        var print = new InventarioImpresionModel { Titulo = model.Titulo, Empresa = model.Contexto.Empresa, Resumen = model.Resumen, Errores = model.Errores, Columnas = columns.Select(c => c.Label).ToList() };
        if (model.Errores.Count == 0) print.Filas = rows.Select(row => columns.Select((c, i) => Format(c.Value(row), precisionOcho?.Contains(i + 1) == true)).ToList()).ToList();
        return View("~/Areas/IV/Views/Shared/InformeImprimir.cshtml", print);
    }

    private static string Format(object? value, bool precisionOcho) => value switch
    {
        DateTime date => date.ToString("dd/MM/yyyy"),
        decimal number => number.ToString(precisionOcho ? "N8" : "N4", CultureInfo.GetCultureInfo("es-PE")),
        _ => value?.ToString() ?? ""
    };
}
