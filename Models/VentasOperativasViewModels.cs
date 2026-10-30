using System.ComponentModel.DataAnnotations;

namespace AspNet10ArcoERP.Models;

public class CajaModel : MaestroEmpresaModel
{
    public int EmpresaSedeId { get; set; }
    public string Sede { get; set; } = "";
    [Required, StringLength(100)] public string Nombre { get; set; } = "";
}

public class VendedorModel : MaestroEmpresaModel
{
    public string Codigo { get; set; } = "";
    [Range(1, int.MaxValue)] public int ProveedorId { get; set; }
    public string Nombre { get; set; } = "";
    public string Documento { get; set; } = "";
    [StringLength(30)] public string? Abreviatura { get; set; }
    [StringLength(50)] public string? ServidorImpresion { get; set; }
}

public class CajaPageModel : ListadoPaginadoModel
{
    public List<CajaModel> Items { get; set; } = new();
    public VentasOperativasFiltrosModel Filtros { get; set; } = new();
    public string? Search { get; set; }
    public string? Estado { get; set; }
}

public class VendedorPageModel : ListadoPaginadoModel
{
    public List<VendedorModel> Items { get; set; } = new();
    public VentasOperativasFiltrosModel Filtros { get; set; } = new();
    public string? Search { get; set; }
    public string? Estado { get; set; }
}

public class VentasOperativasFiltrosModel
{
    public List<LookupItemModel> Sedes { get; set; } = new();
    public List<LookupItemModel> TiposDocumento { get; set; } = new();
    public List<LookupItemModel> Monedas { get; set; } = new();
}

public class AsignacionVentaModel : MaestroEmpresaModel
{
    public int DestinoId { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Documento { get; set; } = "";
    public string Sede { get; set; } = "";
    public string Almacen { get; set; } = "";
    public int Correlativo { get; set; }
    public string Moneda { get; set; } = "";
}

public class AsignacionVentaPageModel : ListadoPaginadoModel
{
    public List<AsignacionVentaModel> Items { get; set; } = new();
}

public class VentaVinculoSolicitud
{
    [Range(1, int.MaxValue)] public int ContextoId { get; set; }
    [Range(1, int.MaxValue)] public int OwnerId { get; set; }
    [Required, StringLength(16)] public string Tipo { get; set; } = "";
    [Required, MinLength(1), MaxLength(500)] public int[] DestinoIds { get; set; } = Array.Empty<int>();
}

public class VentaEstadoSolicitud
{
    [Range(1, int.MaxValue)] public int ContextoId { get; set; }
    [Range(1, int.MaxValue)] public int Id { get; set; }
    [Required, StringLength(64, MinimumLength = 64)] public string Revision { get; set; } = "";
    public bool Activar { get; set; }
}

public class VentaVinculoEstadoSolicitud : VentaEstadoSolicitud
{
    [Range(1, int.MaxValue)] public int OwnerId { get; set; }
    [Required, StringLength(16)] public string Tipo { get; set; } = "";
}
