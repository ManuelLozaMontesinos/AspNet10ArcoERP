using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.IV.Controllers;

[Area("IV")]
public class ApeCierreController(IInventarioService service, IRHService contextService, IAuthService authService, ILogger<ApeCierreController> logger)
    : InventarioProcesoController(service, contextService, authService, logger, "apeCierre/index", "cierre")
{
    [HttpPost]
    public async Task<IActionResult> Aperturar(InventarioProcesoSolicitud model)
        => ModelState.IsValid ? Result(await ProcessService.EjecutarProcesoInventarioAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, "apertura", model)) : InvalidInput();

    [HttpPost]
    public async Task<IActionResult> Cerrar(InventarioProcesoSolicitud model)
        => ModelState.IsValid ? Result(await ProcessService.EjecutarProcesoInventarioAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, "cierre", model)) : InvalidInput();
}
