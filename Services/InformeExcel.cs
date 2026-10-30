using System.Globalization;
using ClosedXML.Excel;

namespace AspNet10ArcoERP.Services;

public static class InformeExcel
{
    public static byte[] Crear<T>(string titulo,string empresa,string resumen,IEnumerable<T> rows,List<(string Label,Func<T,object?> Value)> columns,ISet<int>? precisionOcho=null,bool preservarDecimales=false)
    {
        using var workbook=new XLWorkbook();var sheet=workbook.Worksheets.Add("Consulta");
        sheet.Cell(1,1).Value=titulo;sheet.Cell(2,1).Value=empresa;sheet.Cell(3,1).Value=resumen;
        for(int i=0;i<columns.Count;i++)sheet.Cell(5,i+1).Value=columns[i].Label;
        int rowNumber=6;
        foreach(var item in rows)
        {
            for(int i=0;i<columns.Count;i++)
            {
                var cell=sheet.Cell(rowNumber,i+1);var value=columns[i].Value(item);
                switch(value)
                {
                    case DateTime date:cell.Value=date;cell.Style.DateFormat.Format="dd/mm/yyyy";break;
                    case decimal amount:
                        bool eight=precisionOcho?.Contains(i+1)==true;
                        if((eight||preservarDecimales)&&amount.ToString("G29",CultureInfo.InvariantCulture).Replace(".","").TrimStart('-','0').Length>15)
                            cell.Value=amount.ToString(eight?"F8":"G29",CultureInfo.InvariantCulture);
                        else cell.Value=amount;
                        cell.Style.NumberFormat.Format=eight?"#,##0.00000000":"#,##0.0000";break;
                    case int number:cell.Value=number;break;
                    default:cell.Value=value?.ToString()??"";break;
                }
            }
            rowNumber++;
        }
        sheet.Range(5,1,5,columns.Count).Style.Font.Bold=true;
        sheet.Range(5,1,Math.Max(5,rowNumber-1),columns.Count).SetAutoFilter();sheet.SheetView.FreezeRows(5);
        sheet.Columns(1,columns.Count).Width=20;sheet.Row(1).Style.Font.Bold=true;
        using var stream=new MemoryStream();workbook.SaveAs(stream);return stream.ToArray();
    }
}
