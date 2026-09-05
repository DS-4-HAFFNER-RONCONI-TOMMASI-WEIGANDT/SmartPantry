using Volo.Abp.Modularity;

namespace TP04;

[DependsOn(
    typeof(TP04ApplicationModule),
    typeof(TP04DomainTestModule)
)]
public class TP04ApplicationTestModule : AbpModule
{

}
