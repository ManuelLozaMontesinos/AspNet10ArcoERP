using System.Security.Claims;
using System.Threading.Tasks;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.RH.Controllers
{
    [Area("RH")]
    [Authorize]
    public class TipoRemuneraController : Controller
    {
        private readonly IRHService _rhService;

        public TipoRemuneraController(IRHService rhService)
        {
            _rhService = rhService;
        }

        private int CurrentUserId
        {
            get
            {
                var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(idStr, out int id) ? id : 1;
            }
        }

        public async Task<IActionResult> Index(string? search = null, string? estado = null, int top = 25)
        {
            ViewBag.Search = search;
            ViewBag.Estado = estado;
            ViewBag.Top = top;
            ViewBag.VariablesCalculo = await _rhService.GetVariablesCalculoLookupAsync();

            var list = await _rhService.GetTiposRemuneraListAsync(search, estado, top);
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Obtener(int id)
        {
            var item = await _rhService.GetTipoRemuneraByIdAsync(id);
            if (item == null)
                return Json(new { success = false, message = "Tipo de remuneración no encontrado." });

            return Json(new { success = true, data = item });
        }

        [HttpPost]
        public async Task<IActionResult> Guardar([FromForm] TipoRemuneraModel model)
        {
            var (success, message, id) = await _rhService.SaveTipoRemuneraAsync(model, CurrentUserId);
            return Json(new { success, message, id });
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var (success, message, nuevoEstado) = await _rhService.ToggleEstadoTipoRemuneraAsync(id, CurrentUserId);
            return Json(new { success, message, nuevoEstado });
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var (success, message, _) = await _rhService.ToggleEstadoTipoRemuneraAsync(id, CurrentUserId);
            return Json(new { success, message });
        }
    }
}
