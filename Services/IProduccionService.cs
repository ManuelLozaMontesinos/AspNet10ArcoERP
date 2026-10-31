using AspNet10ArcoERP.Models;

namespace AspNet10ArcoERP.Services;

public interface IProduccionService
{
    Task<ProduccionReportePageModel> GetReporteProcesosAsync(int empresaId,int contextoId,ProduccionReporteFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null,bool exportar=false);
    Task<ProduccionPageModel> GetProcesosAsync(int empresaId,int contextoId,string? search=null,string? estado=null,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<ProduccionProcesoDetalleModel?> GetProcesoAsync(int empresaId,int contextoId,int id);
    Task<ProduccionPageModel?> GetDetallesProcesoAsync(int empresaId,int contextoId,int procesoId,string tipo,int subProcesoId=0,string? search=null,int page=1,int top=25,string? sort=null,string? dir=null);
    Task<(bool success,string message,int id)> SaveProcesoAsync(int empresaId,int contextoId,int userId,ProduccionNombreSolicitud model);
    Task<(bool success,string message,int id)> SaveSubprocesoAsync(int empresaId,int contextoId,int userId,ProduccionNombreSolicitud model);
    Task<(bool success,string message,int id)> AnularProcesoAsync(int empresaId,int contextoId,int userId,ProduccionEstadoSolicitud model);
    Task<(bool success,string message,int id)> AgregarOperadorAsync(int empresaId,int contextoId,int userId,ProduccionOperadorSolicitud model);
    Task<(bool success,string message,int id)> QuitarOperadorAsync(int empresaId,int contextoId,int userId,ProduccionEstadoSolicitud model);

    // Rutas de Producción
    Task<RutaProduccionPageModel> GetRutasProduccionAsync(int empresaId, int contextoId, string? search = null, string? estado = null, int page = 1, int top = 25, string? sort = null, string? dir = null);
    Task<RutaProduccionDetalleModel?> GetRutaProduccionDetalleAsync(int empresaId, int id);
    Task<List<LookupItemModel>> GetSubprocesosDisponiblesAsync(int empresaId, int rutaProduccionId);
    Task<(bool success, string message, int id)> SaveRutaProduccionAsync(int empresaId, int contextoId, int userId, RutaProduccionGuardarSolicitud model);
    Task<(bool success, string message, int id)> AnularRutaProduccionAsync(int empresaId, int contextoId, int userId, int id);
    Task<(bool success, string message, int id)> AgregarSubprocesoRutaAsync(int empresaId, int contextoId, int userId, RutaSubProcesoGuardarSolicitud model);
    Task<(bool success, string message, int id)> ActualizarOrdenSubprocesoRutaAsync(int empresaId, int contextoId, int userId, RutaSubProcesoOrdenSolicitud model);
    Task<(bool success, string message, int id)> QuitarSubprocesoRutaAsync(int empresaId, int contextoId, int userId, int id);

    // Órdenes de Producción
    Task<OrdenProduccionPageModel> GetOrdenesProduccionAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        string? codigo = null,
        string? cotizacion = null,
        string? cliente = null,
        string? vendedor = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? estadoOrdenProduccionId = null,
        bool mostrarAnulados = false,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null);
    Task<OrdenProduccionDetalleModel?> GetOrdenProduccionDetalleAsync(int empresaId, int contextoId, int id);
    Task<(bool success, string message, int id)> SaveOrdenProduccionAsync(int empresaId, int contextoId, int userId, OrdenProduccionGuardarSolicitud model);
    Task<(bool success, string message)> AnularOrdenProduccionAsync(int empresaId, int contextoId, int userId, int id);
    Task<(bool success, string message)> CambiarEstadoOrdenProduccionAsync(int empresaId, int contextoId, int userId, OrdenProduccionCambiarEstadoSolicitud model);
    Task<(bool success, string message, int id)> SaveOrdenProduccionProductoAsync(int empresaId, int contextoId, int userId, OrdenProduccionProductoGuardarSolicitud model);
    Task<(bool success, string message)> AnularOrdenProduccionProductoAsync(int empresaId, int contextoId, int userId, int productoItemId);
    Task<(bool success, string message, int id)> SaveOrdenProduccionSubprocesoAsync(int empresaId, int contextoId, int userId, OrdenProduccionSubprocesoGuardarSolicitud model);
    Task<(bool success, string message)> AnularOrdenProduccionSubprocesoAsync(int empresaId, int contextoId, int userId, int subprocesoItemId);

    // Requerimiento de Insumos
    Task<RequeInsumoPageModel> GetRequerimientosInsumoAsync(
        int empresaId,
        int contextoId,
        string? search = null,
        string? codigo = null,
        string? cotizacion = null,
        string? cliente = null,
        string? vendedor = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? estadoDespachoId = null,
        bool mostrarAnulados = false,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null);
    Task<(bool success, string message)> CerrarInsumoAsync(int empresaId, int contextoId, int userId, RequeInsumoCerrarSolicitud model);

    // Asignación de Producción
    Task<List<PersonalMaquinaItemModel>> GetPersonalMaquinasAsync(int empresaId, DateTime? fecha = null);
    Task<(bool success, string message)> AsignarCortadorAsync(int empresaId, int contextoId, int userId, AsignarCortadorSolicitud model);
}
