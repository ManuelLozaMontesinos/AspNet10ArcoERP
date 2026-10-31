using AspNet10ArcoERP.Models;

namespace AspNet10ArcoERP.Services;

public interface IComprasService
{
    Task<PuntoPedidoPageModel> GetPuntoPedidoAsync(int empresaId,int contextoId,PuntoPedidoFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null,bool exportar=false);
    Task<PedidoCompraPageModel> GetPedidosCompraAsync(int empresaId,int contextoId,string? search=null,string? codigo=null,DateTime? fechaDesde=null,DateTime? fechaHasta=null,int? tipoProductoId=null,int? estadoPedidoCompraId=null,int? estadoAprobacionId=null,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<PedidoCompraDetalleModel?> GetPedidoCompraDetalleAsync(int empresaId,int contextoId,int id);
    Task<List<LookupItemModel>> BuscarProveedoresCompraAsync(int empresaId,string? search);
    Task<List<LookupItemModel>> BuscarProductosCompraAsync(int empresaId,int? tipoProductoId,string? search);
    Task<(bool success,string message,int id)> SavePedidoCompraAsync(int empresaId,int contextoId,int userId,PedidoCompraGuardarSolicitud model);
    Task<(bool success,string message,int id)> AnularPedidoCompraAsync(int empresaId,int contextoId,int userId,int id,string revision);
    Task<(bool success,string message,int id)> EnviarAprobacionPedidoCompraAsync(int empresaId,int contextoId,int userId,int id,string revision);
    Task<List<PedidoCompraProductoItemModel>> GetPedidoCompraProductosAsync(int empresaId,int pedidoCompraId);
    Task<(bool success,string message,int id)> SavePedidoCompraProductoAsync(int empresaId,int contextoId,int userId,PedidoCompraProductoGuardarSolicitud model);
    Task<(bool success,string message,int id)> EliminarPedidoCompraProductoAsync(int empresaId,int contextoId,int userId,int id);
    Task<(string Titulo,byte[] Contenido)> ExportarPedidoCompraExcelAsync(int empresaId,int pedidoCompraId);
    Task<PedidoComAutoPageModel> GetPedidosComAutoAsync(int empresaId,int contextoId,string? search=null,string? codigo=null,DateTime? fechaDesde=null,DateTime? fechaHasta=null,int? tipoProductoId=null,int? estadoAprobacionId=null,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<PedidoComAutoDetalleModel?> GetPedidoComAutoDetalleAsync(int empresaId,int contextoId,int id);
    Task<(bool success,string message,int id)> AprobarPedidoComAutoAsync(int empresaId,int contextoId,int userId,AprobarPedidoComAutoSolicitud model);
    Task<(bool success,string message,int id)> DesaprobarPedidoComAutoAsync(int empresaId,int contextoId,int userId,DesaprobarPedidoComAutoSolicitud model);
    Task<OrdenCompraPageModel> GetOrdenesCompraAsync(int empresaId,int contextoId,string? search=null,string? codigo=null,DateTime? fechaDesde=null,DateTime? fechaHasta=null,int? tipoProductoId=null,int? estadoOrdenCompraId=null,int? estadoAprobacionId=null,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<OrdenCompraDetalleModel?> GetOrdenCompraDetalleAsync(int empresaId,int contextoId,int id);
    Task<(bool success,string message,int id)> SaveOrdenCompraAsync(int empresaId,int contextoId,int userId,OrdenCompraGuardarSolicitud model);
    Task<(bool success,string message,int id)> AnularOrdenCompraAsync(int empresaId,int contextoId,int userId,int id,string revision);
    Task<(bool success,string message,int id)> EnviarAprobacionOrdenCompraAsync(int empresaId,int contextoId,int userId,int id,string revision);
    Task<(bool success,string message,int id)> CambiarEstadoOrdenCompraAsync(int empresaId,int contextoId,int userId,CambiarEstadoOrdenCompraSolicitud model);
    Task<List<OrdenCompraProductoItemModel>> GetOrdenCompraProductosAsync(int empresaId,int ordenCompraId);
    Task<(bool success,string message,int id)> SaveOrdenCompraProductoAsync(int empresaId,int contextoId,int userId,OrdenCompraProductoGuardarSolicitud model);
    Task<(bool success,string message,int id)> EliminarOrdenCompraProductoAsync(int empresaId,int contextoId,int userId,int id);
    Task<(string Titulo,byte[] Contenido)> ExportarOrdenCompraExcelAsync(int empresaId,int ordenCompraId);
    Task<OrdenComAutoPageModel> GetOrdenesComAutoAsync(int empresaId,int contextoId,string? search=null,string? codigo=null,DateTime? fechaDesde=null,DateTime? fechaHasta=null,int? tipoProductoId=null,int? estadoAprobacionId=null,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<(bool success,string message,int id)> AprobarOrdenComAutoAsync(int empresaId,int contextoId,int userId,AprobarOrdenComAutoSolicitud model);
    Task<(bool success,string message,int id)> DesaprobarOrdenComAutoAsync(int empresaId,int contextoId,int userId,DesaprobarOrdenComAutoSolicitud model);
    Task<ValorizaCompraPageModel> GetValorizacionesCompraAsync(int empresaId,int contextoId,string? search=null,string? ordenCompraCodigo=null,string? serie=null,string? numero=null,int? tipoDocumentoComercialId=null,int? condicionPagoId=null,DateTime? fechaDesde=null,DateTime? fechaHasta=null,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<List<LookupItemModel>> BuscarOrdenesCompraParaComprobanteAsync(int empresaId, int contextoId, string? search);
    Task<ValorizaCompraItemModel?> GetValorizacionCompraDetalleAsync(int empresaId,int contextoId,int id);
    Task<(bool success,string message,int id)> SaveValorizacionCompraAsync(int empresaId,int contextoId,int userId,ValorizaCompraGuardarSolicitud model);
    Task<(bool success,string message,int id)> EliminarValorizacionCompraAsync(int empresaId,int contextoId,int userId,int id);
}
