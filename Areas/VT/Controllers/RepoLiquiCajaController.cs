using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class RepoLiquiCajaController(IVentasService service,IRHService context,IAuthService auth,ILogger<RepoLiquiCajaController> logger)
    : VentaReporteController(service,context,auth,logger,"repoLiquiCaja/index","caja") { }
