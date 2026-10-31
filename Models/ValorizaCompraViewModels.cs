using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AspNet10ArcoERP.Models;

public class ValorizaCompraItemModel : MaestroEmpresaModel
{
    public int PeriodoEmpresaSedeId { get; set; }
    public int OrdenCompraId { get; set; }
    public string OrdenCompraCodigo { get; set; } = "";

    public int TipoDocumentoComercialId { get; set; }
    public string TipoDocumentoComercialCodigo { get; set; } = "";
    public string TipoDocumentoComercialNombre { get; set; } = "";
    public string TipoDocumentoComercialAbrev { get; set; } = "";

    public string Serie { get; set; } = "";
    public string Numero { get; set; } = "";
    public string DocumentoCompleto => $"{Serie}-{Numero}";

    public DateTime Fecha { get; set; }
    public string FechaTexto => Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public DateTime? FechaVencimiento { get; set; }
    public string? FechaVencimientoTexto => FechaVencimiento?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = "";
    public string ProveedorDocumento { get; set; } = "";

    public string MonedaSimbolo { get; set; } = "S/";
    public decimal MontoBruto { get; set; }
    public decimal MontoImpuesto { get; set; }
    public decimal MontoTotal { get; set; }

    public string MontoBrutoTexto => MontoBruto.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));
    public string MontoImpuestoTexto => MontoImpuesto.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));
    public string MontoTotalTexto => MontoTotal.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));

    public int? CondicionPagoId { get; set; }
    public string? CondicionPagoNombre { get; set; }

    public int? RegistroReferencialId { get; set; }

    protected override string VersionExtra => $"{OrdenCompraId}|{TipoDocumentoComercialId}|{Serie}|{Numero}|{MontoTotal}";
}

public class ValorizaCompraPageModel : ListadoPaginadoModel
{
    public List<ValorizaCompraItemModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();

    public string? Search { get; set; }
    public string? OrdenCompraCodigo { get; set; }
    public string? Serie { get; set; }
    public string? Numero { get; set; }
    public int? TipoDocumentoComercialId { get; set; }
    public int? CondicionPagoId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }

    public List<LookupItemModel> TiposDocumentoComercial { get; set; } = new();
    public List<LookupItemModel> CondicionesPago { get; set; } = new();
}

public class ValorizaCompraGuardarSolicitud
{
    public int Id { get; set; }

    [Required]
    public int ContextoId { get; set; }

    public string? Revision { get; set; }

    [Required]
    public int OrdenCompraId { get; set; }

    [Required]
    public int TipoDocumentoComercialId { get; set; }

    [Required]
    [MaxLength(4)]
    public string Serie { get; set; } = "";

    [Required]
    [MaxLength(8)]
    public string Numero { get; set; } = "";

    [Required]
    public DateTime Fecha { get; set; } = DateTime.Today;

    public DateTime? FechaVencimiento { get; set; }

    [Required]
    public decimal MontoTotal { get; set; }

    public decimal MontoBruto { get; set; }
    public decimal MontoImpuesto { get; set; }

    public int? CondicionPagoId { get; set; }
}

