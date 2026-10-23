using System;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Xunit;

namespace TP04.ExternalProducts
{
    /// <summary>
    /// TP07: pruebas unitarias de ExternalProductLookupAppService.
    ///
    /// IExternalProductCatalogClient se reemplaza por un mock de NSubstitute: no se usa
    /// OpenFoodFactsProductCatalogClient, ni HttpClient, ni Internet, ni base de datos.
    /// El AppService se instancia directamente con su dependencia (ABP Unit Tests —
    /// Classes With Dependencies), sin levantar el módulo de la aplicación.
    /// </summary>
    public class ExternalProductLookupAppService_Tests
    {
        private const string ExistingBarcode = "3017620422003";
        private const string MissingBarcode = "0000000000000";

        private readonly IExternalProductCatalogClient _catalogClient;
        private readonly ExternalProductLookupAppService _appService;

        public ExternalProductLookupAppService_Tests()
        {
            _catalogClient = Substitute.For<IExternalProductCatalogClient>();
            _appService = new ExternalProductLookupAppService(_catalogClient);
        }

        // ---------------------------------------------------------------------
        // Producto encontrado
        // ---------------------------------------------------------------------

        [Fact]
        public async Task Should_Return_Found_With_Product_Data_When_Product_Exists()
        {
            _catalogClient
                .GetByBarcodeAsync(ExistingBarcode)
                .Returns(Task.FromResult<ExternalProductDto?>(new ExternalProductDto
                {
                    Barcode = ExistingBarcode,
                    Name = "Nutella",
                    Brand = "Ferrero",
                    ImageUrl = "https://images.openfoodfacts.org/images/products/301/762/042/2003/front_en.jpg"
                }));

            var result = await _appService.GetByBarcodeAsync(Search(ExistingBarcode));

            result.Status.ShouldBe(ExternalProductLookupStatus.Found);
            result.Barcode.ShouldBe(ExistingBarcode);
            result.Name.ShouldBe("Nutella");
            result.Brand.ShouldBe("Ferrero");
            result.ImageUrl.ShouldBe("https://images.openfoodfacts.org/images/products/301/762/042/2003/front_en.jpg");
            result.MissingFields.ShouldBeEmpty();

            await _catalogClient.Received(1).GetByBarcodeAsync(ExistingBarcode);
        }

        [Fact]
        public async Task Should_Trim_Product_Data_Received_From_Client()
        {
            _catalogClient
                .GetByBarcodeAsync(ExistingBarcode)
                .Returns(Task.FromResult<ExternalProductDto?>(new ExternalProductDto
                {
                    Barcode = ExistingBarcode,
                    Name = "  Nutella  ",
                    Brand = " Ferrero ",
                    ImageUrl = " https://images.openfoodfacts.org/front.jpg "
                }));

            var result = await _appService.GetByBarcodeAsync(Search(ExistingBarcode));

            result.Status.ShouldBe(ExternalProductLookupStatus.Found);
            result.Name.ShouldBe("Nutella");
            result.Brand.ShouldBe("Ferrero");
            result.ImageUrl.ShouldBe("https://images.openfoodfacts.org/front.jpg");
            result.MissingFields.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_Trim_Barcode_Before_Querying_Client()
        {
            _catalogClient
                .GetByBarcodeAsync(ExistingBarcode)
                .Returns(Task.FromResult<ExternalProductDto?>(new ExternalProductDto
                {
                    Barcode = ExistingBarcode,
                    Name = "Nutella",
                    Brand = "Ferrero",
                    ImageUrl = "https://images.openfoodfacts.org/front.jpg"
                }));

            var result = await _appService.GetByBarcodeAsync(Search("  " + ExistingBarcode + "  "));

            result.Status.ShouldBe(ExternalProductLookupStatus.Found);
            result.Barcode.ShouldBe(ExistingBarcode);

            await _catalogClient.Received(1).GetByBarcodeAsync(ExistingBarcode);
        }

        // ---------------------------------------------------------------------
        // Producto inexistente
        // ---------------------------------------------------------------------

        [Fact]
        public async Task Should_Return_NotFound_When_Client_Returns_Null()
        {
            _catalogClient
                .GetByBarcodeAsync(MissingBarcode)
                .Returns(Task.FromResult<ExternalProductDto?>(null));

            var result = await _appService.GetByBarcodeAsync(Search(MissingBarcode));

            result.Status.ShouldBe(ExternalProductLookupStatus.NotFound);
            result.Barcode.ShouldBe(MissingBarcode);
            result.Name.ShouldBeNull();
            result.Brand.ShouldBeNull();
            result.ImageUrl.ShouldBeNull();
            result.MissingFields.ShouldBeEmpty();

            await _catalogClient.Received(1).GetByBarcodeAsync(MissingBarcode);
        }

        // ---------------------------------------------------------------------
        // Datos ausentes (RF-09)
        // ---------------------------------------------------------------------

        [Fact]
        public async Task Should_Keep_Missing_Data_As_Null_And_Report_It_When_Provider_Omits_All_Fields()
        {
            _catalogClient
                .GetByBarcodeAsync(ExistingBarcode)
                .Returns(Task.FromResult<ExternalProductDto?>(new ExternalProductDto
                {
                    Barcode = ExistingBarcode,
                    Name = null,
                    Brand = null,
                    ImageUrl = null
                }));

            var result = await _appService.GetByBarcodeAsync(Search(ExistingBarcode));

            result.Status.ShouldBe(ExternalProductLookupStatus.Found);
            result.Barcode.ShouldBe(ExistingBarcode);
            result.Name.ShouldBeNull();
            result.Brand.ShouldBeNull();
            result.ImageUrl.ShouldBeNull();
            result.MissingFields.ShouldBe(new[]
            {
                ExternalProductLookupResultDto.NameField,
                ExternalProductLookupResultDto.BrandField,
                ExternalProductLookupResultDto.ImageUrlField
            });
        }

        [Fact]
        public async Task Should_Treat_Blank_Values_As_Missing_Without_Inventing_Data()
        {
            _catalogClient
                .GetByBarcodeAsync(ExistingBarcode)
                .Returns(Task.FromResult<ExternalProductDto?>(new ExternalProductDto
                {
                    Barcode = ExistingBarcode,
                    Name = "   ",
                    Brand = "",
                    ImageUrl = "\t "
                }));

            var result = await _appService.GetByBarcodeAsync(Search(ExistingBarcode));

            result.Status.ShouldBe(ExternalProductLookupStatus.Found);
            result.Name.ShouldBeNull();
            result.Brand.ShouldBeNull();
            result.ImageUrl.ShouldBeNull();
            result.MissingFields.ShouldBe(new[]
            {
                ExternalProductLookupResultDto.NameField,
                ExternalProductLookupResultDto.BrandField,
                ExternalProductLookupResultDto.ImageUrlField
            });
        }

        [Fact]
        public async Task Should_Report_Only_The_Fields_That_Are_Missing()
        {
            _catalogClient
                .GetByBarcodeAsync(ExistingBarcode)
                .Returns(Task.FromResult<ExternalProductDto?>(new ExternalProductDto
                {
                    Barcode = ExistingBarcode,
                    Name = "Nutella",
                    Brand = null,
                    ImageUrl = "https://images.openfoodfacts.org/front.jpg"
                }));

            var result = await _appService.GetByBarcodeAsync(Search(ExistingBarcode));

            result.Status.ShouldBe(ExternalProductLookupStatus.Found);
            result.Name.ShouldBe("Nutella");
            result.Brand.ShouldBeNull();
            result.ImageUrl.ShouldBe("https://images.openfoodfacts.org/front.jpg");
            result.MissingFields.ShouldBe(new[] { ExternalProductLookupResultDto.BrandField });
        }

        // ---------------------------------------------------------------------
        // Límite de solicitudes
        // ---------------------------------------------------------------------

        [Fact]
        public async Task Should_Return_RateLimited_When_Client_Reports_Rate_Limit()
        {
            _catalogClient
                .GetByBarcodeAsync(ExistingBarcode)
                .Returns(Task.FromException<ExternalProductDto?>(new ExternalCatalogRateLimitException("HTTP 429 simulado")));

            var result = await _appService.GetByBarcodeAsync(Search(ExistingBarcode));

            result.Status.ShouldBe(ExternalProductLookupStatus.RateLimited);
            result.Barcode.ShouldBe(ExistingBarcode);
            result.Name.ShouldBeNull();
            result.Brand.ShouldBeNull();
            result.ImageUrl.ShouldBeNull();
            result.MissingFields.ShouldBeEmpty();
        }

        // ---------------------------------------------------------------------
        // Proveedor no disponible
        // ---------------------------------------------------------------------

        [Fact]
        public async Task Should_Return_Unavailable_When_Client_Reports_Provider_Unavailable()
        {
            _catalogClient
                .GetByBarcodeAsync(ExistingBarcode)
                .Returns(Task.FromException<ExternalProductDto?>(new ExternalCatalogUnavailableException("Proveedor caído simulado")));

            var result = await _appService.GetByBarcodeAsync(Search(ExistingBarcode));

            result.Status.ShouldBe(ExternalProductLookupStatus.Unavailable);
            result.Barcode.ShouldBe(ExistingBarcode);
            result.Name.ShouldBeNull();
            result.Brand.ShouldBeNull();
            result.ImageUrl.ShouldBeNull();
            result.MissingFields.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_Not_Hide_Unexpected_Errors_As_Controlled_Results()
        {
            _catalogClient
                .GetByBarcodeAsync(ExistingBarcode)
                .Returns(Task.FromException<ExternalProductDto?>(new InvalidOperationException("Error inesperado")));

            await Should.ThrowAsync<InvalidOperationException>(async () =>
            {
                await _appService.GetByBarcodeAsync(Search(ExistingBarcode));
            });
        }

        // ---------------------------------------------------------------------

        private static ExternalProductSearchDto Search(string barcode)
        {
            return new ExternalProductSearchDto { Barcode = barcode };
        }
    }
}
