using System.Text.Json;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.IV.Controllers;

[Area("IV")]
public class VolumenExistenController(IInventarioService service, IRHService contextService, IAuthService authService, ILogger<VolumenExistenController> logger)
    : InventarioContextController(contextService, authService, logger, "volumenExisten/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(int? sedeId = null, string? search = null, int page = 1, int top = 25, string? sort = null, string? dir = null)
        => !ModelState.IsValid || sedeId is <= 0 || search?.Length>150 ? InvalidInput() : View(await service.GetVolumenExistenciasAsync(EmpresaId,PeriodoEmpresaSedeId,UsuarioId,sedeId,search,page,top,sort,dir));

    [HttpGet]
    public async Task<IActionResult> Obtener(int sedeId, int productoId)
    {
        if (!ModelState.IsValid || sedeId<=0 || productoId<=0) return InvalidInput();
        var row=await service.GetStockMinimoAsync(EmpresaId,PeriodoEmpresaSedeId,UsuarioId,sedeId,productoId);
        return row is null ? NotFound(new { success=false,message="No se encontró el producto en el catálogo vigente de la sede." }) : Json(new { success=true,data=row });
    }

    [HttpPost]
    public async Task<IActionResult> Guardar(StockMinimoSolicitud model)
        => ModelState.IsValid ? Result(await service.SaveStockMinimoAsync(EmpresaId,PeriodoEmpresaSedeId,UsuarioId,model)) : InvalidInput();

    [HttpPost, RequestSizeLimit(StockMinimoFormato.MaxBytes+1024*1024), RequestFormLimits(MultipartBodyLengthLimit=StockMinimoFormato.MaxBytes)]
    public async Task<IActionResult> PrepararImportacion(int sedeId, IFormFile? archivo)
    {
        if (!ModelState.IsValid || sedeId<=0 || archivo is null || archivo.Length is <=0 or >StockMinimoFormato.MaxBytes || !Path.GetExtension(archivo.FileName).Equals(".xlsx",StringComparison.OrdinalIgnoreCase)) return BadRequest(new { success=false,message="Seleccione un archivo XLSX de hasta 10 MB y una sede válida." });
        using var stream=archivo.OpenReadStream();
        var preview=await service.GetImportacionMinimosAsync(EmpresaId,PeriodoEmpresaSedeId,UsuarioId,sedeId,stream);
        return Json(new { success=preview.Errores.Count==0,data=preview });
    }

    [HttpPost, RequestSizeLimit(3*1024*1024)]
    public async Task<IActionResult> ConfirmarImportacion(int contextoId, int sedeId, string? cambiosJson)
    {
        if (!ModelState.IsValid || contextoId<=0 || sedeId<=0 || string.IsNullOrEmpty(cambiosJson) || cambiosJson.Length>2_000_000) return InvalidInput();
        List<StockMinimoCambio>? changes;
        try { changes=JsonSerializer.Deserialize<List<StockMinimoCambio>>(cambiosJson,new JsonSerializerOptions { PropertyNameCaseInsensitive=true,NumberHandling=System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString }); }
        catch (JsonException) { return InvalidInput(); }
        if (changes is null || changes.Count is <1 or >StockMinimoFormato.MaxFilas || changes.Any(c=>c is null)) return InvalidInput();
        return Result(await service.SaveImportacionMinimosAsync(EmpresaId,PeriodoEmpresaSedeId,UsuarioId,new() { ContextoId=contextoId,EmpresaSedeId=sedeId,Cambios=changes }));
    }

    [HttpGet]
    public async Task<IActionResult> Exportar(int sedeId, string? search = null, string? sort = null, string? dir = null)
    {
        if (!ModelState.IsValid || sedeId<=0 || search?.Length>150) return InvalidInput();
        var model=await service.GetVolumenExistenciasAsync(EmpresaId,PeriodoEmpresaSedeId,UsuarioId,sedeId,search,sort:sort,dir:dir,exportar:true);
        if (model.Errores.Count>0) return BadRequest(new { success=false,message=string.Join(" ",model.Errores) });
        return Workbook(model,false);
    }

    [HttpGet]
    public async Task<IActionResult> Plantilla(int sedeId)
    {
        if (!ModelState.IsValid || sedeId<=0) return InvalidInput();
        var model=await service.GetVolumenExistenciasAsync(EmpresaId,PeriodoEmpresaSedeId,UsuarioId,sedeId);
        if (model.Errores.Count>0) return BadRequest(new { success=false,message=string.Join(" ",model.Errores) });
        return Workbook(model,true);
    }

    private IActionResult Workbook(VolumenExistenciaPageModel model,bool template)
    {
        using var book=new XLWorkbook(); var sheet=book.Worksheets.Add("VolumenExistencias");
        for(int i=0;i<StockMinimoFormato.Columnas.Count;i++) sheet.Cell(1,i+1).Value=StockMinimoFormato.Columnas[i];
        int row=2;
        if (!template) foreach(var item in model.Items)
        {
            sheet.Cell(row,1).Value=item.Id; sheet.Cell(row,2).Value=item.ProductoId; sheet.Cell(row,3).Value=item.Codigo; sheet.Cell(row,4).Value=item.Producto; sheet.Cell(row,5).Value=item.Unidad;
            if (item.StockMinimoTexto.Replace(".","").TrimStart('0').Length>15) sheet.Cell(row,6).Value=item.StockMinimoTexto;
            else sheet.Cell(row,6).Value=item.StockMinimo;
            row++;
        }
        sheet.Range(1,1,1,6).Style.Font.Bold=true; sheet.Range(1,1,Math.Max(1,row-1),6).SetAutoFilter(); sheet.SheetView.FreezeRows(1);
        sheet.Columns(1,6).Width=22; sheet.Column(4).Width=55; sheet.Column(6).Style.NumberFormat.Format="0.0000";
        var context=book.Worksheets.Add("Contexto"); context.Cell(1,1).Value="Sede"; context.Cell(1,2).Value=model.SedeSeleccionada; context.Cell(2,1).Value="Ejercicio"; context.Cell(2,2).Value=model.Contexto.Anio; context.Cell(3,1).Value="Búsqueda"; context.Cell(3,2).Value=model.Filtro.Search??"";
        context.Cell(4,1).Value="Importación"; context.Cell(4,2).Value="Primera hoja: seis columnas, sin fórmulas; máximo 5.000 filas. Stock mínimo no negativo con hasta cuatro decimales. Revise la sede al preparar el archivo."; context.Columns(1,2).Width=35;
        using var output=new MemoryStream(); book.SaveAs(output);
        return File(output.ToArray(),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",(template?"plantilla-minimos":"volumen-existencias")+".xlsx");
    }
}
