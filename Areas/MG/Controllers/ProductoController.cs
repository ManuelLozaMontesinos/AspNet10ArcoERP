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
    public class ProductoController : Controller
    {
        private readonly IMaestrosService _maestrosService;

        public ProductoController(IMaestrosService maestrosService)
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

        public async Task<IActionResult> Index(string? search = null)
        {
            var list = await _maestrosService.GetProductosAsync(search, 100);
            ViewBag.FamiliasLookup = await _maestrosService.GetFamiliasProductoLookupAsync();
            ViewBag.UnidadesLookup = await _maestrosService.GetUnidadesMedidaLookupAsync();
            ViewBag.ColoresLookup = await _maestrosService.GetColoresLookupAsync();
            ViewBag.Search = search;
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Obtener(int id)
        {
            var item = await _maestrosService.GetProductoByIdAsync(id);
            if (item == null)
                return Json(new { success = false, message = "Producto no encontrado." });

            return Json(new { success = true, data = item });
        }

        [HttpPost]
        public async Task<IActionResult> Guardar([FromForm] ProductoModel model)
        {
            var (success, message, id) = await _maestrosService.SaveProductoAsync(model, CurrentUserId);
            return Json(new { success, message, id });
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var (success, message, nuevoEstado) = await _maestrosService.ToggleEstadoProductoAsync(id, CurrentUserId);
            return Json(new { success, message, nuevoEstado });
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var (success, message, _) = await _maestrosService.ToggleEstadoProductoAsync(id, CurrentUserId);
            return Json(new { success, message });
        }
    }
}
