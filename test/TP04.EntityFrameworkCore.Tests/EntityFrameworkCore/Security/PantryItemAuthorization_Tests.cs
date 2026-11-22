using System;
using System.Threading.Tasks;
using Shouldly;
using TP04.PantryItems;
using TP04.Products;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Claims;
using Xunit;

namespace TP04.Security;

/// <summary>
/// TP final (RF-10 / RF-11 / RF-14): la despensa es privada. Con la autorización
/// real (SecurityTestModule) se comprueba que un anónimo es rechazado y que un
/// usuario común lista solo sus propios ítems (regla de negocio 3).
/// </summary>
[Collection(TP04TestConsts.CollectionDefinitionName)]
public class PantryItemAuthorization_Tests : TP04TestBase<SecurityTestModule>
{
    private static readonly Guid OtherUserId = new("8d2f4a6b-1c3e-4f5a-9b7d-0e2c4a6f8b13");

    private readonly IPantryItemAppService _pantryItemAppService;
    private readonly ICurrentPrincipalAccessor _principalAccessor;

    public PantryItemAuthorization_Tests()
    {
        _pantryItemAppService = GetRequiredService<IPantryItemAppService>();
        _principalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    [Fact]
    public async Task GetListAsync_Should_Throw_AbpAuthorizationException_When_Anonymous()
    {
        using (_principalAccessor.Change(SecurityTestPrincipals.Anonymous()))
        {
            await Should.ThrowAsync<AbpAuthorizationException>(
                () => _pantryItemAppService.GetListAsync(new GetPantryItemListDto()));
        }
    }

    [Fact]
    public async Task GetListAsync_Should_Return_Own_Items_When_User_Is_Authenticated()
    {
        var userId = SecurityTestPrincipals.DefaultUserId;
        await InsertPantryItemAsync(userId, "7790000020001", "Leche");
        await InsertPantryItemAsync(OtherUserId, "7790000020002", "Yerba");

        using (_principalAccessor.Change(SecurityTestPrincipals.User(userId)))
        {
            var result = await _pantryItemAppService.GetListAsync(new GetPantryItemListDto());

            result.TotalCount.ShouldBe(1);
            result.Items.ShouldHaveSingleItem().ProductName.ShouldBe("Leche");
        }
    }

    private Task InsertPantryItemAsync(Guid ownerId, string barcode, string productName)
    {
        // Datos cargados directo por repositorio: no dependen de la autorización.
        return WithUnitOfWorkAsync(async () =>
        {
            var product = await GetRequiredService<ProductManager>().CreateAsync(barcode, productName);
            await GetRequiredService<IRepository<Product, Guid>>().InsertAsync(product, autoSave: true);

            var item = new PantryItem(Guid.NewGuid(), ownerId, product.Id);
            await GetRequiredService<IRepository<PantryItem, Guid>>().InsertAsync(item, autoSave: true);
        });
    }
}
