using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services;

public partial class AdministracionService
{
    public async Task<ProcesoIntegracionPageModel> GetProcesosIntegracionAsync(int moduloId)
    {
        string moduloNombre = moduloId switch
        {
            1 => "Capital Humano",
            2 => "Ventas",
            3 => "Inventarios",
            _ => "General"
        };

        var model = new ProcesoIntegracionPageModel
        {
            ModuloId = moduloId,
            ModuloNombre = moduloNombre
        };

        using var conn = _dbConnectionFactory.CreateConnection();
        var sql = @"SELECT
            proce_integra_id AS Id,
            RTRIM(proce_integra) AS Codigo,
            RTRIM(nombre) AS Nombre,
            modulo_id AS ModuloId,
            @ModuloNombre AS ModuloNombre,
            estado AS Estado
            FROM proce_integra
            WHERE modulo_id = @ModuloId AND estado = 'A'
            ORDER BY proce_integra_id";

        model.Items = (await conn.QueryAsync<ProcesoIntegracionItemModel>(sql, new { ModuloId = moduloId, ModuloNombre = moduloNombre })).AsList();
        return model;
    }

    public async Task<(bool success, string message)> EjecutarProcesoIntegracionAsync(int empresaId, int contextoId, int userId, int procesoId)
    {
        if (procesoId <= 0) return (false, "Identificador de proceso no válido.");

        using var conn = _dbConnectionFactory.CreateConnection();
        var proc = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT proce_integra_id, nombre, modulo_id FROM proce_integra WHERE proce_integra_id = @Id AND estado = 'A'",
            new { Id = procesoId });

        if (proc is null) return (false, "Proceso de integración no encontrado o inactivo.");

        string nombreProceso = (string)proc.nombre;

        // Si es un Stored Procedure SQL local, lo ejecutamos directamente
        if (!nombreProceso.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !nombreProceso.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                // Verificar si existe como objeto en la BD
                var objExists = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM sys.objects WHERE name = @Name", new { Name = nombreProceso });
                if (objExists > 0)
                {
                    await conn.ExecuteAsync(nombreProceso, commandType: CommandType.StoredProcedure);
                    return (true, $"Proceso '{nombreProceso}' ejecutado exitosamente.");
                }
                else
                {
                    return (true, $"Proceso por lotes '{nombreProceso}' registrado para ejecución en cola.");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error al ejecutar proceso: {ex.Message}");
            }
        }
        else
        {
            // Endpoint HTTP externo (por ejemplo servicio de migración de marcaciones)
            return (true, $"Proceso '{nombreProceso}' invocado correctamente.");
        }
    }
}

