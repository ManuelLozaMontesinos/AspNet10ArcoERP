using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

public abstract class VentaReporteController(IVentasService service,IRHService context,IAuthService auth,ILogger logger,string menu,string tipo)
    : ModuloContextController(context,auth,logger,"vt",menu)
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] VentaReporteFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null)
    {
        if(!ModelState.IsValid)return InvalidInput();
        return View(await service.GetReporteVentaAsync(EmpresaId,PeriodoEmpresaSedeId,tipo,filtro,page,top,sort,dir));
    }
    [HttpGet]
    public async Task<IActionResult> Exportar([FromQuery] VentaReporteFiltro filtro,string? sort=null,string? dir=null)
    {
        if(!ModelState.IsValid)return InvalidInput();
        var model=await service.GetReporteVentaAsync(EmpresaId,PeriodoEmpresaSedeId,tipo,filtro,sort:sort,dir:dir,exportar:true);
        if(model.Errores.Count>0)return BadRequest(new{success=false,message=string.Join(" ",model.Errores)});
        var columns=model.Columnas.Select(c=>(c.Titulo,c.Valor)).ToList();
        return File(InformeExcel.Crear(model.Titulo,model.Contexto.Empresa,model.Resumen,model.Items,columns,preservarDecimales:true),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","ventas-"+tipo+".xlsx");
    }
    [HttpGet]
    public async Task<IActionResult> Imprimir([FromQuery] VentaReporteFiltro filtro,string? sort=null,string? dir=null)
    {
        if(!ModelState.IsValid)return InvalidInput();
        var model=await service.GetReporteVentaAsync(EmpresaId,PeriodoEmpresaSedeId,tipo,filtro,sort:sort,dir:dir,exportar:true);
        if(model.Errores.Count>0)return BadRequest(new{success=false,message=string.Join(" ",model.Errores)});
        // Reuse the existing report-only print renderer; the report contains no inventory-specific UI.
        var columns=model.Columnas;
        var print=new InventarioImpresionModel{Titulo=model.Titulo,Empresa=model.Contexto.Empresa,Resumen=model.Resumen,Columnas=columns.Select(c=>c.Titulo).ToList(),
            Filas=model.Items.Select(row=>columns.Select(c=>VentaReporteColumnas.Format(c,row)).ToList()).ToList()};
        return View("~/Areas/IV/Views/Shared/InformeImprimir.cshtml",print);
    }
}
