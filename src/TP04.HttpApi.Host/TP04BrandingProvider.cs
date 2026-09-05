using Microsoft.Extensions.Localization;
using TP04.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace TP04;

[Dependency(ReplaceServices = true)]
public class TP04BrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<TP04Resource> _localizer;

    public TP04BrandingProvider(IStringLocalizer<TP04Resource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
