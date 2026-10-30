using System.Collections.Generic;

namespace AspNet10ArcoERP.Models
{
    public class LeadSourceModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Estado { get; set; } = "A";
    }

    public class LeadSourcePageModel : ListadoPaginadoModel
    {
        public List<LeadSourceModel> Items { get; set; } = new();
        public string Search { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }

    public class TipoProfesionModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Estado { get; set; } = "A";
    }

    public class TipoProfesionPageModel : ListadoPaginadoModel
    {
        public List<TipoProfesionModel> Items { get; set; } = new();
        public string Search { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }

    public class OportunidadVentaModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Estado { get; set; } = "A";
    }

    public class OportunidadVentaPageModel : ListadoPaginadoModel
    {
        public List<OportunidadVentaModel> Items { get; set; } = new();
        public string Search { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }

    public class TipoVentaClienteModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Estado { get; set; } = "A";
    }

    public class TipoVentaClientePageModel : ListadoPaginadoModel
    {
        public List<TipoVentaClienteModel> Items { get; set; } = new();
        public string Search { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }

    public class MerchandisingMovementModel
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Transaccion { get; set; } = string.Empty;
        public string TipoMovimiento { get; set; } = string.Empty;
        public string Origen { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
        public string Tercero { get; set; } = string.Empty;
        public string Ruc { get; set; } = string.Empty;
        public string EstadoMovimiento { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
    }

    public class MerchandisingMovementLineModel
    {
        public int Id { get; set; }
        public string CodigoProducto { get; set; } = string.Empty;
        public string Producto { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal Total { get; set; }
    }

    public class MerchandisingMovementDetailModel
    {
        public MerchandisingMovementModel Movimiento { get; set; } = new();
        public List<MerchandisingMovementLineModel> Detalle { get; set; } = new();
    }

    public class MerchandisingMovementPageModel : ListadoPaginadoModel
    {
        public List<MerchandisingMovementModel> Items { get; set; } = new();
        public List<LookupItemModel> Transacciones { get; set; } = new();
        public string Search { get; set; } = string.Empty;
        public int? TransaccionId { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
    }
}
