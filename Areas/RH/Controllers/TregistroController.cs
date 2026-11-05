using System.Text;
using AspNet10ArcoERP.Controllers;
using AspNet10ArcoERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.RH.Controllers;

[Area("RH")]
public class TregistroController(
    IRHService service,
    IAuthService auth,
    ILogger<TregistroController> logger)
    : RHContextController(service, auth, logger, "tregistro/index")
{
    [HttpGet]
    public async Task<IActionResult> Index()
        => View(await RHService.GetTRegistroDataAsync(CurrentEmpresaId));

    [HttpGet]
    public async Task<IActionResult> DescargarEstructura()
    {
        var data = await RHService.GetTRegistroDataAsync(CurrentEmpresaId);
        var sb = new StringBuilder();

        // Estructura SUNAT T-Registro: TipoDoc|NumDoc|Pais|FechaNac|ApePat|ApeMat|Nombres|Sexo|Nacionalidad|...
        foreach (var w in data.Trabajadores)
        {
            sb.AppendLine($"01|{w.NumeroDocumento}|PE||{w.NombreCompleto}||||{w.FechaIngreso:dd/MM/yyyy}|{w.TipoContratoCodigo}|{w.Cargo}");
        }

        byte[] bytes = Encoding.Latin1.GetBytes(sb.ToString());
        string fileName = $"RP_DNI_{data.Periodo}.txt";
        return File(bytes, "text/plain", fileName);
    }
}

