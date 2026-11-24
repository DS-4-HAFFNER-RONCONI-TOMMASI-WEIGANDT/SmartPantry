using System;
using System.Collections.Generic;
using Shouldly;
using Xunit;

namespace TP04.Products
{
    /// <summary>
    /// TP final (RF-07 / RF-08): reglas de los datos externos guardados en Product.
    /// Son pruebas unitarias puras: no usan base de datos ni contenedor de ABP
    /// (el constructor y UpdateExternalData son internal, visibles para este proyecto).
    /// </summary>
    public class Product_Tests
    {
        private static readonly DateTime UpdatedAt = new(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc);

        // ---------------------------------------------------------------------
        // Producto sin datos externos
        // ---------------------------------------------------------------------

        [Fact]
        public void New_Product_Should_Have_No_External_Data()
        {
            // RF-09: lo que no se informó queda en null.
            var product = NewProduct();

            product.ImageUrl.ShouldBeNull();
            product.GetCategories().ShouldBeNull();
            product.IngredientsText.ShouldBeNull();
            product.GetAllergenTags().ShouldBeNull();
            product.NutriScore.ShouldBeNull();
            product.NovaGroup.ShouldBeNull();
            product.ExternalDataUpdatedAt.ShouldBeNull();
        }

        // ---------------------------------------------------------------------
        // UpdateExternalData: datos válidos
        // ---------------------------------------------------------------------

        [Fact]
        public void UpdateExternalData_Should_Store_All_Data_When_Data_Is_Valid()
        {
            var product = NewProduct();

            product.UpdateExternalData(CompleteData(), UpdatedAt);

            product.ImageUrl.ShouldBe("https://images.openfoodfacts.org/front.jpg");
            product.GetCategories().ShouldBe(new[] { "Desayunos", "Untables" });
            product.IngredientsText.ShouldBe("Azúcar, aceite vegetal, avellanas");
            product.GetAllergenTags().ShouldBe(new[] { "en:milk", "en:nuts" });
            product.NutriScore.ShouldBe("e");
            product.NovaGroup.ShouldBe(4);
            product.ExternalDataUpdatedAt.ShouldBe(UpdatedAt);
        }

        [Fact]
        public void UpdateExternalData_Should_Trim_And_Normalize_Values()
        {
            var product = NewProduct();

            product.UpdateExternalData(new ProductExternalData
            {
                ImageUrl = "  https://images.openfoodfacts.org/front.jpg  ",
                Categories = new List<string> { "  Desayunos ", "", "desayunos", " Untables" },
                IngredientsText = "  Azúcar  ",
                AllergenTags = new List<string> { " EN:Milk ", "en:milk", "   " },
                NutriScore = " B "
            }, UpdatedAt);

            product.ImageUrl.ShouldBe("https://images.openfoodfacts.org/front.jpg");
            // Se descartan vacíos y repetidos (sin distinguir mayúsculas).
            product.GetCategories().ShouldBe(new[] { "Desayunos", "Untables" });
            product.IngredientsText.ShouldBe("Azúcar");
            product.GetAllergenTags().ShouldBe(new[] { "en:milk" });
            product.NutriScore.ShouldBe("b");
        }

        [Fact]
        public void UpdateExternalData_Should_Store_Nulls_When_Provider_Omits_Data()
        {
            var product = NewProduct();
            product.UpdateExternalData(CompleteData(), UpdatedAt);

            // Los datos externos se reemplazan: lo que el proveedor ya no informa queda en null.
            product.UpdateExternalData(new ProductExternalData
            {
                ImageUrl = "   ",
                Categories = new List<string>(),
                IngredientsText = ""
            }, UpdatedAt.AddDays(1));

            product.ImageUrl.ShouldBeNull();
            product.GetCategories().ShouldBeNull();
            product.IngredientsText.ShouldBeNull();
            product.GetAllergenTags().ShouldBeNull();
            product.NutriScore.ShouldBeNull();
            product.NovaGroup.ShouldBeNull();
            product.ExternalDataUpdatedAt.ShouldBe(UpdatedAt.AddDays(1));
        }

        [Fact]
        public void UpdateExternalData_Should_Not_Change_Name_Or_Brand()
        {
            var product = NewProduct();

            product.UpdateExternalData(CompleteData(), UpdatedAt);

            product.Barcode.ShouldBe("3017620422003");
            product.Name.ShouldBe("Nutella");
            product.Brand.ShouldBe("Ferrero");
        }

        [Theory]
        [InlineData("a")]
        [InlineData("e")]
        public void UpdateExternalData_Should_Accept_NutriScore_When_Between_A_And_E(string nutriScore)
        {
            var product = NewProduct();

            product.UpdateExternalData(new ProductExternalData { NutriScore = nutriScore }, UpdatedAt);

            product.NutriScore.ShouldBe(nutriScore);
        }

        [Theory]
        [InlineData(ProductConsts.MinNovaGroup)]
        [InlineData(ProductConsts.MaxNovaGroup)]
        public void UpdateExternalData_Should_Accept_NovaGroup_When_Between_1_And_4(int novaGroup)
        {
            var product = NewProduct();

            product.UpdateExternalData(new ProductExternalData { NovaGroup = novaGroup }, UpdatedAt);

            product.NovaGroup.ShouldBe(novaGroup);
        }

        [Fact]
        public void UpdateExternalData_Should_Accept_Values_When_At_Max_Length()
        {
            var product = NewProduct();
            var longIngredients = new string('x', ProductConsts.MaxIngredientsTextLength);

            product.UpdateExternalData(new ProductExternalData { IngredientsText = longIngredients }, UpdatedAt);

            product.IngredientsText!.Length.ShouldBe(ProductConsts.MaxIngredientsTextLength);
        }

        // ---------------------------------------------------------------------
        // Alérgenos: null (no informado) vs lista vacía (sin alérgenos)
        // ---------------------------------------------------------------------

        [Fact]
        public void GetAllergenTags_Should_Return_Null_When_Not_Informed()
        {
            var product = NewProduct();

            product.UpdateExternalData(new ProductExternalData { AllergenTags = null }, UpdatedAt);

            product.GetAllergenTags().ShouldBeNull();
        }

        [Fact]
        public void GetAllergenTags_Should_Return_Empty_List_When_Informed_Without_Allergens()
        {
            var product = NewProduct();

            product.UpdateExternalData(new ProductExternalData { AllergenTags = new List<string>() }, UpdatedAt);

            var tags = product.GetAllergenTags();
            tags.ShouldNotBeNull();
            tags.ShouldBeEmpty();
        }

        // ---------------------------------------------------------------------
        // UpdateExternalData: datos inválidos (no deja cambios a medias)
        // ---------------------------------------------------------------------

        [Theory]
        [InlineData("f")]
        [InlineData("unknown")]
        [InlineData("not-applicable")]
        public void UpdateExternalData_Should_Throw_When_NutriScore_Is_Invalid(string nutriScore)
        {
            var product = NewProduct();

            Should.Throw<ArgumentException>(() =>
                product.UpdateExternalData(new ProductExternalData { NutriScore = nutriScore }, UpdatedAt));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(-1)]
        public void UpdateExternalData_Should_Throw_When_NovaGroup_Is_Out_Of_Range(int novaGroup)
        {
            var product = NewProduct();

            Should.Throw<ArgumentException>(() =>
                product.UpdateExternalData(new ProductExternalData { NovaGroup = novaGroup }, UpdatedAt));
        }

        [Theory]
        [InlineData("javascript:alert(1)")]
        [InlineData("ftp://images.openfoodfacts.org/front.jpg")]
        [InlineData("front.jpg")]
        public void UpdateExternalData_Should_Throw_When_ImageUrl_Is_Not_Http(string imageUrl)
        {
            var product = NewProduct();

            Should.Throw<ArgumentException>(() =>
                product.UpdateExternalData(new ProductExternalData { ImageUrl = imageUrl }, UpdatedAt));
        }

        [Fact]
        public void UpdateExternalData_Should_Throw_When_IngredientsText_Is_Too_Long()
        {
            var product = NewProduct();
            var tooLong = new string('x', ProductConsts.MaxIngredientsTextLength + 1);

            Should.Throw<ArgumentException>(() =>
                product.UpdateExternalData(new ProductExternalData { IngredientsText = tooLong }, UpdatedAt));
        }

        [Fact]
        public void UpdateExternalData_Should_Throw_When_Categories_Are_Too_Long()
        {
            var product = NewProduct();
            var tooLong = new List<string> { new string('x', ProductConsts.MaxCategoriesLength + 1) };

            Should.Throw<ArgumentException>(() =>
                product.UpdateExternalData(new ProductExternalData { Categories = tooLong }, UpdatedAt));
        }

        [Fact]
        public void UpdateExternalData_Should_Throw_When_A_List_Value_Contains_Comma()
        {
            // Las listas se guardan separadas por coma: un valor con coma las rompería.
            var product = NewProduct();

            Should.Throw<ArgumentException>(() =>
                product.UpdateExternalData(
                    new ProductExternalData { AllergenTags = new List<string> { "en:milk,en:nuts" } },
                    UpdatedAt));
        }

        [Fact]
        public void UpdateExternalData_Should_Not_Change_Product_When_Any_Value_Is_Invalid()
        {
            var product = NewProduct();
            product.UpdateExternalData(CompleteData(), UpdatedAt);

            // Todo válido salvo el NOVA: no se tiene que asignar ningún dato.
            Should.Throw<ArgumentException>(() =>
                product.UpdateExternalData(new ProductExternalData
                {
                    ImageUrl = "https://images.openfoodfacts.org/otra.jpg",
                    Categories = new List<string> { "Bebidas" },
                    IngredientsText = "Agua",
                    AllergenTags = new List<string>(),
                    NutriScore = "a",
                    NovaGroup = 9
                }, UpdatedAt.AddDays(1)));

            product.ImageUrl.ShouldBe("https://images.openfoodfacts.org/front.jpg");
            product.GetCategories().ShouldBe(new[] { "Desayunos", "Untables" });
            product.IngredientsText.ShouldBe("Azúcar, aceite vegetal, avellanas");
            product.GetAllergenTags().ShouldBe(new[] { "en:milk", "en:nuts" });
            product.NutriScore.ShouldBe("e");
            product.NovaGroup.ShouldBe(4);
            product.ExternalDataUpdatedAt.ShouldBe(UpdatedAt);
        }

        [Fact]
        public void UpdateExternalData_Should_Throw_When_Data_Is_Null()
        {
            var product = NewProduct();

            Should.Throw<ArgumentNullException>(() => product.UpdateExternalData(null!, UpdatedAt));
        }

        // ---------------------------------------------------------------------

        private static Product NewProduct()
        {
            return new Product(Guid.NewGuid(), "3017620422003", "Nutella", "Ferrero");
        }

        private static ProductExternalData CompleteData()
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
    }
}
