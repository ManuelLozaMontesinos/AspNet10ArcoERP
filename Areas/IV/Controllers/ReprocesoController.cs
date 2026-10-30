using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.IV.Controllers;

[Area("IV")]
public class ReprocesoController(IInventarioService service, IRHService contextService, IAuthService authService, ILogger<ReprocesoController> logger)
    : InventarioProcesoController(service, contextService, authService, logger, "reproceso/index", "reproceso")
{
    [HttpPost]
    public async Task<IActionResult> Reprocesar(InventarioProcesoSolicitud model)
        => ModelState.IsValid ? Result(await ProcessService.EjecutarProcesoInventarioAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, "reproceso", model)) : InvalidInput();

    [HttpPost]
    public async Task<IActionResult> ReprocesoAnual(InventarioProcesoSolicitud model)
        => ModelState.IsValid ? Result(await ProcessService.EjecutarProcesoInventarioAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, "anual", model)) : InvalidInput();
}
