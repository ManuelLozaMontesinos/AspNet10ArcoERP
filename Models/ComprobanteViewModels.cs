using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AspNet10ArcoERP.Models;

public class ComprobanteFilaModel
{
    public int ComprobanteId { get; set; }
    public string TipoDocumentoComercial { get; set; } = "";
    public string TipoDocumentoComercialNombre { get; set; } = "";
    public string Serie { get; set; } = "";
    public string Numero { get; set; } = "";
    public string SerieNumero => $"{Serie}-{Numero}";
    public int ListaPrecioId { get; set; }
    public DateTime Fecha { get; set; }
    public int ClienteId { get; set; }
    public string ClienteRazonSocial { get; set; } = "";
    public string ClienteNumeroDocumento { get; set; } = "";
    public int VendedorId { get; set; }
    public string VendedorNombre { get; set; } = "";
    public int? CotizacionId { get; set; }
    public int? PedidoId { get; set; }
    public string CotizacionPedido => CotizacionId > 0 ? $"Cot. #{CotizacionId}" : (PedidoId > 0 ? $"Ped. #{PedidoId}" : "-");
    public string Moneda { get; set; } = "S/";
    public decimal MontoTotal { get; set; }
    public string TipoComprobante { get; set; } = "1";
    public string EstadoComprobante { get; set; } = "1";
    public string EstadoComprobanteNombre { get; set; } = "Pendiente";
    public string EstadoSunat { get; set; } = "1";
    public string EstadoSunatNombre { get; set; } = "Pendiente Generar";
    public string CodigoHash { get; set; } = "";
    public string Emitido { get; set; } = "N";
    public string EsContingencia { get; set; } = "N";
    public string EstadoDespacho { get; set; } = "Pendiente";
    public int? ComprobanteElectronicoId { get; set; }
}

public class ComprobantePageModel : ListadoPaginadoModel
{
    public List<ComprobanteFilaModel> Items { get; set; } = new();
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public int? TipoDocumentoComercialId { get; set; }
    public int? EstadoComprobanteId { get; set; }
    public int? EstadoSunatId { get; set; }
    public string? Search { get; set; }
    public List<LookupItemModel> TiposDocumento { get; set; } = new();
    public List<LookupItemModel> EstadosComprobante { get; set; } = new();
    public List<LookupItemModel> EstadosSunat { get; set; } = new();
    public List<LookupItemModel> Monedas { get; set; } = new();
    public List<LookupItemModel> CondicionesPago { get; set; } = new();
    public List<LookupItemModel> MediosPago { get; set; } = new();
    public List<LookupItemModel> TiposDetraccion { get; set; } = new();
    public List<LookupItemModel> Series { get; set; } = new();
}

public class ComprobanteItemModel
{
    public int ComprobanteProductoId { get; set; }
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal Cantidad { get; set; } = 1;
    public int UnidadMedidaId { get; set; }
    public string UnidadMedida { get; set; } = "UND";
    public decimal PrecioUnitario { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal MontoBruto { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public decimal MontoDescuento { get; set; }
    public decimal MontoNeto { get; set; }
    public decimal MontoIgv { get; set; }
    public decimal MontoTotal { get; set; }
    public int TipoAfectacionIgvId { get; set; } = 1; // 10 Gravado
    public string TipoAfectacionIgvCodigo { get; set; } = "10";
    public int TipoDetraccionId { get; set; }
    public decimal PorcentajeDetraccion { get; set; }
    public decimal MontoDetraccion { get; set; }
}

public class ComprobanteDetalleModel
{
    public int ComprobanteId { get; set; }
    public int PeriodoEmpresaSedeId { get; set; }
    public int TipoDocumentoComercialId { get; set; }
    public string TipoDocumentoComercialCodigo { get; set; } = "01";
    public string TipoDocumentoComercialNombre { get; set; } = "FACTURA";
    public string NumeroOperacion { get; set; } = "";
    public string Serie { get; set; } = "";
    public string Numero { get; set; } = "";
    public DateTime Fecha { get; set; } = DateTime.Today;
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = "";
    public string ClienteRuc { get; set; } = "";
    public string ClienteDireccion { get; set; } = "";
    public int VendedorId { get; set; }
    public string VendedorNombre { get; set; } = "";
    public int MonedaId { get; set; } = 1;
    public string MonedaSimbolo { get; set; } = "S/";
    public decimal MontoTipoCambio { get; set; } = 1.0m;
    public int ListaPrecioId { get; set; }
    public int TipoCondicionPagoId { get; set; }
    public int MedioPagoId { get; set; }
    public int ImpuestoTasaId { get; set; } = 1;
    public decimal PorcentajeIgv { get; set; } = 18m;
    public string? NumeroGuiaRemision { get; set; }
    public string? Observacion { get; set; }
    public decimal MontoBruto { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public decimal MontoDescuento { get; set; }
    public decimal MontoNeto { get; set; }
    public decimal MontoIgv { get; set; }
    public decimal MontoTotal { get; set; }
    public int TipoDetraccionId { get; set; }
    public decimal PorcentajeDetraccion { get; set; }
    public decimal MontoDetraccion { get; set; }
    public string Emitido { get; set; } = "N";
    public string EstadoComprobante { get; set; } = "1";
    public string EstadoComprobanteNombre { get; set; } = "Pendiente";
    public string EstadoSunat { get; set; } = "1";
    public string EstadoSunatNombre { get; set; } = "Pendiente Generar";
    public string? CodigoHash { get; set; }
    public int? ComprobanteElectronicoId { get; set; }
    public List<ComprobanteItemModel> Items { get; set; } = new();
}

public class ComprobanteGuardarSolicitud
{
    public int ComprobanteId { get; set; }
    public int PeriodoEmpresaSedeId { get; set; }
    public int TipoDocumentoComercialId { get; set; }
    public string Serie { get; set; } = "";
    public string? Numero { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Today;
    public int ClienteId { get; set; }
    public string? NombreAlterno { get; set; }
    public int VendedorId { get; set; }
    public int MonedaId { get; set; } = 1;
    public decimal MontoTipoCambio { get; set; } = 1.0m;
    public int ListaPrecioId { get; set; }
    public int TipoCondicionPagoId { get; set; }
    public int MedioPagoId { get; set; }
    public decimal PorcentajeTarjeta { get; set; }
    public int ImpuestoTasaId { get; set; } = 1;
    public string? NumeroGuiaRemision { get; set; }
    public string? Observacion { get; set; }
    public int? CotizacionId { get; set; }
    public int? PedidoId { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public int TipoDetraccionId { get; set; }
    public decimal PorcentajeDetraccion { get; set; }
    public decimal MontoDetraccion { get; set; }
    public string EsContingencia { get; set; } = "N";
    public List<ComprobanteItemModel> Items { get; set; } = new();
}

public class ComprobanteEmisionSolicitud
{
    public int ComprobanteId { get; set; }
    public string EnvioEnLinea { get; set; } = "N";
    public int? CajaId { get; set; }
}

public class NotaCreditoFilaModel
{
    public int NotaCreditoId { get; set; }
    public string Serie { get; set; } = "";
    public string Numero { get; set; } = "";
    public string SerieNumero => $"{Serie}-{Numero}";
    public DateTime Fecha { get; set; }
    public int ComprobanteId { get; set; }
    public string DocumentoReferencia { get; set; } = "";
    public string ClienteRazonSocial { get; set; } = "";
    public string ClienteNumeroDocumento { get; set; } = "";
    public string TipoNotaCreditoNombre { get; set; } = "";
    public string Moneda { get; set; } = "S/";
    public decimal MontoTotal { get; set; }
    public string EstadoSunat { get; set; } = "1";
    public string EstadoSunatNombre { get; set; } = "Pendiente";
    public string CodigoHash { get; set; } = "";
    public string Emitido { get; set; } = "N";
}

public class NotaCreditoPageModel : ListadoPaginadoModel
{
    public List<NotaCreditoFilaModel> Items { get; set; } = new();
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public string? Search { get; set; }
    public List<LookupItemModel> TiposNotaCredito { get; set; } = new();
    public List<LookupItemModel> Series { get; set; } = new();
}

public class NotaCreditoGuardarSolicitud
{
    public int NotaCreditoId { get; set; }
    public int PeriodoEmpresaSedeId { get; set; }
    public int ComprobanteId { get; set; }
    public int TipoNotaCreditoId { get; set; }
    public string Serie { get; set; } = "";
    public string? Numero { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string? Observacion { get; set; }
    public string MotivoSustento { get; set; } = "";
    public decimal MontoTotal { get; set; }
}

public class CanjeDocuModel
{
    public int ComprobanteId { get; set; }
    public string SerieNumero { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string Cliente { get; set; } = "";
    public decimal MontoTotal { get; set; }
    public int NuevoTipoDocComId { get; set; }
    public string NuevaSerie { get; set; } = "";
}

public class CanjeDocuPageModel : ListadoPaginadoModel
{
    public List<ComprobanteFilaModel> Items { get; set; } = new();
    public List<LookupItemModel> TiposDocumentoDestino { get; set; } = new();
}

public class CajaCobranzaFilaModel
{
    public int ComprobanteId { get; set; }
    public string TipoDocumento { get; set; } = "";
    public string SerieNumero { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string Cliente { get; set; } = "";
    public string Moneda { get; set; } = "S/";
    public decimal MontoTotal { get; set; }
    public decimal MontoPagado { get; set; }
    public decimal SaldoPendiente => MontoTotal - MontoPagado;
    public string EstadoCobranza { get; set; } = "Pendiente";
    public string MedioPago { get; set; } = "Efectivo";
}

public class CajaCobranzaPageModel : ListadoPaginadoModel
{
    public List<CajaCobranzaFilaModel> Items { get; set; } = new();
    public int? CajaId { get; set; }
    public DateTime? Fecha { get; set; } = DateTime.Today;
    public List<LookupItemModel> Cajas { get; set; } = new();
    public List<LookupItemModel> MediosPago { get; set; } = new();
}

public class CajaCobranzaPagarSolicitud
{
    public int ComprobanteId { get; set; }
    public int CajaId { get; set; }
    public int MedioPagoId { get; set; }
    public decimal MontoPago { get; set; }
    public string? NumeroOperacion { get; set; }
    public bool EmitirElectronicamente { get; set; } = true;
}
