using System.Security.Claims;
using System.Text.Json;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AspNet10ArcoERP.Areas.RH.Controllers
{
    [Area("RH")]
    [Authorize]
    [AutoValidateAntiforgeryToken]
    public class PlanillaProcesoController : Controller
    {
        private readonly IRHService _rhService;
        private readonly ILogger<PlanillaProcesoController> _logger;
        private int _userId;
        private int _empresaId;
        private int _periodoEmpSedeId;

        public PlanillaProcesoController(IRHService rhService, ILogger<PlanillaProcesoController> logger)
        {
            _rhService = rhService;
            _logger = logger;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            try
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out _userId) || _userId <= 0)
                {
                    context.Result = Unauthorized(new { success = false, message = "Inicie sesión nuevamente." });
                    return;
                }
                // PerfilAcceso updates USER_SESSION, while login claims retain the previous selection.
                var json = HttpContext.Session.GetString("USER_SESSION");
                var session = string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<UserSessionDto>(json);
                if (session is null || session.UsuarioId != _userId || session.EmpresaId <= 0 ||
                    session.EmpresaSedeId <= 0 || session.PeriodoId <= 0)
                {
                    context.Result = Conflict(new { success = false, message = "Seleccione un período y una sede válidos en Perfil de acceso." });
                    return;
                }
                _empresaId = session.EmpresaId;
                var scope = await _rhService.GetPeriodoEmpresaSedePlanillaAsync(
                    _empresaId, session.EmpresaSedeId, session.PeriodoId);
                if (!scope.HasValue)
                {
                    context.Result = Conflict(new { success = false, message = "El período y la sede seleccionados no están habilitados." });
                    return;
                }
                _periodoEmpSedeId = scope.Value;
                int permission = await _rhService.GetPermisoPlanillaProcesoAsync(_userId, _empresaId);
                bool canWrite = (permission & 2) != 0;
                if ((permission & 1) == 0 || (HttpMethods.IsPost(Request.Method) && !canWrite))
                {
                    context.Result = StatusCode(StatusCodes.Status403Forbidden,
                        new { success = false, message = "No cuenta con permisos para realizar esta operación." });
                    return;
                }
                ViewBag.PuedeEscribir = canWrite;
                var executed = await next();
                if (executed.Exception is not null && !executed.ExceptionHandled)
                {
                    _logger.LogError(executed.Exception, "Error en PlanillaProceso/{Action}", context.ActionDescriptor.RouteValues["action"]);
                    executed.ExceptionHandled = true;
                    executed.Result = StatusCode(StatusCodes.Status500InternalServerError,
                        new { success = false, message = "No se pudo completar la operación. Intente nuevamente." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al resolver el acceso a PlanillaProceso");
                context.Result = StatusCode(StatusCodes.Status500InternalServerError,
                    new { success = false, message = "No se pudo verificar el acceso al proceso de planilla." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? anio = null, int? mes = null, string? search = null, int page = 1, int top = 25)
        {
            if (!ModelState.IsValid || search?.Length > 150)
                return BadRequest(new { success = false, message = "Los filtros de búsqueda no son válidos." });
            ViewBag.Anio = anio;
            ViewBag.Mes = mes;
            ViewBag.Search = search;
            return View(await _rhService.GetPlanillasProcesoPageAsync(
                _empresaId, _periodoEmpSedeId, anio, mes, search, page, top));
        }

        [HttpGet]
        public async Task<IActionResult> Obtener(int id)
        {
            if (id <= 0)
                return BadRequest(new { success = false, message = "El identificador del proceso no es válido." });
            var item = await _rhService.GetPlanillaProcesoByIdAsync(id, _empresaId, _periodoEmpSedeId);
            return item is null
                ? NotFound(new { success = false, message = "Proceso no encontrado para el período y la sede activos." })
                : Json(new { success = true, data = item });
        }

        [HttpGet]
        public async Task<IActionResult> DetalleCalculo(int id, int? personalId = null)
        {
            if (id <= 0 || personalId <= 0)
                return BadRequest(new { success = false, message = "El proceso o el trabajador indicado no es válido." });
            var items = await _rhService.GetPlanillaCalculoDetalleAsync(id, _empresaId, _periodoEmpSedeId, personalId);
            return items is null
                ? NotFound(new { success = false, message = "Proceso no encontrado para el período y la sede activos." })
                : Json(new { success = true, data = items });
        }

        [HttpPost]
        public async Task<IActionResult> Guardar([FromForm] PlanillaProcesoCrearModel? model)
        {
            if (model is null || !ModelState.IsValid)
                return BadRequest(new { success = false, message = "Revise los campos obligatorios, las fechas y el tipo de cambio." });
            var result = await _rhService.SavePlanillaProcesoAsync(model, _userId, _empresaId, _periodoEmpSedeId);
            return result.success
                ? Json(new { success = true, message = result.message, id = result.id })
                : BadRequest(new { success = false, message = result.message });
        }

        [HttpPost]
        public async Task<IActionResult> Anular(int id)
        {
            var result = await _rhService.InactivarPlanillaProcesoAsync(id, _userId, _empresaId, _periodoEmpSedeId);
            return result.success
                ? Json(new { success = true, message = result.message })
                : BadRequest(new { success = false, message = result.message });
        }

        [HttpPost]
        public async Task<IActionResult> Procesar(int id)
        {
            if (id <= 0 || await _rhService.GetPlanillaProcesoByIdAsync(id, _empresaId, _periodoEmpSedeId) is null)
                return NotFound(new { success = false, message = "Proceso no encontrado para el período y la sede activos." });
            var result = await _rhService.ProcesarCalculoPlanillaAsync(id, _userId);
            return StatusCode(StatusCodes.Status501NotImplemented, new { success = false, message = result.message });
        }
    }
}
