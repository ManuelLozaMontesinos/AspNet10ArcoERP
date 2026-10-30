using AspNet10ArcoERP.Models;

namespace AspNet10ArcoERP.Services;

public interface IProduccionService
{
    Task<ProduccionPageModel> GetProcesosAsync(int empresaId,int contextoId,string? search=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<ProduccionProcesoDetalleModel?> GetProcesoAsync(int empresaId,int contextoId,int id);
    Task<ProduccionPageModel?> GetDetallesProcesoAsync(int empresaId,int contextoId,int procesoId,string tipo,int subProcesoId=0,string? search=null,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<(bool success,string message,int id)> SaveProcesoAsync(int empresaId,int contextoId,int userId,ProduccionNombreSolicitud model);
    Task<(bool success,string message,int id)> SaveSubprocesoAsync(int empresaId,int contextoId,int userId,ProduccionNombreSolicitud model);
    Task<(bool success,string message,int id)> AnularProcesoAsync(int empresaId,int contextoId,int userId,ProduccionEstadoSolicitud model);
    Task<(bool success,string message,int id)> AgregarOperadorAsync(int empresaId,int contextoId,int userId,ProduccionOperadorSolicitud model);
    Task<(bool success,string message,int id)> QuitarOperadorAsync(int empresaId,int contextoId,int userId,ProduccionEstadoSolicitud model);
}
