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
    public class FamiliaProductoController : Controller
    {
        private readonly IMaestrosService _maestrosService;

        public FamiliaProductoController(IMaestrosService maestrosService)
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

        public async Task<IActionResult> Index()
        {
            var list = await _maestrosService.GetFamiliasProductoAsync();
            ViewBag.ClasesLookup = await _maestrosService.GetClasesProductoLookupAsync();
            ViewBag.Grupos = await _maestrosService.GetGruposProductoAsync();
            ViewBag.Clases = await _maestrosService.GetClasesProductoAsync();
            ViewBag.TiposProducto = await _maestrosService.GetTiposProductoLookupAsync();
            ViewBag.TiposDetraccion = await _maestrosService.GetTiposDetraccionLookupAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> ListarGrupos(int? tipoProductoId = null) =>
            Json(new { success = true, data = await _maestrosService.GetGruposProductoAsync(tipoProductoId) });

        [HttpGet]
        public async Task<IActionResult> ObtenerGrupo(int id)
        {
            var item = await _maestrosService.GetGrupoProductoByIdAsync(id);
            return item == null ? Json(new { success = false, message = "Grupo no encontrado." }) : Json(new { success = true, data = item });
        }

        [HttpPost]
        public async Task<IActionResult> GuardarGrupo([FromForm] GrupoProductoModel model)
        {
            var (success, message, id) = await _maestrosService.SaveGrupoProductoAsync(model, CurrentUserId);
            return Json(new { success, message, id });
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstadoGrupo(int id)
        {
            var (success, message, nuevoEstado) = await _maestrosService.ToggleEstadoGrupoProductoAsync(id, CurrentUserId);
            return Json(new { success, message, nuevoEstado });
        }

        [HttpGet]
        public async Task<IActionResult> ListarClases(int? grupoId = null) =>
            Json(new { success = true, data = await _maestrosService.GetClasesProductoAsync(grupoId) });

        [HttpGet]
        public async Task<IActionResult> ObtenerClase(int id)
        {
            var item = await _maestrosService.GetClaseProductoByIdAsync(id);
            return item == null ? Json(new { success = false, message = "Clase no encontrada." }) : Json(new { success = true, data = item });
        }

        [HttpPost]
        public async Task<IActionResult> GuardarClase([FromForm] ClaseProductoModel model)
        {
            var (success, message, id) = await _maestrosService.SaveClaseProductoAsync(model, CurrentUserId);
            return Json(new { success, message, id });
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstadoClase(int id)
        {
            var (success, message, nuevoEstado) = await _maestrosService.ToggleEstadoClaseProductoAsync(id, CurrentUserId);
            return Json(new { success, message, nuevoEstado });
        }

        [HttpGet]
        public async Task<IActionResult> Obtener(int id)
        {
            var item = await _maestrosService.GetFamiliaProductoByIdAsync(id);
            if (item == null)
                return Json(new { success = false, message = "Familia no encontrada." });

            return Json(new { success = true, data = item });
        }

        [HttpPost]
        public async Task<IActionResult> Guardar([FromForm] GrupoClaseFamiliaModel model)
        {
            var (success, message, id) = await _maestrosService.SaveFamiliaProductoAsync(model, CurrentUserId);
            return Json(new { success, message, id });
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var (success, message, nuevoEstado) = await _maestrosService.ToggleEstadoFamiliaProductoAsync(id, CurrentUserId);
            return Json(new { success, message, nuevoEstado });
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var (success, message, _) = await _maestrosService.ToggleEstadoFamiliaProductoAsync(id, CurrentUserId);
            return Json(new { success, message });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerClases()
        {
            var list = await _maestrosService.GetClasesProductoLookupAsync();
            return Json(new { success = true, data = list });
        }
    }
}
