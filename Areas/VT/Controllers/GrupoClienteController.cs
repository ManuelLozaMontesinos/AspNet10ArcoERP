using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class GrupoClienteController(IVentasService service,IRHService context,IAuthService auth,ILogger<GrupoClienteController> logger)
    : ModuloContextController(context,auth,logger,"vt","grupoCliente/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null)
        =>!ModelState.IsValid||search?.Length>150?InvalidInput():View(await service.GetGruposClienteAsync(EmpresaId,search,estado,page,top,sort,dir));
    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {if(!ModelState.IsValid||id<=0)return InvalidInput();var model=await service.GetGrupoClienteAsync(EmpresaId,id);return model is null?NotFound(new{success=false,message="No se encontró el grupo."}):Json(new{success=true,data=model});}
    [HttpGet]
    public async Task<IActionResult> Clientes(int id,string? search=null,int page=1,int top=25,string? sort=null,string? dir=null)
        =>await People(id,search,page,top,sort,dir,false);
    [HttpGet]
    public async Task<IActionResult> Candidatos(int id,string? search=null,int page=1,int top=25,string? sort=null,string? dir=null)
        =>await People(id,search,page,top,sort,dir,true);
    private async Task<IActionResult> People(int id,string? search,int page,int top,string? sort,string? dir,bool candidates)
    {
        if(!ModelState.IsValid||id<=0||search?.Length>150)return InvalidInput();
        var model=candidates?await service.GetGrupoCandidatosAsync(EmpresaId,id,search,page,top,sort,dir):await service.GetGrupoClientesAsync(EmpresaId,id,search,page,top,sort,dir);
        return model is null?NotFound(new{success=false,message="No se encontró el grupo."}):Json(new{success=true,data=model});
    }
    [HttpPost]
    public async Task<IActionResult> Guardar(MaestroEdicion<GrupoClienteModel> model)
        =>!ModelState.IsValid?InvalidInput():!Context(model.ContextoId)?Changed():Result(await service.SaveGrupoClienteAsync(EmpresaId,UsuarioId,model));
    [HttpPost]
    public async Task<IActionResult> Anular(int id,string revision,int contextoId)
        =>!ModelState.IsValid||id<=0||revision?.Length!=64?InvalidInput():!Context(contextoId)?Changed():Result(await service.AnularGrupoClienteAsync(EmpresaId,UsuarioId,id,revision));
    [HttpPost]
    public async Task<IActionResult> AgregarCliente(int grupoId,int proveedorId,int contextoId)
        =>!ModelState.IsValid||grupoId<=0||proveedorId<=0?InvalidInput():!Context(contextoId)?Changed():Result(await service.AgregarGrupoClienteAsync(EmpresaId,UsuarioId,grupoId,proveedorId));
    [HttpPost]
    public async Task<IActionResult> QuitarCliente(int grupoId,int id,string revision,int contextoId)
        =>!ModelState.IsValid||grupoId<=0||id<=0||revision?.Length!=64?InvalidInput():!Context(contextoId)?Changed():Result(await service.QuitarGrupoClienteAsync(EmpresaId,UsuarioId,grupoId,id,revision));
    private bool Context(int id)=>id==PeriodoEmpresaSedeId;
    private IActionResult Changed()=>Conflict(new{success=false,message="Cambió el contexto de trabajo. Actualice el listado."});
}
