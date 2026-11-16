using System;
using Volo.Abp.Application.Dtos;

namespace TP04.PantryItems
{
    /// <summary>
    /// TP10 (RF-11): ítem de la despensa del usuario autenticado, con el nombre del
    /// producto para mostrarlo en la pantalla "Mi despensa".
    ///
    /// RF-09: Quantity, Unit y ExpirationDate llegan en null cuando el dato no está
    /// disponible. El servidor no inventa valores; la interfaz lo comunica con un
    /// texto localizado. El propietario no se expone: siempre es el usuario actual.
    /// </summary>
    public class PantryItemDto : AuditedEntityDto<Guid>
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string? ProductBrand { get; set; }

        public decimal? Quantity { get; set; }

        public string? Unit { get; set; }

        /// <summary>Fecha calendario sin hora; en JSON viaja como "yyyy-MM-dd".</summary>
        public DateOnly? ExpirationDate { get; set; }

        public PantryItemStatus Status { get; set; }
    }
}
