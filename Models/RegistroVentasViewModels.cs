using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AspNet10ArcoERP.Models;

public class RegistroVentasFiltro
{
    public string Modo { get; set; } = "mes";
    public int? Mes { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string Vista { get; set; } = "consolidado";
    public string Tipo { get; set; } = "general";
    [StringLength(150)] public string? Search { get; set; }
    public bool Consultar { get; set; }
    public Dictionary<string,string> Query(string sort,string dir)=>new()
    {
        ["modo"]=Modo,["mes"]=Mes?.ToString(CultureInfo.InvariantCulture)??"",
        ["desde"]=Desde?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)??"",["hasta"]=Hasta?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)??"",
        ["vista"]=Vista,["tipo"]=Tipo,["search"]=Search??"",["consultar"]="true",["sort"]=sort,["dir"]=dir
    };
}

public class RegistroVentasFila
{
    public int Id { get; set; }
    public string Origen { get; set; } = "";
    public string TipoCodigo { get; set; } = "";
    public string TipoDocumento { get; set; } = "";
    public string Documento { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string ClienteCodigo { get; set; } = "";
    public string Cliente { get; set; } = "";
    public string Identidad { get; set; } = "";
    public string Moneda { get; set; } = "";
    public string Simbolo { get; set; } = "";
    public decimal TipoCambio { get; set; }
    public string CondicionPago { get; set; } = "";
    public string MedioPago { get; set; } = "";
    public string Anticipo { get; set; } = "";
    public string Estado { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string Producto { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Unidad { get; set; } = "";
    public string Grupo { get; set; } = "";
    public string Clase { get; set; } = "";
    public string Familia { get; set; } = "";
    public decimal Cantidad { get; set; }
    public decimal Bruto { get; set; }
    public decimal Neto { get; set; }
    public decimal Igv { get; set; }
    public decimal Total { get; set; }
}

public sealed record RegistroVentasColumna(string Campo,string Titulo,Func<RegistroVentasFila,object?> Valor,bool Numero=false,bool Fecha=false);

public class RegistroVentasPageModel : ListadoPaginadoModel
{
    public RegistroVentasFiltro Filtro { get; set; } = new();
    public VentaReporteContexto Contexto { get; set; } = new();
    public List<LookupItemModel> Meses { get; set; } = new();
    public List<RegistroVentasFila> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();
    public bool Consultada { get; set; }
    public decimal Bruto { get; set; }
    public decimal Neto { get; set; }
    public decimal Igv { get; set; }
    public decimal TotalLocal { get; set; }
    public string Titulo=>"Registro de ventas";
    public string Resumen=>string.Join(" · ",new[]{ $"Ejercicio {Contexto.Anio}",Contexto.Sede,Filtro.Vista=="detallado"?"Detallado":"Consolidado",Filtro.Tipo=="oficial"?"Oficial":"General",
        Filtro.Modo=="rango"?$"{Filtro.Desde:dd/MM/yyyy} al {Filtro.Hasta:dd/MM/yyyy}":Meses.FirstOrDefault(m=>m.Id==Filtro.Mes)?.Nombre,
        string.IsNullOrWhiteSpace(Filtro.Search)?null:"Búsqueda: "+Filtro.Search}.Where(s=>!string.IsNullOrWhiteSpace(s)));
    public string Nota=>Filtro.Vista=="detallado"
        ? "Importes de las líneas en moneda local con el T/C del documento. El detalle conserva los importes de origen y no incorpora las notas de crédito separadas. Oficial incluye todos los tipos de comprobante no internos; General incluye solo el tipo normal, según el reporte original."
        : "Importes en moneda local con el T/C del documento. Las notas de crédito conservan sus importes por fila y se restan en los totales. Los documentos anulados muestran importes cero. Oficial excluye comprobantes internos y temporales; General incluye comprobantes normales. Se conservan las notas de crédito del reporte original en ambas vistas.";
    public string ResumenSalida=>$"{Resumen} · {Nota} · Totales filtrados en moneda local: Bruto {Bruto:N4}; Neto {Neto:N4}; IGV {Igv:N4}; Total {TotalLocal:N4}.";
    public List<RegistroVentasColumna> Columnas
    {
        get
        {
            var result=new List<RegistroVentasColumna>
            {
                new("tipoDocumento","Tipo",r=>r.TipoDocumento),new("documento","Documento",r=>r.Documento),new("fecha","Fecha",r=>r.Fecha,false,true),
                new("clienteCodigo","Código cliente",r=>r.ClienteCodigo),new("cliente","Cliente",r=>r.Cliente),new("identidad","Identidad / RUC",r=>r.Identidad),
                new("moneda","Moneda origen",r=>r.Moneda),new("simbolo","Símbolo",r=>r.Simbolo),new("tipoCambio","T/C",r=>r.TipoCambio,true)
            };
            if(Filtro.Vista=="detallado")result.AddRange(new RegistroVentasColumna[]{
                new("codigo","Código producto",r=>r.Codigo),new("producto","Producto",r=>r.Producto),new("descripcion","Descripción de línea",r=>r.Descripcion),new("unidad","U.M.",r=>r.Unidad),
                new("grupo","Grupo",r=>r.Grupo),new("clase","Clase",r=>r.Clase),new("familia","Familia",r=>r.Familia),new("cantidad","Cantidad",r=>r.Cantidad,true)});
            else result.AddRange(new RegistroVentasColumna[]{new("condicionPago","Condición de pago",r=>r.CondicionPago),new("anticipo","Anticipo",r=>r.Anticipo)});
            result.AddRange(new RegistroVentasColumna[]{new("bruto","Bruto local",r=>r.Bruto,true),new("neto","Neto local",r=>r.Neto,true),new("igv","IGV local",r=>r.Igv,true),new("total","Total local",r=>r.Total,true),new("medioPago","Medio de pago",r=>r.MedioPago),new("estado","Estado",r=>r.Estado)});
            return result;
        }
    }
    public static string Format(RegistroVentasColumna column,RegistroVentasFila row)=>column.Valor(row) switch
    {
        DateTime date=>date.ToString("dd/MM/yyyy"),decimal amount=>amount.ToString("N4",CultureInfo.GetCultureInfo("es-PE")),var value=>value?.ToString()??""
    };
}
