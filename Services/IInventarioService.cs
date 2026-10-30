using AspNet10ArcoERP.Models;

namespace AspNet10ArcoERP.Services;

public interface IInventarioService
{
    Task<VolumenExistenciaPageModel> GetVolumenExistenciasAsync(int empresaId, int scopeId, int userId, int? sedeId = null, string? search = null, int page = 1, int top = 25, string? sort = null, string? dir = null, bool exportar = false);
    Task<StockMinimoRowModel?> GetStockMinimoAsync(int empresaId, int scopeId, int userId, int sedeId, int productoId);
    Task<StockMinimoImportacionModel> GetImportacionMinimosAsync(int empresaId, int scopeId, int userId, int sedeId, Stream archivo);
    Task<(bool success, string message, int id)> SaveStockMinimoAsync(int empresaId, int scopeId, int userId, StockMinimoSolicitud solicitud);
    Task<(bool success, string message, int id)> SaveImportacionMinimosAsync(int empresaId, int scopeId, int userId, StockMinimoSolicitud solicitud);
    Task<SaldoUnidadesPageModel> GetSaldoUnidadesAsync(int empresaId, int scopeId, int userId, InventarioReporteFiltro filtro, int page = 1, int top = 25, string? sort = null, string? dir = null, bool exportar = false);
    Task<List<SaldoAlmacenDetalleModel>?> GetSaldoUnidadesDetalleAsync(int empresaId, int scopeId, int userId, InventarioReporteFiltro filtro, int productoId);
    Task<SaldoValoradoPageModel> GetSaldoValoradoAsync(int empresaId, int scopeId, int userId, InventarioReporteFiltro filtro, int page = 1, int top = 25, string? sort = null, string? dir = null, bool exportar = false);
    Task<InventarioProcesoPageModel> GetProcesosInventarioAsync(int empresaId, int scopeId, int userId, int? almacenId = null, string? search = null, string? estado = null, int page = 1, int top = 25, string? sort = null, string? dir = null);
    Task<InventarioProcesoPreparacionModel> GetPreparacionProcesoAsync(int empresaId, int scopeId, int userId, string operacion, int almacenId, int? mesContableId = null);
    Task<(bool success, string message, int id)> EjecutarProcesoInventarioAsync(int empresaId, int scopeId, int userId, string operacion, InventarioProcesoSolicitud solicitud);
    Task<LlegadaCompraPageModel> GetLlegadasCompraAsync(int empresaId, int scopeId, int userId, LlegadaCompraFiltro filtro, int page = 1, int top = 25, string? sort = null, string? dir = null);
    Task<SaldoConsultaPageModel> GetConsultaSaldosAsync(int empresaId, int periodoEmpresaSedeId, int usuarioId, InventarioReporteFiltro filtro, int page = 1, int top = 25, string? sort = null, string? dir = null, bool exportar = false);
    Task<List<SaldoAlmacenDetalleModel>?> GetSaldoDetalleAsync(int empresaId, int periodoEmpresaSedeId, int usuarioId, int mes, int productoId);
    Task<KardexUnidadPageModel> GetKardexUnidadesAsync(int empresaId, int periodoEmpresaSedeId, int usuarioId, InventarioReporteFiltro filtro, int page = 1, int top = 25, string? sort = null, string? dir = null, bool exportar = false);
    Task<MovimientoReportePageModel> GetReporteMovimientosAsync(int empresaId, int periodoEmpresaSedeId, int usuarioId, InventarioReporteFiltro filtro, int page = 1, int top = 25, string? sort = null, string? dir = null, bool exportar = false);
    Task<InventarioLookupPageModel> GetReporteLookupAsync(int empresaId, string tipo, string? search = null, int page = 1, string? sort = null, string? dir = null);
    Task<InventarioMovimientoDetalleModel?> GetReporteMovimientoDetalleAsync(int empresaId, int periodoEmpresaSedeId, int usuarioId, int id, bool todasSedes);
    Task<TransaccionAlmacenPageModel> GetTransaccionesAsync(int empresaId, string? search = null, int? tipoMovimientoId = null, string? estado = null, int page = 1, int top = 25, string? sort = null, string? dir = null);
    Task<TransaccionAlmacenModel?> GetTransaccionAsync(int id, int empresaId);
    Task<(bool success, string message, int id)> SaveTransaccionAsync(TransaccionAlmacenModel model, int empresaId, int userId);
    Task<(bool success, string message, int id)> AnularTransaccionAsync(int id, int empresaId, int userId);
    Task<AlmacenPageModel> GetAlmacenesAsync(int empresaId, string? search = null, int? sedeId = null, string? estado = null, int page = 1, int top = 25, string? sort = null, string? dir = null);
    Task<AlmacenModel?> GetAlmacenAsync(int id, int empresaId);
    Task<(bool success, string message, int id)> SaveAlmacenAsync(AlmacenModel model, int empresaId, int userId);
    Task<(bool success, string message, int id)> AnularAlmacenAsync(int id, int empresaId, int userId);
    Task<AlmacenDetalleModel?> GetAlmacenDetalleAsync(int almacenId, int empresaId);
    Task<List<AlmacenConfiguracionModel>?> GetConfiguracionAsync(int almacenId, int almacenTransaccionId, int empresaId);
    Task<(bool success, string message, int id)> SaveAlmacenTransaccionAsync(AlmacenTransaccionModel model, int empresaId, int userId);
    Task<(bool success, string message, int id)> AnularAlmacenTransaccionAsync(int almacenId, int id, int empresaId, int userId);
    Task<(bool success, string message, int id)> SaveConfiguracionAsync(int almacenId, AlmacenConfiguracionModel model, int empresaId, int userId);
    Task<(bool success, string message, int id)> AgregarSerieAsync(int almacenId, int documentoSerieId, int empresaId, int userId);
    Task<(bool success, string message, int id)> AnularSerieAsync(int almacenId, int id, int empresaId, int userId);
}
