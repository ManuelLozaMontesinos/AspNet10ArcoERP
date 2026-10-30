using System.Globalization;

namespace AspNet10ArcoERP.Models;

public class DocumentoReporteFiltro
{
    public string Modo { get; set; } = "mes";
    public int? Mes { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int? ProveedorId { get; set; }
    public string? Search { get; set; }
    public bool Consultar { get; set; }

    public Dictionary<string,string> Query(string sort,string dir)=>new()
    {
        ["modo"]=Modo,["mes"]=Mes?.ToString(CultureInfo.InvariantCulture)??"",
        ["desde"]=Desde?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)??"",
        ["hasta"]=Hasta?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)??"",
        ["proveedorId"]=ProveedorId?.ToString(CultureInfo.InvariantCulture)??"",
        ["search"]=Search??"",["consultar"]="true",["sort"]=sort,["dir"]=dir
    };
}

public class DocumentoReporteContexto
{
    public int PeriodoEmpresaSedeId { get; set; }
    public int PeriodoId { get; set; }
    public int Anio { get; set; }
    public string Empresa { get; set; } = "";
    public string Sede { get; set; } = "";
    public DateTime Hoy { get; set; }
}

public class DocumentoReporteFila
{
    public int Id { get; set; }
    public int ProveedorId { get; set; }
    public string Serie { get; set; } = "";
    public string Numero { get; set; } = "";
    public DateTime Fecha { get; set; }
    public DateTime? Traslado { get; set; }
    public DateTime? Vencimiento { get; set; }
    public decimal Flete { get; set; }
    public string Cliente { get; set; } = "";
    public string DocumentoCliente { get; set; } = "";
    public string UbigeoPartida { get; set; } = "";
    public string PuntoPartida { get; set; } = "";
    public string EstablecimientoPartida { get; set; } = "";
    public string TipoReferencia { get; set; } = "";
    public string SerieReferencia { get; set; } = "";
    public string NumeroReferencia { get; set; } = "";
    public string Destinatario { get; set; } = "";
    public string DocumentoDestinatario { get; set; } = "";
    public string UbigeoLlegada { get; set; } = "";
    public string PuntoLlegada { get; set; } = "";
    public string EstablecimientoLlegada { get; set; } = "";
    public string TipoDocumentoConductor { get; set; } = "";
    public string DocumentoConductor { get; set; } = "";
    public string Placa { get; set; } = "";
    public string PlacaAdicional { get; set; } = "";
    public decimal Peso { get; set; }
    public string Observaciones { get; set; } = "";
    public string Motivo { get; set; } = "";
    public string Estado { get; set; } = "";
    public string Sunat { get; set; } = "";
    public string Orden { get; set; } = "";
    public string CodigoProveedor { get; set; } = "";
    public string Proveedor { get; set; } = "";
    public string DocumentoProveedor { get; set; } = "";
    public string Ruc { get; set; } = "";
    public string Moneda { get; set; } = "";
    public string Simbolo { get; set; } = "";
    public string CodigoDocumento { get; set; } = "";
    public string TipoDocumento { get; set; } = "";
    public string CodigoPago { get; set; } = "";
    public string CondicionPago { get; set; } = "";
    public decimal Bruto { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
}

public sealed record DocumentoReporteColumna(string Campo,string Titulo,Func<DocumentoReporteFila,object?> Valor,bool Numero=false,bool Fecha=false,bool Principal=true);

public static class DocumentoReporteColumnas
{
    public static List<DocumentoReporteColumna> Get(string tipo)=>tipo=="guias" ? new()
    {
        new("serie","Serie",r=>r.Serie),new("numero","Número",r=>r.Numero),new("fecha","Fecha",r=>r.Fecha,Fecha:true),new("traslado","Inicio de traslado",r=>r.Traslado,Fecha:true),
        new("cliente","Cliente",r=>r.Cliente),new("documentocliente","Documento cliente",r=>r.DocumentoCliente,Principal:false),
        new("destinatario","Destinatario",r=>r.Destinatario),new("documentodestinatario","Documento destinatario",r=>r.DocumentoDestinatario,Principal:false),
        new("ubigeopartida","Ubigeo de partida",r=>r.UbigeoPartida,Principal:false),new("puntopartida","Punto de partida",r=>r.PuntoPartida,Principal:false),new("establecimientopartida","Establecimiento de partida",r=>r.EstablecimientoPartida,Principal:false),
        new("ubigeollegada","Ubigeo de llegada",r=>r.UbigeoLlegada,Principal:false),new("puntollegada","Punto de llegada",r=>r.PuntoLlegada,Principal:false),new("establecimientollegada","Establecimiento de llegada",r=>r.EstablecimientoLlegada,Principal:false),
        new("tiporeferencia","Tipo de documento referido",r=>r.TipoReferencia,Principal:false),new("seriereferencia","Serie referida",r=>r.SerieReferencia,Principal:false),new("numeroreferencia","Número referido",r=>r.NumeroReferencia,Principal:false),
        new("tipodocumentoconductor","Tipo de documento conductor",r=>r.TipoDocumentoConductor,Principal:false),new("documentoconductor","Documento conductor",r=>r.DocumentoConductor,Principal:false),
        new("placa","Placa",r=>r.Placa),new("placaadicional","Placa adicional",r=>r.PlacaAdicional,Principal:false),
        new("flete","Costo de flete registrado",r=>r.Flete,Numero:true),new("peso","Peso bruto total",r=>r.Peso,Numero:true),new("motivo","Motivo del traslado",r=>r.Motivo),
        new("estado","Estado de guía",r=>r.Estado),new("sunat","Estado SUNAT",r=>r.Sunat),new("observaciones","Observaciones",r=>r.Observaciones,Principal:false)
    } : new()
    {
        new("orden","Orden de compra",r=>r.Orden),new("tipodocumento","Tipo de documento",r=>r.TipoDocumento),new("codigodocumento","Código tipo documento",r=>r.CodigoDocumento,Principal:false),
        new("serie","Serie",r=>r.Serie),new("numero","Número",r=>r.Numero),new("fecha","Fecha del comprobante",r=>r.Fecha,Fecha:true),new("vencimiento","Vencimiento",r=>r.Vencimiento,Fecha:true),
        new("codigoproveedor","Código proveedor",r=>r.CodigoProveedor,Principal:false),new("proveedor","Proveedor",r=>r.Proveedor),new("documentoproveedor","Documento proveedor",r=>r.DocumentoProveedor,Principal:false),new("ruc","RUC",r=>r.Ruc),
        new("moneda","Moneda",r=>r.Moneda),new("simbolo","Símbolo",r=>r.Simbolo,Principal:false),new("bruto","Bruto",r=>r.Bruto,Numero:true),new("impuesto","Impuesto",r=>r.Impuesto,Numero:true),new("total","Total",r=>r.Total,Numero:true),
        new("codigopago","Código condición de pago",r=>r.CodigoPago,Principal:false),new("condicionpago","Condición de pago",r=>r.CondicionPago)
    };

    public static string Format(DocumentoReporteColumna columna,DocumentoReporteFila fila)=>columna.Valor(fila) switch
    {
        DateTime fecha=>fecha.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture),
        decimal valor=>valor.ToString("N4",CultureInfo.GetCultureInfo("es-PE")),var valor=>valor?.ToString()??""
    };
}

public class DocumentoReportePageModel : ListadoPaginadoModel
{
    public string Tipo { get; set; } = "guias";
    public string Titulo=>Tipo=="guias"?"Guías de remisión":"Valorización de comprobantes de compra";
    public DocumentoReporteFiltro Filtro { get; set; } = new();
    public DocumentoReporteContexto Contexto { get; set; } = new();
    public List<LookupItemModel> Meses { get; set; } = new();
    public List<LookupItemModel> Proveedores { get; set; } = new();
    public List<DocumentoReporteFila> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();
    public bool Consultada { get; set; }
    public List<DocumentoReporteColumna> Columnas=>DocumentoReporteColumnas.Get(Tipo);
    public string Resumen=>string.Join(" · ",new[]{ $"Ejercicio {Contexto.Anio}",Tipo=="guias"?Contexto.Sede:"Todas las sedes de la empresa",
        Filtro.Modo=="rango"?$"{Filtro.Desde:dd/MM/yyyy} al {Filtro.Hasta:dd/MM/yyyy}":Meses.FirstOrDefault(m=>m.Id==Filtro.Mes)?.Nombre,
        Proveedores.FirstOrDefault(p=>p.Id==Filtro.ProveedorId)?.Nombre,string.IsNullOrWhiteSpace(Filtro.Search)?null:"Búsqueda: "+Filtro.Search }.Where(s=>!string.IsNullOrWhiteSpace(s)));
}
