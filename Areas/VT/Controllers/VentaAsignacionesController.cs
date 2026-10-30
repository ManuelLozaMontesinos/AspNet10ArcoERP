using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

public abstract class VentaAsignacionesController(IVentasService service,IRHService context,IAuthService auth,ILogger logger,string menu,bool caja)
    : ModuloContextController(context,auth,logger,"vt",menu)
{
    protected IVentasService Ventas { get; } = service;
    private bool Allowed(string tipo)=>caja?tipo=="personal":tipo is "series" or "listas";
    protected bool SameContext(int id)=>id==PeriodoEmpresaSedeId;
    protected IActionResult ContextChanged()=>Conflict(new{success=false,message="Cambió el contexto de trabajo. Actualice el listado."});
    [HttpGet]
    public async Task<IActionResult> Asignaciones(int id,string tipo,string? search=null,int page=1,int top=25,string? sort=null,string? dir=null,int? filtroSedeId=null,int? monedaId=null,int? tipoDocumentoId=null)
        =>await AssignmentPage(id,tipo,false,search,page,top,sort,dir,filtroSedeId,monedaId,tipoDocumentoId);
    [HttpGet]
    public async Task<IActionResult> Candidatos(int id,string tipo,string? search=null,int page=1,int top=25,string? sort=null,string? dir=null,int? filtroSedeId=null,int? monedaId=null,int? tipoDocumentoId=null)
        =>await AssignmentPage(id,tipo,true,search,page,top,sort,dir,filtroSedeId,monedaId,tipoDocumentoId);
    private async Task<IActionResult> AssignmentPage(int id,string tipo,bool candidates,string? search,int page,int top,string? sort,string? dir,int? filtroSedeId,int? monedaId,int? tipoDocumentoId)
    {
        if(!ModelState.IsValid||id<=0||!Allowed(tipo)||search?.Length>150||filtroSedeId is <=0||monedaId is <=0||tipoDocumentoId is <=0)return InvalidInput();
        if((tipo!="listas"&&monedaId.HasValue)||(tipo!="series"&&tipoDocumentoId.HasValue))return InvalidInput();
        var result=await Ventas.GetAsignacionesVentaAsync(EmpresaId,EmpresaSedeId,id,tipo,candidates,search,page,top,sort,dir,filtroSedeId,monedaId,tipoDocumentoId);
        return result is null?NotFound(new{success=false,message="No se encontró el registro en el contexto seleccionado."}):Json(new{success=true,data=result});
    }
    [HttpPost]
    public async Task<IActionResult> AgregarAsignaciones(VentaVinculoSolicitud model)
    {
        if(!ModelState.IsValid||!Allowed(model.Tipo))return InvalidInput();
        return !SameContext(model.ContextoId)?ContextChanged():Result(await Ventas.AgregarAsignacionesVentaAsync(EmpresaId,EmpresaSedeId,UsuarioId,model));
    }
    [HttpPost]
    public async Task<IActionResult> EstadoAsignacion(VentaVinculoEstadoSolicitud model)
    {
        if(!ModelState.IsValid||!Allowed(model.Tipo)||(caja&&model.Activar))return InvalidInput();
        return !SameContext(model.ContextoId)?ContextChanged():Result(await Ventas.CambiarEstadoAsignacionVentaAsync(EmpresaId,EmpresaSedeId,UsuarioId,model));
    }
}
