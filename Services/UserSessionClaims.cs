using System.Security.Claims;
using AspNet10ArcoERP.Models;

namespace AspNet10ArcoERP.Services;

public static class UserSessionClaims
{
    public static void UpdateContext(ClaimsIdentity identity, UserSessionDto session)
    {
        var values = new Dictionary<string, string>
        {
            ["EmpresaId"] = session.EmpresaId.ToString(),
            ["EmpresaNombre"] = session.EmpresaNombre,
            ["SedeId"] = session.EmpresaSedeId.ToString(),
            ["SedeNombre"] = session.SedeNombre,
            ["PeriodoId"] = session.PeriodoId.ToString(),
            ["PeriodoAnio"] = session.PeriodoAnio
        };
        foreach (var value in values)
        {
            foreach (var claim in identity.FindAll(value.Key).ToArray()) identity.RemoveClaim(claim);
            identity.AddClaim(new Claim(value.Key, value.Value));
        }
    }
}
