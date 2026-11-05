namespace AspNet10ArcoERP.Models;

public class ProcesoIntegracionItemModel
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int ModuloId { get; set; }
    public string ModuloNombre { get; set; } = string.Empty;
    public string Estado { get; set; } = "A";
    public DateTime? UltimaEjecucion { get; set; }
}

public class ProcesoIntegracionPageModel
{
    public int ModuloId { get; set; }
    public string ModuloNombre { get; set; } = string.Empty;
    public List<ProcesoIntegracionItemModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();
}

public class EjecutarIntegracionSolicitud
{
    public int ProcesoId { get; set; }
    public string? Parametros { get; set; }
}

