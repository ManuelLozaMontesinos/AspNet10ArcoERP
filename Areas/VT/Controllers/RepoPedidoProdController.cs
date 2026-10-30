using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class RepoPedidoProdController(IVentasService service,IRHService context,IAuthService auth,ILogger<RepoPedidoProdController> logger)
    : VentaReporteController(service,context,auth,logger,"repoPedidoProd/index","pedidos") { }
