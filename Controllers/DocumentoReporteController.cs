using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Controllers;

public abstract class DocumentoReporteController(IReporteDocumentosService service,IRHService context,IAuthService auth,ILogger logger,string area,string menu,string tipo)
    : ModuloContextController(context,auth,logger,area,menu)
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] DocumentoReporteFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null)
    {
        if(!ModelState.IsValid)return InvalidInput();
        return View(await service.GetReporteAsync(EmpresaId,PeriodoEmpresaSedeId,tipo,filtro,page,top,sort,dir));
    }

    [HttpGet]
    public async Task<IActionResult> Exportar([FromQuery] DocumentoReporteFiltro filtro,string? sort=null,string? dir=null)
    {
        if(!ModelState.IsValid)return InvalidInput();
        var model=await service.GetReporteAsync(EmpresaId,PeriodoEmpresaSedeId,tipo,filtro,sort:sort,dir:dir,exportar:true);
        if(model.Errores.Count>0)return BadRequest(new{success=false,message=string.Join(" ",model.Errores)});
        return File(InformeExcel.Crear(model.Titulo,model.Contexto.Empresa,model.Resumen,model.Items,model.Columnas.Select(c=>(c.Titulo,c.Valor)).ToList(),preservarDecimales:true),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","reporte-"+tipo+".xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> Imprimir([FromQuery] DocumentoReporteFiltro filtro,string? sort=null,string? dir=null)
    {
        if(!ModelState.IsValid)return InvalidInput();
        var model=await service.GetReporteAsync(EmpresaId,PeriodoEmpresaSedeId,tipo,filtro,sort:sort,dir:dir,exportar:true);
        if(model.Errores.Count>0)return BadRequest(new{success=false,message=string.Join(" ",model.Errores)});
        var columns=model.Columnas.Where(c=>c.Principal).ToList();
        var print=new InventarioImpresionModel{Titulo=model.Titulo,Empresa=model.Contexto.Empresa,Resumen=model.Resumen,Columnas=columns.Select(c=>c.Titulo).ToList(),
            Filas=model.Items.Select(row=>columns.Select(c=>DocumentoReporteColumnas.Format(c,row)).ToList()).ToList()};
        return View("~/Areas/IV/Views/Shared/InformeImprimir.cshtml",print);
    }
}
