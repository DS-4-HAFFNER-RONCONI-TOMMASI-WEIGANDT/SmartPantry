using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace TP04.ExternalProducts
{
    /// <summary>
    /// Consulta de productos en el catálogo externo por código de barras (TP07 / RF-05).
    /// Endpoint generado por ABP: GET /api/app/external-product-lookup/by-barcode?Barcode={codigo}
    /// </summary>
    public interface IExternalProductLookupAppService : IApplicationService
    {
        Task<ExternalProductLookupResultDto> GetByBarcodeAsync(ExternalProductSearchDto input);
    }
}
