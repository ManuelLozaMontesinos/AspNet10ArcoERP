using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class RepoVentaPorClienteController(IVentasService service, IRHService context, IAuthService auth, ILogger<RepoVentaPorClienteController> logger)
    : VentaReporteController(service, context, auth, logger, "repoVentaPorCliente/index", "cliente") { }

