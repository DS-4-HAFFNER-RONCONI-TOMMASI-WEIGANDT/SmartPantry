using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace TP04.Products
{
    public interface IExternalProductAppService : IApplicationService
    {
        Task<ExternalProductDto> SearchByBarcodeAsync(ExternalProductSearchDto input);
    }
}
