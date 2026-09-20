using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace TP04.Products
{
    /// <summary>
    /// <<DomainService>>
    /// Regla de negocio 4 de la especificación funcional: un producto interno se
    /// identifica de manera única por su código de barras externo cuando dicho código
    /// existe. El criterio de aceptación de RF-08 pide explícitamente evitar duplicados
    /// para el mismo código de barras.
    ///
    /// Esta regla necesita consultar el repositorio (¿ya existe ese barcode?), y un
    /// Aggregate Root no debería depender de un repositorio. Por eso es un Domain
    /// Service: coordina una regla que no pertenece a un único Aggregate.
    ///
    /// Nota para la Wiki: el Modelo de Dominio del TP03 dice "No se identificó un
    /// Domain Service necesario en este modelo inicial". Este caso lo reemplaza y hay
    /// que actualizar esa página.
    /// </summary>
    public class ProductManager : DomainService
    {
        private readonly IRepository<Product, Guid> _productRepository;

        public ProductManager(IRepository<Product, Guid> productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<Product> CreateAsync(string barcode, string name, string? brand = null)
        {
            // Se normaliza ANTES de chequear unicidad: si no, "  123  " y "123" pasan
            // el chequeo como distintos y el duplicado lo termina rechazando el índice
            // único de la base con una excepción de infraestructura (500) en lugar de
            // esta BusinessException.
            var normalizedBarcode = Check.NotNullOrWhiteSpace(
                barcode,
                nameof(barcode),
                maxLength: ProductConsts.MaxBarcodeLength
            ).Trim();

            await CheckBarcodeIsUniqueAsync(normalizedBarcode);

            return new Product(
                GuidGenerator.Create(),
                normalizedBarcode,
                name,
                brand
            );
        }

        private async Task CheckBarcodeIsUniqueAsync(string barcode)
        {
            var existing = await _productRepository.FindAsync(p => p.Barcode == barcode);
            if (existing != null)
            {
                throw new BusinessException(code: "TP04:Product:DuplicateBarcode")
                    .WithData("Barcode", barcode);
            }
        }
    }
}
