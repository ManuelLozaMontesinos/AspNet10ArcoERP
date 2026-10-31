using System.ComponentModel.DataAnnotations;
using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.TS.Controllers;

[Area("TS")]
public class BancarizaController(ITesoreriaService service,IRHService context,IAuthService auth,ILogger<BancarizaController> logger)
    : ModuloContextController(context,auth,logger,"ts","bancariza/index")
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] BancarizacionFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null)
        =>!ModelState.IsValid?InvalidInput():View(await service.GetBancarizacionAsync(EmpresaId,PeriodoEmpresaSedeId,filtro,page,top,sort,dir));
    [HttpGet]
    public async Task<IActionResult> Obtener(int consultaId,int id)
    {
        if(!ModelState.IsValid||consultaId<=0||id<=0)return InvalidInput();
        try{var model=await service.GetBancarizacionDocumentoAsync(EmpresaId,PeriodoEmpresaSedeId,consultaId,id);return model is null?NotFound(new{success=false,message="No se encontró un comprobante de la sede seleccionada que cumpla el umbral de bancarización."}):Json(new{success=true,data=model});}
        catch(ValidationException ex){return BadRequest(new{success=false,message=ex.Message});}
    }
    [HttpGet]
    public async Task<IActionResult> Cobranzas(int consultaId,int comprobanteId,string? search=null,int page=1,int top=25,string? sort=null,string? dir=null)
    {
        if(!ModelState.IsValid||consultaId<=0||comprobanteId<=0||search?.Length>150)return InvalidInput();
        try{var model=await service.GetBancarizacionCobranzasAsync(EmpresaId,PeriodoEmpresaSedeId,consultaId,comprobanteId,search,page,top,sort,dir);return model is null?NotFound(new{success=false,message="No se encontró el comprobante de la sede seleccionada."}):Json(new{success=true,data=model});}
        catch(ValidationException ex){return BadRequest(new{success=false,message=ex.Message});}
    }
    [HttpPost]
    public async Task<IActionResult> Guardar([FromForm] BancarizacionCobranzaSolicitud model)
        =>!ModelState.IsValid?InvalidInput():model.ContextoId!=PeriodoEmpresaSedeId?Conflict(new{success=false,message="Cambió el contexto de trabajo. Actualice el listado."}):Result(await service.SaveBancarizacionCobranzaAsync(EmpresaId,PeriodoEmpresaSedeId,UsuarioId,model));
    [HttpPost]
    public async Task<IActionResult> Anular([FromForm] BancarizacionAnularSolicitud model)
        =>!ModelState.IsValid?InvalidInput():model.ContextoId!=PeriodoEmpresaSedeId?Conflict(new{success=false,message="Cambió el contexto de trabajo. Actualice el listado."}):Result(await service.AnularBancarizacionCobranzaAsync(EmpresaId,PeriodoEmpresaSedeId,UsuarioId,model));
}
