using System;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace TP04.Products
{
    // Hereda de la base de ABP (y no de object) porque ProductManager necesita el
    // repositorio inyectado por el contenedor: no se puede instanciar suelto.
    public class ProductManager_Tests : TP04DomainTestBase
    {
        private readonly ProductManager _productManager;
        private readonly IRepository<Product, Guid> _productRepository;

        public ProductManager_Tests()
        {
            _productManager = GetRequiredService<ProductManager>();
            _productRepository = GetRequiredService<IRepository<Product, Guid>>();
        }

        [Fact]
        public async Task Should_Create_Valid_Product_And_Normalize_Text()
        {
            var product = await _productManager.CreateAsync(
                "  7791234567890  ",
                "  Arroz Largo Fino  ",
                "  Marca X  "
            );

            product.Barcode.ShouldBe("7791234567890");
            product.Name.ShouldBe("Arroz Largo Fino");
            product.Brand.ShouldBe("Marca X");
        }

        [Fact]
        public async Task Should_Not_Allow_Empty_Name()
        {
            await Should.ThrowAsync<ArgumentException>(async () =>
            {
                await _productManager.CreateAsync("7790000000001", "   ");
            });
        }

        [Fact]
        public async Task Should_Not_Allow_Empty_Barcode()
        {
            await Should.ThrowAsync<ArgumentException>(async () =>
            {
                await _productManager.CreateAsync("   ", "Arroz");
            });
        }

        [Fact]
        public async Task Should_Not_Allow_Duplicate_Barcode()
        {
            var first = await _productManager.CreateAsync("7790000000002", "Producto A");
            await _productRepository.InsertAsync(first, autoSave: true);

            await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _productManager.CreateAsync("7790000000002", "Producto B (mismo código)");
            });
        }
    }
}