using System.Data;
using AspNet10ArcoERP.Models;
using Dapper;
using static AspNet10ArcoERP.Services.MaestroEmpresaSql;

namespace AspNet10ArcoERP.Services;

public partial class AdministracionService
{
    private static readonly PasswordService _passwordService = new();

    public async Task<UsuarioPageModel> GetUsuariosAsync(
        int empresaId,
        string? search = null,
        string? estado = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new UsuarioPageModel
        {
            Search = search,
            Estado = State(estado)
        };

        var columns = new Dictionary<string, string>
        {
            ["logon"] = "u.usuario_logon",
            ["nombre"] = "u.nombre_completo",
            ["email"] = "u.email",
            ["estado"] = "u.estado"
        };
        model.Ordenar(sort, dir, columns.Keys, "nombre");
        model.Paginar(0, page, top);

        if (empresaId <= 0)
        {
            model.Errores.Add("Seleccione una empresa válida.");
            return model;
        }

        using var conn = _dbConnectionFactory.CreateConnection();

        var where = new List<string> { "(u.empresa_id = @EmpresaId OR u.empresa_id IS NULL)" };
        var args = new DynamicParameters();
        args.Add("EmpresaId", empresaId);

        if (!string.IsNullOrWhiteSpace(model.Estado))
        {
            where.Add("u.estado = @Estado");
            args.Add("Estado", model.Estado);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(u.usuario_logon LIKE @Search OR u.nombre_completo LIKE @Search OR u.email LIKE @Search)");
            args.Add("Search", Search(search));
        }

        string from = " FROM usuario u WHERE " + string.Join(" AND ", where);

        int total = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*)" + from, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        string sql = @"SELECT u.usuario_id Id,
                              RTRIM(u.usuario_logon) UsuarioLogon,
                              RTRIM(u.nombre_completo) NombreCompleto,
                              RTRIM(u.email) Email,
                              RTRIM(u.email_alt) EmailAlterno,
                              CAST(CASE WHEN u.cambiar_psw_proximo = 'S' THEN 1 ELSE 0 END AS bit) CambiarPswProximo,
                              CAST(CASE WHEN u.no_puede_cambiar_psw = 'S' THEN 1 ELSE 0 END AS bit) NoPuedeCambiarPsw,
                              CAST(CASE WHEN u.no_expira = 'S' THEN 1 ELSE 0 END AS bit) NoExpira,
                              u.fecha_expira FechaExpira,
                              CAST(CASE WHEN u.tiene_horario = 'S' THEN 1 ELSE 0 END AS bit) TieneHorario,
                              u.estado Estado,
                              (SELECT COUNT(1) FROM rol_usuario ru WHERE ru.usuario_id = u.usuario_id) CantidadRoles"
                     + from
                     + $" ORDER BY {columns[model.Sort]} {model.Dir}, u.usuario_id DESC "
                     + "OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";

        var rows = (await conn.QueryAsync<UsuarioItemModel>(sql, args)).AsList();
        model.Items = rows;
        model.VisibleCount = rows.Count;
        return model;
    }

    public async Task<UsuarioDetalleModel?> GetUsuarioDetalleAsync(int empresaId, int id)
    {
        if (empresaId <= 0) return null;

        using var conn = _dbConnectionFactory.CreateConnection();

        var detalle = new UsuarioDetalleModel
        {
            RolesDisponibles = (await conn.QueryAsync<LookupItemModel>(
                @"SELECT r.rol_id Id, RTRIM(r.nombre) Descripcion, RTRIM(r.rol) Codigo
                  FROM rol r
                  WHERE (r.empresa_id = @EmpresaId OR r.empresa_id IS NULL) AND r.estado = 'A'
                  ORDER BY r.nombre", new { EmpresaId = empresaId })).AsList()
        };

        if (id > 0)
        {
            var user = await conn.QuerySingleOrDefaultAsync<UsuarioItemModel>(
                @"SELECT u.usuario_id Id,
                         RTRIM(u.usuario_logon) UsuarioLogon,
                         RTRIM(u.nombre_completo) NombreCompleto,
                         RTRIM(u.email) Email,
                         RTRIM(u.email_alt) EmailAlterno,
                         CAST(CASE WHEN u.cambiar_psw_proximo = 'S' THEN 1 ELSE 0 END AS bit) CambiarPswProximo,
                         CAST(CASE WHEN u.no_puede_cambiar_psw = 'S' THEN 1 ELSE 0 END AS bit) NoPuedeCambiarPsw,
                         CAST(CASE WHEN u.no_expira = 'S' THEN 1 ELSE 0 END AS bit) NoExpira,
                         u.fecha_expira FechaExpira,
                         CAST(CASE WHEN u.tiene_horario = 'S' THEN 1 ELSE 0 END AS bit) TieneHorario,
                         u.estado Estado
                  FROM usuario u
                  WHERE u.usuario_id = @Id", new { Id = id });

            if (user == null) return null;
            detalle.Datos = user;

            detalle.RolesAsignados = (await conn.QueryAsync<int>(
                "SELECT ru.rol_id FROM rol_usuario ru WHERE ru.usuario_id = @Id",
                new { Id = id })).AsList();
        }
        else
        {
            detalle.Datos = new UsuarioItemModel
            {
                NoExpira = true,
                Estado = "A"
            };
        }

        return detalle;
    }

    public async Task<(bool success, string message, int id)> SaveUsuarioAsync(int empresaId, int userId, UsuarioGuardarSolicitud model)
    {
        if (empresaId <= 0 || userId <= 0)
            return (false, "Contexto de empresa o usuario no válido.", 0);

        using var conn = _dbConnectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            if (model.Id > 0)
            {
                await conn.ExecuteAsync("sp_usuario_actualizar", new
                {
                    usuarioId = model.Id,
                    nombreCompleto = Text(model.NombreCompleto),
                    cambiarContrasenaProximo = model.CambiarPswProximo ? "S" : "N",
                    noPuedeCambiarContrasena = "N",
                    noExpira = model.NoExpira ? "S" : "N",
                    fechaExpiracion = model.FechaExpira,
                    email = Text(model.Email),
                    emailAlterno = "",
                    tieneHorario = "N"
                }, tx, commandType: CommandType.StoredProcedure);

                // Roles sincronización
                await conn.ExecuteAsync(
                    "DELETE FROM rol_usuario WHERE usuario_id = @Id",
                    new { Id = model.Id }, tx);

                foreach (var rId in model.RolesIds)
                {
                    await InsertAsync(conn, tx, "sp_rol_usuario_insertar", new
                    {
                        rolId = rId,
                        usuarioId = model.Id
                    });
                }

                tx.Commit();
                return (true, $"Usuario {model.UsuarioLogon} actualizado correctamente.", model.Id);
            }
            else
            {
                // Validación de login único
                int dup = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM usuario WHERE usuario_logon = @Logon",
                    new { Logon = Text(model.UsuarioLogon) }, tx);

                if (dup > 0)
                {
                    tx.Rollback();
                    return (false, $"El nombre de usuario '{model.UsuarioLogon}' ya existe en el sistema.", 0);
                }

                string enc = _passwordService.EncryptPassword(model.Password ?? "123456", out byte[] pubKey);

                int newId = await InsertAsync(conn, tx, "sp_usuario_insertar", new
                {
                    usuarioLogon = Text(model.UsuarioLogon),
                    nombreCompleto = Text(model.NombreCompleto),
                    password = enc,
                    clavePublica = pubKey,
                    cambiarContrasenaProximo = model.CambiarPswProximo ? "S" : "N",
                    noPuedeCambiarContrasena = "N",
                    noExpira = model.NoExpira ? "S" : "N",
                    fechaExpiracion = model.FechaExpira,
                    email = Text(model.Email),
                    emailAlterno = "",
                    codigoRecuperacionContrasena = "",
                    tieneHorario = "N",
                    origenRegistroId = 1
                });

                await conn.ExecuteAsync(
                    "UPDATE usuario SET empresa_id = @EmpresaId WHERE usuario_id = @NewId",
                    new { EmpresaId = empresaId, NewId = newId }, tx);

                foreach (var rId in model.RolesIds)
                {
                    await InsertAsync(conn, tx, "sp_rol_usuario_insertar", new
                    {
                        rolId = rId,
                        usuarioId = newId
                    });
                }

                tx.Commit();
                return (true, $"Usuario {model.UsuarioLogon} registrado exitosamente.", newId);
            }
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, "Error al guardar usuario: " + ex.Message, 0);
        }
    }

    public async Task<(bool success, string message)> CambiarEstadoUsuarioAsync(int id, string nuevoEstado)
    {
        if (id <= 0 || (nuevoEstado != "A" && nuevoEstado != "I"))
            return (false, "Parámetros inválidos.");

        using var conn = _dbConnectionFactory.CreateConnection();
        try
        {
            await conn.ExecuteAsync("sp_usuario_actualizarEstado", new
            {
                usuarioId = id,
                estado = nuevoEstado
            }, commandType: CommandType.StoredProcedure);

            string action = nuevoEstado == "A" ? "habilitado" : "deshabilitado";
            return (true, $"Usuario {action} correctamente.");
        }
        catch (Exception ex)
        {
            return (false, "Error al cambiar estado del usuario: " + ex.Message);
        }
    }

    public async Task<(bool success, string message)> CambiarContrasenaUsuarioAsync(UsuarioCambiarContrasenaSolicitud model)
    {
        if (model.UsuarioId <= 0 || string.IsNullOrWhiteSpace(model.NuevaContrasena))
            return (false, "Datos de contraseña no válidos.");

        using var conn = _dbConnectionFactory.CreateConnection();
        try
        {
            string enc = _passwordService.EncryptPassword(model.NuevaContrasena, out byte[] pubKey);

            await conn.ExecuteAsync("sp_usuario_actualizarContrasena", new
            {
                usuarioId = model.UsuarioId,
                password = enc,
                cambiarContrasenaProximo = model.ForzarCambioProximo ? "S" : "N"
            }, commandType: CommandType.StoredProcedure);

            await conn.ExecuteAsync(
                "UPDATE usuario SET clave_publica = @PubKey WHERE usuario_id = @Id",
                new { PubKey = pubKey, Id = model.UsuarioId });

            return (true, "Contraseña actualizada exitosamente.");
        }
        catch (Exception ex)
        {
            return (false, "Error al actualizar contraseña: " + ex.Message);
        }
    }

    public async Task<RolPageModel> GetRolesAsync(
        int empresaId,
        string? search = null,
        string? estado = null,
        int page = 1,
        int top = 25,
        string? sort = null,
        string? dir = null)
    {
        var model = new RolPageModel
        {
            Search = search,
            Estado = State(estado)
        };

        var columns = new Dictionary<string, string>
        {
            ["codigo"] = "r.rol",
            ["nombre"] = "r.nombre",
            ["estado"] = "r.estado"
        };
        model.Ordenar(sort, dir, columns.Keys, "nombre");
        model.Paginar(0, page, top);

        if (empresaId <= 0)
        {
            model.Errores.Add("Seleccione una empresa válida.");
            return model;
        }

        using var conn = _dbConnectionFactory.CreateConnection();

        var where = new List<string> { "(r.empresa_id = @EmpresaId OR r.empresa_id IS NULL)" };
        var args = new DynamicParameters();
        args.Add("EmpresaId", empresaId);

        if (!string.IsNullOrWhiteSpace(model.Estado))
        {
            where.Add("r.estado = @Estado");
            args.Add("Estado", model.Estado);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(r.rol LIKE @Search OR r.nombre LIKE @Search)");
            args.Add("Search", Search(search));
        }

        string from = " FROM rol r WHERE " + string.Join(" AND ", where);

        int total = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*)" + from, args);
        model.Paginar(total, page, top);

        args.Add("Offset", (model.Page - 1) * model.PageSize);
        args.Add("Top", model.PageSize);

        string sql = @"SELECT r.rol_id Id,
                              RTRIM(r.rol) Codigo,
                              RTRIM(r.nombre) Nombre,
                              r.estado Estado,
                              (SELECT COUNT(1) FROM rol_usuario ru WHERE ru.rol_id = r.rol_id) CantidadUsuarios"
                     + from
                     + $" ORDER BY {columns[model.Sort]} {model.Dir}, r.rol_id DESC "
                     + "OFFSET @Offset ROWS FETCH NEXT @Top ROWS ONLY";

        var rows = (await conn.QueryAsync<RolItemModel>(sql, args)).AsList();
        model.Items = rows;
        model.VisibleCount = rows.Count;
        return model;
    }

    public async Task<(bool success, string message, int id)> SaveRolAsync(int empresaId, int userId, RolGuardarSolicitud model)
    {
        if (empresaId <= 0 || userId <= 0)
            return (false, "Contexto no válido.", 0);

        using var conn = _dbConnectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            if (model.Id > 0)
            {
                await conn.ExecuteAsync("sp_rol_actualizar", new
                {
                    rolId = model.Id,
                    nombre = Text(model.Nombre),
                    usuarioAccionId = userId
                }, tx, commandType: CommandType.StoredProcedure);

                tx.Commit();
                return (true, $"Rol {model.Nombre} actualizado correctamente.", model.Id);
            }
            else
            {
                int newId = await InsertAsync(conn, tx, "sp_rol_insertar", new
                {
                    empresaId = empresaId,
                    rol = Text(model.Codigo).ToUpperInvariant(),
                    nombre = Text(model.Nombre),
                    origenRegistroId = 1,
                    usuarioAccionId = userId
                });

                tx.Commit();
                return (true, $"Rol {model.Nombre} registrado exitosamente.", newId);
            }
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, "Error al guardar rol: " + ex.Message, 0);
        }
    }

    public async Task<(bool success, string message)> AnularRolAsync(int id, int userId)
    {
        if (id <= 0 || userId <= 0)
            return (false, "Identificador no válido.");

        using var conn = _dbConnectionFactory.CreateConnection();
        try
        {
            await conn.ExecuteAsync("sp_rol_eliminar", new
            {
                rolId = id,
                usuarioAccionId = userId
            }, commandType: CommandType.StoredProcedure);

            return (true, "Rol anulado correctamente.");
        }
        catch (Exception ex)
        {
            return (false, "Error al anular rol: " + ex.Message);
        }
    }

    public async Task<List<RolMenuPermisoModel>> GetRolPermisosAsync(int rolId)
    {
        if (rolId <= 0) return new List<RolMenuPermisoModel>();

        using var conn = _dbConnectionFactory.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(
            "sp_rol_menu_listarPorRolId",
            new { rolId },
            commandType: CommandType.StoredProcedure);

        var list = new List<RolMenuPermisoModel>();
        foreach (var r in rows)
        {
            list.Add(new RolMenuPermisoModel
            {
                MenuId = Convert.ToInt32(r.menu_id),
                Nombre = (string)r.nombre ?? "",
                Area = (string)r.area ?? "",
                Url = (string)r.url ?? "",
                Icono = (string)r.icono,
                MenuPadreId = r.menu_padre_id != null ? Convert.ToInt32(r.menu_padre_id) : null,
                Orden = Convert.ToInt32(r.orden ?? 0),
                Lectura = string.Equals((string)r.lectura, "S", StringComparison.OrdinalIgnoreCase),
                Escritura = string.Equals((string)r.escritura, "S", StringComparison.OrdinalIgnoreCase)
            });
        }
        return list;
    }

    public async Task<(bool success, string message)> SaveRolPermisosAsync(int rolId, List<RolMenuPermisoItem> permisos)
    {
        if (rolId <= 0) return (false, "Identificador de rol no válido.");

        using var conn = _dbConnectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            await conn.ExecuteAsync("DELETE FROM rol_menu WHERE rol_id = @RolId", new { RolId = rolId }, tx);

            foreach (var p in permisos)
            {
                if (p.Lectura || p.Escritura)
                {
                    await conn.ExecuteAsync(@"
                        INSERT INTO rol_menu (rol_menu_id, rol_id, menu_id, lectura, escritura)
                        VALUES (NEXT VALUE FOR rol_menu_seq, @RolId, @MenuId, @Lectura, @Escritura)",
                        new
                        {
                            RolId = rolId,
                            MenuId = p.MenuId,
                            Lectura = p.Lectura ? "S" : "N",
                            Escritura = p.Escritura ? "S" : "N"
                        }, tx);
                }
            }

            tx.Commit();
            return (true, "Permisos del rol guardados exitosamente.");
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, "Error al guardar permisos: " + ex.Message);
        }
    }
}

