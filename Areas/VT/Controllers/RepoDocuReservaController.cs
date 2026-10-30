using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class RepoDocuReservaController(IVentasService service,IRHService context,IAuthService auth,ILogger<RepoDocuReservaController> logger)
    : VentaReporteController(service,context,auth,logger,"repoDocuReserva/index","reservas") { }
