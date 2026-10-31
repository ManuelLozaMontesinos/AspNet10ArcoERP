using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.VT.Controllers;

[Area("VT")]
public class CuadroCostoController(IVentasService service,IRHService context,IAuthService auth,ILogger<CuadroCostoController> logger)
    : ModuloContextController(context,auth,logger,"vt","cuadroCosto/index")
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] CuadroCostoFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null)
    {
        if(!ModelState.IsValid)return InvalidInput();
        return View(await service.GetCuadroCostoAsync(EmpresaId,PeriodoEmpresaSedeId,filtro,page,top,sort,dir));
    }

    [HttpGet]
    public async Task<IActionResult> Exportar([FromQuery] CuadroCostoFiltro filtro,string? sort=null,string? dir=null)
    {
        if(!ModelState.IsValid)return InvalidInput();
        var model=await service.GetCuadroCostoAsync(EmpresaId,PeriodoEmpresaSedeId,filtro,sort:sort,dir:dir,exportar:true);
        if(model.Errores.Count>0)return BadRequest(new{success=false,message=string.Join(" ",model.Errores)});
        return File(InformeExcel.Crear("Cuadro de costos",model.Contexto.Empresa,model.ResumenExportacion,model.Items,
            model.Columnas.Select(c=>(c.Titulo,c.Valor)).ToList(),preservarDecimales:true),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","cuadro-costos-"+model.Filtro.Base+".xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> Imprimir([FromQuery] CuadroCostoFiltro filtro,string? sort=null,string? dir=null)
    {
        if(!ModelState.IsValid)return InvalidInput();
        var model=await service.GetCuadroCostoAsync(EmpresaId,PeriodoEmpresaSedeId,filtro,sort:sort,dir:dir,exportar:true);
        if(model.Errores.Count>0)return BadRequest(new{success=false,message=string.Join(" ",model.Errores)});
        var columns=model.Columnas;
        return View("~/Areas/IV/Views/Shared/InformeImprimir.cshtml",new InventarioImpresionModel{Titulo="Cuadro de costos",Empresa=model.Contexto.Empresa,Resumen=model.ResumenExportacion,
            Columnas=columns.Select(c=>c.Titulo).ToList(),Filas=model.Items.Select(row=>columns.Select(c=>CuadroCostoPageModel.Formato(c,row)).ToList()).ToList()});
    }
}
