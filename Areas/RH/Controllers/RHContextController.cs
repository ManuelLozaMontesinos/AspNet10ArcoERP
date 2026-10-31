using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Services;

namespace AspNet10ArcoERP.Areas.RH.Controllers;

public abstract class RHContextController(IRHService service, IAuthService auth, ILogger logger, string menuUrl)
    : ModuloContextController(service, auth, logger, "rh", menuUrl)
{
    protected IRHService RHService { get; } = service;
    protected int CurrentUserId => UsuarioId;
    protected int CurrentEmpresaId => EmpresaId;
    protected int CurrentPeriodoEmpSedeId => PeriodoEmpresaSedeId;
}
