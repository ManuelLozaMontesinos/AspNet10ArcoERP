using System.Security.Claims;
using System.Text.Json;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AspNet10ArcoERP.Areas.RH.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
public abstract class RHContextController(IRHService service, ILogger logger, string menuUrl) : Controller
{
    protected IRHService RHService { get; } = service;
    protected int CurrentUserId { get; private set; }
    protected int CurrentEmpresaId { get; private set; }
    protected int CurrentPeriodoEmpSedeId { get; private set; }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        try
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId <= 0)
            {
                context.Result = Unauthorized(new { success=false, message="Inicie sesión nuevamente." });
                return;
            }
            CurrentUserId = userId;
            var json = HttpContext.Session.GetString("USER_SESSION");
            var session = string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<UserSessionDto>(json);
            if (session is null || session.UsuarioId != userId || session.EmpresaId <= 0 ||
                session.EmpresaSedeId <= 0 || session.PeriodoId <= 0)
            {
                context.Result = Conflict(new { success=false, message="Seleccione un período y una sede válidos en Perfil de acceso." });
                return;
            }
            CurrentEmpresaId = session.EmpresaId;
            var scope = await RHService.GetPeriodoEmpresaSedePlanillaAsync(session.EmpresaId, session.EmpresaSedeId, session.PeriodoId);
            if (!scope.HasValue)
            {
                context.Result = Conflict(new { success=false, message="El período y la sede seleccionados no están habilitados." });
                return;
            }
            CurrentPeriodoEmpSedeId = scope.Value;
            int permission = await RHService.GetPermisoModuloRHAsync(userId, session.EmpresaId, menuUrl);
            bool canWrite = (permission & 2) != 0;
            if ((permission & 1) == 0 || (HttpMethods.IsPost(Request.Method) && !canWrite))
            {
                context.Result = StatusCode(403, new { success=false, message="No cuenta con permisos para realizar esta operación." });
                return;
            }
            ViewBag.PuedeEscribir = canWrite;
            var executed = await next();
            if (executed.Exception is not null && !executed.ExceptionHandled)
            {
                logger.LogError(executed.Exception, "Error en {Module}/{Action}", menuUrl, context.ActionDescriptor.RouteValues["action"]);
                executed.ExceptionHandled = true;
                executed.Result = StatusCode(500, new { success=false, message="No se pudo completar la operación. Intente nuevamente." });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al resolver el acceso a {Module}", menuUrl);
            context.Result = StatusCode(500, new { success=false, message="No se pudo verificar el acceso al módulo." });
        }
    }
}
