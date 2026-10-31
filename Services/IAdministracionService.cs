using AspNet10ArcoERP.Models;

namespace AspNet10ArcoERP.Services
{
    public interface IAdministracionService
    {
        Task<ParametroSistemaPageModel> GetParametrosSistemaAsync(int empresaId,int contextoId,string? search=null,int? grupoId=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null);
        Task<ParametroSistemaModel?> GetParametroSistemaAsync(int empresaId,int contextoId,int id);
        Task<(bool success,string message,int id)> SaveParametroSistemaAsync(int empresaId,int contextoId,int userId,ParametroSistemaSolicitud model);
        Task<DocumentoSeriePageModel> GetDocumentoSeriesAsync(int empresaId,string? search=null,int? sedeId=null,int? tipoDocumentoId=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null);
        Task<DocumentoSerieModel?> GetDocumentoSerieAsync(int empresaId,int id);
        Task<List<LookupItemModel>> GetSerieAlmacenesAsync(int empresaId,int sedeId);
        Task<(bool success,string message,int id)> SaveDocumentoSerieAsync(int empresaId,int userId,MaestroEdicion<DocumentoSerieModel> model);
        Task<(bool success,string message,int id)> AnularDocumentoSerieAsync(int empresaId,int userId,int id,string revision);
        Task<PeriodoSistemaPageModel> GetPeriodosAsync(int? searchYear = null, int page = 1, int pageSize = 25, string? sort = null, string? dir = null);
        Task<PeriodoSistemaModel?> GetPeriodoAsync(int id);
        Task<(bool success, string message, int id)> SavePeriodoAsync(PeriodoSistemaModel model, int userId);

        // Usuarios y Seguridad
        Task<UsuarioPageModel> GetUsuariosAsync(int empresaId, string? search = null, string? estado = null, int page = 1, int top = 25, string? sort = null, string? dir = null);
        Task<UsuarioDetalleModel?> GetUsuarioDetalleAsync(int empresaId, int id);
        Task<(bool success, string message, int id)> SaveUsuarioAsync(int empresaId, int userId, UsuarioGuardarSolicitud model);
        Task<(bool success, string message)> CambiarEstadoUsuarioAsync(int id, string nuevoEstado);
        Task<(bool success, string message)> CambiarContrasenaUsuarioAsync(UsuarioCambiarContrasenaSolicitud model);

        // Roles
        Task<RolPageModel> GetRolesAsync(int empresaId, string? search = null, string? estado = null, int page = 1, int top = 25, string? sort = null, string? dir = null);
        Task<(bool success, string message, int id)> SaveRolAsync(int empresaId, int userId, RolGuardarSolicitud model);
        Task<(bool success, string message)> AnularRolAsync(int id, int userId);

        // Empresas y Sedes
        Task<EmpresaPageModel> GetEmpresasAsync(string? search = null, string? estado = null, int page = 1, int top = 25, string? sort = null, string? dir = null);
        Task<EmpresaDetalleModel?> GetEmpresaDetalleAsync(int id);
        Task<List<LookupItemModel>> GetProvinciasPorDepartamentoAsync(int departamentoId);
        Task<List<LookupItemModel>> GetDistritosPorProvinciaAsync(int provinciaId);
        Task<(bool success, string message, int id)> SaveEmpresaAsync(int userId, EmpresaGuardarSolicitud model);
        Task<(bool success, string message)> CambiarEstadoEmpresaAsync(int id, string nuevoEstado);
        Task<List<EmpresaSedeModel>> GetEmpresaSedesAsync(int empresaId);
        Task<(bool success, string message, int id)> SaveEmpresaSedeAsync(int userId, EmpresaSedeSolicitud model);
        Task<(bool success, string message)> CambiarEstadoEmpresaSedeAsync(int id, string nuevoEstado);
        Task<List<PeriodoSedeItemModel>> GetPeriodoSedesAsync(int empresaId, int periodoId);
        Task<(bool success, string message)> AsignarSedePeriodoAsync(int periodoId, int sedeId, int userId);
        Task<(bool success, string message)> EliminarSedePeriodoAsync(int periodoEmpresaSedeId);
    }
}
