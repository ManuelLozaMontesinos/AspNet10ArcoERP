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
        Task<List<TipoCambioModel>> GetTiposCambioAsync(string? search = null, int? tipo = null, string? estado = null, int top = 25);
        Task<TipoCambioModel?> GetTipoCambioByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveTipoCambioAsync(TipoCambioModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoTipoCambioAsync(int id, int userId);
        Task<List<TipoCambioCatalogoItem>> GetTiposCambioCatalogoAsync();
        Task<List<MonedaCatalogoItem>> GetMonedasCatalogoAsync();

        // 7. Familia de Producto
        Task<List<GrupoClaseFamiliaModel>> GetFamiliasProductoAsync();
        Task<GrupoClaseFamiliaModel?> GetFamiliaProductoByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveFamiliaProductoAsync(GrupoClaseFamiliaModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoFamiliaProductoAsync(int id, int userId);
        Task<List<LookupItemModel>> GetClasesProductoLookupAsync(int? grupoId = null);

        // 8. Producto y Servicio
        Task<List<ProductoModel>> GetProductosAsync(
            string? search = null, 
            string? tipo = null, 
            int? grupoId = null, 
            int? claseId = null,
            int? familiaId = null, 
            string? estado = null, 
            string? sort = null, 
            string? dir = null, 
            int top = 25);
        Task<(int total, int productos, int servicios)> GetProductosCountsAsync();
        Task<ProductoModel?> GetProductoByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveProductoAsync(ProductoModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoProductoAsync(int id, int userId);
        Task<List<LookupItemModel>> GetTiposProductoLookupAsync();
        Task<List<LookupItemModel>> GetGruposProductoLookupAsync(int? tipoProductoId = null);
        Task<List<LookupItemModel>> GetFamiliasProductoLookupAsync(int? grupoId = null, int? claseId = null);
        Task<List<LookupItemModel>> GetUnidadesMedidaLookupAsync();
        Task<List<LookupItemModel>> GetColoresLookupAsync();
        Task<List<LookupItemModel>> GetTiposIgvLookupAsync();
        Task<List<LookupItemModel>> GetEstadosProductoLookupAsync();
        Task<List<LookupItemModel>> GetEstadosComercialesProductoLookupAsync();

        // 8.1 Presentaciones para la Venta
        Task<List<ProductoPresentacionVentaModel>> GetPresentacionesVentaAsync(int productoId);
        Task<(bool success, string message, int id)> SavePresentacionVentaAsync(ProductoPresentacionVentaModel model, int userId);
        Task<(bool success, string message)> DeletePresentacionVentaAsync(int id, int userId);

        // 8.2 Procesos de Producción
        Task<List<ProductoProcesoModel>> GetProductoProcesosAsync(int productoId);
        Task<(bool success, string message, int id)> SaveProductoProcesoAsync(ProductoProcesoModel model, int userId);
        Task<(bool success, string message)> DeleteProductoProcesoAsync(int id, int userId);
        Task<List<SubProcesoLookupModel>> GetSubProcesosLookupAsync();

        // 8.3 Detalles Requeridos
        Task<List<ProductoDetalleModel>> GetProductoDetallesAsync(int productoId);
        Task<(bool success, string message, int id)> SaveProductoDetalleAsync(ProductoDetalleModel model, int userId);
        Task<(bool success, string message)> DeleteProductoDetalleAsync(int id, int userId);
        Task<(bool success, string message)> UpdateDetalleRequeridoAsync(int productoId, string detalleReq, int userId);

        // 8.4 Color Rápido
        Task<(bool success, string message, int id, string nombre)> QuickCreateColorAsync(string nombre, int userId);

        // 9. Cliente / Proveedor
        Task<List<ProveedorModel>> GetProveedoresAsync(
            string? search = null, 
            int? tipoProveedorId = null, 
            string? estado = null, 
            int? tipoNaturalezaId = null, 
            string? sort = null, 
            string? dir = null, 
            int top = 25);
        Task<ProveedorModel?> GetProveedorByIdAsync(int id);
        Task<ProveedorModel?> BuscarProveedorPorDocumentoAsync(string doc);
        Task<(bool success, string message, int id, string codigo)> SaveProveedorAsync(ProveedorModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoProveedorAsync(int id, int userId);
        Task<List<LookupItemModel>> GetTiposNaturalezaJuridicaLookupAsync();
        Task<List<TipoDocIdentidadLookupModel>> GetTiposDocumentoIdentidadLookupAsync(int? tipoNaturalezaJuridicaId = null);
        Task<List<LookupItemModel>> GetTiposViaLookupAsync();
        Task<List<LookupItemModel>> GetTiposZonaLookupAsync();
        Task<List<LookupItemModel>> GetTiposProveedorLookupAsync();
        Task<List<LookupItemModel>> GetCondicionesPagoLookupAsync();
        Task<List<LookupItemModel>> GetDepartamentosLookupAsync();
        Task<List<UbigeoLookupModel>> GetProvinciasLookupAsync(int departamentoId);
        Task<List<UbigeoLookupModel>> GetDistritosLookupAsync(int provinciaId);

        // 10. Combos y Kits de Productos
        Task<List<ComboProductoModel>> GetCombosProductoAsync();
        Task<ComboProductoModel?> GetComboProductoByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveComboProductoAsync(ComboProductoModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoComboProductoAsync(int id, int userId);
        Task<List<LookupItemModel>> GetProductosLookupAsync();

        // 10.1 Gestión Integral de Combos / Kits (Cabecera y Detalle)
        Task<List<ProductoComboModel>> GetCombosBandejaAsync(string? search, int? grupoId, int? claseId, int? familiaId, string? estado, string? sort, string? dir, int top = 25);
        Task<ProductoComboModel?> GetComboCabeceraByIdAsync(int id);
        Task<(bool success, string message, int id, string codigo)> SaveComboCabeceraAsync(ProductoComboModel model, int userId);
        Task<List<ComboDetalleItemModel>> GetComboDetallesAsync(int productoId);
        Task<(bool success, string message, int id)> SaveComboDetalleAsync(ComboDetalleItemModel model, int userId);
        Task<(bool success, string message)> DeleteComboDetalleAsync(int comboProductoId, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoComboAsync(int productoId, int userId);
        Task<List<LookupItemModel>> BuscarProductosParaComboAsync(string search);
        Task<List<LookupItemModel>> GetGruposCombosLookupAsync();

        // 11. Tabla General
        Task<List<TablaGeneralModel>> GetTablasGeneralesAsync();
        Task<TablaGeneralModel?> GetTablaGeneralByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveTablaGeneralAsync(TablaGeneralModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoTablaGeneralAsync(int id, int userId);
    }
}
