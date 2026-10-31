using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.IV.Controllers;

[Area("IV")]
public class ValorizaController(IInventarioService service, IRHService contextService, IAuthService authService, ILogger<ValorizaController> logger)
    : InventarioProcesoController(service, contextService, authService, logger, "valoriza/index", "valorizacion")
{
    [HttpPost]
    public async Task<IActionResult> Valorizar(InventarioProcesoSolicitud model)
        => ModelState.IsValid ? Result(await ProcessService.EjecutarProcesoInventarioAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, "valorizar", model)) : InvalidInput();
}

