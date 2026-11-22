namespace TP04.ExternalProducts;

/// <summary>
/// TP final (HU-05, regla de negocio 5): operación contra el catálogo externo que se
/// registra para las métricas. Nombre compartido entre frentes.
/// </summary>
public enum ExternalCallOperation
{
    /// <summary>Consulta por código de barras (Product Opener API v3, RF-05).</summary>
    BarcodeLookup = 1,

    /// <summary>Búsqueda por texto (Search-a-licious, RF-06).</summary>
    TextSearch = 2
}
