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
    public class PersonalController : Controller
    {
        private readonly IRHService _rhService;

        public PersonalController(IRHService rhService)
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

        // =========================================================================
        // BANDEJA PRINCIPAL (INDEX)
        // =========================================================================
        public async Task<IActionResult> Index(
            string? search = null, 
            string? estado = null, 
            string? sort = null, 
            string? dir = null, 
            int top = 25)
        {
            var list = await _rhService.GetPersonalListAsync(search, estado, sort, dir, top);
            var (total, activos, cesados) = await _rhService.GetPersonalCountsAsync();

            ViewBag.TotalCount = total;
            ViewBag.ActivosCount = activos;
            ViewBag.CesadosCount = cesados;

            // Lookups para el modal y filtros
            ViewBag.TiposDocLookup = await _rhService.GetTiposDocIdentidadLookupAsync();
            ViewBag.SexosLookup = await _rhService.GetSexosLookupAsync();
            ViewBag.EstadosCivilesLookup = await _rhService.GetEstadosCivilesLookupAsync();
            ViewBag.PaisesLookup = await _rhService.GetPaisesLookupAsync();
            ViewBag.DepartamentosLookup = await _rhService.GetDepartamentosLookupAsync();
            ViewBag.TiposViaLookup = await _rhService.GetTiposViaLookupAsync();
            ViewBag.TiposZonaLookup = await _rhService.GetTiposZonaLookupAsync();
            ViewBag.TiposTrabajadorLookup = await _rhService.GetTiposTrabajadorLookupAsync();
            ViewBag.CategoriasOcupacionalesLookup = await _rhService.GetCategoriasOcupacionalesLookupAsync();
            ViewBag.RegimenesLaboralesLookup = await _rhService.GetRegimenesLaboralesLookupAsync();
            ViewBag.SituacionesEducativasLookup = await _rhService.GetSituacionesEducativasLookupAsync();
            ViewBag.CargosLookup = await _rhService.GetCargosPersonalLookupAsync();
            ViewBag.OcupacionesLookup = await _rhService.GetOcupacionesLookupAsync();
            ViewBag.CentrosCostoLookup = await _rhService.GetCentrosCostoLookupAsync();
            ViewBag.TiposContratoLookup = await _rhService.GetTiposContratoLookupAsync();
            ViewBag.SituacionesEspecialesLookup = await _rhService.GetSituacionesEspecialesLookupAsync();
            ViewBag.PeriodicidadesRemuLookup = await _rhService.GetPeriodicidadesRemuLookupAsync();
            ViewBag.RegimenesSaludLookup = await _rhService.GetRegimenesSaludLookupAsync();
            ViewBag.EpsLookup = await _rhService.GetEpsLookupAsync();
            ViewBag.RegimenesPensionariosLookup = await _rhService.GetRegimenesPensionariosLookupAsync();
            ViewBag.SctrPensionLookup = await _rhService.GetSctrPensionLookupAsync();
            ViewBag.SctrSaludLookup = await _rhService.GetSctrSaludLookupAsync();
            ViewBag.TiposPagoRemuLookup = await _rhService.GetTiposPagoRemuLookupAsync();
            ViewBag.BancosLookup = await _rhService.GetBancosLookupAsync();
            ViewBag.ConveniosDobleTribLookup = await _rhService.GetConveniosDobleTribLookupAsync();
            ViewBag.MotivosFinPeriodoLookup = await _rhService.GetMotivosFinPeriodoLookupAsync();
            ViewBag.VinculosFamiliaresLookup = await _rhService.GetVinculosFamiliaresLookupAsync();
            ViewBag.TiposRemuneracionLookup = await _rhService.GetTiposRemuneracionLookupAsync();
            ViewBag.PlanillasLookup = await _rhService.GetPlanillasLookupAsync();
            ViewBag.MonedasLookup = await _rhService.GetMonedasLookupAsync();

            ViewBag.Search = search;
            ViewBag.Estado = estado;
            ViewBag.Sort = string.IsNullOrWhiteSpace(sort) ? "codigo" : sort.ToLower();
            ViewBag.Dir = string.IsNullOrWhiteSpace(dir) ? "desc" : dir.ToLower();
            ViewBag.Top = top > 0 ? top : 25;

            return View(list);
        }

        // =========================================================================
        // OBTENER DETALLE (PARA EDICIÓN)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Obtener(int id)
        {
            var item = await _rhService.GetPersonalByIdAsync(id);
            if (item == null)
                return Json(new { success = false, message = "Trabajador no encontrado." });

            return Json(new { success = true, data = item });
        }

        // =========================================================================
        // GUARDAR PERSONAL (CREAR / EDITAR)
        // =========================================================================
        [HttpPost]
        public async Task<IActionResult> Guardar([FromForm] PersonalModel model)
        {
            var (success, message, id) = await _rhService.SavePersonalAsync(model, CurrentUserId);
            return Json(new { success, message, id });
        }

        // =========================================================================
        // CAMBIAR ESTADO
        // =========================================================================
        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var (success, message, nuevoEstado) = await _rhService.ToggleEstadoPersonalAsync(id, CurrentUserId);
            return Json(new { success, message, nuevoEstado });
        }

        // =========================================================================
        // REGISTRAR CESE
        // =========================================================================
        [HttpPost]
        public async Task<IActionResult> RegistrarCese(int id, DateTime fechaCese, int? motivoId)
        {
            var (success, message) = await _rhService.RegistrarCesePersonalAsync(id, fechaCese, motivoId, CurrentUserId);
            return Json(new { success, message });
        }

        // =========================================================================
        // CASCADAS DE UBIGEO Y PENSIONES (AJAX)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> ProvinciasPorDepartamento(int? departamentoId)
        {
            var list = await _rhService.GetProvinciasLookupAsync(departamentoId);
            return Json(list);
        }

        [HttpGet]
        public async Task<IActionResult> DistritosPorProvincia(int? provinciaId)
        {
            var list = await _rhService.GetDistritosLookupAsync(provinciaId);
            return Json(list);
        }

        [HttpGet]
        public async Task<IActionResult> EsquemasPorRegimenPensionario(int? regimenPensionarioId)
        {
            var list = await _rhService.GetEsquemasPensionariosLookupAsync(regimenPensionarioId);
            return Json(list);
        }

        // =========================================================================
        // PESTAÑAS: PERIODOS LABORALES
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> PeriodosLaborales(int personalId)
        {
            var list = await _rhService.GetPeriodosLaboralesAsync(personalId);
            return Json(new { success = true, data = list });
        }

        [HttpPost]
        public async Task<IActionResult> GuardarPeriodoLaboral([FromForm] PersonalPeriodoLaboralModel model)
        {
            var (success, message, id) = await _rhService.SavePeriodoLaboralAsync(model, CurrentUserId);
            return Json(new { success, message, id });
        }

        // =========================================================================
        // PESTAÑAS: REMUNERACIONES
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Remuneraciones(int personalId)
        {
            var list = await _rhService.GetPersonalRemuneracionesAsync(personalId);
            return Json(new { success = true, data = list });
        }

        [HttpPost]
        public async Task<IActionResult> GuardarRemuneracion([FromForm] PersonalRemuneracionModel model)
        {
            var (success, message, id) = await _rhService.SavePersonalRemuneracionAsync(model, CurrentUserId);
            return Json(new { success, message, id });
        }

        [HttpPost]
        public async Task<IActionResult> EliminarRemuneracion(int id)
        {
            var success = await _rhService.DeletePersonalRemuneracionAsync(id, CurrentUserId);
            return Json(new { success, message = success ? "Remuneración eliminada." : "No se pudo eliminar." });
        }

        // =========================================================================
        // PESTAÑAS: DERECHOHABIENTES
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Dependientes(int personalId)
        {
            var list = await _rhService.GetPersonalDependientesAsync(personalId);
            return Json(new { success = true, data = list });
        }

        [HttpPost]
        public async Task<IActionResult> GuardarDependiente([FromForm] PersonalDependienteModel model)
        {
            var (success, message, id) = await _rhService.SavePersonalDependienteAsync(model, CurrentUserId);
            return Json(new { success, message, id });
        }

        [HttpPost]
        public async Task<IActionResult> EliminarDependiente(int id)
        {
            var success = await _rhService.DeletePersonalDependienteAsync(id, CurrentUserId);
            return Json(new { success, message = success ? "Derechohabiente eliminado." : "No se pudo eliminar." });
        }

        // =========================================================================
        // PESTAÑAS: PLANILLAS
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Planillas(int personalId)
        {
            var list = await _rhService.GetPersonalPlanillasAsync(personalId);
            return Json(new { success = true, data = list });
        }

        [HttpPost]
        public async Task<IActionResult> GuardarPlanilla([FromForm] PersonalPlanillaModel model)
        {
            var (success, message, id) = await _rhService.SavePersonalPlanillaAsync(model, CurrentUserId);
            return Json(new { success, message, id });
        }

        [HttpPost]
        public async Task<IActionResult> EliminarPlanilla(int id)
        {
            var success = await _rhService.DeletePersonalPlanillaAsync(id, CurrentUserId);
            return Json(new { success, message = success ? "Asignación de planilla eliminada." : "No se pudo eliminar." });
        }
    }
}
