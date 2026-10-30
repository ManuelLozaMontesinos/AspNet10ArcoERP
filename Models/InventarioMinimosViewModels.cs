using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace AspNet10ArcoERP.Models;

public class VolumenExistenciaPageModel : InventarioReporteModel
{
    public int? EmpresaSedeId { get; set; }
    public string SedeSeleccionada { get; set; } = "";
    public List<LookupItemModel> Sedes { get; set; } = new();
    public List<StockMinimoRowModel> Items { get; set; } = new();
    public override string Ambito => SedeSeleccionada;
}

public class StockMinimoRowModel
{
    public int Id { get; set; }
    public int ContextoId { get; set; }
    public int EmpresaSedeId { get; set; }
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = "";
    public string Producto { get; set; } = "";
    public string Unidad { get; set; } = "";
    public string EstadoProducto { get; set; } = "";
    public decimal StockMinimo { get; set; }
    public string StockMinimoTexto => StockMinimo.ToString(CultureInfo.InvariantCulture);
    public DateTime? FechaRevision { get; set; }
    public string Revision => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        $"{ContextoId}|{EmpresaSedeId}|{ProductoId}|{Id}|{FechaRevision?.Ticks}|{StockMinimo.ToString(CultureInfo.InvariantCulture)}")));
}

public class StockMinimoCambio
{
    [Range(1, int.MaxValue)] public int ProductoId { get; set; }
    [Range(typeof(decimal), "0", "999999999999999.9999")] public decimal StockMinimo { get; set; }
    [Required, StringLength(64, MinimumLength = 64)] public string Revision { get; set; } = "";
}

public class StockMinimoSolicitud
{
    [Range(1, int.MaxValue)] public int ContextoId { get; set; }
    [Range(1, int.MaxValue)] public int EmpresaSedeId { get; set; }
    [Required, MinLength(1), MaxLength(5000)] public List<StockMinimoCambio> Cambios { get; set; } = new();
}

public class StockMinimoImportacionModel
{
    public int ContextoId { get; set; }
    public int EmpresaSedeId { get; set; }
    public string Sede { get; set; } = "";
    public int Anio { get; set; }
    public List<StockMinimoImportFila> Filas { get; set; } = new();
    public List<string> Errores { get; set; } = new();
}

public class StockMinimoImportFila : StockMinimoCambio
{
    public int Fila { get; set; }
    public string Codigo { get; set; } = "";
    public string Producto { get; set; } = "";
    public decimal Anterior { get; set; }
    public string StockMinimoTexto => StockMinimo.ToString(CultureInfo.InvariantCulture);
    public string AnteriorTexto => Anterior.ToString(CultureInfo.InvariantCulture);
}

public static class StockMinimoFormato
{
    public const int MaxFilas = 5000;
    public const int MaxBytes = 10 * 1024 * 1024;
    public static IReadOnlyList<string> Columnas { get; } = Array.AsReadOnly(new[] { "alm_vol_prod_id", "producto_id", "codigo", "descripcion", "unidad_medida", "stock_minimo" });
}
