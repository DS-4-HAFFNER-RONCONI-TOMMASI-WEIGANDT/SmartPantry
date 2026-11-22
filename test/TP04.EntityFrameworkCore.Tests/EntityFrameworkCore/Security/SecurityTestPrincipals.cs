using System;
using System.Collections.Generic;
using System.Security.Claims;
using Volo.Abp.Security.Claims;

namespace TP04.Security;

/// <summary>
/// TP final: usuarios de prueba para la suite de autorización real (SecurityTestModule).
/// Se usan con ICurrentPrincipalAccessor.Change(...) dentro de un using.
///
/// User() y Admin() crean la identidad con un tipo de autenticación: sin él,
/// ClaimsIdentity.IsAuthenticated es false y la autorización de ASP.NET los trata
/// como anónimos aunque tengan Id.
/// </summary>
public static class SecurityTestPrincipals
{
    public const string AuthenticationType = "SecurityTest";

    // Mismo Id que el admin de prueba de FakeCurrentPrincipalAccessor.
    public static readonly Guid AdminId = new("2e701e62-0953-4dd3-910b-dc6cc93ccb0d");

    public static readonly Guid DefaultUserId = new("5c3a8e1f-7b2d-4c6e-9a10-2f4b6d8e0a13");

    /// <summary>Sin sesión ni token: identidad vacía y no autenticada.</summary>
    public static ClaimsPrincipal Anonymous()
    {
        return new ClaimsPrincipal(new ClaimsIdentity());
    }

    /// <summary>Usuario común autenticado, sin rol admin.</summary>
    public static ClaimsPrincipal User(Guid? id = null)
    {
        return Authenticated(new List<Claim>
        {
            new(AbpClaimTypes.UserId, (id ?? DefaultUserId).ToString()),
            new(AbpClaimTypes.UserName, "user"),
            new(AbpClaimTypes.Email, "user@smartpantry.test")
        });
    }

    /// <summary>Admin autenticado con el claim de rol "admin" (recibe los permisos del rol).</summary>
    public static ClaimsPrincipal Admin()
    {
        return Authenticated(new List<Claim>
        {
            new(AbpClaimTypes.UserId, AdminId.ToString()),
            new(AbpClaimTypes.UserName, "admin"),
            new(AbpClaimTypes.Email, "admin@abp.io"),
            new(AbpClaimTypes.Role, "admin")
        });
    }

    private static ClaimsPrincipal Authenticated(List<Claim> claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationType));
    }
}
