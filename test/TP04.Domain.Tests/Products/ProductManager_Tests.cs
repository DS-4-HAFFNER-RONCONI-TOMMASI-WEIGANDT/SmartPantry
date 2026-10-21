using Shouldly;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Xunit;

namespace TP04.Products
{
    // Hereda de la base de ABP (y no de object) porque ProductManager necesita el
    // repositorio inyectado por el contenedor: no se puede instanciar suelto.
    public abstract class ProductManager_Tests<TStartupModule> : TP04DomainTestBase<TStartupModule>
        where TStartupModule : IAbpModule
    {
        private readonly ProductManager _productManager;
        private readonly IRepository<Product, Guid> _productRepository;

        public ProductManager_Tests()
        {
            _productManager = GetRequiredService<ProductManager>();
            _productRepository = GetRequiredService<IRepository<Product, Guid>>();
        }

        // ---------------------------------------------------------------------
        // Creación (TP05)
        // ---------------------------------------------------------------------

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

        // ---------------------------------------------------------------------
        // Modificación (TP06)
        // ---------------------------------------------------------------------

        [Fact]
        public async Task Update_Valid_Should_Keep_Normalizations()
        {
            var product = await CreateAndInsertAsync("7790000000100", "Harina", "Marca Vieja");

            await _productManager.UpdateAsync(
                product,
                "  7790000000101  ",
                "  Harina 000  ",
                "   "
            );

            product.Barcode.ShouldBe("7790000000101");
            product.Name.ShouldBe("Harina 000");
            product.Brand.ShouldBeNull(); // marca en blanco se normaliza a null
        }

        [Fact]
        public async Task Update_Should_Allow_Keeping_Same_Barcode()
        {
            // El propio producto no cuenta como duplicado de sí mismo.
            var product = await CreateAndInsertAsync("7790000000110", "Azúcar", "Marca A");

            await _productManager.UpdateAsync(product, " 7790000000110 ", "Azúcar Común", "Marca B");

            product.Barcode.ShouldBe("7790000000110");
            product.Name.ShouldBe("Azúcar Común");
            product.Brand.ShouldBe("Marca B");
        }

        [Fact]
        public async Task Update_With_Invalid_Name_Should_Be_Rejected_And_Keep_Previous_State()
        {
            var product = await CreateAndInsertAsync("7790000000120", "Yerba", "Marca Y");

            // Barcode y marca válidos + nombre inválido: no debe aplicarse ninguno.
            await Should.ThrowAsync<ArgumentException>(async () =>
            {
                await _productManager.UpdateAsync(product, "7790000000121", "   ", "Otra Marca");
            });

            ShouldHaveState(product, "7790000000120", "Yerba", "Marca Y");
        }

        [Fact]
        public async Task Update_With_Too_Long_Brand_Should_Be_Rejected_And_Keep_Previous_State()
        {
            var product = await CreateAndInsertAsync("7790000000130", "Aceite", "Marca C");

            // El error está en el último campo: barcode y nombre (válidos) tampoco
            // deben quedar asignados.
            await Should.ThrowAsync<ArgumentException>(async () =>
            {
                await _productManager.UpdateAsync(
                    product,
                    "7790000000131",
                    "Aceite de Girasol",
                    new string('x', ProductConsts.MaxBrandLength + 1)
                );
            });

            ShouldHaveState(product, "7790000000130", "Aceite", "Marca C");
        }

        [Fact]
        public async Task Update_With_Barcode_Of_Another_Product_Should_Be_Rejected_And_Keep_Previous_State()
        {
            await CreateAndInsertAsync("7790000000140", "Leche");
            var product = await CreateAndInsertAsync("7790000000141", "Queso", "Marca Q");

            var exception = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _productManager.UpdateAsync(product, " 7790000000140 ", "Queso Cremoso", "Marca Q");
            });

            exception.Code.ShouldBe(TP04DomainErrorCodes.ProductDuplicateBarcode);
            ShouldHaveState(product, "7790000000141", "Queso", "Marca Q");
        }

        // ---------------------------------------------------------------------

        private async Task<Product> CreateAndInsertAsync(string barcode, string name, string? brand = null)
        {
            var product = await _productManager.CreateAsync(barcode, name, brand);
            await _productRepository.InsertAsync(product, autoSave: true);
            return product;
        }

        private static void ShouldHaveState(Product product, string barcode, string name, string? brand)
        {
            product.Barcode.ShouldBe(barcode);
            product.Name.ShouldBe(name);
            product.Brand.ShouldBe(brand);
        }
    }
}
