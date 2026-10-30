using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class MedioPagoController(IVentasService service,IRHService context,IAuthService auth,ILogger<MedioPagoController> logger)
    : ModuloContextController(context,auth,logger,"vt","medioPago/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null)
        =>!ModelState.IsValid||search?.Length>150?InvalidInput():View(await service.GetMediosPagoAsync(EmpresaId,search,estado,page,top,sort,dir));
    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {if(!ModelState.IsValid||id<=0)return InvalidInput();var model=await service.GetMedioPagoAsync(EmpresaId,id);return model is null?NotFound(new{success=false,message="No se encontró el medio de pago."}):Json(new{success=true,data=model});}
    [HttpPost]
    public async Task<IActionResult> Guardar(MaestroEdicion<MedioPagoModel> model)
        =>!ModelState.IsValid?InvalidInput():model.ContextoId!=PeriodoEmpresaSedeId?Conflict(new{success=false,message="Cambió el contexto de trabajo. Actualice el listado."}):Result(await service.SaveMedioPagoAsync(EmpresaId,UsuarioId,model));
    [HttpPost]
    public async Task<IActionResult> Anular(int id,string revision,int contextoId)
        =>!ModelState.IsValid||id<=0||revision?.Length!=64?InvalidInput():contextoId!=PeriodoEmpresaSedeId?Conflict(new{success=false,message="Cambió el contexto de trabajo. Actualice el listado."}):Result(await service.AnularMedioPagoAsync(EmpresaId,UsuarioId,id,revision));
}
