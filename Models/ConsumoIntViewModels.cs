using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AspNet10ArcoERP.Models;

public class ConsumoIntItemModel : MaestroEmpresaModel
{
    public int PeriodoEmpresaSedeId { get; set; }
    public string Codigo { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string FechaTexto => Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public int AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = "";

    public int CentroCostoId { get; set; }
    public string CentroCostoNombre { get; set; } = "";
    public string CentroCostoSigla { get; set; } = "";

    public int PersonalId { get; set; }
    public string PersonalNombre { get; set; } = "";
    public string PersonalDocumento { get; set; } = "";

    public int? EstadoAprobacionId { get; set; }
    public string? EstadoAprobacionCodigo { get; set; }
    public string? EstadoAprobacionNombre { get; set; }

    public int? PersonalAutorizaId { get; set; }
    public string? PersonalAutorizaNombre { get; set; }
    public DateTime? FechaAutorizacion { get; set; }
    public string? FechaAutorizacionTexto => FechaAutorizacion?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public string Observacion { get; set; } = "";

    public int EstadoMovimientoAlmacenId { get; set; }
    public string EstadoMovimientoAlmacenNombre { get; set; } = "";

    public string UsuarioLogon { get; set; } = "";
    public int CantidadItems { get; set; }
    public decimal MontoTotal { get; set; }
    public string MontoTotalTexto => MontoTotal.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));

    public bool EsPendienteAprobacion => EstadoAprobacionCodigo == "01" || EstadoAprobacionId == 1;
    public bool EsAprobado => EstadoAprobacionCodigo == "02" || EstadoAprobacionId == 2;
    public bool EsDesaprobado => EstadoAprobacionCodigo == "03" || EstadoAprobacionId == 3;
    public bool EsAnulado => EstadoMovimientoAlmacenId == 3 || Estado == "I";

    protected override string VersionExtra => $"{PeriodoEmpresaSedeId}|{Codigo}|{AlmacenId}|{CentroCostoId}|{PersonalId}|{EstadoAprobacionId}|{EstadoMovimientoAlmacenId}";
}

public class ConsumoIntPageModel : ListadoPaginadoModel
{
    public List<ConsumoIntItemModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();

    public string? Search { get; set; }
    public string? Codigo { get; set; }
    public int? AlmacenId { get; set; }
    public int? CentroCostoId { get; set; }
    public int? PersonalId { get; set; }
    public int? EstadoAprobacionId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }

    public List<LookupItemModel> Almacenes { get; set; } = new();
    public List<LookupItemModel> CentrosCosto { get; set; } = new();
    public List<LookupItemModel> Personal { get; set; } = new();
    public List<LookupItemModel> EstadosAprobacion { get; set; } = new();
}

public class ConsumoIntGuardarSolicitud
{
    public int Id { get; set; }

    [Required]
    public int ContextoId { get; set; }

    public string? Revision { get; set; }

    [Required]
    public int AlmacenId { get; set; }

    [Required]
    public int CentroCostoId { get; set; }

    [Required]
    public int PersonalId { get; set; }

    [Required]
    public DateTime Fecha { get; set; } = DateTime.Today;

    public string? Observacion { get; set; }
}

public class ConsumoIntAutoPageModel : ListadoPaginadoModel
{
    public List<ConsumoIntItemModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();

    public string? Search { get; set; }
    public string? Codigo { get; set; }
    public int? AlmacenId { get; set; }
    public int? CentroCostoId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }

    public List<LookupItemModel> Almacenes { get; set; } = new();
    public List<LookupItemModel> CentrosCosto { get; set; } = new();
}

public class AprobarConsumoIntSolicitud
{
    [Required]
    public int Id { get; set; }

    [Required]
    public int ContextoId { get; set; }

    public string? Revision { get; set; }
}

public class DesaprobarConsumoIntSolicitud
{
    [Required]
    public int Id { get; set; }

    [Required]
    public int ContextoId { get; set; }

    public string? Revision { get; set; }
}

