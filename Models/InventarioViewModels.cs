using System.ComponentModel.DataAnnotations;

namespace AspNet10ArcoERP.Models;

public class TransaccionAlmacenModel
{
    [Range(0, int.MaxValue)] public int Id { get; set; }
    public string Codigo { get; set; } = "";
    [Required, StringLength(100)] public string Nombre { get; set; } = "";
    [Range(1, int.MaxValue)] public int? TipoMovimientoId { get; set; }
    public string TipoMovimientoNombre { get; set; } = "";
    [Range(1, int.MaxValue)] public int? TipoOperacionId { get; set; }
    public string TipoOperacionNombre { get; set; } = "";
    public bool EsCompra { get; set; }
    public string Estado { get; set; } = "A";
}

public class TransaccionAlmacenPageModel : ListadoPaginadoModel
{
    public List<TransaccionAlmacenModel> Items { get; set; } = new();
    public List<LookupItemModel> TiposMovimiento { get; set; } = new();
    public List<LookupItemModel> TiposOperacion { get; set; } = new();
}

public class AlmacenModel
{
    [Range(0, int.MaxValue)] public int Id { get; set; }
    [Range(1, int.MaxValue)] public int EmpresaSedeId { get; set; }
    public string SedeNombre { get; set; } = "";
    [Required, StringLength(100)] public string Nombre { get; set; } = "";
    [Required, StringLength(20)] public string Abreviatura { get; set; } = "";
    public bool EsVenta { get; set; }
    [Range(1, int.MaxValue)] public int TipoViaId { get; set; }
    [StringLength(50)] public string? NombreVia { get; set; }
    [StringLength(10)] public string? NumeroVia { get; set; }
    [StringLength(10)] public string? DepartamentoInterior { get; set; }
    [StringLength(10)] public string? Interior { get; set; }
    [StringLength(10)] public string? Manzana { get; set; }
    [StringLength(10)] public string? Lote { get; set; }
    [StringLength(10)] public string? Kilometro { get; set; }
    [StringLength(10)] public string? Block { get; set; }
    [StringLength(10)] public string? Etapa { get; set; }
    [Range(1, int.MaxValue)] public int TipoZonaId { get; set; }
    [StringLength(25)] public string? NombreZona { get; set; }
    [Range(1, int.MaxValue)] public int DistritoId { get; set; }
    public int ProvinciaId { get; set; }
    public int DepartamentoId { get; set; }
    [StringLength(4)] public string? CodigoSunat { get; set; }
    [StringLength(50)] public string? ServidorImpresion { get; set; }
    public string Direccion { get; set; } = "";
    public string Estado { get; set; } = "A";
}

public class AlmacenPageModel : ListadoPaginadoModel
{
    public List<AlmacenModel> Items { get; set; } = new();
    public List<LookupItemModel> Sedes { get; set; } = new();
    public List<LookupItemModel> TiposVia { get; set; } = new();
    public List<LookupItemModel> TiposZona { get; set; } = new();
    public List<LookupItemModel> Departamentos { get; set; } = new();
}

public class AlmacenTransaccionModel
{
    [Range(0, int.MaxValue)] public int Id { get; set; }
    [Range(1, int.MaxValue)] public int AlmacenId { get; set; }
    [Range(1, int.MaxValue)] public int TransaccionId { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string TipoMovimientoNombre { get; set; } = "";
    public string Estado { get; set; } = "A";
}

public class AlmacenConfiguracionModel
{
    public int Id { get; set; }
    [Range(1, int.MaxValue)] public int AlmacenTransaccionId { get; set; }
    [Range(1, int.MaxValue)] public int AtributoId { get; set; }
    public string Nombre { get; set; } = "";
    public bool Visible { get; set; }
    public bool Requerido { get; set; }
}

public class AlmacenSerieModel
{
    public int Id { get; set; }
    public int DocumentoSerieId { get; set; }
    public string DocumentoNombre { get; set; } = "";
    public string Serie { get; set; } = "";
    public int Correlativo { get; set; }
    public string Estado { get; set; } = "A";
}

public class AlmacenDetalleModel
{
    public List<AlmacenTransaccionModel> Transacciones { get; set; } = new();
    public List<LookupItemModel> TransaccionesDisponibles { get; set; } = new();
    public List<AlmacenSerieModel> Series { get; set; } = new();
    public List<LookupItemModel> SeriesDisponibles { get; set; } = new();
}
