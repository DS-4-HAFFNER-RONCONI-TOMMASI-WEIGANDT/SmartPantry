using TP04.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace TP04.Permissions;

public class TP04PermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(TP04Permissions.GroupName);

        var booksPermission = myGroup.AddPermission(TP04Permissions.Books.Default, L("Permission:Books"));
        booksPermission.AddChild(TP04Permissions.Books.Create, L("Permission:Books.Create"));
        booksPermission.AddChild(TP04Permissions.Books.Edit, L("Permission:Books.Edit"));
        booksPermission.AddChild(TP04Permissions.Books.Delete, L("Permission:Books.Delete"));

        var authorsPermission = myGroup.AddPermission(TP04Permissions.Authors.Default, L("Permission:Authors"));
        authorsPermission.AddChild(TP04Permissions.Authors.Create, L("Permission:Authors.Create"));
        authorsPermission.AddChild(TP04Permissions.Authors.Edit, L("Permission:Authors.Edit"));
        authorsPermission.AddChild(TP04Permissions.Authors.Delete, L("Permission:Authors.Delete"));
        //Define your own permissions here. Example:
        //myGroup.AddPermission(TP04Permissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<TP04Resource>(name);
    }
}
