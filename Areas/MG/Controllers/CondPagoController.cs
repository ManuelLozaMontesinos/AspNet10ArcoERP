using System.Security.Claims;
using System.Threading.Tasks;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.MG.Controllers
{
    [Area("MG")]
    [Authorize]
    public class CondPagoController : Controller
    {
        private readonly IMaestrosService _maestrosService;

        public CondPagoController(IMaestrosService maestrosService)
        {
            _maestrosService = maestrosService;
        }

        private int CurrentUserId
        {
            get
            {
                var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(idStr, out int id) ? id : 1;
            }
        }

        private int CurrentEmpresaId => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 1;

        public async Task<IActionResult> Index()
        {
            var list = await _maestrosService.GetCondicionesPagoAsync(CurrentEmpresaId);
            ViewBag.TiposCondicionPago = await _maestrosService.GetTiposCondicionPagoLookupAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Obtener(int id)
        {
            var item = await _maestrosService.GetCondPagoByIdAsync(id, CurrentEmpresaId);
            if (item == null)
                return Json(new { success = false, message = "Condición de pago no encontrada." });

            return Json(new { success = true, data = item });
        }

        [HttpPost]
        public async Task<IActionResult> Guardar([FromForm] CondPagoModel model)
        {
            var (success, message, id) = await _maestrosService.SaveCondPagoAsync(model, CurrentUserId, CurrentEmpresaId);
            return Json(new { success, message, id });
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var (success, message, nuevoEstado) = await _maestrosService.ToggleEstadoCondPagoAsync(id, CurrentUserId, CurrentEmpresaId);
            return Json(new { success, message, nuevoEstado });
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var (success, message, _) = await _maestrosService.ToggleEstadoCondPagoAsync(id, CurrentUserId, CurrentEmpresaId);
            return Json(new { success, message });
        }
    }
}
