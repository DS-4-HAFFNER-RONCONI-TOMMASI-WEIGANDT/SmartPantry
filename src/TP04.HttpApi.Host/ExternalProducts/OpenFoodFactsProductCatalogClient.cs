using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace TP04.ExternalProducts
{
    /// <summary>
    /// Implementación de <see cref="IExternalProductCatalogClient"/> sobre Open Food Facts
    /// (Product Opener API v3).
    ///
    /// Recibe el HttpClient por constructor: lo crea y configura IHttpClientFactory
    /// (URL base, tiempo máximo de espera y User-Agent) en TP04HttpApiHostModule.
    /// Nunca se instancia con new HttpClient().
    ///
    /// Responsabilidades:
    ///   - Armar la ruta de API v3 con los campos necesarios.
    ///   - Interpretar los códigos HTTP del proveedor:
    ///       200 -> producto (null si la respuesta no trae producto)
    ///       404 -> null (no existe)
    ///       429 -> ExternalCatalogRateLimitException
    ///       otros / timeout / error de red / JSON inválido -> ExternalCatalogUnavailableException
    ///   - Traducir el JSON externo al DTO interno ExternalProductDto.
    /// Si el proveedor cambia su respuesta, la adaptación se hace solo en esta clase.
    /// </summary>
    public class OpenFoodFactsProductCatalogClient : IExternalProductCatalogClient
    {
        internal const string ProductPath = "api/v3/product/";

        internal const string ProductFields =
            "code,product_name,product_name_es,brands,image_front_url,image_url";

        private const string FailureStatus = "failure";

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenFoodFactsProductCatalogClient> _logger;

        public OpenFoodFactsProductCatalogClient(
            HttpClient httpClient,
            ILogger<OpenFoodFactsProductCatalogClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ExternalProductDto?> GetByBarcodeAsync(string barcode)
        {
            var code = Check.NotNullOrWhiteSpace(barcode, nameof(barcode)).Trim();
            var requestUri = $"{ProductPath}{Uri.EscapeDataString(code)}?fields={ProductFields}";

            HttpResponseMessage response;

            try
            {
                // Se usa el modo por defecto (ResponseContentRead) para que el tiempo máximo
                // de espera configurado cubra también la lectura completa del cuerpo.
                response = await _httpClient.GetAsync(requestUri);
            }
            catch (OperationCanceledException ex)
            {
                // HttpClient informa el tiempo de espera agotado como TaskCanceledException.
                _logger.LogWarning(ex, "Open Food Facts no respondió a tiempo para el código {Barcode}.", code);
                throw new ExternalCatalogUnavailableException(
                    "Open Food Facts no respondió dentro del tiempo máximo de espera.", ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Error de red al consultar Open Food Facts para el código {Barcode}.", code);
                throw new ExternalCatalogUnavailableException(
                    "No se pudo establecer la comunicación con Open Food Facts.", ex);
            }

            using (response)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("Open Food Facts limitó las solicitudes (HTTP 429) para el código {Barcode}.", code);
                    throw new ExternalCatalogRateLimitException(
                        "Open Food Facts rechazó la consulta por límite de solicitudes (HTTP 429).");
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Open Food Facts respondió HTTP {StatusCode} para el código {Barcode}.",
                        (int)response.StatusCode,
                        code);
                    throw new ExternalCatalogUnavailableException(
                        $"Open Food Facts respondió HTTP {(int)response.StatusCode}.");
                }

                var body = await ReadBodyAsync(response, code);

                if (body?.Product == null
                    || string.Equals(body.Status, FailureStatus, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return MapToExternalProduct(code, body);
            }
        }

        private async Task<OpenFoodFactsProductResponse?> ReadBodyAsync(HttpResponseMessage response, string code)
        {
            try
            {
                return await response.Content.ReadFromJsonAsync<OpenFoodFactsProductResponse>(JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Open Food Facts devolvió un JSON inválido para el código {Barcode}.", code);
                throw new ExternalCatalogUnavailableException(
                    "Open Food Facts devolvió una respuesta que no se puede interpretar.", ex);
            }
            catch (NotSupportedException ex)
            {
                // Content-Type distinto de JSON (por ejemplo, una página HTML de error).
                _logger.LogWarning(ex, "Open Food Facts devolvió un contenido no JSON para el código {Barcode}.", code);
                throw new ExternalCatalogUnavailableException(
                    "Open Food Facts devolvió una respuesta que no es JSON.", ex);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Se agotó el tiempo leyendo la respuesta de Open Food Facts para el código {Barcode}.", code);
                throw new ExternalCatalogUnavailableException(
                    "Open Food Facts no completó la respuesta dentro del tiempo máximo de espera.", ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Se cortó la respuesta de Open Food Facts para el código {Barcode}.", code);
                throw new ExternalCatalogUnavailableException(
                    "Se interrumpió la respuesta de Open Food Facts.", ex);
            }
        }

        private static ExternalProductDto MapToExternalProduct(string requestedCode, OpenFoodFactsProductResponse body)
        {
            var product = body.Product!;

            return new ExternalProductDto
            {
                Barcode = FirstNonBlank(body.Code) ?? requestedCode,
                Name = FirstNonBlank(product.ProductNameEs, product.ProductName),
                Brand = FirstNonBlank(product.Brands),
                ImageUrl = FirstNonBlank(product.ImageFrontUrl, product.ImageUrl)
            };
        }

        private static string? FirstNonBlank(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return null;
        }
    }
}
