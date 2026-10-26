using System.Threading.Tasks;

namespace TP04.ExternalProducts
{
    public interface IExternalProductCatalogClient
    {
        Task<ExternalProductDto?> GetByBarcodeAsync(string barcode);
    }
}
