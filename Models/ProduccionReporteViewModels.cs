using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AspNet10ArcoERP.Models;

public class ProduccionReporteFiltro
{
    public string Vista { get; set; } = "estadistico";
    public string Modo { get; set; } = "mes";
    public int? Mes { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    [StringLength(150)] public string? Search { get; set; }
    public bool Consultar { get; set; }
    public Dictionary<string,string> Query(string sort,string dir)=>new()
    {
        ["vista"]=Vista,["modo"]=Modo,["mes"]=Mes?.ToString(CultureInfo.InvariantCulture)??"",
        ["desde"]=Desde?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)??"",["hasta"]=Hasta?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)??"",
        ["search"]=Search??"",["consultar"]="true",["sort"]=sort,["dir"]=dir
    };
}

public class ProduccionReporteFila
{
    public int Id { get; set; }
    public int Grupo { get; set; }
    public DateTime? Fecha { get; set; }
    public string? Maquina { get; set; }
    public string Operador { get; set; } = "";
    public decimal? Piezas { get; set; }
    public decimal? Planchas { get; set; }
    public decimal? Promedio { get; set; }
    public string Pedido { get; set; } = "";
    public string Proceso { get; set; } = "";
    public DateTime? FechaEntrega { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public int? Interrupciones { get; set; }
    public string? Observacion { get; set; }
    public string Estado { get; set; } = "";
}

public sealed record ProduccionReporteColumna(string Campo,string Titulo,Func<ProduccionReporteFila,object?> Valor,bool Numero=false,bool Fecha=false,bool Hora=false);

public class ProduccionReportePageModel : ListadoPaginadoModel
{
    public ProduccionReporteFiltro Filtro { get; set; } = new();
    public ConsultaSedeContextoModel Contexto { get; set; } = new();
    public int ContextoId { get; set; }
    public List<LookupItemModel> Meses { get; set; } = new();
    public List<ProduccionReporteFila> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();
    public bool Consultada { get; set; }
    public string Resumen=>string.Join(" · ",new[]{$"Ejercicio {Contexto.Anio}",Contexto.NombreSede(ContextoId),Filtro.Vista=="detallado"?"Detallado":"Estadístico",
        Filtro.Modo=="rango"?$"{Filtro.Desde:dd/MM/yyyy} al {Filtro.Hasta:dd/MM/yyyy}":Meses.FirstOrDefault(m=>m.Id==Filtro.Mes)?.Nombre,
        string.IsNullOrWhiteSpace(Filtro.Search)?null:"Búsqueda: "+Filtro.Search}.Where(s=>!string.IsNullOrWhiteSpace(s)));
    public string Nota=>Filtro.Vista=="detallado"
        ? "Operaciones terminadas de corte lineal de melamina, aglomerado y alta gama. Las piezas y planchas conservan la clasificación del reporte detallado. Una celda vacía indica que no existe una cantidad de origen."
        : "Operaciones terminadas agrupadas por día de inicio, máquina y operador. Melamina y alta gama se agrupan juntas; aglomerado se calcula por separado. El promedio es piezas / planchas y queda vacío si no hay planchas o son cero. No se suman promedios entre filas.";
    public string ResumenSalida=>Resumen+" · "+Nota;
    public List<ProduccionReporteColumna> Columnas=>Filtro.Vista=="detallado"?new()
    {
        new("pedido","Pedido",r=>r.Pedido),new("fecha","Fecha pedido",r=>r.Fecha,false,true),new("operador","Operador",r=>r.Operador),
        new("proceso","Proceso",r=>r.Proceso),new("maquina","Máquina",r=>r.Maquina),new("fechaentrega","Entrega",r=>r.FechaEntrega,false,true,true),
        new("fechainicio","Inicio",r=>r.FechaInicio,false,true,true),new("fechafin","Fin",r=>r.FechaFin,false,true,true),new("interrupciones","Interrupciones",r=>r.Interrupciones,true),
        new("observacion","Observación",r=>r.Observacion),new("piezas","Piezas",r=>r.Piezas,true),new("planchas","Planchas",r=>r.Planchas,true),new("estado","Estado",r=>r.Estado)
    }:new()
    {
        new("fecha","Fecha inicio",r=>r.Fecha,false,true),new("maquina","Máquina",r=>r.Maquina),new("operador","Operador",r=>r.Operador),
        new("piezas","Piezas",r=>r.Piezas,true),new("planchas","Planchas",r=>r.Planchas,true),new("promedio","Promedio",r=>r.Promedio,true)
    };
    public static string Formato(ProduccionReporteColumna column,ProduccionReporteFila row)=>column.Valor(row) switch
    {
        DateTime date=>date.ToString(column.Hora?"dd/MM/yyyy HH:mm:ss":"dd/MM/yyyy"),decimal value=>value.ToString(column.Campo=="promedio"?"N2":"N4",CultureInfo.GetCultureInfo("es-PE")),var value=>value?.ToString()??""
    };
}
