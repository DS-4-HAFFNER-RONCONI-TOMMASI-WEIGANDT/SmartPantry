using System;

namespace TP04.Products
{
    public class ExternalProductDto
    {
        public string Barcode { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Brand { get; set; }
        public string? ImageUrl { get; set; }
        
        // Propiedades útiles para identificar fallas o estados según pide el TP07
        public bool IsFound { get; set; }
        public bool IsServiceUnavailable { get; set; }
        public bool IsRateLimitExceeded { get; set; }
    }
}
