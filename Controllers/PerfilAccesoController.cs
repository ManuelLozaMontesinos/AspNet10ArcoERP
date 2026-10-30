using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Controllers
{
    [Authorize]
    public class PerfilAccesoController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IRHService _contextService;

        public PerfilAccesoController(IAuthService authService, IRHService contextService)
        {
            _authService = authService;
            _contextService = contextService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? returnUrl = null)
        {
            if (!TryGetUserId(out int userId)) return Unauthorized();

            var model = await _authService.GetPerfilAccesoDataAsync(userId);
            model.ReturnUrl = LocalReturnUrl(returnUrl);
            var json = HttpContext.Session.GetString("USER_SESSION");
            UserSessionDto? session = null;
            try { if (!string.IsNullOrWhiteSpace(json)) session = JsonSerializer.Deserialize<UserSessionDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
            catch (JsonException) { /* Let the user select a fresh context. */ }
            if (session?.UsuarioId == userId)
            {
                model.PeriodoId = session.PeriodoId; model.EmpresaId = session.EmpresaId; model.EmpresaSedeId = session.EmpresaSedeId;
            }
            else
            {
                model.EmpresaId = ClaimId("EmpresaId"); model.EmpresaSedeId = ClaimId("SedeId");
                model.PeriodoId = ClaimId("PeriodoId");
                if (model.PeriodoId <= 0)
                {
                    var matches = model.Periodos.Where(item => item.Item2 == User.FindFirstValue("PeriodoAnio")).ToList();
                    if (matches.Count == 1) model.PeriodoId = matches[0].Item1;
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Grabar(int periodoId, int empresaId, int empresaSedeId, string? returnUrl = null)
        {
            if (!TryGetUserId(out int userId)) return Unauthorized();
            if (!ModelState.IsValid || periodoId <= 0 || empresaId <= 0 || empresaSedeId <= 0 ||
                !((await _contextService.GetPeriodoEmpresaSedePlanillaAsync(empresaId, empresaSedeId, periodoId)).HasValue))
                return await InvalidContextAsync(userId, periodoId, empresaId, empresaSedeId, returnUrl);

            var updatedSession = await _authService.GetUserSessionAsync(userId, periodoId, empresaId, empresaSedeId);
            if (updatedSession is null || updatedSession.UsuarioId != userId || updatedSession.EmpresaId != empresaId ||
                updatedSession.EmpresaSedeId != empresaSedeId || updatedSession.PeriodoId != periodoId)
                return await InvalidContextAsync(userId, periodoId, empresaId, empresaSedeId, returnUrl);

            var ticket = await HttpContext.AuthenticateAsync();
            var principal = User.Clone();
            if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated) return Unauthorized();
            UserSessionClaims.UpdateContext(identity, updatedSession);
            await HttpContext.SignInAsync(principal, ticket.Properties);
            HttpContext.User = principal;
            HttpContext.Session.SetString("USER_SESSION", JsonSerializer.Serialize(updatedSession));

            var destination = LocalReturnUrl(returnUrl);
            return destination is null ? RedirectToAction("Index", "Home") : LocalRedirect(destination);
        }

        private bool TryGetUserId(out int userId) => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId > 0;
        private int ClaimId(string type) => int.TryParse(User.FindFirstValue(type), out int value) && value > 0 ? value : 0;
        private string? LocalReturnUrl(string? value) => !string.IsNullOrEmpty(value) && Url.IsLocalUrl(value) ? value : null;

        private async Task<IActionResult> InvalidContextAsync(int userId, int periodoId, int empresaId, int empresaSedeId, string? returnUrl)
        {
            var model = await _authService.GetPerfilAccesoDataAsync(userId);
            model.PeriodoId = periodoId; model.EmpresaId = empresaId; model.EmpresaSedeId = empresaSedeId;
            model.ReturnUrl = LocalReturnUrl(returnUrl);
            ModelState.AddModelError(string.Empty, "Seleccione un período activo y una sede que pertenezca a la empresa elegida.");
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", model);
        }
    }
}
