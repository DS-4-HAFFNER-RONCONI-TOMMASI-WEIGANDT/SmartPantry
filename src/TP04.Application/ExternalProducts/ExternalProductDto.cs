using System.Collections.Generic;

namespace TP04.ExternalProducts
{
    public class ExternalProductDto
    {
        public string Barcode { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Brand { get; set; }
        public string? ImageUrl { get; set; }

        // TP final (RF-07): datos ampliados. Contrato compartido entre frentes; el mapeo
        // desde Open Food Facts lo completa el Frente 1. null = el proveedor no lo informó.

        public List<string>? Categories { get; set; }

        public string? IngredientsText { get; set; }

        /// <summary>Tags canónicos de Open Food Facts, por ejemplo "en:milk".</summary>
        public List<string>? AllergenTags { get; set; }

        /// <summary>Nutri-Score de "a" a "e".</summary>
        public string? NutriScore { get; set; }

        /// <summary>Grupo NOVA de 1 a 4.</summary>
        public int? NovaGroup { get; set; }
    }
}
