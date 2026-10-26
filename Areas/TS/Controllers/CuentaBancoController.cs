using System.Security.Claims;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.TS.Controllers
{
    [Area("TS")]
    [Authorize]
    public class CuentaBancoController : Controller
    {
        private readonly IMaestrosService _maestrosService;

        public CuentaBancoController(IMaestrosService maestrosService)
        {
            _maestrosService = maestrosService;
        }

        private int CurrentUserId =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 1;

        private int CurrentEmpresaId =>
            int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 1;

        public async Task<IActionResult> Index(string? search = null, int? bancoId = null, string? estado = null, int top = 25)
        {
            int pageSize = top is 25 or 50 or 100 or 500 ? top : 25;
            ViewBag.Search = search;
            ViewBag.BancoId = bancoId;
            ViewBag.Estado = estado;
            ViewBag.Top = pageSize;
            ViewBag.Bancos = await _maestrosService.GetBancosCuentaLookupAsync();
            ViewBag.Monedas = await _maestrosService.GetMonedasCuentaLookupAsync();
            ViewBag.Total = await _maestrosService.GetCuentasBancoCountAsync(CurrentEmpresaId, search, bancoId, estado);

            var cuentas = await _maestrosService.GetCuentasBancoAsync(
                CurrentEmpresaId,
                search,
                bancoId,
                estado,
                pageSize);
            return View(cuentas);
        }

        [HttpGet]
        public async Task<IActionResult> Obtener(int id)
        {
            var cuenta = await _maestrosService.GetCuentaBancoByIdAsync(id, CurrentEmpresaId);
            if (cuenta == null)
                return NotFound(new { success = false, message = "Cuenta bancaria no encontrada." });

            return Json(new { success = true, data = cuenta });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar([FromForm] CuentaBancoModel model)
        {
            var result = await _maestrosService.SaveCuentaBancoAsync(model, CurrentUserId, CurrentEmpresaId);
            return Json(new { success = result.success, message = result.message, id = result.id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var result = await _maestrosService.ToggleEstadoCuentaBancoAsync(id, CurrentUserId, CurrentEmpresaId);
            return Json(new { success = result.success, message = result.message, nuevoEstado = result.nuevoEstado });
        }
    }
}
