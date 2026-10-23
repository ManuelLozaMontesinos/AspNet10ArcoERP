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
    public class PlanillaController : Controller
    {
        private readonly IRHService _rhService;

        public PlanillaController(IRHService rhService)
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

        public async Task<IActionResult> Index(string? search = null, string? estado = null, int top = 25)
        {
            ViewBag.Search = search;
            ViewBag.Estado = estado;
            ViewBag.Top = top;
            ViewBag.TiposTrabajador = await _rhService.GetTiposTrabajadorLookupAsync();

            var list = await _rhService.GetPlanillasAdminListAsync(CurrentEmpresaId, search, estado, top);
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Obtener(int id)
        {
            var item = await _rhService.GetPlanillaAdminByIdAsync(id);
            if (item == null)
                return Json(new { success = false, message = "Planilla no encontrada." });

            return Json(new { success = true, data = item });
        }

        [HttpPost]
        public async Task<IActionResult> Guardar([FromForm] PlanillaModel model)
        {
            model.EmpresaId = CurrentEmpresaId;
            var (success, message, id) = await _rhService.SavePlanillaAsync(model, CurrentUserId);
            return Json(new { success, message, id });
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var (success, message, nuevoEstado) = await _rhService.ToggleEstadoPlanillaAsync(id, CurrentUserId);
            return Json(new { success, message, nuevoEstado });
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var (success, message, _) = await _rhService.ToggleEstadoPlanillaAsync(id, CurrentUserId);
            return Json(new { success, message });
        }
    }
}
