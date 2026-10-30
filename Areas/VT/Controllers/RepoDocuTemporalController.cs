using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class RepoDocuTemporalController(IVentasService service,IRHService context,IAuthService auth,ILogger<RepoDocuTemporalController> logger)
    : VentaReporteController(service,context,auth,logger,"repoDocuTemporal/index","temporales") { }
