using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace TP04.Products
{
    // TP06: migrado del servicio manual de TP05 a CrudAppService.
    //
    // Provienen del servicio base SIN cambios:
    //   - GetAsync(id)      -> Repository.GetAsync + mapeo a ProductDto (404 si no existe).
    //   - GetListAsync(input) -> filtrado, conteo, ordenamiento dinámico (input.Sorting),
    //                          paginado (SkipCount/MaxResultCount) y mapeo a ProductDto.
    //   - DeleteAsync(id)   -> Repository.DeleteAsync (borrado físico: Product no es ISoftDelete).
    //
    // Se adaptan (override) porque intervienen reglas del dominio:
    //   - CreateAsync -> la creación pasa por ProductManager (barcode único, normalización).
    //                   El base mapearía CreateProductDto -> Product con ObjectMapper, lo que
    //                   saltearía el Domain Service (y el constructor de Product es internal).
    //   - UpdateAsync -> la modificación pasa por ProductManager.UpdateAsync (unicidad del
    //                   barcode si cambia + validación atómica en Product.Update). El base
    //                   copiaría el DTO sobre la entidad con ObjectMapper, salteando las reglas.
    //   - ApplyDefaultSorting -> orden por nombre cuando el cliente no envía Sorting.
    //
    // Decisión temporal (TP05, se mantiene en TP06): [AllowAnonymous] habilita el acceso
    // sin login únicamente para verificar las operaciones en Swagger, porque todavía no se
    // implementó autenticación. No representa la política final de autorización.
    [AllowAnonymous]
    public class ProductAppService :
        CrudAppService<
            Product,
            ProductDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateProductDto,
            UpdateProductDto>,
        IProductAppService
    {
        private readonly ProductManager _productManager;

        public ProductAppService(
            IRepository<Product, Guid> repository,
            ProductManager productManager)
            : base(repository)
        {
            _productManager = productManager;
        }

        public override async Task<ProductDto> CreateAsync(CreateProductDto input)
        {
            await CheckCreatePolicyAsync();

            var product = await _productManager.CreateAsync(
                input.Barcode,
                input.Name,
                input.Brand
            );

            // autoSave: true fuerza el SaveChanges ahora, para que CreationTime ya
            // esté completo en el DTO que se devuelve.
            await Repository.InsertAsync(product, autoSave: true);

            return await MapToGetOutputDtoAsync(product);
        }

        public override async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto input)
        {
            await CheckUpdatePolicyAsync();

            // Id inexistente -> EntityNotFoundException (404), igual que en GetAsync.
            var product = await GetEntityByIdAsync(id);

            // Si algo es inválido, ProductManager/Product lanzan la excepción antes de
            // modificar la entidad y la unidad de trabajo no persiste nada.
            await _productManager.UpdateAsync(
                product,
                input.Barcode,
                input.Name,
                input.Brand
            );

            await Repository.UpdateAsync(product, autoSave: true);

            return await MapToGetOutputDtoAsync(product);
        }

        protected override IQueryable<Product> ApplyDefaultSorting(IQueryable<Product> query)
        {
            return query.OrderBy(p => p.Name);
        }
    }
}
