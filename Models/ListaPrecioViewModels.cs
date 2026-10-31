using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AspNet10ArcoERP.Models;

public class ListaPrecioItemModel : MaestroEmpresaModel
{
    public int EmpresaSedeId { get; set; }
    public string SedeNombre { get; set; } = "";
    public string Nombre { get; set; } = "";
    public int ImpuestoTasaId { get; set; }
    public decimal Tasa { get; set; }
    public string ImpuestoNombre { get; set; } = "";
    public int MonedaId { get; set; }
    public string MonedaNombre { get; set; } = "";
    public string Simbolo { get; set; } = "";
    public int CantidadProductos { get; set; }
    public int CantidadGrupos { get; set; }

    protected override string VersionExtra => $"{EmpresaSedeId}|{Nombre}|{ImpuestoTasaId}|{MonedaId}";
}

public class ListaPrecioPageModel : ListadoPaginadoModel
{
    public List<ListaPrecioItemModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();
    public string? Search { get; set; }
    public int? SedeId { get; set; }
    public int? MonedaId { get; set; }
    public string? Estado { get; set; }
    public List<LookupItemModel> Sedes { get; set; } = new();
    public List<LookupItemModel> Monedas { get; set; } = new();
    public List<LookupItemModel> Tasas { get; set; } = new();
}

public class ListaPrecioDetalleModel
{
    public ListaPrecioItemModel Cabecera { get; set; } = new();
    public List<LookupItemModel> Sedes { get; set; } = new();
    public List<LookupItemModel> Monedas { get; set; } = new();
    public List<LookupItemModel> Tasas { get; set; } = new();
}

public class ListaPrecioSolicitud
{
    [Range(0, int.MaxValue)] public int Id { get; set; }
    [Range(1, int.MaxValue)] public int ContextoId { get; set; }
    [Range(1, int.MaxValue)] public int EmpresaSedeId { get; set; }
    [Required, StringLength(100, MinimumLength = 2)] public string Nombre { get; set; } = "";
    [Range(1, int.MaxValue)] public int ImpuestoTasaId { get; set; }
    [Range(1, int.MaxValue)] public int MonedaId { get; set; }
    [StringLength(64)] public string Revision { get; set; } = "";
}

public class ListaPrecioProductoItemModel : MaestroEmpresaModel
{
    public int ListaPrecioId { get; set; }
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Familia { get; set; } = "";
    public string Clase { get; set; } = "";
    public string Grupo { get; set; } = "";
    public decimal ValorUnitario { get; set; }
    public decimal PrecioUnitario { get; set; }
    public string PermiteDescuento { get; set; } = "N";
    public decimal Tasa { get; set; }

    public string ValorUnitarioTexto => ValorUnitario.ToString("N4", CultureInfo.GetCultureInfo("es-PE"));
    public string PrecioUnitarioTexto => PrecioUnitario.ToString("N4", CultureInfo.GetCultureInfo("es-PE"));

    protected override string VersionExtra => $"{ListaPrecioId}|{ProductoId}|{ValorUnitario}|{PrecioUnitario}|{PermiteDescuento}";
}

public class ListaPrecioProductoPageModel : ListadoPaginadoModel
{
    public List<ListaPrecioProductoItemModel> Items { get; set; } = new();
    public int ListaPrecioId { get; set; }
    public string? Search { get; set; }
    public string? Estado { get; set; }
}

public class ListaPrecioProductoSolicitud
{
    [Range(1, int.MaxValue)] public int Id { get; set; } // lista_p_prod_id
    [Range(1, int.MaxValue)] public int ContextoId { get; set; }
    [Range(1, int.MaxValue)] public int ListaPrecioId { get; set; }
    [Range(typeof(decimal), "0", "999999999.9999")] public decimal ValorUnitario { get; set; }
    [Range(typeof(decimal), "0", "999999999.9999")] public decimal PrecioUnitario { get; set; }
    [Required, RegularExpression("^[SN]$")] public string PermiteDescuento { get; set; } = "N";
    [Required, RegularExpression("^[AI]$")] public string Estado { get; set; } = "A";
    [Required, StringLength(64, MinimumLength = 64)] public string Revision { get; set; } = "";
}

public class ListaPrecioGrupoItemModel : MaestroEmpresaModel
{
    public int ListaPrecioId { get; set; }
    public int GrupoProveId { get; set; }
    public string GrupoNombre { get; set; } = "";
    public int CantidadDescuentos { get; set; }

    protected override string VersionExtra => $"{ListaPrecioId}|{GrupoProveId}";
}

public class ListaPrecioGrupoSolicitud
{
    [Range(1, int.MaxValue)] public int ContextoId { get; set; }
    [Range(1, int.MaxValue)] public int ListaPrecioId { get; set; }
    [Range(1, int.MaxValue)] public int GrupoProveId { get; set; }
}

public class ListaPrecioGrupoProductoItemModel : MaestroEmpresaModel
{
    public int ListaPProveId { get; set; }
    public int ListaPProdId { get; set; }
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal PrecioUnitario { get; set; }
    public decimal PorcentajePrecioBase { get; set; }

    public string PrecioUnitarioTexto => PrecioUnitario.ToString("N4", CultureInfo.GetCultureInfo("es-PE"));
    public string PorcentajePrecioBaseTexto => PorcentajePrecioBase.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));

    protected override string VersionExtra => $"{ListaPProveId}|{ListaPProdId}|{PorcentajePrecioBase}";
}

public class ListaPrecioGrupoProductoPageModel : ListadoPaginadoModel
{
    public List<ListaPrecioGrupoProductoItemModel> Items { get; set; } = new();
    public int ListaPProveId { get; set; }
    public string? Search { get; set; }
    public string? Estado { get; set; }
}

public class ListaPrecioGrupoProductoSolicitud
{
    [Range(0, int.MaxValue)] public int Id { get; set; } // lista_p_p_prod_id (0 para nuevo)
    [Range(1, int.MaxValue)] public int ContextoId { get; set; }
    [Range(1, int.MaxValue)] public int ListaPProveId { get; set; }
    [Range(1, int.MaxValue)] public int ListaPProdId { get; set; }
    [Range(typeof(decimal), "-100.0000", "1000.0000")] public decimal PorcentajePrecioBase { get; set; }
    [StringLength(64)] public string Revision { get; set; } = "";
}

