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
    public class ComboProductoController : Controller
    {
        private readonly IMaestrosService _maestrosService;

        public ComboProductoController(IMaestrosService maestrosService)
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
            int? grupoId = null,
            int? claseId = null,
            int? familiaId = null,
            string? estado = null,
            string? sort = null,
            string? dir = null,
            int top = 25)
        {
            var list = await _maestrosService.GetCombosBandejaAsync(search, grupoId, claseId, familiaId, estado, sort, dir, top);

            ViewBag.GruposCombosLookup = await _maestrosService.GetGruposCombosLookupAsync();
            ViewBag.ClasesLookup = await _maestrosService.GetClasesProductoLookupAsync(grupoId);
            ViewBag.FamiliasLookup = await _maestrosService.GetFamiliasProductoLookupAsync(grupoId, claseId);
            ViewBag.UnidadesLookup = await _maestrosService.GetUnidadesMedidaLookupAsync();
            ViewBag.EstadosProductoLookup = await _maestrosService.GetEstadosProductoLookupAsync();
            ViewBag.EstadosComercialesLookup = await _maestrosService.GetEstadosComercialesProductoLookupAsync();

            ViewBag.Search = search;
            ViewBag.GrupoId = grupoId;
            ViewBag.ClaseId = claseId;
            ViewBag.FamiliaId = familiaId;
            ViewBag.Estado = estado;
            ViewBag.Sort = string.IsNullOrWhiteSpace(sort) ? "id" : sort.ToLower();
            ViewBag.Dir = string.IsNullOrWhiteSpace(dir) ? "desc" : dir.ToLower();
            ViewBag.Top = top > 0 ? top : 25;

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Obtener(int id)
        {
            var item = await _maestrosService.GetComboCabeceraByIdAsync(id);
            if (item == null)
                return Json(new { success = false, message = "Combo no encontrado." });

            var detalles = await _maestrosService.GetComboDetallesAsync(id);
            return Json(new { success = true, data = item, detalles });
        }

        [HttpPost]
        public async Task<IActionResult> GuardarCabecera([FromForm] ProductoComboModel model)
        {
            var (success, message, id, codigo) = await _maestrosService.SaveComboCabeceraAsync(model, CurrentUserId);
            return Json(new { success, message, id, codigo });
        }

        // Compatibilidad hacia atrás
        [HttpPost]
        public Task<IActionResult> Guardar([FromForm] ProductoComboModel model) => GuardarCabecera(model);

        [HttpGet]
        public async Task<IActionResult> ListarDetalles(int productoId)
        {
            var detalles = await _maestrosService.GetComboDetallesAsync(productoId);
            return Json(new { success = true, data = detalles });
        }

        [HttpPost]
        public async Task<IActionResult> GuardarDetalle([FromForm] ComboDetalleItemModel model)
        {
            var (success, message, id) = await _maestrosService.SaveComboDetalleAsync(model, CurrentUserId);
            return Json(new { success, message, id });
        }

        [HttpPost]
        public async Task<IActionResult> EliminarDetalle(int id)
        {
            var (success, message) = await _maestrosService.DeleteComboDetalleAsync(id, CurrentUserId);
            return Json(new { success, message });
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var (success, message, nuevoEstado) = await _maestrosService.ToggleEstadoComboAsync(id, CurrentUserId);
            return Json(new { success, message, nuevoEstado });
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var (success, message, _) = await _maestrosService.ToggleEstadoComboAsync(id, CurrentUserId);
            return Json(new { success, message });
        }

        [HttpGet]
        public async Task<IActionResult> BuscarProductos(string q)
        {
            var list = await _maestrosService.BuscarProductosParaComboAsync(q);
            return Json(new { success = true, data = list });
        }

        [HttpGet]
        public async Task<IActionResult> ClasesPorGrupo(int grupoId)
        {
            var clases = await _maestrosService.GetClasesProductoLookupAsync(grupoId);
            return Json(new { success = true, data = clases });
        }

        [HttpGet]
        public async Task<IActionResult> FamiliasPorClase(int? grupoId, int? claseId)
        {
            var familias = await _maestrosService.GetFamiliasProductoLookupAsync(grupoId, claseId);
            return Json(new { success = true, data = familias });
        }
    }
}
