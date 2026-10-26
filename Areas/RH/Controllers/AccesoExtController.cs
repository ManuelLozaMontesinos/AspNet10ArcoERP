using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.RH.Controllers
{
    [Area("RH")]
    [Authorize]
    public class AccesoExtController : Controller
    {
        private readonly IRHService _rhService;

        public AccesoExtController(IRHService rhService)
        {
            _rhService = rhService;
        }

        private int CurrentUserId
        {
            get
            {
                var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(value, out var id) ? id : 1;
            }
        }

        private int CurrentEmpresaId
        {
            get
            {
                var value = User.FindFirst("EmpresaId")?.Value;
                return int.TryParse(value, out var id) ? id : 1;
            }
        }

        public async Task<IActionResult> Index(
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            int? estadoAprobacionId = null,
            int page = 1,
            int top = 25)
        {
            var pageSize = top is 25 or 50 or 100 or 500 ? top : 25;
            var model = await _rhService.GetSolicitudesAccesoAsync(
                CurrentEmpresaId,
                fechaDesde,
                fechaHasta,
                estadoAprobacionId,
                page,
                pageSize);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprobar([FromBody] ResolverSolicitudAccesoModel? model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "No se recibieron los datos de la solicitud." });

            var result = await _rhService.ResolverSolicitudAccesoAsync(
                model.Id,
                model.HoraAutorizada,
                true,
                CurrentEmpresaId,
                CurrentUserId);
            return result.success
                ? Json(new { success = true, message = result.message })
                : BadRequest(new { success = false, message = result.message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desaprobar(int id)
        {
            var result = await _rhService.ResolverSolicitudAccesoAsync(
                id,
                null,
                false,
                CurrentEmpresaId,
                CurrentUserId);
            return result.success
                ? Json(new { success = true, message = result.message })
                : BadRequest(new { success = false, message = result.message });
        }
    }
}
