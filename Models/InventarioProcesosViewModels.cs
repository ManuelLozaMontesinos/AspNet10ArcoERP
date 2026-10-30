using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AspNet10ArcoERP.Models;

public class InventarioProcesoPageModel : ListadoPaginadoModel
{
    public string Tipo { get; set; } = "cierre";
    public InventarioReporteScope Contexto { get; set; } = new();
    public int? AlmacenId { get; set; }
    public string? Search { get; set; }
    public string? Estado { get; set; }
    public List<LookupItemModel> Almacenes { get; set; } = new();
    public List<InventarioCierreRowModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();
    public string Titulo => Tipo == "reproceso" ? "Reproceso de inventario" : "Apertura y cierre de almacenes";
    public string Resumen => $"Ejercicio {Contexto.Anio} · {Contexto.Sede}";
}

public class InventarioCierreRowModel
{
    public int Id { get; set; }
    public int MesContableId { get; set; }
    public string MesCodigo { get; set; } = "";
    public string MesNombre { get; set; } = "";
    public DateTime? FechaCierre { get; set; }
    public string Estado { get; set; } = "";
    public string EstadoNombre { get; set; } = "";
    public string MovimientoPostCierre { get; set; } = "";
    public DateTime FechaRevision { get; set; }
    public string Revision => string.Join("|", Id.ToString(CultureInfo.InvariantCulture), FechaRevision.Ticks.ToString(CultureInfo.InvariantCulture), Estado,
        FechaCierre?.Ticks.ToString(CultureInfo.InvariantCulture) ?? "", MovimientoPostCierre);
    public bool AdmiteReproceso => string.CompareOrdinal(MesCodigo, "00") >= 0 && string.CompareOrdinal(MesCodigo, "12") <= 0;
}

public class InventarioProcesoSolicitud
{
    [Range(1, int.MaxValue)] public int ContextoId { get; set; }
    [Range(1, int.MaxValue)] public int AlmacenId { get; set; }
    public int? MesContableId { get; set; }
    [StringLength(128)] public string? Revision { get; set; }
    public int? HastaMes { get; set; }
}

public class InventarioProcesoPreparacionModel
{
    public int ContextoId { get; set; }
    public string Operacion { get; set; } = "";
    public int AlmacenId { get; set; }
    public string Almacen { get; set; } = "";
    public string Sede { get; set; } = "";
    public int Anio { get; set; }
    public bool EsVenta { get; set; }
    public int? MesContableId { get; set; }
    public string Revision { get; set; } = "";
    public int HastaMes { get; set; }
    public List<InventarioCierreRowModel> Meses { get; set; } = new();
    public List<string> Errores { get; set; } = new();
}

public class LlegadaCompraFiltro
{
    [StringLength(150)] public string? Search { get; set; }
    [StringLength(150)] public string? Proveedor { get; set; }
    public DateTime? Fecha { get; set; }
}

public class LlegadaCompraPageModel : ListadoPaginadoModel
{
    public LlegadaCompraFiltro Filtro { get; set; } = new();
    public InventarioReporteScope Contexto { get; set; } = new();
    public bool SinProveedor { get; set; }
    public List<LlegadaCompraRowModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();
}

public class LlegadaCompraRowModel
{
    public int Id { get; set; }
    public string Proveedor { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string Producto { get; set; } = "";
    public string Unidad { get; set; } = "";
    public DateTime? Fecha { get; set; }
    public decimal Cantidad { get; set; }
    public string OrdenCompra { get; set; } = "";
    public decimal CantidadOrdenada { get; set; }
    public decimal CantidadIngresada { get; set; }
    public decimal Pendiente => CantidadOrdenada - CantidadIngresada;
}
