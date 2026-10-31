using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class RepoRegVentasController(IVentasService service,IRHService context,IAuthService auth,ILogger<RepoRegVentasController> logger)
    : ModuloContextController(context,auth,logger,"vt","repoRegVentas/index")
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] RegistroVentasFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null)
        =>!ModelState.IsValid?InvalidInput():View(await service.GetRegistroVentasAsync(EmpresaId,PeriodoEmpresaSedeId,filtro,page,top,sort,dir));

    [HttpGet]
    public async Task<IActionResult> Exportar([FromQuery] RegistroVentasFiltro filtro,string? sort=null,string? dir=null)
    {
        if(!ModelState.IsValid)return InvalidInput();
        var model=await service.GetRegistroVentasAsync(EmpresaId,PeriodoEmpresaSedeId,filtro,sort:sort,dir:dir,exportar:true);
        if(model.Errores.Count>0)return BadRequest(new{success=false,message=string.Join(" ",model.Errores)});
        return File(InformeExcel.Crear(model.Titulo,model.Contexto.Empresa,model.ResumenSalida,model.Items,model.Columnas.Select(c=>(c.Titulo,c.Valor)).ToList(),preservarDecimales:true),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","registro-ventas.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> Imprimir([FromQuery] RegistroVentasFiltro filtro,string? sort=null,string? dir=null)
    {
        if(!ModelState.IsValid)return InvalidInput();
        var model=await service.GetRegistroVentasAsync(EmpresaId,PeriodoEmpresaSedeId,filtro,sort:sort,dir:dir,exportar:true);
        if(model.Errores.Count>0)return BadRequest(new{success=false,message=string.Join(" ",model.Errores)});
        var columns=model.Columnas;var print=new InventarioImpresionModel{Titulo=model.Titulo,Empresa=model.Contexto.Empresa,Resumen=model.ResumenSalida,Columnas=columns.Select(c=>c.Titulo).ToList(),
            Filas=model.Items.Select(row=>columns.Select(c=>RegistroVentasPageModel.Format(c,row)).ToList()).ToList()};
        return View("~/Areas/IV/Views/Shared/InformeImprimir.cshtml",print);
    }
}
