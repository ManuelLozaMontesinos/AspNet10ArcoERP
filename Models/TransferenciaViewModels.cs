using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AspNet10ArcoERP.Models;

public class TransfeSalidaItemModel : MaestroEmpresaModel
{
    public int PeriodoEmpresaSedeId { get; set; }
    public string Codigo { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string FechaTexto => Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public int AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = "";

    public int AlmacenDestinoId { get; set; }
    public string AlmacenDestinoNombre { get; set; } = "";

    public string NumeroGuiaRemision { get; set; } = "";
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

    public bool EsTransito => EstadoMovimientoAlmacenCodigo == "T" || EstadoMovimientoAlmacenId == 2;
    public bool EsAnulado => EstadoMovimientoAlmacenCodigo == "9" || EstadoMovimientoAlmacenId == 3 || Estado == "I";
    public bool EsRecepcionado => EstadoMovimientoAlmacenCodigo == "M" || EstadoMovimientoAlmacenId == 1;

    protected override string VersionExtra => $"{PeriodoEmpresaSedeId}|{Codigo}|{AlmacenId}|{AlmacenDestinoId}|{EstadoMovimientoAlmacenId}";
}

public class TransfeSalidaPageModel : ListadoPaginadoModel
{
    public List<TransfeSalidaItemModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();

    public string? Search { get; set; }
    public string? Codigo { get; set; }
    public int? AlmacenId { get; set; }
    public int? AlmacenDestinoId { get; set; }
    public int? EstadoMovimientoAlmacenId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }

    public List<LookupItemModel> Almacenes { get; set; } = new();
    public List<LookupItemModel> AlmacenesDestino { get; set; } = new();
    public List<LookupItemModel> EstadosMovimiento { get; set; } = new();
}

public class TransfeSalidaGuardarSolicitud
{
    public int Id { get; set; }

    [Required]
    public int ContextoId { get; set; }

    public string? Revision { get; set; }

    [Required]
    public int AlmacenId { get; set; }

    [Required]
    public int AlmacenDestinoId { get; set; }

    [Required]
    public DateTime Fecha { get; set; } = DateTime.Today;

    public string? NumeroGuiaRemision { get; set; }
    public string? SerieDocumento { get; set; }
    public string? NumeroDocumento { get; set; }
    public string? Observacion { get; set; }
}

public class TransfeIngresoItemModel : MaestroEmpresaModel
{
    public int PeriodoEmpresaSedeId { get; set; }
    public string Codigo { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string FechaTexto => Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public int AlmacenOrigenId { get; set; }
    public string AlmacenOrigenNombre { get; set; } = "";

    public int AlmacenDestinoId { get; set; }
    public string AlmacenDestinoNombre { get; set; } = "";

    public string NumeroGuiaRemision { get; set; } = "";
    public string SerieDocumento { get; set; } = "";
    public string NumeroDocumento { get; set; } = "";
    public string Observacion { get; set; } = "";

    public int EstadoMovimientoAlmacenId { get; set; }
    public string EstadoMovimientoAlmacenCodigo { get; set; } = "";
    public string EstadoMovimientoAlmacenNombre { get; set; } = "";

    public int CantidadItems { get; set; }
    public decimal MontoTotal { get; set; }
    public string MontoTotalTexto => MontoTotal.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));

    public bool EsPendienteRecepcion => EstadoMovimientoAlmacenCodigo == "T" || EstadoMovimientoAlmacenId == 2;
    public bool EsRecepcionado => EstadoMovimientoAlmacenCodigo == "M" || EstadoMovimientoAlmacenId == 1;

    protected override string VersionExtra => $"{PeriodoEmpresaSedeId}|{Codigo}|{AlmacenOrigenId}|{AlmacenDestinoId}|{EstadoMovimientoAlmacenId}";
}

public class TransfeIngresoPageModel : ListadoPaginadoModel
{
    public List<TransfeIngresoItemModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();

    public string? Search { get; set; }
    public string? Codigo { get; set; }
    public int? AlmacenOrigenId { get; set; }
    public int? EstadoMovimientoAlmacenId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }

    public List<LookupItemModel> AlmacenesOrigen { get; set; } = new();
    public List<LookupItemModel> EstadosMovimiento { get; set; } = new();
}

public class RecepcionarTransferenciaSolicitud
{
    [Required]
    public int Id { get; set; }

    [Required]
    public int ContextoId { get; set; }

    public string? Revision { get; set; }

    [Required]
    public DateTime FechaRecepcion { get; set; } = DateTime.Today;

    public string? Observacion { get; set; }
}

