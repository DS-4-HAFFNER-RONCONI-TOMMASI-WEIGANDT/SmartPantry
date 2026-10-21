using System.Threading.Tasks;

namespace TP04.Products
{
    public interface IExternalProductCatalogClient
    {
        Task<ExternalProductDto?> GetByBarcodeAsync(string barcode);
    }
}
