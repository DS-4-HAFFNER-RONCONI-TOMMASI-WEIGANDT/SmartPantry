using Volo.Abp.Modularity;

namespace TP04;

public abstract class TP04ApplicationTestBase<TStartupModule> : TP04TestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
