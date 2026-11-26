using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TP04.PantryItems;
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
        private readonly IRepository<PantryItem, Guid> _pantryItemRepository;

        public ProductManager_Tests()
        {
            _productManager = GetRequiredService<ProductManager>();
            _productRepository = GetRequiredService<IRepository<Product, Guid>>();
            _pantryItemRepository = GetRequiredService<IRepository<PantryItem, Guid>>();
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
        // Guardar desde el catálogo externo (TP final, RF-08)
        // Integración: la clase concreta corre contra SQLite (EfCoreProductManager_Tests).
        // ---------------------------------------------------------------------

        [Fact]
        public async Task SaveFromExternalAsync_Should_Create_Product_And_Persist_Data_When_Barcode_Is_New()
        {
            var result = await _productManager.SaveFromExternalAsync(
                "3017620422003", "Nutella", "Ferrero", ExternalData());
            await SaveResultAsync(result);

            result.AlreadyExisted.ShouldBeFalse();

            // Se relee de la base para comprobar que todo quedó guardado.
            var saved = await _productRepository.GetAsync(result.Product.Id);
            saved.Barcode.ShouldBe("3017620422003");
            saved.Name.ShouldBe("Nutella");
            saved.Brand.ShouldBe("Ferrero");
            saved.ImageUrl.ShouldBe("https://images.openfoodfacts.org/front.jpg");
            saved.GetCategories().ShouldBe(new[] { "Desayunos", "Untables" });
            saved.IngredientsText.ShouldBe("Azúcar, aceite vegetal, avellanas");
            saved.GetAllergenTags().ShouldBe(new[] { "en:milk", "en:nuts" });
            saved.NutriScore.ShouldBe("e");
            saved.NovaGroup.ShouldBe(4);
            saved.ExternalDataUpdatedAt.ShouldNotBeNull();
        }

        [Fact]
        public async Task SaveFromExternalAsync_Should_Keep_A_Single_Product_When_Saved_Twice()
        {
            // Regla de negocio 4 / RF-08: guardar dos veces el mismo código no duplica.
            var first = await _productManager.SaveFromExternalAsync(
                "7790000000200", "Galletitas", "Marca G", ExternalData());
            await SaveResultAsync(first);

            var second = await _productManager.SaveFromExternalAsync(
                "7790000000200", "Galletitas", "Marca G", new ProductExternalData { NutriScore = "c" });
            await SaveResultAsync(second);

            second.AlreadyExisted.ShouldBeTrue();
            second.Product.Id.ShouldBe(first.Product.Id);
            (await CountByBarcodeAsync("7790000000200")).ShouldBe(1);

            // Los datos externos se reemplazan por los de la segunda consulta.
            var saved = await _productRepository.GetAsync(first.Product.Id);
            saved.NutriScore.ShouldBe("c");
            saved.ImageUrl.ShouldBeNull();
        }

        [Fact]
        public async Task SaveFromExternalAsync_Should_Not_Duplicate_When_Barcode_Has_Spaces()
        {
            var first = await _productManager.SaveFromExternalAsync(
                "7790000000210", "Fideos", null, ExternalData());
            await SaveResultAsync(first);

            var second = await _productManager.SaveFromExternalAsync(
                "  7790000000210  ", "Fideos", null, ExternalData());
            await SaveResultAsync(second);

            second.AlreadyExisted.ShouldBeTrue();
            (await CountByBarcodeAsync("7790000000210")).ShouldBe(1);
        }

        [Fact]
        public async Task SaveFromExternalAsync_Should_Update_Product_Created_Manually()
        {
            // Un producto cargado a mano (TP05) también cuenta: no se crea otro.
            var manual = await CreateAndInsertAsync("7790000000220", "Arroz", "Marca A");

            var result = await _productManager.SaveFromExternalAsync(
                "7790000000220", "Rice", "Brand A", ExternalData());
            await SaveResultAsync(result);

            result.AlreadyExisted.ShouldBeTrue();
            result.Product.Id.ShouldBe(manual.Id);
            (await CountByBarcodeAsync("7790000000220")).ShouldBe(1);
        }

        [Fact]
        public async Task SaveFromExternalAsync_Should_Keep_Name_And_Brand_When_Product_Exists()
        {
            // El nombre y la marca pueden haber sido corregidos por un admin (TP06):
            // solo se actualizan los datos externos.
            await CreateAndInsertAsync("7790000000230", "Yerba Mate Suave", "Marca Corregida");

            var result = await _productManager.SaveFromExternalAsync(
                "7790000000230", "yerba mate", "marca proveedor", ExternalData());
            await SaveResultAsync(result);

            var saved = await _productRepository.GetAsync(result.Product.Id);
            saved.Name.ShouldBe("Yerba Mate Suave");
            saved.Brand.ShouldBe("Marca Corregida");
            saved.NutriScore.ShouldBe("e");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("   ")]
        public async Task SaveFromExternalAsync_Should_Use_Barcode_As_Name_When_Provider_Omits_Name(string? name)
        {
            // RF-09: no se inventa un nombre; se muestra el código de barras.
            var result = await _productManager.SaveFromExternalAsync(
                " 7790000000240 ", name, null, ExternalData());

            result.Product.Name.ShouldBe("7790000000240");
            result.Product.Brand.ShouldBeNull();
        }

        [Fact]
        public async Task SaveFromExternalAsync_Should_Throw_And_Not_Create_When_External_Data_Is_Invalid()
        {
            await Should.ThrowAsync<ArgumentException>(async () =>
            {
                await _productManager.SaveFromExternalAsync(
                    "7790000000250", "Gaseosa", null, new ProductExternalData { NovaGroup = 7 });
            });

            (await CountByBarcodeAsync("7790000000250")).ShouldBe(0);
        }

        [Fact]
        public async Task SaveFromExternalAsync_Should_Throw_When_Barcode_Is_Empty()
        {
            await Should.ThrowAsync<ArgumentException>(async () =>
            {
                await _productManager.SaveFromExternalAsync("   ", "Producto", null, ExternalData());
            });
        }

        // ---------------------------------------------------------------------
        // Producto en uso (TP final, RF-08)
        // ---------------------------------------------------------------------

        [Fact]
        public async Task EnsureNotInUseAsync_Should_Not_Throw_When_Product_Is_Not_In_Any_Pantry()
        {
            var product = await CreateAndInsertAsync("7790000000300", "Lentejas");

            await Should.NotThrowAsync(() => EnsureNotInUseInUnitOfWorkAsync(product.Id));
        }

        [Fact]
        public async Task EnsureNotInUseAsync_Should_Throw_ProductInUse_When_Product_Is_In_A_Pantry()
        {
            var product = await CreateAndInsertAsync("7790000000310", "Polenta");
            await InsertPantryItemAsync(Guid.NewGuid(), product.Id);

            var exception = await Should.ThrowAsync<BusinessException>(
                () => EnsureNotInUseInUnitOfWorkAsync(product.Id));

            exception.Code.ShouldBe(TP04DomainErrorCodes.ProductInUse);
        }

        [Fact]
        public async Task EnsureNotInUseAsync_Should_Throw_ProductInUse_When_Product_Is_In_Several_Pantries()
        {
            // Dos usuarios con el mismo producto: la consulta tiene que contemplar
            // más de un ítem (FindAsync con condición fallaría con dos resultados).
            var product = await CreateAndInsertAsync("7790000000320", "Garbanzos");
            await InsertPantryItemAsync(Guid.NewGuid(), product.Id);
            await InsertPantryItemAsync(Guid.NewGuid(), product.Id);

            var exception = await Should.ThrowAsync<BusinessException>(
                () => EnsureNotInUseInUnitOfWorkAsync(product.Id));

            exception.Code.ShouldBe(TP04DomainErrorCodes.ProductInUse);
        }

        [Fact]
        public async Task EnsureNotInUseAsync_Should_Throw_ProductInUse_When_Only_A_Consumed_Item_Uses_It()
        {
            // Regla de negocio 6: el consumido es historial y sigue referenciando al producto.
            var product = await CreateAndInsertAsync("7790000000330", "Avena");
            var item = new PantryItem(Guid.NewGuid(), Guid.NewGuid(), product.Id);
            item.MarkAsConsumed();
            await _pantryItemRepository.InsertAsync(item, autoSave: true);

            var exception = await Should.ThrowAsync<BusinessException>(
                () => EnsureNotInUseInUnitOfWorkAsync(product.Id));

            exception.Code.ShouldBe(TP04DomainErrorCodes.ProductInUse);
        }

        // ---------------------------------------------------------------------

        /// <summary>
        /// EnsureNotInUseAsync arma la consulta con GetQueryableAsync, que necesita una
        /// Unit of Work abierta mientras se ejecuta. En la aplicación la abre el AppService;
        /// acá, como se llama al Manager directo, la abre la prueba.
        /// </summary>
        private Task EnsureNotInUseInUnitOfWorkAsync(Guid productId)
        {
            return WithUnitOfWorkAsync(() => _productManager.EnsureNotInUseAsync(productId));
        }

        private async Task InsertPantryItemAsync(Guid ownerId, Guid productId)
        {
            await _pantryItemRepository.InsertAsync(
                new PantryItem(Guid.NewGuid(), ownerId, productId),
                autoSave: true);
        }

        /// <summary>
        /// Hace lo mismo que hará el AppService al guardar desde el catálogo externo:
        /// inserta si es nuevo y actualiza si ya existía.
        /// </summary>
        private async Task SaveResultAsync(ExternalProductSaveResult result)
        {
            if (result.AlreadyExisted)
            {
                await _productRepository.UpdateAsync(result.Product, autoSave: true);
            }
            else
            {
                await _productRepository.InsertAsync(result.Product, autoSave: true);
            }
        }

        private async Task<int> CountByBarcodeAsync(string barcode)
        {
            return (await _productRepository.GetListAsync(p => p.Barcode == barcode)).Count;
        }

        private static ProductExternalData ExternalData()
        {
            return new ProductExternalData
            {
                ImageUrl = "https://images.openfoodfacts.org/front.jpg",
                Categories = new List<string> { "Desayunos", "Untables" },
                IngredientsText = "Azúcar, aceite vegetal, avellanas",
                AllergenTags = new List<string> { "en:milk", "en:nuts" },
                NutriScore = "e",
                NovaGroup = 4
            };
        }

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
