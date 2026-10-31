using AspNet10ArcoERP.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspNet10ArcoERP.Services;

public interface IVentasService
{
    Task<VentaProductoPageModel> GetVentasPorProductoAsync(int empresaId,int contextoId,VentaProductoFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null,bool exportar=false);
    Task<RegistroVentasPageModel> GetRegistroVentasAsync(int empresaId,int contextoId,RegistroVentasFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null,bool exportar=false);
    Task<CortesGratisPageModel> GetCortesGratisClasesAsync(int empresaId,int contextoId,string? search=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<CortesGratisDetalleModel?> GetCortesGratisClaseAsync(int empresaId,int contextoId,int id);
    Task<CortesGratisProductoPageModel?> GetCortesGratisProductosAsync(int empresaId,int contextoId,int claseId,int? familiaId=null,string? search=null,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<(bool success,string message,int id)> SaveCorteGratisAsync(int empresaId,int contextoId,int userId,CorteGratisSolicitud model);
    Task<CuadroCostoPageModel> GetCuadroCostoAsync(int empresaId,int contextoId,CuadroCostoFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null,bool exportar=false);
    Task<DetraccionPageModel> GetDetraccionesAsync(int empresaId,int contextoId,DetraccionFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<DetraccionFila?> GetDetraccionAsync(int empresaId,int contextoId,int consultaId,int id);
    Task<(bool success,string message,int id)> SaveDetraccionPagoAsync(int empresaId,int contextoId,int userId,DetraccionPagoSolicitud model);
    Task<VentaReportePageModel> GetReporteVentaAsync(int empresaId,int contextoId,string tipo,VentaReporteFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null,bool exportar=false);
    Task<CajaPageModel> GetCajasAsync(int empresaId,int sedeId,string? search=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<CajaModel?> GetCajaAsync(int empresaId,int sedeId,int id);
    Task<(bool success,string message,int id)> SaveCajaAsync(int empresaId,int sedeId,int userId,MaestroEdicion<CajaModel> model);
    Task<(bool success,string message,int id)> AnularCajaAsync(int empresaId,int sedeId,int userId,VentaEstadoSolicitud model);
    Task<VendedorPageModel> GetVendedoresAsync(int empresaId,string? search=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<VendedorModel?> GetVendedorAsync(int empresaId,int id);
    Task<(bool success,string message,int id)> SaveVendedorAsync(int empresaId,int userId,MaestroEdicion<VendedorModel> model);
    Task<(bool success,string message,int id)> CambiarEstadoVendedorAsync(int empresaId,int userId,VentaEstadoSolicitud model);
    Task<AsignacionVentaPageModel> GetPersonalVendedorAsync(int empresaId,string? search=null,int page=1,int top=25,string? sort=null,string? dir=null,int? filtroSedeId=null);
    Task<AsignacionVentaPageModel?> GetAsignacionesVentaAsync(int empresaId,int sedeId,int ownerId,string tipo,bool candidatos=false,string? search=null,int page=1,int top=25,string? sort=null,string? dir=null,int? filtroSedeId=null,int? monedaId=null,int? tipoDocumentoId=null);
    Task<(bool success,string message,int id)> AgregarAsignacionesVentaAsync(int empresaId,int sedeId,int userId,VentaVinculoSolicitud model);
    Task<(bool success,string message,int id)> CambiarEstadoAsignacionVentaAsync(int empresaId,int sedeId,int userId,VentaVinculoEstadoSolicitud model);
    Task<MedioPagoPageModel> GetMediosPagoAsync(int empresaId,string? search=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<MedioPagoModel?> GetMedioPagoAsync(int empresaId,int id);
    Task<(bool success,string message,int id)> SaveMedioPagoAsync(int empresaId,int userId,MaestroEdicion<MedioPagoModel> model);
    Task<(bool success,string message,int id)> AnularMedioPagoAsync(int empresaId,int userId,int id,string revision);
    Task<GrupoClientePageModel> GetGruposClienteAsync(int empresaId,string? search=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<GrupoClienteModel?> GetGrupoClienteAsync(int empresaId,int id);
    Task<(bool success,string message,int id)> SaveGrupoClienteAsync(int empresaId,int userId,MaestroEdicion<GrupoClienteModel> model);
    Task<(bool success,string message,int id)> AnularGrupoClienteAsync(int empresaId,int userId,int id,string revision);
    Task<GrupoClienteDetalleModel?> GetGrupoClientesAsync(int empresaId,int grupoId,string? search=null,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<GrupoClienteDetalleModel?> GetGrupoCandidatosAsync(int empresaId,int grupoId,string? search=null,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<(bool success,string message,int id)> AgregarGrupoClienteAsync(int empresaId,int userId,int grupoId,int proveedorId);
    Task<(bool success,string message,int id)> QuitarGrupoClienteAsync(int empresaId,int userId,int grupoId,int id,string revision);
    Task<ListaPrecioPageModel> GetListasPrecioAsync(int empresaId,int contextoId,string? search=null,int? sedeId=null,int? monedaId=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<ListaPrecioDetalleModel?> GetListaPrecioDetalleAsync(int empresaId,int id);
    Task<List<LookupItemModel>> GetListaPrecioLookupsAsync(int empresaId,string tipo);
    Task<(bool success,string message,int id)> SaveListaPrecioAsync(int empresaId,int userId,ListaPrecioSolicitud model);
    Task<(bool success,string message,int id)> AnularListaPrecioAsync(int empresaId,int userId,int id,string revision);
    Task<ListaPrecioProductoPageModel> GetListaPrecioProductosAsync(int empresaId,int listaPrecioId,string? search=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<(bool success,string message,int id)> SaveListaPrecioProductoAsync(int empresaId,int userId,ListaPrecioProductoSolicitud model);
    Task<List<ListaPrecioGrupoItemModel>> GetListaPrecioGruposAsync(int empresaId,int listaPrecioId);
    Task<List<LookupItemModel>> GetGruposClienteDisponiblesAsync(int empresaId,int listaPrecioId);
    Task<(bool success,string message,int id)> SaveListaPrecioGrupoAsync(int empresaId,int userId,ListaPrecioGrupoSolicitud model);
    Task<(bool success,string message,int id)> QuitarListaPrecioGrupoAsync(int empresaId,int userId,int id);
    Task<ListaPrecioGrupoProductoPageModel> GetListaPrecioGrupoProductosAsync(int empresaId,int listaPProveId,string? search=null,int page=1,int top=25);
    Task<(bool success,string message,int id)> SaveListaPrecioGrupoProductoAsync(int empresaId,int userId,ListaPrecioGrupoProductoSolicitud model);
    Task<(bool success,string message,int id)> QuitarListaPrecioGrupoProductoAsync(int empresaId,int userId,int id);
    Task<(string Titulo,byte[] Contenido)> ExportarListaPrecioExcelAsync(int empresaId,int listaPrecioId);
    Task<CotizacionPageModel> GetCotizacionesAsync(int empresaId,int contextoId,string? search=null,string? codigo=null,DateTime? fechaDesde=null,DateTime? fechaHasta=null,int? vendedorId=null,int? estadoCotizacionId=null,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<CotizacionDetalleModel?> GetCotizacionDetalleAsync(int empresaId,int contextoId,int id);
    Task<List<LookupItemModel>> BuscarClientesCotizacionAsync(int empresaId,string? search);
    Task<List<ProductoPrecioLookupModel>> BuscarProductosCotizacionAsync(int listaPrecioId,string? search);
    Task<(bool success,string message,int id)> SaveCotizacionAsync(int empresaId,int contextoId,int userId,CotizacionGuardarSolicitud model);
    Task<(bool success,string message,int id)> AnularCotizacionAsync(int empresaId,int contextoId,int userId,int id,string revision);
    Task<List<CotizacionProductoItemModel>> GetCotizacionProductosAsync(int empresaId,int cotizacionId);
    Task<(bool success,string message,int id)> SaveCotizacionProductoAsync(int empresaId,int contextoId,int userId,CotizacionProductoGuardarSolicitud model);
    Task<(bool success,string message,int id)> EliminarCotizacionProductoAsync(int empresaId,int contextoId,int userId,int id);
    Task<(bool success,string message,int id)> GenerarPedidoDesdeCotizacionAsync(int empresaId,int contextoId,int userId,GenerarPedidoSolicitud model);
    Task<(string Titulo,byte[] Contenido)> ExportarCotizacionExcelAsync(int empresaId,int cotizacionId);
    Task<CotizaAutorizaPageModel> GetCotizacionesAutorizarAsync(int empresaId,int? periodoEmpresaSedeId,string? search=null,string? codigo=null,string? cliente=null,string? vendedor=null,int? estadoAprobacionId=null,DateTime? fechaDesde=null,DateTime? fechaHasta=null,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<CotizaAutorizaDetalleModel?> GetCotizacionAutorizacionDetalleAsync(int empresaId,int cotizacionAutorizacionId);
    Task<(bool success,string message,int id)> AprobarCotizacionAutorizacionAsync(int empresaId,int periodoEmpresaSedeId,int userId,AprobarCotizacionAutorizacionSolicitud model);
    Task<(bool success,string message,int id)> DesaprobarCotizacionAutorizacionAsync(int empresaId,int periodoEmpresaSedeId,int userId,DesaprobarCotizacionAutorizacionSolicitud model);
    Task<PedidoPageModel> GetPedidosAsync(int empresaId,int contextoId,string? search=null,string? codigo=null,DateTime? fechaDesde=null,DateTime? fechaHasta=null,int? vendedorId=null,int? estadoCotizacionId=null,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<PedidoDetalleModel?> GetPedidoDetalleAsync(int empresaId,int contextoId,int id);
    Task<(bool success,string message,int id)> SavePedidoAsync(int empresaId,int contextoId,int userId,PedidoGuardarSolicitud model);
    Task<(bool success,string message,int id)> AnularPedidoAsync(int empresaId,int contextoId,int userId,int id,string revision);
    Task<(bool success,string message,int id)> CambiarACotizacionAsync(int empresaId,int contextoId,int userId,int id,string revision);
    Task<(bool success,string message,int id)> CambiarEstadoPedidoAsync(int empresaId,int contextoId,int userId,CambiarEstadoPedidoSolicitud model);
    Task<(string Titulo,byte[] Contenido)> ExportarPedidoExcelAsync(int empresaId,int pedidoId);

    // Facturación y Comprobantes
    Task<ComprobantePageModel> GetComprobantesAsync(int empresaId, int contextoId, string? search = null, int? tipoDocId = null, int? estadoComproId = null, int? estadoSunatId = null, DateTime? fechaDesde = null, DateTime? fechaHasta = null, int page = 1, int top = 25, string? sort = null, string? dir = null);
    Task<ComprobanteDetalleModel?> GetComprobanteDetalleAsync(int empresaId, int comprobanteId);
    Task<List<LookupItemModel>> GetSeriesComprobanteAsync(int contextoId, int tipoDocComId);
    Task<string> GetSiguienteNumeroComprobanteAsync(int contextoId, int tipoDocComId, string serie);
    Task<(bool success, string message, int id)> SaveComprobanteAsync(int empresaId, int contextoId, int userId, ComprobanteGuardarSolicitud model);
    Task<(bool success, string message, int id)> AnularComprobanteAsync(int empresaId, int contextoId, int userId, int comprobanteId, string motivo);
    Task<(bool success, string message, ResponseDTO? feResponse)> EmitirComprobanteAsync(int empresaId, int contextoId, int userId, ComprobanteEmisionSolicitud model);

    // Notas de Crédito
    Task<NotaCreditoPageModel> GetNotasCreditoAsync(int empresaId, int contextoId, string? search = null, DateTime? fechaDesde = null, DateTime? fechaHasta = null, int page = 1, int top = 25, string? sort = null, string? dir = null);
    Task<(bool success, string message, int id)> SaveNotaCreditoAsync(int empresaId, int contextoId, int userId, NotaCreditoGuardarSolicitud model);

    // Canje de Documentos
    Task<CanjeDocuPageModel> GetCanjeDocuAsync(int empresaId, int contextoId, string? search = null, int page = 1, int top = 25);
    Task<(bool success, string message, int id)> ProcesarCanjeDocuAsync(int empresaId, int contextoId, int userId, CanjeDocuModel model);

    // Caja y Cobranza
    Task<CajaCobranzaPageModel> GetCajaCobranzaAsync(int empresaId, int contextoId, int? cajaId = null, DateTime? fecha = null, string? search = null, int page = 1, int top = 25);
    Task<(bool success, string message, ResponseDTO? feResponse)> PagarCajaCobranzaAsync(int empresaId, int contextoId, int userId, CajaCobranzaPagarSolicitud model);

    // Control Tributario Electrónico (FE)
    Task<DocEmitidoPageModel> GetDocumentosEmitidosAsync(int empresaId, int contextoId, DateTime? fechaDesde = null, DateTime? fechaHasta = null, int? tipoDocId = null, string? estadoDoc = null, string? search = null, int page = 1, int top = 25);
    Task<ResumenDiarioPageModel> GetResumenDiarioAsync(int empresaId, DateTime fecha, int page = 1, int top = 25);
}
