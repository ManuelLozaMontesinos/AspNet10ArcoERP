using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace AspNet10ArcoERP.Models;

public class ConsultaSedeContextoModel
{
    public int PeriodoId { get; set; }
    public int Anio { get; set; }
    public string Empresa { get; set; } = "";
    public List<LookupItemModel> Sedes { get; set; } = new();
    public List<string> Errores { get; set; } = new();
    public string NombreSede(int? id)=>Sedes.FirstOrDefault(s=>s.Id==id)?.Nombre??"Seleccione una sede";
}

public class PuntoPedidoFiltro
{
    public int? ConsultaId { get; set; }
    public string? Search { get; set; }
    public string? Codigo { get; set; }
    public string? Documento { get; set; }
    public string? RazonSocial { get; set; }
    public Dictionary<string,string> Query(string sort,string dir)=>new(){["consultaId"]=ConsultaId?.ToString()??"",["search"]=Search??"",["codigo"]=Codigo??"",["documento"]=Documento??"",["razonSocial"]=RazonSocial??"",["sort"]=sort,["dir"]=dir};
}

public class PuntoPedidoFila
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int VinculoId { get; set; }
    public int ProveedorId { get; set; }
    public string Codigo { get; set; } = "";
    public string Producto { get; set; } = "";
    public string Unidad { get; set; } = "";
    public string Proveedor { get; set; } = "";
    public string Documento { get; set; } = "";
    public decimal PesoNeto { get; set; }
    public decimal PesoBruto { get; set; }
    public int MasterPack { get; set; }
    public decimal StockMinimo { get; set; }
    public decimal Saldo { get; set; }
}

public class PuntoPedidoPageModel : ListadoPaginadoModel
{
    public PuntoPedidoFiltro Filtro { get; set; } = new();
    public ConsultaSedeContextoModel Contexto { get; set; } = new();
    public List<PuntoPedidoFila> Items { get; set; } = new();
    public bool Consultada { get; set; }
    public string Resumen=>$"Ejercicio {Contexto.Anio} · {Contexto.NombreSede(Filtro.ConsultaId)}";
}

public class DetraccionFiltro
{
    public int? ConsultaId { get; set; }
    public string? Search { get; set; }
    public string? Numero { get; set; }
    public string? Documento { get; set; }
    public string? RazonSocial { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public Dictionary<string,string> Query(string sort,string dir)=>new()
    {
        ["consultaId"]=ConsultaId?.ToString()??"",["search"]=Search??"",["numero"]=Numero??"",["documento"]=Documento??"",["razonSocial"]=RazonSocial??"",
        ["desde"]=Desde?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)??"",["hasta"]=Hasta?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)??"",["sort"]=sort,["dir"]=dir
    };
}

public class DetraccionFila
{
    public int Id { get; set; }
    public int ConsultaId { get; set; }
    public string TipoDocumento { get; set; } = "";
    public string Documento { get; set; } = "";
    public string NumeroOperacion { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string Cliente { get; set; } = "";
    public string DocumentoCliente { get; set; } = "";
    public string Moneda { get; set; } = "";
    public decimal Total { get; set; }
    public string TipoDetraccion { get; set; } = "";
    public decimal Tasa { get; set; }
    public decimal MontoDetraccion { get; set; }
    public string NumeroOperacionPago { get; set; } = "";
    public DateTime? FechaPago { get; set; }
    public decimal MontoPago { get; set; }
    public decimal MontoCargo { get; set; }
    public DateTime FechaRevision { get; set; }
    public string MontoPagoTexto=>MontoPago.ToString("G29",CultureInfo.InvariantCulture);
    public string MontoCargoTexto=>MontoCargo.ToString("G29",CultureInfo.InvariantCulture);
    // The legacy payment procedure does not update fecha_modifica. Include its actual values.
    public string Revision=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|",new[]{Id.ToString(),ConsultaId.ToString(),FechaRevision.Ticks.ToString(),
        Documento,Cliente,Total.ToString("G29",CultureInfo.InvariantCulture),TipoDetraccion,Tasa.ToString("G29",CultureInfo.InvariantCulture),MontoDetraccion.ToString("G29",CultureInfo.InvariantCulture),
        NumeroOperacionPago,FechaPago?.Ticks.ToString()??"",MontoPagoTexto,MontoCargoTexto}))));
}

public class DetraccionPageModel : ListadoPaginadoModel
{
    public DetraccionFiltro Filtro { get; set; } = new();
    public ConsultaSedeContextoModel Contexto { get; set; } = new();
    public List<DetraccionFila> Items { get; set; } = new();
    public string Resumen=>$"Ejercicio {Contexto.Anio} · {Contexto.NombreSede(Filtro.ConsultaId)}";
}

public class DetraccionPagoSolicitud
{
    [Range(1,int.MaxValue)] public int ContextoId { get; set; }
    [Range(1,int.MaxValue)] public int ConsultaId { get; set; }
    [Range(1,int.MaxValue)] public int Id { get; set; }
    [Required,StringLength(64,MinimumLength=64)] public string Revision { get; set; } = "";
    [Required,StringLength(50)] public string NumeroOperacionPago { get; set; } = "";
    [Required] public DateTime? FechaPago { get; set; }
    [Required,RegularExpression(@"\d{1,8}(\.\d{1,4})?")] public string MontoPagoTexto { get; set; } = "";
    [Required,RegularExpression(@"\d{1,8}(\.\d{1,4})?")] public string MontoCargoTexto { get; set; } = "0";
}
