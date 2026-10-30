using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.CM.Controllers;

[Area("CM")]
public class RepoValorizaComprobController(IReporteDocumentosService service,IRHService context,IAuthService auth,ILogger<RepoValorizaComprobController> logger)
    : DocumentoReporteController(service,context,auth,logger,"cm","repoValorizaComprob/index","compras");
