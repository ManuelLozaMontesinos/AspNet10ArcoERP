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
    public class PerfilAccesoController : Controller
    {
        private readonly IAuthService _authService;

        public PerfilAccesoController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int userId = 1;
            var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claimId, out int parsed)) userId = parsed;

            var model = await _authService.GetPerfilAccesoDataAsync(userId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Grabar(int periodoId, int empresaId, int empresaSedeId)
        {
            int userId = 1;
            var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claimId, out int parsed)) userId = parsed;

            var updatedSession = await _authService.GetUserSessionAsync(userId, periodoId, empresaId, empresaSedeId);
            if (updatedSession != null)
            {
                HttpContext.Session.SetString("USER_SESSION", JsonSerializer.Serialize(updatedSession));
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
