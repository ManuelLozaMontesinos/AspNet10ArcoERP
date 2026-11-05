using System.Collections.Generic;
using System.Threading.Tasks;
using AspNet10ArcoERP.Models;

namespace AspNet10ArcoERP.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardMetricsAsync(int? vendedorIdFiltro = null, int mesesInactivos = 5, int? vendedorClientesFiltro = null, bool esAdmin = true, int? sedeId = null, int? anio = null);
        Task<List<ClienteInactivoDto>> GetClientesInactivosAsync(int meses = 5, int? vendedorId = null, int? sedeId = null);
        Task<(List<string> labels, List<int> cantidades, List<double> montos, int totalPedidos, string totalMonto)> GetPedidosVendedorAsync(int vendedorId, int dias = 12);
        Task<(List<string> labels, List<int> cantidades, List<double> montos, int totalCotizaciones, string totalMonto)> GetCotizacionesVendedorAsync(int vendedorId, int dias = 12, int? anio = null);
        Task<dynamic> GetKpisPorSedeAsync(int? sedeId = null, int? anio = null, int? vendedorId = null);
        Task<List<ProductoStockDto>> GetStockInsumosAsync(int? almacenId = null, int? sedeId = null);
        Task<List<AlmacenItemDto>> GetAlmacenesActivosAsync(int? sedeId = null);
        Task<List<CategoriaFiltroItemDto>> GetGruposActivosAsync();
        Task<List<CategoriaFiltroItemDto>> GetClasesPorGrupoAsync(int? grupoId);
        Task<List<CategoriaFiltroItemDto>> GetFamiliasPorClaseAsync(int? claseId);
        Task<List<TopMenorStockDto>> GetTopMenorStockAsync(int? grupoId = null, int? claseId = null, int? familiaId = null, int? sedeId = null);
        Task<List<CalendarioAreaDto>> GetAreasAgendaAsync(int usuarioId, bool esAdmin);
        Task<List<CalendarioEventoDto>> GetEventosAgendaAsync(int? areaId = null);
        Task<Dictionary<string, string>> GetDashboardRolesConfigAsync();
        Task<bool> SaveDashboardRoleConfigAsync(string rolCodigo, string configJson);
    }
}
