using System.Text.Json.Serialization;

namespace TP04.ExternalProducts
{
    /// <summary>
    /// Resultado de la consulta al catálogo externo, expresado en términos propios
    /// de SmartPantry (no en códigos ni estados del proveedor).
    /// Se serializa como texto para que el cliente lea "Found", "NotFound", etc.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ExternalProductLookupStatus
    {
        /// <summary>El producto existe en el catálogo externo.</summary>
        Found = 1,

        /// <summary>El catálogo externo no tiene un producto con ese código.</summary>
        NotFound = 2,

        /// <summary>El proveedor rechazó la consulta por exceder el límite de solicitudes.</summary>
        RateLimited = 3,

        /// <summary>El proveedor no respondió, falló o devolvió una respuesta inutilizable.</summary>
        Unavailable = 4
    }
}
