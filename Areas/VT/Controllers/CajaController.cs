using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class CajaController(IVentasService service,IRHService context,IAuthService auth,ILogger<CajaController> logger)
    : VentaAsignacionesController(service,context,auth,logger,"caja/index",true)
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null)
        =>!ModelState.IsValid||search?.Length>150?InvalidInput():View(await Ventas.GetCajasAsync(EmpresaId,EmpresaSedeId,search,estado,page,top,sort,dir));
    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        if(!ModelState.IsValid||id<=0)return InvalidInput();var model=await Ventas.GetCajaAsync(EmpresaId,EmpresaSedeId,id);
        return model is null?NotFound(new{success=false,message="No se encontró la caja en la sede seleccionada."}):Json(new{success=true,data=model});
    }
    [HttpPost]
    public async Task<IActionResult> Guardar(MaestroEdicion<CajaModel> model)
        =>!ModelState.IsValid?InvalidInput():!SameContext(model.ContextoId)?ContextChanged():Result(await Ventas.SaveCajaAsync(EmpresaId,EmpresaSedeId,UsuarioId,model));
    [HttpPost]
    public async Task<IActionResult> Estado(VentaEstadoSolicitud model)
        =>!ModelState.IsValid||model.Activar?InvalidInput():!SameContext(model.ContextoId)?ContextChanged():Result(await Ventas.AnularCajaAsync(EmpresaId,EmpresaSedeId,UsuarioId,model));
}
