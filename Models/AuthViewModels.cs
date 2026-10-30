using System;

namespace AspNet10ArcoERP.Models
{
    public class UserSessionDto
    {
        public int UsuarioId { get; set; }
        public string UsuarioLogon { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int EmpresaId { get; set; } = 1;
        public string EmpresaNombre { get; set; } = "ARCO";
        public int EmpresaSedeId { get; set; } = 1;
        public string SedeNombre { get; set; } = "Sede Principal";
        public int PeriodoId { get; set; } = 1;
        public string PeriodoAnio { get; set; } = "2026";
        public bool IsAdmin { get; set; } = true;
    }

    public class LoginViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
        public string? ReturnUrl { get; set; }
    }

    public class PerfilAccesoViewModel
    {
        public string? ReturnUrl { get; set; }
        public int PeriodoId { get; set; }
        public int EmpresaId { get; set; }
        public int EmpresaSedeId { get; set; }

        public List<Tuple<int, string>> Periodos { get; set; } = new();
        public List<Tuple<int, string>> Empresas { get; set; } = new();
        public List<Tuple<int, string>> Sedes { get; set; } = new();
    }
}
