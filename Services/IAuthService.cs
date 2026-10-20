using System.Threading.Tasks;
using AspNet10ArcoERP.Models;

namespace AspNet10ArcoERP.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Message, UserSessionDto? User)> AuthenticateAsync(string username, string password);
        Task<PerfilAccesoViewModel> GetPerfilAccesoDataAsync(int usuarioId);
        Task<UserSessionDto?> GetUserSessionAsync(int usuarioId, int? periodoId = null, int? empresaId = null, int? sedeId = null);
    }
}
