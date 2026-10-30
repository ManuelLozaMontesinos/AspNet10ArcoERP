using System;
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
    public class MarcacionController : Controller
    {
        private readonly IRHService _rhService;

        public MarcacionController(IRHService rhService)
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

        private int CurrentEmpresaId
        {
            get
            {
                var empStr = User.FindFirst("EmpresaId")?.Value;
                return int.TryParse(empStr, out int id) ? id : 1;
            }
        }

        public async Task<IActionResult> Index(DateTime? fechaDesde = null, DateTime? fechaHasta = null, int? personalId = null, string? estado = null, string? verificado = null, string? search = null, int top = 25)
        {
            top = top is 25 or 50 or 100 or 500 ? top : 25;
            ViewBag.FechaDesde = fechaDesde?.ToString("yyyy-MM-dd");
            ViewBag.FechaHasta = fechaHasta?.ToString("yyyy-MM-dd");
            ViewBag.PersonalId = personalId;
            ViewBag.Estado = estado;
            ViewBag.Verificado = verificado;
            ViewBag.Search = search;
            ViewBag.Top = top;

            ViewBag.Personal = await _rhService.GetPersonalLookupAsync(CurrentEmpresaId);
            ViewBag.HorasRemunerativas = await _rhService.GetHorasRemunerativasListAsync(CurrentEmpresaId, estado: "A", top: 100);

            var list = await _rhService.GetMarcacionesListAsync(CurrentEmpresaId, fechaDesde, fechaHasta, personalId, estado, verificado, search, top);
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Obtener(int id)
        {
            var item = await _rhService.GetMarcacionByIdAsync(id);
            if (item == null)
                return Json(new { success = false, message = "Marcación no encontrada." });

            return Json(new { success = true, data = item });
        }

        [HttpPost]
        public async Task<IActionResult> Guardar([FromForm] MarcacionCrearEditarModel model)
        {
            if (model.PersonalId <= 0)
                return Json(new { success = false, message = "Debe seleccionar un colaborador de la lista." });

            var result = await _rhService.SaveMarcacionAsync(model, CurrentUserId);
            return Json(new { success = result.success, message = result.message, id = result.id });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleEstado(int id)
        {
            var result = await _rhService.ToggleEstadoMarcacionAsync(id, CurrentUserId);
            return Json(new { success = result.success, message = result.message, nuevoEstado = result.nuevoEstado });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleVerificado(int id)
        {
            var result = await _rhService.ToggleVerificadoMarcacionAsync(id, CurrentUserId);
            return Json(new { success = result.success, message = result.message, nuevoVerificado = result.nuevoVerificado });
        }
    }
}
