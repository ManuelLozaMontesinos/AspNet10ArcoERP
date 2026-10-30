using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.IV.Controllers;

[Area("IV")]
public class ConsultaLlegadaController(IInventarioService service, IRHService contextService, IAuthService authService, ILogger<ConsultaLlegadaController> logger)
    : InventarioContextController(contextService, authService, logger, "consultaLlegada/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(LlegadaCompraFiltro filtro, int page = 1, int top = 25, string? sort = null, string? dir = null)
        => !ModelState.IsValid ? InvalidInput() : View(await service.GetLlegadasCompraAsync(EmpresaId, PeriodoEmpresaSedeId, UsuarioId, filtro, page, top, sort, dir));
}
