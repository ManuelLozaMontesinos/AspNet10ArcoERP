using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AspNet10ArcoERP.Areas.MK.Controllers
{
    [Area("MK")]
    [Authorize]
    public class ProspectoController : Controller
    {
        private readonly IMaestrosService _maestrosService;
        private readonly IMKService _mkService;

        public ProspectoController(IMaestrosService maestrosService, IMKService mkService)
        {
            _maestrosService = maestrosService;
            _mkService = mkService;
        }

        private int CurrentEmpresaId =>
            int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 1;
        private int CurrentUserId =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 1;

        public async Task<IActionResult> Index(
            string? search = null,
            string? estado = null,
            int? naturaleza = null,
            string? sort = null,
            string? dir = null,
            int top = 25)
        {
            var pageSize = top is 25 or 50 or 100 or 500 ? top : 25;
            var list = await _maestrosService.GetProveedoresAsync(
                search, null, estado, naturaleza, sort, dir, pageSize, CurrentEmpresaId, "S");

            ViewBag.Search = search;
            ViewBag.Tipo = null;
            ViewBag.Estado = estado;
            ViewBag.Naturaleza = naturaleza;
            ViewBag.Sort = string.IsNullOrWhiteSpace(sort) ? "id" : sort.ToLowerInvariant();
            ViewBag.Dir = string.IsNullOrWhiteSpace(dir) ? "desc" : dir.ToLowerInvariant();
            ViewBag.Top = pageSize;
            ViewBag.EsProspecto = "S";
            ViewBag.TiposNaturaleza = await _maestrosService.GetTiposNaturalezaJuridicaLookupAsync();
            ViewBag.TiposDoc = await _maestrosService.GetTiposDocumentoIdentidadLookupAsync();
            ViewBag.TiposVia = await _maestrosService.GetTiposViaLookupAsync();
            ViewBag.TiposZona = await _maestrosService.GetTiposZonaLookupAsync();
            ViewBag.TiposProveedor = await _maestrosService.GetTiposProveedorLookupAsync();
            ViewBag.CondicionesPago = await _maestrosService.GetCondicionesPagoLookupAsync(CurrentEmpresaId);
            ViewBag.TiposProfesion = await _maestrosService.GetTiposProfesionLookupAsync();
            ViewBag.TiposVentaCliente = await _maestrosService.GetTiposVentaClienteLookupAsync();
            ViewBag.Departamentos = await _maestrosService.GetDepartamentosLookupAsync();
            ViewBag.LeadSources = await _mkService.GetLeadSourceLookupsAsync();
            ViewBag.OportunidadesVenta = await _mkService.GetOportunidadVentaLookupsAsync();

            return View("~/Areas/MG/Views/Proveedor/Index.cshtml", list);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerOportunidades(int prospectoId)
        {
            if (prospectoId <= 0)
                return BadRequest(new { success = false, message = "El identificador del prospecto no es válido." });

            var ids = await _mkService.GetProspectOpportunityIdsAsync(prospectoId, CurrentEmpresaId);
            return ids == null
                ? NotFound(new { success = false, message = "Prospecto no encontrado." })
                : Json(new { success = true, data = ids });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarOportunidades(int prospectoId, [FromForm] int[]? oportunidadIds)
        {
            if (prospectoId <= 0)
                return BadRequest(new { success = false, message = "El identificador del prospecto no es válido." });

            var result = await _mkService.SaveProspectOpportunityIdsAsync(
                prospectoId, CurrentEmpresaId, oportunidadIds ?? Array.Empty<int>(), CurrentUserId);
            return result.success
                ? Json(new { success = true, message = result.message })
                : BadRequest(new { success = false, message = result.message });
        }
    }
}
