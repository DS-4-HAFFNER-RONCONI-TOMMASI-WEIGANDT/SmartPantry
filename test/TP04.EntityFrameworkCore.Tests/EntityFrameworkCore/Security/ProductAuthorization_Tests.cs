using System;
using System.Threading.Tasks;
using Shouldly;
using TP04.PantryItems;
using TP04.Permissions;
using TP04.Products;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Security.Claims;
using Xunit;

namespace TP04.Security;

/// <summary>
/// TP final (RF-08): autorización real de ProductAppService (SecurityTestModule).
///
///  - Anónimo: rechazado en todos los métodos, incluidos los heredados de
///    CrudAppService (GetAsync, GetListAsync, DeleteAsync).
///  - Usuario común: crea, consulta y lista (catálogo compartido), pero no edita
///    ni borra productos que pueden estar usando otras despensas.
///  - Admin con TP04.Products.Manage: edita y borra.
///
/// Cada prueba carga un producto real, así un método sin autorización devuelve
/// datos (y la prueba falla) en lugar de terminar en un 404 que lo disimule.
/// </summary>
[Collection(TP04TestConsts.CollectionDefinitionName)]
public class ProductAuthorization_Tests : TP04TestBase<SecurityTestModule>
{
    private readonly IProductAppService _productAppService;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly ICurrentPrincipalAccessor _principalAccessor;

    public ProductAuthorization_Tests()
    {
        _productAppService = GetRequiredService<IProductAppService>();
        _productRepository = GetRequiredService<IRepository<Product, Guid>>();
        _principalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    // ---------- Anónimo ----------

    [Fact]
    public async Task CreateAsync_Should_Throw_AbpAuthorizationException_When_Anonymous()
    {
        using (_principalAccessor.Change(SecurityTestPrincipals.Anonymous()))
        {
            await Should.ThrowAsync<AbpAuthorizationException>(
                () => _productAppService.CreateAsync(NewCreateDto("7790000030001")));
        }
    }

    [Fact]
    public async Task GetAsync_Should_Throw_AbpAuthorizationException_When_Anonymous()
    {
        var productId = await InsertProductAsync("7790000030002");

        using (_principalAccessor.Change(SecurityTestPrincipals.Anonymous()))
        {
            await Should.ThrowAsync<AbpAuthorizationException>(
                () => _productAppService.GetAsync(productId));
        }
    }

    [Fact]
    public async Task GetListAsync_Should_Throw_AbpAuthorizationException_When_Anonymous()
    {
        await InsertProductAsync("7790000030003");

        using (_principalAccessor.Change(SecurityTestPrincipals.Anonymous()))
        {
            await Should.ThrowAsync<AbpAuthorizationException>(
                () => _productAppService.GetListAsync(new PagedAndSortedResultRequestDto()));
        }
    }

    [Fact]
    public async Task UpdateAsync_Should_Throw_AbpAuthorizationException_When_Anonymous()
    {
        var productId = await InsertProductAsync("7790000030004");

        using (_principalAccessor.Change(SecurityTestPrincipals.Anonymous()))
        {
            await Should.ThrowAsync<AbpAuthorizationException>(
                () => _productAppService.UpdateAsync(productId, NewUpdateDto("7790000030004")));
        }
    }

    [Fact]
    public async Task DeleteAsync_Should_Throw_AbpAuthorizationException_When_Anonymous()
    {
        var productId = await InsertProductAsync("7790000030005");

        using (_principalAccessor.Change(SecurityTestPrincipals.Anonymous()))
        {
            await Should.ThrowAsync<AbpAuthorizationException>(
                () => _productAppService.DeleteAsync(productId));
        }

        (await ProductExistsAsync(productId)).ShouldBeTrue();
    }

    // ---------- Usuario común ----------

    [Fact]
    public async Task Create_Get_And_GetList_Should_Succeed_When_User_Is_Authenticated()
    {
        using (_principalAccessor.Change(SecurityTestPrincipals.User()))
        {
            var created = await _productAppService.CreateAsync(NewCreateDto("7790000030006"));

            var fetched = await _productAppService.GetAsync(created.Id);
            fetched.Barcode.ShouldBe("7790000030006");

            var list = await _productAppService.GetListAsync(new PagedAndSortedResultRequestDto());
            list.Items.ShouldContain(p => p.Id == created.Id);
        }
    }

    [Fact]
    public async Task UpdateAsync_Should_Throw_AbpAuthorizationException_When_User_Lacks_Manage_Permission()
    {
        var productId = await InsertProductAsync("7790000030007");

        using (_principalAccessor.Change(SecurityTestPrincipals.User()))
        {
            await Should.ThrowAsync<AbpAuthorizationException>(
                () => _productAppService.UpdateAsync(productId, NewUpdateDto("7790000030007")));
        }
    }

    [Fact]
    public async Task DeleteAsync_Should_Throw_AbpAuthorizationException_When_User_Lacks_Manage_Permission()
    {
        var productId = await InsertProductAsync("7790000030008");

        using (_principalAccessor.Change(SecurityTestPrincipals.User()))
        {
            await Should.ThrowAsync<AbpAuthorizationException>(
                () => _productAppService.DeleteAsync(productId));
        }

        (await ProductExistsAsync(productId)).ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteAsync_Should_Throw_AbpAuthorizationException_Not_ProductInUse_When_User_Lacks_Manage_Permission()
    {
        // El permiso se controla antes que la regla "producto en uso": un usuario sin
        // Products.Manage no tiene que poder averiguar si el producto está en alguna despensa.
        var productId = await InsertProductAsync("7790000030011");
        await InsertPantryItemAsync(productId);

        using (_principalAccessor.Change(SecurityTestPrincipals.User()))
        {
            await Should.ThrowAsync<AbpAuthorizationException>(
                () => _productAppService.DeleteAsync(productId));
        }

        (await ProductExistsAsync(productId)).ShouldBeTrue();
    }

    // ---------- Admin con Products.Manage ----------

    [Fact]
    public async Task UpdateAsync_Should_Succeed_When_Admin_Has_Manage_Permission()
    {
        await GrantManagePermissionToAdminRoleAsync();
        var productId = await InsertProductAsync("7790000030009");

        using (_principalAccessor.Change(SecurityTestPrincipals.Admin()))
        {
            var updated = await _productAppService.UpdateAsync(productId, NewUpdateDto("7790000030009", "Leche descremada"));

            updated.Name.ShouldBe("Leche descremada");
        }
    }

    [Fact]
    public async Task DeleteAsync_Should_Succeed_When_Admin_Has_Manage_Permission()
    {
        await GrantManagePermissionToAdminRoleAsync();
        var productId = await InsertProductAsync("7790000030010");

        using (_principalAccessor.Change(SecurityTestPrincipals.Admin()))
        {
            await _productAppService.DeleteAsync(productId);
        }

        (await ProductExistsAsync(productId)).ShouldBeFalse();
    }

    // ---------- Ayudantes ----------

    /// <summary>
    /// Otorga el permiso en la propia prueba para no depender del seed de datos.
    /// </summary>
    private Task GrantManagePermissionToAdminRoleAsync()
    {
        return WithUnitOfWorkAsync(() =>
            GetRequiredService<IPermissionManager>()
                .SetForRoleAsync("admin", TP04Permissions.Products.Manage, true));
    }

    /// <summary>Carga un producto por repositorio: no pasa por la autorización.</summary>
    private Task<Guid> InsertProductAsync(string barcode)
    {
        return WithUnitOfWorkAsync(async () =>
        {
            var product = await GetRequiredService<ProductManager>().CreateAsync(barcode, "Leche", "La Serenísima");
            await _productRepository.InsertAsync(product, autoSave: true);
            return product.Id;
        });
    }

    /// <summary>Pone el producto en la despensa de otro usuario, por repositorio.</summary>
    private Task InsertPantryItemAsync(Guid productId)
    {
        return WithUnitOfWorkAsync(() =>
            GetRequiredService<IRepository<PantryItem, Guid>>()
                .InsertAsync(new PantryItem(Guid.NewGuid(), Guid.NewGuid(), productId), autoSave: true));
    }

    private Task<bool> ProductExistsAsync(Guid id)
    {
        return WithUnitOfWorkAsync(async () => await _productRepository.FindAsync(id) != null);
    }

    private static CreateProductDto NewCreateDto(string barcode)
    {
        return new CreateProductDto { Barcode = barcode, Name = "Yerba", Brand = "Playadito" };
    }

    private static UpdateProductDto NewUpdateDto(string barcode, string name = "Leche entera")
    {
        return new UpdateProductDto { Barcode = barcode, Name = name, Brand = "La Serenísima" };
    }
}
