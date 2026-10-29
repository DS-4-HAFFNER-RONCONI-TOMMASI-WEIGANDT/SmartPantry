using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Modularity;

namespace TP04;

[DependsOn(
    typeof(TP04ApplicationModule),
    typeof(TP04DomainTestModule)
)]
public class TP04ApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // TP08: el worker periódico no se arranca en las pruebas. La regla se prueba
        // llamando directamente a ExpirationWarningManager con una fecha fija, así
        // la suite (y CI) no espera ningún temporizador.
        Configure<AbpBackgroundWorkerOptions>(options =>
        {
            options.IsEnabled = false;
        });
    }
}
