using System.ComponentModel.DataAnnotations;

namespace AspNet10ArcoERP.Models
{
    public class GuiaRemiItemModel
    {
        public int Id { get; set; }
        public string SerieNumero { get; set; } = string.Empty;
        public string Serie { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string FechaTexto => Fecha.ToString("dd/MM/yyyy");
        public DateTime FechaInicioTraslado { get; set; }
        public string FechaInicioTrasladoTexto => FechaInicioTraslado.ToString("dd/MM/yyyy");
        public int? AlmacenId { get; set; }
        public string AlmacenNombre { get; set; } = string.Empty;
        public int? MovimientoAlmacenId { get; set; }
        public string MovimientoAlmacenCodigo { get; set; } = string.Empty;
        public int ProveedorDestinoId { get; set; }
        public string Destinatario { get; set; } = string.Empty;
        public string DestinatarioDocumento { get; set; } = string.Empty;
        public int MotivoTrasladoId { get; set; }
        public string MotivoTraslado { get; set; } = string.Empty;
        public string Transportista { get; set; } = string.Empty;
        public string PuntoPartida { get; set; } = string.Empty;
        public string PuntoLlegada { get; set; } = string.Empty;
        public decimal PesoBrutoTotal { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public int EstadoGuiaRemisionId { get; set; }
        public string EstadoGuiaRemisionCodigo { get; set; } = string.Empty;
        public string EstadoGuiaRemisionNombre { get; set; } = string.Empty;
        public int? EstadoAprobacionId { get; set; }
        public string EstadoAprobacionCodigo { get; set; } = string.Empty;
        public string EstadoAprobacionNombre { get; set; } = string.Empty;
        public string EstadoSunatCodigo { get; set; } = string.Empty;
        public string EstadoSunatNombre { get; set; } = string.Empty;
        public string Impreso { get; set; } = "N";
        public string Estado { get; set; } = "A";
        public string EstadoTexto => Estado == "A" ? "Activo" : "Anulado";
        public string Revision => $"{Id}|{Serie}|{Numero}|{EstadoGuiaRemisionId}|{EstadoAprobacionId}|{Estado}";
    }

    public class GuiaRemiPageModel : ListadoPaginadoModel
    {
        public int? AlmacenId { get; set; }
        public string? Codigo { get; set; }
        public string? Search { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public int? EstadoGuiaId { get; set; }
        public int? EstadoAprobacionId { get; set; }
        public int? MotivoTrasladoId { get; set; }
        public bool EsInterno { get; set; } = true;

        public List<LookupItemModel> Almacenes { get; set; } = new();
        public List<LookupItemModel> MotivosTraslado { get; set; } = new();
        public List<LookupItemModel> EstadosGuia { get; set; } = new();
        public List<LookupItemModel> EstadosAprobacion { get; set; } = new();

        public List<GuiaRemiItemModel> Items { get; set; } = new();
        public List<string> Errores { get; set; } = new();
    }

    public class GuiaRemiDetalleModel
    {
        public int Id { get; set; }
        public int AlmacenId { get; set; }
        public int TipoDocumentoComercialId { get; set; } = 9; // Guia Remision Remitente
        public string Serie { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Today;
        public DateTime FechaInicioTraslado { get; set; } = DateTime.Today;
        public int? MovimientoAlmacenId { get; set; }
        public string? MovimientoAlmacenCodigo { get; set; }
        public int MotivoTrasladoId { get; set; } = 6; // Traslado entre establecimientos default
        public int ProveedorDestinoId { get; set; }
        public string? ProveedorDestinoNombre { get; set; }
        public string? ProveedorDestinoDocumento { get; set; }
        public int? TransportistaId { get; set; }
        public int? TransportistaConductorId { get; set; }
        public int? TransportistaUnidadId { get; set; }

        public int DepartamentoPartidaId { get; set; } = 15;
        public int ProvinciaPartidaId { get; set; } = 128;
        public int DistritoPartidaId { get; set; } = 1251;
        public string? PuntoPartida { get; set; }
        public string? CodigoSunatPartida { get; set; }

        public int DepartamentoLlegadaId { get; set; } = 15;
        public int ProvinciaLlegadaId { get; set; } = 128;
        public int DistritoLlegadaId { get; set; } = 1251;
        public string? PuntoLlegada { get; set; }
        public string? CodigoSunatLlegada { get; set; }

        public decimal CostoFlete { get; set; }
        public decimal PesoBrutoTotal { get; set; }
        public string? Observacion { get; set; }
        public string EsInterno { get; set; } = "S";
        public string EsVenta { get; set; } = "N";
        public int EstadoGuiaRemisionId { get; set; } = 1; // En Edición
        public int? EstadoAprobacionId { get; set; }
        public string Estado { get; set; } = "A";
        public string? Revision { get; set; }

        public List<LookupItemModel> Almacenes { get; set; } = new();
        public List<LookupItemModel> Series { get; set; } = new();
        public List<LookupItemModel> MotivosTraslado { get; set; } = new();
        public List<LookupItemModel> Transportistas { get; set; } = new();
        public List<LookupItemModel> Conductores { get; set; } = new();
        public List<LookupItemModel> Unidades { get; set; } = new();
        public List<LookupItemModel> Departamentos { get; set; } = new();
        public List<LookupItemModel> ProvinciasPartida { get; set; } = new();
        public List<LookupItemModel> DistritosPartida { get; set; } = new();
        public List<LookupItemModel> ProvinciasLlegada { get; set; } = new();
        public List<LookupItemModel> DistritosLlegada { get; set; } = new();

        public List<GuiaRemiProductoModel> Productos { get; set; } = new();
    }

    public class GuiaRemiProductoModel
    {
        public int Id { get; set; }
        public int GuiaRemisionId { get; set; }
        public int? MovimientoAlmacenProductoId { get; set; }
        public int? ProductoId { get; set; }
        public string ProductoCodigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal PesoNeto { get; set; }
        public decimal PesoBruto { get; set; }
        public string Estado { get; set; } = "A";
    }

    public class GuiaRemiGuardarSolicitud
    {
        public int Id { get; set; }
        public int AlmacenId { get; set; }
        [Required]
        public string Serie { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Today;
        public DateTime FechaInicioTraslado { get; set; } = DateTime.Today;
        public int? MovimientoAlmacenId { get; set; }
        public int MotivoTrasladoId { get; set; }
        public int ProveedorDestinoId { get; set; }
        public int? TransportistaId { get; set; }
        public int? TransportistaConductorId { get; set; }
        public int? TransportistaUnidadId { get; set; }
        public int DistritoPartidaId { get; set; }
        [Required]
        public string PuntoPartida { get; set; } = string.Empty;
        public string? CodigoSunatPartida { get; set; }
        public int DistritoLlegadaId { get; set; }
        [Required]
        public string PuntoLlegada { get; set; } = string.Empty;
        public string? CodigoSunatLlegada { get; set; }
        public decimal CostoFlete { get; set; }
        public decimal PesoBrutoTotal { get; set; }
        public string? Observacion { get; set; }
        public string EsInterno { get; set; } = "S";
        public string? Revision { get; set; }

        public List<GuiaRemiProductoGuardarSolicitud> Productos { get; set; } = new();
    }

    public class GuiaRemiProductoGuardarSolicitud
    {
        public int Id { get; set; }
        public int? MovimientoAlmacenProductoId { get; set; }
        public int? ProductoId { get; set; }
        public string ProductoCodigo { get; set; } = string.Empty;
        [Required]
        public string Descripcion { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal PesoNeto { get; set; }
        public decimal PesoBruto { get; set; }
    }

    public class GuiaRemiAnularSolicitud
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Debe ingresar el motivo de anulación.")]
        [StringLength(200, ErrorMessage = "El motivo no puede superar 200 caracteres.")]
        public string MotivoAnulacion { get; set; } = string.Empty;
        public string? Revision { get; set; }
    }

    public class GuiaRemiAutoPageModel : ListadoPaginadoModel
    {
        public int? AlmacenId { get; set; }
        public string? Codigo { get; set; }
        public string? Search { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public int? MotivoTrasladoId { get; set; }

        public List<LookupItemModel> Almacenes { get; set; } = new();
        public List<LookupItemModel> MotivosTraslado { get; set; } = new();

        public List<GuiaRemiItemModel> Items { get; set; } = new();
        public List<string> Errores { get; set; } = new();
    }

    public class AprobarGuiaRemiSolicitud
    {
        public int Id { get; set; }
        public string? Observacion { get; set; }
        public string? Revision { get; set; }
    }

    public class DesaprobarGuiaRemiSolicitud
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Debe indicar el motivo de desaprobación.")]
        public string Motivo { get; set; } = string.Empty;
        public string? Revision { get; set; }
    }
}

