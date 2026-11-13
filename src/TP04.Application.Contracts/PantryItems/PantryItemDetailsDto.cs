using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using TP04.Localization;

namespace TP04.PantryItems
{
    /// <summary>
    /// TP10: datos editables de un ítem de despensa, compartidos por el alta (RF-10)
    /// y la edición (RF-11). Todos son opcionales (RF-09).
    ///
    /// La validación se hace en el servidor: cantidad no negativa, largo de la unidad
    /// y fecha dentro de un rango razonable. Que la fecha sea una fecha real lo
    /// garantiza el tipo DateOnly: un valor como "2026-02-30" no se puede convertir y
    /// la API responde 400. La interfaz repite estas reglas, pero no las reemplaza.
    /// </summary>
    public abstract class PantryItemDetailsDto : IValidatableObject
    {
        [Range(0, PantryItemConsts.MaxQuantity)]
        public decimal? Quantity { get; set; }

        [StringLength(PantryItemConsts.MaxUnitLength)]
        public string? Unit { get; set; }

        public DateOnly? ExpirationDate { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ExpirationDate.HasValue &&
                (ExpirationDate.Value < PantryItemConsts.MinExpirationDate ||
                 ExpirationDate.Value > PantryItemConsts.MaxExpirationDate))
            {
                var localizer = validationContext.GetRequiredService<IStringLocalizer<TP04Resource>>();

                yield return new ValidationResult(
                    localizer["Pantry:Validation:InvalidDate"],
                    new[] { nameof(ExpirationDate) });
            }
        }
    }
}
