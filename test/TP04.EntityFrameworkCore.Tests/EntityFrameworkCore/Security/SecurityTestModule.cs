using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TP04.EntityFrameworkCore;
using Volo.Abp.Authorization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Modularity;

namespace TP04.Security;

/// <summary>
/// TP final: módulo de pruebas con la autorización REAL de ABP.
///
/// Por qué existe: la suite normal (TP04TestBaseModule) llama a
/// AddAlwaysAllowAuthorization(), que reemplaza la autorización por versiones que
/// aprueban todo. Así se prueban las reglas de negocio sin preocuparse por permisos,
/// pero ninguna prueba comprueba [Authorize] ni las políticas. Este módulo vuelve a
/// poner las implementaciones reales para probar el rechazo del acceso anónimo y la
/// diferencia entre usuario común y admin.
///
/// Depende de TP04EntityFrameworkCoreTestModule (misma base SQLite en memoria). ABP
/// configura los módulos dependientes primero, así que estos reemplazos pisan a los
/// de AddAlwaysAllowAuthorization().
/// </summary>
[DependsOn(typeof(TP04EntityFrameworkCoreTestModule))]
public class SecurityTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Replace(ServiceDescriptor.Transient<IAuthorizationService, AbpAuthorizationService>());
        context.Services.Replace(ServiceDescriptor.Transient<IAbpAuthorizationService, AbpAuthorizationService>());
        context.Services.Replace(ServiceDescriptor.Transient<IMethodInvocationAuthorizationService, MethodInvocationAuthorizationService>());
        context.Services.Replace(ServiceDescriptor.Transient<IPermissionChecker, PermissionChecker>());
    }
}
