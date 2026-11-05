using System.ComponentModel.DataAnnotations;

namespace AspNet10ArcoERP.Models;

public class AnticipoClienteItemModel : MaestroEmpresaModel
{
    public DateTime Fecha { get; set; }
    public string NumeroOperacion { get; set; } = string.Empty;
    public int ClienteId { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public int MonedaId { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public string SimboloMoneda { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public decimal MontoAplicado { get; set; }
    public decimal Saldo => Math.Max(0, Monto - MontoAplicado);
    public string MedioPago { get; set; } = string.Empty;
    public string Banco { get; set; } = string.Empty;
    public string NumeroRef { get; set; } = string.Empty;
    public string Extornado { get; set; } = "N";
    public string EstadoComprobante { get; set; } = string.Empty;
    public string Observacion { get; set; } = string.Empty;

    protected override string VersionExtra => $"{Id}|{Monto}|{MontoAplicado}|{Extornado}|{EstadoComprobante}";
}

public class AnticipoClientePageModel : ListadoPaginadoModel
{
    public string? Search { get; set; }
    public int? ClienteId { get; set; }
    public int? MonedaId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public bool ConSaldo { get; set; }
    public List<LookupItemModel> Monedas { get; set; } = new();
    public List<LookupItemModel> MediosPago { get; set; } = new();
    public List<LookupItemModel> Bancos { get; set; } = new();
    public List<AnticipoClienteItemModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();
}

public class AnticipoClienteGuardarSolicitud
{
    public int ContextoId { get; set; }
    public int Id { get; set; }
    [Required, Range(1, int.MaxValue)]
    public int ClienteId { get; set; }
    [Range(1, int.MaxValue)]
    public int MonedaId { get; set; } = 1; // 1 = Soles
    [Range(0.01, 99999999)]
    public decimal Monto { get; set; }
    [Range(1, int.MaxValue)]
    public int MedioPagoId { get; set; } = 1;
    public int? BancoId { get; set; }
    [StringLength(50)]
    public string? NumeroRef { get; set; }
    public DateTime? FechaRef { get; set; }
    [StringLength(250)]
    public string? Observacion { get; set; }
    public int? VendedorId { get; set; }
}
