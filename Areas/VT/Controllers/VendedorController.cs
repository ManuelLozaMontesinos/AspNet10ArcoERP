using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class VendedorController(IVentasService service,IRHService context,IAuthService auth,ILogger<VendedorController> logger)
    : VentaAsignacionesController(service,context,auth,logger,"vendedor/index",false)
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null)
        =>!ModelState.IsValid||search?.Length>150?InvalidInput():View(await Ventas.GetVendedoresAsync(EmpresaId,search,estado,page,top,sort,dir));
    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        if(!ModelState.IsValid||id<=0)return InvalidInput();var model=await Ventas.GetVendedorAsync(EmpresaId,id);
        return model is null?NotFound(new{success=false,message="No se encontró el vendedor."}):Json(new{success=true,data=model});
    }
    [HttpGet]
    public async Task<IActionResult> Personas(string? search=null,int page=1,int top=25,string? sort=null,string? dir=null,int? filtroSedeId=null)
        =>!ModelState.IsValid||search?.Length>150||filtroSedeId is <=0?InvalidInput():Json(new{success=true,data=await Ventas.GetPersonalVendedorAsync(EmpresaId,search,page,top,sort,dir,filtroSedeId)});
    [HttpPost]
    public async Task<IActionResult> Guardar(MaestroEdicion<VendedorModel> model)
        =>!ModelState.IsValid?InvalidInput():!SameContext(model.ContextoId)?ContextChanged():Result(await Ventas.SaveVendedorAsync(EmpresaId,UsuarioId,model));
    [HttpPost]
    public async Task<IActionResult> Estado(VentaEstadoSolicitud model)
        =>!ModelState.IsValid?InvalidInput():!SameContext(model.ContextoId)?ContextChanged():Result(await Ventas.CambiarEstadoVendedorAsync(EmpresaId,UsuarioId,model));
}
