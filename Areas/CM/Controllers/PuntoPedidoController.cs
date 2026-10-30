using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.CM.Controllers;

[Area("CM")]
public class PuntoPedidoController(IComprasService service,IRHService context,IAuthService auth,ILogger<PuntoPedidoController> logger)
    : ModuloContextController(context,auth,logger,"cm","puntoPedido/index")
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] PuntoPedidoFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null)
        =>!ModelState.IsValid?InvalidInput():View(await service.GetPuntoPedidoAsync(EmpresaId,PeriodoEmpresaSedeId,filtro,page,top,sort,dir));

    [HttpGet]
    public async Task<IActionResult> Exportar([FromQuery] PuntoPedidoFiltro filtro,string? sort=null,string? dir=null)
    {
        if(!ModelState.IsValid)return InvalidInput();
        var model=await service.GetPuntoPedidoAsync(EmpresaId,PeriodoEmpresaSedeId,filtro,sort:sort,dir:dir,exportar:true);
        if(model.Contexto.Errores.Count>0)return BadRequest(new{success=false,message=string.Join(" ",model.Contexto.Errores)});
        var columns=new List<(string,Func<PuntoPedidoFila,object?>)>{("Código",r=>r.Codigo),("Producto",r=>r.Producto),("U.M.",r=>r.Unidad),("Proveedor",r=>r.Proveedor),("Documento",r=>r.Documento),("Stock mínimo",r=>r.StockMinimo),("Saldo disponible",r=>r.Saldo),("Peso neto",r=>r.PesoNeto),("Peso bruto",r=>r.PesoBruto),("Master pack",r=>r.MasterPack)};
        return File(InformeExcel.Crear("Punto de pedido",model.Contexto.Empresa,model.Resumen,model.Items,columns,preservarDecimales:true),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","punto-pedido.xlsx");
    }
}
