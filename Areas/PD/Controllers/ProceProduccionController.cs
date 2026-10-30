using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.PD.Controllers;

[Area("PD")]
public class ProceProduccionController(IProduccionService service,IRHService context,IAuthService auth,ILogger<ProceProduccionController> logger)
    : ModuloContextController(context,auth,logger,"pd","proceProduccion/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null)
        =>!ModelState.IsValid||search?.Length>150?InvalidInput():View(await service.GetProcesosAsync(EmpresaId,PeriodoEmpresaSedeId,search,estado,page,top,sort,dir));
    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        if(!ModelState.IsValid||id<=0)return InvalidInput();var data=await service.GetProcesoAsync(EmpresaId,PeriodoEmpresaSedeId,id);
        return data is null?NotFound(new{success=false,message="No se encontró el proceso."}):Json(new{success=true,data});
    }
    [HttpGet]
    public async Task<IActionResult> Detalles(int procesoId,string tipo,int subProcesoId=0,string? search=null,int page=1,int top=25,string? sort=null,string? dir=null)
    {
        if(!ModelState.IsValid||procesoId<=0||tipo is not ("subprocesos" or "operadores" or "personas")||search?.Length>150||(tipo!="subprocesos"&&subProcesoId<=0))return InvalidInput();
        var data=await service.GetDetallesProcesoAsync(EmpresaId,PeriodoEmpresaSedeId,procesoId,tipo,subProcesoId,search,page,top,sort,dir);
        return data is null?NotFound(new{success=false,message="No se encontró el proceso o subproceso en el contexto habilitado."}):Json(new{success=true,data});
    }
    private IActionResult Changed()=>Conflict(new{success=false,message="El contexto de trabajo cambió. Actualice el listado."});
    [HttpPost]
    public async Task<IActionResult> Guardar(ProduccionNombreSolicitud model)
        =>!ModelState.IsValid?InvalidInput():model.ContextoId!=PeriodoEmpresaSedeId?Changed():Result(await service.SaveProcesoAsync(EmpresaId,PeriodoEmpresaSedeId,UsuarioId,model));
    [HttpPost]
    public async Task<IActionResult> GuardarSubproceso(ProduccionNombreSolicitud model)
        =>!ModelState.IsValid?InvalidInput():model.ContextoId!=PeriodoEmpresaSedeId?Changed():Result(await service.SaveSubprocesoAsync(EmpresaId,PeriodoEmpresaSedeId,UsuarioId,model));
    [HttpPost]
    public async Task<IActionResult> Anular(ProduccionEstadoSolicitud model)
        =>!ModelState.IsValid?InvalidInput():model.ContextoId!=PeriodoEmpresaSedeId?Changed():Result(await service.AnularProcesoAsync(EmpresaId,PeriodoEmpresaSedeId,UsuarioId,model));
    [HttpPost]
    public async Task<IActionResult> AgregarOperador(ProduccionOperadorSolicitud model)
        =>!ModelState.IsValid?InvalidInput():model.ContextoId!=PeriodoEmpresaSedeId?Changed():Result(await service.AgregarOperadorAsync(EmpresaId,PeriodoEmpresaSedeId,UsuarioId,model));
    [HttpPost]
    public async Task<IActionResult> QuitarOperador(ProduccionEstadoSolicitud model)
        =>!ModelState.IsValid?InvalidInput():model.ContextoId!=PeriodoEmpresaSedeId?Changed():Result(await service.QuitarOperadorAsync(EmpresaId,PeriodoEmpresaSedeId,UsuarioId,model));
}
