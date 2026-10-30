using System.Globalization;
using System.IO.Compression;
using AspNet10ArcoERP.Models;
using ClosedXML.Excel;
using Dapper;

namespace AspNet10ArcoERP.Services;

public partial class InventarioService
{
    private record MinimumInput(int Fila, int RegistroId, int ProductoId, string Codigo, decimal Stock);

    public async Task<StockMinimoImportacionModel> GetImportacionMinimosAsync(int empresaId, int scopeId, int userId, int sedeId, Stream archivo)
    {
        var model = new StockMinimoImportacionModel { ContextoId=scopeId,EmpresaSedeId=sedeId };
        using var conn = factory.CreateConnection(); var scope = await MinimoContextAsync(conn,null,empresaId,scopeId,userId,sedeId);
        if (scope is null) { model.Errores.Add("Seleccione una sede activa de la empresa y un contexto de trabajo válido."); return model; }
        model.Anio=scope.Anio; model.Sede=scope.Sede;
        var configured = (await conn.QueryAsync<string>(@"SELECT i.columna FROM data_imp_item i JOIN data_import d ON d.data_import_id=i.data_import_id WHERE d.data_import='0002' ORDER BY i.orden")).AsList();
        if (!configured.SequenceEqual(StockMinimoFormato.Columnas)) { model.Errores.Add("La configuración de columnas del formato de importación cambió. Revísela antes de continuar."); return model; }
        using var copy = new MemoryStream(); var buffer = new byte[81920]; int count;
        while ((count=await archivo.ReadAsync(buffer))>0)
        {
            if (copy.Length+count>StockMinimoFormato.MaxBytes) { model.Errores.Add("El archivo supera el límite de 10 MB."); return model; }
            copy.Write(buffer,0,count);
        }
        copy.Position=0; List<MinimumInput> inputs;
        // Only untrusted workbook parsing is inside this error boundary; database failures propagate normally.
        try { inputs=ReadMinimumWorkbook(copy,model.Errores); }
        catch (Exception) { model.Errores.Add("El archivo no es un XLSX válido o contiene una estructura no admitida."); return model; }
        if (model.Errores.Count>0) return model;
        var catalog = new List<StockMinimoRowModel>();
        foreach (var chunk in inputs.Chunk(500))
        {
            catalog.AddRange(await conn.QueryAsync<StockMinimoRowModel>(MinimoSelect+MinimoFrom+" AND (avp.alm_vol_prod_id IN @Records OR prd.producto_id IN @Products OR prd.producto IN @Codes)",
                new { EmpresaId=empresaId,ScopeId=scopeId,SedeId=sedeId,Anio=scope.Anio,Activos=false,Records=chunk.Where(r=>r.RegistroId>0).Select(r=>r.RegistroId).Distinct().ToArray(),Products=chunk.Where(r=>r.ProductoId>0).Select(r=>r.ProductoId).Distinct().ToArray(),Codes=chunk.Where(r=>r.Codigo.Length>0).Select(r=>r.Codigo).Distinct().ToArray() }));
        }
        catalog=catalog.DistinctBy(r=>(r.ProductoId,r.Id)).ToList(); var seen=new Dictionary<int,int>();
        foreach (var input in inputs)
        {
            var matches=catalog.Where(r=>input.RegistroId>0?r.Id==input.RegistroId:input.ProductoId>0?r.ProductoId==input.ProductoId:string.Equals(r.Codigo,input.Codigo,StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count!=1) { model.Errores.Add($"Fila {input.Fila}: producto o registro inexistente, ambiguo o fuera del catálogo vigente de la empresa/sede."); continue; }
            var row=matches[0];
            if ((input.ProductoId>0 && input.ProductoId!=row.ProductoId) || (input.Codigo.Length>0 && !string.Equals(input.Codigo,row.Codigo,StringComparison.OrdinalIgnoreCase)))
            { model.Errores.Add($"Fila {input.Fila}: los identificadores y el código no corresponden al mismo producto."); continue; }
            if (seen.TryGetValue(row.ProductoId,out int first)) { model.Errores.Add($"Fila {input.Fila}: el producto ya aparece en la fila {first}. Corrija el duplicado."); continue; }
            seen[row.ProductoId]=input.Fila;
            model.Filas.Add(new() { Fila=input.Fila,ProductoId=row.ProductoId,Codigo=row.Codigo,Producto=row.Producto,Anterior=row.StockMinimo,StockMinimo=input.Stock,Revision=row.Revision });
        }
        return model;
    }

    private static List<MinimumInput> ReadMinimumWorkbook(Stream file, List<string> errors)
    {
        using (var zip=new ZipArchive(file,ZipArchiveMode.Read,true))
        {
            if (zip.Entries.Count>5000 || zip.Entries.Sum(e=>e.Length)>50L*1024*1024) { errors.Add("El contenido descomprimido del archivo supera el límite permitido."); return new(); }
        }
        file.Position=0; using var workbook=new XLWorkbook(file);
        if (workbook.Worksheets.Count==0) { errors.Add("El archivo no contiene una hoja de datos."); return new(); }
        var sheet=workbook.Worksheet(1); int last=sheet.LastRowUsed(XLCellsUsedOptions.Contents)?.RowNumber()??0;
        if (sheet.LastColumnUsed(XLCellsUsedOptions.Contents)?.ColumnNumber()!=6 || StockMinimoFormato.Columnas.Where((name,i)=>sheet.Cell(1,i+1).HasFormula || sheet.Cell(1,i+1).GetString()!=name).Any())
        { errors.Add("Las seis columnas de la primera hoja deben coincidir, en orden, con el formato exportado."); return new(); }
        if (last<2 || last>StockMinimoFormato.MaxFilas+1) { errors.Add("El archivo debe contener entre 1 y 5.000 filas de datos."); return new(); }
        var inputs=new List<MinimumInput>();
        for (int line=2;line<=last;line++)
        {
            var cells=Enumerable.Range(1,6).Select(i=>sheet.Cell(line,i)).ToArray(); if (cells.All(c=>c.IsEmpty())) continue;
            if (cells.Any(c=>c.HasFormula)) { errors.Add($"Fila {line}: sustituya las fórmulas por valores antes de importar."); continue; }
            bool idOk=MinimumId(cells[0],out int record),productOk=MinimumId(cells[1],out int product); string code=cells[2].GetString().Trim();
            decimal stock=0; bool numeric=cells[5].DataType==XLDataType.Number?cells[5].TryGetValue(out stock):decimal.TryParse(cells[5].GetString(),NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out stock);
            if (!idOk || !productOk || code.Length>50 || (record==0 && product==0 && code.Length==0)) errors.Add($"Fila {line}: indique un ID de registro, ID de producto o código válido.");
            else if (!numeric || stock<0 || stock>999999999999999.9999m || decimal.Round(stock,4)!=stock) errors.Add($"Fila {line}: el stock mínimo debe ser numérico, no negativo y tener hasta cuatro decimales. Use celdas numéricas o punto decimal sin separadores de miles.");
            else inputs.Add(new(line,record,product,code,stock));
        }
        if (inputs.Count==0 && errors.Count==0) errors.Add("El archivo no contiene filas de datos."); return inputs;
    }

    private static bool MinimumId(IXLCell cell,out int value)
    {
        value=0; if (cell.IsEmpty()) return true;
        return int.TryParse(cell.GetString(),NumberStyles.None,CultureInfo.InvariantCulture,out value) && value>=0;
    }
}
