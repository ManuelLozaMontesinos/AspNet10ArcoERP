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

        public async Task<IActionResult> Index()
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

            var vm = await _dashboardService.GetDashboardMetricsAsync(
                vendedorIdFiltro: null, 
                mesesInactivos: 5, 
                vendedorClientesFiltro: null, 
                esAdmin: esAdmin
            );

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> V2()
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

            var vm = await _dashboardService.GetDashboardMetricsAsync(
                vendedorIdFiltro: null, 
                mesesInactivos: 5, 
                vendedorClientesFiltro: null, 
                esAdmin: esAdmin
            );

            return View(vm);
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerMetricas(int? vendedorId = null, int mesesInactivos = 5, int? vendedorClientes = null)
        {
            try
            {
                bool esAdmin = EsAdminOGerencia();
                var vm = await _dashboardService.GetDashboardMetricsAsync(vendedorId, mesesInactivos, vendedorClientes, esAdmin);

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
                        pedidosMesCantidad = vm.PedidosMesCantidad
                    },
                    vendedores = vm.ListaVendedores.Select(v => new { id = v.Id, nombre = v.Nombre }).ToList(),
                    vendedorData = new
                    {
                        id = vm.PrimerVendedorId,
                        nombre = vm.PrimerVendedorNombre,
                        cantidades = vm.VendedorCantidades,
                        montos = vm.VendedorMontos
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
        public async Task<JsonResult> ObtenerClientesInactivos(int meses = 5, int? vendedorId = null)
        {
            try
            {
                var list = await _dashboardService.GetClientesInactivosAsync(meses, vendedorId);
                return Json(new { success = true, meses = meses, data = list });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerDatosDashboard(int mesesInactivos = 5)
        {
            return await ObtenerMetricas(null, mesesInactivos, null);
        }
    }
}
