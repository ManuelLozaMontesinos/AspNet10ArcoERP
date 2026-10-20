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

    public class ProveedorModel
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string RazonSocial { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string ApellidoPat { get; set; } = string.Empty;
        public string ApellidoMat { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public string NumeroRuc { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Contacto { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public int TipoDocumentoIdentidadId { get; set; } = 6; // RUC por defecto
        public string Estado { get; set; } = "A";

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

    public class TipoCambioModel
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int OrigenRegistroId { get; set; } = 1;
        public string Estado { get; set; } = "A";
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
        public short Correlativo { get; set; } = 1;

        public string Estado { get; set; } = "A";
    }

    public class ProductoModel
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string CodReferencial { get; set; } = string.Empty;
        public int FamiliaId { get; set; }
        public string FamiliaNombre { get; set; } = string.Empty;
        public int UnidadMedidaId { get; set; } = 1;
        public string UnidadMedida { get; set; } = "UND";
        public int TipoIgvId { get; set; } = 1;
        public decimal? PesoNeto { get; set; } = 0;
        public decimal? PesoBruto { get; set; } = 0;
        public decimal? ValorReposicion { get; set; } = 0;
        public decimal? Volumen { get; set; } = 0;
        public int? ColorId { get; set; }
        public int MasterPack { get; set; } = 1;
        public string Estado { get; set; } = "A";
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

    public class CondPagoModel
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int TipoCondicionPagoId { get; set; } = 1;
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
