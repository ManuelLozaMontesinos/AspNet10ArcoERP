using System.ComponentModel.DataAnnotations;

namespace AspNet10ArcoERP.Models;

public class UsuarioItemModel : MaestroEmpresaModel
{
    public string UsuarioLogon { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? EmailAlterno { get; set; }
    public bool CambiarPswProximo { get; set; }
    public bool NoPuedeCambiarPsw { get; set; }
    public bool NoExpira { get; set; }
    public DateTime? FechaExpira { get; set; }
    public bool TieneHorario { get; set; }
    public int CantidadRoles { get; set; }

    protected override string VersionExtra => $"{UsuarioLogon}|{NombreCompleto}|{Email}|{Estado}";
}

public class UsuarioPageModel : ListadoPaginadoModel
{
    public string? Search { get; set; }
    public string? Estado { get; set; }
    public List<UsuarioItemModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();
}

public class UsuarioDetalleModel
{
    public UsuarioItemModel Datos { get; set; } = new();
    public List<LookupItemModel> RolesDisponibles { get; set; } = new();
    public List<int> RolesAsignados { get; set; } = new();
}

public class UsuarioGuardarSolicitud
{
    public int Id { get; set; }
    [Required, StringLength(30)]
    public string UsuarioLogon { get; set; } = string.Empty;
    [Required, StringLength(100)]
    public string NombreCompleto { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(50)]
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public bool CambiarPswProximo { get; set; }
    public bool NoExpira { get; set; } = true;
    public DateTime? FechaExpira { get; set; }
    public List<int> RolesIds { get; set; } = new();
    public string? Revision { get; set; }
}

public class UsuarioCambiarContrasenaSolicitud
{
    public int UsuarioId { get; set; }
    [Required, StringLength(100, MinimumLength = 4)]
    public string NuevaContrasena { get; set; } = string.Empty;
    public bool ForzarCambioProximo { get; set; }
}

public class RolItemModel : MaestroEmpresaModel
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int CantidadUsuarios { get; set; }

    protected override string VersionExtra => $"{Codigo}|{Nombre}|{Estado}";
}

public class RolPageModel : ListadoPaginadoModel
{
    public string? Search { get; set; }
    public string? Estado { get; set; }
    public List<RolItemModel> Items { get; set; } = new();
    public List<string> Errores { get; set; } = new();
}

public class RolGuardarSolicitud
{
    public int Id { get; set; }
    [Required, StringLength(10)]
    public string Codigo { get; set; } = string.Empty;
    [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;
    public string? Revision { get; set; }
}

