using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;
using TP04.Authors;
using TP04.Books;
using TP04.Products;

namespace TP04;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class TP04BookToBookDtoMapper : MapperBase<Book, BookDto>
{
    [MapperIgnoreTarget(nameof(BookDto.AuthorName))]
    public override partial BookDto Map(Book source);

    [MapperIgnoreTarget(nameof(BookDto.AuthorName))]
    public override partial void Map(Book source, BookDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class TP04CreateUpdateBookDtoToBookMapper : MapperBase<CreateUpdateBookDto, Book>
{
    public override partial Book Map(CreateUpdateBookDto source);

    public override partial void Map(CreateUpdateBookDto source, Book destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class TP04AuthorToAuthorDtoMapper : MapperBase<Author, AuthorDto>
{
    public override partial AuthorDto Map(Author source);

    public override partial void Map(Author source, AuthorDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class TP04CreateUpdateAuthorDtoToAuthorMapper : MapperBase<CreateUpdateAuthorDto, Author>
{
    public override partial Author Map(CreateUpdateAuthorDto source);

    public override partial void Map(CreateUpdateAuthorDto source, Author destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class TP04AuthorToAuthorExcelDtoMapper : MapperBase<Author, AuthorExcelDto>
{
    public override partial AuthorExcelDto Map(Author source);

    public override partial void Map(Author source, AuthorExcelDto destination);
}

// Mapper de Product -> ProductDto (lo usa el ObjectMapper de ABP en ProductAppService).
//
// TP final (RF-07 / RF-08): se escribe a mano en lugar de generarlo con Mapperly.
// En la entidad, Categories y AllergenTags son texto separado por comas y en el DTO
// son listas: Mapperly no sabe convertir uno en otro, así que se usan los métodos
// del dominio GetCategories() y GetAllergenTags(). Mismo estilo que el mapeo manual
// de PantryItemAppService.MapToDto.
public class TP04ProductToProductDtoMapper : MapperBase<Product, ProductDto>
{
    public override ProductDto Map(Product source)
    {
        var destination = new ProductDto();
        Map(source, destination);
        return destination;
    }

    public override void Map(Product source, ProductDto destination)
    {
        destination.Id = source.Id;
        destination.CreationTime = source.CreationTime;
        destination.CreatorId = source.CreatorId;
        destination.LastModificationTime = source.LastModificationTime;
        destination.LastModifierId = source.LastModifierId;

        destination.Barcode = source.Barcode;
        destination.Name = source.Name;
        destination.Brand = source.Brand;

        destination.ImageUrl = source.ImageUrl;
        destination.Categories = source.GetCategories();
        destination.IngredientsText = source.IngredientsText;
        destination.AllergenTags = source.GetAllergenTags();
        destination.NutriScore = source.NutriScore;
        destination.NovaGroup = source.NovaGroup;
        destination.ExternalDataUpdatedAt = source.ExternalDataUpdatedAt;
    }
}