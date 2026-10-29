using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.Account;
using Volo.Abp.Identity;
using Volo.Abp.Mapperly;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.TenantManagement;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TP04.PantryItems;
using Volo.Abp;
using Volo.Abp.BackgroundWorkers;

namespace TP04;

[DependsOn(
    typeof(TP04DomainModule),
    typeof(TP04ApplicationContractsModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpAccountApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule),
    typeof(AbpBackgroundWorkersModule)
    )]
public class TP04ApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // TP08: umbral, zona horaria y período del worker de vencimientos.
        var configuration = context.Services.GetConfiguration();
        context.Services.Configure<ExpirationWarningOptions>(
            configuration.GetSection(ExpirationWarningOptions.ConfigurationSection));
    }

    public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        // TP08 / RF-15: registro del worker periódico de vencimientos.
        // En las pruebas AbpBackgroundWorkerOptions.IsEnabled = false y no se registra.
        var workerOptions = context.ServiceProvider
            .GetRequiredService<IOptions<AbpBackgroundWorkerOptions>>().Value;

        if (!workerOptions.IsEnabled)
        {
            return;
        }

        await context.AddBackgroundWorkerAsync<ExpirationWarningWorker>();
    }
}
