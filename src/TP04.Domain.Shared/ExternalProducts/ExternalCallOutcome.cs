namespace TP04.ExternalProducts;

/// <summary>
/// TP final (HU-05): resultado de una llamada al catálogo externo, para las métricas.
/// Nombre compartido entre frentes.
/// </summary>
public enum ExternalCallOutcome
{
    /// <summary>El proveedor respondió con datos.</summary>
    Success = 1,

    /// <summary>El proveedor respondió, pero no tiene el producto.</summary>
    NotFound = 2,

    /// <summary>El proveedor rechazó la llamada por límite de solicitudes.</summary>
    RateLimited = 3,

    /// <summary>El proveedor no respondió, falló o devolvió algo inutilizable.</summary>
    Unavailable = 4
}
