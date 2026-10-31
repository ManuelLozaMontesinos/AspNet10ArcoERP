using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AspNet10ArcoERP.Models;

public class BancarizacionFiltro
{
    public int? ConsultaId { get; set; }
    [StringLength(150)] public string? Search { get; set; }
    [StringLength(8)] public string? Numero { get; set; }
    [StringLength(35)] public string? Identidad { get; set; }
    [StringLength(150)] public string? Cliente { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string? Bancarizado { get; set; }
}

public class BancarizacionDocumentoModel : MaestroEmpresaModel
{
    public int ConsultaId { get; set; }
    public int MonedaId { get; set; }
    public string Tipo { get; set; } = "";
    public string Documento { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string Cliente { get; set; } = "";
    public string Identidad { get; set; } = "";
    public string Moneda { get; set; } = "";
    public decimal TipoCambio { get; set; }
    public decimal Neto { get; set; }
    public decimal Igv { get; set; }
    public decimal Total { get; set; }
    public decimal TotalLocal { get; set; }
    public string Bancarizado { get; set; } = "";
    public string Pedido { get; set; } = "";
    public string Vendedor { get; set; } = "";
    public string ListaPrecio { get; set; } = "";
    public string CondicionPago { get; set; } = "";
    public string MedioPago { get; set; } = "";
    public string Observacion { get; set; } = "";
    public string NetoTexto=>Neto.ToString("G29",CultureInfo.InvariantCulture);
    public string IgvTexto=>Igv.ToString("G29",CultureInfo.InvariantCulture);
    public string TotalTexto=>Total.ToString("G29",CultureInfo.InvariantCulture);
    public string TipoCambioTexto=>TipoCambio.ToString("G29",CultureInfo.InvariantCulture);
    protected override string VersionExtra=>$"{ConsultaId}|{MonedaId}|{Fecha.Ticks}|{TipoCambio.ToString("G29",CultureInfo.InvariantCulture)}|{Total.ToString("G29",CultureInfo.InvariantCulture)}|{Bancarizado}";
}

public class BancarizacionPageModel : ListadoPaginadoModel
{
    public ConsultaSedeContextoModel Contexto { get; set; } = new();
    public BancarizacionFiltro Filtro { get; set; } = new();
    public decimal Umbral { get; set; }
    public List<BancarizacionDocumentoModel> Items { get; set; } = new();
}

public class BancarizacionDetalleModel
{
    public BancarizacionDocumentoModel Datos { get; set; } = new();
    public List<LookupItemModel> Cuentas { get; set; } = new();
    public string Sede { get; set; } = "";
    public DateTime FechaNueva { get; set; } = DateTime.Today;
}

public class BancarizacionCobranzaModel : MaestroEmpresaModel
{
    public int ComprobanteId { get; set; }
    public int CuentaId { get; set; }
    public DateTime Fecha { get; set; }
    public string Operacion { get; set; } = "";
    public decimal Monto { get; set; }
    public string MontoTexto=>Monto.ToString("G29",CultureInfo.InvariantCulture);
    public string MontoVisible=>Monto.ToString("N4",CultureInfo.GetCultureInfo("es-PE"));
    public string Cuenta { get; set; } = "";
    protected override string VersionExtra=>$"{ComprobanteId}|{CuentaId}|{Fecha.Ticks}|{Operacion}|{MontoTexto}";
}

public class BancarizacionCobranzaPageModel : ListadoPaginadoModel
{
    public List<BancarizacionCobranzaModel> Items { get; set; } = new();
}

public class BancarizacionCobranzaSolicitud
{
    [Range(1,int.MaxValue)] public int ContextoId { get; set; }
    [Range(1,int.MaxValue)] public int ConsultaId { get; set; }
    [Range(1,int.MaxValue)] public int ComprobanteId { get; set; }
    [Range(0,int.MaxValue)] public int Id { get; set; }
    [Required,StringLength(64,MinimumLength=64)] public string DocumentoRevision { get; set; } = "";
    [StringLength(64)] public string? Revision { get; set; }
    [Required] public DateTime? Fecha { get; set; }
    [Required,StringLength(50)] public string Operacion { get; set; } = "";
    [Required,Range(typeof(decimal),"0.0001","999999999999999.9999")] public decimal? Monto { get; set; }
    [Range(1,int.MaxValue)] public int CuentaId { get; set; }
}

public class BancarizacionAnularSolicitud
{
    [Range(1,int.MaxValue)] public int ContextoId { get; set; }
    [Range(1,int.MaxValue)] public int ConsultaId { get; set; }
    [Range(1,int.MaxValue)] public int ComprobanteId { get; set; }
    [Range(1,int.MaxValue)] public int Id { get; set; }
    [Required,StringLength(64,MinimumLength=64)] public string DocumentoRevision { get; set; } = "";
    [Required,StringLength(64,MinimumLength=64)] public string Revision { get; set; } = "";
}
