using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.MK.Controllers
{
    [Area("MK")]
    [Authorize]
    public class ClienteController : Controller
    {
        private readonly IMaestrosService _maestrosService;

        public ClienteController(IMaestrosService maestrosService)
        {
            _maestrosService = maestrosService;
        }

        private int CurrentEmpresaId =>
            int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 1;

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
                search, null, estado, naturaleza, sort, dir, pageSize, CurrentEmpresaId, "N");

            ViewBag.Search = search;
            ViewBag.Tipo = null;
            ViewBag.Estado = estado;
            ViewBag.Naturaleza = naturaleza;
            ViewBag.Sort = string.IsNullOrWhiteSpace(sort) ? "id" : sort.ToLowerInvariant();
            ViewBag.Dir = string.IsNullOrWhiteSpace(dir) ? "desc" : dir.ToLowerInvariant();
            ViewBag.Top = pageSize;
            ViewBag.EsProspecto = "N";
            ViewBag.EsMarketingCliente = true;
            ViewBag.TiposNaturaleza = await _maestrosService.GetTiposNaturalezaJuridicaLookupAsync();
            ViewBag.TiposDoc = await _maestrosService.GetTiposDocumentoIdentidadLookupAsync();
            ViewBag.TiposVia = await _maestrosService.GetTiposViaLookupAsync();
            ViewBag.TiposZona = await _maestrosService.GetTiposZonaLookupAsync();
            ViewBag.TiposProveedor = await _maestrosService.GetTiposProveedorLookupAsync();
            ViewBag.CondicionesPago = await _maestrosService.GetCondicionesPagoLookupAsync(CurrentEmpresaId);
            ViewBag.TiposProfesion = await _maestrosService.GetTiposProfesionLookupAsync();
            ViewBag.TiposVentaCliente = await _maestrosService.GetTiposVentaClienteLookupAsync();
            ViewBag.Departamentos = await _maestrosService.GetDepartamentosLookupAsync();

            return View("~/Areas/MG/Views/Proveedor/Index.cshtml", list);
        }
    }
}
