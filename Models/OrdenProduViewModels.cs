using System.ComponentModel.DataAnnotations;

namespace AspNet10ArcoERP.Models;

public class OrdenProduccionItemModel : MaestroEmpresaModel
{
    public string Codigo { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public int? CotizacionId { get; set; }
    public string? CotizacionCodigo { get; set; }
    public int ProveedorId { get; set; }
    public string ClienteRazonSocial { get; set; } = string.Empty;
    public string ClienteDocumento { get; set; } = string.Empty;
    public string? VendedorNombre { get; set; }
    public int AlmacenId { get; set; }
    public string? AlmacenNombre { get; set; }
    public int? RutaProduccionId { get; set; }
    public string? RutaProduccionNombre { get; set; }
    public DateTime? FechaEntrega { get; set; }
    public int EstadoOrdenProduccionId { get; set; }
    public string EstadoOrdenProduccionNombre { get; set; } = string.Empty;
    public int? EstadoPlanoMelId { get; set; }
    public string? EstadoPlanoMelNombre { get; set; }
    public int? EstadoPlanoMdfId { get; set; }
    public string? EstadoPlanoMdfNombre { get; set; }
    public int? EstadoDespachoId { get; set; }
    public string? EstadoDespachoNombre { get; set; }
    public int Modificaciones { get; set; }
    public string? Comentarios { get; set; }
    public string? Observacion { get; set; }
    public string? IncluyeInsumo { get; set; }
    public int CantidadItems { get; set; }
    public int CantidadSubprocesos { get; set; }

    protected override string VersionExtra => $"{Codigo}|{EstadoOrdenProduccionId}|{Modificaciones}|{Estado}";
}

public class OrdenProduccionProductoModel
{
    public int Id { get; set; }
    public int OrdenProduccionId { get; set; }
    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = string.Empty;
    public string ProductoDescripcion { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Cantidad { get; set; }
    public decimal CantidadEntregada { get; set; }
    public DateTime? FechaEntrega { get; set; }
    public string? EntregadoA { get; set; }
    public string UnidadMedida { get; set; } = string.Empty;
    public string? Insumo { get; set; }
    public string Estado { get; set; } = "A";
}

public class OrdenProduccionSubprocesoModel
{
    public int Id { get; set; }
    public int OrdenProduccionId { get; set; }
    public int SubProcesoProduccionId { get; set; }
    public string SubProcesoNombre { get; set; } = string.Empty;
    public int Orden { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public int? PersonalId { get; set; }
    public string? PersonalNombre { get; set; }
    public int? MaquinaId { get; set; }
    public string? MaquinaNombre { get; set; }
    public int? MaquinaRealId { get; set; }
    public string? MaquinaRealNombre { get; set; }
    public string? Observacion { get; set; }
    public int EstadoSubProcesoId { get; set; }
    public string EstadoSubProcesoNombre { get; set; } = string.Empty;
    public string Estado { get; set; } = "A";
}

public class OrdenProduccionPageModel : ListadoPaginadoModel
{
    public string? Search { get; set; }
    public string? Codigo { get; set; }
    public string? Cotizacion { get; set; }
    public string? Cliente { get; set; }
    public string? Vendedor { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public int? EstadoOrdenProduccionId { get; set; }
    public bool MostrarAnulados { get; set; }
    public List<LookupItemModel> Estados { get; set; } = new();
    public List<OrdenProduccionItemModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();
}

public class OrdenProduccionDetalleModel
{
    public OrdenProduccionItemModel Cabecera { get; set; } = new();
    public List<OrdenProduccionProductoModel> Productos { get; set; } = new();
    public List<OrdenProduccionSubprocesoModel> Subprocesos { get; set; } = new();
    public List<LookupItemModel> Almacenes { get; set; } = new();
    public List<LookupItemModel> Rutas { get; set; } = new();
    public List<LookupItemModel> Estados { get; set; } = new();
    public List<LookupItemModel> EstadosPlanoMel { get; set; } = new();
    public List<LookupItemModel> EstadosPlanoMdf { get; set; } = new();
    public List<LookupItemModel> Maquinas { get; set; } = new();
    public List<LookupItemModel> Personal { get; set; } = new();
}

public class OrdenProduccionGuardarSolicitud
{
    public int Id { get; set; }
    public int ContextoId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Today;
    public int AlmacenId { get; set; }
    public int ProveedorId { get; set; }
    public int? CotizacionId { get; set; }
    public int? RutaProduccionId { get; set; }
    public DateTime? FechaEntrega { get; set; }
    public string? Observacion { get; set; }
    public string? Revision { get; set; }
}

public class OrdenProduccionCambiarEstadoSolicitud
{
    public int OrdenProduccionId { get; set; }
    public int? EstadoPlanoMelId { get; set; }
    public int? EstadoPlanoMdfId { get; set; }
    public string? Comentarios { get; set; }
    public int? Modificaciones { get; set; }
}

public class OrdenProduccionProductoGuardarSolicitud
{
    public int Id { get; set; }
    public int OrdenProduccionId { get; set; }
    public int ProductoId { get; set; }
    public string? Descripcion { get; set; }
    public decimal Cantidad { get; set; }
    public decimal CantidadEntregada { get; set; }
    public string? EntregadoA { get; set; }
}

public class OrdenProduccionSubprocesoGuardarSolicitud
{
    public int Id { get; set; }
    public int OrdenProduccionId { get; set; }
    public int SubProcesoProduccionId { get; set; }
    public int? PersonalId { get; set; }
    public int? MaquinaId { get; set; }
    public int? MaquinaRealId { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string? Observacion { get; set; }
}

public class RequeInsumoPageModel : ListadoPaginadoModel
{
    public string? Search { get; set; }
    public string? Codigo { get; set; }
    public string? Cotizacion { get; set; }
    public string? Cliente { get; set; }
    public string? Vendedor { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public int? EstadoDespachoId { get; set; }
    public bool MostrarAnulados { get; set; }
    public List<LookupItemModel> EstadosDespacho { get; set; } = new();
    public List<OrdenProduccionItemModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();
}

public class RequeInsumoCerrarSolicitud
{
    public int OrdenProduccionId { get; set; }
    public int EstadoDespachoId { get; set; } = 3; // ATENDIDO
    public string? Observacion { get; set; }
}

public class PersonalMaquinaItemModel
{
    public int Id { get; set; }
    public int MaquinaId { get; set; }
    public string MaquinaNombre { get; set; } = string.Empty;
    public int PersonalId { get; set; }
    public string PersonalNombre { get; set; } = string.Empty;
    public int? AsistenteId { get; set; }
    public string? AsistenteNombre { get; set; }
    public DateTime Fecha { get; set; }
}

public class AsignarCortadorSolicitud
{
    public int OrdenProduccionId { get; set; }
    public int SubProcesoProduccionId { get; set; }
    public int MaquinaId { get; set; }
    public int? PersonalId { get; set; }
    public string? Observacion { get; set; }
}

