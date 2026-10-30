using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.AS.Controllers;

[Area("AS")]
public class DocuComSerieController(IAdministracionService service,IRHService context,IAuthService auth,ILogger<DocuComSerieController> logger)
    : ModuloContextController(context,auth,logger,"as","docuComSerie/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search=null,int? sedeId=null,int? tipoDocumentoId=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null)
        => !ModelState.IsValid||search?.Length>150||sedeId is <=0||tipoDocumentoId is <=0?InvalidInput():View(await service.GetDocumentoSeriesAsync(EmpresaId,search,sedeId,tipoDocumentoId,estado,page,top,sort,dir));
    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {if(!ModelState.IsValid||id<=0)return InvalidInput();var model=await service.GetDocumentoSerieAsync(EmpresaId,id);return model is null?NotFound(new{success=false,message="No se encontró la serie."}):Json(new{success=true,data=model});}
    [HttpGet]
    public async Task<IActionResult> Almacenes(int sedeId)
        =>!ModelState.IsValid||sedeId<=0?InvalidInput():Json(new{success=true,data=await service.GetSerieAlmacenesAsync(EmpresaId,sedeId)});
    [HttpPost]
    public async Task<IActionResult> Guardar(MaestroEdicion<DocumentoSerieModel> model)
        => !ModelState.IsValid?InvalidInput():model.ContextoId!=PeriodoEmpresaSedeId?Conflict(new{success=false,message="Cambió el contexto de trabajo. Actualice el listado."}):Result(await service.SaveDocumentoSerieAsync(EmpresaId,UsuarioId,model));
    [HttpPost]
    public async Task<IActionResult> Anular(int id,string revision,int contextoId)
        => !ModelState.IsValid||id<=0||revision?.Length!=64?InvalidInput():contextoId!=PeriodoEmpresaSedeId?Conflict(new{success=false,message="Cambió el contexto de trabajo. Actualice el listado."}):Result(await service.AnularDocumentoSerieAsync(EmpresaId,UsuarioId,id,revision));
}
