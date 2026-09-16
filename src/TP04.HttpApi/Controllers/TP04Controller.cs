using TP04.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace TP04.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class TP04Controller : AbpControllerBase
{
    protected TP04Controller()
    {
        LocalizationResource = typeof(TP04Resource);
    }
}
