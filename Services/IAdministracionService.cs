using AspNet10ArcoERP.Models;

namespace AspNet10ArcoERP.Services
{
    public interface IAdministracionService
    {
        Task<DocumentoSeriePageModel> GetDocumentoSeriesAsync(int empresaId,string? search=null,int? sedeId=null,int? tipoDocumentoId=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null);
        Task<DocumentoSerieModel?> GetDocumentoSerieAsync(int empresaId,int id);
        Task<List<LookupItemModel>> GetSerieAlmacenesAsync(int empresaId,int sedeId);
        Task<(bool success,string message,int id)> SaveDocumentoSerieAsync(int empresaId,int userId,MaestroEdicion<DocumentoSerieModel> model);
        Task<(bool success,string message,int id)> AnularDocumentoSerieAsync(int empresaId,int userId,int id,string revision);
        Task<PeriodoSistemaPageModel> GetPeriodosAsync(int? searchYear = null, int page = 1, int pageSize = 25, string? sort = null, string? dir = null);
        Task<PeriodoSistemaModel?> GetPeriodoAsync(int id);
        Task<(bool success, string message, int id)> SavePeriodoAsync(PeriodoSistemaModel model, int userId);
    }
}
