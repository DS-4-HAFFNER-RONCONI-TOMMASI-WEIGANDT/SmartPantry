using Volo.Abp.Modularity;

namespace TP04;

/* Inherit from this class for your domain layer tests. */
public abstract class TP04DomainTestBase<TStartupModule> : TP04TestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
