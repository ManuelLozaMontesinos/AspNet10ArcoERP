using System;
using System.Collections.Generic;

namespace AspNet10ArcoERP.Models
{
    public class LookupItemModel
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
    }

    public class CuentaBancoPageModel : ListadoPaginadoModel
    {
        public List<CuentaBancoModel> Items { get; set; } = new();
    }

    public class CuentaBancoModel
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public int BancoId { get; set; }
        public string BancoNombre { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int MonedaId { get; set; }
        public string MonedaNombre { get; set; } = string.Empty;
        public string Cuenta { get; set; } = string.Empty;
        public string CuentaInterbancaria { get; set; } = string.Empty;
        public string Estado { get; set; } = "A";
    }

    public class UbigeoLookupModel
    {
        public int Id { get; set; }
        public int PadreId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
    }

    public class TipoDocIdentidadLookupModel
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Abreviatura { get; set; } = string.Empty;
        public string FormatoValidacion { get; set; } = string.Empty;
    }

    public class ProveedorModel
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; } = 1;
        public string Codigo { get; set; } = string.Empty; // char(7)
        public int TipoNaturalezaJuridicaId { get; set; } = 2; // 1 = Natural, 2 = Jurídica (Default en empresas)
        public string TipoNaturalezaJuridicaNombre { get; set; } = string.Empty;
        public string TipoNaturalezaJuridicaCodigo { get; set; } = string.Empty;

        public string RazonSocial { get; set; } = string.Empty;
        public string Abreviatura { get; set; } = string.Empty; // Nombre Comercial
        public string ApellidoPat { get; set; } = string.Empty;
        public string ApellidoMat { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;

        public int TipoDocumentoIdentidadId { get; set; } = 3; // 3 = RUC (tipo_doc_iden_id=3 en BD es 06 RUC)
        public string TipoDocumentoIdentidadNombre { get; set; } = string.Empty;
        public string TipoDocumentoIdentidadCodigo { get; set; } = string.Empty;
        public string FormatoValidacion { get; set; } = string.Empty;

        public string Documento { get; set; } = string.Empty;
        public string NumeroDocumentoIdentidad { get; set; } = string.Empty;
        public string NumeroRuc { get; set; } = string.Empty;
        public string NumeroCCI { get; set; } = string.Empty; // Código de Cuenta Interbancaria

        public string Contacto { get; set; } = string.Empty;

        // Dirección estructurada
        public int TipoViaId { get; set; } = 12; // 12 = '-'
        public string TipoViaSigla { get; set; } = string.Empty;
        public string TipoViaNombre { get; set; } = string.Empty;
        public string NombreVia { get; set; } = string.Empty;
        public string NumeroVia { get; set; } = string.Empty;
        public string Interior { get; set; } = string.Empty;
        public string Dpto { get; set; } = string.Empty;
        public string Manzana { get; set; } = string.Empty;
        public string Lote { get; set; } = string.Empty;
        public string Kilometro { get; set; } = string.Empty;
        public string Block { get; set; } = string.Empty;
        public string Etapa { get; set; } = string.Empty;

        public int TipoZonaId { get; set; } = 12; // 12 = '-'
        public string TipoZonaSigla { get; set; } = string.Empty;
        public string TipoZonaNombre { get; set; } = string.Empty;
        public string NombreZona { get; set; } = string.Empty;
        public string Referencia { get; set; } = string.Empty;

        // Ubigeo
        public int DepartamentoId { get; set; } = 15; // 15 = Lima
        public string DepartamentoNombre { get; set; } = string.Empty;
        public int ProvinciaId { get; set; } = 128; // 128 = Lima
        public string ProvinciaNombre { get; set; } = string.Empty;
        public int DistritoId { get; set; } = 1281; // 1281 = Lima Cercado
        public string DistritoNombre { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Web { get; set; } = string.Empty;

        public int TipoProveedorId { get; set; } = 1; // 1 = Cliente, 2 = Proveedor, 3 = Ambos
        public string TipoProveedorNombre { get; set; } = string.Empty;
        public int? CondicionPagoId { get; set; } = 1; // 1 = Contado
        public string CondicionPagoNombre { get; set; } = string.Empty;
        public int? TipoProfesionId { get; set; }
        public string TipoProfesionNombre { get; set; } = string.Empty;
        public int? TipoVentaClienteId { get; set; }
        public string TipoVentaClienteNombre { get; set; } = string.Empty;
        public int? LeadSourceId { get; set; }
        public string LeadSourceNombre { get; set; } = string.Empty;

        public string EsLocal { get; set; } = "S";
        public string EsProspecto { get; set; } = "N";
        public string Estado { get; set; } = "A";

        // Helpers de visualización
        public string Direccion
        {
            get => DireccionCompleta;
            set { if (string.IsNullOrWhiteSpace(NombreVia) && !string.IsNullOrWhiteSpace(value)) NombreVia = value; }
        }

        public string DireccionCompleta
        {
            get
            {
                var partes = new List<string>();
                string via = $"{TipoViaSigla} {NombreVia}".Trim();
                if (!string.IsNullOrWhiteSpace(via)) partes.Add(via);
                if (!string.IsNullOrWhiteSpace(NumeroVia)) partes.Add($"N° {NumeroVia.Trim()}");
                if (!string.IsNullOrWhiteSpace(Interior)) partes.Add($"Int. {Interior.Trim()}");
                if (!string.IsNullOrWhiteSpace(Manzana)) partes.Add($"Mz. {Manzana.Trim()}");
                if (!string.IsNullOrWhiteSpace(Lote)) partes.Add($"Lt. {Lote.Trim()}");
                if (!string.IsNullOrWhiteSpace(NombreZona)) partes.Add($"{TipoZonaSigla} {NombreZona}".Trim());
                if (!string.IsNullOrWhiteSpace(DistritoNombre)) partes.Add(DistritoNombre);
                if (!string.IsNullOrWhiteSpace(ProvinciaNombre) && ProvinciaNombre != DistritoNombre) partes.Add(ProvinciaNombre);

                return partes.Count > 0 ? string.Join(" ", partes) : NombreVia;
            }
        }

        public string DocumentoPrincipal => !string.IsNullOrWhiteSpace(NumeroRuc)
            ? NumeroRuc
            : (!string.IsNullOrWhiteSpace(NumeroDocumentoIdentidad) ? NumeroDocumentoIdentidad : Documento);

        public string NombreCompleto => !string.IsNullOrWhiteSpace(RazonSocial) 
            ? RazonSocial 
            : $"{Nombres} {ApellidoPat} {ApellidoMat}".Trim();
    }

    public class RemMinVitalModel
    {
        public int Id { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaAlta { get; set; } = DateTime.Today;
        public DateTime? FechaBaja { get; set; }
        public string Estado { get; set; } = "A";
    }

    public class UitModel
    {
        public int Id { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaAlta { get; set; } = DateTime.Today;
        public DateTime? FechaBaja { get; set; }
        public string Estado { get; set; } = "A";
    }

    public class CentroCostoModel
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; } = 1;
        public string Nombre { get; set; } = string.Empty;
        public string Sigla { get; set; } = string.Empty;
        public string Estado { get; set; } = "A";
    }

    public class GrupoProductoModel
    {
        public int Id { get; set; }
        public int TipoProductoId { get; set; } = 1;
        public string TipoProductoNombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public short Correlativo { get; set; } = 1;
        public string UsarIncrementoFraccion { get; set; } = "N";
        public string EsCombo { get; set; } = "N";
        public string EsInsumo { get; set; } = "N";
        public string Estado { get; set; } = "A";
        public DateTime FechaAlta { get; set; } = DateTime.Today;
        public DateTime? FechaBaja { get; set; }
        public int ClasesCount { get; set; }
        public int FamiliasCount { get; set; }
    }

    public class ClaseProductoModel
    {
        public int Id { get; set; }
        public int GrupoId { get; set; }
        public string GrupoNombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public short Correlativo { get; set; } = 1;
        public int TipoDetraccionId { get; set; } = 1;
        public string TipoDetraccionNombre { get; set; } = string.Empty;
        public decimal TipoDetraccionTasa { get; set; }
        public int FamiliasCount { get; set; }
        public string Estado { get; set; } = "A";
        public DateTime FechaAlta { get; set; } = DateTime.Today;
        public DateTime? FechaBaja { get; set; }
    }

    public class TransportistaModel
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string NumeroRuc { get; set; } = string.Empty;
        public string RazonSocial { get; set; } = string.Empty;
        public string NumeroInscripcionVehicular { get; set; } = string.Empty;
        public string Estado { get; set; } = "A";
        public List<TransportistaUnidadModel> Unidades { get; set; } = new();
        public List<TransportistaConductorModel> Conductores { get; set; } = new();
    }

    public class TransportistaUnidadModel
    {
        public int Id { get; set; }
        public int TransportistaId { get; set; }
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public string Placa { get; set; } = string.Empty;
        public string Placa2 { get; set; } = string.Empty;
        public string Estado { get; set; } = "A";
    }

    public class TransportistaConductorModel
    {
        public int Id { get; set; }
        public int TransportistaId { get; set; }
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string ApellidoMaterno { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public int TipoDocumentoIdentidadId { get; set; }
        public string TipoDocumentoNombre { get; set; } = string.Empty;
        public string NumeroDocumentoIdentidad { get; set; } = string.Empty;
        public string NumeroLicencia { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Estado { get; set; } = "A";
    }

    public class TipoCambioModel
    {
        public int Id { get; set; }
        public int TipoCambioId { get; set; } = 1;
        public string TipoCambioNombre { get; set; } = "OFICIAL";
        public DateTime Fecha { get; set; } = DateTime.Today;
        public int MonedaId { get; set; } = 2;
        public string MonedaNombre { get; set; } = "DOLARES AMERICANOS";
        public string MonedaSimbolo { get; set; } = "USD";
        public decimal MontoCompra { get; set; }
        public decimal MontoVenta { get; set; }
        public string Estado { get; set; } = "A";
    }

    public class TipoCambioCatalogoItem
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
    }

    public class TipoCambioMaestroModel
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Estado { get; set; } = "A";
    }

    public class MonedaCatalogoItem
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Simbolo { get; set; } = string.Empty;
    }

    public class GrupoClaseFamiliaModel
    {
        public int GrupoId { get; set; }
        public string GrupoCodigo { get; set; } = string.Empty;
        public string GrupoNombre { get; set; } = string.Empty;

        public int ClaseId { get; set; }
        public string ClaseCodigo { get; set; } = string.Empty;
        public string ClaseNombre { get; set; } = string.Empty;

        public int FamiliaId { get; set; }
        public string FamiliaCodigo { get; set; } = string.Empty;
        public string FamiliaNombre { get; set; } = string.Empty;
        public short Correlativo { get; set; }

        public string Estado { get; set; } = "A";
    }

    public class ProductoModel
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string CodReferencial { get; set; } = string.Empty;
        
        // Tipo de Producto (1 = PRODUCTO, 2 = SERVICIO)
        public int TipoProductoId { get; set; } = 1;
        public string TipoProductoCodigo { get; set; } = "P";
        public string TipoProductoNombre { get; set; } = "PRODUCTO";

        // Jerarquía
        public int GrupoId { get; set; }
        public string GrupoCodigo { get; set; } = string.Empty;
        public string GrupoNombre { get; set; } = string.Empty;

        public int ClaseId { get; set; }
        public string ClaseCodigo { get; set; } = string.Empty;
        public string ClaseNombre { get; set; } = string.Empty;

        public int FamiliaId { get; set; }
        public string FamiliaCodigo { get; set; } = string.Empty;
        public string FamiliaNombre { get; set; } = string.Empty;

        // Atributos comerciales y físicos
        public int UnidadMedidaId { get; set; } = 1;
        public string UnidadMedida { get; set; } = "UND";
        public string UnidadMedidaNombre { get; set; } = "UNIDADES";
        public int TipoIgvId { get; set; } = 1;
        public string TipoIgvNombre { get; set; } = "GRAVADO";

        public decimal? PesoNeto { get; set; } = 0;
        public decimal? PesoBruto { get; set; } = 0;
        public decimal? ValorReposicion { get; set; } = 0;
        public decimal? Volumen { get; set; } = 0;

        public int? ColorId { get; set; }
        public string ColorNombre { get; set; } = string.Empty;
        public int MasterPack { get; set; } = 1;

        // Estados
        public int EstadoProductoId { get; set; } = 1;
        public string EstadoProductoNombre { get; set; } = "ACTIVO";
        public int EstadoComProdId { get; set; } = 1;
        public string EstadoComProdNombre { get; set; } = "STOCK";
        public string Estado { get; set; } = "A";

        // Banderas (Flags)
        public string TienePresVta { get; set; } = "N";
        public string EsOpera { get; set; } = "N";
        public string EsPerecible { get; set; } = "N";
        public string DetalleReq { get; set; } = "N";
    }

    public class ProductoPresentacionVentaModel
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public int UnidadMedidaId { get; set; }
        public string UnidadMedidaNombre { get; set; } = string.Empty;
        public decimal CantidadEquivalente { get; set; } = 1;
        public string Estado { get; set; } = "A";
    }

    public class ProductoProcesoModel
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public int SubProcesoProduccionId { get; set; }
        public string SubProcesoNombre { get; set; } = string.Empty;
        public int ProcesoProduccionId { get; set; }
        public string ProcesoNombre { get; set; } = string.Empty;
        public int Orden { get; set; } = 1;
        public string Estado { get; set; } = "A";
    }

    public class ProductoDetalleModel
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Estado { get; set; } = "A";
    }

    public class SubProcesoLookupModel
    {
        public int SubProcesoId { get; set; }
        public int ProcesoId { get; set; }
        public string ProcesoNombre { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
    }

    public class ComboProductoModel
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public string ProductoPrincipal { get; set; } = string.Empty;
        public int ProdIncluId { get; set; }
        public string ProductoIncluido { get; set; } = string.Empty;
        public decimal Cantidad { get; set; } = 1;
        public decimal PorcentajePrecio { get; set; } = 100;
        public int NumDecRedo { get; set; } = 2;
        public string Estado { get; set; } = "A";
    }

    public class ProductoComboModel
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int UnidadMedidaId { get; set; } = 1;
        public string UnidadMedida { get; set; } = string.Empty;
        public int TipoProductoId { get; set; } = 1;
        public int GrupoId { get; set; }
        public string GrupoNombre { get; set; } = string.Empty;
        public int ClaseId { get; set; }
        public string ClaseNombre { get; set; } = string.Empty;
        public int FamiliaId { get; set; }
        public string FamiliaNombre { get; set; } = string.Empty;
        public int EstadoProductoId { get; set; } = 1;
        public int EstadoComProdId { get; set; } = 1;
        public string Estado { get; set; } = "A";
        public int ItemsCount { get; set; }
    }

    public class ComboDetalleItemModel
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public int ProdIncluId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal Cantidad { get; set; } = 1;
        public decimal PorcentajePrecio { get; set; } = 100;
        public int NumDecRedo { get; set; } = 1;
        public string Estado { get; set; } = "A";
    }

    public class CondPagoModel
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int TipoCondicionPagoId { get; set; } = 1;
        public string TipoCondicionPagoNombre { get; set; } = string.Empty;
        public int Dias { get; set; }
        public string Estado { get; set; } = "A";
    }

    public class ColorModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Estado { get; set; } = "A";
    }

    public class TablaGeneralModel
    {
        public int Id { get; set; }
        public string Tabla { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Prefijo { get; set; } = string.Empty;
        public int Correlativo { get; set; } = 1;
        public string Estado { get; set; } = "A";
    }
}
