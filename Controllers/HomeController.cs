using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public HomeController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        private bool EsAdminOGerencia()
        {
            var roleClaims = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            if (roleClaims.Contains("S0001") || roleClaims.Contains("00003") || 
                roleClaims.Any(r => r.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) || 
                                    r.Equals("Administradores", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            // Por defecto en desarrollo para permitir ver todos los controles administrativos
            return true;
        }

        public async Task<IActionResult> Index(int? sedeDashboard = null, int? anio = null)
        {
            var userSessionJson = HttpContext.Session.GetString("USER_SESSION");
            UserSessionDto userSession;

            if (!string.IsNullOrEmpty(userSessionJson))
            {
                userSession = JsonSerializer.Deserialize<UserSessionDto>(userSessionJson) ?? new UserSessionDto();
            }
            else
            {
                userSession = new UserSessionDto
                {
                    UsuarioLogon = User.Identity?.Name ?? "administrador",
                    NombreCompleto = User.FindFirst(ClaimTypes.GivenName)?.Value ?? "Administrador",
                    EmpresaNombre = User.FindFirst("EmpresaNombre")?.Value ?? "ARCO",
                    SedeNombre = User.FindFirst("SedeNombre")?.Value ?? "Sede Principal",
                    PeriodoAnio = User.FindFirst("PeriodoAnio")?.Value ?? "2026"
                };
            }

            ViewBag.UserSession = userSession;
            bool esAdmin = EsAdminOGerencia();
            ViewBag.EsAdmin = esAdmin;
            ViewBag.RolActualCodigo = "S0001";
            ViewBag.SedeDashboardInicial = sedeDashboard ?? 0;

            var vm = await _dashboardService.GetDashboardMetricsAsync(
                vendedorIdFiltro: null, 
                mesesInactivos: 5, 
                vendedorClientesFiltro: null, 
                esAdmin: esAdmin,
                sedeId: sedeDashboard,
                anio: anio
            );

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> V2(int? sedeDashboard = null, int? anio = null)
        {
            return await Index(sedeDashboard, anio);
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerMetricas(int? vendedorId = null, int mesesInactivos = 5, int? vendedorClientes = null, int? sedeId = null, int? anio = null, int? cotizacionVendedorId = null)
        {
            try
            {
                bool esAdmin = EsAdminOGerencia();
                var vm = await _dashboardService.GetDashboardMetricsAsync(vendedorId, mesesInactivos, vendedorClientes, esAdmin, sedeId, anio);

                var response = new
                {
                    success = true,
                    kpis = new
                    {
                        facturacionMes = vm.FacturacionMes,
                        pedidosActivos = vm.PedidosActivos,
                        cotizacionesActivas = vm.CotizacionesActivas,
                        ordenesProduccionMes = vm.OrdenesProduccionMes,
                        ordenesCompraMes = vm.OrdenesCompraMes,
                        pedidosMesMonto = vm.PedidosMesMonto,
                        pedidosMesCantidad = vm.PedidosMesCantidad,
                        consumoInternoMes = vm.ConsumoInternoMes,
                        ingresosMermaMes = vm.IngresosMermaMes,
                        salidasMermaMes = vm.SalidasMermaMes,
                        guiasRemisionMes = vm.GuiasRemisionMes
                    },
                    vendedores = vm.ListaVendedores.Select(v => new { id = v.Id, nombre = v.Nombre }).ToList(),
                    vendedorData = new
                    {
                        id = vm.PrimerVendedorId,
                        nombre = vm.PrimerVendedorNombre,
                        cantidades = vm.VendedorCantidades,
                        montos = vm.VendedorMontos
                    },
                    cotizacionData = new
                    {
                        id = vm.PrimerCotizacionVendedorId,
                        nombre = vm.PrimerCotizacionVendedorNombre,
                        cantidades = vm.CotizacionCantidades,
                        montos = vm.CotizacionMontos
                    },
                    topPedidos = vm.TopPedidos,
                    topPedidosFecha = vm.TopPedidosFecha,
                    clientesInactivos = vm.ClientesInactivos,
                    graficoVentas = new
                    {
                        meses = vm.MesesVentas,
                        ventasAnterior = vm.VentasAnterior,
                        ventasActual = vm.VentasActual,
                        ventasBarraActual = vm.VentasBarraActual
                    },
                    graficoStackedBar = new
                    {
                        labels = vm.StackedLabels,
                        datasets = new object[]
                        {
                            new { label = "Central", data = vm.DsCentral },
                            new { label = "Ilo", data = vm.DsIlo }
                        }
                    },
                    graficoProduccion = new
                    {
                        labels = vm.ProdLabels,
                        valores = vm.ProdValores
                    }
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerPedidosVendedor(int vendedorId, int dias = 12)
        {
            try
            {
                var (labels, cantidades, montos, totalPedidos, totalMonto) = await _dashboardService.GetPedidosVendedorAsync(vendedorId, dias);
                return Json(new
                {
                    success = true,
                    vendedorId = vendedorId,
                    dias = dias,
                    labels = labels,
                    cantidades = cantidades,
                    montos = montos,
                    totalPedidos = totalPedidos,
                    totalMonto = totalMonto
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerCotizacionesVendedor(int vendedorId, int dias = 12, int? anio = null)
        {
            try
            {
                var (labels, cantidades, montos, totalCotizaciones, totalMonto) = await _dashboardService.GetCotizacionesVendedorAsync(vendedorId, dias, anio);
                return Json(new
                {
                    success = true,
                    vendedorId = vendedorId,
                    dias = dias,
                    labels = labels,
                    cantidades = cantidades,
                    montos = montos,
                    totalCotizaciones = totalCotizaciones,
                    totalMonto = totalMonto
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerClientesInactivos(int meses = 5, int? vendedorId = null, int? sedeId = null)
        {
            try
            {
                var list = await _dashboardService.GetClientesInactivosAsync(meses, vendedorId, sedeId);
                return Json(new { success = true, meses = meses, data = list });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerDatosDashboard(int? vendedorId = null, int mesesInactivos = 5, int? vendedorClientes = null, int? sedeId = null, int? anio = null, int? cotizacionVendedorId = null)
        {
            return await ObtenerMetricas(vendedorId, mesesInactivos, vendedorClientes, sedeId, anio, cotizacionVendedorId);
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerKpisPorSede(int? sedeId = null, int? anio = null, int? vendedorId = null)
        {
            try
            {
                var data = await _dashboardService.GetKpisPorSedeAsync(sedeId, anio, vendedorId);
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerStockInsumos(int? almacenId = null, int? sedeId = null)
        {
            try
            {
                var data = await _dashboardService.GetStockInsumosAsync(almacenId, sedeId);
                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerClasesPorGrupo(int? grupoId)
        {
            try
            {
                var data = await _dashboardService.GetClasesPorGrupoAsync(grupoId);
                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerFamiliasPorClase(int? claseId)
        {
            try
            {
                var data = await _dashboardService.GetFamiliasPorClaseAsync(claseId);
                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerTopMenorStock(int? grupoId = null, int? claseId = null, int? familiaId = null, int? sedeId = null)
        {
            try
            {
                var data = await _dashboardService.GetTopMenorStockAsync(grupoId, claseId, familiaId, sedeId);
                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerEventosAgenda(int? areaId = null)
        {
            try
            {
                var data = await _dashboardService.GetEventosAgendaAsync(areaId);
                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerConfiguracionDashboardRoles()
        {
            try
            {
                var data = await _dashboardService.GetDashboardRolesConfigAsync();
                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> GuardarConfiguracionDashboardRol([FromBody] RolConfigModel model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.RolCodigo))
                {
                    return Json(new { success = false, message = "Datos del rol inválidos." });
                }

                bool result = await _dashboardService.SaveDashboardRoleConfigAsync(model.RolCodigo, model.ConfigJson);
                return Json(new { success = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
