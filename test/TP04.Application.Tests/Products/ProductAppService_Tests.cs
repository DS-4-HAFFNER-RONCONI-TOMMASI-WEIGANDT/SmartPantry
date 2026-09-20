using Shouldly;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Validation;
using Xunit;

namespace TP04.Products
{
    public class ProductAppService_Tests : TP04ApplicationTestBase
    {
        private readonly IProductAppService _productAppService;

        public ProductAppService_Tests()
        {
            _productAppService = GetRequiredService<IProductAppService>();
        }

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
    }
}