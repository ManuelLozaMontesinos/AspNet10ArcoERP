using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AspNet10ArcoERP.Models;

public class OrdenCompraItemModel : MaestroEmpresaModel
{
    public int PeriodoEmpresaSedeId { get; set; }
    public string Codigo { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string FechaTexto => Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public int TipoProductoId { get; set; }
    public string TipoProductoNombre { get; set; } = "";

    public int? PedidoCompraId { get; set; }
    public string? PedidoCompraCodigo { get; set; }

    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = "";
    public string ProveedorDocumento { get; set; } = "";

    public DateTime? FechaEntrega { get; set; }
    public string? FechaEntregaTexto => FechaEntrega?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public int MonedaId { get; set; }
    public string MonedaSimbolo { get; set; } = "S/";
    public string MonedaNombre { get; set; } = "SOLES";

    public decimal TipoCambio { get; set; }
    public int ImpuestoTasaId { get; set; }
    public decimal ImpuestoTasa { get; set; } = 18m;

    public decimal MontoBruto { get; set; }
    public decimal MontoImpuesto { get; set; }
    public decimal MontoTotal { get; set; }

    public string MontoBrutoTexto => MontoBruto.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));
    public string MontoImpuestoTexto => MontoImpuesto.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));
    public string MontoTotalTexto => MontoTotal.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));

    public string PlazoEntrega { get; set; } = "";
    public string LugarEntrega { get; set; } = "";

    public int CondicionPagoId { get; set; }
    public string CondicionPagoNombre { get; set; } = "";

    public string Observacion { get; set; } = "";
    public string PermitePagoParcial { get; set; } = "S";
    public string EsModificacion { get; set; } = "N";

    public int? EstadoAprobacionId { get; set; }
    public string? EstadoAprobacionCodigo { get; set; }
    public string? EstadoAprobacionNombre { get; set; }

    public int EstadoOrdenCompraId { get; set; }
    public string EstadoOrdenCompraCodigo { get; set; } = "";
    public string EstadoOrdenCompraNombre { get; set; } = "";

    public int CantidadItems { get; set; }

    protected override string VersionExtra => $"{PeriodoEmpresaSedeId}|{Codigo}|{ProveedorId}|{MonedaId}|{MontoTotal}|{EstadoOrdenCompraId}|{EstadoAprobacionId}";
}

public class OrdenCompraPageModel : ListadoPaginadoModel
{
    public List<OrdenCompraItemModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();

    public string? Search { get; set; }
    public string? Codigo { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public int? TipoProductoId { get; set; }
    public int? EstadoOrdenCompraId { get; set; }
    public int? EstadoAprobacionId { get; set; }

    public List<LookupItemModel> TiposProducto { get; set; } = new();
    public List<LookupItemModel> EstadosOrdenCompra { get; set; } = new();
    public List<LookupItemModel> EstadosAprobacion { get; set; } = new();
    public List<LookupItemModel> Monedas { get; set; } = new();
    public List<LookupItemModel> CondicionesPago { get; set; } = new();
}

public class OrdenCompraProductoItemModel : MaestroEmpresaModel
{
    public int OrdenCompraId { get; set; }
    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string UnidadMedida { get; set; } = "UND";

    public decimal Cantidad { get; set; }
    public decimal CantidadIngresada { get; set; }
    public decimal CantidadPendiente => Math.Max(0m, Cantidad - CantidadIngresada);

    public decimal ValorUnitario { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal MontoBruto { get; set; }
    public decimal MontoImpuesto { get; set; }
    public decimal MontoTotal { get; set; }
    public string EsAdicional { get; set; } = "N";

    public string CantidadTexto => Cantidad.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));
    public string ValorUnitarioTexto => ValorUnitario.ToString("N4", CultureInfo.GetCultureInfo("es-PE"));
    public string PrecioUnitarioTexto => PrecioUnitario.ToString("N4", CultureInfo.GetCultureInfo("es-PE"));
    public string MontoTotalTexto => MontoTotal.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));

    protected override string VersionExtra => $"{OrdenCompraId}|{ProductoId}|{Cantidad}|{PrecioUnitario}|{MontoTotal}";
}

public class OrdenCompraDetalleModel
{
    public OrdenCompraItemModel Cabecera { get; set; } = new();
    public List<OrdenCompraProductoItemModel> Productos { get; set; } = new();
    public List<LookupItemModel> TiposProducto { get; set; } = new();
    public List<LookupItemModel> Monedas { get; set; } = new();
    public List<LookupItemModel> CondicionesPago { get; set; } = new();
    public List<LookupItemModel> TasasImpuesto { get; set; } = new();
    public List<LookupItemModel> Proveedores { get; set; } = new();
}

public class OrdenCompraGuardarSolicitud
{
    public int Id { get; set; }

    [Required]
    public int ContextoId { get; set; }

    public string? Revision { get; set; }

    [Required]
    public DateTime Fecha { get; set; } = DateTime.Today;

    [Required]
    public int TipoProductoId { get; set; }

    public int? PedidoCompraId { get; set; }

    [Required]
    public int ProveedorId { get; set; }

    public DateTime? FechaEntrega { get; set; }

    [Required]
    public int MonedaId { get; set; }

    public decimal TipoCambio { get; set; } = 1.0m;

    [Required]
    public int ImpuestoTasaId { get; set; }

    [MaxLength(100)]
    public string? PlazoEntrega { get; set; }

    [MaxLength(200)]
    public string? LugarEntrega { get; set; }

    [Required]
    public int CondicionPagoId { get; set; }

    [MaxLength(400)]
    public string? Observacion { get; set; }
}

public class OrdenCompraProductoGuardarSolicitud
{
    public int Id { get; set; }

    [Required]
    public int ContextoId { get; set; }

    [Required]
    public int OrdenCompraId { get; set; }

    [Required]
    public int ProductoId { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Descripcion { get; set; } = "";

    [Required]
    public decimal Cantidad { get; set; } = 1m;

    public decimal ValorUnitario { get; set; }
    public decimal PrecioUnitario { get; set; }
    public string EsAdicional { get; set; } = "N";
}

public class CambiarEstadoOrdenCompraSolicitud
{
    [Required]
    public int Id { get; set; }

    [Required]
    public int ContextoId { get; set; }

    public string? Revision { get; set; }

    [Required]
    public int EstadoOrdenCompraId { get; set; }
}

public class OrdenComAutoPageModel : ListadoPaginadoModel
{
    public List<OrdenCompraItemModel> Items { get; set; } = new();
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

public class AprobarOrdenComAutoSolicitud
{
    [Required]
    public int Id { get; set; }

    [Required]
    public int ContextoId { get; set; }

    public string? Revision { get; set; }
}

public class DesaprobarOrdenComAutoSolicitud
{
    [Required]
    public int Id { get; set; }

    [Required]
    public int ContextoId { get; set; }

    public string? Revision { get; set; }
}

