using AspNet10ArcoERP.Models;

namespace AspNet10ArcoERP.Services;

public interface IReporteDocumentosService
{
    Task<DocumentoReportePageModel> GetReporteAsync(int empresaId,int contextoId,string tipo,DocumentoReporteFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null,bool exportar=false);
}
