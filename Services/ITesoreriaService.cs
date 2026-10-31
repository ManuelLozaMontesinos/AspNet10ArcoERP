using AspNet10ArcoERP.Models;

namespace AspNet10ArcoERP.Services;

public interface ITesoreriaService
{
    Task<BancarizacionPageModel> GetBancarizacionAsync(int empresaId,int contextoId,BancarizacionFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<BancarizacionDetalleModel?> GetBancarizacionDocumentoAsync(int empresaId,int contextoId,int consultaId,int id);
    Task<BancarizacionCobranzaPageModel?> GetBancarizacionCobranzasAsync(int empresaId,int contextoId,int consultaId,int comprobanteId,string? search=null,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<(bool success,string message,int id)> SaveBancarizacionCobranzaAsync(int empresaId,int contextoId,int userId,BancarizacionCobranzaSolicitud model);
    Task<(bool success,string message,int id)> AnularBancarizacionCobranzaAsync(int empresaId,int contextoId,int userId,BancarizacionAnularSolicitud model);
}
