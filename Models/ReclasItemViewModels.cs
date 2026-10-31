using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AspNet10ArcoERP.Models;

public class ReclasItemModel : MaestroEmpresaModel
{
    public int PeriodoEmpresaSedeId { get; set; }
    public string Codigo { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string FechaTexto => Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public int MesContableId { get; set; }
    public int MovimientoAlmacenId { get; set; }
    public string MovimientoAlmacenCodigo { get; set; } = "";

    public int AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = "";

    public string ClienteProveedorNombre { get; set; } = "";
    public string Observacion { get; set; } = "";

    public int EstadoMovimientoReclasificacionId { get; set; }
    public string EstadoMovimientoReclasificacionCodigo { get; set; } = "";
    public string EstadoMovimientoReclasificacionNombre { get; set; } = "";

    public int? UsuarioAutorizaId { get; set; }
    public string? UsuarioAutorizaNombre { get; set; }
    public DateTime? FechaAutorizacion { get; set; }
    public string? FechaAutorizacionTexto => FechaAutorizacion?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public int CantidadItems { get; set; }

    public bool EsBorrador => EstadoMovimientoReclasificacionId == 1;
    public bool EsPendienteAprobacion => EstadoMovimientoReclasificacionId == 5;
    public bool EsAprobado => EstadoMovimientoReclasificacionId == 2;
    public bool EsDesaprobado => EstadoMovimientoReclasificacionId == 4;
    public bool EsAnulado => EstadoMovimientoReclasificacionId == 3 || Estado == "I";

    protected override string VersionExtra => $"{PeriodoEmpresaSedeId}|{Codigo}|{AlmacenId}|{MovimientoAlmacenId}|{EstadoMovimientoReclasificacionId}";
}

public class ReclasItemProductoModel
{
    public int MovimientoAlmacenProductoId { get; set; }
    public int MovimientoAlmacenId { get; set; }

    public int ProductoOrigenId { get; set; }
    public string ProductoOrigenCodigo { get; set; } = "";
    public string ProductoOrigenNombre { get; set; } = "";
    public string UnidadMedidaOrigen { get; set; } = "";
    public decimal CantidadOrigen { get; set; }

    public decimal ValorUnitario { get; set; }

    public int? ProductoDestinoId { get; set; }
    public string? ProductoDestinoCodigo { get; set; }
    public string? ProductoDestinoNombre { get; set; }
    public string? UnidadMedidaDestino { get; set; }
    public decimal? CantidadDestino { get; set; }

    public bool TieneReclasificacion => ProductoDestinoId.HasValue && ProductoDestinoId.Value > 0 && CantidadDestino.HasValue && CantidadDestino.Value > 0;
}

public class ReclasItemDetalleModel : ReclasItemModel
{
    public List<ReclasItemProductoModel> Productos { get; set; } = new();
}

public class ReclasItemPageModel : ListadoPaginadoModel
{
    public List<ReclasItemModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();

    public string? Search { get; set; }
    public string? Codigo { get; set; }
    public int? AlmacenId { get; set; }
    public int? EstadoId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }

    public List<LookupItemModel> Almacenes { get; set; } = new();
    public List<LookupItemModel> Estados { get; set; } = new();
}

public class ReclasItemGuardarSolicitud
{
    public int Id { get; set; }

    [Required]
    public int ContextoId { get; set; }

    public string? Revision { get; set; }

    [Required]
    public int AlmacenId { get; set; }

    [Required]
    public int MovimientoAlmacenId { get; set; }

    [Required]
    public DateTime Fecha { get; set; } = DateTime.Today;

    public string? Observacion { get; set; }
}

public class ReclasItemProductoGuardarSolicitud
{
    [Required]
    public int MovimientoAlmacenProductoId { get; set; }

    [Required]
    public int ContextoId { get; set; }

    [Required]
    public int ProductoDestinoId { get; set; }

    [Required]
    public decimal CantidadDestino { get; set; }
}

public class ReclasItemAutoPageModel : ListadoPaginadoModel
{
    public List<ReclasItemModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();

    public string? Search { get; set; }
    public string? Codigo { get; set; }
    public int? AlmacenId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }

    public List<LookupItemModel> Almacenes { get; set; } = new();
}

public class AprobarReclasItemSolicitud
{
    [Required]
    public int Id { get; set; }

    [Required]
    public int ContextoId { get; set; }

    public string? Revision { get; set; }
}

public class DesaprobarReclasItemSolicitud
{
    [Required]
    public int Id { get; set; }

    [Required]
    public int ContextoId { get; set; }

    public string? Revision { get; set; }
}

