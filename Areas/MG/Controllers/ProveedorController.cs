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
    public class ProveedorController : Controller
    {
        private readonly IMaestrosService _maestrosService;

        public ProveedorController(IMaestrosService maestrosService)
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
            int? tipo = null, 
            string? estado = null, 
            int? naturaleza = null,
            string? sort = null, 
            string? dir = null,
            int top = 25)
        {
            var list = await _maestrosService.GetProveedoresAsync(search, tipo, estado, naturaleza, sort, dir, top);

            ViewBag.Search = search;
            ViewBag.Tipo = tipo;
            ViewBag.Estado = estado;
            ViewBag.Naturaleza = naturaleza;
            ViewBag.Sort = string.IsNullOrWhiteSpace(sort) ? "id" : sort.ToLower();
            ViewBag.Dir = string.IsNullOrWhiteSpace(dir) ? "desc" : dir.ToLower();
            ViewBag.Top = top > 0 ? top : 25;
            ViewBag.TiposNaturaleza = await _maestrosService.GetTiposNaturalezaJuridicaLookupAsync();
            ViewBag.TiposDoc = await _maestrosService.GetTiposDocumentoIdentidadLookupAsync();
            ViewBag.TiposVia = await _maestrosService.GetTiposViaLookupAsync();
            ViewBag.TiposZona = await _maestrosService.GetTiposZonaLookupAsync();
            ViewBag.TiposProveedor = await _maestrosService.GetTiposProveedorLookupAsync();
            ViewBag.CondicionesPago = await _maestrosService.GetCondicionesPagoLookupAsync();
            ViewBag.Departamentos = await _maestrosService.GetDepartamentosLookupAsync();

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Obtener(int id)
        {
            var item = await _maestrosService.GetProveedorByIdAsync(id);
            if (item == null)
                return Json(new { success = false, message = "Registro no encontrado." });

            return Json(new { success = true, data = item });
        }

        [HttpGet]
        public async Task<IActionResult> ListarProvincias(int departamentoId)
        {
            var list = await _maestrosService.GetProvinciasLookupAsync(departamentoId);
            return Json(new { success = true, data = list });
        }

        [HttpGet]
        public async Task<IActionResult> ListarDistritos(int provinciaId)
        {
            var list = await _maestrosService.GetDistritosLookupAsync(provinciaId);
            return Json(new { success = true, data = list });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerDocumentosPorNaturaleza(int naturalezaId)
        {
            var list = await _maestrosService.GetTiposDocumentoIdentidadLookupAsync(naturalezaId);
            return Json(new { success = true, data = list });
        }

        [HttpGet]
        public async Task<IActionResult> ConsultarDocumento(string doc)
        {
            if (string.IsNullOrWhiteSpace(doc))
                return Json(new { success = false, message = "Debe ingresar un número de documento." });

            doc = doc.Trim();

            // 1. Buscar en BD local
            var local = await _maestrosService.BuscarProveedorPorDocumentoAsync(doc);
            if (local != null)
            {
                return Json(new { 
                    success = true, 
                    source = "BD", 
                    message = $"Se encontró un registro en la base de datos: {local.NombreCompleto}",
                    data = local 
                });
            }

            // 2. Detección automática por formato
            bool esRuc = doc.Length == 11 && long.TryParse(doc, out _);
            bool esDni = doc.Length == 8 && int.TryParse(doc, out _);

            if (esRuc)
            {
                bool esRucNatural = doc.StartsWith("10");
                return Json(new {
                    success = true,
                    source = "FORMATO",
                    tipoNaturalezaJuridicaId = esRucNatural ? 1 : 2,
                    tipoDocumentoIdentidadId = 3, // RUC
                    numeroRuc = doc,
                    numeroDocumentoIdentidad = esRucNatural ? doc.Substring(2, 8) : doc,
                    message = esRucNatural ? "RUC de Persona Natural (10...)" : "RUC de Persona Jurídica (20...)"
                });
            }
            else if (esDni)
            {
                return Json(new {
                    success = true,
                    source = "FORMATO",
                    tipoNaturalezaJuridicaId = 1, // Natural
                    tipoDocumentoIdentidadId = 1, // DNI
                    numeroDocumentoIdentidad = doc,
                    numeroRuc = "",
                    message = "DNI de Persona Natural"
                });
            }

            return Json(new { success = false, message = "Documento no registrado en base de datos. Puede continuar con el llenado manual." });
        }

        [HttpPost]
        public async Task<IActionResult> Guardar([FromForm] ProveedorModel model)
        {
            var (success, message, id, codigo) = await _maestrosService.SaveProveedorAsync(model, CurrentUserId);
            return Json(new { success, message, id, codigo });
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var (success, message, nuevoEstado) = await _maestrosService.ToggleEstadoProveedorAsync(id, CurrentUserId);
            return Json(new { success, message, nuevoEstado });
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var (success, message, _) = await _maestrosService.ToggleEstadoProveedorAsync(id, CurrentUserId);
            return Json(new { success, message });
        }
    }
}
