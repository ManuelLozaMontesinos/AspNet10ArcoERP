using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AspNet10ArcoERP.Models;

public class ParametroSistemaModel : MaestroEmpresaModel
{
    public int GrupoId { get; set; }
    public string Grupo { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string TipoNombre { get; set; } = "";
    public bool TieneMantenimiento { get; set; }
    public string? Cadena { get; set; }
    public decimal Numero { get; set; }
    public string NumeroTexto=>Numero.ToString("G29",CultureInfo.InvariantCulture);
    public decimal RangoDesde { get; set; }
    public decimal RangoHasta { get; set; }
    public bool Editable=>TieneMantenimiento&&Estado=="A"&&Tipo is "01" or "02" or "03" or "07" or "08";
    public string HoraDesde=>Hora(RangoDesde);
    public string HoraHasta=>Hora(RangoHasta);
    public string Valor=>Tipo switch {"01"=>Cadena??"","07"=>"••••••••","02"=>Numero.ToString("N0",CultureInfo.GetCultureInfo("es-PE")),"03"=>Numero.ToString("N4",CultureInfo.GetCultureInfo("es-PE")),"08"=>HoraDesde+" – "+HoraHasta,_=>""};
    private static string Hora(decimal value)=>value.ToString("00.00",CultureInfo.InvariantCulture).Replace('.',':');
    protected override string VersionExtra=>$"{GrupoId}|{Codigo}|{Nombre}|{Tipo}|{TieneMantenimiento}";
}

public class ParametroSistemaPageModel : ListadoPaginadoModel
{
    public List<ParametroSistemaModel> Items { get; set; } = new();
    public List<LookupItemModel> Grupos { get; set; } = new();
    public List<string> Errores { get; set; } = new();
    public string? Search { get; set; }
    public int? GrupoId { get; set; }
    public string? Estado { get; set; }
}

public class ParametroSistemaSolicitud
{
    [Range(1,int.MaxValue)] public int ContextoId { get; set; }
    [Range(1,int.MaxValue)] public int Id { get; set; }
    [Required,StringLength(64,MinimumLength=64)] public string Revision { get; set; } = "";
    [StringLength(100)] public string? Cadena { get; set; }
    public decimal? Numero { get; set; }
    [StringLength(5)] public string? Desde { get; set; }
    [StringLength(5)] public string? Hasta { get; set; }
    public bool CambiarSecreto { get; set; }
}
