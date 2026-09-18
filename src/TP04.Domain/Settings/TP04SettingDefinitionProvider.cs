using Volo.Abp.Settings;

namespace TP04.Settings;

public class TP04SettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(TP04Settings.MySetting1));
    }
}
