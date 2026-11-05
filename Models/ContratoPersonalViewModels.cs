namespace AspNet10ArcoERP.Models;

public class ContratoPersonalItemModel
{
    public int PersonalId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public int? TipoContratoId { get; set; }
    public string TipoContratoCodigo { get; set; } = string.Empty;
    public string TipoContratoNombre { get; set; } = string.Empty;
    public DateTime? FechaIngreso { get; set; }
    public DateTime? FechaCese { get; set; }
    public string Estado { get; set; } = "A";
}

public class ContratoPersonalPageModel : ListadoPaginadoModel
{
    public string? Search { get; set; }
    public int? TipoContratoId { get; set; }
    public List<LookupItemModel> TiposContrato { get; set; } = new();
    public List<ContratoPersonalItemModel> Items { get; set; } = new();
}

public class ActualizarContratoSolicitud
{
    public int PersonalId { get; set; }
    public int TipoContratoId { get; set; }
    public DateTime? FechaCese { get; set; }
}

public class TRegistroExportModel
{
    public string Periodo { get; set; } = string.Empty;
    public int TotalTrabajadores { get; set; }
    public List<ContratoPersonalItemModel> Trabajadores { get; set; } = new();
}

