using System;

namespace TP04.ExternalProducts
{
    public class ExternalCatalogRateLimitException : Exception
    {
        public ExternalCatalogRateLimitException(string message, Exception? inner = null) : base(message, inner) { }
    }

    public class ExternalCatalogUnavailableException : Exception
    {
        public ExternalCatalogUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
    }
}