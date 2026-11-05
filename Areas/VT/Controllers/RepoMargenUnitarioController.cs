using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class RepoMargenUnitarioController(IVentasService service, IRHService context, IAuthService auth, ILogger<RepoMargenUnitarioController> logger)
    : VentaReporteController(service, context, auth, logger, "repoMargenUnitario/index", "margen") { }

