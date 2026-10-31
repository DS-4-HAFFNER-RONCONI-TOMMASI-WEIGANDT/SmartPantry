using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Shouldly;
using TP04.ExternalProducts;
using TP04.PantryItems;
using TP04.Products;
using Xunit;

namespace TP04.Security
{
    /// <summary>
    /// TP09: comprueba la configuración de seguridad declarada en los AppServices.
    ///
    /// Las pruebas de AppServices de la suite usan AlwaysAllowAuthorizationService, que
    /// omite las restricciones de autorización. Por eso estas pruebas revisan los
    /// atributos por reflexión: si alguien vuelve a agregar [AllowAnonymous] o quita
    /// [Authorize], falla el CI. No reemplazan la matriz HTTP (401/403/200) del PR.
    /// </summary>
    public class AppServiceAuthorization_Tests
    {
        [Theory]
        [InlineData(typeof(ExternalProductLookupAppService))] // RF-05
        [InlineData(typeof(ProductAppService))]               // RF-08
        public void Own_AppServices_Should_Require_Authentication(Type appServiceType)
        {
            var authorize = appServiceType.GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToList();

            authorize.ShouldNotBeEmpty();
            appServiceType.IsDefined(typeof(AllowAnonymousAttribute), inherit: true).ShouldBeFalse();
        }

        [Theory]
        [InlineData(typeof(ExternalProductLookupAppService))] // RF-05
        [InlineData(typeof(ProductAppService))]               // RF-08
        public void Own_AppServices_Should_Not_Require_Admin_Permissions(Type appServiceType)
        {
            // [Authorize] sin política: alcanza con estar autenticado, así el usuario
            // común conserva la consulta (RF-05) y el guardado (RF-08).
            var authorize = appServiceType.GetCustomAttributes<AuthorizeAttribute>(inherit: true);

            authorize.ShouldAllBe(a => string.IsNullOrEmpty(a.Policy) && string.IsNullOrEmpty(a.Roles));
        }

        [Theory]
        [InlineData(typeof(ExternalProductLookupAppService))] // RF-05
        [InlineData(typeof(ProductAppService))]               // RF-08
        public void Own_AppService_Methods_Should_Not_Allow_Anonymous(Type appServiceType)
        {
            // Incluye los métodos heredados de CrudAppService (GetAsync, GetListAsync, DeleteAsync).
            var anonymousMethods = appServiceType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.IsDefined(typeof(AllowAnonymousAttribute), inherit: true))
                .Select(m => m.Name)
                .ToList();

            anonymousMethods.ShouldBeEmpty();
        }

        [Fact]
        public void Expiration_Worker_Should_Not_Require_Authentication()
        {
            // TP08: el worker corre sin sesión interactiva; no debe tener [Authorize].
            typeof(ExpirationWarningWorker)
                .IsDefined(typeof(AuthorizeAttribute), inherit: true)
                .ShouldBeFalse();
        }
    }
}
