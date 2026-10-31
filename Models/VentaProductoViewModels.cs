using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AspNet10ArcoERP.Models;

public class VentaProductoFiltro
{
    public string Modo { get; set; } = "mes";
    public int? Mes { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public bool IncluirInterno { get; set; }
    public bool ExcluirObsequio { get; set; }
    [StringLength(150)] public string? Search { get; set; }
    public bool Consultar { get; set; }
    public Dictionary<string,string> Query(string sort,string dir)=>new()
    {
        ["modo"]=Modo,["mes"]=Mes?.ToString(CultureInfo.InvariantCulture)??"",
        ["desde"]=Desde?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)??"",["hasta"]=Hasta?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)??"",
        ["incluirInterno"]=IncluirInterno.ToString().ToLowerInvariant(),["excluirObsequio"]=ExcluirObsequio.ToString().ToLowerInvariant(),
        ["search"]=Search??"",["consultar"]="true",["sort"]=sort,["dir"]=dir
    };
}

public class VentaProductoFila
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Producto { get; set; } = "";
    public string Grupo { get; set; } = "";
    public string Clase { get; set; } = "";
    public string Familia { get; set; } = "";
    public decimal Minimo { get; set; }
    public decimal Maximo { get; set; }
    public decimal Promedio { get; set; }
    public int Tickets { get; set; }
    public decimal Cantidad { get; set; }
    public decimal Bruto { get; set; }
    public decimal Igv { get; set; }
    public decimal Importe { get; set; }
}

public sealed record VentaProductoColumna(string Campo,string Titulo,Func<VentaProductoFila,object?> Valor,bool Numero=false);

public class VentaProductoPageModel : ListadoPaginadoModel
{
    public VentaProductoFiltro Filtro { get; set; } = new();
    public VentaReporteContexto Contexto { get; set; } = new();
    public List<LookupItemModel> Meses { get; set; } = new();
    public List<VentaProductoFila> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();
    public bool Consultada { get; set; }
    public decimal Bruto { get; set; }
    public decimal Igv { get; set; }
    public decimal Importe { get; set; }
    public string Titulo=>"Ventas por producto";
    public string Resumen=>string.Join(" · ",new[]{ $"Ejercicio {Contexto.Anio}",Contexto.Sede,
        Filtro.Modo=="rango"?$"{Filtro.Desde:dd/MM/yyyy} al {Filtro.Hasta:dd/MM/yyyy}":Meses.FirstOrDefault(m=>m.Id==Filtro.Mes)?.Nombre,
        Filtro.IncluirInterno?"Con internos":"Sin internos",Filtro.ExcluirObsequio?"Sin obsequios":"Con obsequios",
        string.IsNullOrWhiteSpace(Filtro.Search)?null:"Búsqueda: "+Filtro.Search}.Where(s=>!string.IsNullOrWhiteSpace(s)));
    public string Nota=>"Importes en moneda local con el T/C de compra diario del reporte de ventas; si falta ese T/C, los importes extranjeros se calculan en cero. Las notas de crédito descuentan cantidad y total; bruto e IGV conservan los signos del reporte original. Mínimo y máximo corresponden a cantidades por comprobante y producto."+
        (Filtro.Modo=="rango"?" En las notas de crédito, el límite final de los importes usa la fecha del comprobante de referencia; las estadísticas usan la fecha de la nota, conforme al reporte original.":"");
    public string ResumenSalida=>$"{Resumen} · {Nota} · Totales filtrados en moneda local: Bruto {Bruto:N4}; IGV {Igv:N4}; Total {Importe:N4}.";
    public static IReadOnlyList<VentaProductoColumna> Columnas { get; } = new VentaProductoColumna[]
    {
        new("codigo","Código",r=>r.Codigo),new("producto","Producto",r=>r.Producto),new("grupo","Grupo",r=>r.Grupo),new("clase","Clase",r=>r.Clase),new("familia","Familia",r=>r.Familia),
        new("minimo","Cantidad mínima",r=>r.Minimo,true),new("maximo","Cantidad máxima",r=>r.Maximo,true),new("promedio","Cantidad promedio",r=>r.Promedio,true),
        new("tickets","Tickets",r=>r.Tickets,true),new("cantidad","Cantidad",r=>r.Cantidad,true),new("bruto","Bruto local",r=>r.Bruto,true),new("igv","IGV local",r=>r.Igv,true),new("importe","Total local",r=>r.Importe,true)
    };
    public static string Format(VentaProductoColumna column,VentaProductoFila row)=>column.Valor(row) switch
    {
        decimal amount=>amount.ToString("N4",CultureInfo.GetCultureInfo("es-PE")),int count=>count.ToString("N0",CultureInfo.GetCultureInfo("es-PE")),var value=>value?.ToString()??""
    };
}
