using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace TP04.Products
{
    // Implementado manualmente (sin CrudAppService) a pedido de la consigna del TP05,
    // para observar explícitamente el recorrido Application -> Domain (ProductManager)
    // -> EntityFrameworkCore.
    //
    // Decisión temporal (TP05): [AllowAnonymous] habilita el acceso sin login
    // únicamente para poder verificar la operación en Swagger, porque todavía no se
    // implementó autenticación. No representa la política final de autorización y se
    // revisará cuando se incorpore seguridad.
    [AllowAnonymous]
    public class ProductAppService : ApplicationService, IProductAppService
    {
        private readonly IRepository<Product, Guid> _productRepository;
        private readonly ProductManager _productManager;

        public ProductAppService(
            IRepository<Product, Guid> productRepository,
            ProductManager productManager)
        {
            _productRepository = productRepository;
            _productManager = productManager;
        }

        public async Task<ProductDto> CreateAsync(CreateProductDto input)
        {
            // La regla "no duplicar código de barras" (RF-08) se valida DENTRO de
            // ProductManager. Este servicio solo coordina: le pide al Domain Service
            // el Product ya validado y lo persiste.
            var product = await _productManager.CreateAsync(
                input.Barcode,
                input.Name,
                input.Brand
            );

            // autoSave: true fuerza el SaveChanges ahora, para que CreationTime ya
            // esté completo en el DTO que se devuelve.
            await _productRepository.InsertAsync(product, autoSave: true);

            return ObjectMapper.Map<Product, ProductDto>(product);
        }

        public async Task<ProductDto> GetAsync(Guid id)
        {
            // Id inexistente -> EntityNotFoundException automática de ABP (404).
            var product = await _productRepository.GetAsync(id);

            return ObjectMapper.Map<Product, ProductDto>(product);
        }
    }
}