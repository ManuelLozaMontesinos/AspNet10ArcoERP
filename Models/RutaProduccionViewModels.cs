using System.ComponentModel.DataAnnotations;

namespace AspNet10ArcoERP.Models;

public class RutaProduccionItemModel : MaestroEmpresaModel
{
    public string Nombre { get; set; } = string.Empty;
    public int CantidadSubprocesos { get; set; }
    protected override string VersionExtra => Nombre;
}

public class RutaSubProcesoItemModel
{
    public int Id { get; set; }
    public int RutaProduccionId { get; set; }
    public int SubProcesoProduccionId { get; set; }
    public string SubProcesoNombre { get; set; } = string.Empty;
    public int ProcesoProduccionId { get; set; }
    public string ProcesoNombre { get; set; } = string.Empty;
    public int Orden { get; set; }
    public string Estado { get; set; } = "A";
}

public class RutaProduccionPageModel : ListadoPaginadoModel
{
    public string? Search { get; set; }
    public string? Estado { get; set; }
    public List<string> Errores { get; set; } = new();
    public List<RutaProduccionItemModel> Items { get; set; } = new();
}

public class RutaProduccionDetalleModel
{
    public RutaProduccionItemModel Cabecera { get; set; } = new();
    public List<RutaSubProcesoItemModel> Subprocesos { get; set; } = new();
    public List<LookupItemModel> SubprocesosDisponibles { get; set; } = new();
}

public class RutaProduccionGuardarSolicitud
{
    public int Id { get; set; }
    [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;
    public string? Revision { get; set; }
    public int ContextoId { get; set; }
}

public class RutaSubProcesoGuardarSolicitud
{
    [Required]
    public int RutaProduccionId { get; set; }
    [Required]
    public int SubProcesoProduccionId { get; set; }
    public int Orden { get; set; }
    public int ContextoId { get; set; }
}

public class RutaSubProcesoOrdenSolicitud
{
    [Required]
    public int RutaSubProcesoId { get; set; }
    [Required]
    public int Orden { get; set; }
    public int ContextoId { get; set; }
}
