using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AspNet10ArcoERP.Models;

public class MovAlmacenItemModel : MaestroEmpresaModel
{
    public int PeriodoEmpresaSedeId { get; set; }
    public string Codigo { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string FechaTexto => Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public int AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = "";

    public int TipoMovimientoAlmacenId { get; set; }
    public string TipoMovimientoAlmacenCodigo { get; set; } = "";
    public string TipoMovimientoAlmacenNombre { get; set; } = "";

    public int TransaccionAlmacenId { get; set; }
    public string TransaccionAlmacenCodigo { get; set; } = "";
    public string TransaccionAlmacenNombre { get; set; } = "";

    public int? AlmacenDestinoId { get; set; }
    public string? AlmacenDestinoNombre { get; set; }

    public int? ProveedorId { get; set; }
    public string? ProveedorNombre { get; set; }
    public string? ProveedorDocumento { get; set; }

    public int? OrdenCompraId { get; set; }
    public string NumeroOrdenCompra { get; set; } = "";

    public string NumeroGuiaRemision { get; set; } = "";

    public int? TipoDocumentoComercialId { get; set; }
    public string? TipoDocumentoComercialAbrev { get; set; }
    public string SerieDocumento { get; set; } = "";
    public string NumeroDocumento { get; set; } = "";
    public string DocumentoReferencialCompleto =>
        string.IsNullOrWhiteSpace(SerieDocumento) && string.IsNullOrWhiteSpace(NumeroDocumento)
            ? ""
            : $"{SerieDocumento}-{NumeroDocumento}";

    public string Observacion { get; set; } = "";

    public int EstadoMovimientoAlmacenId { get; set; }
    public string EstadoMovimientoAlmacenCodigo { get; set; } = "";
    public string EstadoMovimientoAlmacenNombre { get; set; } = "";

    public string UsuarioLogon { get; set; } = "";
    public int CantidadItems { get; set; }
    public decimal MontoTotal { get; set; }
    public string MontoTotalTexto => MontoTotal.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));

    public bool EsAnulado => EstadoMovimientoAlmacenCodigo == "9" || Estado == "I";

    protected override string VersionExtra => $"{PeriodoEmpresaSedeId}|{Codigo}|{AlmacenId}|{TipoMovimientoAlmacenId}|{TransaccionAlmacenId}|{EstadoMovimientoAlmacenId}";
}

public class MovAlmacenPageModel : ListadoPaginadoModel
{
    public List<MovAlmacenItemModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();

    public string? Search { get; set; }
    public string? Codigo { get; set; }
    public int? AlmacenId { get; set; }
    public int? TipoMovimientoAlmacenId { get; set; }
    public int? TransaccionAlmacenId { get; set; }
    public int? EstadoMovimientoAlmacenId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }

    public List<LookupItemModel> Almacenes { get; set; } = new();
    public List<LookupItemModel> TiposMovimiento { get; set; } = new();
    public List<LookupItemModel> Transacciones { get; set; } = new();
    public List<LookupItemModel> EstadosMovimiento { get; set; } = new();
}

public class MovAlmacenProductoItemModel : MaestroEmpresaModel
{
    public int MovimientoAlmacenId { get; set; }
    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string UnidadMedida { get; set; } = "UND";

    public decimal CantidadDocumento { get; set; }
    public decimal Cantidad { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal Total => Cantidad * ValorUnitario;

    public string CantidadTexto => Cantidad.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));
    public string ValorUnitarioTexto => ValorUnitario.ToString("N4", CultureInfo.GetCultureInfo("es-PE"));
    public string TotalTexto => Total.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));

    public decimal PesoNeto { get; set; }
    public decimal PesoBruto { get; set; }

    protected override string VersionExtra => $"{MovimientoAlmacenId}|{ProductoId}|{Cantidad}|{ValorUnitario}";
}

public class MovAlmacenDetalleModel
{
    public MovAlmacenItemModel Cabecera { get; set; } = new();
    public List<MovAlmacenProductoItemModel> Productos { get; set; } = new();

    public List<LookupItemModel> Almacenes { get; set; } = new();
    public List<LookupItemModel> TiposMovimiento { get; set; } = new();
    public List<LookupItemModel> Transacciones { get; set; } = new();
    public List<LookupItemModel> TiposDocumentoComercial { get; set; } = new();
}

public class MovAlmacenGuardarSolicitud
{
    public int Id { get; set; }

    [Required]
    public int ContextoId { get; set; }

    public string? Revision { get; set; }

    [Required]
    public int AlmacenId { get; set; }

    [Required]
    public int TipoMovimientoAlmacenId { get; set; }

    [Required]
    public int TransaccionAlmacenId { get; set; }

    [Required]
    public DateTime Fecha { get; set; } = DateTime.Today;

    public int? ProveedorId { get; set; }
    public int? AlmacenDestinoId { get; set; }
    public int? OrdenCompraId { get; set; }
    public string? NumeroOrdenCompra { get; set; }
    public string? NumeroGuiaRemision { get; set; }

    public int? TipoDocumentoComercialId { get; set; }
    public string? SerieDocumento { get; set; }
    public string? NumeroDocumento { get; set; }

    public string? Observacion { get; set; }
}

public class MovAlmacenProductoGuardarSolicitud
{
    public int Id { get; set; }

    [Required]
    public int MovimientoAlmacenId { get; set; }

    [Required]
    public int ContextoId { get; set; }

    public string? Revision { get; set; }

    [Required]
    public int ProductoId { get; set; }

    public string? Descripcion { get; set; }

    [Required]
    public decimal Cantidad { get; set; }

    public decimal CantidadDocumento { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal PesoNeto { get; set; }
    public decimal PesoBruto { get; set; }
}

