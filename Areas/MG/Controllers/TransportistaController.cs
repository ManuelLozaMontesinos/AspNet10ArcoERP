using System.Security.Claims;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.MG.Controllers
{
    [Area("MG")]
    [Authorize]
    public class TransportistaController : Controller
    {
        private readonly IMaestrosService _maestrosService;

        public TransportistaController(IMaestrosService maestrosService)
        {
            _maestrosService = maestrosService;
        }

        private int CurrentUserId => int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 1;
        private int CurrentEmpresaId => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 1;

        public async Task<IActionResult> Index(string? search = null, string? estado = null, int top = 25)
        {
            int safeTop = top is 25 or 50 or 100 or 500 ? top : 25;
            ViewBag.Search = search;
            ViewBag.Estado = estado;
            ViewBag.Top = safeTop;
            ViewBag.Documentos = await _maestrosService.GetDocumentosTransportistaLookupAsync();
            return View(await _maestrosService.GetTransportistasAsync(search, estado, safeTop, CurrentEmpresaId));
        }

        [HttpGet]
        public async Task<IActionResult> Obtener(int id)
        {
            var item = await _maestrosService.GetTransportistaByIdAsync(id, CurrentEmpresaId);
            if (item == null)
                return Json(new { success = false, message = "Transportista no encontrado." });

            item.Unidades = await _maestrosService.GetTransportistaUnidadesAsync(id);
            item.Conductores = await _maestrosService.GetTransportistaConductoresAsync(id);
            return Json(new { success = true, data = item });
        }

        [HttpPost]
        public async Task<IActionResult> Guardar([FromForm] TransportistaModel model)
        {
            var (success, message, id) = await _maestrosService.SaveTransportistaAsync(model, CurrentUserId, CurrentEmpresaId);
            return Json(new { success, message, id });
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var (success, message, nuevoEstado) = await _maestrosService.ToggleEstadoTransportistaAsync(id, CurrentUserId, CurrentEmpresaId);
            return Json(new { success, message, nuevoEstado });
        }

        [HttpGet]
        public async Task<IActionResult> ListarUnidades(int transportistaId)
        {
            if (await _maestrosService.GetTransportistaByIdAsync(transportistaId, CurrentEmpresaId) == null)
                return NotFound(new { success = false, message = "Transportista no encontrado." });
            return Json(new { success = true, data = await _maestrosService.GetTransportistaUnidadesAsync(transportistaId) });
        }

        [HttpPost]
        public async Task<IActionResult> GuardarUnidad([FromForm] TransportistaUnidadModel model)
        {
            if (await _maestrosService.GetTransportistaByIdAsync(model.TransportistaId, CurrentEmpresaId) == null)
                return NotFound(new { success = false, message = "Transportista no encontrado." });
            var (success, message, id) = await _maestrosService.SaveTransportistaUnidadAsync(model, CurrentUserId);
            return Json(new { success, message, id });
        }

        [HttpPost]
        public async Task<IActionResult> EliminarUnidad(int id, int transportistaId)
        {
            if (await _maestrosService.GetTransportistaByIdAsync(transportistaId, CurrentEmpresaId) == null)
                return NotFound(new { success = false, message = "Transportista no encontrado." });
            if (!(await _maestrosService.GetTransportistaUnidadesAsync(transportistaId)).Any(x => x.Id == id))
                return NotFound(new { success = false, message = "Unidad no encontrada." });
            var (success, message) = await _maestrosService.DeleteTransportistaUnidadAsync(id, CurrentUserId);
            return Json(new { success, message });
        }

        [HttpGet]
        public async Task<IActionResult> ListarConductores(int transportistaId)
        {
            if (await _maestrosService.GetTransportistaByIdAsync(transportistaId, CurrentEmpresaId) == null)
                return NotFound(new { success = false, message = "Transportista no encontrado." });
            return Json(new { success = true, data = await _maestrosService.GetTransportistaConductoresAsync(transportistaId) });
        }

        [HttpPost]
        public async Task<IActionResult> GuardarConductor([FromForm] TransportistaConductorModel model)
        {
            if (await _maestrosService.GetTransportistaByIdAsync(model.TransportistaId, CurrentEmpresaId) == null)
                return NotFound(new { success = false, message = "Transportista no encontrado." });
            var (success, message, id) = await _maestrosService.SaveTransportistaConductorAsync(model, CurrentUserId);
            return Json(new { success, message, id });
        }

        [HttpPost]
        public async Task<IActionResult> EliminarConductor(int id, int transportistaId)
        {
            if (await _maestrosService.GetTransportistaByIdAsync(transportistaId, CurrentEmpresaId) == null)
                return NotFound(new { success = false, message = "Transportista no encontrado." });
            if (!(await _maestrosService.GetTransportistaConductoresAsync(transportistaId)).Any(x => x.Id == id))
                return NotFound(new { success = false, message = "Conductor no encontrado." });
            var (success, message) = await _maestrosService.DeleteTransportistaConductorAsync(id, CurrentUserId);
            return Json(new { success, message });
        }
    }
}
