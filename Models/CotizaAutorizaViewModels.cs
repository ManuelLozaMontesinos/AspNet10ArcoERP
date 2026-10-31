using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace AspNet10ArcoERP.Models;

public class CotizaAutorizaItemModel : MaestroEmpresaModel
{
    public int CotizacionAutorizacionId { get; set; }
    public int CotizacionId { get; set; }
    public string? EmpresaSedeNombre { get; set; }
    public string? TipoAutorizacionCodigo { get; set; }
    public string? TipoAutorizacionNombre { get; set; }
    public string? Codigo { get; set; }
    public DateTime? Fecha { get; set; }
    public string? FechaTexto => Fecha?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    public string? ClienteNombre { get; set; }
    public string? ClienteDocumento { get; set; }
    public string? VendedorNombre { get; set; }
    public string? MonedaSimbolo { get; set; }
    public decimal MontoTotal { get; set; }
    public string MontoTotalTexto => MontoTotal.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));
    public int EstadoAprobacionId { get; set; }
    public string? EstadoAprobacionCodigo { get; set; }
    public string? EstadoAprobacionNombre { get; set; }

    protected override string VersionExtra => $"{CotizacionAutorizacionId}|{CotizacionId}|{EstadoAprobacionId}|{MontoTotal}";
}

public sealed class CotizaAutorizaPageModel : ListadoPaginadoModel
{
    public List<CotizaAutorizaItemModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();

    public string? Search { get; set; }
    public string? Codigo { get; set; }
    public string? Cliente { get; set; }
    public string? Vendedor { get; set; }
    public int? EstadoAprobacionId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public List<LookupItemModel> EstadosAprobacion { get; set; } = new();
}

public sealed class CotizaAutorizaProductoItemModel
{
    public int Id { get; set; }
    public int CotizacionProductoId { get; set; }
    public int CotizacionId { get; set; }
    public int ProductoId { get; set; }
    public string? ProductoCodigo { get; set; }
    public string? ProductoNombre { get; set; }
    public string? UnidadMedidaCodigo { get; set; }
    public string? Descripcion { get; set; }
    public decimal Cantidad { get; set; }
    public decimal ValorUnitarioLista { get; set; }
    public decimal PrecioUnitarioLista { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal PrecioAutorizado { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public decimal MontoNeto { get; set; }
    public decimal MontoIgv { get; set; }
    public decimal MontoTotal { get; set; }
    public string? TipoIgv { get; set; }
    public string? SolicitaAutorizacionDescuento { get; set; }
    public string? IncluyeDescuentoListaPrecio { get; set; }
    public string? SinSaldo { get; set; }
    public string? Estado { get; set; }
}

public sealed class CotizaAutorizaDetalleModel
{
    public int Id { get; set; }
    public int CotizacionAutorizacionId { get; set; }
    public int CotizacionId { get; set; }
    public string? Revision { get; set; }
    public string? TipoAutorizacionCodigo { get; set; }
    public string? TipoAutorizacionNombre { get; set; }
    public int EstadoAprobacionId { get; set; }
    public string? EstadoAprobacionCodigo { get; set; }
    public string? EstadoAprobacionNombre { get; set; }
    public string? Codigo { get; set; }
    public DateTime? Fecha { get; set; }
    public string? FechaTexto { get; set; }
    public int ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public string? ClienteDocumento { get; set; }
    public int VendedorId { get; set; }
    public string? VendedorNombre { get; set; }
    public int MonedaId { get; set; }
    public string? MonedaNombre { get; set; }
    public string? MonedaSimbolo { get; set; }
    public int TipoCondicionPagoId { get; set; }
    public string? TipoCondicionPagoNombre { get; set; }
    public int ListaPrecioId { get; set; }
    public string? ListaPrecioNombre { get; set; }
    public string? Observacion { get; set; }
    public decimal TipoCambio { get; set; }
    public decimal ImpuestoTasa { get; set; }
    public decimal MontoBruto { get; set; }
    public decimal MontoDctoItem { get; set; }
    public decimal MontoNeto { get; set; }
    public decimal MontoIgv { get; set; }
    public decimal MontoTotal { get; set; }
    public string? Estado { get; set; }
    public IReadOnlyList<CotizaAutorizaProductoItemModel> Productos { get; set; } = Array.Empty<CotizaAutorizaProductoItemModel>();
}

public sealed class AprobarCotizacionAutorizacionItemSolicitud
{
    public int CotizacionProductoId { get; set; }
    public decimal PrecioAutorizado { get; set; }
}

public sealed class AprobarCotizacionAutorizacionSolicitud
{
    [Required]
    public int ContextoId { get; set; }

    [Required]
    public int CotizacionAutorizacionId { get; set; }

    [Required]
    public int CotizacionId { get; set; }

    public string? Revision { get; set; }

    public List<AprobarCotizacionAutorizacionItemSolicitud> Items { get; set; } = new();
}

public sealed class DesaprobarCotizacionAutorizacionSolicitud
{
    [Required]
    public int ContextoId { get; set; }

    [Required]
    public int CotizacionAutorizacionId { get; set; }

    [Required]
    public int CotizacionId { get; set; }

    public string? Revision { get; set; }

    [MaxLength(400)]
    public string? Observacion { get; set; }
}
