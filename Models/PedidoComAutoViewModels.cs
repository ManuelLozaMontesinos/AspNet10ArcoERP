using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AspNet10ArcoERP.Models;

public class PedidoComAutoItemModel : MaestroEmpresaModel
{
    public int PeriodoEmpresaSedeId { get; set; }
    public string Codigo { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string FechaTexto => Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public int TipoProductoId { get; set; }
    public string TipoProductoNombre { get; set; } = "";

    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = "";
    public string ProveedorDocumento { get; set; } = "";

    public string Observacion { get; set; } = "";
    public string EsModificacion { get; set; } = "N";

    public int? EstadoAprobacionId { get; set; }
    public string? EstadoAprobacionCodigo { get; set; }
    public string? EstadoAprobacionNombre { get; set; }

    public int EstadoPedidoCompraId { get; set; }
    public string EstadoPedidoCompraCodigo { get; set; } = "";
    public string EstadoPedidoCompraNombre { get; set; } = "";

    public int CantidadItems { get; set; }

    protected override string VersionExtra => $"{PeriodoEmpresaSedeId}|{Codigo}|{ProveedorId}|{TipoProductoId}|{EstadoPedidoCompraId}|{EstadoAprobacionId}";
}

public class PedidoComAutoPageModel : ListadoPaginadoModel
{
    public List<PedidoComAutoItemModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();

    public string? Search { get; set; }
    public string? Codigo { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public int? TipoProductoId { get; set; }
    public int? EstadoAprobacionId { get; set; }

    public List<LookupItemModel> TiposProducto { get; set; } = new();
    public List<LookupItemModel> EstadosAprobacion { get; set; } = new();
}

public class PedidoComAutoDetalleModel
{
    public PedidoComAutoItemModel Cabecera { get; set; } = new();
    public List<PedidoCompraProductoItemModel> Productos { get; set; } = new();
}

public class AprobarPedidoComAutoSolicitud
{
    [Required]
    public int Id { get; set; }

    [Required]
    public int ContextoId { get; set; }

    public string? Revision { get; set; }
}

public class DesaprobarPedidoComAutoSolicitud
{
    [Required]
    public int Id { get; set; }

    [Required]
    public int ContextoId { get; set; }

    public string? Revision { get; set; }
}

