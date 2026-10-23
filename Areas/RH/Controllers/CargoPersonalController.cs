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
    public class CargoPersonalController : Controller
    {
        private readonly IRHService _rhService;

        public CargoPersonalController(IRHService rhService)
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

            var list = await _rhService.GetCargosPersonalListAsync(search, estado, top);
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Obtener(int id)
        {
            var item = await _rhService.GetCargoPersonalByIdAsync(id);
            if (item == null)
                return Json(new { success = false, message = "Cargo no encontrado." });

            return Json(new { success = true, data = item });
        }

        [HttpPost]
        public async Task<IActionResult> Guardar([FromForm] CargoPersonalModel model)
        {
            var (success, message, id) = await _rhService.SaveCargoPersonalAsync(model, CurrentUserId);
            return Json(new { success, message, id });
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var (success, message, nuevoEstado) = await _rhService.ToggleEstadoCargoPersonalAsync(id, CurrentUserId);
            return Json(new { success, message, nuevoEstado });
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var (success, message, _) = await _rhService.ToggleEstadoCargoPersonalAsync(id, CurrentUserId);
            return Json(new { success, message });
        }
    }
}
