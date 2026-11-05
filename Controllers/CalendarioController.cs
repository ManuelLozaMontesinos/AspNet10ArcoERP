using System;
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
    public class CalendarioController : Controller
    {
        private readonly ICalendarioService _calendarioService;
        private readonly INotifyService _notifyService;

        public CalendarioController(ICalendarioService calendarioService, INotifyService notifyService)
        {
            _calendarioService = calendarioService;
            _notifyService = notifyService;
        }

        private int ObtenerUsuarioId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out int id) && id > 0)
                return id;

            var userSessionJson = HttpContext.Session.GetString("USER_SESSION");
            if (!string.IsNullOrEmpty(userSessionJson))
            {
                try
                {
                    var session = JsonSerializer.Deserialize<UserSessionDto>(userSessionJson);
                    if (session != null && session.UsuarioId > 0) return session.UsuarioId;
                }
                catch { }
            }
            return 1;
        }

        private string ObtenerUsuarioLogon()
        {
            return User.Identity?.Name ?? "administrador";
        }

        private bool EsAdministrador()
        {
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            return roles.Contains("S0001") ||
                   roles.Any(r => r.IndexOf("Admin", StringComparison.OrdinalIgnoreCase) >= 0) ||
                   string.Equals(User.Identity?.Name, "administrador", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(User.Identity?.Name, "admin", StringComparison.OrdinalIgnoreCase);
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? cid = null)
        {
            int usuarioId = ObtenerUsuarioId();
            string usuarioLogon = ObtenerUsuarioLogon();
            bool esAdmin = EsAdministrador();

            var areas = await _calendarioService.GetAreasUsuarioAsync(usuarioId, esAdmin);

            ViewBag.cid = cid;
            ViewBag.Usuario = usuarioLogon;
            ViewBag.UsuarioId = usuarioId;
            ViewBag.EsAdmin = esAdmin;
            ViewBag.Areas = areas;
            ViewBag.TieneMultiplesAreas = esAdmin || (areas != null && areas.Count > 1);
            ViewBag.AreaSeleccionadaId = (areas != null && areas.Count > 0) ? areas[0].AreaId : 1;
            ViewBag.AreaSeleccionadaNombre = (areas != null && areas.Count > 0) ? areas[0].Nombre : "General";

            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                       || !string.IsNullOrEmpty(Request.Headers["x-app-cid"]);

            if (isAjax)
            {
                return PartialView();
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerAreas()
        {
            int usuarioId = ObtenerUsuarioId();
            bool esAdmin = EsAdministrador();
            var areas = await _calendarioService.GetAreasUsuarioAsync(usuarioId, esAdmin);
            return Json(new { success = true, esAdmin, data = areas });
        }

        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> ObtenerEventos(int? areaId)
        {
            int usuarioId = ObtenerUsuarioId();
            bool esAdmin = EsAdministrador();
            var eventos = await _calendarioService.GetEventosAreaAsync(areaId ?? 0, usuarioId, esAdmin);
            return Json(eventos);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarEvento([FromBody] EventoInputModel model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "Datos de evento inválidos." });
            }

            int usuarioId = ObtenerUsuarioId();
            string usuarioLogon = ObtenerUsuarioLogon();
            bool esAdmin = EsAdministrador();

            var (success, eventoId, message) = await _calendarioService.GuardarEventoAsync(model, usuarioId, usuarioLogon, esAdmin);
            return Json(new { success, eventoId, message });
        }

        [HttpPost]
        public async Task<IActionResult> MoverEvento([FromBody] MoverEventoInputModel model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "Datos del evento inválidos." });
            }

            int usuarioId = ObtenerUsuarioId();
            bool esAdmin = EsAdministrador();

            var (success, message) = await _calendarioService.MoverOActualizarEventoAsync(model, usuarioId, esAdmin);
            return Json(new { success, message });
        }

        [HttpPost]
        public async Task<IActionResult> EliminarEvento(int id)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "Identificador de evento no válido." });
            }

            int usuarioId = ObtenerUsuarioId();
            bool esAdmin = EsAdministrador();

            var (success, message) = await _calendarioService.EliminarEventoAsync(id, usuarioId, esAdmin);
            return Json(new { success, message });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerEtiquetas(int? areaId)
        {
            int usuarioId = ObtenerUsuarioId();
            bool esAdmin = EsAdministrador();
            var etiquetas = await _calendarioService.GetEtiquetasAreaAsync(areaId ?? 0, usuarioId, esAdmin);
            return Json(new { success = true, data = etiquetas });
        }

        [HttpPost]
        public async Task<IActionResult> GuardarEtiqueta([FromBody] EtiquetaInputModel model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "Datos de etiqueta inválidos." });
            }

            int usuarioId = ObtenerUsuarioId();
            bool esAdmin = EsAdministrador();

            var (success, message) = await _calendarioService.GuardarEtiquetaAreaAsync(model, usuarioId, esAdmin);
            return Json(new { success, message });
        }

        #region Endpoints de Administración

        [HttpGet]
        public async Task<IActionResult> AdminObtenerAreas()
        {
            if (!EsAdministrador())
            {
                return Json(new { success = false, message = "Acceso denegado. Se requieren permisos de administrador." });
            }

            var areas = await _calendarioService.AdminGetAreasAsync();
            return Json(new { success = true, data = areas });
        }

        [HttpPost]
        public async Task<IActionResult> AdminGuardarArea([FromBody] AreaAdminInputModel model)
        {
            if (!EsAdministrador())
            {
                return Json(new { success = false, message = "Acceso denegado. Se requieren permisos de administrador." });
            }

            var (success, message, areaId) = await _calendarioService.AdminGuardarAreaAsync(model);
            return Json(new { success, message, areaId });
        }

        [HttpPost]
        public async Task<IActionResult> AdminCambiarEstadoArea(int areaId, string estado)
        {
            if (!EsAdministrador())
            {
                return Json(new { success = false, message = "Acceso denegado. Se requieren permisos de administrador." });
            }

            var (success, message) = await _calendarioService.AdminCambiarEstadoAreaAsync(areaId, estado);
            return Json(new { success, message });
        }

        [HttpPost]
        public async Task<IActionResult> AdminEliminarArea(int areaId)
        {
            if (!EsAdministrador())
            {
                return Json(new { success = false, message = "Acceso denegado. Se requieren permisos de administrador." });
            }

            var (success, message) = await _calendarioService.AdminEliminarAreaAsync(areaId);
            return Json(new { success, message });
        }

        [HttpGet]
        public async Task<IActionResult> AdminObtenerUsuariosPorArea(int areaId)
        {
            if (!EsAdministrador())
            {
                return Json(new { success = false, message = "Acceso denegado. Se requieren permisos de administrador." });
            }

            var (areaNombre, areaColor, asignados, disponibles) = await _calendarioService.AdminGetUsuariosAreaAsync(areaId);
            return Json(new
            {
                success = true,
                areaId,
                areaNombre,
                areaColor,
                asignados,
                disponibles
            });
        }

        [HttpPost]
        public async Task<IActionResult> AdminAsignarUsuarioArea(int usuarioId, int areaId, bool esPrincipal = false)
        {
            if (!EsAdministrador())
            {
                return Json(new { success = false, message = "Acceso denegado. Se requieren permisos de administrador." });
            }

            var (success, message) = await _calendarioService.AdminAsignarUsuarioAreaAsync(usuarioId, areaId, esPrincipal);
            return Json(new { success, message });
        }

        [HttpPost]
        public async Task<IActionResult> AdminRemoverUsuarioArea(int usuarioId, int areaId)
        {
            if (!EsAdministrador())
            {
                return Json(new { success = false, message = "Acceso denegado. Se requieren permisos de administrador." });
            }

            var (success, message) = await _calendarioService.AdminRemoverUsuarioAreaAsync(usuarioId, areaId);
            return Json(new { success, message });
        }

        [HttpPost]
        public async Task<IActionResult> AdminToggleUsuarioPrincipal(int usuarioId, int areaId)
        {
            if (!EsAdministrador())
            {
                return Json(new { success = false, message = "Acceso denegado. Se requieren permisos de administrador." });
            }

            var (success, message) = await _calendarioService.AdminToggleUsuarioPrincipalAsync(usuarioId, areaId);
            return Json(new { success, message });
        }

        #endregion
    }
}

