using System.Globalization;

namespace AspNet10ArcoERP.Models;

public class InventarioReporteFiltro
{
    public string Modo { get; set; } = "mes";
    public int? Mes { get; set; }
    public DateTime? Fecha { get; set; }
    public int? AlmacenId { get; set; }
    public int? TransaccionId { get; set; }
    public int? ProductoId { get; set; }
    public int? ProveedorId { get; set; }
    public string? Search { get; set; }
    public string? Codigo { get; set; }
    public string? Referencia { get; set; }
    public bool Consultar { get; set; }
    public string Variante { get; set; } = "detalle";
    public bool? IncluirReserva { get; set; }

    public Dictionary<string, string> Query(string sort, string dir) => new()
    {
        ["modo"] = Modo, ["mes"] = Mes?.ToString(CultureInfo.InvariantCulture) ?? "",
        ["fecha"] = Fecha?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "",
        ["almacenId"] = AlmacenId?.ToString() ?? "", ["transaccionId"] = TransaccionId?.ToString() ?? "",
        ["productoId"] = ProductoId?.ToString() ?? "", ["proveedorId"] = ProveedorId?.ToString() ?? "",
        ["search"] = Search ?? "", ["codigo"] = Codigo ?? "", ["referencia"] = Referencia ?? "",
        ["consultar"] = "true", ["sort"] = sort, ["dir"] = dir,
        ["variante"] = Variante, ["incluirReserva"] = IncluirReserva?.ToString().ToLowerInvariant() ?? ""
    };
}

public class InventarioReporteScope
{
    public int PeriodoEmpresaSedeId { get; set; }
    public int PeriodoId { get; set; }
    public int EmpresaSedeId { get; set; }
    public int Anio { get; set; }
    public string Empresa { get; set; } = "";
    public string Sede { get; set; } = "";
    public bool Administrador { get; set; }
}

public class InventarioReporteModel : ListadoPaginadoModel
{
    public string Tipo { get; set; } = "";
    public string Titulo { get; set; } = "";
    public InventarioReporteFiltro Filtro { get; set; } = new();
    public InventarioReporteScope Contexto { get; set; } = new();
    public List<LookupItemModel> Almacenes { get; set; } = new();
    public List<LookupItemModel> Meses { get; set; } = new();
    public List<LookupItemModel> Transacciones { get; set; } = new();
    public List<string> Errores { get; set; } = new();
    public string ProductoNombre { get; set; } = "";
    public string ProveedorNombre { get; set; } = "";
    public bool Consultada { get; set; }
    public virtual string Ambito => Contexto.Sede;
    public string Resumen
    {
        get
        {
            var parts = new List<string?> { $"Ejercicio {Contexto.Anio}", Ambito,
                Filtro.Modo == "anual" ? "Todo el año" : Filtro.Modo == "dia" ? Filtro.Fecha?.ToString("dd/MM/yyyy") : Meses.FirstOrDefault(m => m.Id == Filtro.Mes)?.Nombre };
            if (Filtro.AlmacenId.HasValue) parts.Add(Almacenes.FirstOrDefault(a => a.Id == Filtro.AlmacenId)?.Nombre);
            if (Filtro.TransaccionId.HasValue) parts.Add(Transacciones.FirstOrDefault(t => t.Id == Filtro.TransaccionId)?.Nombre);
            if (Filtro.ProductoId.HasValue) parts.Add(ProductoNombre);
            if (Filtro.ProveedorId.HasValue) parts.Add(ProveedorNombre);
            if (!string.IsNullOrWhiteSpace(Filtro.Search)) parts.Add("Búsqueda: " + Filtro.Search);
            if (!string.IsNullOrWhiteSpace(Filtro.Codigo)) parts.Add("Código: " + Filtro.Codigo);
            if (!string.IsNullOrWhiteSpace(Filtro.Referencia)) parts.Add("Referencia: " + Filtro.Referencia);
            if (Tipo == "saldo-unidades") parts.Add(Filtro.Variante switch { "general" => "Consolidado general", "almacenes" => "Por almacén · " + (Filtro.IncluirReserva == true ? "descontando reservas" : "sin descontar reservas"), _ => "Detallado" });
            return string.Join(" · ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }
    }
}

public class SaldoConsultaPageModel : InventarioReporteModel
{
    public List<SaldoConsultaRowModel> Items { get; set; } = new();
}

public class SaldoConsultaRowModel
{
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Referencia { get; set; } = "";
    public string Unidad { get; set; } = "";
    public decimal Fisico { get; set; }
    public decimal Reserva { get; set; }
    public decimal Disponible { get; set; }
    public Dictionary<int, decimal> PorAlmacen { get; set; } = new();
}

public class SaldoAlmacenDetalleModel
{
    public string Almacen { get; set; } = "";
    public string Estado { get; set; } = "";
    public decimal Anterior { get; set; }
    public decimal Ingreso { get; set; }
    public decimal Salida { get; set; }
    public decimal Fisico { get; set; }
    public decimal Reserva { get; set; }
    public decimal Disponible { get; set; }
}

public class KardexUnidadPageModel : InventarioReporteModel
{
    public List<KardexUnidadRowModel> Items { get; set; } = new();
}

public class KardexUnidadRowModel
{
    public int Id { get; set; }
    public int MovimientoId { get; set; }
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = "";
    public string Producto { get; set; } = "";
    public string Unidad { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string Operacion { get; set; } = "";
    public string TipoMovimiento { get; set; } = "";
    public string Transaccion { get; set; } = "";
    public string Guia { get; set; } = "";
    public string Documento { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal Ingreso { get; set; }
    public decimal Salida { get; set; }
    public decimal Saldo { get; set; }
    public int OrdenRegistro { get; set; }
}

public class MovimientoReportePageModel : InventarioReporteModel
{
    public List<MovimientoReporteRowModel> Items { get; set; } = new();
}

public class MovimientoReporteRowModel
{
    public int Id { get; set; }
    public int MovimientoId { get; set; }
    public DateTime Fecha { get; set; }
    public string Operacion { get; set; } = "";
    public string Solicitante { get; set; } = "";
    public string TipoMovimiento { get; set; } = "";
    public string OrdenCompra { get; set; } = "";
    public string Tercero { get; set; } = "";
    public string Guia { get; set; } = "";
    public string Documento { get; set; } = "";
    public string Observacion { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string Producto { get; set; } = "";
    public string Unidad { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal Cantidad { get; set; }
    public decimal ValorUnitario { get; set; }
}

public class InventarioLookupPageModel : ListadoPaginadoModel
{
    public List<LookupItemModel> Items { get; set; } = new();
}

public class InventarioMovimientoDetalleModel
{
    public int Id { get; set; }
    public int AlmacenId { get; set; }
    public string Operacion { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string Almacen { get; set; } = "";
    public string Transaccion { get; set; } = "";
    public string Tercero { get; set; } = "";
    public string Guia { get; set; } = "";
    public string Documento { get; set; } = "";
    public string Observacion { get; set; } = "";
    public List<MovimientoReporteRowModel> Productos { get; set; } = new();
}

public class InventarioImpresionModel
{
    public string Titulo { get; set; } = "";
    public string Empresa { get; set; } = "";
    public string Resumen { get; set; } = "";
    public List<string> Errores { get; set; } = new();
    public List<string> Columnas { get; set; } = new();
    public List<List<string>> Filas { get; set; } = new();
}
