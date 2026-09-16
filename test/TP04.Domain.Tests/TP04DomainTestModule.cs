using Volo.Abp.Modularity;

namespace TP04;

[DependsOn(
    typeof(TP04DomainModule),
    typeof(TP04TestBaseModule)
)]
public class TP04DomainTestModule : AbpModule
{

}
