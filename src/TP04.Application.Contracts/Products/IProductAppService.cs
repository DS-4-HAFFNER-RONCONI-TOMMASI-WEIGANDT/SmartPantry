using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace TP04.Products
{
    // TP06: el contrato pasa a ser ICrudAppService. Mantiene las firmas de TP05
    // (CreateAsync(CreateProductDto) y GetAsync(Guid)) y agrega:
    //   - GetListAsync(PagedAndSortedResultRequestDto) -> PagedResultDto<ProductDto>
    //   - UpdateAsync(Guid, UpdateProductDto)
    //   - DeleteAsync(Guid)
    public interface IProductAppService :
        ICrudAppService<
            ProductDto,                     // DTO de salida (nunca la entidad)
            Guid,                           // Clave primaria
            PagedAndSortedResultRequestDto, // Paginado + ordenamiento de la lista
            CreateProductDto,               // Entrada de creación (TP05)
            UpdateProductDto>               // Entrada de modificación (TP06)
    {
    }
}
