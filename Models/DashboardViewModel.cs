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
        public string Vendedor { get; set; } = string.Empty;
        public string NumeroDocumentoIdentidad { get; set; } = string.Empty;
        public DateTime FechaUltimaCompra { get; set; }
        public string FechaFormateada { get; set; } = string.Empty;
        public int MesesSinComprar { get; set; }
    }

    public class VendedorDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    public class RolDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
    }

    public class DashboardViewModel
    {
        // KPIs Principales
        public string FacturacionMes { get; set; } = "S/ 0.00";
        public string PedidosActivos { get; set; } = "0";
        public string CotizacionesActivas { get; set; } = "0";
        public string OrdenesProduccionMes { get; set; } = "0";
        public string OrdenesCompraMes { get; set; } = "0";
        public string PedidosMesMonto { get; set; } = "S/ 0.00";
        public int PedidosMesCantidad { get; set; } = 0;

        // Evolución de Ventas (12 meses móviles)
        public string[] MesesVentas { get; set; } = Array.Empty<string>();
        public double[] VentasAnterior { get; set; } = Array.Empty<double>();
        public double[] VentasActual { get; set; } = Array.Empty<double>();
        public double?[] VentasBarraActual { get; set; } = Array.Empty<double?>();

        // Distribución por Sede (Central vs Ilo)
        public List<string> StackedLabels { get; set; } = new();
        public double[] DsCentral { get; set; } = Array.Empty<double>();
        public double[] DsIlo { get; set; } = Array.Empty<double>();

        // Desempeño de Vendedores
        public List<VendedorDto> ListaVendedores { get; set; } = new();
        public int PrimerVendedorId { get; set; } = 50;
        public string PrimerVendedorNombre { get; set; } = "CRISTIAN";
        public List<int> VendedorCantidades { get; set; } = new();
        public List<double> VendedorMontos { get; set; } = new();

        // Estado de Producción
        public string[] ProdLabels { get; set; } = new[] { "En Proceso", "Culminadas", "Pendientes", "Anuladas" };
        public int[] ProdValores { get; set; } = new[] { 0, 0, 0, 0 };

        // Tablas de detalle
        public List<TopPedidoDto> TopPedidos { get; set; } = new();
        public string TopPedidosFecha { get; set; } = DateTime.Today.ToString("dd/MM/yyyy");
        public List<ClienteInactivoDto> ClientesInactivos { get; set; } = new();

        // Permisos y Roles
        public List<RolDto> ListaRoles { get; set; } = new();
        public bool EsAdmin { get; set; } = true;
        public bool PuedeFiltrarVendedorClientes { get; set; } = true;
        public string RolActualCodigo { get; set; } = "S0001";
    }
}
