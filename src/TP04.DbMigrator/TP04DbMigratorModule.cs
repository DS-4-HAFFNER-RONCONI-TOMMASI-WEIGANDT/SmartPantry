using TP04.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace TP04.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(TP04EntityFrameworkCoreModule),
    typeof(TP04ApplicationContractsModule)
)]
public class TP04DbMigratorModule : AbpModule
{
}
