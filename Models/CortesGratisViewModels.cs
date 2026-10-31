using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AspNet10ArcoERP.Models;

public class CorteGratisClaseModel : MaestroEmpresaModel
{
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
}

public class CortesGratisPageModel : ListadoPaginadoModel
{
    public List<CorteGratisClaseModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();
    public string? Search { get; set; }
    public string? Estado { get; set; }
}

public class CortesGratisDetalleModel
{
    public CorteGratisClaseModel Datos { get; set; } = new();
    public List<LookupItemModel> Familias { get; set; } = new();
    public string Sede { get; set; } = "";
}

public class CorteGratisModel : MaestroEmpresaModel
{
    public int ClaseId { get; set; }
    public int FamiliaId { get; set; }
    public int ProductoId { get; set; }
    public int EmpresaSedeId { get; set; }
    public string Codigo { get; set; } = "";
    public string Producto { get; set; } = "";
    public string Familia { get; set; } = "";
    public string ProductoEstado { get; set; } = "";
    public string ClaseEstado { get; set; } = "";
    public decimal? Cantidad { get; set; }
    public string CantidadTexto=>Cantidad?.ToString("G29",CultureInfo.InvariantCulture)??"";
    public string CantidadVisible=>Cantidad?.ToString("N2",CultureInfo.GetCultureInfo("es-PE"))??"";
    protected override string VersionExtra=>$"{ClaseId}|{FamiliaId}|{ProductoId}|{EmpresaSedeId}|{CantidadTexto}|{ProductoEstado}|{ClaseEstado}";
}

public class CortesGratisProductoPageModel : ListadoPaginadoModel
{
    public List<CorteGratisModel> Items { get; set; } = new();
}

public class CorteGratisSolicitud
{
    [Range(1,int.MaxValue)] public int ContextoId { get; set; }
    [Range(1,int.MaxValue)] public int ClaseId { get; set; }
    [Range(1,int.MaxValue)] public int ProductoId { get; set; }
    [Range(1,int.MaxValue)] public int Id { get; set; }
    [Required,StringLength(64,MinimumLength=64)] public string Revision { get; set; } = "";
    [Required,Range(typeof(decimal),"-99999999.99","99999999.99")] public decimal? Cantidad { get; set; }
}
