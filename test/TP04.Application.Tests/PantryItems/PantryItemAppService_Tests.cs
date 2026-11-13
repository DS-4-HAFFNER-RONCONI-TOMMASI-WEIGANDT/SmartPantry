using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using TP04.Products;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Volo.Abp.Users;
using Volo.Abp.Validation;
using Xunit;

namespace TP04.PantryItems
{
    /// <summary>
    /// TP10: pruebas de aplicación de la despensa (RF-10 / RF-11).
    ///
    /// El usuario actual es el de FakeCurrentPrincipalAccessor (admin de prueba). Los
    /// ítems "ajenos" se insertan directo con el repositorio y otro OwnerId.
    ///
    /// Importante: estas pruebas usan AlwaysAllowAuthorizationService, así que NO
    /// comprueban [Authorize] ni el acceso HTTP (401). Eso lo cubren
    /// AppServiceAuthorization_Tests (atributos) y la matriz manual de Swagger del PR.
    /// Sí comprueban el control de propietario, que es lógica del propio servicio.
    /// </summary>
    public abstract class PantryItemAppService_Tests<TStartupModule> : TP04ApplicationTestBase<TStartupModule>
        where TStartupModule : IAbpModule
    {
        private static readonly Guid OtherUserId = new Guid("9b1f6a3c-4d2e-4f7a-8c5b-1e2d3f4a5b6c");

        private readonly IPantryItemAppService _pantryItemAppService;
        private readonly IRepository<PantryItem, Guid> _pantryItemRepository;
        private readonly IRepository<Product, Guid> _productRepository;
        private readonly ProductManager _productManager;
        private readonly ICurrentUser _currentUser;

        private int _nextBarcode = 1;

        protected PantryItemAppService_Tests()
        {
            _pantryItemAppService = GetRequiredService<IPantryItemAppService>();
            _pantryItemRepository = GetRequiredService<IRepository<PantryItem, Guid>>();
            _productRepository = GetRequiredService<IRepository<Product, Guid>>();
            _productManager = GetRequiredService<ProductManager>();
            _currentUser = GetRequiredService<ICurrentUser>();
        }

        // ---------------------------------------------------------------------
        // Alta (RF-10) y listado (RF-11)
        // ---------------------------------------------------------------------

        [Fact]
        public async Task Create_Should_Assign_Current_User_And_Return_Product_Name()
        {
            var product = await CreateProductAsync("Leche");

            var created = await _pantryItemAppService.CreateAsync(new CreatePantryItemDto
            {
                ProductId = product.Id,
                Quantity = 2,
                Unit = "l",
                ExpirationDate = new DateOnly(2026, 12, 1)
            });

            created.ProductName.ShouldBe("Leche");
            created.Quantity.ShouldBe(2m);
            created.Unit.ShouldBe("l");
            created.Status.ShouldBe(PantryItemStatus.Active);

            var stored = await _pantryItemRepository.GetAsync(created.Id);
            stored.OwnerId.ShouldBe(CurrentUserId);
        }

        [Fact]
        public async Task Create_With_Unknown_Product_Should_Throw_Not_Found()
        {
            await Should.ThrowAsync<EntityNotFoundException>(() =>
                _pantryItemAppService.CreateAsync(new CreatePantryItemDto { ProductId = Guid.NewGuid() }));
        }

        [Fact]
        public async Task GetList_Should_Return_Only_Current_User_Items()
        {
            var mine = await CreateItemAsync(CurrentUserId, "Arroz");
            await CreateItemAsync(OtherUserId, "Producto ajeno");

            var result = await _pantryItemAppService.GetListAsync(new GetPantryItemListDto());

            result.TotalCount.ShouldBe(1);
            result.Items.Count.ShouldBe(1);
            result.Items[0].Id.ShouldBe(mine.Id);
            result.Items[0].ProductName.ShouldBe("Arroz");
        }

        [Fact]
        public async Task GetList_Should_Return_Null_For_Missing_Data()
        {
            // RF-09: lo que no se cargó llega en null; el servidor no inventa valores.
            await CreateItemAsync(CurrentUserId, "Sal");

            var result = await _pantryItemAppService.GetListAsync(new GetPantryItemListDto());

            var item = result.Items.Single();
            item.Quantity.ShouldBeNull();
            item.Unit.ShouldBeNull();
            item.ExpirationDate.ShouldBeNull();
        }

        [Fact]
        public async Task GetList_Should_Be_Paged()
        {
            await CreateItemAsync(CurrentUserId, "A", new DateOnly(2026, 12, 1));
            await CreateItemAsync(CurrentUserId, "B", new DateOnly(2026, 12, 2));
            await CreateItemAsync(CurrentUserId, "C", new DateOnly(2026, 12, 3));

            var firstPage = await _pantryItemAppService.GetListAsync(new GetPantryItemListDto
            {
                SkipCount = 0,
                MaxResultCount = 2
            });
            var secondPage = await _pantryItemAppService.GetListAsync(new GetPantryItemListDto
            {
                SkipCount = 2,
                MaxResultCount = 2
            });

            firstPage.TotalCount.ShouldBe(3);
            firstPage.Items.Select(i => i.ProductName).ShouldBe(new[] { "A", "B" });
            secondPage.Items.Select(i => i.ProductName).ShouldBe(new[] { "C" });
        }

        [Fact]
        public async Task GetList_Default_Order_Should_Put_Items_Without_Date_Last()
        {
            await CreateItemAsync(CurrentUserId, "Sin fecha");
            await CreateItemAsync(CurrentUserId, "Vence después", new DateOnly(2026, 12, 20));
            await CreateItemAsync(CurrentUserId, "Vence antes", new DateOnly(2026, 12, 5));

            var result = await _pantryItemAppService.GetListAsync(new GetPantryItemListDto());

            result.Items.Select(i => i.ProductName)
                .ShouldBe(new[] { "Vence antes", "Vence después", "Sin fecha" });
        }

        [Fact]
        public async Task GetList_Should_Filter_By_Status()
        {
            await CreateItemAsync(CurrentUserId, "Activo");
            await CreateItemAsync(CurrentUserId, "Consumido", consumed: true);

            var active = await _pantryItemAppService.GetListAsync(new GetPantryItemListDto
            {
                Status = PantryItemStatus.Active
            });
            var consumed = await _pantryItemAppService.GetListAsync(new GetPantryItemListDto
            {
                Status = PantryItemStatus.Consumed
            });
            var all = await _pantryItemAppService.GetListAsync(new GetPantryItemListDto());

            active.Items.Single().ProductName.ShouldBe("Activo");
            consumed.Items.Single().ProductName.ShouldBe("Consumido");
            all.TotalCount.ShouldBe(2);
        }

        // ---------------------------------------------------------------------
        // Edición (RF-11) y validación en servidor
        // ---------------------------------------------------------------------

        [Fact]
        public async Task Update_Should_Change_Quantity_Unit_And_Date()
        {
            var item = await CreateItemAsync(CurrentUserId, "Yogur");

            var updated = await _pantryItemAppService.UpdateAsync(item.Id, new UpdatePantryItemDto
            {
                Quantity = 4,
                Unit = "unidades",
                ExpirationDate = new DateOnly(2026, 11, 20)
            });

            updated.Quantity.ShouldBe(4m);
            updated.Unit.ShouldBe("unidades");
            updated.ExpirationDate.ShouldBe(new DateOnly(2026, 11, 20));

            var stored = await _pantryItemRepository.GetAsync(item.Id);
            stored.Quantity.ShouldBe(4m);
            stored.Unit.ShouldBe("unidades");
            stored.ExpirationDate.ShouldBe(new DateOnly(2026, 11, 20));
        }

        [Fact]
        public async Task Update_With_Negative_Quantity_Should_Fail_Validation()
        {
            var item = await CreateItemAsync(CurrentUserId, "Queso");

            await Should.ThrowAsync<AbpValidationException>(() =>
                _pantryItemAppService.UpdateAsync(item.Id, new UpdatePantryItemDto { Quantity = -1 }));
        }

        [Fact]
        public async Task Update_With_Too_Long_Unit_Should_Fail_Validation()
        {
            var item = await CreateItemAsync(CurrentUserId, "Harina");

            await Should.ThrowAsync<AbpValidationException>(() =>
                _pantryItemAppService.UpdateAsync(item.Id, new UpdatePantryItemDto
                {
                    Unit = new string('x', PantryItemConsts.MaxUnitLength + 1)
                }));
        }

        [Fact]
        public async Task Update_With_Out_Of_Range_Date_Should_Fail_Validation()
        {
            var item = await CreateItemAsync(CurrentUserId, "Aceite");

            await Should.ThrowAsync<AbpValidationException>(() =>
                _pantryItemAppService.UpdateAsync(item.Id, new UpdatePantryItemDto
                {
                    ExpirationDate = new DateOnly(1999, 12, 31)
                }));
        }

        [Fact]
        public async Task Update_Consumed_Item_Should_Throw_Business_Exception()
        {
            var item = await CreateItemAsync(CurrentUserId, "Pan", consumed: true);

            var ex = await Should.ThrowAsync<BusinessException>(() =>
                _pantryItemAppService.UpdateAsync(item.Id, new UpdatePantryItemDto { Quantity = 1 }));

            ex.Code.ShouldBe(TP04DomainErrorCodes.PantryItemAlreadyConsumed);
        }

        // ---------------------------------------------------------------------
        // Consumo (regla de negocio 6) y eliminación
        // ---------------------------------------------------------------------

        [Fact]
        public async Task MarkAsConsumed_Should_Keep_Item_As_History()
        {
            var item = await CreateItemAsync(CurrentUserId, "Manteca", new DateOnly(2026, 11, 10));

            var result = await _pantryItemAppService.MarkAsConsumedAsync(item.Id);

            result.Status.ShouldBe(PantryItemStatus.Consumed);
            var stored = await _pantryItemRepository.FindAsync(item.Id);
            stored.ShouldNotBeNull(); // no se elimina automáticamente
            stored!.Status.ShouldBe(PantryItemStatus.Consumed);
        }

        [Fact]
        public async Task Delete_Should_Remove_Item()
        {
            var item = await CreateItemAsync(CurrentUserId, "Jamón");

            await _pantryItemAppService.DeleteAsync(item.Id);

            (await _pantryItemRepository.FindAsync(item.Id)).ShouldBeNull();
        }

        // ---------------------------------------------------------------------
        // Ítem de otro usuario: no se lee ni se modifica (404 en todas)
        // ---------------------------------------------------------------------

        [Fact]
        public async Task Other_User_Item_Should_Not_Be_Read()
        {
            var foreign = await CreateItemAsync(OtherUserId, "Ajeno");

            await Should.ThrowAsync<EntityNotFoundException>(() =>
                _pantryItemAppService.GetAsync(foreign.Id));
        }

        [Fact]
        public async Task Other_User_Item_Should_Not_Be_Modified_Consumed_Or_Deleted()
        {
            var foreign = await CreateItemAsync(OtherUserId, "Ajeno", new DateOnly(2026, 12, 1));

            await Should.ThrowAsync<EntityNotFoundException>(() =>
                _pantryItemAppService.UpdateAsync(foreign.Id, new UpdatePantryItemDto { Quantity = 99 }));

            await Should.ThrowAsync<EntityNotFoundException>(() =>
                _pantryItemAppService.MarkAsConsumedAsync(foreign.Id));

            await Should.ThrowAsync<EntityNotFoundException>(() =>
                _pantryItemAppService.DeleteAsync(foreign.Id));

            // El ítem ajeno sigue intacto.
            var stored = await _pantryItemRepository.GetAsync(foreign.Id);
            stored.Quantity.ShouldBeNull();
            stored.Status.ShouldBe(PantryItemStatus.Active);
            stored.ExpirationDate.ShouldBe(new DateOnly(2026, 12, 1));
        }

        [Fact]
        public async Task Unknown_Item_Should_Throw_Not_Found()
        {
            await Should.ThrowAsync<EntityNotFoundException>(() =>
                _pantryItemAppService.GetAsync(Guid.NewGuid()));
        }

        // ---------------------------------------------------------------------
        // Ayudas
        // ---------------------------------------------------------------------

        private Guid CurrentUserId => _currentUser.GetId();

        private async Task<Product> CreateProductAsync(string name)
        {
            var barcode = (7790000010000 + _nextBarcode++).ToString();
            var product = await _productManager.CreateAsync(barcode, name);
            return await _productRepository.InsertAsync(product, autoSave: true);
        }

        private async Task<PantryItem> CreateItemAsync(
            Guid ownerId,
            string productName,
            DateOnly? expirationDate = null,
            bool consumed = false)
        {
            var product = await CreateProductAsync(productName);

            var item = new PantryItem(Guid.NewGuid(), ownerId, product.Id, expirationDate);
            if (consumed)
            {
                item.MarkAsConsumed();
            }

            return await _pantryItemRepository.InsertAsync(item, autoSave: true);
        }
    }
}
