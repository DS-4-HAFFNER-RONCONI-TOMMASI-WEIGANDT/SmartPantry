using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Modularity;
using Volo.Abp.Validation;
using Xunit;

namespace TP04.Products
{
    public abstract class ProductAppService_Tests<TStartupModule> : TP04ApplicationTestBase<TStartupModule>
        where TStartupModule : IAbpModule
    {
        private readonly IProductAppService _productAppService;

        public ProductAppService_Tests()
        {
            _productAppService = GetRequiredService<IProductAppService>();
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
