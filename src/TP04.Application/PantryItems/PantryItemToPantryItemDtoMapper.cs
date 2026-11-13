using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace TP04.PantryItems;

[Mapper]
public partial class PantryItemToPantryItemDtoMapper : MapperBase<PantryItem, PantryItemDto>
{
    public override partial PantryItemDto Map(PantryItem source);

    public override partial void Map(PantryItem source, PantryItemDto destination);
}