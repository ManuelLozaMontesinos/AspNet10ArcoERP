using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        private const string SqlTipoCambioApply = @"
            OUTER APPLY (
                SELECT TOP 1 tcd.monto_v 
                FROM tipo_camb_dia tcd 
                WHERE c.moneda_id = 2 AND ISNULL(c.monto_tc, 0) <= 0 AND tcd.fecha <= c.fecha AND tcd.moneda_id = 2 AND tcd.estado = 'A' 
                ORDER BY tcd.fecha DESC
            ) tc";

        private const string SqlMontoEnSoles = @"(CASE WHEN c.moneda_id = 2 THEN c.monto_total * (CASE WHEN ISNULL(c.monto_tc, 0) > 0 THEN c.monto_tc ELSE ISNULL(tc.monto_v, 3.50) END) ELSE c.monto_total END)";

        private const string ClientesInactivosSql = @"
            ;WITH Movimientos AS (
                SELECT proveedor_id, fecha, vendedor_id, 1 AS origen, comprobante_id AS registro_id
                FROM comprobante
                WHERE estado = 'A'
                  AND (@sede IS NULL OR @sede = 0 OR periodo_emp_sede_id IN (SELECT pes.periodo_emp_sede_id FROM periodo_emp_sede pes WHERE pes.empresa_sede_id = @sede))
                UNION ALL
                SELECT proveedor_id, fecha, vendedor_id, 0 AS origen, cotiza_id AS registro_id
                FROM cotiza
                WHERE es_pedido = 'S' AND estado = 'A'
                  AND (@sede IS NULL OR @sede = 0 OR periodo_emp_sede_id IN (SELECT pes.periodo_emp_sede_id FROM periodo_emp_sede pes WHERE pes.empresa_sede_id = @sede))
            ),
            UltimaCompra AS (
                SELECT proveedor_id, fecha, vendedor_id,
                       ROW_NUMBER() OVER (PARTITION BY proveedor_id ORDER BY fecha DESC, origen DESC, registro_id DESC) AS rn
                FROM Movimientos
            )
            SELECT TOP 10
                p.proveedor_id AS Id,
                p.proveedor AS Codigo,
                p.num_doc_iden AS NumeroDocumentoIdentidad,
                ISNULL(NULLIF(RTRIM(LTRIM(p.razon_social)), ''), ISNULL(RTRIM(LTRIM(p.nombres + ' ' + ISNULL(p.apellido_pat, ''))), 'Sin Nombre')) AS Cliente,
                COALESCE(NULLIF(RTRIM(LTRIM(vp.razon_social)), ''),
                         NULLIF(RTRIM(LTRIM(ISNULL(vp.nombres, '') + ' ' + ISNULL(vp.apellido_pat, ''))), ''),
                         NULLIF(RTRIM(LTRIM(v.abrev)), ''), 'Sin vendedor') AS Vendedor,
                u.fecha AS FechaUltimaCompra,
                DATEDIFF(month, u.fecha, GETDATE()) AS MesesSinComprar
            FROM UltimaCompra u
            INNER JOIN proveedor p ON u.proveedor_id = p.proveedor_id
            LEFT JOIN vendedor v ON u.vendedor_id = v.vendedor_id
            LEFT JOIN proveedor vp ON v.proveedor_id = vp.proveedor_id
            WHERE u.rn = 1
              AND (@vid IS NULL OR u.vendedor_id = @vid)
              AND u.fecha <= DATEADD(month, -@meses, GETDATE())
            ORDER BY u.fecha DESC";

        public DashboardService(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<DashboardViewModel> GetDashboardMetricsAsync(int? vendedorIdFiltro = null, int mesesInactivos = 5, int? vendedorClientesFiltro = null, bool esAdmin = true, int? sedeId = null, int? anio = null)
        {
            var vm = new DashboardViewModel
            {
                EsAdmin = esAdmin,
                PuedeFiltrarVendedorClientes = esAdmin
            };

            int anioConsulta = anio ?? DateTime.Today.Year;
            int mesConsulta = DateTime.Today.Month;
            DateTime hoy = DateTime.Today;

            using var conn = _dbConnectionFactory.CreateConnection();

            // 1. KPIs Generales
            decimal facturacionMes = 0m;
            int pedidosActivos = 0;
            int cotizacionesActivas = 0;
            int ordenesProduccionMes = 0;
            int ordenesCompraMes = 0;
            double sumatoriaPedidosMes = 0;
            int cantidadPedidosMes = 0;
            int consumoInternoMes = 0;
            int ingresosMermaMes = 0;
            int salidasMermaMes = 0;
            int guiasRemisionMes = 0;

            try
            {
                // Facturacion
                facturacionMes = await conn.ExecuteScalarAsync<decimal>($@"
                    SELECT ISNULL(SUM({SqlMontoEnSoles}), 0)
                    FROM comprobante c
                    INNER JOIN estado_compro ec ON c.estado_compro_id = ec.estado_compro_id
                    LEFT JOIN periodo_emp_sede pes ON c.periodo_emp_sede_id = pes.periodo_emp_sede_id
                    {SqlTipoCambioApply}
                    WHERE YEAR(c.fecha) = @Anio 
                      AND MONTH(c.fecha) = @Mes 
                      AND c.estado = 'A'
                      AND (ec.estado_compro = '3' OR UPPER(ec.nombre) LIKE '%EMITID%' OR c.estado_compro_id = 3)
                      AND (@Vid IS NULL OR c.vendedor_id = @Vid)
                      AND (@Sede IS NULL OR @Sede = 0 OR pes.empresa_sede_id = @Sede)",
                    new { Anio = anioConsulta, Mes = mesConsulta, Vid = vendedorIdFiltro, Sede = sedeId });
            }
            catch { }

            try
            {
                // Pedidos activos
                pedidosActivos = await conn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*)
                    FROM cotiza c
                    LEFT JOIN periodo_emp_sede pes ON c.periodo_emp_sede_id = pes.periodo_emp_sede_id
                    WHERE c.es_pedido = 'S' AND c.estado_cotiza_id IN (2, 4) AND c.estado = 'A'
                      AND (@Anio IS NULL OR @Anio = 0 OR YEAR(c.fecha) = @Anio)
                      AND (@Vid IS NULL OR c.vendedor_id = @Vid)
                      AND (@Sede IS NULL OR @Sede = 0 OR pes.empresa_sede_id = @Sede)",
                    new { Anio = anioConsulta, Vid = vendedorIdFiltro, Sede = sedeId });
            }
            catch { }

            try
            {
                // Cotizaciones activas
                cotizacionesActivas = await conn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*)
                    FROM cotiza c
                    LEFT JOIN periodo_emp_sede pes ON c.periodo_emp_sede_id = pes.periodo_emp_sede_id
                    WHERE c.es_pedido = 'N' AND c.estado_cotiza_id = 1 AND c.estado = 'A'
                      AND (@Anio IS NULL OR @Anio = 0 OR YEAR(c.fecha) = @Anio)
                      AND (@Vid IS NULL OR c.vendedor_id = @Vid)
                      AND (@Sede IS NULL OR @Sede = 0 OR pes.empresa_sede_id = @Sede)",
                    new { Anio = anioConsulta, Vid = vendedorIdFiltro, Sede = sedeId });
            }
            catch { }

            try
            {
                // Ordenes produccion
                ordenesProduccionMes = await conn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*)
                    FROM orden_pro o
                    LEFT JOIN cotiza c ON c.cotiza_id = o.cotiza_id
                    LEFT JOIN periodo_emp_sede pes ON o.periodo_emp_sede_id = pes.periodo_emp_sede_id
                    WHERE YEAR(o.fecha) = @Anio AND MONTH(o.fecha) = @Mes
                      AND ISNULL(o.estado, 'A') <> 'I' AND ISNULL(o.estado_orden_p_id, 0) <> 4
                      AND (@Vid IS NULL OR c.vendedor_id = @Vid)
                      AND (@Sede IS NULL OR @Sede = 0 OR pes.empresa_sede_id = @Sede)",
                    new { Anio = anioConsulta, Mes = mesConsulta, Vid = vendedorIdFiltro, Sede = sedeId });
            }
            catch { }

            try
            {
                // Ordenes compra
                ordenesCompraMes = await conn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*)
                    FROM orden_co o
                    LEFT JOIN periodo_emp_sede pes ON o.periodo_emp_sede_id = pes.periodo_emp_sede_id
                    WHERE YEAR(o.fecha) = @Anio AND MONTH(o.fecha) = @Mes AND o.estado = 'A'
                      AND (@Sede IS NULL OR @Sede = 0 OR pes.empresa_sede_id = @Sede)",
                    new { Anio = anioConsulta, Mes = mesConsulta, Sede = sedeId });
            }
            catch { }

            try
            {
                // Sumatoria y cantidad de pedidos del mes
                var pedStats = await conn.QueryFirstOrDefaultAsync<dynamic>($@"
                    SELECT 
                        ROUND(ISNULL(SUM({SqlMontoEnSoles}), 0), 2) AS Sumatoria,
                        COUNT(*) AS Cantidad
                    FROM cotiza c
                    LEFT JOIN periodo_emp_sede pes ON c.periodo_emp_sede_id = pes.periodo_emp_sede_id
                    {SqlTipoCambioApply}
                    WHERE c.es_pedido = 'S' AND c.estado = 'A' AND c.estado_cotiza_id IN (2, 3, 4)
                      AND YEAR(c.fecha) = @Anio AND MONTH(c.fecha) = @Mes
                      AND (@Vid IS NULL OR c.vendedor_id = @Vid)
                      AND (@Sede IS NULL OR @Sede = 0 OR pes.empresa_sede_id = @Sede)",
                    new { Anio = anioConsulta, Mes = mesConsulta, Vid = vendedorIdFiltro, Sede = sedeId });

                if (pedStats != null)
                {
                    sumatoriaPedidosMes = Convert.ToDouble(pedStats.Sumatoria ?? 0);
                    cantidadPedidosMes = Convert.ToInt32(pedStats.Cantidad ?? 0);
                }
            }
            catch { }

            try
            {
                // Consumo interno y mermas
                var movStats = await conn.QueryAsync<dynamic>(@"
                    SELECT m.trans_alma_id, COUNT(*) AS Cantidad
                    FROM mov_almacen m
                    LEFT JOIN periodo_emp_sede pes ON m.periodo_emp_sede_id = pes.periodo_emp_sede_id
                    WHERE YEAR(m.fecha) = @Anio AND MONTH(m.fecha) = @Mes AND m.estado = 'A'
                      AND ISNULL(m.estado_mov_alm_id, 0) <> 3 AND m.estado_aproba_id = 2
                      AND m.trans_alma_id IN (7, 8, 9)
                      AND (@Sede IS NULL OR @Sede = 0 OR pes.empresa_sede_id = @Sede)
                    GROUP BY m.trans_alma_id",
                    new { Anio = anioConsulta, Mes = mesConsulta, Sede = sedeId });

                foreach (var row in movStats)
                {
                    int tid = Convert.ToInt32(row.trans_alma_id);
                    int cant = Convert.ToInt32(row.Cantidad);
                    if (tid == 9) consumoInternoMes = cant;
                    else if (tid == 7) ingresosMermaMes = cant;
                    else if (tid == 8) salidasMermaMes = cant;
                }
            }
            catch { }

            try
            {
                // Guias de remision emitidas
                guiasRemisionMes = await conn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*)
                    FROM guia_remision g
                    LEFT JOIN periodo_emp_sede pes ON g.periodo_emp_sede_id = pes.periodo_emp_sede_id
                    WHERE YEAR(g.fecha) = @Anio AND MONTH(g.fecha) = @Mes AND g.estado = 'A'
                      AND g.estado_guia_r_id = 2
                      AND (@Sede IS NULL OR @Sede = 0 OR pes.empresa_sede_id = @Sede)",
                    new { Anio = anioConsulta, Mes = mesConsulta, Sede = sedeId });
            }
            catch { }

            vm.FacturacionMes = "S/ " + facturacionMes.ToString("N2", CultureInfo.InvariantCulture);
            vm.PedidosActivos = pedidosActivos.ToString();
            vm.CotizacionesActivas = cotizacionesActivas.ToString();
            vm.OrdenesProduccionMes = ordenesProduccionMes.ToString();
            vm.OrdenesCompraMes = ordenesCompraMes.ToString();
            vm.PedidosMesMonto = "S/ " + sumatoriaPedidosMes.ToString("N2", CultureInfo.InvariantCulture);
            vm.PedidosMesCantidad = cantidadPedidosMes;
            vm.ConsumoInternoMes = consumoInternoMes;
            vm.IngresosMermaMes = ingresosMermaMes;
            vm.SalidasMermaMes = salidasMermaMes;
            vm.GuiasRemisionMes = guiasRemisionMes;

            // 2. Grafico de Evolucion de Ventas (12 meses)
            string[] nombresMeses = { "", "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Set", "Oct", "Nov", "Dic" };
            DateTime inicioMesActual = new DateTime(hoy.Year, hoy.Month, 1);
            DateTime inicioPeriodoActual = inicioMesActual.AddMonths(-11);

            vm.MesesVentas = Enumerable.Range(0, 12)
                .Select(i => nombresMeses[inicioPeriodoActual.AddMonths(i).Month])
                .ToArray();
            vm.VentasAnterior = new double[12];
            vm.VentasActual = new double[12];
            vm.VentasBarraActual = new double?[12];

            try
            {
                var ventasMesesRows = await conn.QueryAsync<dynamic>($@"
                    SELECT 
                        YEAR(c.fecha) AS Anio,
                        MONTH(c.fecha) AS Mes,
                        ROUND(ISNULL(SUM({SqlMontoEnSoles}), 0) / 1000.0, 1) AS TotalMiles
                    FROM comprobante c
                    INNER JOIN estado_compro ec ON c.estado_compro_id = ec.estado_compro_id
                    LEFT JOIN periodo_emp_sede pes ON c.periodo_emp_sede_id = pes.periodo_emp_sede_id
                    {SqlTipoCambioApply}
                    WHERE c.fecha >= @FechaDesde AND c.fecha < @FechaHasta
                      AND c.estado = 'A'
                      AND (ec.estado_compro = '3' OR UPPER(ec.nombre) LIKE '%EMITID%' OR c.estado_compro_id = 3)
                      AND (@Vid IS NULL OR c.vendedor_id = @Vid)
                      AND (@Sede IS NULL OR @Sede = 0 OR pes.empresa_sede_id = @Sede)
                    GROUP BY YEAR(c.fecha), MONTH(c.fecha)",
                    new { 
                        FechaDesde = inicioPeriodoActual.AddYears(-1), 
                        FechaHasta = inicioMesActual.AddMonths(1),
                        Vid = vendedorIdFiltro,
                        Sede = sedeId
                    });

                var dictVentas = new Dictionary<string, double>();
                foreach (var r in ventasMesesRows)
                {
                    string k = $"{(int)(r.Anio ?? 0)}-{(int)(r.Mes ?? 0)}";
                    dictVentas[k] = Convert.ToDouble(r.TotalMiles ?? 0);
                }

                for (int i = 0; i < 12; i++)
                {
                    DateTime mesRef = inicioPeriodoActual.AddMonths(i);
                    string keyAct = $"{mesRef.Year}-{mesRef.Month}";
                    string keyAnt = $"{mesRef.Year - 1}-{mesRef.Month}";

                    double vAct = dictVentas.TryGetValue(keyAct, out double va) ? va : 0;
                    double vAnt = dictVentas.TryGetValue(keyAnt, out double van) ? van : 0;

                    vm.VentasActual[i] = vAct;
                    vm.VentasBarraActual[i] = vAct;
                    vm.VentasAnterior[i] = vAnt;
                }
            }
            catch { }

            // 3. Distribución por Sede (Últimos 12 días)
            var stackedDias = new List<string>();
            var fechasLista = new List<DateTime>();
            for (int i = 11; i >= 0; i--)
            {
                DateTime f = hoy.AddDays(-i).Date;
                fechasLista.Add(f);
                stackedDias.Add(f.Day + " " + nombresMeses[f.Month]);
            }
            vm.StackedLabels = stackedDias;
            vm.DsCentral = new double[12];
            vm.DsIlo = new double[12];

            try
            {
                var sedeRows = await conn.QueryAsync<dynamic>($@"
                    SELECT 
                        CAST(c.fecha AS date) AS Dia,
                        pes.empresa_sede_id AS SedeId,
                        ROUND(ISNULL(SUM({SqlMontoEnSoles}), 0) / 1000.0, 1) AS TotalMiles
                    FROM comprobante c
                    INNER JOIN estado_compro ec ON c.estado_compro_id = ec.estado_compro_id
                    LEFT JOIN periodo_emp_sede pes ON c.periodo_emp_sede_id = pes.periodo_emp_sede_id
                    {SqlTipoCambioApply}
                    WHERE c.fecha >= @FechaDesde AND c.fecha < @FechaHasta
                      AND c.estado = 'A'
                      AND (ec.estado_compro = '3' OR UPPER(ec.nombre) LIKE '%EMITID%' OR c.estado_compro_id = 3)
                      AND (@Vid IS NULL OR c.vendedor_id = @Vid)
                    GROUP BY CAST(c.fecha AS date), pes.empresa_sede_id",
                    new { FechaDesde = hoy.AddDays(-11).Date, FechaHasta = hoy.AddDays(1).Date, Vid = vendedorIdFiltro });

                var dictSedes = new Dictionary<string, double>();
                foreach (var r in sedeRows)
                {
                    string k = $"{(DateTime)r.Dia:yyyy-MM-dd}-{(int)(r.SedeId ?? 1)}";
                    dictSedes[k] = Convert.ToDouble(r.TotalMiles ?? 0);
                }

                for (int i = 0; i < fechasLista.Count; i++)
                {
                    string fStr = fechasLista[i].ToString("yyyy-MM-dd");
                    vm.DsCentral[i] = dictSedes.TryGetValue($"{fStr}-1", out double c) ? c : 0;
                    vm.DsIlo[i] = dictSedes.TryGetValue($"{fStr}-2", out double il) ? il : 0;
                }
            }
            catch { }

            // 4. Vendedores
            try
            {
                var vendedores = await conn.QueryAsync<VendedorDto>(@"
                    SELECT DISTINCT v.vendedor_id AS Id, 
                           COALESCE(NULLIF(RTRIM(LTRIM(p.razon_social)), ''), 
                                    NULLIF(RTRIM(LTRIM(ISNULL(p.nombres, '') + ' ' + ISNULL(p.apellido_pat, ''))), ''), 
                                    v.abrev, 'Vendedor ' + CAST(v.vendedor_id AS VARCHAR)) AS Nombre
                    FROM vendedor v
                    LEFT JOIN proveedor p ON v.proveedor_id = p.proveedor_id
                    WHERE v.estado = 'A'
                    ORDER BY Nombre");
                vm.ListaVendedores = vendedores.ToList();
            }
            catch { }

            // Seleccionar primer vendedor por defecto
            int vidDefecto = vendedorIdFiltro ?? (vm.ListaVendedores.FirstOrDefault()?.Id ?? 50);
            vm.PrimerVendedorId = vidDefecto;
            vm.PrimerVendedorNombre = vm.ListaVendedores.FirstOrDefault(v => v.Id == vidDefecto)?.Nombre ?? "CRISTIAN";

            var (vLabels, vCants, vMontos, _, _) = await GetPedidosVendedorAsync(vidDefecto, 12);
            vm.VendedorCantidades = vCants;
            vm.VendedorMontos = vMontos;

            // 4.1 Cotizaciones por Vendedor inicial
            int cotizVidDefecto = 0; // Por defecto consolidado (Todos los vendedores)
            vm.PrimerCotizacionVendedorId = cotizVidDefecto;
            vm.PrimerCotizacionVendedorNombre = "Todos los vendedores";
            var (_, cotizCants, cotizMontos, _, _) = await GetCotizacionesVendedorAsync(cotizVidDefecto, 12, anioConsulta);
            vm.CotizacionCantidades = cotizCants;
            vm.CotizacionMontos = cotizMontos;

            // 5. Estado de Producción
            try
            {
                var prodRows = await conn.QueryAsync<dynamic>(@"
                    SELECT o.estado_orden_p_id, COUNT(*) AS Cantidad
                    FROM orden_pro o
                    WHERE YEAR(o.fecha) = @Anio AND MONTH(o.fecha) = @Mes AND ISNULL(o.estado, 'A') <> 'I'
                    GROUP BY o.estado_orden_p_id",
                    new { Anio = anioConsulta, Mes = mesConsulta });

                int enProceso = 0, culminadas = 0, pendientes = 0, anuladas = 0;
                foreach (var row in prodRows)
                {
                    int st = Convert.ToInt32(row.estado_orden_p_id);
                    int cnt = Convert.ToInt32(row.Cantidad);
                    if (st == 1) pendientes += cnt;
                    else if (st == 2) enProceso += cnt;
                    else if (st == 3) culminadas += cnt;
                    else if (st == 4) anuladas += cnt;
                }
                vm.ProdLabels = new[] { "En Proceso", "Culminadas", "Pendientes", "Anuladas" };
                vm.ProdValores = new[] { enProceso, culminadas, pendientes, anuladas };
            }
            catch { }

            // 6. Top Pedidos del Día
            try
            {
                DateTime fechaTop = hoy;
                var topRows = await conn.QueryAsync<dynamic>($@"
                    SELECT TOP 10 
                        c.cotiza_id AS Id,
                        c.cotiza AS Codigo,
                        ISNULL(NULLIF(RTRIM(LTRIM(p.razon_social)), ''), ISNULL(RTRIM(LTRIM(p.nombres + ' ' + ISNULL(p.apellido_pat, ''))), 'Sin Nombre')) AS Cliente,
                        ROUND(ISNULL({SqlMontoEnSoles}, 0), 2) AS Monto
                    FROM cotiza c
                    INNER JOIN proveedor p ON c.proveedor_id = p.proveedor_id
                    LEFT JOIN periodo_emp_sede pes ON c.periodo_emp_sede_id = pes.periodo_emp_sede_id
                    {SqlTipoCambioApply}
                    WHERE c.es_pedido = 'S' AND c.estado = 'A' AND CAST(c.fecha AS date) = @Fecha
                      AND (@Vid IS NULL OR c.vendedor_id = @Vid)
                      AND (@Sede IS NULL OR @Sede = 0 OR pes.empresa_sede_id = @Sede)
                    ORDER BY Monto DESC",
                    new { Fecha = fechaTop.Date, Vid = vendedorIdFiltro, Sede = sedeId });

                var topList = new List<TopPedidoDto>();
                foreach (var r in topRows)
                {
                    decimal m = Convert.ToDecimal(r.Monto ?? 0);
                    topList.Add(new TopPedidoDto
                    {
                        Id = (int)r.Id,
                        Codigo = (string)r.Codigo ?? "",
                        Cliente = (string)r.Cliente ?? "",
                        Monto = m,
                        MontoFormateado = "S/ " + m.ToString("N2", CultureInfo.InvariantCulture)
                    });
                }

                // Si no hay pedidos hoy, buscar la fecha más reciente con pedidos
                if (topList.Count == 0)
                {
                    var ultimaFecha = await conn.ExecuteScalarAsync<DateTime?>(@"
                        SELECT MAX(CAST(c.fecha AS date))
                        FROM cotiza c
                        WHERE c.es_pedido = 'S' AND c.estado = 'A'");

                    if (ultimaFecha.HasValue)
                    {
                        fechaTop = ultimaFecha.Value;
                        var topRowsRecientes = await conn.QueryAsync<dynamic>($@"
                            SELECT TOP 10 
                                c.cotiza_id AS Id,
                                c.cotiza AS Codigo,
                                ISNULL(NULLIF(RTRIM(LTRIM(p.razon_social)), ''), ISNULL(RTRIM(LTRIM(p.nombres + ' ' + ISNULL(p.apellido_pat, ''))), 'Sin Nombre')) AS Cliente,
                                ROUND(ISNULL({SqlMontoEnSoles}, 0), 2) AS Monto
                            FROM cotiza c
                            INNER JOIN proveedor p ON c.proveedor_id = p.proveedor_id
                            LEFT JOIN periodo_emp_sede pes ON c.periodo_emp_sede_id = pes.periodo_emp_sede_id
                            {SqlTipoCambioApply}
                            WHERE c.es_pedido = 'S' AND c.estado = 'A' AND CAST(c.fecha AS date) = @Fecha
                              AND (@Vid IS NULL OR c.vendedor_id = @Vid)
                              AND (@Sede IS NULL OR @Sede = 0 OR pes.empresa_sede_id = @Sede)
                            ORDER BY Monto DESC",
                            new { Fecha = fechaTop.Date, Vid = vendedorIdFiltro, Sede = sedeId });

                        foreach (var r in topRowsRecientes)
                        {
                            decimal m = Convert.ToDecimal(r.Monto ?? 0);
                            topList.Add(new TopPedidoDto
                            {
                                Id = (int)r.Id,
                                Codigo = (string)r.Codigo ?? "",
                                Cliente = (string)r.Cliente ?? "",
                                Monto = m,
                                MontoFormateado = "S/ " + m.ToString("N2", CultureInfo.InvariantCulture)
                            });
                        }
                    }
                }

                vm.TopPedidos = topList;
                vm.TopPedidosFecha = fechaTop.ToString("dd/MM/yyyy");
            }
            catch { }

            // 7. Clientes Inactivos
            vm.ClientesInactivos = await GetClientesInactivosAsync(mesesInactivos, vendedorClientesFiltro, sedeId);

            // 8. Stock Insumos Críticos
            vm.ListaStockInsumos = await GetStockInsumosAsync(0, sedeId);
            vm.ListaAlmacenesStock = await GetAlmacenesActivosAsync(sedeId);
            vm.MesActualStock = DateTime.Today.ToString("MMMM yyyy", new CultureInfo("es-ES"));

            // 9. Top Menor Stock
            vm.ListaGruposProducto = await GetGruposActivosAsync();
            vm.ListaTopMenorStock = await GetTopMenorStockAsync(null, null, null, sedeId);

            // 10. Agenda / Calendario
            vm.AreasAgenda = await GetAreasAgendaAsync(0, esAdmin);
            vm.EventosAgenda = await GetEventosAgendaAsync(vm.AreasAgenda.FirstOrDefault()?.AreaId ?? 1);
            vm.AreaAgendaInicialId = vm.AreasAgenda.FirstOrDefault()?.AreaId ?? 1;
            vm.AreaAgendaInicialNombre = vm.AreasAgenda.FirstOrDefault()?.Nombre ?? "Administración";

            // 11. Roles y configuración
            try
            {
                var roles = await conn.QueryAsync<RolDto>(@"
                    SELECT rol_id AS Id, rol AS Codigo, nombre AS Nombre
                    FROM rol
                    WHERE estado = 'A'
                    ORDER BY rol_id");
                vm.ListaRoles = roles.ToList();
            }
            catch { }

            var roleConfigs = await GetDashboardRolesConfigAsync();
            vm.DashboardRolesConfigJson = System.Text.Json.JsonSerializer.Serialize(roleConfigs);

            return vm;
        }

        public async Task<List<ClienteInactivoDto>> GetClientesInactivosAsync(int meses = 5, int? vendedorId = null, int? sedeId = null)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var list = await conn.QueryAsync<ClienteInactivoDto>(ClientesInactivosSql, new { meses, vid = vendedorId, sede = sedeId });
                var result = list.ToList();
                foreach (var item in result)
                {
                    item.FechaFormateada = item.FechaUltimaCompra.ToString("dd/MM/yyyy");
                }
                return result;
            }
            catch
            {
                return new List<ClienteInactivoDto>();
            }
        }

        public async Task<(List<string> labels, List<int> cantidades, List<double> montos, int totalPedidos, string totalMonto)> GetPedidosVendedorAsync(int vendedorId, int dias = 12)
        {
            var labels = new List<string>();
            var cantidades = new List<int>();
            var montos = new List<double>();
            int totalPedidos = 0;
            double sumMonto = 0;

            DateTime hoy = DateTime.Today;
            string[] mesesCortos = { "", "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Set", "Oct", "Nov", "Dic" };
            var fechas = new List<DateTime>();

            for (int i = dias - 1; i >= 0; i--)
            {
                DateTime f = hoy.AddDays(-i).Date;
                fechas.Add(f);
                labels.Add(f.Day + " " + mesesCortos[f.Month]);
            }

            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                DateTime fDesde = fechas.First();
                DateTime fHasta = fechas.Last().AddDays(1);

                var rows = await conn.QueryAsync<dynamic>($@"
                    SELECT 
                        CAST(c.fecha AS date) AS Dia,
                        COUNT(*) AS Cantidad,
                        ROUND(ISNULL(SUM({SqlMontoEnSoles}), 0), 2) AS MontoTotal
                    FROM cotiza c
                    LEFT JOIN periodo_emp_sede pes ON c.periodo_emp_sede_id = pes.periodo_emp_sede_id
                    {SqlTipoCambioApply}
                    WHERE c.es_pedido = 'S' 
                      AND c.estado = 'A'
                      AND c.estado_cotiza_id IN (2, 3, 4)
                      AND c.vendedor_id = @Vid
                      AND c.fecha >= @FDesde AND c.fecha < @FHasta
                    GROUP BY CAST(c.fecha AS date)",
                    new { Vid = vendedorId, FDesde = fDesde, FHasta = fHasta });

                var dict = rows.ToDictionary(
                    r => ((DateTime)r.Dia).ToString("yyyy-MM-dd"),
                    r => (Cant: Convert.ToInt32(r.Cantidad), Monto: Convert.ToDouble(r.MontoTotal ?? 0))
                );

                foreach (var f in fechas)
                {
                    string key = f.ToString("yyyy-MM-dd");
                    if (dict.TryGetValue(key, out var val))
                    {
                        cantidades.Add(val.Cant);
                        montos.Add(val.Monto);
                        totalPedidos += val.Cant;
                        sumMonto += val.Monto;
                    }
                    else
                    {
                        cantidades.Add(0);
                        montos.Add(0);
                    }
                }
            }
            catch
            {
                for (int i = 0; i < dias; i++)
                {
                    cantidades.Add(0);
                    montos.Add(0);
                }
            }

            string totalMontoFormateado = "S/ " + sumMonto.ToString("N2", CultureInfo.InvariantCulture);
            return (labels, cantidades, montos, totalPedidos, totalMontoFormateado);
        }

        public async Task<(List<string> labels, List<int> cantidades, List<double> montos, int totalCotizaciones, string totalMonto)> GetCotizacionesVendedorAsync(int vendedorId, int dias = 12, int? anio = null)
        {
            var labels = new List<string>();
            var cantidades = new List<int>();
            var montos = new List<double>();
            int totalCotizaciones = 0;
            double sumMonto = 0;

            if (dias < 4 || dias > 30) dias = 12;

            int anioConsulta = anio ?? DateTime.Today.Year;
            DateTime hoy = DateTime.Today;
            int mesConsulta = (anioConsulta == hoy.Year) ? hoy.Month : 12;
            DateTime fechaAncla = (anioConsulta == hoy.Year)
                ? hoy
                : new DateTime(anioConsulta, mesConsulta, (mesConsulta == hoy.Month ? Math.Min(hoy.Day, DateTime.DaysInMonth(anioConsulta, mesConsulta)) : DateTime.DaysInMonth(anioConsulta, mesConsulta)));

            string[] mesesCortos = { "", "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Set", "Oct", "Nov", "Dic" };
            var fechas = new List<DateTime>();

            for (int i = dias - 1; i >= 0; i--)
            {
                DateTime f = fechaAncla.AddDays(-i).Date;
                fechas.Add(f);
                labels.Add(f.Day + " " + mesesCortos[f.Month]);
            }

            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                DateTime fDesde = fechas.First();
                DateTime fHasta = fechaAncla.AddDays(1).Date;

                var rows = await conn.QueryAsync<dynamic>($@"
                    SELECT 
                        CAST(c.fecha AS date) AS Dia,
                        COUNT(*) AS Cantidad,
                        ROUND(ISNULL(SUM({SqlMontoEnSoles}), 0), 2) AS MontoTotal
                    FROM cotiza c
                    LEFT JOIN periodo_emp_sede pes ON c.periodo_emp_sede_id = pes.periodo_emp_sede_id
                    {SqlTipoCambioApply}
                    WHERE c.es_pedido = 'N' 
                      AND c.estado = 'A'
                      AND (@Vid <= 0 OR c.vendedor_id = @Vid)
                      AND c.fecha >= @FDesde AND c.fecha < @FHasta
                    GROUP BY CAST(c.fecha AS date)",
                    new { Vid = vendedorId, FDesde = fDesde, FHasta = fHasta });

                var dict = rows.ToDictionary(
                    r => ((DateTime)r.Dia).ToString("yyyy-MM-dd"),
                    r => (Cant: Convert.ToInt32(r.Cantidad), Monto: Convert.ToDouble(r.MontoTotal ?? 0))
                );

                foreach (var f in fechas)
                {
                    string key = f.ToString("yyyy-MM-dd");
                    if (dict.TryGetValue(key, out var val))
                    {
                        cantidades.Add(val.Cant);
                        montos.Add(val.Monto);
                        totalCotizaciones += val.Cant;
                        sumMonto += val.Monto;
                    }
                    else
                    {
                        cantidades.Add(0);
                        montos.Add(0);
                    }
                }
            }
            catch
            {
                for (int i = 0; i < dias; i++)
                {
                    cantidades.Add(0);
                    montos.Add(0);
                }
            }

            string totalMontoFormateado = "S/ " + sumMonto.ToString("N2", CultureInfo.InvariantCulture);
            return (labels, cantidades, montos, totalCotizaciones, totalMontoFormateado);
        }

        public async Task<dynamic> GetKpisPorSedeAsync(int? sedeId = null, int? anio = null, int? vendedorId = null)
        {
            int anioConsulta = anio ?? DateTime.Today.Year;
            int mesConsulta = DateTime.Today.Month;

            using var conn = _dbConnectionFactory.CreateConnection();

            decimal facturacionMes = 0m;
            int pedidosActivos = 0;
            int cotizacionesActivas = 0;
            int ordenesProduccionMes = 0;
            int ordenesCompraMes = 0;
            double sumatoriaPedidosMes = 0;
            int cantidadPedidosMes = 0;
            int consumoInternoMes = 0;
            int ingresosMermaMes = 0;
            int salidasMermaMes = 0;
            int guiasRemisionMes = 0;

            try
            {
                facturacionMes = await conn.ExecuteScalarAsync<decimal>($@"
                    SELECT ISNULL(SUM({SqlMontoEnSoles}), 0)
                    FROM comprobante c
                    INNER JOIN estado_compro ec ON c.estado_compro_id = ec.estado_compro_id
                    LEFT JOIN periodo_emp_sede pes ON c.periodo_emp_sede_id = pes.periodo_emp_sede_id
                    {SqlTipoCambioApply}
                    WHERE YEAR(c.fecha) = @Anio AND MONTH(c.fecha) = @Mes AND c.estado = 'A'
                      AND (ec.estado_compro = '3' OR UPPER(ec.nombre) LIKE '%EMITID%' OR c.estado_compro_id = 3)
                      AND (@Vid IS NULL OR c.vendedor_id = @Vid)
                      AND (@Sede IS NULL OR @Sede = 0 OR pes.empresa_sede_id = @Sede)",
                    new { Anio = anioConsulta, Mes = mesConsulta, Vid = vendedorId, Sede = sedeId });
            }
            catch { }

            try
            {
                pedidosActivos = await conn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*)
                    FROM cotiza c
                    LEFT JOIN periodo_emp_sede pes ON c.periodo_emp_sede_id = pes.periodo_emp_sede_id
                    WHERE c.es_pedido = 'S' AND c.estado_cotiza_id IN (2, 4) AND c.estado = 'A'
                      AND (@Anio IS NULL OR @Anio = 0 OR YEAR(c.fecha) = @Anio)
                      AND (@Vid IS NULL OR c.vendedor_id = @Vid)
                      AND (@Sede IS NULL OR @Sede = 0 OR pes.empresa_sede_id = @Sede)",
                    new { Anio = anioConsulta, Vid = vendedorId, Sede = sedeId });
            }
            catch { }

            try
            {
                cotizacionesActivas = await conn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*)
                    FROM cotiza c
                    LEFT JOIN periodo_emp_sede pes ON c.periodo_emp_sede_id = pes.periodo_emp_sede_id
                    WHERE c.es_pedido = 'N' AND c.estado_cotiza_id = 1 AND c.estado = 'A'
                      AND (@Anio IS NULL OR @Anio = 0 OR YEAR(c.fecha) = @Anio)
                      AND (@Vid IS NULL OR c.vendedor_id = @Vid)
                      AND (@Sede IS NULL OR @Sede = 0 OR pes.empresa_sede_id = @Sede)",
                    new { Anio = anioConsulta, Vid = vendedorId, Sede = sedeId });
            }
            catch { }

            try
            {
                ordenesProduccionMes = await conn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*)
                    FROM orden_pro o
                    LEFT JOIN cotiza c ON c.cotiza_id = o.cotiza_id
                    LEFT JOIN periodo_emp_sede pes ON o.periodo_emp_sede_id = pes.periodo_emp_sede_id
                    WHERE YEAR(o.fecha) = @Anio AND MONTH(o.fecha) = @Mes
                      AND ISNULL(o.estado, 'A') <> 'I' AND ISNULL(o.estado_orden_p_id, 0) <> 4
                      AND (@Vid IS NULL OR c.vendedor_id = @Vid)
                      AND (@Sede IS NULL OR @Sede = 0 OR pes.empresa_sede_id = @Sede)",
                    new { Anio = anioConsulta, Mes = mesConsulta, Vid = vendedorId, Sede = sedeId });
            }
            catch { }

            try
            {
                ordenesCompraMes = await conn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*)
                    FROM orden_co o
                    LEFT JOIN periodo_emp_sede pes ON o.periodo_emp_sede_id = pes.periodo_emp_sede_id
                    WHERE YEAR(o.fecha) = @Anio AND MONTH(o.fecha) = @Mes AND o.estado = 'A'
                      AND (@Sede IS NULL OR @Sede = 0 OR pes.empresa_sede_id = @Sede)",
                    new { Anio = anioConsulta, Mes = mesConsulta, Sede = sedeId });
            }
            catch { }

            try
            {
                var pedStats = await conn.QueryFirstOrDefaultAsync<dynamic>($@"
                    SELECT 
                        ROUND(ISNULL(SUM({SqlMontoEnSoles}), 0), 2) AS Sumatoria,
                        COUNT(*) AS Cantidad
                    FROM cotiza c
                    LEFT JOIN periodo_emp_sede pes ON c.periodo_emp_sede_id = pes.periodo_emp_sede_id
                    {SqlTipoCambioApply}
                    WHERE c.es_pedido = 'S' AND c.estado = 'A' AND c.estado_cotiza_id IN (2, 3, 4)
                      AND YEAR(c.fecha) = @Anio AND MONTH(c.fecha) = @Mes
                      AND (@Vid IS NULL OR c.vendedor_id = @Vid)
                      AND (@Sede IS NULL OR @Sede = 0 OR pes.empresa_sede_id = @Sede)",
                    new { Anio = anioConsulta, Mes = mesConsulta, Vid = vendedorId, Sede = sedeId });

                if (pedStats != null)
                {
                    sumatoriaPedidosMes = Convert.ToDouble(pedStats.Sumatoria ?? 0);
                    cantidadPedidosMes = Convert.ToInt32(pedStats.Cantidad ?? 0);
                }
            }
            catch { }

            try
            {
                var movStats = await conn.QueryAsync<dynamic>(@"
                    SELECT m.trans_alma_id, COUNT(*) AS Cantidad
                    FROM mov_almacen m
                    LEFT JOIN periodo_emp_sede pes ON m.periodo_emp_sede_id = pes.periodo_emp_sede_id
                    WHERE YEAR(m.fecha) = @Anio AND MONTH(m.fecha) = @Mes AND m.estado = 'A'
                      AND ISNULL(m.estado_mov_alm_id, 0) <> 3 AND m.estado_aproba_id = 2
                      AND m.trans_alma_id IN (7, 8, 9)
                      AND (@Sede IS NULL OR @Sede = 0 OR pes.empresa_sede_id = @Sede)
                    GROUP BY m.trans_alma_id",
                    new { Anio = anioConsulta, Mes = mesConsulta, Sede = sedeId });

                foreach (var row in movStats)
                {
                    int tid = Convert.ToInt32(row.trans_alma_id);
                    int cant = Convert.ToInt32(row.Cantidad);
                    if (tid == 9) consumoInternoMes = cant;
                    else if (tid == 7) ingresosMermaMes = cant;
                    else if (tid == 8) salidasMermaMes = cant;
                }
            }
            catch { }

            try
            {
                guiasRemisionMes = await conn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*)
                    FROM guia_remision g
                    LEFT JOIN periodo_emp_sede pes ON g.periodo_emp_sede_id = pes.periodo_emp_sede_id
                    WHERE YEAR(g.fecha) = @Anio AND MONTH(g.fecha) = @Mes AND g.estado = 'A'
                      AND g.estado_guia_r_id = 2
                      AND (@Sede IS NULL OR @Sede = 0 OR pes.empresa_sede_id = @Sede)",
                    new { Anio = anioConsulta, Mes = mesConsulta, Sede = sedeId });
            }
            catch { }

            return new
            {
                success = true,
                facturacionMes = "S/ " + facturacionMes.ToString("N2", CultureInfo.InvariantCulture),
                pedidosActivos = pedidosActivos.ToString(),
                cotizacionesActivas = cotizacionesActivas.ToString(),
                ordenesProduccionMes = ordenesProduccionMes.ToString(),
                ordenesCompraMes = ordenesCompraMes.ToString(),
                pedidosMesMonto = "S/ " + sumatoriaPedidosMes.ToString("N2", CultureInfo.InvariantCulture),
                pedidosMesCantidad = cantidadPedidosMes,
                consumoInternoMes = consumoInternoMes,
                ingresosMermaMes = ingresosMermaMes,
                salidasMermaMes = salidasMermaMes,
                guiasRemisionMes = guiasRemisionMes
            };
        }

        public async Task<List<ProductoStockDto>> GetStockInsumosAsync(int? almacenId = null, int? sedeId = null)
        {
            var lista = new List<ProductoStockDto>();
            int sedeFiltro = (sedeId.HasValue && sedeId.Value > 0) ? sedeId.Value : 1;

            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var rows = await conn.QueryAsync<dynamic>(@"
                    DECLARE @targetCodes TABLE (codigo VARCHAR(50), orden_fijo INT);
                    INSERT INTO @targetCodes VALUES 
                    ('FERLMP001003', 1),
                    ('FERHER005002', 2),
                    ('FERPYB001004', 3),
                    ('FERHER001021', 4),
                    ('FERHER001033', 5),
                    ('FERHER001034', 6),
                    ('FERPYB002020', 7),
                    ('FERPYB002021', 8),
                    ('FERPYB002022', 9);

                    WITH UltimoSaldoAlmacen AS (
                        SELECT 
                            s.producto_id,
                            s.almacen_id,
                            (s.cant_ant + s.cant_ing - s.cant_sal) AS saldo_alm,
                            ROW_NUMBER() OVER (
                                PARTITION BY s.producto_id, s.almacen_id 
                                ORDER BY pes.periodo_id DESC, s.mes_contable_id DESC
                            ) AS rn
                        FROM dbo.mov_alm_saldo s
                        INNER JOIN dbo.periodo_emp_sede pes ON s.periodo_emp_sede_id = pes.periodo_emp_sede_id
                        INNER JOIN dbo.producto p ON s.producto_id = p.producto_id
                        INNER JOIN @targetCodes tc ON RTRIM(p.producto) = tc.codigo
                        WHERE (@almacenId IS NULL OR @almacenId = 0 OR s.almacen_id = @almacenId)
                          AND pes.empresa_sede_id = @sedeSesion
                    ),
                    ConsolidadoSaldo AS (
                        SELECT 
                            producto_id,
                            SUM(saldo_alm) AS stock_total
                        FROM UltimoSaldoAlmacen
                        WHERE rn = 1
                        GROUP BY producto_id
                    ),
                    ConsumoMesActual AS (
                        SELECT 
                            map.producto_id,
                            SUM(map.cantidad) AS consumo_mes
                        FROM dbo.mov_alm_prod map
                        INNER JOIN dbo.mov_almacen ma ON map.mov_almacen_id = ma.mov_almacen_id
                        LEFT JOIN dbo.periodo_emp_sede pes ON ma.periodo_emp_sede_id = pes.periodo_emp_sede_id
                        INNER JOIN dbo.producto p ON map.producto_id = p.producto_id
                        INNER JOIN @targetCodes tc ON RTRIM(p.producto) = tc.codigo
                        WHERE ma.tipo_mov_alma_id = 2
                          AND ma.estado = 'A'
                          AND (@almacenId IS NULL OR @almacenId = 0 OR ma.almacen_id = @almacenId)
                          AND pes.empresa_sede_id = @sedeSesion
                          AND ma.fecha >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)
                          AND ma.fecha < DATEADD(month, 1, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1))
                        GROUP BY map.producto_id
                    )
                    SELECT 
                        tc.orden_fijo,
                        p.producto_id,
                        p.producto AS codigo,
                        p.descripcion,
                        ISNULL(um.abrev, 'UND') AS unidad_medida,
                        ISNULL(cs.stock_total, 0) AS stock_actual,
                        ISNULL(avp.stock_min, 20) AS stock_min,
                        ISNULL(cm.consumo_mes, 0) AS consumo_mes
                    FROM @targetCodes tc
                    INNER JOIN dbo.producto p ON tc.codigo = RTRIM(p.producto)
                    LEFT JOIN dbo.alm_vol_prod avp ON p.producto_id = avp.producto_id AND avp.empresa_sede_id = @sedeSesion
                    LEFT JOIN dbo.unidad_medida um ON p.unidad_medida_id = um.unidad_medida_id
                    LEFT JOIN ConsolidadoSaldo cs ON p.producto_id = cs.producto_id
                    LEFT JOIN ConsumoMesActual cm ON p.producto_id = cm.producto_id
                    ORDER BY tc.orden_fijo ASC;",
                    new { almacenId = (object?)almacenId ?? DBNull.Value, sedeSesion = sedeFiltro });

                foreach (var r in rows)
                {
                    decimal stock = r.stock_actual != null ? Convert.ToDecimal(r.stock_actual) : 0m;
                    decimal consumo = r.consumo_mes != null ? Convert.ToDecimal(r.consumo_mes) : 0m;
                    decimal rawStockMin = r.stock_min != null ? Convert.ToDecimal(r.stock_min) : 0m;
                    decimal stockMin = rawStockMin > 0 ? rawStockMin : 20m;

                    string estado;
                    string badge;
                    if (stock <= 0m) { estado = "Agotado"; badge = "danger"; }
                    else if (stock <= stockMin) { estado = "Stock Bajo"; badge = "warning"; }
                    else { estado = "Disponible"; badge = "success"; }

                    lista.Add(new ProductoStockDto
                    {
                        Orden = Convert.ToInt32(r.orden_fijo),
                        Codigo = (string)r.codigo ?? "",
                        ProductoId = (int)r.producto_id,
                        Descripcion = (string)r.descripcion ?? "",
                        UnidadMedida = (string)r.unidad_medida ?? "UND",
                        StockActual = stock,
                        ConsumoMes = consumo,
                        StockActualFormateado = stock.ToString("N2", CultureInfo.InvariantCulture),
                        ConsumoMesFormateado = consumo.ToString("N2", CultureInfo.InvariantCulture),
                        EstadoStock = estado,
                        BadgeColor = badge
                    });
                }
            }
            catch { }

            return lista;
        }

        public async Task<List<AlmacenItemDto>> GetAlmacenesActivosAsync(int? sedeId = null)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var rows = await conn.QueryAsync<AlmacenItemDto>(@"
                    SELECT almacen_id AS AlmacenId, nombre AS Nombre 
                    FROM dbo.almacen 
                    WHERE estado = 'A' 
                      AND (@sede IS NULL OR @sede = 0 OR empresa_sede_id = @sede) 
                    ORDER BY almacen_id",
                    new { sede = sedeId });
                return rows.ToList();
            }
            catch
            {
                return new List<AlmacenItemDto>();
            }
        }

        public async Task<List<CategoriaFiltroItemDto>> GetGruposActivosAsync()
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var rows = await conn.QueryAsync<CategoriaFiltroItemDto>(@"
                    SELECT grupo_producto_id AS Id, nombre AS Nombre 
                    FROM dbo.grupo_producto 
                    WHERE estado = 'A' AND ISNULL(es_combo, 'N') <> 'S' AND ISNULL(tipo_producto_id, 1) <> 2
                    ORDER BY nombre");
                return rows.ToList();
            }
            catch
            {
                return new List<CategoriaFiltroItemDto>();
            }
        }

        public async Task<List<CategoriaFiltroItemDto>> GetClasesPorGrupoAsync(int? grupoId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var rows = await conn.QueryAsync<CategoriaFiltroItemDto>(@"
                    SELECT clase_producto_id AS Id, nombre AS Nombre 
                    FROM dbo.clase_producto 
                    WHERE estado = 'A' 
                      AND (@grpId IS NULL OR @grpId = 0 OR grupo_producto_id = @grpId)
                    ORDER BY nombre",
                    new { grpId = grupoId });
                return rows.ToList();
            }
            catch
            {
                return new List<CategoriaFiltroItemDto>();
            }
        }

        public async Task<List<CategoriaFiltroItemDto>> GetFamiliasPorClaseAsync(int? claseId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var rows = await conn.QueryAsync<CategoriaFiltroItemDto>(@"
                    SELECT familia_producto_id AS Id, nombre AS Nombre 
                    FROM dbo.familia_producto 
                    WHERE estado = 'A' 
                      AND (@clsId IS NULL OR @clsId = 0 OR clase_producto_id = @clsId)
                    ORDER BY nombre",
                    new { clsId = claseId });
                return rows.ToList();
            }
            catch
            {
                return new List<CategoriaFiltroItemDto>();
            }
        }

        public async Task<List<TopMenorStockDto>> GetTopMenorStockAsync(int? grupoId = null, int? claseId = null, int? familiaId = null, int? sedeId = null)
        {
            var lista = new List<TopMenorStockDto>();
            int sedeFiltro = (sedeId.HasValue && sedeId.Value > 0) ? sedeId.Value : 1;

            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var rows = await conn.QueryAsync<dynamic>(@"
                    WITH SaldoAbierto AS (
                        SELECT 
                            mas.producto_id,
                            SUM((mas.cant_ant + mas.cant_ing) - (mas.cant_reserva + mas.cant_sal)) AS stock_total
                        FROM dbo.mov_alm_saldo mas
                        INNER JOIN dbo.mov_alm_cierre mac ON mac.periodo_emp_sede_id = mas.periodo_emp_sede_id
                            AND mac.almacen_id = mas.almacen_id
                            AND mac.mes_contable_id = mas.mes_contable_id
                        INNER JOIN dbo.estado_ape_cie eac ON eac.estado_ape_cie_id = mac.estado_ape_cie_id
                        LEFT JOIN dbo.periodo_emp_sede pes ON mac.periodo_emp_sede_id = pes.periodo_emp_sede_id
                        WHERE eac.estado_ape_cie = 'A'
                          AND pes.empresa_sede_id = @sedeId
                        GROUP BY mas.producto_id
                    )
                    SELECT TOP 10 
                        p.producto_id,
                        p.producto AS codigo,
                        p.descripcion AS nombre,
                        ISNULL(s.stock_total, 0) AS cantidad,
                        avp.stock_min,
                        ISNULL(um.abrev, 'UND') AS unidad_medida,
                        g.nombre AS grupo_nombre,
                        cl.nombre AS clase_nombre,
                        f.nombre AS familia_nombre
                    FROM dbo.alm_vol_prod avp
                    INNER JOIN dbo.producto p ON avp.producto_id = p.producto_id
                    LEFT JOIN SaldoAbierto s ON p.producto_id = s.producto_id
                    LEFT JOIN dbo.unidad_medida um ON p.unidad_medida_id = um.unidad_medida_id
                    LEFT JOIN dbo.familia_producto f ON p.familia_producto_id = f.familia_producto_id
                    LEFT JOIN dbo.clase_producto cl ON f.clase_producto_id = cl.clase_producto_id
                    LEFT JOIN dbo.grupo_producto g ON cl.grupo_producto_id = g.grupo_producto_id
                    WHERE avp.empresa_sede_id = @sedeId
                      AND avp.stock_min > 0
                      AND p.estado_producto_id = 1
                      AND ISNULL(g.es_combo, 'N') <> 'S'
                      AND ISNULL(g.tipo_producto_id, 1) <> 2
                      AND (@grupoId IS NULL OR @grupoId = 0 OR g.grupo_producto_id = @grupoId)
                      AND (@claseId IS NULL OR @claseId = 0 OR cl.clase_producto_id = @claseId)
                      AND (@familiaId IS NULL OR @familiaId = 0 OR f.familia_producto_id = @familiaId)
                    ORDER BY ISNULL(s.stock_total, 0) ASC, (ISNULL(s.stock_total, 0) - avp.stock_min) ASC, p.descripcion ASC;",
                    new { sedeId = sedeFiltro, grupoId, claseId, familiaId });

                int orden = 1;
                foreach (var r in rows)
                {
                    decimal cantidad = r.cantidad != null ? Convert.ToDecimal(r.cantidad) : 0m;
                    decimal stockMin = r.stock_min != null ? Convert.ToDecimal(r.stock_min) : 0m;

                    string estado;
                    string badge;
                    if (cantidad <= 0m) { estado = "Agotado"; badge = "danger"; }
                    else if (cantidad <= stockMin) { estado = "Stock Crítico"; badge = "warning"; }
                    else { estado = "Stock Bajo"; badge = "info"; }

                    lista.Add(new TopMenorStockDto
                    {
                        Orden = orden++,
                        ProductoId = (int)r.producto_id,
                        Codigo = (string)r.codigo ?? "",
                        Nombre = (string)r.nombre ?? "",
                        Cantidad = cantidad,
                        CantidadFormateada = cantidad.ToString("N2", CultureInfo.InvariantCulture),
                        UnidadMedida = (string)r.unidad_medida ?? "UND",
                        GrupoNombre = (string)r.grupo_nombre ?? "",
                        ClaseNombre = (string)r.clase_nombre ?? "",
                        FamiliaNombre = (string)r.familia_nombre ?? "",
                        EstadoStock = estado,
                        BadgeColor = badge
                    });
                }
            }
            catch { }

            return lista;
        }

        public async Task<List<CalendarioAreaDto>> GetAreasAgendaAsync(int usuarioId, bool esAdmin)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var rows = await conn.QueryAsync<CalendarioAreaDto>(@"
                    SELECT area_id AS AreaId, nombre AS Nombre, ISNULL(descripcion, '') AS Descripcion, ISNULL(color, '#0d6efd') AS Color
                    FROM dbo.calendario_area
                    ORDER BY area_id");
                return rows.ToList();
            }
            catch
            {
                return new List<CalendarioAreaDto>();
            }
        }

        public async Task<List<CalendarioEventoDto>> GetEventosAgendaAsync(int? areaId = null)
        {
            var list = new List<CalendarioEventoDto>();
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var rows = await conn.QueryAsync<dynamic>(@"
                    SELECT e.evento_id, e.area_id, e.usuario_id, e.usuario_logon, e.titulo, e.contexto,
                           e.fecha_inicio, e.fecha_fin, e.todo_el_dia, e.color,
                           ISNULL(a.nombre, 'General') AS area_nombre
                    FROM dbo.calendario_evento e
                    LEFT JOIN dbo.calendario_area a ON e.area_id = a.area_id
                    WHERE e.estado = 'A' 
                      AND (@areaId IS NULL OR @areaId = 0 OR e.area_id = @areaId)
                    ORDER BY e.fecha_inicio ASC",
                    new { areaId });

                foreach (var r in rows)
                {
                    list.Add(new CalendarioEventoDto
                    {
                        EventoId = (int)r.evento_id,
                        AreaId = (int)r.area_id,
                        AreaNombre = (string)r.area_nombre ?? "General",
                        Titulo = (string)r.titulo ?? "",
                        Contexto = (string)r.contexto ?? "",
                        FechaInicio = (DateTime)r.fecha_inicio,
                        FechaFin = r.fecha_fin != null ? (DateTime?)r.fecha_fin : null,
                        TodoElDia = Convert.ToBoolean(r.todo_el_dia),
                        Color = (string)r.color ?? "#0d6efd",
                        UsuarioLogon = (string)r.usuario_logon ?? ""
                    });
                }
            }
            catch { }

            return list;
        }

        private async Task EnsureDashboardRolConfigTableAsync(IDbConnection conn)
        {
            try
            {
                await conn.ExecuteAsync(@"
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'dashboard_rol_config')
                    BEGIN
                        CREATE TABLE dbo.dashboard_rol_config (
                            rol_codigo VARCHAR(50) NOT NULL PRIMARY KEY,
                            config_json NVARCHAR(MAX) NOT NULL,
                            fecha_modifica DATETIME NOT NULL DEFAULT GETDATE()
                        );
                    END");
            }
            catch { }
        }

        public async Task<Dictionary<string, string>> GetDashboardRolesConfigAsync()
        {
            var dict = new Dictionary<string, string>();
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                await EnsureDashboardRolConfigTableAsync(conn);
                var rows = await conn.QueryAsync<dynamic>("SELECT rol_codigo, config_json FROM dbo.dashboard_rol_config");
                foreach (var r in rows)
                {
                    string rol = (string)r.rol_codigo;
                    string cfg = (string)r.config_json;
                    if (!string.IsNullOrEmpty(rol)) dict[rol] = cfg;
                }
            }
            catch { }
            return dict;
        }

        public async Task<bool> SaveDashboardRoleConfigAsync(string rolCodigo, string configJson)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                await EnsureDashboardRolConfigTableAsync(conn);
                int rows = await conn.ExecuteAsync(@"
                    IF EXISTS (SELECT 1 FROM dbo.dashboard_rol_config WHERE rol_codigo = @Rol)
                        UPDATE dbo.dashboard_rol_config SET config_json = @Config, fecha_modifica = GETDATE() WHERE rol_codigo = @Rol
                    ELSE
                        INSERT INTO dbo.dashboard_rol_config (rol_codigo, config_json, fecha_modifica) VALUES (@Rol, @Config, GETDATE())",
                    new { Rol = rolCodigo, Config = configJson });
                return rows > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
