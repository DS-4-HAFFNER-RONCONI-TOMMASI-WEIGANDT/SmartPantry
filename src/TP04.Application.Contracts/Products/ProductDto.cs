using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace TP04.Products
{
    public class ProductDto : AuditedEntityDto<Guid>
    {
        public string Barcode { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Brand { get; set; }

        // TP final (RF-07 / RF-08): datos guardados desde Open Food Facts.
        // null = el proveedor no lo informó (RF-09); la interfaz muestra "No disponible".

        public string? ImageUrl { get; set; }

        public List<string>? Categories { get; set; }

        public string? IngredientsText { get; set; }

        /// <summary>
        /// Tags de Open Food Facts, por ejemplo "en:milk". null = no informado;
        /// lista vacía = informado, sin alérgenos detectados.
        /// </summary>
        public List<string>? AllergenTags { get; set; }

        /// <summary>Nutri-Score de "a" a "e".</summary>
        public string? NutriScore { get; set; }

        /// <summary>Grupo NOVA de 1 a 4.</summary>
        public int? NovaGroup { get; set; }

        /// <summary>Última vez que se guardaron los datos externos (UTC). null = nunca.</summary>
        public DateTime? ExternalDataUpdatedAt { get; set; }
    }
}
