using System;
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
                    NombreCompleto = User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value ?? "Administrador",
                    EmpresaNombre = User.FindFirst("EmpresaNombre")?.Value ?? "ARCO",
                    SedeNombre = User.FindFirst("SedeNombre")?.Value ?? "Sede Principal",
                    PeriodoAnio = User.FindFirst("PeriodoAnio")?.Value ?? "2026"
                };
            }

            ViewBag.UserSession = userSession;
            var vm = await _dashboardService.GetDashboardMetricsAsync(5);
            return View(vm);
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerDatosDashboard(int mesesInactivos = 5)
        {
            var vm = await _dashboardService.GetDashboardMetricsAsync(mesesInactivos);
            var response = new
            {
                success = true,
                kpis = new
                {
                    facturacionMes = vm.FacturacionMes,
                    pedidosActivos = vm.PedidosActivos,
                    cotizacionesActivas = vm.CotizacionesActivas,
                    ordenesProduccionMes = vm.OrdenesProduccionMes,
                    ordenesCompraMes = vm.OrdenesCompraMes
                },
                topPedidos = vm.TopPedidos,
                clientesInactivos = vm.ClientesInactivos,
                graficoVentas = new
                {
                    meses = vm.MesesVentas,
                    ventas2025 = vm.Ventas2025,
                    ventas2026 = vm.Ventas2026
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

        [HttpGet]
        public async Task<JsonResult> ObtenerClientesInactivos(int meses = 5)
        {
            var list = await _dashboardService.GetClientesInactivosAsync(meses);
            return Json(new { success = true, meses = meses, data = list });
        }
    }
}
