using AspNet10ArcoERP.Models;

namespace AspNet10ArcoERP.Services
{
    public interface IAdministracionService
    {
        Task<PeriodoSistemaPageModel> GetPeriodosAsync(int? searchYear = null, int page = 1, int pageSize = 25);
        Task<PeriodoSistemaModel?> GetPeriodoAsync(int id);
        Task<(bool success, string message, int id)> SavePeriodoAsync(PeriodoSistemaModel model, int userId);
    }
}
