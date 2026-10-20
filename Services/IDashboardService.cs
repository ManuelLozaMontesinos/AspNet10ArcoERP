using System.Collections.Generic;
using System.Threading.Tasks;
using AspNet10ArcoERP.Models;

namespace AspNet10ArcoERP.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardMetricsAsync(int mesesInactivos = 5);
        Task<List<ClienteInactivoDto>> GetClientesInactivosAsync(int meses = 5);
    }
}

