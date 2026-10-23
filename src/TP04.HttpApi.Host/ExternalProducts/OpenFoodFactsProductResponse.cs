using System.Text.Json.Serialization;

namespace TP04.ExternalProducts
{
    /// <summary>
    /// Respuesta de Product Opener API v3 (GET /api/v3/product/{barcode}).
    /// Solo se deserializan los campos que usa SmartPantry; el resto se ignora.
    /// Clase interna del cliente: nunca se expone como DTO del endpoint propio.
    /// </summary>
    internal sealed class OpenFoodFactsProductResponse
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        /// <summary>"success", "success_with_warnings" o "failure".</summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("product")]
        public OpenFoodFactsProduct? Product { get; set; }
    }

    /// <summary>
    /// Campos del producto solicitados con el parámetro "fields".
    /// Cualquiera puede faltar en la respuesta del proveedor.
    /// </summary>
    internal sealed class OpenFoodFactsProduct
    {
        [JsonPropertyName("product_name")]
        public string? ProductName { get; set; }

        [JsonPropertyName("product_name_es")]
        public string? ProductNameEs { get; set; }

        [JsonPropertyName("brands")]
        public string? Brands { get; set; }

        [JsonPropertyName("image_front_url")]
        public string? ImageFrontUrl { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }
    }
}
