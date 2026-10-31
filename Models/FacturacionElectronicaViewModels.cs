using System;
using System.Collections.Generic;

namespace AspNet10ArcoERP.Models;

public class FacturacionElectronicaOpciones
{
    public string Ambiente { get; set; } = "BETA";
    public string ApiUrl { get; set; } = "http://localhost:2940/";
    public string SunatBetaEndpoint { get; set; } = "https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService";
    public bool ModoLocalPruebas { get; set; } = true;
    public bool SimularSiNoDisponible { get; set; } = true;
    public string RucPrueba { get; set; } = "20123456789";
    public string RazonSocialPrueba { get; set; } = "ARCO ERP - ENTORNO DE PRUEBAS SUNAT BETA";
}

public class InvoiceDTO
{
    public InvoiceDTO()
    {
        Items = new List<InvoiceItemDTO>();
        FormaPagoCuotas = new List<FormaPagoCuotaDTO>();
    }

    public string SignedBy { get; set; } = "";
    public string EnvioEnLinea { get; set; } = "N";
    public int TipoComprobante { get; set; }
    public string Serie { get; set; } = "";
    public string Numero { get; set; } = "";
    public DateTime Fecha { get; set; } = DateTime.Today;
    public DateTime Hora { get; set; } = DateTime.Now;
    public string TipoOperacion { get; set; } = "0101"; // Venta interna general
    public string TipoMoneda { get; set; } = "PEN";
    public string Observacion { get; set; } = "";

    // Proveedor (Emisor)
    public string ProveedorNombre { get; set; } = "";
    public string ProveedorNombreComercial { get; set; } = "";
    public string ProveedorDireccion { get; set; } = "";
    public string ProveedorTipoDocumento { get; set; } = "6"; // 6 = RUC
    public string CodigoEstablecimiento { get; set; } = "0000";
    public string ProveedorRUC { get; set; } = "";
    public string ProveedorUbigeo { get; set; } = "150101";
    public string ProveedorUrbanizacionZona { get; set; } = "";
    public string ProveedorDepartamento { get; set; } = "LIMA";
    public string ProveedorProvincia { get; set; } = "LIMA";
    public string ProveedorDistrito { get; set; } = "LIMA";
    public string ProveedorPais { get; set; } = "PE";

    // Cliente (Receptor)
    public string ClienteNombre { get; set; } = "";
    public string ClienteDireccion { get; set; } = "";
    public string ClienteNumeroDocumento { get; set; } = "";
    public string ClienteTipoDocumento { get; set; } = "6"; // 6 = RUC, 1 = DNI, etc.

    // Referencias para Nota de Crédito o Anticipo
    public string DocumentoReferenciaSerie { get; set; } = "";
    public string DocumentoReferenciaNumero { get; set; } = "";
    public string TipoDocumentoReferencia { get; set; } = "";
    public decimal DocumentoReferenciaMontoNeto { get; set; }
    public decimal DocumentoReferenciaMontoIGV { get; set; }
    public decimal DocumentoReferenciaMontoTotal { get; set; }
    public string TipoNota { get; set; } = "";
    public string Sustento { get; set; } = "";

    // Totales
    public decimal TotalGravadoDocumento { get; set; }
    public decimal TotalImpuestoDocumento { get; set; }
    public decimal TotalDocumento { get; set; }
    public decimal PorcentajeIGV { get; set; } = 18m;
    public decimal TotalGravado { get; set; }
    public decimal TotalInafecto { get; set; }
    public decimal TotalExonerado { get; set; }
    public decimal TotalGratuito { get; set; }
    public decimal PorcentajeDescuentoGlobal { get; set; }
    public decimal TotalDescuentoGlobal { get; set; }
    public decimal TotalDescuentoItem { get; set; }
    public decimal TotalIGV { get; set; }
    public decimal TotalISC { get; set; }
    public decimal Total { get; set; }

    // Forma de Pago
    public string FormaPago { get; set; } = "Contado";
    public decimal MontoFormaPago { get; set; }
    public List<FormaPagoCuotaDTO> FormaPagoCuotas { get; set; }

    // Detracciones
    public string TipoDetraccion { get; set; } = "";
    public decimal PorcentajeDetraccion { get; set; }
    public decimal MontoDetraccion { get; set; }
    public string CuentaDetraccion { get; set; } = "";

    public string MontoLetras { get; set; } = "";
    public List<InvoiceItemDTO> Items { get; set; }
}

public class InvoiceItemDTO
{
    public string Codigo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string UnidadMedida { get; set; } = "NIU";
    public decimal Cantidad { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal PrecioUnitario { get; set; }
    public string TipoAfectacion { get; set; } = "10"; // 10: Gravado - Operación Onerosa
    public decimal MontoBruto { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public decimal MontoDescuento { get; set; }
    public decimal MontoNeto { get; set; }
    public decimal MontoIGV { get; set; }
    public decimal MontoTotal { get; set; }
}

public class FormaPagoCuotaDTO
{
    public string Cuota { get; set; } = "Cuota001";
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
}

public class ResponseDTO
{
    public int TipoComprobante { get; set; }
    public string Serie { get; set; } = "";
    public string Numero { get; set; } = "";
    public string HashCode { get; set; } = "";
    public bool Success { get; set; } = true;
    public string Error { get; set; } = "";
    public string EstadoComprobante { get; set; } = "2"; // 2: Enviado / Aceptado
    public int ComprobanteElectronicoId { get; set; }
    public string SunatCDR { get; set; } = "";
    public string SunatNota { get; set; } = "";
    public string SunatCodigo { get; set; } = "0"; // 0 = Aceptado
    public string SunatDescripcion { get; set; } = "";
    public string SunatNumeroTicket { get; set; } = "";
    public List<int> ComprobanteElectronicoList { get; set; } = new();
    public string? XmlUrl { get; set; }
    public string? CdrUrl { get; set; }
    public bool EsModoBeta { get; set; } = true;
}

public class ResumenDTO
{
    public ResumenDTO()
    {
        Items = new List<ResumenItemDTO>();
    }
    public int CorrelativoEnvio { get; set; } = 1;
    public DateTime FechaComprobante { get; set; } = DateTime.Today;
    public DateTime FechaResumen { get; set; } = DateTime.Today;
    public string ProveedorNombre { get; set; } = "";
    public string ProveedorTipoDocumento { get; set; } = "6";
    public string ProveedorRUC { get; set; } = "";
    public string NumeroTicket { get; set; } = "";
    public List<ResumenItemDTO> Items { get; set; }
}

public class ResumenItemDTO
{
    public int DocumentoElectronicoId { get; set; }
    public string TipoDocumento { get; set; } = "03";
    public string Serie { get; set; } = "";
    public string Correlativo { get; set; } = "";
    public string TipoMoneda { get; set; } = "PEN";
    public string ClienteTipoDocumentoIdentidad { get; set; } = "1";
    public string ClienteNumeroDocumentoIdentidad { get; set; } = "";
    public decimal TotalGravado { get; set; }
    public decimal TotalInafecto { get; set; }
    public decimal TotalExonerado { get; set; }
    public decimal TotalOtrosCargos { get; set; }
    public decimal TotalIGV { get; set; }
    public decimal TotalISC { get; set; }
    public decimal TotalOtrosTributos { get; set; }
    public decimal Total { get; set; }
    public decimal TotalVentaGratuita { get; set; }
    public string TipoDocumentoModifica { get; set; } = "";
    public string SerieModifica { get; set; } = "";
    public string CorrelativoModifica { get; set; } = "";
    public string RegimenPercepcion { get; set; } = "";
    public decimal BaseImponiblePercepcion { get; set; }
    public decimal MontoPercepcion { get; set; }
    public decimal MontoTotalPercepcion { get; set; }
    public string Estado { get; set; } = "1"; // 1 = Adicionar, 2 = Modificar, 3 = Anulado
}

public class DocEmitidoItemModel
{
    public int ComprobanteId { get; set; }
    public int? ComprobanteElectronicoId { get; set; }
    public DateTime Fecha { get; set; }
    public string Serie { get; set; } = "";
    public string Numero { get; set; } = "";
    public string SerieNumero => $"{Serie}-{Numero}";
    public int TipoDocumentoComercialId { get; set; }
    public string TipoDocumentoComercial { get; set; } = "";
    public string TipoDocumentoNombre { get; set; } = "";
    public string ClienteRuc { get; set; } = "";
    public string ClienteNombre { get; set; } = "";
    public string Moneda { get; set; } = "S/";
    public decimal MontoTotal { get; set; }
    public string EstadoSunat { get; set; } = "1";
    public string EstadoSunatNombre { get; set; } = "Pendiente";
    public string CodigoHash { get; set; } = "";
    public bool TieneXml { get; set; }
    public bool TieneCdr { get; set; }
}

public class DocEmitidoPageModel : ListadoPaginadoModel
{
    public List<DocEmitidoItemModel> Items { get; set; } = new();
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public int? TipoDocumentoComercialId { get; set; }
    public string? EstadoDocumento { get; set; }
    public string? Search { get; set; }
    public List<LookupItemModel> TiposDocumento { get; set; } = new();
    public List<LookupItemModel> EstadosSunat { get; set; } = new();
    public FacturacionElectronicaOpciones Opciones { get; set; } = new();
}

public class ResumenDiarioItemModel
{
    public int Id { get; set; }
    public DateTime FechaEmision { get; set; }
    public DateTime FechaGeneracion { get; set; }
    public string Ticket { get; set; } = "";
    public int CantidadDocumentos { get; set; }
    public decimal TotalGravado { get; set; }
    public decimal TotalIgv { get; set; }
    public decimal TotalGeneral { get; set; }
    public string Estado { get; set; } = "Enviado";
    public string MensajeSunat { get; set; } = "";
}

public class ResumenDiarioPageModel : ListadoPaginadoModel
{
    public List<ResumenDiarioItemModel> Items { get; set; } = new();
    public DateTime Fecha { get; set; } = DateTime.Today;
    public FacturacionElectronicaOpciones Opciones { get; set; } = new();
}

public class EnviarDocumentoRequest
{
    public int ComprobanteId { get; set; }
    public string TipoDocumento { get; set; } = "01";
    public string EnvioEnLinea { get; set; } = "N";
}

