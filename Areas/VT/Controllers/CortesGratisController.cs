using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class CortesGratisController(IVentasService service,IRHService context,IAuthService auth,ILogger<CortesGratisController> logger)
    : ModuloContextController(context,auth,logger,"vt","cortesGratis/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null)
        =>!ModelState.IsValid||search?.Length>150?InvalidInput():View(await service.GetCortesGratisClasesAsync(EmpresaId,PeriodoEmpresaSedeId,search,estado,page,top,sort,dir));
    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        if(!ModelState.IsValid||id<=0)return InvalidInput();var model=await service.GetCortesGratisClaseAsync(EmpresaId,PeriodoEmpresaSedeId,id);
        return model is null?NotFound(new{success=false,message="No se encontró la clase de productos."}):Json(new{success=true,data=model});
    }
    [HttpGet]
    public async Task<IActionResult> Productos(int claseId,int? familiaId=null,string? search=null,int page=1,int top=25,string? sort=null,string? dir=null)
    {
        if(!ModelState.IsValid||claseId<=0||familiaId is <=0||search?.Length>150)return InvalidInput();
        var model=await service.GetCortesGratisProductosAsync(EmpresaId,PeriodoEmpresaSedeId,claseId,familiaId,search,page,top,sort,dir);
        return model is null?NotFound(new{success=false,message="No se encontró la clase o familia de la sede seleccionada."}):Json(new{success=true,data=model});
    }
    [HttpPost]
    public async Task<IActionResult> Guardar([FromForm] CorteGratisSolicitud model)
        =>!ModelState.IsValid?InvalidInput():model.ContextoId!=PeriodoEmpresaSedeId?Conflict(new{success=false,message="Cambió el contexto de trabajo. Actualice el listado."}):Result(await service.SaveCorteGratisAsync(EmpresaId,PeriodoEmpresaSedeId,UsuarioId,model));
}
