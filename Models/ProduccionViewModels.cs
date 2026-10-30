using System.ComponentModel.DataAnnotations;

namespace AspNet10ArcoERP.Models;

public class ProduccionRegistroModel : MaestroEmpresaModel
{
    public int ProcesoId { get; set; }
    public int SubProcesoId { get; set; }
    public int PersonalId { get; set; }
    public string Nombre { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string Documento { get; set; } = "";
    public string Sede { get; set; } = "";
    public string EstadoPersonal { get; set; } = "";
    protected override string VersionExtra=>$"{ProcesoId}|{SubProcesoId}|{PersonalId}|{Nombre}";
}

public class ProduccionPageModel : ListadoPaginadoModel
{
    public List<ProduccionRegistroModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();
    public string? Search { get; set; }
    public string? Estado { get; set; }
}

public class ProduccionProcesoDetalleModel
{
    public ProduccionRegistroModel Datos { get; set; } = new();
    public List<LookupItemModel> Subprocesos { get; set; } = new();
}

public class ProduccionNombreSolicitud
{
    [Range(1,int.MaxValue)] public int ContextoId { get; set; }
    [Range(0,int.MaxValue)] public int ProcesoId { get; set; }
    [Range(0,int.MaxValue)] public int Id { get; set; }
    [Required,StringLength(150)] public string Nombre { get; set; } = "";
    [StringLength(64)] public string? Revision { get; set; }
}

public class ProduccionEstadoSolicitud
{
    [Range(1,int.MaxValue)] public int ContextoId { get; set; }
    [Range(1,int.MaxValue)] public int ProcesoId { get; set; }
    [Range(1,int.MaxValue)] public int Id { get; set; }
    [Range(0,int.MaxValue)] public int SubProcesoId { get; set; }
    [Required,StringLength(64,MinimumLength=64)] public string Revision { get; set; } = "";
}

public class ProduccionOperadorSolicitud
{
    [Range(1,int.MaxValue)] public int ContextoId { get; set; }
    [Range(1,int.MaxValue)] public int ProcesoId { get; set; }
    [Range(1,int.MaxValue)] public int SubProcesoId { get; set; }
    [Range(1,int.MaxValue)] public int PersonalId { get; set; }
}
