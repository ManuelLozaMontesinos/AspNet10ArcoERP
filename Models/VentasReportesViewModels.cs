using System.Globalization;

namespace AspNet10ArcoERP.Models;

public class VentaReporteFiltro
{
    public string? Modo { get; set; }
    public int? Mes { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int? VendedorId { get; set; }
    public string? Search { get; set; }
    public string Agrupar { get; set; } = "producto";
    public bool Consultar { get; set; }
    public Dictionary<string,string> Query(string sort,string dir)=>new()
    {
        ["modo"]=Modo??"",["mes"]=Mes?.ToString(CultureInfo.InvariantCulture)??"",
        ["desde"]=Desde?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)??"",["hasta"]=Hasta?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)??"",
        ["vendedorId"]=VendedorId?.ToString()??"",["search"]=Search??"",["agrupar"]=Agrupar,["consultar"]="true",["sort"]=sort,["dir"]=dir
    };
}

public class VentaReporteContexto
{
    public int PeriodoEmpresaSedeId { get; set; }
    public int EmpresaSedeId { get; set; }
    public int Anio { get; set; }
    public string Empresa { get; set; } = "";
    public string Sede { get; set; } = "";
    public DateTime Hoy { get; set; }
}

public class VentaReporteRowModel
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int SaldoId { get; set; }
    public int ClienteId { get; set; }
    public int VendedorId { get; set; }
    public string Codigo { get; set; } = "";
    public string Producto { get; set; } = "";
    public string Unidad { get; set; } = "";
    public string Detalle { get; set; } = "";
    public string Pedido { get; set; } = "";
    public string ListaPrecio { get; set; } = "";
    public string Cliente { get; set; } = "";
    public string Ruc { get; set; } = "";
    public string Vendedor { get; set; } = "";
    public string TipoDocumento { get; set; } = "";
    public string Documento { get; set; } = "";
    public DateTime? Fecha { get; set; }
    public string Moneda { get; set; } = "";
    public string MedioPago { get; set; } = "";
    public string Estado { get; set; } = "";
    public decimal TipoCambio { get; set; }
    public decimal Bruto { get; set; }
    public decimal Neto { get; set; }
    public decimal Igv { get; set; }
    public decimal Total { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal ReservaReferencia { get; set; }
}

public sealed record VentaReporteColumna(string Campo,string Titulo,Func<VentaReporteRowModel,object?> Valor,bool Numero=false,bool Fecha=false);

public static class VentaReporteColumnas
{
    public static List<VentaReporteColumna> Get(string tipo,string agrupar="producto")
    {
        if(tipo=="pedidos")return new()
        {
            new("pedido","Pedido",r=>r.Pedido),new("codigo","Código",r=>r.Codigo),new("producto","Producto",r=>r.Producto),new("unidad","U.M.",r=>r.Unidad),new("detalle","Detalle",r=>r.Detalle),
            new("cantidad","Cantidad",r=>r.Cantidad,true),new("precioUnitario","P. unitario",r=>r.PrecioUnitario,true),new("total","Total",r=>r.Total,true),new("moneda","Moneda",r=>r.Moneda),
            new("listaPrecio","Lista de precios",r=>r.ListaPrecio),new("cliente","Cliente",r=>r.Cliente),new("vendedor","Vendedor",r=>r.Vendedor),new("estado","Estado",r=>r.Estado)
        };
        if(tipo=="reservas")
        {
            var product=new List<VentaReporteColumna>{new("codigo","Código",r=>r.Codigo),new("producto","Producto / servicio",r=>r.Producto),new("reservaReferencia","Reserva de referencia",r=>r.ReservaReferencia,true)};
            var document=new List<VentaReporteColumna>{new("tipoDocumento","Tipo",r=>r.TipoDocumento),new("documento","Documento",r=>r.Documento),new("fecha","Fecha",r=>r.Fecha,false,true),new("cliente","Cliente",r=>r.Cliente),new("vendedor","Vendedor",r=>r.Vendedor)};
            return (agrupar=="documento"?document.Concat(product):product.Concat(document)).Concat(new[]{new VentaReporteColumna("cantidad","Cantidad pendiente",r=>r.Cantidad,true),new("estado","Estado",r=>r.Estado)}).ToList();
        }
        return new()
        {
            new("fecha","Fecha",r=>r.Fecha,false,true),new("tipoDocumento","Tipo",r=>r.TipoDocumento),new("documento","Documento",r=>r.Documento),new("codigo","Código cliente",r=>r.Codigo),new("cliente","Cliente",r=>r.Cliente),new("ruc","RUC",r=>r.Ruc),
            new("moneda","Moneda origen",r=>r.Moneda),new("tipoCambio","T/C",r=>r.TipoCambio,true),new("bruto","Bruto local",r=>r.Bruto,true),new("neto","Neto local",r=>r.Neto,true),new("igv","IGV local",r=>r.Igv,true),new("total","Total local",r=>r.Total,true),
            new("medioPago","Medio de pago",r=>r.MedioPago),new("estado","Estado",r=>r.Estado)
        };
    }
    public static string Format(VentaReporteColumna column,VentaReporteRowModel row)=>column.Valor(row) switch
    {
        DateTime date=>date.ToString("dd/MM/yyyy"),decimal amount=>amount.ToString("N4",CultureInfo.GetCultureInfo("es-PE")),_=>column.Valor(row)?.ToString()??""
    };
}

public class VentaReportePageModel : ListadoPaginadoModel
{
    public string Tipo { get; set; } = "";
    public string Titulo { get; set; } = "";
    public VentaReporteFiltro Filtro { get; set; } = new();
    public VentaReporteContexto Contexto { get; set; } = new();
    public List<LookupItemModel> Meses { get; set; } = new();
    public List<LookupItemModel> Vendedores { get; set; } = new();
    public List<VentaReporteRowModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();
    public bool Consultada { get; set; }
    public decimal TotalLocal { get; set; }
    public List<VentaReporteColumna> Columnas=>VentaReporteColumnas.Get(Tipo,Filtro.Agrupar);
    public string Resumen=>string.Join(" · ",new[]{ $"Ejercicio {Contexto.Anio}",Contexto.Sede,
        Tipo=="reservas"?$"Mes de reserva: {Contexto.Hoy:MM} (mes actual del servidor)":Filtro.Modo=="rango"?$"{Filtro.Desde:dd/MM/yyyy} al {Filtro.Hasta:dd/MM/yyyy}":Filtro.Modo=="anual"?"Todo el ejercicio":Meses.FirstOrDefault(m=>m.Id==Filtro.Mes)?.Nombre,
        Vendedores.FirstOrDefault(v=>v.Id==Filtro.VendedorId)?.Nombre,string.IsNullOrWhiteSpace(Filtro.Search)?null:"Búsqueda: "+Filtro.Search }.Where(s=>!string.IsNullOrWhiteSpace(s)));
}
