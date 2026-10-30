using AspNet10ArcoERP.Models;

namespace AspNet10ArcoERP.Services;

public interface IVentasService
{
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
}
