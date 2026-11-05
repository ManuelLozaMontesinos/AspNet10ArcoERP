using System.ComponentModel.DataAnnotations;

namespace AspNet10ArcoERP.Models;

public class DespachoItemModel : MaestroEmpresaModel
{
    public int ComprobanteId { get; set; }
    public string TipoDocumento { get; set; } = string.Empty;
    public string SerieNumero { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public string CotizacionPedido { get; set; } = string.Empty;
    public string ColorLista { get; set; } = string.Empty;
    public int? EstadoDespachoId { get; set; }
    public string EstadoDespachoCodigo { get; set; } = string.Empty;
    public string EstadoDespachoNombre { get; set; } = string.Empty;
    public string PersonalDespacho { get; set; } = string.Empty;
    public DateTime? FechaDespacho { get; set; }
    public string ObservacionDespacho { get; set; } = string.Empty;
    public string EntregaDespacho { get; set; } = string.Empty;

    protected override string VersionExtra => $"{ComprobanteId}|{EstadoDespachoId}|{FechaDespacho:yyyyMMddHHmmss}|{ObservacionDespacho}";
}

public class DespachoDetalleItemModel : MaestroEmpresaModel
{
    public int ComprobanteProductoId { get; set; }
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string UnidadMedida { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal CantidadEntregada { get; set; }
    public decimal CantidadPendiente => Math.Max(0, Cantidad - CantidadEntregada);
    public DateTime? FechaDespacho { get; set; }
    public string ObservacionDespacho { get; set; } = string.Empty;
    public string PersonalDespacho { get; set; } = string.Empty;
    public string EntregaDespacho { get; set; } = string.Empty;

    protected override string VersionExtra => $"{ComprobanteProductoId}|{CantidadEntregada}|{FechaDespacho:yyyyMMddHHmmss}";
}

public class DespachoPageModel : ListadoPaginadoModel
{
    public string? Search { get; set; }
    public string? Codigo { get; set; }
    public string? Cotizacion { get; set; }
    public string? Cliente { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public int? EstadoDespachoId { get; set; }
    public List<LookupItemModel> EstadosDespacho { get; set; } = new();
    public List<DespachoItemModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();
}

public class DespachoDetalleModel
{
    public DespachoItemModel Cabecera { get; set; } = new();
    public List<DespachoDetalleItemModel> Items { get; set; } = new();
}

public class DespachoEntregaSolicitud
{
    public int ContextoId { get; set; }
    public int ComprobanteId { get; set; }
    public int ComprobanteProductoId { get; set; }
    [Range(0.0001, 99999999)]
    public decimal Cantidad { get; set; }
    public string? Observacion { get; set; }
    public int? PersonalId { get; set; }
}

public class DespachoCerrarSolicitud
{
    public int ContextoId { get; set; }
    public int ComprobanteId { get; set; }
    public string? Observacion { get; set; }
}
