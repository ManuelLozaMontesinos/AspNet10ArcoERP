using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class DetraccionController(IVentasService service,IRHService context,IAuthService auth,ILogger<DetraccionController> logger)
    : ModuloContextController(context,auth,logger,"vt","detraccion/index")
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] DetraccionFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null)
        =>!ModelState.IsValid?InvalidInput():View(await service.GetDetraccionesAsync(EmpresaId,PeriodoEmpresaSedeId,filtro,page,top,sort,dir));
    [HttpGet]
    public async Task<IActionResult> Obtener(int consultaId,int id)
    {
        if(!ModelState.IsValid||consultaId<=0||id<=0)return InvalidInput();
        var row=await service.GetDetraccionAsync(EmpresaId,PeriodoEmpresaSedeId,consultaId,id);
        return row is null?NotFound(new{success=false,message="No se encontró una factura emitida con detracción en esta sede y ejercicio."}):Json(new{success=true,data=new{
            row.Id,row.ConsultaId,row.Revision,row.TipoDocumento,row.Documento,Fecha=row.Fecha.ToString("yyyy-MM-dd"),row.Cliente,row.DocumentoCliente,row.Moneda,
            Total=row.Total.ToString("G29",System.Globalization.CultureInfo.InvariantCulture),row.TipoDetraccion,Tasa=row.Tasa.ToString("G29",System.Globalization.CultureInfo.InvariantCulture),MontoDetraccion=row.MontoDetraccion.ToString("G29",System.Globalization.CultureInfo.InvariantCulture),
            row.NumeroOperacionPago,FechaPago=row.FechaPago?.ToString("yyyy-MM-dd"),row.MontoPagoTexto,row.MontoCargoTexto}});
    }
    [HttpPost]
    public async Task<IActionResult> Guardar(DetraccionPagoSolicitud model)
    {
        if(!ModelState.IsValid)return InvalidInput();
        if(model.ContextoId!=PeriodoEmpresaSedeId)return Conflict(new{success=false,message="El contexto de trabajo cambió. Actualice el listado antes de guardar."});
        return Result(await service.SaveDetraccionPagoAsync(EmpresaId,PeriodoEmpresaSedeId,UsuarioId,model));
    }
}
