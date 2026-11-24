using System.Collections.Generic;

namespace TP04.Products
{
    /// <summary>
    /// RF-07 / RF-08: datos de Open Food Facts que se guardan en el producto interno.
    ///
    /// Es un tipo propio del dominio porque Domain no puede depender de los DTOs de
    /// Application (ExternalProductDto). Quien guarda un producto externo traduce el
    /// DTO a este tipo. Todos los datos son opcionales: null = el proveedor no lo
    /// informó (RF-09). Las reglas (formato y largos) las valida
    /// <see cref="Product.UpdateExternalData"/>.
    /// </summary>
    public class ProductExternalData
    {
        public string? ImageUrl { get; init; }

        public List<string>? Categories { get; init; }

        public string? IngredientsText { get; init; }

        /// <summary>
        /// Tags canónicos, por ejemplo "en:milk". null = no informado; una lista vacía =
        /// informado, sin alérgenos detectados.
        /// </summary>
        public List<string>? AllergenTags { get; init; }

        /// <summary>Nutri-Score de "a" a "e".</summary>
        public string? NutriScore { get; init; }

        /// <summary>Grupo NOVA de 1 a 4.</summary>
        public int? NovaGroup { get; init; }
    }
}
