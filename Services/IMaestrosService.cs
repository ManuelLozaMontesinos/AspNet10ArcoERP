using System.Collections.Generic;
using System.Threading.Tasks;
using AspNet10ArcoERP.Models;

namespace AspNet10ArcoERP.Services
{
    public interface IMaestrosService
    {
        // 1. Color
        Task<List<ColorModel>> GetColoresAsync();
        Task<ColorModel?> GetColorByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveColorAsync(ColorModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoColorAsync(int id, int userId);

        // 2. UIT
        Task<List<UitModel>> GetUitsAsync();
        Task<UitModel?> GetUitByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveUitAsync(UitModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoUitAsync(int id, int userId);

        // 3. Remuneración Mínima Vital
        Task<List<RemMinVitalModel>> GetRemMinVitalAsync();
        Task<RemMinVitalModel?> GetRemMinVitalByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveRemMinVitalAsync(RemMinVitalModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoRemMinVitalAsync(int id, int userId);

        // 4. Centro de Costo
        Task<List<CentroCostoModel>> GetCentrosCostoAsync();
        Task<CentroCostoModel?> GetCentroCostoByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveCentroCostoAsync(CentroCostoModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoCentroCostoAsync(int id, int userId);

        // 5. Condición de Pago
        Task<List<CondPagoModel>> GetCondicionesPagoAsync();
        Task<CondPagoModel?> GetCondPagoByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveCondPagoAsync(CondPagoModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoCondPagoAsync(int id, int userId);

        // 6. Tipo de Cambio
        Task<List<TipoCambioModel>> GetTiposCambioAsync();
        Task<TipoCambioModel?> GetTipoCambioByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveTipoCambioAsync(TipoCambioModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoTipoCambioAsync(int id, int userId);

        // 7. Familia de Producto
        Task<List<GrupoClaseFamiliaModel>> GetFamiliasProductoAsync();
        Task<GrupoClaseFamiliaModel?> GetFamiliaProductoByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveFamiliaProductoAsync(GrupoClaseFamiliaModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoFamiliaProductoAsync(int id, int userId);
        Task<List<LookupItemModel>> GetClasesProductoLookupAsync();

        // 8. Producto
        Task<List<ProductoModel>> GetProductosAsync(string? search = null, int top = 100);
        Task<ProductoModel?> GetProductoByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveProductoAsync(ProductoModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoProductoAsync(int id, int userId);
        Task<List<LookupItemModel>> GetFamiliasProductoLookupAsync();
        Task<List<LookupItemModel>> GetUnidadesMedidaLookupAsync();
        Task<List<LookupItemModel>> GetColoresLookupAsync();

        // 9. Proveedor
        Task<List<ProveedorModel>> GetProveedoresAsync(string? search = null, int top = 100);
        Task<ProveedorModel?> GetProveedorByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveProveedorAsync(ProveedorModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoProveedorAsync(int id, int userId);
        Task<List<LookupItemModel>> GetTiposDocumentoIdentidadLookupAsync();

        // 10. Combo Producto
        Task<List<ComboProductoModel>> GetCombosProductoAsync();
        Task<ComboProductoModel?> GetComboProductoByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveComboProductoAsync(ComboProductoModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoComboProductoAsync(int id, int userId);
        Task<List<LookupItemModel>> GetProductosLookupAsync();

        // 11. Tabla General
        Task<List<TablaGeneralModel>> GetTablasGeneralesAsync();
        Task<TablaGeneralModel?> GetTablaGeneralByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveTablaGeneralAsync(TablaGeneralModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoTablaGeneralAsync(int id, int userId);
    }
}
