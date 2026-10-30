using AspNet10ArcoERP.Models;

namespace AspNet10ArcoERP.Services
{
    public interface IMKService
    {
        Task<LeadSourcePageModel> GetLeadSourcesAsync(string? search = null, string? estado = null, int page = 1, int pageSize = 25, string? sort = null, string? dir = null);
        Task<LeadSourceModel?> GetLeadSourceByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveLeadSourceAsync(LeadSourceModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleLeadSourceAsync(int id, int userId);
        Task<TipoProfesionPageModel> GetTipoProfesionesAsync(string? search = null, string? estado = null, int page = 1, int pageSize = 25, string? sort = null, string? dir = null);
        Task<TipoProfesionModel?> GetTipoProfesionByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveTipoProfesionAsync(TipoProfesionModel model, int userId);
        Task<(bool success, string message)> InactivarTipoProfesionAsync(int id, int userId);
        Task<OportunidadVentaPageModel> GetOportunidadesVentaAsync(string? search = null, string? estado = null, int page = 1, int pageSize = 25, string? sort = null, string? dir = null);
        Task<OportunidadVentaModel?> GetOportunidadVentaByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveOportunidadVentaAsync(OportunidadVentaModel model, int userId);
        Task<(bool success, string message)> InactivarOportunidadVentaAsync(int id, int userId);
        Task<TipoVentaClientePageModel> GetTiposVentaClienteAsync(string? search = null, string? estado = null, int page = 1, int pageSize = 25, string? sort = null, string? dir = null);
        Task<TipoVentaClienteModel?> GetTipoVentaClienteByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveTipoVentaClienteAsync(TipoVentaClienteModel model, int userId);
        Task<(bool success, string message)> InactivarTipoVentaClienteAsync(int id, int userId);
        Task<List<LookupItemModel>> GetLeadSourceLookupsAsync();
        Task<List<LookupItemModel>> GetOportunidadVentaLookupsAsync();
        Task<List<int>?> GetProspectOpportunityIdsAsync(int prospectoId, int empresaId);
        Task<(bool success, string message)> SaveProspectOpportunityIdsAsync(int prospectoId, int empresaId, IEnumerable<int> oportunidadIds, int userId);
        Task<MerchandisingMovementPageModel> GetMerchandisingMovementsAsync(
            string? search, int? transaccionId, DateTime? desde, DateTime? hasta,
            int periodoAnio, int empresaSedeId, int page, int pageSize, string? sort = null, string? dir = null);
        Task<MerchandisingMovementDetailModel?> GetMerchandisingMovementDetailAsync(
            int id, int periodoAnio, int empresaSedeId);
        Task<List<LookupItemModel>> GetMerchandisingTransactionsAsync(int periodoAnio, int empresaSedeId);
    }
}
