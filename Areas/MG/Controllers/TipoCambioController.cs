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
    public class TipoCambioController : Controller
    {
        private readonly IMaestrosService _maestrosService;

        public TipoCambioController(IMaestrosService maestrosService)
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

        public async Task<IActionResult> Index(
            string? search = null,
            int? tipo = null,
            string? estado = null,
            int top = 25)
        {
            int safeTop = top is 25 or 50 or 100 or 500 ? top : 25;
            var list = await _maestrosService.GetTiposCambioAsync(search, tipo, estado, safeTop);
            ViewBag.Search = search;
            ViewBag.Tipo = tipo;
            ViewBag.Estado = estado;
            ViewBag.Top = safeTop;
            ViewBag.TiposCambio = await _maestrosService.GetTiposCambioCatalogoAsync();
            ViewBag.Monedas = await _maestrosService.GetMonedasCatalogoAsync();
            ViewBag.TiposCambioMaestros = await _maestrosService.GetTiposCambioMaestrosAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTipoCambioMaestro(int id)
        {
            var item = await _maestrosService.GetTipoCambioMaestroByIdAsync(id);
            return item == null
                ? Json(new { success = false, message = "Tipo de cambio no encontrado." })
                : Json(new { success = true, data = item });
        }

        [HttpPost]
        public async Task<IActionResult> GuardarTipoCambioMaestro([FromForm] TipoCambioMaestroModel model)
        {
            var (success, message, id) = await _maestrosService.SaveTipoCambioMaestroAsync(model, CurrentUserId);
            return Json(new { success, message, id });
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstadoTipoCambioMaestro(int id)
        {
            var (success, message, nuevoEstado) = await _maestrosService.ToggleEstadoTipoCambioMaestroAsync(id, CurrentUserId);
            return Json(new { success, message, nuevoEstado });
        }

        [HttpGet]
        public async Task<IActionResult> Obtener(int id)
        {
            var item = await _maestrosService.GetTipoCambioByIdAsync(id);
            if (item == null)
                return Json(new { success = false, message = "Tipo de cambio no encontrado." });

            return Json(new { success = true, data = item });
        }

        [HttpPost]
        public async Task<IActionResult> Guardar([FromForm] TipoCambioModel model)
        {
            var (success, message, id) = await _maestrosService.SaveTipoCambioAsync(model, CurrentUserId);
            return Json(new { success, message, id });
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var (success, message, nuevoEstado) = await _maestrosService.ToggleEstadoTipoCambioAsync(id, CurrentUserId);
            return Json(new { success, message, nuevoEstado });
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var (success, message, _) = await _maestrosService.ToggleEstadoTipoCambioAsync(id, CurrentUserId);
            return Json(new { success, message });
        }
    }
}
