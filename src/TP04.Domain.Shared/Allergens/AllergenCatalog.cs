using System;
using System.Collections.Generic;

namespace TP04.Allergens;

/// <summary>
/// TP final (RF-03 / RF-07 / RF-12): los 14 alérgenos canónicos de Open Food Facts.
///
/// Es la lista común que usan el detalle del producto (Frente 1), las preferencias y
/// las advertencias (Frente 3). Los tags tienen el formato del proveedor ("en:milk") y
/// cada uno tiene su texto localizado en es.json / en.json con la clave "Allergen:milk".
/// Nombre compartido entre frentes: no renombrar sin avisar al grupo.
/// </summary>
public static class AllergenCatalog
{
    public const string TagPrefix = "en:";
    public const string LocalizationKeyPrefix = "Allergen:";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        "en:gluten",
        "en:crustaceans",
        "en:eggs",
        "en:fish",
        "en:peanuts",
        "en:soybeans",
        "en:milk",
        "en:nuts",
        "en:celery",
        "en:mustard",
        "en:sesame-seeds",
        "en:sulphur-dioxide-and-sulphites",
        "en:lupin",
        "en:molluscs"
    };

    private static readonly HashSet<string> KnownTags = new(All, StringComparer.Ordinal);

    /// <summary>
    /// true si el tag es uno de los 14 alérgenos. Ignora mayúsculas y espacios
    /// alrededor; null o vacío devuelven false.
    /// </summary>
    public static bool IsKnown(string? tag)
    {
        return Normalize(tag) is { } normalized && KnownTags.Contains(normalized);
    }

    /// <summary>
    /// Clave de localización del alérgeno, por ejemplo "en:milk" → "Allergen:milk".
    /// Un tag desconocido lanza ArgumentException: así nadie muestra una clave sin
    /// texto; quien llama comprueba antes con <see cref="IsKnown"/>.
    /// </summary>
    public static string GetLocalizationKey(string tag)
    {
        var normalized = Normalize(tag);

        if (normalized is null || !KnownTags.Contains(normalized))
        {
            throw new ArgumentException($"'{tag}' no es un alérgeno conocido del catálogo.", nameof(tag));
        }

        return LocalizationKeyPrefix + normalized.Substring(TagPrefix.Length);
    }

    private static string? Normalize(string? tag)
    {
        return string.IsNullOrWhiteSpace(tag) ? null : tag.Trim().ToLowerInvariant();
    }
}
