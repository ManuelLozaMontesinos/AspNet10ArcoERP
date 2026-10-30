using System.Security.Claims;
using System.Text.Json;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AspNet10ArcoERP.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
public abstract class ModuloContextController(IRHService contextService, IAuthService authService, ILogger logger, string areaCode, string menuUrl) : Controller
{
    protected int EmpresaId { get; private set; }
    protected int EmpresaSedeId { get; private set; }
    protected int UsuarioId { get; private set; }
    protected int PeriodoEmpresaSedeId { get; private set; }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        try
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId) || userId <= 0)
            {
                context.Result = Unauthorized(new { success = false, message = "Inicie sesión nuevamente." }); return;
            }
            var json = HttpContext.Session.GetString("USER_SESSION");
            UserSessionDto? session;
            try { session = string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<UserSessionDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
            catch (JsonException) { context.Result = ContextRequired("Vuelva a seleccionar su contexto de trabajo en Perfil de acceso."); return; }
            bool restoring = session is null;
            if (restoring && HttpMethods.IsGet(Request.Method)) session = await RecoverSessionAsync(userId);
            if (session is null || session.UsuarioId != userId || session.EmpresaId <= 0 || session.EmpresaSedeId <= 0 || session.PeriodoId <= 0)
            {
                context.Result = ContextRequired("Seleccione un período y una sede válidos en Perfil de acceso."); return;
            }
            var scope = await contextService.GetPeriodoEmpresaSedePlanillaAsync(session.EmpresaId, session.EmpresaSedeId, session.PeriodoId);
            if (!scope.HasValue)
            {
                context.Result = ContextRequired("El período y la sede seleccionados no están habilitados."); return;
            }
            UsuarioId = userId; EmpresaId = session.EmpresaId; EmpresaSedeId = session.EmpresaSedeId;
            PeriodoEmpresaSedeId = scope.Value;
            int permission = await contextService.GetPermisoModuloAsync(userId, EmpresaId, areaCode.ToLowerInvariant(), menuUrl);
            bool canWrite = (permission & 2) != 0;
            if ((permission & 1) == 0 || (HttpMethods.IsPost(Request.Method) && !canWrite))
            {
                context.Result = StatusCode(403, new { success = false, message = "No cuenta con permisos para esta operación." }); return;
            }
            if (restoring) HttpContext.Session.SetString("USER_SESSION", JsonSerializer.Serialize(session));
            ViewBag.PuedeEscribir = canWrite;
            ViewBag.ContextoId = scope.Value;
            ViewBag.SedeNombre = session.SedeNombre;
            var executed = await next();
            if (executed.Exception is not null && !executed.ExceptionHandled)
            {
                logger.LogError(executed.Exception, "Error en {Area}/{Module}/{Action}", areaCode, menuUrl, context.ActionDescriptor.RouteValues["action"]);
                executed.ExceptionHandled = true;
                executed.Result = StatusCode(500, new { success = false, message = "No se pudo completar la operación. Intente nuevamente." });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al resolver acceso a {Area}/{Module}", areaCode, menuUrl);
            context.Result = StatusCode(500, new { success = false, message = "No se pudo verificar el acceso al módulo." });
        }
    }

    private async Task<UserSessionDto?> RecoverSessionAsync(int userId)
    {
        if (!int.TryParse(User.FindFirstValue("EmpresaId"), out int companyId) || companyId <= 0 ||
            !int.TryParse(User.FindFirstValue("SedeId"), out int siteId) || siteId <= 0) return null;
        int periodId;
        var periodClaim = User.FindFirstValue("PeriodoId");
        if (periodClaim is not null)
        {
            if (!int.TryParse(periodClaim, out periodId) || periodId <= 0) return null;
        }
        else
        {
            // Older authentication cookies contain the selected year, but not its database ID.
            string? year = User.FindFirstValue("PeriodoAnio");
            if (string.IsNullOrWhiteSpace(year)) return null;
            var profile = await authService.GetPerfilAccesoDataAsync(userId);
            var matches = profile.Periodos.Where(item => item.Item2 == year).ToList();
            if (matches.Count != 1) return null;
            periodId = matches[0].Item1;
        }
        var candidate = await authService.GetUserSessionAsync(userId, periodId, companyId, siteId);
        return candidate is not null && candidate.UsuarioId == userId && candidate.EmpresaId == companyId &&
            candidate.EmpresaSedeId == siteId && candidate.PeriodoId == periodId ? candidate : null;
    }

    private IActionResult ContextRequired(string message)
    {
        bool listing = string.Equals(ControllerContext.ActionDescriptor.ActionName, "Index", StringComparison.OrdinalIgnoreCase);
        string returnUrl = listing ? $"{Request.PathBase}{Request.Path}{Request.QueryString}" :
            Url.Action("Index", ControllerContext.ActionDescriptor.ControllerName, new { area = areaCode.ToUpperInvariant() })!;
        bool navigation = HttpMethods.IsGet(Request.Method) && listing &&
            !string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase) &&
            !Request.Headers.Accept.Any(value => value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
        if (navigation) return RedirectToAction("Index", "PerfilAcceso", new { area = "", returnUrl });
        return Conflict(new { success = false, message, redirectUrl = Url.Action("Index", "PerfilAcceso", new { area = "", returnUrl }) });
    }

    protected IActionResult Result((bool success, string message, int id) result)
        => result.success ? Json(new { success = true, result.message, result.id }) : BadRequest(new { success = false, result.message });

    protected IActionResult InvalidInput() => BadRequest(new { success = false, message = "Revise los campos y sus valores antes de continuar." });
}
