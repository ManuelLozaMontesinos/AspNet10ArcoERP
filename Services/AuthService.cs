using System;
using System.Data;
using System.Threading.Tasks;
using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services
{
    public class AuthService : IAuthService
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IPasswordService _passwordService;

        public AuthService(IDbConnectionFactory dbConnectionFactory, IPasswordService passwordService)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _passwordService = passwordService;
        }

        public async Task<(bool Success, string Message, UserSessionDto? User)> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return (false, "Ingrese usuario y contraseña.", null);
            }

            using var conn = _dbConnectionFactory.CreateConnection();

            const string userSql = @"
                SELECT TOP 1 
                    usuario_id AS UsuarioId,
                    empresa_id AS EmpresaId,
                    usuario_logon AS UsuarioLogon,
                    nombre_completo AS NombreCompleto,
                    password AS Password,
                    clave_publica AS ClavePublica,
                    email AS Email,
                    estado AS Estado
                FROM usuario
                WHERE usuario_logon = @Username AND estado = 'A'";

            var dbUser = await conn.QueryFirstOrDefaultAsync<dynamic>(userSql, new { Username = username.Trim() });

            if (dbUser == null)
            {
                return (false, "El usuario ingresado no existe o se encuentra inactivo.", null);
            }

            string dbPwd = (string)dbUser.Password;
            byte[] dbPubKey = (byte[])dbUser.ClavePublica;

            bool isValid = _passwordService.VerifyPassword(password.Trim(), dbPwd, dbPubKey);
            if (!isValid)
            {
                return (false, "Contraseña incorrecta.", null);
            }

            int usuarioId = (int)dbUser.UsuarioId;
            var session = await GetUserSessionAsync(usuarioId);

            return (true, "Inicio de sesión exitoso.", session);
        }

        public async Task<PerfilAccesoViewModel> GetPerfilAccesoDataAsync(int usuarioId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var model = new PerfilAccesoViewModel();

            var periodos = await conn.QueryAsync<(int Id, string Nombre)>(
                "SELECT periodo_id, CAST(periodo AS VARCHAR) FROM periodo WHERE estado = 'A' ORDER BY periodo DESC");
            foreach (var p in periodos) model.Periodos.Add(Tuple.Create(p.Id, p.Nombre));

            var empresas = await conn.QueryAsync<(int Id, string Nombre)>(
                "SELECT empresa_id, ISNULL(nombre_com, nombre) FROM empresa WHERE estado = 'A'");
            foreach (var e in empresas) model.Empresas.Add(Tuple.Create(e.Id, e.Nombre));

            var sedes = await conn.QueryAsync<(int Id, string Nombre)>(
                "SELECT empresa_sede_id, nombre FROM empresa_sede WHERE estado = 'A'");
            foreach (var s in sedes) model.Sedes.Add(Tuple.Create(s.Id, s.Nombre));

            return model;
        }

        public async Task<UserSessionDto?> GetUserSessionAsync(int usuarioId, int? periodoId = null, int? empresaId = null, int? sedeId = null)
        {
            using var conn = _dbConnectionFactory.CreateConnection();

            var u = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT TOP 1 
                    usuario_id AS UsuarioId,
                    usuario_logon AS UsuarioLogon,
                    nombre_completo AS NombreCompleto,
                    ISNULL(email, '') AS Email
                FROM usuario 
                WHERE usuario_id = @Id", new { Id = usuarioId });

            if (u == null) return null;

            // Default period (2026 / latest)
            int pId = periodoId ?? await conn.ExecuteScalarAsync<int>(
                "SELECT TOP 1 periodo_id FROM periodo WHERE estado = 'A' ORDER BY periodo DESC");
            string pAnio = await conn.ExecuteScalarAsync<string>(
                "SELECT TOP 1 CAST(periodo AS VARCHAR) FROM periodo WHERE periodo_id = @PId", new { PId = pId }) ?? "2026";

            // Default company
            int empId = empresaId ?? await conn.ExecuteScalarAsync<int>(
                "SELECT TOP 1 empresa_id FROM empresa WHERE estado = 'A' ORDER BY empresa_id");
            string empNombre = await conn.ExecuteScalarAsync<string>(
                "SELECT TOP 1 ISNULL(nombre_com, nombre) FROM empresa WHERE empresa_id = @EId", new { EId = empId }) ?? "ARCO";

            // Default sede
            int sId = sedeId ?? await conn.ExecuteScalarAsync<int>(
                "SELECT TOP 1 empresa_sede_id FROM empresa_sede WHERE empresa_id = @EId AND estado = 'A' ORDER BY empresa_sede_id", new { EId = empId });
            string sNombre = await conn.ExecuteScalarAsync<string>(
                "SELECT TOP 1 nombre FROM empresa_sede WHERE empresa_sede_id = @SId", new { SId = sId }) ?? "Sede Principal";

            return new UserSessionDto
            {
                UsuarioId = (int)u.UsuarioId,
                UsuarioLogon = (string)u.UsuarioLogon,
                NombreCompleto = (string)u.NombreCompleto,
                Email = (string)u.Email,
                EmpresaId = empId,
                EmpresaNombre = empNombre,
                EmpresaSedeId = sId,
                SedeNombre = sNombre,
                PeriodoId = pId,
                PeriodoAnio = pAnio,
                IsAdmin = true
            };
        }
    }
}
