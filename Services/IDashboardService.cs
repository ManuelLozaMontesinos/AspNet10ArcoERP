using System.Collections.Generic;
using System.Threading.Tasks;
using AspNet10ArcoERP.Models;

namespace AspNet10ArcoERP.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardMetricsAsync(int? vendedorIdFiltro = null, int mesesInactivos = 5, int? vendedorClientesFiltro = null, bool esAdmin = true);
        Task<List<ClienteInactivoDto>> GetClientesInactivosAsync(int meses = 5, int? vendedorId = null);
        Task<(List<string> labels, List<int> cantidades, List<double> montos, int totalPedidos, string totalMonto)> GetPedidosVendedorAsync(int vendedorId, int dias = 12);
    }
}
