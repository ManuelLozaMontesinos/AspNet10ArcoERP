using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AspNet10ArcoERP.Models;

public class PedidoItemModel : MaestroEmpresaModel
{
    public int PeriodoEmpresaSedeId { get; set; }
    public string Codigo { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string FechaTexto => Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    public DateTime? FechaEntrega { get; set; }
    public string? FechaEntregaTexto => FechaEntrega?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = "";
    public string ClienteDocumento { get; set; } = "";

    public int VendedorId { get; set; }
    public string VendedorNombre { get; set; } = "";

    public int MonedaId { get; set; }
    public string MonedaSimbolo { get; set; } = "";
    public string MonedaNombre { get; set; } = "";
    public decimal MontoTipoCambio { get; set; }

    public int ListaPrecioId { get; set; }
    public string ListaPrecioNombre { get; set; } = "";

    public int TipoCondicionPagoId { get; set; }
    public string CondicionPagoNombre { get; set; } = "";

    public string Observacion { get; set; } = "";

    public decimal MontoBruto { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public decimal MontoDescuento { get; set; }
    public decimal MontoNeto { get; set; }
    public decimal MontoIgv { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal MontoAnticipo { get; set; }
    public decimal MontoSaldo => Math.Max(0m, MontoTotal - MontoAnticipo);

    public string MontoTotalTexto => MontoTotal.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));
    public string MontoAnticipoTexto => MontoAnticipo.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));
    public string MontoSaldoTexto => MontoSaldo.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));

    public int EstadoCotizacionId { get; set; }
    public string EstadoCotizacionCodigo { get; set; } = "";
    public string EstadoCotizacionNombre { get; set; } = "";

    public string EnviadoProduccion { get; set; } = "N";
    public int CantidadItems { get; set; }

    protected override string VersionExtra => $"{PeriodoEmpresaSedeId}|{Codigo}|{ClienteId}|{MontoTotal}|{EstadoCotizacionId}|{EnviadoProduccion}";
}

public class PedidoPageModel : ListadoPaginadoModel
{
    public List<PedidoItemModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();

    public string? Search { get; set; }
    public string? Codigo { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public int? VendedorId { get; set; }
    public int? EstadoCotizacionId { get; set; }

    public List<LookupItemModel> Vendedores { get; set; } = new();
    public List<LookupItemModel> EstadosCotizacion { get; set; } = new();
    public List<LookupItemModel> Monedas { get; set; } = new();
    public List<LookupItemModel> ListasPrecio { get; set; } = new();
    public List<LookupItemModel> CondicionesPago { get; set; } = new();
}

public class PedidoDetalleModel
{
    public PedidoItemModel Cabecera { get; set; } = new();
    public List<CotizacionProductoItemModel> Productos { get; set; } = new();
    public List<LookupItemModel> Vendedores { get; set; } = new();
    public List<LookupItemModel> Monedas { get; set; } = new();
    public List<LookupItemModel> ListasPrecio { get; set; } = new();
    public List<LookupItemModel> CondicionesPago { get; set; } = new();
}

public class PedidoGuardarSolicitud
{
    public int Id { get; set; }

    [Required]
    public int ContextoId { get; set; }

    public string? Revision { get; set; }

    [Required]
    public DateTime Fecha { get; set; } = DateTime.Today;

    public DateTime? FechaEntrega { get; set; }

    [Required]
    public int VendedorId { get; set; }

    [Required]
    public int ClienteId { get; set; }

    [Required]
    public int MonedaId { get; set; }

    [Required]
    public int TipoCondicionPagoId { get; set; }

    [Required]
    public int ListaPrecioId { get; set; }

    [MaxLength(400)]
    public string? Observacion { get; set; }
}

public class CambiarEstadoPedidoSolicitud
{
    [Required]
    public int ContextoId { get; set; }

    [Required]
    public int PedidoId { get; set; }

    [Required]
    public int NuevoEstadoId { get; set; }

    public string? Revision { get; set; }
}

