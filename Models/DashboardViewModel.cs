using System;
using System.Collections.Generic;

namespace AspNet10ArcoERP.Models
{
    public class TopPedidoDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Cliente { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string MontoFormateado { get; set; } = string.Empty;
    }

    public class ClienteInactivoDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Cliente { get; set; } = string.Empty;
        public DateTime FechaUltimaCompra { get; set; }
        public string FechaFormateada { get; set; } = string.Empty;
        public int MesesSinComprar { get; set; }
    }

    public class DashboardViewModel
    {
        public string FacturacionMes { get; set; } = "S/ 0.00";
        public string PedidosActivos { get; set; } = "169";
        public string CotizacionesActivas { get; set; } = "7,637";
        public string OrdenesProduccionMes { get; set; } = "0";
        public string OrdenesCompraMes { get; set; } = "0";

        public string[] MesesVentas { get; set; } = Array.Empty<string>();
        public double[] Ventas2025 { get; set; } = Array.Empty<double>();
        public double[] Ventas2026 { get; set; } = Array.Empty<double>();

        public List<string> StackedLabels { get; set; } = new();
        public double[] DsCentral { get; set; } = Array.Empty<double>();
        public double[] DsIlo { get; set; } = Array.Empty<double>();

        public string[] ProdLabels { get; set; } = Array.Empty<string>();
        public int[] ProdValores { get; set; } = Array.Empty<int>();

        public List<TopPedidoDto> TopPedidos { get; set; } = new();
        public List<ClienteInactivoDto> ClientesInactivos { get; set; } = new();
    }
}
