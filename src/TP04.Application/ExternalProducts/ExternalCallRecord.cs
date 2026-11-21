using System;

namespace TP04.ExternalProducts;

/// <summary>
/// TP final (HU-05, regla de negocio 5): datos de UNA llamada al catálogo externo.
///
/// A propósito no tiene código de barras, texto buscado, usuario, tokens ni la
/// respuesta del proveedor: las métricas solo necesitan qué se hizo, cómo terminó y
/// cuánto tardó. Lo genera el Frente 1 y lo persiste el Frente 4.
/// </summary>
public class ExternalCallRecord
{
    public ExternalCallOperation Operation { get; init; }

    public ExternalCallOutcome Outcome { get; init; }

    public long DurationMs { get; init; }

    /// <summary>Momento de la llamada, en UTC.</summary>
    public DateTime OccurredAt { get; init; }
}
