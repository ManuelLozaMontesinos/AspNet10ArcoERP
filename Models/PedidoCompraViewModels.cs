using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AspNet10ArcoERP.Models;

public class PedidoCompraItemModel : MaestroEmpresaModel
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

public class PedidoCompraPageModel : ListadoPaginadoModel
{
    public List<PedidoCompraItemModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();

    public string? Search { get; set; }
    public string? Codigo { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public int? TipoProductoId { get; set; }
    public int? EstadoPedidoCompraId { get; set; }
    public int? EstadoAprobacionId { get; set; }

    public List<LookupItemModel> TiposProducto { get; set; } = new();
    public List<LookupItemModel> EstadosPedidoCompra { get; set; } = new();
    public List<LookupItemModel> EstadosAprobacion { get; set; } = new();
}

public class PedidoCompraProductoItemModel : MaestroEmpresaModel
{
    public int PedidoCompraId { get; set; }
    public int? ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string UnidadMedida { get; set; } = "";
    public decimal Cantidad { get; set; }
    public decimal PesoNeto { get; set; }
    public decimal PesoBruto { get; set; }
    public string StockEnProveedor { get; set; } = "N";
    public string EsAdicional { get; set; } = "N";

    public string CantidadTexto => Cantidad.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));
    public string PesoNetoTexto => PesoNeto.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));

    protected override string VersionExtra => $"{PedidoCompraId}|{ProductoId}|{Cantidad}|{StockEnProveedor}";
}

public class PedidoCompraDetalleModel
{
    public PedidoCompraItemModel Cabecera { get; set; } = new();
    public List<PedidoCompraProductoItemModel> Productos { get; set; } = new();
    public List<LookupItemModel> TiposProducto { get; set; } = new();
    public List<LookupItemModel> Proveedores { get; set; } = new();
}

public class PedidoCompraGuardarSolicitud
{
    public int Id { get; set; }

    [Required]
    public int ContextoId { get; set; }

    public string? Revision { get; set; }

    [Required]
    public DateTime Fecha { get; set; } = DateTime.Today;

    [Required]
    public int TipoProductoId { get; set; }

    [Required]
    public int ProveedorId { get; set; }

    [MaxLength(400)]
    public string? Observacion { get; set; }
}

public class PedidoCompraProductoGuardarSolicitud
{
    public int Id { get; set; }

    [Required]
    public int ContextoId { get; set; }

    [Required]
    public int PedidoCompraId { get; set; }

    public int? ProductoId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Descripcion { get; set; } = "";

    [Required]
    public decimal Cantidad { get; set; } = 1m;

    public decimal PesoNeto { get; set; } = 0m;
    public decimal PesoBruto { get; set; } = 0m;

    public string StockEnProveedor { get; set; } = "N";
    public string EsAdicional { get; set; } = "N";
}

