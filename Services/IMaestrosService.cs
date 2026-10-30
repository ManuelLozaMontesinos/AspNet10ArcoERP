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
        Task<List<CentroCostoModel>> GetCentrosCostoAsync(int empresaId = 1);
        Task<CentroCostoModel?> GetCentroCostoByIdAsync(int id, int empresaId = 1);
        Task<(bool success, string message, int id)> SaveCentroCostoAsync(CentroCostoModel model, int userId, int empresaId = 1);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoCentroCostoAsync(int id, int userId, int empresaId = 1);

        // 5. Condición de Pago
        Task<List<CondPagoModel>> GetCondicionesPagoAsync(int empresaId = 1);
        Task<CondPagoModel?> GetCondPagoByIdAsync(int id, int empresaId = 1);
        Task<(bool success, string message, int id)> SaveCondPagoAsync(CondPagoModel model, int userId, int empresaId = 1);
        Task<List<LookupItemModel>> GetTiposCondicionPagoLookupAsync();
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoCondPagoAsync(int id, int userId, int empresaId = 1);

        // 6. Tipo de Cambio
        Task<List<TipoCambioModel>> GetTiposCambioAsync(string? search = null, int? tipo = null, string? estado = null, int top = 25);
        Task<TipoCambioModel?> GetTipoCambioByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveTipoCambioAsync(TipoCambioModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoTipoCambioAsync(int id, int userId);
        Task<List<TipoCambioCatalogoItem>> GetTiposCambioCatalogoAsync();
        Task<List<MonedaCatalogoItem>> GetMonedasCatalogoAsync();
        Task<List<TipoCambioMaestroModel>> GetTiposCambioMaestrosAsync();
        Task<TipoCambioMaestroModel?> GetTipoCambioMaestroByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveTipoCambioMaestroAsync(TipoCambioMaestroModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoTipoCambioMaestroAsync(int id, int userId);

        // 7. Familia de Producto
        Task<List<GrupoClaseFamiliaModel>> GetFamiliasProductoAsync(int? claseId = null, int? grupoId = null);
        Task<GrupoClaseFamiliaModel?> GetFamiliaProductoByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveFamiliaProductoAsync(GrupoClaseFamiliaModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoFamiliaProductoAsync(int id, int userId);
        Task<List<LookupItemModel>> GetClasesProductoLookupAsync(int? grupoId = null, string? tipo = null);
        Task<List<GrupoProductoModel>> GetGruposProductoAsync(int? tipoProductoId = null, string? search = null, string? estado = null);
        Task<GrupoProductoModel?> GetGrupoProductoByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveGrupoProductoAsync(GrupoProductoModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoGrupoProductoAsync(int id, int userId);
        Task<List<ClaseProductoModel>> GetClasesProductoAsync(int? grupoId = null);
        Task<ClaseProductoModel?> GetClaseProductoByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveClaseProductoAsync(ClaseProductoModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoClaseProductoAsync(int id, int userId);
        Task<List<LookupItemModel>> GetTiposDetraccionLookupAsync();

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
        Task<List<LookupItemModel>> GetGruposProductoLookupAsync(int? tipoProductoId = null, string? tipo = null);
        Task<List<LookupItemModel>> GetFamiliasProductoLookupAsync(int? grupoId = null, int? claseId = null, string? tipo = null);
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
            int top = 25,
            int empresaId = 1,
            string? esProspecto = null);
        Task<ProveedorModel?> GetProveedorByIdAsync(int id, int empresaId = 1);
        Task<ProveedorModel?> BuscarProveedorPorDocumentoAsync(string doc, int empresaId = 1);
        Task<(bool success, string message, int id, string codigo)> SaveProveedorAsync(ProveedorModel model, int userId, int empresaId = 1);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoProveedorAsync(int id, int userId, int empresaId = 1);
        Task<List<LookupItemModel>> GetTiposNaturalezaJuridicaLookupAsync();
        Task<List<TipoDocIdentidadLookupModel>> GetTiposDocumentoIdentidadLookupAsync(int? tipoNaturalezaJuridicaId = null);
        Task<List<LookupItemModel>> GetTiposViaLookupAsync();
        Task<List<LookupItemModel>> GetTiposZonaLookupAsync();
        Task<List<LookupItemModel>> GetTiposProveedorLookupAsync();
        Task<List<LookupItemModel>> GetCondicionesPagoLookupAsync(int empresaId = 1);
        Task<List<LookupItemModel>> GetTiposProfesionLookupAsync();
        Task<List<LookupItemModel>> GetTiposVentaClienteLookupAsync();
        Task<List<LookupItemModel>> GetDepartamentosLookupAsync();
        Task<List<UbigeoLookupModel>> GetProvinciasLookupAsync(int departamentoId);
        Task<List<UbigeoLookupModel>> GetDistritosLookupAsync(int provinciaId);

        // 10. Combos y Kits de Productos
        Task<List<ComboProductoModel>> GetCombosProductoAsync();
        Task<ComboProductoModel?> GetComboProductoByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveComboProductoAsync(ComboProductoModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoComboProductoAsync(int id, int userId);
        Task<List<LookupItemModel>> GetProductosLookupAsync();

        // Transportistas and related vehicles/drivers
        Task<List<TransportistaModel>> GetTransportistasAsync(string? search = null, string? estado = null, int top = 25, int empresaId = 1);
        Task<TransportistaModel?> GetTransportistaByIdAsync(int id, int empresaId);
        Task<(bool success, string message, int id)> SaveTransportistaAsync(TransportistaModel model, int userId, int empresaId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoTransportistaAsync(int id, int userId, int empresaId);
        Task<List<TransportistaUnidadModel>> GetTransportistaUnidadesAsync(int transportistaId);
        Task<(bool success, string message, int id)> SaveTransportistaUnidadAsync(TransportistaUnidadModel model, int userId);
        Task<(bool success, string message)> DeleteTransportistaUnidadAsync(int id, int userId);
        Task<List<TransportistaConductorModel>> GetTransportistaConductoresAsync(int transportistaId);
        Task<(bool success, string message, int id)> SaveTransportistaConductorAsync(TransportistaConductorModel model, int userId);
        Task<(bool success, string message)> DeleteTransportistaConductorAsync(int id, int userId);
        Task<List<TipoDocIdentidadLookupModel>> GetDocumentosTransportistaLookupAsync();

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
        Task<List<LookupItemModel>> GetClasesCombosLookupAsync(int? grupoId = null);
        Task<List<LookupItemModel>> GetFamiliasCombosLookupAsync(int? grupoId = null, int? claseId = null);

        // 10.2 Cuentas bancarias de empresa
        Task<CuentaBancoPageModel> GetCuentasBancoPageAsync(int empresaId, string? search = null, int? bancoId = null, string? estado = null, int page = 1, int pageSize = 25, string? sort = null, string? dir = null);
        Task<CuentaBancoModel?> GetCuentaBancoByIdAsync(int id, int empresaId);
        Task<List<LookupItemModel>> GetBancosCuentaLookupAsync();
        Task<List<LookupItemModel>> GetMonedasCuentaLookupAsync();
        Task<(bool success, string message, int id)> SaveCuentaBancoAsync(CuentaBancoModel model, int userId, int empresaId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoCuentaBancoAsync(int id, int userId, int empresaId);

        // 11. Tabla General
        Task<List<TablaGeneralModel>> GetTablasGeneralesAsync();
        Task<TablaGeneralModel?> GetTablaGeneralByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveTablaGeneralAsync(TablaGeneralModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoTablaGeneralAsync(int id, int userId);
    }
}
