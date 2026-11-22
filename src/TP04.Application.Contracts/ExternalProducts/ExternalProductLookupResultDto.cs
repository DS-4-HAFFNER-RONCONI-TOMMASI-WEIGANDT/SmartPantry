using System.Collections.Generic;

namespace TP04.ExternalProducts
{
    /// <summary>
    /// Salida propia de la consulta por código de barras (RF-05).
    ///
    /// No replica la estructura JSON de Open Food Facts: si el proveedor cambia su
    /// respuesta, la adaptación se hace en el cliente externo y este contrato no cambia.
    ///
    /// RF-09 (datos incompletos): cuando el producto se encontró pero el proveedor no
    /// informa nombre, marca o imagen, el campo queda en null (no se inventa un valor)
    /// y su nombre se agrega a <see cref="MissingFields"/> para que el cliente lo reconozca.
    /// En los estados distintos de <see cref="ExternalProductLookupStatus.Found"/> los
    /// datos del producto quedan en null y <see cref="MissingFields"/> vacío.
    ///
    /// TP final (RF-07): se agregan categorías, ingredientes, alérgenos, Nutri-Score y
    /// NOVA. Son contrato compartido entre frentes (no renombrar sin avisar al grupo).
    /// Hasta que el Frente 1 complete el mapeo, quedan en null y todavía no se
    /// informan en <see cref="MissingFields"/>; después siguen la misma regla RF-09.
    /// </summary>
    public class ExternalProductLookupResultDto
    {
        public const string NameField = "Name";
        public const string BrandField = "Brand";
        public const string ImageUrlField = "ImageUrl";
        public const string CategoriesField = "Categories";
        public const string IngredientsTextField = "IngredientsText";
        public const string AllergenTagsField = "AllergenTags";
        public const string NutriScoreField = "NutriScore";
        public const string NovaGroupField = "NovaGroup";

        public ExternalProductLookupStatus Status { get; set; }

        public string Barcode { get; set; } = string.Empty;

        public string? Name { get; set; }

        public string? Brand { get; set; }

        public string? ImageUrl { get; set; }

        public List<string>? Categories { get; set; }

        public string? IngredientsText { get; set; }

        /// <summary>Tags canónicos de Open Food Facts, por ejemplo "en:milk" (ver AllergenCatalog).</summary>
        public List<string>? AllergenTags { get; set; }

        /// <summary>Nutri-Score de "a" a "e".</summary>
        public string? NutriScore { get; set; }

        /// <summary>Grupo NOVA de 1 a 4.</summary>
        public int? NovaGroup { get; set; }

        public List<string> MissingFields { get; set; } = new();
    }
}
