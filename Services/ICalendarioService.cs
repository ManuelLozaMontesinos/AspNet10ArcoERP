using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNet10ArcoERP.Models;

namespace AspNet10ArcoERP.Services
{
    public interface ICalendarioService
    {
        Task<List<CalendarioAreaModel>> GetAreasUsuarioAsync(int usuarioId, bool esAdmin);
        Task<List<object>> GetEventosAreaAsync(int areaId, int usuarioId, bool esAdmin);
        Task<(bool Success, int EventoId, string Message)> GuardarEventoAsync(EventoInputModel model, int usuarioId, string usuarioLogon, bool esAdmin);
        Task<(bool Success, string Message)> MoverOActualizarEventoAsync(MoverEventoInputModel model, int usuarioId, bool esAdmin);
        Task<(bool Success, string Message)> EliminarEventoAsync(int id, int usuarioId, bool esAdmin);
        Task<List<CalendarioEtiquetaModel>> GetEtiquetasAreaAsync(int areaId, int usuarioId, bool esAdmin);
        Task<(bool Success, string Message)> GuardarEtiquetaAreaAsync(EtiquetaInputModel model, int usuarioId, bool esAdmin);
        
        // Administracion
        Task<List<CalendarioAreaModel>> AdminGetAreasAsync();
        Task<(bool Success, string Message, int AreaId)> AdminGuardarAreaAsync(AreaAdminInputModel model);
        Task<(bool Success, string Message)> AdminCambiarEstadoAreaAsync(int areaId, string estado);
        Task<(bool Success, string Message)> AdminEliminarAreaAsync(int areaId);
        Task<(string AreaNombre, string AreaColor, List<UsuarioAreaModel> Asignados, List<UsuarioAreaModel> Disponibles)> AdminGetUsuariosAreaAsync(int areaId);
        Task<(bool Success, string Message)> AdminAsignarUsuarioAreaAsync(int usuarioId, int areaId, bool esPrincipal);
        Task<(bool Success, string Message)> AdminRemoverUsuarioAreaAsync(int usuarioId, int areaId);
        Task<(bool Success, string Message)> AdminToggleUsuarioPrincipalAsync(int usuarioId, int areaId);
    }
}

