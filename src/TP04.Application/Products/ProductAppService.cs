using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using TP04.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace TP04.Products
{
    // TP06: migrado del servicio manual de TP05 a CrudAppService.
    //
    // Se sobrescriben solo para aplicar la autorización (la lógica es la del base):
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
    // TP09 (RF-08): se reemplaza el [AllowAnonymous] temporal de TP05/TP06 por [Authorize].
    // Alcanza con estar autenticado (sin política específica), así el usuario común
    // conserva el guardado de productos. Sin sesión ni token la API responde 401.
    //
    // TP final: el catálogo es compartido. Crear, consultar y listar quedan para
    // cualquier usuario autenticado; editar y borrar exigen TP04.Products.Manage (admin),
    // porque un producto puede estar en la despensa de otros usuarios.
    [Authorize]
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

            // Las aplican CheckUpdatePolicyAsync() (en UpdateAsync) y el DeleteAsync del base.
            UpdatePolicyName = TP04Permissions.Products.Manage;
            DeletePolicyName = TP04Permissions.Products.Manage;
        }

        // GetAsync, GetListAsync y DeleteAsync se sobrescriben solo llamando a base.
        // Motivo: el interceptor de autorización de ABP busca [Authorize] en el método
        // y en la clase que lo DECLARA. Un método heredado se declara en CrudAppService
        // (sin [Authorize]), así que al llamar al servicio sin pasar por HTTP quedaba
        // abierto a un anónimo. Declarándolos acá se aplica el [Authorize] de esta clase.
        // (Por HTTP ya estaban protegidos por el controlador; lo detectó ProductAuthorization_Tests.)
        public override Task<ProductDto> GetAsync(Guid id)
        {
            return base.GetAsync(id);
        }

        public override Task<PagedResultDto<ProductDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            return base.GetListAsync(input);
        }

        public override Task DeleteAsync(Guid id)
        {
            return base.DeleteAsync(id);
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
