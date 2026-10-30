using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.IV.Controllers;

[Area("IV")]
public class RepoGuiaRemiController(IReporteDocumentosService service,IRHService context,IAuthService auth,ILogger<RepoGuiaRemiController> logger)
    : DocumentoReporteController(service,context,auth,logger,"iv","repoGuiaRemi/index","guias");
