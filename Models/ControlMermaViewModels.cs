using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AspNet10ArcoERP.Models;

public class ControlMermaItemModel : MaestroEmpresaModel
{
    public int PeriodoEmpresaSedeId { get; set; }
    public string Codigo { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string FechaTexto => Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public int TipoMovimientoAlmacenId { get; set; }
    public string TipoMovimientoAlmacenCodigo { get; set; } = "";
    public string TipoMovimientoAlmacenNombre { get; set; } = "";

    public int TransaccionAlmacenId { get; set; }
    public string TransaccionAlmacenCodigo { get; set; } = "";
    public string TransaccionAlmacenNombre { get; set; } = "";

    public int AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = "";

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

    public bool EsIngreso => TipoMovimientoAlmacenId == 1 || TipoMovimientoAlmacenCodigo == "I";
    public bool EsSalida => TipoMovimientoAlmacenId == 2 || TipoMovimientoAlmacenCodigo == "S";
    public bool EsPendienteAprobacion => EstadoAprobacionCodigo == "01" || EstadoAprobacionId == 1;
    public bool EsAprobado => EstadoAprobacionCodigo == "02" || EstadoAprobacionId == 2;
    public bool EsDesaprobado => EstadoAprobacionCodigo == "03" || EstadoAprobacionId == 3;
    public bool EsAnulado => EstadoMovimientoAlmacenId == 3 || Estado == "I";

    protected override string VersionExtra => $"{PeriodoEmpresaSedeId}|{Codigo}|{AlmacenId}|{TipoMovimientoAlmacenId}|{EstadoAprobacionId}|{EstadoMovimientoAlmacenId}";
}

public class ControlMermaPageModel : ListadoPaginadoModel
{
    public List<ControlMermaItemModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();

    public string? Search { get; set; }
    public string? Codigo { get; set; }
    public int? AlmacenId { get; set; }
    public int? TipoMovimientoAlmacenId { get; set; }
    public int? EstadoAprobacionId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }

    public List<LookupItemModel> Almacenes { get; set; } = new();
    public List<LookupItemModel> TiposMovimiento { get; set; } = new();
    public List<LookupItemModel> EstadosAprobacion { get; set; } = new();
}

public class ControlMermaGuardarSolicitud
{
    public int Id { get; set; }

    [Required]
    public int ContextoId { get; set; }

    public string? Revision { get; set; }

    [Required]
    public int TipoMovimientoAlmacenId { get; set; }

    [Required]
    public int AlmacenId { get; set; }

    [Required]
    public DateTime Fecha { get; set; } = DateTime.Today;

    public string? Observacion { get; set; }
}

public class ControlMermaAutoPageModel : ListadoPaginadoModel
{
    public List<ControlMermaItemModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();

    public string? Search { get; set; }
    public string? Codigo { get; set; }
    public int? AlmacenId { get; set; }
    public int? TipoMovimientoAlmacenId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }

    public List<LookupItemModel> Almacenes { get; set; } = new();
    public List<LookupItemModel> TiposMovimiento { get; set; } = new();
}

public class AprobarControlMermaSolicitud
{
    [Required]
    public int Id { get; set; }

    [Required]
    public int ContextoId { get; set; }

    public string? Revision { get; set; }
}

public class DesaprobarControlMermaSolicitud
{
    [Required]
    public int Id { get; set; }

    [Required]
    public int ContextoId { get; set; }

    public string? Revision { get; set; }
}

