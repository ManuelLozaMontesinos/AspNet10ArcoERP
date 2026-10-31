using System.ComponentModel.DataAnnotations;

namespace AspNet10ArcoERP.Models
{
    public class EmpresaItemModel
    {
        public int Id { get; set; }
        public string Ruc { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string NombreComercial { get; set; } = string.Empty;
        public int SectorId { get; set; }
        public string Sector { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Estado { get; set; } = "A";
        public string EstadoTexto => Estado == "A" ? "Activo" : "Inactivo";
        public int TotalSedes { get; set; }
    }

    public class EmpresaPageModel : ListadoPaginadoModel
    {
        public string? Search { get; set; }
        public string? Estado { get; set; }
        public List<EmpresaItemModel> Items { get; set; } = new();
        public List<string> Errores { get; set; } = new();
    }

    public class EmpresaDetalleModel
    {
        public int Id { get; set; }
        public int SectorId { get; set; } = 1;
        public string Ruc { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string NombreComercial { get; set; } = string.Empty;
        public int TipoViaId { get; set; } = 1;
        public string? NombreVia { get; set; }
        public string? NumeroVia { get; set; }
        public string? Dpto { get; set; }
        public string? Interior { get; set; }
        public string? Manzana { get; set; }
        public string? Lote { get; set; }
        public string? Kilometro { get; set; }
        public string? Block { get; set; }
        public string? Etapa { get; set; }
        public int TipoZonaId { get; set; } = 1;
        public string? NombreZona { get; set; }
        public int DepartamentoId { get; set; } = 15; // Lima default
        public int ProvinciaId { get; set; } = 128;   // Lima default
        public int DistritoId { get; set; } = 1251;   // Lima default
        public int? BancoId { get; set; }
        public string? NumCtaDetrac { get; set; }
        public string Estado { get; set; } = "A";
        public string? Revision { get; set; }

        public List<LookupItemModel> Sectores { get; set; } = new();
        public List<LookupItemModel> TiposVia { get; set; } = new();
        public List<LookupItemModel> TiposZona { get; set; } = new();
        public List<LookupItemModel> Departamentos { get; set; } = new();
        public List<LookupItemModel> Provincias { get; set; } = new();
        public List<LookupItemModel> Distritos { get; set; } = new();
        public List<LookupItemModel> Bancos { get; set; } = new();
        public List<LookupItemModel> Periodos { get; set; } = new();
        public List<EmpresaSedeModel> Sedes { get; set; } = new();
    }

    public class EmpresaGuardarSolicitud
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "El RUC es obligatorio.")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "El RUC debe tener 11 dígitos.")]
        public string Ruc { get; set; } = string.Empty;

        [Required(ErrorMessage = "La razón social es obligatoria.")]
        [StringLength(100, ErrorMessage = "La razón social no puede exceder 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "El nombre comercial no puede exceder 50 caracteres.")]
        public string? NombreComercial { get; set; }

        public int SectorId { get; set; } = 1;
        public int TipoViaId { get; set; } = 1;
        public string? NombreVia { get; set; }
        public string? NumeroVia { get; set; }
        public string? Dpto { get; set; }
        public string? Interior { get; set; }
        public string? Manzana { get; set; }
        public string? Lote { get; set; }
        public string? Kilometro { get; set; }
        public string? Block { get; set; }
        public string? Etapa { get; set; }
        public int TipoZonaId { get; set; } = 1;
        public string? NombreZona { get; set; }
        public int DistritoId { get; set; }
        public int? BancoId { get; set; }
        public string? NumCtaDetrac { get; set; }
        public string Estado { get; set; } = "A";
        public string? Revision { get; set; }
    }

    public class EmpresaSedeModel
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string CodigoSunat { get; set; } = string.Empty;
        public string EsCentral { get; set; } = "N";
        public bool EsCentralBool => EsCentral == "S";
        public int TipoViaId { get; set; } = 1;
        public string? NombreVia { get; set; }
        public string? NumeroVia { get; set; }
        public string? Dpto { get; set; }
        public string? Interior { get; set; }
        public string? Manzana { get; set; }
        public string? Lote { get; set; }
        public string? Kilometro { get; set; }
        public string? Block { get; set; }
        public string? Etapa { get; set; }
        public int TipoZonaId { get; set; } = 1;
        public string? NombreZona { get; set; }
        public int DepartamentoId { get; set; }
        public int ProvinciaId { get; set; }
        public int DistritoId { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Telefono2 { get; set; }
        public string? Email { get; set; }
        public string Estado { get; set; } = "A";
        public string EstadoTexto => Estado == "A" ? "Activo" : "Inactivo";
    }

    public class EmpresaSedeSolicitud
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }

        public string? Codigo { get; set; }

        [Required(ErrorMessage = "El nombre de la sede es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El código SUNAT es obligatorio.")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "El código SUNAT debe tener 4 dígitos.")]
        public string CodigoSunat { get; set; } = "0000";

        public string EsCentral { get; set; } = "N";

        public int TipoViaId { get; set; } = 1;
        public string? NombreVia { get; set; }
        public string? NumeroVia { get; set; }
        public string? Dpto { get; set; }
        public string? Interior { get; set; }
        public string? Manzana { get; set; }
        public string? Lote { get; set; }
        public string? Kilometro { get; set; }
        public string? Block { get; set; }
        public string? Etapa { get; set; }
        public int TipoZonaId { get; set; } = 1;
        public string? NombreZona { get; set; }
        public int DistritoId { get; set; }
        public string? Telefono { get; set; }
        public string? Telefono2 { get; set; }
        public string? Email { get; set; }
        public string Estado { get; set; } = "A";
    }

    public class PeriodoSedeItemModel
    {
        public int Id { get; set; }
        public int PeriodoId { get; set; }
        public int Periodo { get; set; }
        public int EmpresaSedeId { get; set; }
        public string SedeCodigo { get; set; } = string.Empty;
        public string SedeNombre { get; set; } = string.Empty;
        public string Estado { get; set; } = "A";
        public string EstadoTexto => Estado == "A" ? "Activo" : "Inactivo";
    }

    public class PeriodoSedeAsignarSolicitud
    {
        public int PeriodoId { get; set; }
        public int EmpresaSedeId { get; set; }
    }
}
