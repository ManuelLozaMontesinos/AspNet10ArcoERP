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
    public class ProductoController : Controller
    {
        private readonly IMaestrosService _maestrosService;

        public ProductoController(IMaestrosService maestrosService)
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
            string? tipo = null,
            int? grupoId = null,
            int? claseId = null,
            int? familiaId = null, 
            string? estado = null, 
            string? sort = null, 
            string? dir = null, 
            int top = 25)
        {
            var list = await _maestrosService.GetProductosAsync(search, tipo, grupoId, claseId, familiaId, estado, sort, dir, top);
            var (total, productos, servicios) = await _maestrosService.GetProductosCountsAsync();

            ViewBag.TotalCount = total;
            ViewBag.ProductosCount = productos;
            ViewBag.ServiciosCount = servicios;

            ViewBag.TiposProductoLookup = await _maestrosService.GetTiposProductoLookupAsync();
            ViewBag.GruposProductoLookup = await _maestrosService.GetGruposProductoLookupAsync();
            ViewBag.ClasesLookup = await _maestrosService.GetClasesProductoLookupAsync(grupoId);
            ViewBag.FamiliasLookup = await _maestrosService.GetFamiliasProductoLookupAsync(grupoId, claseId);
            ViewBag.UnidadesLookup = await _maestrosService.GetUnidadesMedidaLookupAsync();
            ViewBag.ColoresLookup = await _maestrosService.GetColoresLookupAsync();
            ViewBag.TiposIgvLookup = await _maestrosService.GetTiposIgvLookupAsync();
            ViewBag.EstadosProductoLookup = await _maestrosService.GetEstadosProductoLookupAsync();
            ViewBag.EstadosComercialesLookup = await _maestrosService.GetEstadosComercialesProductoLookupAsync();

            ViewBag.Search = search;
            ViewBag.Tipo = string.IsNullOrWhiteSpace(tipo) ? null : tipo.ToUpper();
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
            var item = await _maestrosService.GetProductoByIdAsync(id);
            if (item == null)
                return Json(new { success = false, message = "Producto o servicio no encontrado." });

            return Json(new { success = true, data = item });
        }

        [HttpPost]
        public async Task<IActionResult> Guardar([FromForm] ProductoModel model)
        {
            var (success, message, id) = await _maestrosService.SaveProductoAsync(model, CurrentUserId);
            return Json(new { success, message, id });
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var (success, message, nuevoEstado) = await _maestrosService.ToggleEstadoProductoAsync(id, CurrentUserId);
            return Json(new { success, message, nuevoEstado });
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var (success, message, _) = await _maestrosService.ToggleEstadoProductoAsync(id, CurrentUserId);
            return Json(new { success, message });
        }

        [HttpGet]
        public async Task<IActionResult> ClasesPorGrupo(int? grupoId)
        {
            var clases = await _maestrosService.GetClasesProductoLookupAsync(grupoId);
            return Json(clases);
        }

        [HttpGet]
        public async Task<IActionResult> FamiliasPorGrupo(int? grupoId, int? claseId)
        {
            var familias = await _maestrosService.GetFamiliasProductoLookupAsync(grupoId, claseId);
            return Json(familias);
        }

        [HttpGet]
        public async Task<IActionResult> GruposPorTipo(int? tipoProductoId)
        {
            var grupos = await _maestrosService.GetGruposProductoLookupAsync(tipoProductoId);
            return Json(grupos);
        }

        // =========================================================================
        // PESTAÑA 1: PRESENTACIÓN PARA LA VENTA
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> ListarPresentaciones(int productoId)
        {
            var list = await _maestrosService.GetPresentacionesVentaAsync(productoId);
            return Json(new { success = true, data = list });
        }

        [HttpPost]
        public async Task<IActionResult> GuardarPresentacion([FromForm] ProductoPresentacionVentaModel model)
        {
            var (success, message, id) = await _maestrosService.SavePresentacionVentaAsync(model, CurrentUserId);
            return Json(new { success, message, id });
        }

        [HttpPost]
        public async Task<IActionResult> EliminarPresentacion(int id)
        {
            var (success, message) = await _maestrosService.DeletePresentacionVentaAsync(id, CurrentUserId);
            return Json(new { success, message });
        }

        // =========================================================================
        // PESTAÑA 2: PROCESOS DE PRODUCCIÓN
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> ListarProcesos(int productoId)
        {
            var list = await _maestrosService.GetProductoProcesosAsync(productoId);
            return Json(new { success = true, data = list });
        }

        [HttpGet]
        public async Task<IActionResult> SubProcesosLookup()
        {
            var list = await _maestrosService.GetSubProcesosLookupAsync();
            return Json(new { success = true, data = list });
        }

        [HttpPost]
        public async Task<IActionResult> GuardarProceso([FromForm] ProductoProcesoModel model)
        {
            var (success, message, id) = await _maestrosService.SaveProductoProcesoAsync(model, CurrentUserId);
            return Json(new { success, message, id });
        }

        [HttpPost]
        public async Task<IActionResult> EliminarProceso(int id)
        {
            var (success, message) = await _maestrosService.DeleteProductoProcesoAsync(id, CurrentUserId);
            return Json(new { success, message });
        }

        // =========================================================================
        // PESTAÑA 3: DETALLE REQUERIDO
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> ListarDetalles(int productoId)
        {
            var list = await _maestrosService.GetProductoDetallesAsync(productoId);
            return Json(new { success = true, data = list });
        }

        [HttpPost]
        public async Task<IActionResult> GuardarDetalle([FromForm] ProductoDetalleModel model)
        {
            var (success, message, id) = await _maestrosService.SaveProductoDetalleAsync(model, CurrentUserId);
            return Json(new { success, message, id });
        }

        [HttpPost]
        public async Task<IActionResult> EliminarDetalle(int id)
        {
            var (success, message) = await _maestrosService.DeleteProductoDetalleAsync(id, CurrentUserId);
            return Json(new { success, message });
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarDetalleRequerido(int productoId, string detalleReq)
        {
            var (success, message) = await _maestrosService.UpdateDetalleRequeridoAsync(productoId, detalleReq, CurrentUserId);
            return Json(new { success, message });
        }

        // =========================================================================
        // CREACIÓN RÁPIDA DE COLOR
        // =========================================================================
        [HttpPost]
        public async Task<IActionResult> GuardarColorRapido(string nombre)
        {
            var (success, message, id, nom) = await _maestrosService.QuickCreateColorAsync(nombre, CurrentUserId);
            return Json(new { success, message, id, nombre = nom });
        }
    }
}

