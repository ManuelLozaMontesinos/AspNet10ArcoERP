using AspNet10ArcoERP.Models;

namespace AspNet10ArcoERP.Services;

public interface IComprasService
{
    Task<PuntoPedidoPageModel> GetPuntoPedidoAsync(int empresaId,int contextoId,PuntoPedidoFiltro filtro,int page=1,int top=25,string? sort=null,string? dir=null,bool exportar=false);
}
