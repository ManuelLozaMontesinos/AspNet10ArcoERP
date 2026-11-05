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

    public class ProductoStockDto
    {
        public int Orden { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public int ProductoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal StockActual { get; set; }
        public decimal ConsumoMes { get; set; }
        public string StockActualFormateado { get; set; } = string.Empty;
        public string ConsumoMesFormateado { get; set; } = string.Empty;
        public string EstadoStock { get; set; } = string.Empty;
        public string BadgeColor { get; set; } = "secondary";
    }

    public class AlmacenItemDto
    {
        public int AlmacenId { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    public class TopMenorStockDto
    {
        public int Orden { get; set; }
        public int ProductoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public string CantidadFormateada { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public string GrupoNombre { get; set; } = string.Empty;
        public string ClaseNombre { get; set; } = string.Empty;
        public string FamiliaNombre { get; set; } = string.Empty;
        public string CategoriaCompleta
        {
            get
            {
                var partes = new List<string>();
                if (!string.IsNullOrWhiteSpace(GrupoNombre)) partes.Add(GrupoNombre);
                if (!string.IsNullOrWhiteSpace(ClaseNombre)) partes.Add(ClaseNombre);
                if (!string.IsNullOrWhiteSpace(FamiliaNombre)) partes.Add(FamiliaNombre);
                return partes.Count > 0 ? string.Join(" / ", partes) : "-";
            }
        }
        public string EstadoStock { get; set; } = string.Empty;
        public string BadgeColor { get; set; } = "danger";
    }

    public class CategoriaFiltroItemDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    public class CalendarioAreaDto
    {
        public int AreaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Color { get; set; } = "#0d6efd";
    }

    public class CalendarioEventoDto
    {
        public int EventoId { get; set; }
        public int AreaId { get; set; }
        public string AreaNombre { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Contexto { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public bool TodoElDia { get; set; }
        public string Color { get; set; } = "#0d6efd";
        public string UsuarioLogon { get; set; } = string.Empty;
        public string FechaInicioFormateada => FechaInicio.ToString("dd/MM/yyyy HH:mm");
    }

    public class DashboardViewModel
    {
        // 1. KPIs Principales
        public string FacturacionMes { get; set; } = "S/ 0.00";
        public string PedidosActivos { get; set; } = "0";
        public string CotizacionesActivas { get; set; } = "0";
        public string OrdenesProduccionMes { get; set; } = "0";
        public string OrdenesCompraMes { get; set; } = "0";
        public string PedidosMesMonto { get; set; } = "S/ 0.00";
        public int PedidosMesCantidad { get; set; } = 0;
        public int ConsumoInternoMes { get; set; } = 0;
        public int IngresosMermaMes { get; set; } = 0;
        public int SalidasMermaMes { get; set; } = 0;
        public int GuiasRemisionMes { get; set; } = 0;

        // 2. Evolución de Ventas (12 meses móviles)
        public string[] MesesVentas { get; set; } = Array.Empty<string>();
        public double[] VentasAnterior { get; set; } = Array.Empty<double>();
        public double[] VentasActual { get; set; } = Array.Empty<double>();
        public double?[] VentasBarraActual { get; set; } = Array.Empty<double?>();

        // 3. Distribución por Sede (Central vs Ilo)
        public List<string> StackedLabels { get; set; } = new();
        public double[] DsCentral { get; set; } = Array.Empty<double>();
        public double[] DsIlo { get; set; } = Array.Empty<double>();

        // 4. Desempeño de Vendedores
        public List<VendedorDto> ListaVendedores { get; set; } = new();
        public int PrimerVendedorId { get; set; } = 50;
        public string PrimerVendedorNombre { get; set; } = "CRISTIAN";
        public List<int> VendedorCantidades { get; set; } = new();
        public List<double> VendedorMontos { get; set; } = new();

        // 4.1 Cotizaciones por Vendedor
        public int PrimerCotizacionVendedorId { get; set; } = 0;
        public string PrimerCotizacionVendedorNombre { get; set; } = "Todos los vendedores";
        public List<int> CotizacionCantidades { get; set; } = new();
        public List<double> CotizacionMontos { get; set; } = new();

        // 5. Estado de Producción
        public string[] ProdLabels { get; set; } = new[] { "En Proceso", "Culminadas", "Pendientes", "Anuladas" };
        public int[] ProdValores { get; set; } = new[] { 0, 0, 0, 0 };

        // 6. Tablas de Detalle
        public List<TopPedidoDto> TopPedidos { get; set; } = new();
        public string TopPedidosFecha { get; set; } = DateTime.Today.ToString("dd/MM/yyyy");
        public List<ClienteInactivoDto> ClientesInactivos { get; set; } = new();

        // 7. Stock de Insumos Críticos
        public List<ProductoStockDto> ListaStockInsumos { get; set; } = new();
        public List<AlmacenItemDto> ListaAlmacenesStock { get; set; } = new();
        public string MesActualStock { get; set; } = DateTime.Today.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-ES"));

        // 8. Top Menor Stock
        public List<TopMenorStockDto> ListaTopMenorStock { get; set; } = new();
        public List<CategoriaFiltroItemDto> ListaGruposProducto { get; set; } = new();

        // 9. Agenda / Calendario de Recordatorios
        public List<CalendarioAreaDto> AreasAgenda { get; set; } = new();
        public List<CalendarioEventoDto> EventosAgenda { get; set; } = new();
        public bool TienePermisoCalendario { get; set; } = true;
        public int AreaAgendaInicialId { get; set; } = 1;
        public string AreaAgendaInicialNombre { get; set; } = "Administración";

        // 10. Permisos y Configuración de Roles
        public List<RolDto> ListaRoles { get; set; } = new();
        public bool EsAdmin { get; set; } = true;
        public bool PuedeFiltrarVendedorClientes { get; set; } = true;
        public string RolActualCodigo { get; set; } = "S0001";
        public string DashboardRolesConfigJson { get; set; } = "{}";
    }

    public class RolConfigModel
    {
        public string RolCodigo { get; set; } = string.Empty;
        public string ConfigJson { get; set; } = string.Empty;
    }
}
