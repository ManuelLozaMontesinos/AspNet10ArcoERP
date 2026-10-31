using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AspNet10ArcoERP.Models;

public class CuadroCostoFiltro
{
    public int? ListaPrecioId { get; set; }
    public string Base { get; set; } = "consulta";
    [StringLength(12)] public string? Codigo { get; set; }
    [StringLength(150)] public string? Descripcion { get; set; }
    public bool Consultar { get; set; }
    public Dictionary<string,string> Query(string sort,string dir) => new()
    {
        ["listaPrecioId"]=ListaPrecioId?.ToString(CultureInfo.InvariantCulture)??"",["base"]=Base,
        ["codigo"]=Codigo??"",["descripcion"]=Descripcion??"",["consultar"]="true",["sort"]=sort,["dir"]=dir
    };
}

public class CuadroCostoFila
{
    public int Id { get; set; }
    public int SaldoId { get; set; }
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = "";
    public string Producto { get; set; } = "";
    public decimal Promedio { get; set; }
    public DateTime? FechaCompra { get; set; }
    public decimal UltimaCompra { get; set; }
    public decimal Reposicion { get; set; }
    public decimal Precio { get; set; }
    public decimal MargenA { get; set; }
    public decimal MargenB { get; set; }
}

public sealed record CuadroCostoColumna(string Campo,string Titulo,Func<CuadroCostoFila,object?> Valor,bool Numero=false,bool Fecha=false);

public class CuadroCostoPageModel : ListadoPaginadoModel
{
    public CuadroCostoFiltro Filtro { get; set; } = new();
    public VentaReporteContexto Contexto { get; set; } = new();
    public List<LookupItemModel> Listas { get; set; } = new();
    public List<CuadroCostoFila> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();
    public List<string> Avisos { get; set; } = new();
    public bool Consultada { get; set; }
    public string Lista { get; set; } = "";
    public string Moneda { get; set; } = "";
    public decimal FactorIgv { get; set; }
    public decimal TipoCambio { get; set; }
    public string BaseNombre => Filtro.Base=="reporte"?"Reporte legacy: costos sin IGV y precio local":"Consulta legacy: costos con IGV y precio de lista";
    public string Resumen => $"Ejercicio {Contexto.Anio} · {Contexto.Sede} · Mes {Contexto.Hoy:MM} (actual del servidor) · {Lista} · {BaseNombre}";
    public string Nota => Filtro.Base=="reporte"
        ? $"Costos sin IGV; precio de venta en moneda local. T/C compra tipo 02 del {Contexto.Hoy:dd/MM/yyyy}: {TipoCambio:N4}. La fecha de última compra se muestra para S01."
        : $"Costos promedio y de última compra con IGV ({(FactorIgv-1)*100:N4} %); precio de venta en {Moneda}, sin conversión. La fecha de última compra se muestra para S01 y S13.";
    public List<CuadroCostoColumna> Columnas => new()
    {
        new("codigo","Código",r=>r.Codigo),new("producto","Producto",r=>r.Producto),new("promedio","Costo promedio",r=>r.Promedio,true),
        new("fechacompra","F. última compra",r=>r.FechaCompra,false,true),new("ultimacompra","Última compra",r=>r.UltimaCompra,true),
        new("reposicion","V. reposición",r=>r.Reposicion,true),new("precio","Precio de venta",r=>r.Precio,true),
        new("margena","Margen A (fracción)",r=>r.MargenA,true),new("margenb","Margen B (fracción)",r=>r.MargenB,true)
    };
    public static string Formato(CuadroCostoColumna column,CuadroCostoFila row) => column.Valor(row) switch
    {
        DateTime date=>date.ToString("dd/MM/yyyy"),decimal value=>value.ToString("N8",CultureInfo.GetCultureInfo("es-PE")),var value=>value?.ToString()??""
    };
    public string ResumenExportacion => string.Join(" · ",new[]{Resumen,Nota,
        string.IsNullOrWhiteSpace(Filtro.Codigo)?null:"Código: "+Filtro.Codigo,
        string.IsNullOrWhiteSpace(Filtro.Descripcion)?null:"Producto: "+Filtro.Descripcion,
        Avisos.Count==0?null:string.Join(" ",Avisos)}.Where(s=>!string.IsNullOrWhiteSpace(s)));
}
