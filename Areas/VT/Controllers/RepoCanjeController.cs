using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class RepoCanjeController(IVentasService service,IRHService context,IAuthService auth,ILogger<RepoCanjeController> logger)
    : VentaReporteController(service,context,auth,logger,"repoCanje/index","canjes") { }
