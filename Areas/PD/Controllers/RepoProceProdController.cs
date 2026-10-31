using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.PD.Controllers;

[Area("PD")]
public class RepoProceProdController(IProduccionService service,IRHService context,IAuthService auth,ILogger<RepoProceProdController> logger)
    : ModuloContextController(context,auth,logger,"pd","repoProceProd/index")
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ProduccionReporteFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null)
    {
        if(!ModelState.IsValid)return InvalidInput();
        return View(await service.GetReporteProcesosAsync(EmpresaId,PeriodoEmpresaSedeId,filtro,page,top,sort,dir));
    }
    [HttpGet]
    public async Task<IActionResult> Exportar([FromQuery] ProduccionReporteFiltro filtro,string? sort=null,string? dir=null)
    {
        if(!ModelState.IsValid)return InvalidInput();
        var model=await service.GetReporteProcesosAsync(EmpresaId,PeriodoEmpresaSedeId,filtro,sort:sort,dir:dir,exportar:true);
        if(model.Errores.Count>0)return BadRequest(new{success=false,message=string.Join(" ",model.Errores)});
        var columns=model.Columnas;var hours=columns.Select((c,i)=>(Column:c,Index:i+1)).Where(c=>c.Column.Hora).Select(c=>c.Index).ToHashSet();
        return File(InformeExcel.Crear("Procesos de producción",model.Contexto.Empresa,model.ResumenSalida,model.Items,columns.Select(c=>(c.Titulo,c.Valor)).ToList(),preservarDecimales:true,fechasConHora:hours),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","procesos-produccion-"+model.Filtro.Vista+".xlsx");
    }
    [HttpGet]
    public async Task<IActionResult> Imprimir([FromQuery] ProduccionReporteFiltro filtro,string? sort=null,string? dir=null)
    {
        if(!ModelState.IsValid)return InvalidInput();
        var model=await service.GetReporteProcesosAsync(EmpresaId,PeriodoEmpresaSedeId,filtro,sort:sort,dir:dir,exportar:true);
        if(model.Errores.Count>0)return BadRequest(new{success=false,message=string.Join(" ",model.Errores)});
        var columns=model.Columnas;
        return View("~/Areas/IV/Views/Shared/InformeImprimir.cshtml",new InventarioImpresionModel{Titulo="Procesos de producción",Empresa=model.Contexto.Empresa,Resumen=model.ResumenSalida,
            Columnas=columns.Select(c=>c.Titulo).ToList(),Filas=model.Items.Select(row=>columns.Select(c=>ProduccionReportePageModel.Formato(c,row)).ToList()).ToList()});
    }
}
