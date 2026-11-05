using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.CM.Controllers;

[Area("CM")]
public class ProveCompraController(IMaestrosService maestrosService, IRHService context, IAuthService auth, ILogger<ProveCompraController> logger)
    : ModuloContextController(context, auth, logger, "cm", "proveCompra/index")
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        string? estado = null,
        int? naturaleza = null,
        string? sort = null,
        string? dir = null,
        int top = 25)
    {
        if (!ModelState.IsValid || search?.Length > 150) return InvalidInput();

        var pageSize = top is 25 or 50 or 100 or 500 ? top : 25;
        // tipo: 2 = PROVEEDOR, esProspecto = "N"
        var list = await maestrosService.GetProveedoresAsync(
            search, 2, estado, naturaleza, sort, dir, pageSize, EmpresaId, "N");

        ViewBag.Search = search;
        ViewBag.Tipo = 2; // Proveedor
        ViewBag.Estado = estado;
        ViewBag.Naturaleza = naturaleza;
        ViewBag.Sort = string.IsNullOrWhiteSpace(sort) ? "id" : sort.ToLowerInvariant();
        ViewBag.Dir = string.IsNullOrWhiteSpace(dir) ? "desc" : dir.ToLowerInvariant();
        ViewBag.Top = pageSize;
        ViewBag.EsProspecto = "N";
        ViewBag.EsMarketingCliente = false;
        ViewBag.EsProveedor = true;
        ViewBag.TiposNaturaleza = await maestrosService.GetTiposNaturalezaJuridicaLookupAsync();
        ViewBag.TiposDoc = await maestrosService.GetTiposDocumentoIdentidadLookupAsync();
        ViewBag.TiposVia = await maestrosService.GetTiposViaLookupAsync();
        ViewBag.TiposZona = await maestrosService.GetTiposZonaLookupAsync();
        ViewBag.TiposProveedor = await maestrosService.GetTiposProveedorLookupAsync();
        ViewBag.CondicionesPago = await maestrosService.GetCondicionesPagoLookupAsync(EmpresaId);
        ViewBag.Departamentos = await maestrosService.GetDepartamentosLookupAsync();

        return View(list);
    }
}

