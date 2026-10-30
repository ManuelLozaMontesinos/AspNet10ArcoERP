namespace AspNet10ArcoERP.Models;

public class SaldoUnidadesPageModel : InventarioReporteModel
{
    public List<SaldoUnidadesRowModel> Items { get; set; } = new();
    public List<LookupItemModel> ColumnasAlmacen { get; set; } = new();
    public override string Ambito => Filtro.Variante == "detalle" ? "Almacén seleccionado" : "Todas las sedes de la empresa";
}

public class SaldoUnidadesRowModel
{
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = "";
    public string Producto { get; set; } = "";
    public string Unidad { get; set; } = "";
    public decimal Anterior { get; set; }
    public decimal Ingreso { get; set; }
    public decimal Salida { get; set; }
    public decimal Reserva { get; set; }
    public decimal Saldo { get; set; }
    public Dictionary<int, decimal> PorAlmacen { get; set; } = new();
}

public class SaldoValoradoPageModel : InventarioReporteModel
{
    public List<SaldoValoradoRowModel> Items { get; set; } = new();
    public override string Ambito => "Consolidado de la empresa · Todas las sedes";
}

public class SaldoValoradoRowModel
{
    public int Id { get; set; }
    public string Sede { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string Producto { get; set; } = "";
    public string Unidad { get; set; } = "";
    public decimal Anterior { get; set; }
    public decimal ValorUnitarioAnterior { get; set; }
    public decimal Ingreso { get; set; }
    public decimal Salida { get; set; }
    public decimal Reserva { get; set; }
    public decimal Saldo { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal Importe { get; set; }
}
