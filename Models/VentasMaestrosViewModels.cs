using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace AspNet10ArcoERP.Models;

public abstract class MaestroEmpresaModel
{
    [Range(0, int.MaxValue)] public int Id { get; set; }
    public string Estado { get; set; } = "A";
    public DateTime FechaRevision { get; set; }
    public string Revision => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{Id}|{FechaRevision.Ticks}|{Estado}|{VersionExtra}")));
    protected virtual string VersionExtra => "";
}

public class MedioPagoModel : MaestroEmpresaModel
{
    public string Codigo { get; set; } = "";
    [Required, StringLength(100)] public string Nombre { get; set; } = "";
    [Required, StringLength(5)] public string Abreviatura { get; set; } = "";
    [Range(typeof(decimal), "0", "99999999.9999")] public decimal PorcentajeTarjeta { get; set; }
}

public class MedioPagoPageModel : ListadoPaginadoModel
{
    public List<MedioPagoModel> Items { get; set; } = new();
    public string? Search { get; set; }
    public string? Estado { get; set; }
}

public class GrupoClienteModel : MaestroEmpresaModel
{
    [Required, StringLength(100)] public string Nombre { get; set; } = "";
    public bool IgnorarIncrementoFraccion { get; set; } = true;
}

public class GrupoClientePageModel : ListadoPaginadoModel
{
    public List<GrupoClienteModel> Items { get; set; } = new();
    public string? Search { get; set; }
    public string? Estado { get; set; }
}

public class GrupoClienteItemModel : MaestroEmpresaModel
{
    public int GrupoId { get; set; }
    public int ProveedorId { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Documento { get; set; } = "";
}

public class GrupoClienteDetalleModel : ListadoPaginadoModel
{
    public List<GrupoClienteItemModel> Items { get; set; } = new();
}

public class MaestroEdicion<T> where T : MaestroEmpresaModel, new()
{
    [Range(1, int.MaxValue)] public int ContextoId { get; set; }
    [StringLength(64)] public string? Revision { get; set; }
    [Required] public T Datos { get; set; } = new();
}

public class DocumentoSerieModel : MaestroEmpresaModel
{
    [Range(1, int.MaxValue)] public int EmpresaSedeId { get; set; }
    public string Sede { get; set; } = "";
    [Range(1, int.MaxValue)] public int TipoDocumentoId { get; set; }
    public string TipoDocumento { get; set; } = "";
    [Required, StringLength(4)] public string Serie { get; set; } = "";
    [Range(0, int.MaxValue)] public int Correlativo { get; set; }
    public bool Electronico { get; set; }
    public bool ComprobantePago { get; set; }
    public bool EsContingencia { get; set; }
    public bool EnvioEnLinea { get; set; }
    [Required, StringLength(10)] public string Formato { get; set; } = "00000000";
    public short MaximoItems { get; set; }
    [StringLength(30)] public string? ArchivoFormato { get; set; }
    public int? TipoRelacionadoId { get; set; }
    public string TipoRelacionado { get; set; } = "";
    public int? AlmacenId { get; set; }
    public string Almacen { get; set; } = "";
    protected override string VersionExtra => Correlativo.ToString(CultureInfo.InvariantCulture);
}

public class TipoDocumentoSerieLookup : LookupItemModel
{
    public bool Electronico { get; set; }
    public bool ComprobantePago { get; set; }
}

public class DocumentoSeriePageModel : ListadoPaginadoModel
{
    public List<DocumentoSerieModel> Items { get; set; } = new();
    public List<LookupItemModel> Sedes { get; set; } = new();
    public List<TipoDocumentoSerieLookup> TiposDocumento { get; set; } = new();
    public string? Search { get; set; }
    public string? Estado { get; set; }
    public int? SedeId { get; set; }
    public int? TipoDocumentoId { get; set; }
}
