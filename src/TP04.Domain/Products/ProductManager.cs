using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace TP04.Products
{
    /// <summary>
    /// <<DomainService>>
    /// Regla de negocio 4 / Invariante 4: no puede haber dos productos con el mismo
    /// código de barras. El criterio de aceptación de RF-08 pide explícitamente
    /// evitar duplicados para el mismo código de barras.
    ///
    /// Esta regla necesita consultar el repositorio (¿ya existe ese barcode?), y un
    /// Aggregate Root no debería depender de un repositorio. Por eso es un Domain
    /// Service: coordina una regla que no pertenece a un único Aggregate.
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
            var normalizedBarcode = Product.NormalizeBarcode(barcode);

            await CheckBarcodeIsUniqueAsync(normalizedBarcode);

            return new Product(
                GuidGenerator.Create(),
                normalizedBarcode,
                name,
                brand
            );
        }

        /// <summary>
        /// TP06: modifica un producto existente. El barcode es editable para poder
        /// corregir errores de carga sin borrar el producto (cuando exista la
        /// despensa, los PantryItem lo referencian por Id y el borrado deja de ser
        /// una opción). Si el barcode cambia, se vuelve a verificar la unicidad
        /// excluyendo al propio producto.
        ///
        /// Todas las verificaciones ocurren antes de modificar la entidad: una
        /// entrada inválida o un barcode duplicado no dejan cambios parciales.
        /// </summary>
        public async Task UpdateAsync(Product product, string barcode, string name, string? brand)
        {
            Check.NotNull(product, nameof(product));

            var normalizedBarcode = Product.NormalizeBarcode(barcode);

            if (normalizedBarcode != product.Barcode)
            {
                await CheckBarcodeIsUniqueAsync(normalizedBarcode, product.Id);
            }

            product.Update(normalizedBarcode, name, brand);
        }

        /// <summary>
        /// RF-08: guarda un producto de Open Food Facts en el catálogo interno sin
        /// duplicar el código de barras (regla de negocio 4).
        ///
        ///   - Si ya existe un producto con ese código: se actualizan solo sus datos
        ///     externos. Nombre y marca se conservan porque pueden haber sido corregidos
        ///     a mano (TP06).
        ///   - Si no existe: se crea. Si el proveedor no informa el nombre, se usa el
        ///     código de barras como nombre visible (Name es obligatorio y no se inventa
        ///     un nombre, RF-09).
        ///
        /// Como CreateAsync, no inserta ni actualiza en el repositorio: lo hace quien
        /// llama, según <see cref="ExternalProductSaveResult.AlreadyExisted"/>. El índice
        /// único de la base sigue de respaldo si dos usuarios guardan el mismo código a
        /// la vez.
        /// </summary>
        public async Task<ExternalProductSaveResult> SaveFromExternalAsync(
            string barcode,
            string? name,
            string? brand,
            ProductExternalData externalData)
        {
            Check.NotNull(externalData, nameof(externalData));

            // Se normaliza antes de buscar, igual que en CreateAsync: "  123  " y "123"
            // son el mismo producto.
            var normalizedBarcode = Product.NormalizeBarcode(barcode);

            var existing = await _productRepository.FindAsync(p => p.Barcode == normalizedBarcode);
            if (existing != null)
            {
                existing.UpdateExternalData(externalData, DateTime.UtcNow);
                return new ExternalProductSaveResult(existing, alreadyExisted: true);
            }

            var visibleName = string.IsNullOrWhiteSpace(name) ? normalizedBarcode : name;

            var product = new Product(GuidGenerator.Create(), normalizedBarcode, visibleName, brand);
            product.UpdateExternalData(externalData, DateTime.UtcNow);

            return new ExternalProductSaveResult(product, alreadyExisted: false);
        }

        private async Task CheckBarcodeIsUniqueAsync(string barcode, Guid? excludedProductId = null)
        {
            var existing = await _productRepository.FindAsync(p => p.Barcode == barcode);
            if (existing != null && existing.Id != excludedProductId)
            {
                throw new BusinessException(TP04DomainErrorCodes.ProductDuplicateBarcode)
                    .WithData("Barcode", barcode);
            }
        }
    }
}
