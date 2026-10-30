using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Services;

namespace AspNet10ArcoERP.Areas.IV.Controllers;

public abstract class InventarioContextController(IRHService contextService, IAuthService authService, ILogger logger, string menuUrl)
    : ModuloContextController(contextService, authService, logger, "iv", menuUrl) { }
