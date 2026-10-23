using System.Threading.Tasks;

namespace TP04.ExternalProducts
{
    public class ExternalProductLookupAppService : TP04AppService, IExternalProductLookupAppService
    {
        private readonly IExternalProductCatalogClient _catalogClient;

        public ExternalProductLookupAppService(IExternalProductCatalogClient catalogClient)
        {
            _catalogClient = catalogClient;
        }

        public async Task<ExternalProductLookupResultDto> GetByBarcodeAsync(ExternalProductSearchDto input)
        {
            var barcode = input.Barcode.Trim();

            try
            {
                var external = await _catalogClient.GetByBarcodeAsync(barcode);

                return external is null
                    ? WithoutProduct(barcode, ExternalProductLookupStatus.NotFound)
                    : MapFound(barcode, external);
            }
            catch (ExternalCatalogRateLimitException)
            {
                return WithoutProduct(barcode, ExternalProductLookupStatus.RateLimited);
            }
            catch (ExternalCatalogUnavailableException)
            {
                return WithoutProduct(barcode, ExternalProductLookupStatus.Unavailable);
            }
        }

        private static ExternalProductLookupResultDto WithoutProduct(string barcode, ExternalProductLookupStatus status)
        {
            // Name, Brand e ImageUrl quedan en null y MissingFields vacío (como documenta el contrato).
            return new ExternalProductLookupResultDto { Status = status, Barcode = barcode };
        }

        private static ExternalProductLookupResultDto MapFound(string barcode, ExternalProductDto external)
        {
            var result = new ExternalProductLookupResultDto
            {
                Status = ExternalProductLookupStatus.Found,
                Barcode = string.IsNullOrWhiteSpace(external.Barcode) ? barcode : external.Barcode.Trim(),
                Name = NullIfBlank(external.Name),
                Brand = NullIfBlank(external.Brand),
                ImageUrl = NullIfBlank(external.ImageUrl)
            };

            // RF-09: el campo queda en null y se informa su nombre; no se inventa ningún valor.
            if (result.Name is null) result.MissingFields.Add(ExternalProductLookupResultDto.NameField);
            if (result.Brand is null) result.MissingFields.Add(ExternalProductLookupResultDto.BrandField);
            if (result.ImageUrl is null) result.MissingFields.Add(ExternalProductLookupResultDto.ImageUrlField);

            return result;
        }

        private static string? NullIfBlank(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}