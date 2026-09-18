using TP04.Localization;
using Volo.Abp.Application.Services;

namespace TP04;

/* Inherit your application services from this class.
 */
public abstract class TP04AppService : ApplicationService
{
    protected TP04AppService()
    {
        LocalizationResource = typeof(TP04Resource);
    }
}
