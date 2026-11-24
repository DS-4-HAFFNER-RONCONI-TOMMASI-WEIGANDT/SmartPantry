using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TP04.PantryItems;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Volo.Abp.Validation;
using Xunit;

namespace TP04.Products
{
    public abstract class ProductAppService_Tests<TStartupModule> : TP04ApplicationTestBase<TStartupModule>
        where TStartupModule : IAbpModule
    {
        private readonly IProductAppService _productAppService;
        private readonly IRepository<PantryItem, Guid> _pantryItemRepository;
        private readonly IRepository<Product, Guid> _productRepository;
        private readonly ProductManager _productManager;

        public ProductAppService_Tests()
        {
            _productAppService = GetRequiredService<IProductAppService>();
            _pantryItemRepository = GetRequiredService<IRepository<PantryItem, Guid>>();
            _productRepository = GetRequiredService<IRepository<Product, Guid>>();
            _productManager = GetRequiredService<ProductManager>();
        }

        // ---------------------------------------------------------------------
        // Creación y consulta por Id (TP05)
        // ---------------------------------------------------------------------

        [Fact]
        public async Task Should_Create_And_Get_Product_By_Id()
        {
            var created = await _productAppService.CreateAsync(new CreateProductDto
            {
                Barcode = "7790000000010",
                Name = "Fideos",
                Brand = "Marca Y"
            });

            created.Id.ShouldNotBe(Guid.Empty);

            var fetched = await _productAppService.GetAsync(created.Id);

            fetched.Barcode.ShouldBe("7790000000010");
            fetched.Name.ShouldBe("Fideos");
            fetched.Brand.ShouldBe("Marca Y");
        }

        [Fact]
        public async Task Should_Not_Create_Product_Without_Required_Fields()
        {
            await Should.ThrowAsync<AbpValidationException>(async () =>
            {
                await _productAppService.CreateAsync(new CreateProductDto
                {
                    Barcode = "",
                    Name = ""
                });
            });
        }

        [Fact]
        public async Task Should_Not_Create_Product_With_Duplicate_Barcode()
        {
            await _productAppService.CreateAsync(new CreateProductDto
            {
                Barcode = "7790000000099",
                Name = "Producto original"
            });

            await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _productAppService.CreateAsync(new CreateProductDto
                {
                    Barcode = "7790000000099",
                    Name = "Producto duplicado"
                });
            });
        }

        [Fact]
        public async Task GetAsync_Should_Throw_When_Id_Does_Not_Exist()
        {
            await Should.ThrowAsync<EntityNotFoundException>(async () =>
            {
                await _productAppService.GetAsync(Guid.NewGuid());
            });
        }

        // ---------------------------------------------------------------------
        // Recorrido completo (TP06): registrar, listar, modificar, consultar y eliminar
        // ---------------------------------------------------------------------

        [Fact]
        public async Task Should_Create_List_Update_Get_And_Delete_Product()
        {
            // Registrar
            var created = await _productAppService.CreateAsync(new CreateProductDto
            {
                Barcode = "7790000000200",
                Name = "Arroz",
                Brand = "Marca R"
            });

            // Listar
            var list = await _productAppService.GetListAsync(new PagedAndSortedResultRequestDto());
            list.TotalCount.ShouldBe(1);
            list.Items.ShouldContain(p => p.Id == created.Id && p.Barcode == "7790000000200");

            // Modificar (incluye corrección del barcode y normalización en el dominio)
            var updated = await _productAppService.UpdateAsync(created.Id, new UpdateProductDto
            {
                Barcode = "  7790000000201  ",
                Name = "  Arroz Integral  ",
                Brand = "  "
            });

            updated.Id.ShouldBe(created.Id);
            updated.Barcode.ShouldBe("7790000000201");
            updated.Name.ShouldBe("Arroz Integral");
            updated.Brand.ShouldBeNull();

            // Consultar: los cambios quedaron persistidos
            var fetched = await _productAppService.GetAsync(created.Id);
            fetched.Barcode.ShouldBe("7790000000201");
            fetched.Name.ShouldBe("Arroz Integral");
            fetched.Brand.ShouldBeNull();
            fetched.LastModificationTime.ShouldNotBeNull();

            // Eliminar
            await _productAppService.DeleteAsync(created.Id);

            // Consultar el Id eliminado -> EntityNotFoundException (404 en la API)
            await Should.ThrowAsync<EntityNotFoundException>(async () =>
            {
                await _productAppService.GetAsync(created.Id);
            });

            var listAfterDelete = await _productAppService.GetListAsync(new PagedAndSortedResultRequestDto());
            listAfterDelete.TotalCount.ShouldBe(0);
        }

        // ---------------------------------------------------------------------
        // Lista paginada y ordenable (TP06)
        // ---------------------------------------------------------------------

        [Fact]
        public async Task GetListAsync_Should_Be_Paged_And_Sorted()
        {
            await CreateProductAsync("7790000000301", "Cacao");
            await CreateProductAsync("7790000000302", "Avena");
            await CreateProductAsync("7790000000303", "Budín");

            var firstPage = await _productAppService.GetListAsync(new PagedAndSortedResultRequestDto
            {
                Sorting = "Name",
                SkipCount = 0,
                MaxResultCount = 2
            });

            firstPage.TotalCount.ShouldBe(3);
            firstPage.Items.Count.ShouldBe(2);
            firstPage.Items.Select(p => p.Name).ShouldBe(new[] { "Avena", "Budín" });

            var secondPage = await _productAppService.GetListAsync(new PagedAndSortedResultRequestDto
            {
                Sorting = "Name",
                SkipCount = 2,
                MaxResultCount = 2
            });

            secondPage.TotalCount.ShouldBe(3);
            secondPage.Items.Select(p => p.Name).ShouldBe(new[] { "Cacao" });

            var descending = await _productAppService.GetListAsync(new PagedAndSortedResultRequestDto
            {
                Sorting = "Name desc",
                MaxResultCount = 10
            });

            descending.Items.Select(p => p.Name).ShouldBe(new[] { "Cacao", "Budín", "Avena" });
        }

        [Fact]
        public async Task GetListAsync_Without_Sorting_Should_Order_By_Name()
        {
            await CreateProductAsync("7790000000311", "Té");
            await CreateProductAsync("7790000000312", "Café");

            var result = await _productAppService.GetListAsync(new PagedAndSortedResultRequestDto());

            result.Items.Select(p => p.Name).ShouldBe(new[] { "Café", "Té" });
        }

        // ---------------------------------------------------------------------
        // Modificación: casos inválidos (TP06)
        // ---------------------------------------------------------------------

        [Fact]
        public async Task UpdateAsync_With_Barcode_Of_Another_Product_Should_Throw_And_Not_Persist_Changes()
        {
            await CreateProductAsync("7790000000400", "Lentejas");
            var product = await CreateProductAsync("7790000000401", "Garbanzos", "Marca G");

            var exception = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _productAppService.UpdateAsync(product.Id, new UpdateProductDto
                {
                    Barcode = "7790000000400",
                    Name = "Garbanzos Secos",
                    Brand = "Otra"
                });
            });

            exception.Code.ShouldBe(TP04DomainErrorCodes.ProductDuplicateBarcode);

            var fetched = await _productAppService.GetAsync(product.Id);
            fetched.Barcode.ShouldBe("7790000000401");
            fetched.Name.ShouldBe("Garbanzos");
            fetched.Brand.ShouldBe("Marca G");
        }

        [Fact]
        public async Task UpdateAsync_Without_Required_Fields_Should_Throw_Validation_And_Not_Persist_Changes()
        {
            var product = await CreateProductAsync("7790000000410", "Polenta", "Marca P");

            await Should.ThrowAsync<AbpValidationException>(async () =>
            {
                await _productAppService.UpdateAsync(product.Id, new UpdateProductDto
                {
                    Barcode = "7790000000411",
                    Name = ""
                });
            });

            var fetched = await _productAppService.GetAsync(product.Id);
            fetched.Barcode.ShouldBe("7790000000410");
            fetched.Name.ShouldBe("Polenta");
            fetched.Brand.ShouldBe("Marca P");
        }

        [Fact]
        public async Task UpdateAsync_Should_Throw_When_Id_Does_Not_Exist()
        {
            await Should.ThrowAsync<EntityNotFoundException>(async () =>
            {
                await _productAppService.UpdateAsync(Guid.NewGuid(), new UpdateProductDto
                {
                    Barcode = "7790000000420",
                    Name = "Inexistente"
                });
            });
        }

        // ---------------------------------------------------------------------
        // Producto en uso (TP final, RF-08)
        // El borrado de un producto sin uso ya lo cubre
        // Should_Create_List_Update_Get_And_Delete_Product.
        // ---------------------------------------------------------------------

        [Fact]
        public async Task DeleteAsync_Should_Throw_ProductInUse_And_Keep_Product_When_It_Is_In_A_Pantry()
        {
            var product = await CreateProductAsync("7790000000500", "Mermelada");
            await _pantryItemRepository.InsertAsync(
                new PantryItem(Guid.NewGuid(), Guid.NewGuid(), product.Id),
                autoSave: true);

            var exception = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _productAppService.DeleteAsync(product.Id);
            });

            exception.Code.ShouldBe(TP04DomainErrorCodes.ProductInUse);

            // El producto no se borró.
            var stillThere = await _productAppService.GetAsync(product.Id);
            stillThere.Barcode.ShouldBe("7790000000500");
        }

        // ---------------------------------------------------------------------
        // Datos externos en ProductDto (TP final, RF-07 / RF-08)
        // ---------------------------------------------------------------------

        [Fact]
        public async Task GetAsync_Should_Return_External_Data_When_Product_Has_It()
        {
            var productId = await InsertProductWithExternalDataAsync("3017620422003");

            var dto = await _productAppService.GetAsync(productId);

            dto.Name.ShouldBe("Nutella");
            dto.ImageUrl.ShouldBe("https://images.openfoodfacts.org/front.jpg");
            dto.Categories.ShouldBe(new[] { "Desayunos", "Untables" });
            dto.IngredientsText.ShouldBe("Azúcar, aceite vegetal, avellanas");
            dto.AllergenTags.ShouldBe(new[] { "en:milk", "en:nuts" });
            dto.NutriScore.ShouldBe("e");
            dto.NovaGroup.ShouldBe(4);
            dto.ExternalDataUpdatedAt.ShouldNotBeNull();
        }

        [Fact]
        public async Task GetListAsync_Should_Return_External_Data_When_Product_Has_It()
        {
            var productId = await InsertProductWithExternalDataAsync("3017620422004");

            var list = await _productAppService.GetListAsync(new PagedAndSortedResultRequestDto());

            var dto = list.Items.Single(p => p.Id == productId);
            dto.Categories.ShouldBe(new[] { "Desayunos", "Untables" });
            dto.AllergenTags.ShouldBe(new[] { "en:milk", "en:nuts" });
        }

        [Fact]
        public async Task GetAsync_Should_Return_Null_External_Data_When_Product_Was_Created_Manually()
        {
            // RF-09: lo que nunca se informó queda en null (la interfaz muestra "No disponible").
            var created = await CreateProductAsync("7790000000510", "Arroz", "Marca R");

            var dto = await _productAppService.GetAsync(created.Id);

            dto.ImageUrl.ShouldBeNull();
            dto.Categories.ShouldBeNull();
            dto.IngredientsText.ShouldBeNull();
            dto.AllergenTags.ShouldBeNull();
            dto.NutriScore.ShouldBeNull();
            dto.NovaGroup.ShouldBeNull();
            dto.ExternalDataUpdatedAt.ShouldBeNull();
        }

        [Fact]
        public async Task GetAsync_Should_Return_Empty_AllergenTags_When_Informed_Without_Allergens()
        {
            // La diferencia null / lista vacía llega igual al cliente (la usa RF-12).
            var result = await _productManager.SaveFromExternalAsync(
                "7790000000520", "Agua mineral", null,
                new ProductExternalData { AllergenTags = new List<string>() });
            await _productRepository.InsertAsync(result.Product, autoSave: true);

            var dto = await _productAppService.GetAsync(result.Product.Id);

            dto.AllergenTags.ShouldNotBeNull();
            dto.AllergenTags.ShouldBeEmpty();
        }

        // ---------------------------------------------------------------------

        /// <summary>
        /// Guarda un producto con datos externos por el Manager (el guardado desde el
        /// catálogo externo todavía no tiene endpoint propio).
        /// </summary>
        private async Task<Guid> InsertProductWithExternalDataAsync(string barcode)
        {
            var result = await _productManager.SaveFromExternalAsync(barcode, "Nutella", "Ferrero", new ProductExternalData
            {
                ImageUrl = "https://images.openfoodfacts.org/front.jpg",
                Categories = new List<string> { "Desayunos", "Untables" },
                IngredientsText = "Azúcar, aceite vegetal, avellanas",
                AllergenTags = new List<string> { "en:milk", "en:nuts" },
                NutriScore = "e",
                NovaGroup = 4
            });

            await _productRepository.InsertAsync(result.Product, autoSave: true);
            return result.Product.Id;
        }

        private Task<ProductDto> CreateProductAsync(string barcode, string name, string? brand = null)
        {
            return _productAppService.CreateAsync(new CreateProductDto
            {
                Barcode = barcode,
                Name = name,
                Brand = brand
            });
        }
    }
}
