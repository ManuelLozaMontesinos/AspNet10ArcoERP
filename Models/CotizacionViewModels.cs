using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AspNet10ArcoERP.Models;

public class CotizacionItemModel : MaestroEmpresaModel
{
    public int PeriodoEmpresaSedeId { get; set; }
    public string Codigo { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string FechaTexto => Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

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

    public string MontoTotalTexto => MontoTotal.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));
    public string MontoNetoTexto => MontoNeto.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));
    public string MontoIgvTexto => MontoIgv.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));

    public int EstadoCotizacionId { get; set; }
    public string EstadoCotizacionCodigo { get; set; } = "";
    public string EstadoCotizacionNombre { get; set; } = "";

    public string EsPedido { get; set; } = "N";
    public string EnviadoProduccion { get; set; } = "N";
    public int CantidadItems { get; set; }

    protected override string VersionExtra => $"{PeriodoEmpresaSedeId}|{Codigo}|{ClienteId}|{MontoTotal}|{EstadoCotizacionId}|{EsPedido}";
}

public class CotizacionPageModel : ListadoPaginadoModel
{
    public List<CotizacionItemModel> Items { get; set; } = new();
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

public class CotizacionDetalleModel
{
    public CotizacionItemModel Cabecera { get; set; } = new();
    public List<CotizacionProductoItemModel> Productos { get; set; } = new();
    public List<LookupItemModel> Vendedores { get; set; } = new();
    public List<LookupItemModel> Monedas { get; set; } = new();
    public List<LookupItemModel> ListasPrecio { get; set; } = new();
    public List<LookupItemModel> CondicionesPago { get; set; } = new();
}

public class CotizacionProductoItemModel : MaestroEmpresaModel
{
    public int CotizacionId { get; set; }
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Familia { get; set; } = "";
    public string UnidadMedida { get; set; } = "";
    public decimal Cantidad { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal MontoBruto { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public decimal MontoDescuento1 { get; set; }
    public decimal MontoNeto { get; set; }
    public decimal MontoIgv { get; set; }
    public decimal MontoTotal { get; set; }
    public string PermiteDescuento { get; set; } = "N";
    public string SinSaldo { get; set; } = "N";
    public string SolicitaAutorizacion { get; set; } = "N";

    public string CantidadTexto => Cantidad.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));
    public string PrecioUnitarioTexto => PrecioUnitario.ToString("N4", CultureInfo.GetCultureInfo("es-PE"));
    public string MontoTotalTexto => MontoTotal.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));

    protected override string VersionExtra => $"{CotizacionId}|{ProductoId}|{Cantidad}|{PrecioUnitario}|{MontoTotal}";
}

public class CotizacionGuardarSolicitud
{
    [Range(0, int.MaxValue)] public int Id { get; set; } // 0 para nueva
    [Range(1, int.MaxValue)] public int ContextoId { get; set; }
    [Required] public DateTime Fecha { get; set; } = DateTime.Today;
    [Range(1, int.MaxValue)] public int ClienteId { get; set; }
    [Range(1, int.MaxValue)] public int VendedorId { get; set; }
    [Range(1, int.MaxValue)] public int MonedaId { get; set; }
    [Range(1, int.MaxValue)] public int ListaPrecioId { get; set; }
    [Range(1, int.MaxValue)] public int TipoCondicionPagoId { get; set; }
    [StringLength(400)] public string Observacion { get; set; } = "";
    [StringLength(64)] public string Revision { get; set; } = "";
}

public class CotizacionProductoGuardarSolicitud
{
    [Range(0, int.MaxValue)] public int Id { get; set; } // 0 para nuevo ítem
    [Range(1, int.MaxValue)] public int ContextoId { get; set; }
    [Range(1, int.MaxValue)] public int CotizacionId { get; set; }
    [Range(1, int.MaxValue)] public int ProductoId { get; set; }
    [StringLength(150)] public string Descripcion { get; set; } = "";
    [Range(typeof(decimal), "0.0001", "99999999.9999")] public decimal Cantidad { get; set; }
    [Range(typeof(decimal), "0", "999999999.9999")] public decimal ValorUnitario { get; set; }
    [Range(typeof(decimal), "0", "999999999.9999")] public decimal PrecioUnitario { get; set; }
    [Range(typeof(decimal), "0", "100")] public decimal PorcentajeDescuento { get; set; }
    [RegularExpression("^[SN]$")] public string PermiteDescuento { get; set; } = "N";
}

public class GenerarPedidoSolicitud
{
    [Range(1, int.MaxValue)] public int CotizacionId { get; set; }
    [Range(1, int.MaxValue)] public int ContextoId { get; set; }
    public DateTime? FechaEntrega { get; set; }
}

public class ProductoPrecioLookupModel
{
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string UnidadMedida { get; set; } = "";
    public decimal ValorUnitario { get; set; }
    public decimal PrecioUnitario { get; set; }
    public string PermiteDescuento { get; set; } = "N";
    public int TipoAfectacionIgvId { get; set; }
}

