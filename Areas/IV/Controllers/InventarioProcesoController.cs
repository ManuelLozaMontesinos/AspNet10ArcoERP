using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.IV.Controllers;

public abstract class InventarioProcesoController(IInventarioService service, IRHService contextService, IAuthService authService, ILogger logger, string menuUrl, string tipo)
    : InventarioContextController(contextService, authService, logger, menuUrl)
{
    protected IInventarioService ProcessService { get; } = service;

    [HttpGet]
    public async Task<IActionResult> Index(int? almacenId = null, string? search = null, string? estado = null, int page = 1, int top = 25, string? sort = null, string? dir = null)
    {
        if (!ModelState.IsValid || almacenId is <= 0 || search?.Length > 150) return InvalidInput();
        var model = await ProcessService.GetProcesosInventarioAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, almacenId, search, estado, page, top, sort, dir);
        model.Tipo = tipo; return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Preparar(string operacion, int almacenId, int? mesContableId = null)
    {
        if (!ModelState.IsValid || almacenId <= 0 || mesContableId is <= 0 ||
            (tipo == "cierre" ? operacion is not ("apertura" or "cierre") : operacion is not ("reproceso" or "anual"))) return InvalidInput();
        var model = await ProcessService.GetPreparacionProcesoAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, operacion, almacenId, mesContableId);
        return model.Errores.Count > 0 ? BadRequest(new { success = false, message = string.Join(" ", model.Errores) }) : Json(new { success = true, data = model });
    }
}
