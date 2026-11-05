using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class RepoVentaPorVendController(IVentasService service, IRHService context, IAuthService auth, ILogger<RepoVentaPorVendController> logger)
    : VentaReporteController(service, context, auth, logger, "repoVentaPorVend/index", "vendedor") { }

