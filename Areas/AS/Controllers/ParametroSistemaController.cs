using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.AS.Controllers;

[Area("AS")]
public class ParametroSistemaController(IAdministracionService service,IRHService context,IAuthService auth,ILogger<ParametroSistemaController> logger)
    : ModuloContextController(context,auth,logger,"as","parametroSistema/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search=null,int? grupoId=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null)
        =>!ModelState.IsValid||search?.Length>150||grupoId is <=0?InvalidInput():View(await service.GetParametrosSistemaAsync(EmpresaId,PeriodoEmpresaSedeId,search,grupoId,estado,page,top,sort,dir));
    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        if(!ModelState.IsValid||id<=0)return InvalidInput();var model=await service.GetParametroSistemaAsync(EmpresaId,PeriodoEmpresaSedeId,id);
        return model is null?NotFound(new{success=false,message="No se encontró el parámetro."}):Json(new{success=true,data=model});
    }
    [HttpPost]
    public async Task<IActionResult> Guardar([FromForm] ParametroSistemaSolicitud model)
        =>!ModelState.IsValid?InvalidInput():model.ContextoId!=PeriodoEmpresaSedeId?Conflict(new{success=false,message="Cambió el contexto de trabajo. Actualice el listado."}):Result(await service.SaveParametroSistemaAsync(EmpresaId,PeriodoEmpresaSedeId,UsuarioId,model));
}
