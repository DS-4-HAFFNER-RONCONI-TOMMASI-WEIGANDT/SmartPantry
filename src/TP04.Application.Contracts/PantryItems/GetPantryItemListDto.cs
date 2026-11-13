using Volo.Abp.Application.Dtos;

namespace TP04.PantryItems
{
    /// <summary>
    /// TP10 (RF-11): listado paginado de la despensa propia.
    ///
    /// Status es opcional: Active muestra lo que hay en la despensa, Consumed el
    /// historial (regla de negocio 6) y null ambos.
    ///
    /// Sorting admite "productName", "productName desc", "expirationDate" y
    /// "expirationDate desc". Sin Sorting: primero activos, luego por fecha de
    /// vencimiento (los sin fecha al final) y por nombre.
    /// </summary>
    public class GetPantryItemListDto : PagedAndSortedResultRequestDto
    {
        public PantryItemStatus? Status { get; set; }
    }
}
